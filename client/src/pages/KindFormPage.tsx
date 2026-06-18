import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useKind, useKindMutaties, useMedewerkers, useStamgroepen } from '../api/queries';
import { ApiFout } from '../api/client';
import { OpvangdagenKiezer } from '../components/OpvangdagenKiezer';
import { Contracttype, Weekdag, type KindInvoer } from '../types';

const LEEG: KindInvoer = {
  voornaam: '',
  achternaam: '',
  geboortedatum: '',
  stamgroepId: '',
  startdatum: '',
  einddatum: null,
  contracttype: Contracttype.Weken49,
  gewensteOpvangdagen: Weekdag.Maandag | Weekdag.Dinsdag,
  mentorId: null,
  oudercontact: null,
};

export function KindFormPage() {
  const { id } = useParams();
  const bewerkenModus = !!id;
  const navigate = useNavigate();

  const { data: groepen } = useStamgroepen();
  const { data: medewerkers } = useMedewerkers();
  const { data: bestaand } = useKind(id);
  const { aanmaken, bewerken } = useKindMutaties();

  const [model, setModel] = useState<KindInvoer>(LEEG);
  const [ouder, setOuder] = useState(false);
  const [fout, setFout] = useState<string | null>(null);

  useEffect(() => {
    if (bestaand) {
      setModel({
        voornaam: bestaand.voornaam,
        achternaam: bestaand.achternaam,
        geboortedatum: bestaand.geboortedatum,
        stamgroepId: bestaand.stamgroepId,
        startdatum: bestaand.startdatum,
        einddatum: bestaand.einddatum,
        contracttype: bestaand.contracttype,
        gewensteOpvangdagen: bestaand.gewensteOpvangdagen,
        mentorId: bestaand.mentorId,
        oudercontact: bestaand.oudercontact,
      });
      setOuder(!!bestaand.oudercontact);
    }
  }, [bestaand]);

  function zet<K extends keyof KindInvoer>(veld: K, waarde: KindInvoer[K]) {
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
    const invoer: KindInvoer = { ...model, oudercontact: ouder ? model.oudercontact : null };
    try {
      if (bewerkenModus && id) {
        await bewerken.mutateAsync({ id, invoer });
      } else {
        await aanmaken.mutateAsync(invoer);
      }
      navigate('/kinderen');
    } catch (err) {
      setFout(err instanceof ApiFout ? err.message : 'Opslaan mislukt.');
    }
  }

  const bezig = aanmaken.isPending || bewerken.isPending;

  return (
    <div className="view" style={{ maxWidth: 680 }}>
      <div className="ph">
        <div>
          <h1>{bewerkenModus ? 'Kind bewerken' : 'Kind toevoegen'}</h1>
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
            <Veld label="Stamgroep">
              <select required value={model.stamgroepId} onChange={(e) => zet('stamgroepId', e.target.value)}>
                <option value="">Kies…</option>
                {groepen?.map((g) => (
                  <option key={g.id} value={g.id}>
                    {g.naam} ({g.aantalKinderen}/{g.maxKinderen})
                  </option>
                ))}
              </select>
            </Veld>
            <Veld label="Startdatum">
              <input type="date" required value={model.startdatum} onChange={(e) => zet('startdatum', e.target.value)} />
            </Veld>
            <Veld label="Einddatum (optioneel)">
              <input
                type="date"
                value={model.einddatum ?? ''}
                onChange={(e) => zet('einddatum', e.target.value || null)}
              />
            </Veld>
            <Veld label="Contracttype">
              <select value={model.contracttype} onChange={(e) => zet('contracttype', Number(e.target.value))}>
                <option value={Contracttype.Weken49}>49 weken</option>
                <option value={Contracttype.Weken40}>40 weken (schoolweken)</option>
              </select>
            </Veld>
            <Veld label="Mentor (voor observaties)">
              <select value={model.mentorId ?? ''} onChange={(e) => zet('mentorId', e.target.value || null)}>
                <option value="">Geen mentor</option>
                {medewerkers?.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.voornaam} {m.achternaam}
                  </option>
                ))}
              </select>
            </Veld>
          </div>

          <div className="fld" style={{ marginTop: 4 }}>
            <label>Gewenste opvangdagen</label>
            <OpvangdagenKiezer waarde={model.gewensteOpvangdagen} onChange={(v) => zet('gewensteOpvangdagen', v)} />
          </div>

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
          <button type="button" onClick={() => navigate('/kinderen')} className="btn btn-outline btn-sm">
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
