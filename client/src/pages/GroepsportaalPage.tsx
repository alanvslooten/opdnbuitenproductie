import { useState } from 'react';
import {
  useGroepsportaalDashboard,
  useGroepsportaalDienst,
  useGroepsportaalKinderen,
  useGroepsportaalMedewerkers,
  useGroepsportaalMutaties,
  useGroepsportaalUren,
  useStamgroepen,
} from '../api/queries';
import { ApiFout } from '../api/client';
import type { KindDto } from '../types';
import { datumNl, korteDatum, vandaagIso, verschuifDagen } from '../datum';

function tijd(iso: string): string {
  return new Date(iso).toLocaleTimeString('nl-NL', { hour: '2-digit', minute: '2-digit' });
}

function urenLabel(kwartieren: number): string {
  return `${(kwartieren / 4).toLocaleString('nl-NL')} u`;
}

function nuTijd(): string {
  return new Date().toLocaleTimeString('nl-NL', { hour: '2-digit', minute: '2-digit' });
}

/**
 * Uitklokknop met een kiesbare tijd (default: nu). Handig als iemand vergeten is uit
 * te klokken en het op het juiste tijdstip wil corrigeren. De tijd geldt op de datum
 * van de registratie en wordt als UTC-ISO naar de API gestuurd.
 */
function UitklokKnop({ datum, bezig, onUitklok }: { datum: string; bezig: boolean; onUitklok: (uitgeklokt: string) => void }) {
  const [tijd, setTijd] = useState(nuTijd);

  function klok() {
    const iso = new Date(`${datum}T${tijd}:00`).toISOString();
    onUitklok(iso);
  }

  return (
    <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
      <input
        type="time"
        value={tijd}
        onChange={(e) => setTijd(e.target.value)}
        title="Uitkloktijd"
        style={{
          padding: '4px 6px',
          background: 'var(--surface2)',
          border: '1.5px solid var(--border)',
          borderRadius: 6,
          color: 'var(--text)',
          fontSize: 11,
          outline: 'none',
        }}
      />
      <button onClick={klok} disabled={bezig} className="btn btn-outline btn-xs">
        <i className="ti ti-logout" /> Uitklokken
      </button>
    </div>
  );
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

      <Dashboard datum={datum} />
      <DienstVanDeDag datum={datum} />
      <Inklokken datum={datum} />
      <KinderenOpLocatie />
    </div>
  );
}

