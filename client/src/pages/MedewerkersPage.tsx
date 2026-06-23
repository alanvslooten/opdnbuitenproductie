import { useState, type FormEvent } from 'react';
import { useMedewerkers, useMedewerkerMutaties, useMedewerkerUren, useStamgroepen, useVerlofsaldo } from '../api/queries';
import { ApiFout } from '../api/client';
import { datumNl, vandaagIso } from '../datum';
import { ROL_LABEL, VERLOFCATEGORIE_LABEL, WEEKDAGEN, type MedewerkerDto } from '../types';

const LEEG = {
  voornaam: '',
  achternaam: '',
  rol: 2,
  vasteWerkdagen: 0,
  beschikbaarheidsdagen: 0,
  contracturen: 24,
  vasteStamgroepId: '' as string,
  telefoon: '',
  email: '',
  noodcontactNaam: '',
  noodcontactTelefoon: '',
  contractVast: true,
  contracteinddatum: '' as string,
};

function dagenTekst(vlaggen: number): string {
  const d = WEEKDAGEN.filter((w) => (vlaggen & w.vlag) !== 0).map((w) => w.korte);
  return d.length ? d.join(' ') : '—';
}

export function MedewerkersPage() {
  const { data, isLoading, error } = useMedewerkers();
  const { data: groepen } = useStamgroepen();
  const { aanmaken, bewerken, verwijderen } = useMedewerkerMutaties();

  const [form, setForm] = useState({ ...LEEG });
  const [bewerktId, setBewerktId] = useState<string | null>(null);
  const [fout, setFout] = useState<string | null>(null);
  const [detail, setDetail] = useState<MedewerkerDto | null>(null);

  function vul(m: MedewerkerDto) {
    setBewerktId(m.id);
    setForm({
      voornaam: m.voornaam,
      achternaam: m.achternaam,
      rol: m.rol,
      vasteWerkdagen: m.vasteWerkdagen,
      beschikbaarheidsdagen: m.beschikbaarheidsdagen,
      contracturen: m.contracturen,
      vasteStamgroepId: m.vasteStamgroepId ?? '',
      telefoon: m.telefoon ?? '',
      email: m.email ?? '',
      noodcontactNaam: m.noodcontactNaam ?? '',
      noodcontactTelefoon: m.noodcontactTelefoon ?? '',
      contractVast: m.contractVast,
      contracteinddatum: m.contracteinddatum ?? '',
    });
    setFout(null);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function reset() {
    setBewerktId(null);
    setForm({ ...LEEG });
    setFout(null);
  }

  function toggle(veld: 'vasteWerkdagen' | 'beschikbaarheidsdagen', vlag: number) {
    setForm((f) => ({ ...f, [veld]: f[veld] ^ vlag }));
  }

  async function opslaan(e: FormEvent) {
    e.preventDefault();
    setFout(null);
    const invoer = {
      voornaam: form.voornaam,
      achternaam: form.achternaam,
      rol: form.rol,
      vasteWerkdagen: form.vasteWerkdagen,
      beschikbaarheidsdagen: form.beschikbaarheidsdagen,
      contracturen: form.contracturen,
      vasteStamgroepId: form.vasteStamgroepId || null,
      telefoon: form.telefoon || null,
      email: form.email || null,
      noodcontactNaam: form.noodcontactNaam || null,
      noodcontactTelefoon: form.noodcontactTelefoon || null,
      contractVast: form.contractVast,
      contracteinddatum: form.contractVast ? null : form.contracteinddatum || null,
    };
    try {
      if (bewerktId) await bewerken.mutateAsync({ id: bewerktId, invoer });
      else await aanmaken.mutateAsync(invoer);
      reset();
    } catch (err) {
      setFout(err instanceof ApiFout ? err.message : 'Opslaan mislukt.');
    }
  }

  async function verwijder(m: MedewerkerDto) {
    setFout(null);
    if (!confirm(`Medewerker "${m.voornaam} ${m.achternaam}" verwijderen?`)) return;
    try {
      await verwijderen.mutateAsync(m.id);
      if (bewerktId === m.id) reset();
    } catch (err) {
      setFout(err instanceof ApiFout ? err.message : 'Verwijderen mislukt.');
    }
  }

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Medewerkers</h1>
          <p>Rollen, thuisgroep, vaste werkdagen en beschikbaarheid</p>
        </div>
      </div>

      {fout && (
        <div className="alert alert-bad">
          <i className="ti ti-alert-circle" />
          <span>{fout}</span>
        </div>
      )}

      <form onSubmit={opslaan} className="card" style={{ marginBottom: 16 }}>
        <div className="card-h">
          <h3>
            <i className="ti ti-user-plus" style={{ color: 'var(--primary)' }} />
            {bewerktId ? 'Medewerker bewerken' : 'Medewerker toevoegen'}
          </h3>
        </div>
        <div className="card-b" style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Voornaam</label>
              <input value={form.voornaam} onChange={(e) => setForm((f) => ({ ...f, voornaam: e.target.value }))} required />
            </div>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Achternaam</label>
              <input value={form.achternaam} onChange={(e) => setForm((f) => ({ ...f, achternaam: e.target.value }))} required />
            </div>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Rol</label>
              <select value={form.rol} onChange={(e) => setForm((f) => ({ ...f, rol: Number(e.target.value) }))}>
                {ROL_LABEL.map((label, i) => (
                  <option key={i} value={i}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Thuisgroep</label>
              <select value={form.vasteStamgroepId} onChange={(e) => setForm((f) => ({ ...f, vasteStamgroepId: e.target.value }))}>
                <option value="">— geen —</option>
                {groepen?.map((g) => (
                  <option key={g.id} value={g.id}>
                    {g.naam}
                  </option>
                ))}
              </select>
            </div>
            <div className="fld" style={{ marginBottom: 0, width: 130 }}>
              <label>Contracturen/week</label>
              <input type="number" min={0} max={40} step={0.5} value={form.contracturen} onChange={(e) => setForm((f) => ({ ...f, contracturen: Number(e.target.value) }))} />
            </div>
          </div>

          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 32 }}>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Vaste werkdagen</label>
              <div style={{ display: 'flex', gap: 4 }}>
                {WEEKDAGEN.map((w) => (
                  <button type="button" key={w.vlag} onClick={() => toggle('vasteWerkdagen', w.vlag)} className={`fchip${(form.vasteWerkdagen & w.vlag) !== 0 ? ' on' : ''}`}>
                    {w.korte}
                  </button>
                ))}
              </div>
            </div>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Beschikbaarheidsdagen</label>
              <div style={{ display: 'flex', gap: 4 }}>
                {WEEKDAGEN.map((w) => {
                  const aan = (form.beschikbaarheidsdagen & w.vlag) !== 0;
                  return (
                    <button
                      type="button"
                      key={w.vlag}
                      onClick={() => toggle('beschikbaarheidsdagen', w.vlag)}
                      className="fchip"
                      style={aan ? { background: 'var(--amber)', borderColor: 'var(--amber)', color: '#fff' } : undefined}
                    >
                      {w.korte}
                    </button>
                  );
                })}
              </div>
            </div>
          </div>

          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Telefoon</label>
              <input value={form.telefoon} onChange={(e) => setForm((f) => ({ ...f, telefoon: e.target.value }))} />
            </div>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>E-mail</label>
              <input type="email" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} />
            </div>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Noodcontact (naam)</label>
              <input value={form.noodcontactNaam} onChange={(e) => setForm((f) => ({ ...f, noodcontactNaam: e.target.value }))} />
            </div>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Noodcontact (telefoon)</label>
              <input value={form.noodcontactTelefoon} onChange={(e) => setForm((f) => ({ ...f, noodcontactTelefoon: e.target.value }))} />
            </div>
          </div>

          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'flex-end' }}>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Contracttype</label>
              <select value={form.contractVast ? '1' : '0'} onChange={(e) => setForm((f) => ({ ...f, contractVast: e.target.value === '1' }))}>
                <option value="1">Vast</option>
                <option value="0">Tijdelijk</option>
              </select>
            </div>
            {!form.contractVast && (
              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Contract einddatum</label>
                <input type="date" value={form.contracteinddatum} onChange={(e) => setForm((f) => ({ ...f, contracteinddatum: e.target.value }))} />
              </div>
            )}
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <button type="submit" disabled={aanmaken.isPending || bewerken.isPending} className="btn btn-primary btn-sm">
              {bewerktId ? 'Wijzigingen opslaan' : 'Medewerker toevoegen'}
            </button>
            {bewerktId && (
              <button type="button" onClick={reset} className="btn btn-outline btn-sm">
                Annuleren
              </button>
            )}
          </div>
        </div>
      </form>

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon medewerkers niet laden.</p>}

      <div className="tbl-wrap">
        <table className="tbl">
          <thead>
            <tr>
              <th>Naam</th>
              <th>Rol</th>
              <th>Thuisgroep</th>
              <th>Vaste dagen</th>
              <th>Beschikbaar</th>
              <th>Uren</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {data?.map((m) => (
              <tr key={m.id}>
                <td style={{ fontWeight: 600 }}>
                  <button
                    onClick={() => setDetail(m)}
                    style={{ background: 'none', border: 'none', padding: 0, font: 'inherit', fontWeight: 600, color: 'var(--blue)', cursor: 'pointer' }}
                  >
                    {m.voornaam} {m.achternaam}
                  </button>
                </td>
                <td>{ROL_LABEL[m.rol] ?? m.rol}</td>
                <td>{m.vasteStamgroepNaam ?? '—'}</td>
                <td>
                  <span style={{ color: 'var(--blue)' }}>{dagenTekst(m.vasteWerkdagen)}</span>
                </td>
                <td>
                  <span style={{ color: 'var(--amber)' }}>{dagenTekst(m.beschikbaarheidsdagen)}</span>
                </td>
                <td>{m.contracturen}</td>
                <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                  <button onClick={() => vul(m)} className="btn btn-outline btn-xs" style={{ marginRight: 6 }}>
                    <i className="ti ti-pencil" /> Bewerken
                  </button>
                  <button onClick={() => verwijder(m)} className="btn btn-rose btn-xs">
                    <i className="ti ti-trash" />
                  </button>
                </td>
              </tr>
            ))}
            {data?.length === 0 && (
              <tr>
                <td colSpan={7} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                  Nog geen medewerkers.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {detail && <MedewerkerDetail m={detail} onBewerk={() => { vul(detail); setDetail(null); }} onSluit={() => setDetail(null)} />}
    </div>
  );
}

