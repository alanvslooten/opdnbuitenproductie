import { useState, type CSSProperties } from 'react';
import { useRooster, useRoosterMutaties, useVerstuurdeRoosters } from '../api/queries';
import { datumNl, korteDatum, vandaagIso, verschuifDagen, weekBeginIso } from '../datum';
import { RoosterCelKleur, RoosterStatus, type RoosterCelDto } from '../types';

const CEL_STYLE: Record<number, CSSProperties> = {
  [RoosterCelKleur.Leeg]: { background: 'var(--surface)', color: 'var(--text3)' },
  [RoosterCelKleur.Standaard]: { background: 'var(--blue-l)', color: 'var(--blue)' },
  [RoosterCelKleur.VerlofAangevraagd]: { background: 'var(--amber-l)', color: 'var(--amber)' },
  [RoosterCelKleur.VerlofGoedgekeurd]: { background: 'var(--green-l)', color: 'var(--green)' },
  [RoosterCelKleur.Ziek]: { background: 'var(--rose-l)', color: 'var(--rose)' },
};
const INDICATOR_CHIP = ['chip-done', 'chip-need', 'chip-over'];

interface EditState {
  dienstId: string;
  naam: string;
  taak: string;
  kwartier: number;
}

export function RoosterPage() {
  const [datum, setDatum] = useState(vandaagIso());
  const { data, isLoading, error } = useRooster(datum);
  const { genereren, versturen, herroepen, dienstBijwerken, dienstToevoegen, dienstVerwijderen } = useRoosterMutaties();
  const [edit, setEdit] = useState<EditState | null>(null);

  const verstuurd = data?.status === RoosterStatus.Verstuurd;

  function celKlik(groepId: string, medewerkerId: string, naam: string, cel: RoosterCelDto) {
    if (cel.dienstId) {
      setEdit({ dienstId: cel.dienstId, naam, taak: cel.taakomschrijving ?? '', kwartier: cel.urencorrectieKwartieren });
    } else if (cel.kleur === RoosterCelKleur.Leeg) {
      void dienstToevoegen.mutateAsync({ medewerkerId, stamgroepId: groepId, datum: cel.datum });
    }
  }

  async function bewaarEdit() {
    if (!edit) return;
    await dienstBijwerken.mutateAsync({ id: edit.dienstId, invoer: { taakomschrijving: edit.taak || null, urencorrectieKwartieren: edit.kwartier } });
    setEdit(null);
  }

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Werkrooster</h1>
          <p>Diensten per stamgroep met live BKR-indicator</p>
        </div>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12, flexWrap: 'wrap' }}>
        <div className="wk-nav" style={{ marginBottom: 0 }}>
          <button onClick={() => setDatum((d) => verschuifDagen(weekBeginIso(d), -7))}>
            <i className="ti ti-chevron-left" />
          </button>
          <span>Week van {data ? korteDatum(data.weekBegin) : '…'}</span>
          <button onClick={() => setDatum((d) => verschuifDagen(weekBeginIso(d), 7))}>
            <i className="ti ti-chevron-right" />
          </button>
        </div>
        <button className="btn btn-outline btn-sm" onClick={() => setDatum(vandaagIso())}>
          <i className="ti ti-calendar" /> Vandaag
        </button>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 14, flexWrap: 'wrap' }}>
        <button onClick={() => genereren.mutate(datum)} disabled={genereren.isPending} className="btn btn-primary btn-sm">
          <i className="ti ti-wand" /> {genereren.isPending ? 'Genereren…' : 'Auto-rooster genereren'}
        </button>
        {data?.bestaat && (
          <span className={`chip ${verstuurd ? 'chip-done' : 'chip-need'}`}>
            {verstuurd ? `Verstuurd${data.verstuurdOp ? ' op ' + korteDatum(data.verstuurdOp.slice(0, 10)) : ''}` : 'Concept'}
          </span>
        )}
        {data?.bestaat && !verstuurd && data.roosterweekId && (
          <button onClick={() => versturen.mutate(data.roosterweekId!)} disabled={versturen.isPending} className="btn btn-blue btn-sm">
            <i className="ti ti-send" /> Rooster versturen
          </button>
        )}
        {data?.bestaat && verstuurd && data.roosterweekId && (
          <button
            onClick={() => {
              if (confirm('Verstuurd rooster herroepen? Het gaat terug naar concept en kan daarna opnieuw worden verstuurd.')) {
                herroepen.mutate(data.roosterweekId!);
              }
            }}
            disabled={herroepen.isPending}
            className="btn btn-amber btn-sm"
          >
            <i className="ti ti-arrow-back-up" /> Herroepen
          </button>
        )}
      </div>

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon het rooster niet laden.</p>}

      {data && !data.bestaat && (
        <div className="empty" style={{ border: '1.5px dashed var(--border2)', borderRadius: 'var(--r)' }}>
          <i className="ti ti-calendar-off" />
          <p>Nog geen rooster voor deze week. Klik op "Auto-rooster genereren" voor een voorstel.</p>
        </div>
      )}

      {data?.groepen.map((g) => (
        <div key={g.stamgroepId} className="tbl-wrap" style={{ marginBottom: 16 }}>
          <table className="tbl">
            <thead>
              <tr>
                <th>{g.naam}</th>
                {g.indicatoren.map((ind) => (
                  <th key={ind.datum} style={{ textAlign: 'center' }}>
                    <div>{korteDatum(ind.datum)}</div>
                    <span
                      className={`chip ${INDICATOR_CHIP[ind.kleur]}`}
                      style={{ marginTop: 4 }}
                      title={`${ind.aantalKinderen} kinderen · nodig ${ind.nodigPmers ?? '—'} · ingepland ${ind.ingeplandPmers}`}
                    >
                      {ind.ingeplandPmers}/{ind.nodigPmers ?? '!'} b
                    </span>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {g.rijen.map((rij) => (
                <tr key={rij.medewerkerId}>
                  <td style={{ fontWeight: 600 }}>{rij.naam}</td>
                  {rij.cellen.map((cel) => (
                    <td
                      key={cel.datum}
                      style={{ cursor: 'pointer', textAlign: 'center', fontSize: 10, ...CEL_STYLE[cel.kleur] }}
                      onClick={() => celKlik(g.stamgroepId, rij.medewerkerId, rij.naam, cel)}
                      title={cel.kleur === RoosterCelKleur.Leeg ? 'Klik om in te plannen' : 'Klik om te bewerken'}
                    >
                      {cel.dienstId ? (
                        <>
                          <div>{cel.taakomschrijving || 'dienst'}</div>
                          {cel.urencorrectieKwartieren !== 0 && (
                            <div style={{ fontSize: 9, opacity: 0.7 }}>
                              {cel.urencorrectieKwartieren > 0 ? '+' : ''}
                              {cel.urencorrectieKwartieren / 4}u
                            </div>
                          )}
                        </>
                      ) : cel.kleur === RoosterCelKleur.Leeg ? (
                        '+'
                      ) : cel.kleur === RoosterCelKleur.Ziek ? (
                        'ziek'
                      ) : cel.kleur === RoosterCelKleur.VerlofGoedgekeurd ? (
                        'verlof'
                      ) : (
                        'verlof?'
                      )}
                    </td>
                  ))}
                </tr>
              ))}
              {g.rijen.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', color: 'var(--text3)', padding: '16px 0' }}>
                    Geen medewerkers in deze groep.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      ))}

      <p style={{ fontSize: 10, color: 'var(--text3)' }}>
        Indicator = ingepland/nodig begeleiders (groen=marge, oranje=precies genoeg, rood=tekort of groep te vol).
        Cel: blauw=dienst, oranje=verlof aangevraagd, groen=verlof goedgekeurd, rood=ziek. Klik een lege cel om bij te plannen.
      </p>

      <VerstuurdeRoostersLog />

      {edit && (
        <div className="overlay on" onClick={() => setEdit(null)}>
          <div className="modal" style={{ maxWidth: 380 }} onClick={(e) => e.stopPropagation()}>
            <div className="modal-h">
              <h2>
                <i className="ti ti-clock-edit" /> Dienst — {edit.naam}
              </h2>
              <button className="xbtn" onClick={() => setEdit(null)}>
                <i className="ti ti-x" />
              </button>
            </div>
            <div className="modal-b">
              <div className="fld">
                <label>Taakomschrijving</label>
                <input value={edit.taak} onChange={(e) => setEdit({ ...edit, taak: e.target.value })} />
              </div>
              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Urencorrectie (kwartieren, +/–)</label>
                <input type="number" step={1} value={edit.kwartier} onChange={(e) => setEdit({ ...edit, kwartier: Number(e.target.value) })} />
                <span style={{ fontSize: 10, color: 'var(--text3)' }}>{edit.kwartier / 4} uur</span>
              </div>
            </div>
            <div className="modal-f" style={{ justifyContent: 'space-between' }}>
              <button
                onClick={() => {
                  void dienstVerwijderen.mutateAsync(edit.dienstId);
                  setEdit(null);
                }}
                className="btn btn-rose btn-sm"
              >
                <i className="ti ti-trash" /> Verwijderen
              </button>
              <div style={{ display: 'flex', gap: 7 }}>
                <button onClick={() => setEdit(null)} className="btn btn-outline btn-sm">
                  Annuleren
                </button>
                <button onClick={bewaarEdit} className="btn btn-primary btn-sm">
                  Opslaan
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

const PERIODES = [
  { sleutel: 'alles', label: 'Alles' },
  { sleutel: 'jaar', label: 'Dit jaar' },
  { sleutel: 'kwartaal', label: 'Dit kwartaal' },
  { sleutel: 'maand', label: 'Deze maand' },
  { sleutel: 'week', label: 'Deze week' },
] as const;

// Begin-datum (ISO) van de gekozen periode, of undefined voor "alles".
function periodeVan(sleutel: string): string | undefined {
  if (sleutel === 'alles') return undefined;
  if (sleutel === 'week') return weekBeginIso(vandaagIso());
  const [j, m] = vandaagIso().slice(0, 10).split('-').map(Number);
  if (sleutel === 'jaar') return `${j}-01-01`;
  if (sleutel === 'maand') return `${j}-${String(m).padStart(2, '0')}-01`;
  // kwartaal
  const kwartaalStartMaand = Math.floor((m - 1) / 3) * 3 + 1;
  return `${j}-${String(kwartaalStartMaand).padStart(2, '0')}-01`;
}

function VerstuurdeRoostersLog() {
  const [periode, setPeriode] = useState<string>('alles');
  const { data } = useVerstuurdeRoosters(periodeVan(periode));

  return (
    <div className="card" style={{ marginTop: 16 }}>
      <div className="card-h">
        <h3>
          <i className="ti ti-history" style={{ color: 'var(--primary)' }} /> Verstuurde roosters
        </h3>
        <div className="tabs" style={{ marginBottom: 0 }}>
          {PERIODES.map((p) => (
            <button key={p.sleutel} className={`tab${periode === p.sleutel ? ' on' : ''}`} onClick={() => setPeriode(p.sleutel)}>
              {p.label}
            </button>
          ))}
        </div>
      </div>
      <div className="card-b" style={{ padding: 0 }}>
        <table className="tbl">
          <thead>
            <tr>
              <th>Week van</th>
              <th>Verstuurd op</th>
              <th>Diensten</th>
            </tr>
          </thead>
          <tbody>
            {data?.map((r) => (
              <tr key={r.id}>
                <td style={{ fontWeight: 600 }}>{datumNl(r.weekBegin)}</td>
                <td style={{ color: 'var(--text2)' }}>{r.verstuurdOp ? datumNl(r.verstuurdOp.slice(0, 10)) : '—'}</td>
                <td>{r.aantalDiensten}</td>
              </tr>
            ))}
            {data && data.length === 0 && (
              <tr>
                <td colSpan={3} style={{ textAlign: 'center', color: 'var(--text3)', padding: '20px 0' }}>
                  Geen verstuurde roosters in deze periode.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
