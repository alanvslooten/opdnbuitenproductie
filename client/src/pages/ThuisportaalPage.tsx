import { useState } from 'react';
import {
  useBeschikbaarheid,
  useThuisMutaties,
  useThuisRooster,
  useThuisSaldo,
  useThuisUrenoverzicht,
  useThuisVerlof,
} from '../api/queries';
import { ApiFout } from '../api/client';
import { datumNl, korteDatum, vandaagIso, verschuifDagen, weekBeginIso } from '../datum';
import {
  DIENSTSOORT_LABEL,
  Dienstsoort,
  VERLOFCATEGORIE_LABEL,
  VERLOFSTATUS_LABEL,
  VerlofCategorie,
  VerlofStatus,
  WEEKDAGEN,
  type ThuisVerlofInvoer,
} from '../types';

function correctieLabel(kwartieren: number): string {
  if (kwartieren === 0) return '';
  const teken = kwartieren > 0 ? '+' : '−';
  return ` (${teken}${Math.abs(kwartieren / 4).toLocaleString('nl-NL')} u)`;
}

export function ThuisportaalPage() {
  const [weekBegin, setWeekBegin] = useState(() => weekBeginIso(vandaagIso()));

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Mijn portaal</h1>
          <p>Je rooster, beschikbaarheid, verlof en uren</p>
        </div>
      </div>

      <MijnRooster weekBegin={weekBegin} setWeekBegin={setWeekBegin} />
      <MijnBeschikbaarheid />
      <MijnVerlof />
      <MijnUren />
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

function MijnRooster({ weekBegin, setWeekBegin }: { weekBegin: string; setWeekBegin: (d: string) => void }) {
  const { data, isLoading } = useThuisRooster(weekBegin);

  const navigatie = (
    <div className="wk-nav" style={{ marginBottom: 0 }}>
      <button onClick={() => setWeekBegin(verschuifDagen(weekBegin, -7))}>
        <i className="ti ti-chevron-left" />
      </button>
      <span style={{ minWidth: 140 }}>week van {korteDatum(weekBegin)}</span>
      <button onClick={() => setWeekBegin(verschuifDagen(weekBegin, 7))}>
        <i className="ti ti-chevron-right" />
      </button>
    </div>
  );

  return (
    <Kaart titel="Mijn rooster" icon="ti-calendar-week" rechts={navigatie}>
      {isLoading ? (
        <p style={{ color: 'var(--text3)' }}>Laden…</p>
      ) : !data?.verstuurd ? (
        <div className="alert alert-warn" style={{ marginBottom: 0 }}>
          <i className="ti ti-alert-triangle" />
          <span>Het rooster voor deze week is nog niet verstuurd door de planner.</span>
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 6 }}>
          {['Ma', 'Di', 'Wo', 'Do', 'Vr'].map((label, i) => {
            const datum = verschuifDagen(weekBegin, i);
            const diensten = data.dagen.filter((d) => d.datum === datum);
            return (
              <div key={datum} style={{ border: '1px solid var(--border)', borderRadius: 'var(--r-sm)', overflow: 'hidden', minHeight: 76 }}>
                <div style={{ background: 'var(--surface2)', padding: '4px 6px', fontSize: 10, fontWeight: 700, color: 'var(--text3)', textAlign: 'center' }}>
                  {label} {datum.slice(8, 10)}
                </div>
                <div style={{ padding: 6, display: 'flex', flexDirection: 'column', gap: 4 }}>
                  {diensten.length === 0 ? (
                    <span style={{ fontSize: 10, color: 'var(--text3)', textAlign: 'center' }}>—</span>
                  ) : (
                    diensten.map((d) => (
                      <div key={d.stamgroepId} style={{ background: 'var(--blue-l)', color: 'var(--blue)', borderRadius: 5, padding: '4px 6px', fontSize: 10, fontWeight: 600, textAlign: 'center' }}>
                        {d.stamgroepNaam}
                        {d.dienstsoort !== Dienstsoort.Regulier && (
                          <div style={{ fontWeight: 700, color: d.dienstsoort === Dienstsoort.Vroege ? 'var(--teal)' : 'var(--indigo)' }}>
                            {DIENSTSOORT_LABEL[d.dienstsoort]}
                          </div>
                        )}
                        {d.taakomschrijving && <div style={{ fontWeight: 400 }}>{d.taakomschrijving}</div>}
                        {d.urencorrectieKwartieren !== 0 && <div style={{ fontWeight: 400, opacity: 0.8 }}>{correctieLabel(d.urencorrectieKwartieren)}</div>}
                      </div>
                    ))
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </Kaart>
  );
}

function MijnBeschikbaarheid() {
  const { data, isLoading } = useBeschikbaarheid();
  const { beschikbaarheidBijwerken } = useThuisMutaties();
  const [concept, setConcept] = useState<number | null>(null);

  const huidig = concept ?? data?.beschikbaarheidsdagen ?? 0;
  const vast = data?.vasteWerkdagen ?? 0;

  function toggle(vlag: number) {
    setConcept(huidig & vlag ? huidig & ~vlag : huidig | vlag);
  }

  const fout = beschikbaarheidBijwerken.error;

  return (
    <Kaart titel="Mijn beschikbaarheid" icon="ti-user-check">
      {isLoading ? (
        <p style={{ color: 'var(--text3)' }}>Laden…</p>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          <p style={{ fontSize: 11, color: 'var(--text3)' }}>
            Vaste werkdagen worden door de planner bepaald (grijs). Geef hieronder aan op welke extra dagen je
            inzetbaar bent.
          </p>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
            {WEEKDAGEN.map((dag) => {
              const isVast = (vast & dag.vlag) !== 0;
              const isAan = (huidig & dag.vlag) !== 0;
              return (
                <button
                  key={dag.vlag}
                  disabled={isVast}
                  onClick={() => toggle(dag.vlag)}
                  className={`fchip${!isVast && isAan ? ' on' : ''}`}
                  style={isVast ? { opacity: 0.55, cursor: 'not-allowed' } : undefined}
                  title={isVast ? 'Vaste werkdag (door planner)' : undefined}
                >
                  {dag.lange}
                  {isVast && ' (vast)'}
                </button>
              );
            })}
          </div>
          {fout instanceof ApiFout && <p style={{ fontSize: 12, color: 'var(--rose)' }}>{fout.message}</p>}
          <button onClick={() => beschikbaarheidBijwerken.mutate(huidig)} disabled={beschikbaarheidBijwerken.isPending} className="btn btn-primary btn-sm" style={{ alignSelf: 'flex-start' }}>
            Beschikbaarheid opslaan
          </button>
        </div>
      )}
    </Kaart>
  );
}

const LEEG_VERLOF: ThuisVerlofInvoer = {
  begindatum: vandaagIso(),
  einddatum: vandaagIso(),
  aantalUren: 8,
  categorie: VerlofCategorie.Vakantieuren,
  reden: '',
};

function MijnVerlof() {
  const { data: saldo } = useThuisSaldo();
  const { data: aanvragen, isLoading } = useThuisVerlof();
  const { verlofAanvragen, verlofIntrekken } = useThuisMutaties();
  const [form, setForm] = useState<ThuisVerlofInvoer>(LEEG_VERLOF);

  function indienen(e: React.FormEvent) {
    e.preventDefault();
    verlofAanvragen.mutate({ ...form, reden: form.reden?.trim() || null }, { onSuccess: () => setForm(LEEG_VERLOF) });
  }

  const fout = verlofAanvragen.error;

  return (
    <Kaart titel="Mijn verlof" icon="ti-beach">
      <div className="g2">
        <div>
          <h4 style={{ fontSize: 12, fontWeight: 700, color: 'var(--text2)', marginBottom: 8 }}>Saldo</h4>
          <div className="tbl-wrap" style={{ border: 'none', boxShadow: 'none' }}>
            <table className="tbl">
              <thead>
                <tr>
                  <th>Categorie</th>
                  <th style={{ textAlign: 'right' }}>Resterend</th>
                </tr>
              </thead>
              <tbody>
                {saldo?.map((s) => (
                  <tr key={s.categorie}>
                    <td>{VERLOFCATEGORIE_LABEL[s.categorie]}</td>
                    <td style={{ textAlign: 'right' }}>
                      {s.resterendNaReservering.toLocaleString('nl-NL')} / {s.toegekend.toLocaleString('nl-NL')} u
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <form onSubmit={indienen} style={{ marginTop: 14, display: 'flex', flexDirection: 'column', gap: 8 }}>
            <h4 style={{ fontSize: 12, fontWeight: 700, color: 'var(--text2)' }}>Nieuwe aanvraag</h4>
            <div className="frow">
              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Van</label>
                <input type="date" value={form.begindatum} onChange={(e) => setForm({ ...form, begindatum: e.target.value })} />
              </div>
              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Tot en met</label>
                <input type="date" value={form.einddatum} onChange={(e) => setForm({ ...form, einddatum: e.target.value })} />
              </div>
            </div>
            <div className="frow">
              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Uren</label>
                <input type="number" min={0.25} step={0.25} value={form.aantalUren} onChange={(e) => setForm({ ...form, aantalUren: Number(e.target.value) })} />
              </div>
              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Categorie</label>
                <select value={form.categorie} onChange={(e) => setForm({ ...form, categorie: Number(e.target.value) })}>
                  {VERLOFCATEGORIE_LABEL.map((label, i) => (
                    <option key={i} value={i}>
                      {label}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div className="fld" style={{ marginBottom: 0 }}>
              <input type="text" placeholder="Reden (optioneel)" value={form.reden ?? ''} onChange={(e) => setForm({ ...form, reden: e.target.value })} />
            </div>
            {fout instanceof ApiFout && <p style={{ fontSize: 12, color: 'var(--rose)' }}>{fout.message}</p>}
            <button type="submit" disabled={verlofAanvragen.isPending} className="btn btn-primary btn-sm" style={{ alignSelf: 'flex-start' }}>
              Verlof aanvragen
            </button>
          </form>
        </div>

        <div>
          <h4 style={{ fontSize: 12, fontWeight: 700, color: 'var(--text2)', marginBottom: 8 }}>Aanvragen</h4>
          {isLoading ? (
            <p style={{ color: 'var(--text3)' }}>Laden…</p>
          ) : aanvragen && aanvragen.length > 0 ? (
            <div>
              {aanvragen.map((a) => (
                <div key={a.id} className="tl-item">
                  <span
                    className="tl-dot"
                    style={{
                      background:
                        a.status === VerlofStatus.Goedgekeurd ? 'var(--green)' : a.status === VerlofStatus.Afgekeurd ? 'var(--rose)' : 'var(--amber)',
                    }}
                  />
                  <div className="tl-body" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div>
                      <h5>
                        {korteDatum(a.begindatum)} – {korteDatum(a.einddatum)}
                      </h5>
                      <p>
                        {a.aantalUren.toLocaleString('nl-NL')} u · {VERLOFCATEGORIE_LABEL[a.categorie]} ·{' '}
                        {VERLOFSTATUS_LABEL[a.status]}
                      </p>
                    </div>
                    {a.status === VerlofStatus.Openstaand && (
                      <button onClick={() => verlofIntrekken.mutate(a.id)} className="btn btn-outline btn-xs">
                        Intrekken
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p style={{ fontSize: 12, color: 'var(--text3)' }}>Nog geen aanvragen.</p>
          )}
        </div>
      </div>
    </Kaart>
  );
}

const UREN_PERIODES = [
  { sleutel: 'maand', label: 'Maand' },
  { sleutel: 'kwartaal', label: 'Kwartaal' },
  { sleutel: 'jaar', label: 'Jaar' },
] as const;

function urenPeriodeVan(sleutel: string): string {
  const [j, m] = vandaagIso().slice(0, 10).split('-').map(Number);
  if (sleutel === 'jaar') return `${j}-01-01`;
  if (sleutel === 'kwartaal') {
    const km = Math.floor((m - 1) / 3) * 3 + 1;
    return `${j}-${String(km).padStart(2, '0')}-01`;
  }
  return `${j}-${String(m).padStart(2, '0')}-01`;
}

function MijnUren() {
  const [periode, setPeriode] = useState('maand');
  const { data, isLoading } = useThuisUrenoverzicht(urenPeriodeVan(periode));

  const tabs = (
    <div className="tabs" style={{ marginBottom: 0 }}>
      {UREN_PERIODES.map((p) => (
        <button key={p.sleutel} className={`tab${periode === p.sleutel ? ' on' : ''}`} onClick={() => setPeriode(p.sleutel)}>
          {p.label}
        </button>
      ))}
    </div>
  );

  return (
    <Kaart titel="Mijn geregistreerde uren" icon="ti-clock-hour-4" rechts={tabs}>
      {isLoading || !data ? (
        <p style={{ color: 'var(--text3)' }}>Laden…</p>
      ) : (
        <>
          <div className="g3" style={{ marginBottom: 14 }}>
            <Stat label="Gewerkt" waarde={`${data.gewerkteUren} u`} kleur="var(--blue)" />
            <Stat label="Verwacht (contract)" waarde={`${data.verwachteUren} u`} kleur="var(--text2)" />
            <Stat
              label={data.meerMinderUren >= 0 ? 'Meerwerk' : 'Minderwerk'}
              waarde={`${data.meerMinderUren >= 0 ? '+' : ''}${data.meerMinderUren} u`}
              kleur={data.meerMinderUren >= 0 ? 'var(--green)' : 'var(--rose)'}
            />
          </div>
          <h5 style={{ fontSize: 11, fontWeight: 700, color: 'var(--text3)', textTransform: 'uppercase', marginBottom: 6 }}>
            Per week
          </h5>
          {data.perWeek.length === 0 ? (
            <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen geregistreerde uren in deze periode.</p>
          ) : (
            data.perWeek.map((w) => (
              <div key={w.weekBegin} className="tl-item">
                <span className="tl-dot" style={{ background: 'var(--green)' }} />
                <div className="tl-body" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <h5>week van {datumNl(w.weekBegin)}</h5>
                  <p>
                    <strong>{w.gewerkteUren} u</strong> · {w.aantalSessies} dag(en)
                  </p>
                </div>
              </div>
            ))
          )}
        </>
      )}
    </Kaart>
  );
}

function Stat({ label, waarde, kleur }: { label: string; waarde: string; kleur: string }) {
  return (
    <div style={{ background: 'var(--surface2)', borderRadius: 'var(--r-sm)', padding: '10px 12px' }}>
      <div style={{ fontSize: 9, fontWeight: 700, color: 'var(--text3)', textTransform: 'uppercase' }}>{label}</div>
      <div style={{ fontSize: 20, fontWeight: 700, color: kleur }}>{waarde}</div>
    </div>
  );
}
