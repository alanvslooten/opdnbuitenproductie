import { useState } from 'react';
import { useDagplaatsingen, useStamgroepen, useWeekplanning } from '../api/queries';
import { BkrBadge } from '../components/BkrBadge';
import { DagplaatsingDialog } from '../components/DagplaatsingDialog';
import type { DagplaatsingDto } from '../types';
import { korteDatum, vandaagIso, verschuifDagen, weekBeginIso } from '../datum';

const BANNER = 'https://images.unsplash.com/photo-1567405258710-35a7015252c0?q=80&w=1200&auto=format&fit=crop';

interface Gekozen {
  kindId: string;
  naam: string;
  datum: string;
  groepId: string;
}

export function WeekplanningPage() {
  const [datum, setDatum] = useState(vandaagIso());
  const { data, isLoading, error } = useWeekplanning(datum);
  const { data: stamgroepen } = useStamgroepen();

  const weekBegin = data?.weekBegin ?? weekBeginIso(datum);
  const { data: afwijkingen } = useDagplaatsingen(weekBegin, verschuifDagen(weekBegin, 4));
  const [gekozen, setGekozen] = useState<Gekozen | null>(null);

  // Snelle opzoeking van een afwijking per (kind, datum) — datum als kale yyyy-MM-dd.
  const afwijkingBij = new Map<string, DagplaatsingDto>();
  for (const a of afwijkingen ?? []) {
    afwijkingBij.set(`${a.kindId}|${a.datum.slice(0, 10)}`, a);
  }
  const sleutel = (kindId: string, dagDatum: string) => `${kindId}|${dagDatum.slice(0, 10)}`;

  const gekozenAfwijking = gekozen
    ? afwijkingBij.get(sleutel(gekozen.kindId, gekozen.datum))
    : undefined;

  // Dag-koppen afleiden uit de eerste groep (alle groepen hebben dezelfde 5 dagen).
  const dagKoppen = data?.stamgroepen[0]?.dagen ?? [];

  return (
    <div className="view">
      <div className="page-banner">
        <img src={BANNER} alt="" />
        <div className="page-banner-overlay">
          <div className="page-banner-text">
            <h1>
              <i className="ti ti-calendar-week" /> Weekplanning
            </h1>
            <p>Aanwezigheid en BKR per stamgroep, per dag</p>
          </div>
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

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon de weekplanning niet laden.</p>}

      {data && (
        <div className="tbl-wrap">
          <table className="tbl">
            <thead>
              <tr>
                <th>Stamgroep</th>
                {dagKoppen.map((d) => (
                  <th key={d.datum} style={{ textAlign: 'center' }}>
                    <div>{korteDatum(d.datum)}</div>
                    {d.isSchoolvakantie && (
                      <div style={{ fontSize: 9, fontWeight: 600, color: 'var(--amber)' }}>vakantie</div>
                    )}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {data.stamgroepen.map((g) => (
                <tr key={g.stamgroepId} style={{ verticalAlign: 'top' }}>
                  <td style={{ fontWeight: 600 }}>
                    {g.naam}
                    <div style={{ fontSize: 10, fontWeight: 400, color: 'var(--text3)' }}>max {g.maxKinderen}</div>
                  </td>
                  {g.dagen.map((d) => (
                    <td key={d.datum} style={{ textAlign: 'center' }}>
                      <BkrBadge bkr={d.bkr} />
                      {d.begeleiders.length > 0 && (
                        <div style={{ marginTop: 4, display: 'flex', flexDirection: 'column', gap: 2, alignItems: 'center' }}>
                          {d.begeleiders.map((b) => (
                            <span
                              key={b.medewerkerId}
                              className="badge b-violet"
                              style={{ fontSize: 9 }}
                              title={b.taakomschrijving ?? 'Ingeplande begeleider'}
                            >
                              <i className="ti ti-user" /> {b.naam}
                            </span>
                          ))}
                        </div>
                      )}
                      {d.kinderen.length > 0 && (
                        <ul style={{ marginTop: 4, listStyle: 'none', fontSize: 10, color: 'var(--text3)' }}>
                          {d.kinderen.map((k) => {
                            const heeftAfwijking = afwijkingBij.has(sleutel(k.id, d.datum));
                            return (
                              <li key={k.id}>
                                <button
                                  type="button"
                                  className="lnk"
                                  title="Dagplaatsing wijzigen (ruildag, andere groep, afwezig)"
                                  onClick={() =>
                                    setGekozen({
                                      kindId: k.id,
                                      naam: k.voornaam,
                                      datum: d.datum,
                                      groepId: g.stamgroepId,
                                    })
                                  }
                                  style={{
                                    background: 'none',
                                    border: 'none',
                                    padding: 0,
                                    font: 'inherit',
                                    color: heeftAfwijking ? 'var(--violet)' : 'var(--text3)',
                                    fontWeight: heeftAfwijking ? 600 : 400,
                                    cursor: 'pointer',
                                  }}
                                >
                                  {heeftAfwijking && <i className="ti ti-calendar-cog" style={{ fontSize: 9 }} />} {k.voornaam}
                                </button>
                              </li>
                            );
                          })}
                        </ul>
                      )}
                    </td>
                  ))}
                </tr>
              ))}
              {data.stamgroepen.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                    Geen stamgroepen om te plannen.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <p style={{ marginTop: 12, fontSize: 10, color: 'var(--text3)' }}>
        BKR-badge per dag: aantal kinderen · vereiste begeleiders. Rood = boven het wettelijk groepsmaximum.
        Klik een kind om een dagplaatsing te wijzigen (ruildag, andere groep, afwezig);
        <i className="ti ti-calendar-cog" style={{ color: 'var(--violet)' }} /> markeert een afwijking.
      </p>

      {gekozen && stamgroepen && (
        <DagplaatsingDialog
          kindId={gekozen.kindId}
          kindNaam={gekozen.naam}
          datum={gekozen.datum}
          huidigeGroepId={gekozen.groepId}
          bestaand={gekozenAfwijking}
          stamgroepen={stamgroepen}
          onSluit={() => setGekozen(null)}
        />
      )}
    </div>
  );
}
