import { useState, type FormEvent } from 'react';
import { useMedewerkers, useMedewerkerMutaties, useStamgroepen } from '../api/queries';
import { ApiFout } from '../api/client';
import { ROL_LABEL, WEEKDAGEN, type MedewerkerDto } from '../types';

const LEEG = {
  voornaam: '',
  achternaam: '',
  rol: 2,
  vasteWerkdagen: 0,
  beschikbaarheidsdagen: 0,
  contracturen: 24,
  vasteStamgroepId: '' as string,
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
    });
    setFout(null);
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
                  {m.voornaam} {m.achternaam}
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
    </div>
  );
}
