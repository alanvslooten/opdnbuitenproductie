import { useState } from 'react';
import {
  useGroepsportaalDienst,
  useGroepsportaalKinderen,
  useGroepsportaalMedewerkers,
  useGroepsportaalMutaties,
  useGroepsportaalUren,
  useStamgroepen,
} from '../api/queries';
import { ApiFout } from '../api/client';
import { korteDatum, vandaagIso, verschuifDagen } from '../datum';

function tijd(iso: string): string {
  return new Date(iso).toLocaleTimeString('nl-NL', { hour: '2-digit', minute: '2-digit' });
}

function urenLabel(kwartieren: number): string {
  return `${(kwartieren / 4).toLocaleString('nl-NL')} u`;
}

function Kaart({ titel, icon, children, rechts }: { titel: string; icon: string; children: React.ReactNode; rechts?: React.ReactNode }) {
  return (
    <div className="card" style={{ marginBottom: 16 }}>
      <div className="card-h">
        <h3>
          <i className={`ti ${icon}`} style={{ color: 'var(--primary)' }} /> {titel}
        </h3>
        {rechts}
      </div>
      <div className="card-b">{children}</div>
    </div>
  );
}

export function GroepsportaalPage() {
  const [datum, setDatum] = useState(vandaagIso());

  const navigatie = (
    <div className="wk-nav" style={{ marginBottom: 0 }}>
      <button onClick={() => setDatum(verschuifDagen(datum, -1))}>
        <i className="ti ti-chevron-left" />
      </button>
      <span style={{ minWidth: 120 }}>{korteDatum(datum)}</span>
      <button onClick={() => setDatum(verschuifDagen(datum, 1))}>
        <i className="ti ti-chevron-right" />
      </button>
    </div>
  );

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Groepsportaal</h1>
          <p>Dienst van de dag, in-/uitklokken en kinderen op locatie</p>
        </div>
        {navigatie}
      </div>

      <DienstVanDeDag datum={datum} />
      <Inklokken datum={datum} />
      <KinderenOpLocatie />
    </div>
  );
}

