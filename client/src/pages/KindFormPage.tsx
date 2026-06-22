import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useKind, useKindMutaties, useMedewerkers, useStamgroepen } from '../api/queries';
import { ApiFout } from '../api/client';
import { OpvangdagenKiezer } from '../components/OpvangdagenKiezer';
import { Contracttype, Weekdag, type KindInvoer, type OudercontactDto } from '../types';

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
  oudercontacten: [],
};

const LEEG_CONTACT: OudercontactDto = { naam: '', telefoon: '', email: '' };

export function KindFormPage() {
  const { id } = useParams();
  const bewerkenModus = !!id;
  const navigate = useNavigate();

  const { data: groepen } = useStamgroepen();
  const { data: medewerkers } = useMedewerkers();
  const { data: bestaand } = useKind(id);
  const { aanmaken, bewerken } = useKindMutaties();

  const [model, setModel] = useState<KindInvoer>(LEEG);
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
        oudercontacten: bestaand.oudercontacten ?? [],
      });
    }
  }, [bestaand]);

  function zet<K extends keyof KindInvoer>(veld: K, waarde: KindInvoer[K]) {
    setModel((m) => ({ ...m, [veld]: waarde }));
  }

  function zetContact(index: number, veld: keyof OudercontactDto, waarde: string) {
    setModel((m) => ({
      ...m,
      oudercontacten: m.oudercontacten.map((c, i) => (i === index ? { ...c, [veld]: waarde } : c)),
    }));
  }

  function voegContactToe() {
    setModel((m) => ({ ...m, oudercontacten: [...m.oudercontacten, { ...LEEG_CONTACT }] }));
  }

  function verwijderContact(index: number) {
    setModel((m) => ({ ...m, oudercontacten: m.oudercontacten.filter((_, i) => i !== index) }));
  }

  async function verstuur(e: FormEvent) {
    e.preventDefault();
    setFout(null);
    const invoer: KindInvoer = {
      ...model,
      oudercontacten: model.oudercontacten.filter((c) => c.naam.trim() || c.telefoon.trim() || c.email.trim()),
    };
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
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 4 }}>
              <span style={{ fontSize: 12, fontWeight: 600, color: 'var(--text2)' }}>
                Oudercontacten (ouder / verzorger / voogd)
              </span>
              <button type="button" onClick={voegContactToe} className="btn btn-outline btn-xs">
                <i className="ti ti-plus" /> Contact toevoegen
              </button>
            </div>
            <p style={{ fontSize: 10, color: 'var(--text3)', marginBottom: 8 }}>
              Leg er bij voorkeur minimaal twee vast. De eerste geldt als primair contact.
            </p>
            {model.oudercontacten.length === 0 && (
              <p style={{ fontSize: 12, color: 'var(--text3)' }}>Nog geen contacten.</p>
            )}
            {model.oudercontacten.map((c, i) => (
              <div
                key={i}
                style={{ display: 'flex', gap: 10, alignItems: 'flex-end', marginBottom: 10 }}
              >
                <div className="frow" style={{ gridTemplateColumns: '1fr 1fr 1fr', flex: 1 }}>
                  <Veld label={i === 0 ? 'Naam (primair)' : 'Naam'}>
                    <input value={c.naam} onChange={(e) => zetContact(i, 'naam', e.target.value)} />
                  </Veld>
                  <Veld label="Telefoon">
                    <input value={c.telefoon} onChange={(e) => zetContact(i, 'telefoon', e.target.value)} />
                  </Veld>
                  <Veld label="E-mail">
                    <input type="email" value={c.email} onChange={(e) => zetContact(i, 'email', e.target.value)} />
                  </Veld>
                </div>
                <button
                  type="button"
                  onClick={() => verwijderContact(i)}
                  className="btn btn-rose btn-xs"
                  title="Verwijderen"
                  style={{ marginBottom: 2 }}
                >
                  <i className="ti ti-trash" />
                </button>
              </div>
            ))}
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
