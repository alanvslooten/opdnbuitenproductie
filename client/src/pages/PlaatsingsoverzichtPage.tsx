import { useState } from 'react';
import { useWeekplanning } from '../api/queries';
import { korteDatum, vandaagIso, verschuifDagen, weekBeginIso } from '../datum';

const BANNER = 'https://images.unsplash.com/photo-1587616211892-f743fcca64f9?q=80&w=1200&auto=format&fit=crop';

/**
 * "Waar is er plek?" — een compact overzicht per stamgroep per dag met de vrije
 * capaciteit (max − aanwezig) en of de groep het maximum of de BKR al raakt. Hergebruikt
 * de weekplanning-data (aanwezigheid + BKR per dag, inclusief dagafwijkingen).
 */
export function PlaatsingsoverzichtPage() {
  const [datum, setDatum] = useState(vandaagIso());
  const { data, isLoading, error } = useWeekplanning(datum);
  const dagKoppen = data?.stamgroepen[0]?.dagen ?? [];

  return (
    <div className="view">
      <div className="page-banner">
        <img src={BANNER} alt="" />
        <div className="page-banner-overlay">
          <div className="page-banner-text">
            <h1>
              <i className="ti ti-map-search" /> Waar is er plek?
            </h1>
            <p>Vrije capaciteit per stamgroep, per dag</p>
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
      {error && <p style={{ color: 'var(--rose)' }}>Kon het overzicht niet laden.</p>}

      {data && (
        <div className="tbl-wrap">
          <table className="tbl">
            <thead>
              <tr>
                <th>Stamgroep</th>
                {dagKoppen.map((d) => (
                  <th key={d.datum} style={{ textAlign: 'center' }}>
                    {korteDatum(d.datum)}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {data.stamgroepen.map((g) => (
                <tr key={g.stamgroepId}>
                  <td style={{ fontWeight: 600 }}>
                    {g.naam}
                    <div style={{ fontSize: 10, fontWeight: 400, color: 'var(--text3)' }}>max {g.maxKinderen}</div>
                  </td>
                  {g.dagen.map((d) => {
                    const vrij = g.maxKinderen - d.bkr.aantalKinderen;
                    const vol = d.bkr.overschrijdt || vrij <= 0;
                    const kleur = vol ? 'var(--rose)' : vrij <= 2 ? 'var(--amber)' : 'var(--green)';
                    return (
                      <td key={d.datum} style={{ textAlign: 'center' }}>
                        <span style={{ fontWeight: 700, color: kleur, fontSize: 15 }}>
                          {vol ? 'vol' : vrij}
                        </span>
                        <div style={{ fontSize: 9, color: 'var(--text3)' }}>
                          {d.bkr.aantalKinderen}/{g.maxKinderen}
                          {d.bkr.vereisteHoeveelheidPmers != null && ` · ${d.bkr.vereisteHoeveelheidPmers} bg`}
                        </div>
                      </td>
                    );
                  })}
                </tr>
              ))}
              {data.stamgroepen.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                    Geen stamgroepen.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <p style={{ marginTop: 12, fontSize: 10, color: 'var(--text3)' }}>
        Getal = vrije plekken (max − aanwezig) die dag. Groen = ruimte, oranje = bijna vol, rood = vol of boven
        het wettelijk maximum. Onderregel: aanwezig/max · vereiste begeleiders. Houdt rekening met dagafwijkingen.
      </p>
    </div>
  );
}
