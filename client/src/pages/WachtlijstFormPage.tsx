import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useStamgroepen, useWachtlijstInschrijving, useWachtlijstMutaties } from '../api/queries';
import { ApiFout } from '../api/client';
import { OpvangdagenKiezer } from '../components/OpvangdagenKiezer';
import { vandaagIso } from '../datum';
import { Contracttype, Weekdag, type WachtlijstInvoer } from '../types';

const LEEG: WachtlijstInvoer = {
  voornaam: '',
  achternaam: '',
  geboortedatum: '',
  inschrijfdatumWachtlijst: vandaagIso(),
  gewensteStartdatum: '',
  gewensteOpvangdagen: Weekdag.Maandag | Weekdag.Dinsdag,
  contracttype: Contracttype.Weken49,
  gewensteStamgroepId: null,
  isIntern: false,
  handmatigBovenaan: false,
  notitie: null,
  oudercontact: null,
};

export function WachtlijstFormPage() {
  const { id } = useParams();
  const bewerkenModus = !!id;
  const navigate = useNavigate();

  const { data: groepen } = useStamgroepen();
  const { data: bestaand } = useWachtlijstInschrijving(id);
  const { aanmaken, bewerken } = useWachtlijstMutaties();

  const [model, setModel] = useState<WachtlijstInvoer>(LEEG);
  const [ouder, setOuder] = useState(false);
  const [fout, setFout] = useState<string | null>(null);

  useEffect(() => {
    if (bestaand) {
      setModel({
        voornaam: bestaand.voornaam,
        achternaam: bestaand.achternaam,
        geboortedatum: bestaand.geboortedatum,
        inschrijfdatumWachtlijst: bestaand.inschrijfdatumWachtlijst,
        gewensteStartdatum: bestaand.gewensteStartdatum,
        gewensteOpvangdagen: bestaand.gewensteOpvangdagen,
        contracttype: bestaand.contracttype,
        gewensteStamgroepId: bestaand.gewensteStamgroepId,
        isIntern: bestaand.isIntern,
        handmatigBovenaan: bestaand.handmatigBovenaan,
        notitie: bestaand.notitie,
        oudercontact: bestaand.oudercontact,
      });
      setOuder(!!bestaand.oudercontact);
    }
  }, [bestaand]);

  function zet<K extends keyof WachtlijstInvoer>(veld: K, waarde: WachtlijstInvoer[K]) {
    setModel((m) => ({ ...m, [veld]: waarde }));
  }

  function zetOuder(veld: 'naam' | 'telefoon' | 'email', waarde: string) {
    setModel((m) => ({
      ...m,
      oudercontact: { naam: '', telefoon: '', email: '', ...m.oudercontact, [veld]: waarde },
    }));
  }

  async function verstuur(e: FormEvent) {
    e.preventDefault();
    setFout(null);
    const invoer: WachtlijstInvoer = { ...model, oudercontact: ouder ? model.oudercontact : null };
    try {
      if (bewerkenModus && id) {
        await bewerken.mutateAsync({ id, invoer });
      } else {
        await aanmaken.mutateAsync(invoer);
      }
      navigate('/wachtlijst');
    } catch (err) {
      setFout(err instanceof ApiFout ? err.message : 'Opslaan mislukt.');
    }
  }

  const bezig = aanmaken.isPending || bewerken.isPending;

  return (
    <div className="view" style={{ maxWidth: 680 }}>
      <div className="ph">
        <div>
          <h1>{bewerkenModus ? 'Inschrijving bewerken' : 'Inschrijving toevoegen'}</h1>
        </div>
      </div>

      {fout && (
        <div className="alert alert-bad">
          <i className="ti ti-alert-circle" />
          <span>{fout}</span>
        </div>
      )}

      <form onSubmit={verstuur} className="card">
        <div className="card-b">
          <div className="frow">
            <Veld label="Voornaam">
              <input required value={model.voornaam} onChange={(e) => zet('voornaam', e.target.value)} />
            </Veld>
            <Veld label="Achternaam">
              <input required value={model.achternaam} onChange={(e) => zet('achternaam', e.target.value)} />
            </Veld>
            <Veld label="Geboortedatum">
              <input type="date" required value={model.geboortedatum} onChange={(e) => zet('geboortedatum', e.target.value)} />
            </Veld>
            <Veld label="Inschrijfdatum wachtlijst">
              <input type="date" required value={model.inschrijfdatumWachtlijst} onChange={(e) => zet('inschrijfdatumWachtlijst', e.target.value)} />
            </Veld>
            <Veld label="Gewenste startdatum">
              <input type="date" required value={model.gewensteStartdatum} onChange={(e) => zet('gewensteStartdatum', e.target.value)} />
            </Veld>
            <Veld label="Contracttype">
              <select value={model.contracttype} onChange={(e) => zet('contracttype', Number(e.target.value))}>
                <option value={Contracttype.Weken49}>49 weken</option>
                <option value={Contracttype.Weken40}>40 weken (schoolweken)</option>
              </select>
            </Veld>
            <Veld label="Voorkeursgroep (optioneel)">
              <select value={model.gewensteStamgroepId ?? ''} onChange={(e) => zet('gewensteStamgroepId', e.target.value || null)}>
                <option value="">Geen voorkeur</option>
                {groepen?.map((g) => (
                  <option key={g.id} value={g.id}>
                    {g.naam}
                  </option>
                ))}
              </select>
            </Veld>
          </div>

          <div className="fld">
            <label>Gewenste opvangdagen</label>
            <OpvangdagenKiezer waarde={model.gewensteOpvangdagen} onChange={(v) => zet('gewensteOpvangdagen', v)} />
          </div>

          <div style={{ display: 'flex', gap: 24, flexWrap: 'wrap', marginBottom: 14 }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 12, fontWeight: 600, color: 'var(--text2)' }}>
              <input type="checkbox" checked={model.isIntern} onChange={(e) => zet('isIntern', e.target.checked)} />
              Interne aanvraag (broertje/zusje of doorstroom)
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 12, fontWeight: 600, color: 'var(--text2)' }}>
              <input type="checkbox" checked={model.handmatigBovenaan} onChange={(e) => zet('handmatigBovenaan', e.target.checked)} />
              Handmatig bovenaan (personeelskind)
            </label>
          </div>

          <Veld label="Notitie (optioneel)">
            <textarea value={model.notitie ?? ''} onChange={(e) => zet('notitie', e.target.value || null)} rows={2} />
          </Veld>

          <div style={{ borderTop: '1px solid var(--border)', paddingTop: 14, marginTop: 6 }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 12, fontWeight: 600, color: 'var(--text2)' }}>
              <input type="checkbox" checked={ouder} onChange={(e) => setOuder(e.target.checked)} />
              Oudercontact toevoegen
            </label>
            {ouder && (
              <div className="frow" style={{ marginTop: 12, gridTemplateColumns: '1fr 1fr 1fr' }}>
                <Veld label="Naam">
                  <input value={model.oudercontact?.naam ?? ''} onChange={(e) => zetOuder('naam', e.target.value)} />
                </Veld>
                <Veld label="Telefoon">
                  <input value={model.oudercontact?.telefoon ?? ''} onChange={(e) => zetOuder('telefoon', e.target.value)} />
                </Veld>
                <Veld label="E-mail">
                  <input type="email" value={model.oudercontact?.email ?? ''} onChange={(e) => zetOuder('email', e.target.value)} />
                </Veld>
              </div>
            )}
          </div>
        </div>

        <div className="modal-f" style={{ position: 'static', borderRadius: 0 }}>
          <button type="button" onClick={() => navigate('/wachtlijst')} className="btn btn-outline btn-sm">
            Annuleren
          </button>
          <button type="submit" disabled={bezig} className="btn btn-primary btn-sm">
            {bezig ? 'Opslaan…' : 'Opslaan'}
          </button>
        </div>
      </form>
    </div>
  );
}

function Veld({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="fld">
      <label>{label}</label>
      {children}
    </div>
  );
}