function DienstVanDeDag({ datum }: { datum: string }) {
  const { data, isLoading } = useGroepsportaalDienst(datum);

  return (
    <Kaart titel="Dienst van de dag" icon="ti-clock">
      {isLoading ? (
        <p style={{ color: 'var(--text3)' }}>Laden…</p>
      ) : (
        <>
          {data && !data.roosterVerstuurd && (
            <div className="alert alert-warn">
              <i className="ti ti-alert-triangle" />
              <span>Let op: het rooster van deze week is nog niet definitief verstuurd.</span>
            </div>
          )}
          {data && data.diensten.length > 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column' }}>
              {data.diensten.map((d) => (
                <div key={d.dienstId} className="tl-item">
                  <span className="tl-dot" style={{ background: 'var(--blue)' }} />
                  <div className="tl-body" style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <h5>{d.medewerkerNaam}</h5>
                    <p>
                      {d.stamgroepNaam}
                      {d.taakomschrijving && <span> — {d.taakomschrijving}</span>}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen diensten ingepland op deze dag.</p>
          )}
        </>
      )}
    </Kaart>
  );
}

function Inklokken({ datum }: { datum: string }) {
  const { data: medewerkers } = useGroepsportaalMedewerkers();
  const { data: stamgroepen } = useStamgroepen();
  const { data: registraties, isLoading } = useGroepsportaalUren(datum);
  const { inklokken, uitklokken } = useGroepsportaalMutaties();

  const [medewerkerId, setMedewerkerId] = useState('');
  const [stamgroepId, setStamgroepId] = useState('');
  const vandaag = vandaagIso();

  function inklok() {
    if (!medewerkerId) return;
    inklokken.mutate({ medewerkerId, stamgroepId: stamgroepId || null, roosterdienstId: null }, { onSuccess: () => setMedewerkerId('') });
  }

  const fout = inklokken.error;

  return (
    <Kaart titel="In- en uitklokken" icon="ti-clock-play">
      {datum !== vandaag ? (
        <p style={{ fontSize: 12, color: 'var(--text3)' }}>Inklokken kan alleen op vandaag. Bekijk een andere dag voor de historie.</p>
      ) : (
        <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-end', gap: 10, marginBottom: 14 }}>
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Medewerker</label>
            <select value={medewerkerId} onChange={(e) => setMedewerkerId(e.target.value)} style={{ width: 220 }}>
              <option value="">— kies jezelf —</option>
              {medewerkers?.map((m) => (
                <option key={m.id} value={m.id}>
                  {m.naam}
                </option>
              ))}
            </select>
          </div>
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Groep (optioneel)</label>
            <select value={stamgroepId} onChange={(e) => setStamgroepId(e.target.value)} style={{ width: 180 }}>
              <option value="">— geen —</option>
              {stamgroepen?.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.naam}
                </option>
              ))}
            </select>
          </div>
          <button onClick={inklok} disabled={!medewerkerId || inklokken.isPending} className="btn btn-primary btn-sm">
            <i className="ti ti-login" /> Inklokken
          </button>
        </div>
      )}

      {fout instanceof ApiFout && <p style={{ fontSize: 12, color: 'var(--rose)', marginBottom: 10 }}>{fout.message}</p>}

      {isLoading ? (
        <p style={{ color: 'var(--text3)' }}>Laden…</p>
      ) : registraties && registraties.length > 0 ? (
        <div>
          {registraties.map((u) => (
            <div key={u.id} className="tl-item">
              <span className="tl-dot" style={{ background: u.isOpen ? 'var(--green)' : 'var(--border2)' }} />
              <div className="tl-body" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                  <h5>{u.medewerkerNaam}</h5>
                  <p>
                    {u.stamgroepNaam && <span>{u.stamgroepNaam} · </span>}
                    {tijd(u.ingeklokt)}
                    {u.uitgeklokt ? ` – ${tijd(u.uitgeklokt)}` : ' – …'}
                  </p>
                </div>
                {u.isOpen ? (
                  <button onClick={() => uitklokken.mutate(u.id)} disabled={uitklokken.isPending} className="btn btn-outline btn-xs">
                    <i className="ti ti-logout" /> Uitklokken
                  </button>
                ) : (
                  <span style={{ fontSize: 11, color: 'var(--text2)' }}>{urenLabel(u.gewerkteKwartieren)}</span>
                )}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <p style={{ fontSize: 12, color: 'var(--text3)' }}>Nog niemand ingeklokt op deze dag.</p>
      )}
    </Kaart>
  );
}

function KinderenOpLocatie() {
  const { data, isLoading } = useGroepsportaalKinderen();

  return (
    <Kaart titel="Kinderen op locatie" icon="ti-mood-kid">
      <div className="alert alert-info">
        <i className="ti ti-info-circle" />
        <span>Oudergegevens zijn alléén hier op locatie zichtbaar — niet in het thuis-portaal.</span>
      </div>
      {isLoading ? (
        <p style={{ color: 'var(--text3)' }}>Laden…</p>
      ) : data && data.length > 0 ? (
        <div className="tbl-wrap" style={{ border: 'none', boxShadow: 'none' }}>
          <table className="tbl">
            <thead>
              <tr>
                <th>Kind</th>
                <th>Ouder/verzorger</th>
                <th>Telefoon</th>
              </tr>
            </thead>
            <tbody>
              {data.map((k) => (
                <tr key={k.id}>
                  <td style={{ fontWeight: 600 }}>
                    {k.voornaam} {k.achternaam}
                  </td>
                  <td>{k.oudercontact?.naam ?? '—'}</td>
                  <td>{k.oudercontact?.telefoon ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen kinderen gevonden.</p>
      )}
    </Kaart>
  );
}
