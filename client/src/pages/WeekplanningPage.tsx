import { useState } from 'react';
import { useWeekplanning } from '../api/queries';
import { BkrBadge } from '../components/BkrBadge';
import { korteDatum, vandaagIso, verschuifDagen, weekBeginIso } from '../datum';

const BANNER = 'https://images.unsplash.com/photo-1567405258710-35a7015252c0?q=80&w=1200&auto=format&fit=crop';

export function WeekplanningPage() {
  const [datum, setDatum] = useState(vandaagIso());
  const { data, isLoading, error } = useWeekplanning(datum);

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
                      {d.kinderen.length > 0 && (
                        <ul style={{ marginTop: 4, listStyle: 'none', fontSize: 10, color: 'var(--text3)' }}>
                          {d.kinderen.map((k) => (
                            <li key={k.id}>{k.voornaam}</li>
                          ))}
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
        BKR-badge per dag: aantal kinderen · vereiste pm'ers. Rood = boven het wettelijk groepsmaximum.
      </p>
    </div>
  );
}
