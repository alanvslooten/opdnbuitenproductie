import { useState } from 'react';
import {
  useBeschikbaarheid,
  useThuisMutaties,
  useThuisRooster,
  useThuisSaldo,
  useThuisUren,
  useThuisVerlof,
} from '../api/queries';
import { ApiFout } from '../api/client';
import { korteDatum, vandaagIso, verschuifDagen, weekBeginIso } from '../datum';
import {
  VERLOFCATEGORIE_LABEL,
  VERLOFSTATUS_LABEL,
  VerlofCategorie,
  VerlofStatus,
  WEEKDAGEN,
  type ThuisVerlofInvoer,
} from '../types';

function urenLabel(kwartieren: number): string {
  return `${(kwartieren / 4).toLocaleString('nl-NL')} u`;
}

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
      ) : data.dagen.length === 0 ? (
        <p style={{ fontSize: 12, color: 'var(--text3)' }}>Je bent deze week niet ingepland.</p>
      ) : (
        <div>
          {data.dagen.map((d) => (
            <div key={`${d.datum}-${d.stamgroepId}`} className="tl-item">
              <span className="tl-dot" style={{ background: 'var(--blue)' }} />
              <div className="tl-body" style={{ display: 'flex', justifyContent: 'space-between' }}>
                <h5>
                  {korteDatum(d.datum)} · {d.stamgroepNaam}
                  {d.taakomschrijving && <span style={{ fontWeight: 400, color: 'var(--text3)' }}> — {d.taakomschrijving}</span>}
                </h5>
                {d.urencorrectieKwartieren !== 0 && <p>{correctieLabel(d.urencorrectieKwartieren)}</p>}
              </div>
            </div>
          ))}
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

function MijnUren() {
  const { data, isLoading } = useThuisUren();
  return (
    <Kaart titel="Mijn geregistreerde uren (deze week)" icon="ti-clock-hour-4">
      {isLoading ? (
        <p style={{ color: 'var(--text3)' }}>Laden…</p>
      ) : data && data.length > 0 ? (
        <div>
          {data.map((u) => (
            <div key={u.id} className="tl-item">
              <span className="tl-dot" style={{ background: u.isOpen ? 'var(--amber)' : 'var(--green)' }} />
              <div className="tl-body" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h5>
                  {korteDatum(u.datum)}
                  {u.stamgroepNaam && <span style={{ fontWeight: 400, color: 'var(--text3)' }}> · {u.stamgroepNaam}</span>}
                </h5>
                <p>{u.isOpen ? <em style={{ color: 'var(--amber)' }}>nog ingeklokt</em> : urenLabel(u.gewerkteKwartieren)}</p>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen uren geregistreerd deze week.</p>
      )}
    </Kaart>
  );
}