function Dashboard({ datum }: { datum: string }) {
  const { data } = useGroepsportaalDashboard(datum);

  const kaarten: { num: number; lbl: string; icon: string; variant: string }[] = [
    { num: data?.aanwezigVandaag ?? 0, lbl: 'Kinderen vandaag', icon: 'ti-mood-kid', variant: 'v-primary' },
    { num: data?.medewerkersVandaag ?? 0, lbl: 'Begeleiders ingepland', icon: 'ti-users', variant: 'v-violet' },
    { num: data?.ingeklokt ?? 0, lbl: 'Nu ingeklokt', icon: 'ti-clock-play', variant: 'v-green' },
    { num: data?.observatiesOpen ?? 0, lbl: 'Observaties open', icon: 'ti-clipboard-check', variant: 'v-gold' },
  ];

  return (
    <div style={{ marginBottom: 16 }}>
      {data?.stamgroepNaam && (
        <p style={{ fontSize: 12, color: 'var(--text3)', marginBottom: 8 }}>
          Groep <strong style={{ color: 'var(--text2)' }}>{data.stamgroepNaam}</strong> · {data.kinderenInGroep} kinderen in de groep
        </p>
      )}
      <div className="stats" style={{ marginBottom: 0 }}>
        {kaarten.map((k) => (
          <div key={k.lbl} className={`sc ${k.variant}`}>
            <div className="sc-icon">
              <i className={`ti ${k.icon}`} />
            </div>
            <div className="sc-num">{k.num}</div>
            <div className="sc-lbl">{k.lbl}</div>
          </div>
        ))}
      </div>
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
  const [pincode, setPincode] = useState('');
  const vandaag = vandaagIso();

  function inklok() {
    if (!medewerkerId) return;
    inklokken.mutate(
      { medewerkerId, stamgroepId: stamgroepId || null, roosterdienstId: null, pincode: pincode || null },
      { onSuccess: () => { setMedewerkerId(''); setPincode(''); } },
    );
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
          <div className="fld" style={{ marginBottom: 0, width: 110 }}>
            <label>Pincode</label>
            <input
              type="password"
              inputMode="numeric"
              value={pincode}
              placeholder="••••"
              onChange={(e) => setPincode(e.target.value.replace(/\D/g, '').slice(0, 6))}
            />
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
                  <UitklokKnop
                    datum={u.datum}
                    bezig={uitklokken.isPending}
                    onUitklok={(uitgeklokt) => uitklokken.mutate({ id: u.id, uitgeklokt })}
                  />
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
  const { data: stamgroepen } = useStamgroepen();
  const [geselecteerd, setGeselecteerd] = useState<KindDto | null>(null);

  const groepNaam = (id: string) => stamgroepen?.find((g) => g.id === id)?.naam ?? '—';

  return (
    <Kaart titel="Kinderen op locatie" icon="ti-mood-kid">
      <div className="alert alert-info">
        <i className="ti ti-info-circle" />
        <span>Oudergegevens zijn alléén hier op locatie zichtbaar — niet in het thuis-portaal. Klik een kind voor de details.</span>
      </div>
      {isLoading ? (
        <p style={{ color: 'var(--text3)' }}>Laden…</p>
      ) : data && data.length > 0 ? (
        <div className="tbl-wrap" style={{ border: 'none', boxShadow: 'none' }}>
          <table className="tbl">
            <thead>
              <tr>
                <th>Kind</th>
                <th>Groep</th>
                <th>Ouder/verzorger</th>
                <th>Telefoon</th>
              </tr>
            </thead>
            <tbody>
              {data.map((k) => (
                <tr key={k.id} style={{ cursor: 'pointer' }} onClick={() => setGeselecteerd(k)}>
                  <td style={{ fontWeight: 600 }}>
                    {k.voornaam} {k.achternaam}
                  </td>
                  <td>{groepNaam(k.stamgroepId)}</td>
                  <td>{k.oudercontacten[0]?.naam ?? '—'}</td>
                  <td>{k.oudercontacten[0]?.telefoon ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen kinderen gevonden.</p>
      )}

      {geselecteerd && (
        <KindDetail kind={geselecteerd} groepNaam={groepNaam(geselecteerd.stamgroepId)} onSluit={() => setGeselecteerd(null)} />
      )}
    </Kaart>
  );
}

function KindDetail({ kind, groepNaam, onSluit }: { kind: KindDto; groepNaam: string; onSluit: () => void }) {
  return (
    <div className="overlay on" onClick={onSluit}>
      <div className="modal" style={{ maxWidth: 460 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-h">
          <h2>
            <i className="ti ti-mood-kid" style={{ color: 'var(--primary)' }} /> {kind.voornaam} {kind.achternaam}
          </h2>
          <button className="xbtn" onClick={onSluit}>
            <i className="ti ti-x" />
          </button>
        </div>
        <div className="modal-b">
          <div className="g2" style={{ marginBottom: 14 }}>
            <Veld label="Groep" waarde={groepNaam} />
            <Veld label="Geboortedatum" waarde={datumNl(kind.geboortedatum)} />
            <Veld label="Startdatum" waarde={datumNl(kind.startdatum)} />
            <Veld label="Opvangdagen" waarde={`${kind.gewensteOpvangdagen} dag(en)`} />
          </div>
          <h3 style={{ fontSize: 12, fontWeight: 700, marginBottom: 8 }}>
            <i className="ti ti-phone" style={{ color: 'var(--primary)' }} /> Contact ouders / verzorgers / voogden
          </h3>
          {kind.oudercontacten.length > 0 ? (
            kind.oudercontacten.map((oc, i) => (
              <div key={i} className="g2" style={{ marginBottom: 10 }}>
                <Veld label={i === 0 ? 'Naam (primair)' : 'Naam'} waarde={oc.naam} />
                <Veld label="Telefoon" waarde={oc.telefoon} />
                <Veld label="E-mail" waarde={oc.email} />
              </div>
            ))
          ) : (
            <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen oudercontact vastgelegd.</p>
          )}
        </div>
      </div>
    </div>
  );
}

function Veld({ label, waarde }: { label: string; waarde: string }) {
  return (
    <div>
      <div style={{ fontSize: 9, fontWeight: 700, color: 'var(--text3)', textTransform: 'uppercase', marginBottom: 2 }}>{label}</div>
      <div style={{ fontSize: 13, color: 'var(--text)' }}>{waarde || '—'}</div>
    </div>
  );
}
