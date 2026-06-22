import { useState } from 'react';
import { useMaandplanning } from '../api/queries';
import { BkrBadge } from '../components/BkrBadge';
import { korteDatum, vandaagIso } from '../datum';

const MAANDEN = [
  'januari', 'februari', 'maart', 'april', 'mei', 'juni',
  'juli', 'augustus', 'september', 'oktober', 'november', 'december',
];

// Verschuif een ISO-datum een aantal maanden (op de 1e, tijdzone-veilig).
function verschuifMaand(iso: string, delta: number): string {
  const [jaar, maand] = iso.slice(0, 10).split('-').map(Number);
  const d = new Date(jaar, maand - 1 + delta, 1);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`;
}

export function MaandplanningPage() {
  const [datum, setDatum] = useState(vandaagIso());
  const { data, isLoading, error } = useMaandplanning(datum);

  const titel = data ? `${MAANDEN[data.maand - 1]} ${data.jaar}` : '…';

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Maandplanning</h1>
          <p>Alleen-lezen maandoverzicht van bezetting en BKR per stamgroep</p>
        </div>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 14, flexWrap: 'wrap' }}>
        <div className="wk-nav" style={{ marginBottom: 0 }}>
          <button onClick={() => setDatum((d) => verschuifMaand(d, -1))}>
            <i className="ti ti-chevron-left" />
          </button>
          <span>{titel}</span>
          <button onClick={() => setDatum((d) => verschuifMaand(d, 1))}>
            <i className="ti ti-chevron-right" />
          </button>
        </div>
        <button className="btn btn-outline btn-sm" onClick={() => setDatum(vandaagIso())}>
          <i className="ti ti-calendar" /> Deze maand
        </button>
      </div>

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon de maandplanning niet laden.</p>}

      {data?.weken.map((week) => (
        <div key={week.weekBegin} className="card" style={{ marginBottom: 12 }}>
          <div className="card-h">
            <h3>
              <i className="ti ti-calendar-week" style={{ color: 'var(--primary)' }} /> Week van {korteDatum(week.weekBegin)}
            </h3>
          </div>
          <div className="tbl-wrap" style={{ border: 'none', boxShadow: 'none' }}>
            <table className="tbl">
              <thead>
                <tr>
                  <th>Stamgroep</th>
                  {week.stamgroepen[0]?.dagen.map((d) => (
                    <th key={d.datum} style={{ textAlign: 'center' }}>
                      {korteDatum(d.datum)}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {week.stamgroepen.map((g) => (
                  <tr key={g.stamgroepId}>
                    <td style={{ fontWeight: 600 }}>{g.naam}</td>
                    {g.dagen.map((d) => (
                      <td key={d.datum} style={{ textAlign: 'center' }}>
                        <BkrBadge bkr={d.bkr} />
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ))}

      <p style={{ fontSize: 10, color: 'var(--text3)' }}>
        BKR-badge per dag: aantal kinderen · vereiste begeleiders. Rood = boven het wettelijk groepsmaximum.
      </p>
    </div>
  );
}