const PERIODES = [
  { sleutel: 'maand', label: 'Deze maand' },
  { sleutel: 'kwartaal', label: 'Dit kwartaal' },
  { sleutel: 'jaar', label: 'Dit jaar' },
] as const;

function periodeVan(sleutel: string): string {
  const [j, m] = vandaagIso().slice(0, 10).split('-').map(Number);
  if (sleutel === 'jaar') return `${j}-01-01`;
  if (sleutel === 'kwartaal') {
    const km = Math.floor((m - 1) / 3) * 3 + 1;
    return `${j}-${String(km).padStart(2, '0')}-01`;
  }
  return `${j}-${String(m).padStart(2, '0')}-01`;
}

function MedewerkerDetail({ m, onBewerk, onSluit }: { m: MedewerkerDto; onBewerk: () => void; onSluit: () => void }) {
  const [periode, setPeriode] = useState('maand');
  const { data: uren } = useMedewerkerUren(m.id, periodeVan(periode));
  const { data: saldi } = useVerlofsaldo(m.id);

  const contract = m.contractVast
    ? 'Vast contract'
    : `Tijdelijk${m.contracteinddatum ? ` tot ${datumNl(m.contracteinddatum)}` : ''}` +
      `${m.resterendeContractmaanden != null ? ` · nog ${m.resterendeContractmaanden} mnd` : ''}`;

  return (
    <div className="overlay on" onClick={onSluit}>
      <div className="modal" style={{ maxWidth: 560 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-h">
          <h2><i className="ti ti-user" style={{ color: 'var(--primary)' }} /> {m.voornaam} {m.achternaam}</h2>
          <button className="xbtn" onClick={onSluit}><i className="ti ti-x" /></button>
        </div>
        <div className="modal-b">
          <div className="g2" style={{ marginBottom: 14 }}>
            <Veld label="Rol" waarde={ROL_LABEL[m.rol] ?? String(m.rol)} />
            <Veld label="Contract" waarde={contract} />
            <Veld label="Telefoon" waarde={m.telefoon ?? '—'} />
            <Veld label="E-mail" waarde={m.email ?? '—'} />
            <Veld label="Noodcontact" waarde={m.noodcontactNaam ?? '—'} />
            <Veld label="Noodcontact tel." waarde={m.noodcontactTelefoon ?? '—'} />
            <Veld label="Contracturen/week" waarde={`${m.contracturen} u`} />
          </div>

          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
            <h3 style={{ fontSize: 12, fontWeight: 700 }}><i className="ti ti-clock" style={{ color: 'var(--primary)' }} /> Urenoverzicht</h3>
            <div className="tabs" style={{ marginBottom: 0 }}>
              {PERIODES.map((p) => (
                <button key={p.sleutel} className={`tab${periode === p.sleutel ? ' on' : ''}`} onClick={() => setPeriode(p.sleutel)}>{p.label}</button>
              ))}
            </div>
          </div>
          {uren ? (
            <div className="g3" style={{ marginBottom: 14 }}>
              <Veld label="Gewerkt" waarde={`${uren.gewerkteUren} u`} />
              <Veld label="Verwacht (contract)" waarde={`${uren.verwachteUren} u`} />
              <Veld
                label="Meer / minder"
                waarde={`${uren.meerMinderUren >= 0 ? '+' : ''}${uren.meerMinderUren} u`}
              />
            </div>
          ) : (
            <p style={{ fontSize: 12, color: 'var(--text3)' }}>Uren laden…</p>
          )}

          <h3 style={{ fontSize: 12, fontWeight: 700, marginBottom: 6 }}><i className="ti ti-beach" style={{ color: 'var(--amber)' }} /> Verlofsaldo</h3>
          {saldi && saldi.length > 0 ? (
            <div className="g2">
              {saldi.map((s) => (
                <Veld key={s.categorie} label={VERLOFCATEGORIE_LABEL[s.categorie] ?? 'Verlof'} waarde={`${s.resterend} u resterend van ${s.toegekend} u`} />
              ))}
            </div>
          ) : (
            <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen saldo ingesteld.</p>
          )}
        </div>
        <div className="modal-f" style={{ position: 'static' }}>
          <button onClick={onBewerk} className="btn btn-primary btn-sm"><i className="ti ti-pencil" /> Bewerken</button>
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
