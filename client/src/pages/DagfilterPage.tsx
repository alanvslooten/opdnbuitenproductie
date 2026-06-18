import { useState } from 'react';
import { useDagplanning, useStamgroepen } from '../api/queries';
import { korteDatum, vandaagIso } from '../datum';
import { LEEFTIJDSGROEP_LABEL, Contracttype } from '../types';

export function DagfilterPage() {
  const [datum, setDatum] = useState(vandaagIso());
  const [stamgroepId, setStamgroepId] = useState('');
  const { data: groepen } = useStamgroepen();
  const { data, isLoading, error } = useDagplanning(datum, stamgroepId || undefined);

  const groepNaam = (id: string) => groepen?.find((g) => g.id === id)?.naam ?? '—';

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Dagfilter — wie is er?</h1>
          <p>Aanwezige kinderen op een gekozen dag, eventueel per stamgroep</p>
        </div>
      </div>

      <div className="frow" style={{ maxWidth: 460, marginBottom: 14 }}>
        <div className="fld">
          <label>Datum</label>
          <input type="date" value={datum} onChange={(e) => setDatum(e.target.value)} />
        </div>
        <div className="fld">
          <label>Stamgroep</label>
          <select value={stamgroepId} onChange={(e) => setStamgroepId(e.target.value)}>
            <option value="">Alle</option>
            {groepen?.map((g) => (
              <option key={g.id} value={g.id}>
                {g.naam}
              </option>
            ))}
          </select>
        </div>
      </div>

      <p style={{ marginBottom: 12, fontSize: 12, color: 'var(--text2)' }}>
        {korteDatum(datum)} — <strong>{data?.length ?? 0}</strong> kind(eren) aanwezig.
      </p>

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon de dagplanning niet laden.</p>}

      <div className="tbl-wrap">
        <table className="tbl">
          <thead>
            <tr>
              <th>Naam</th>
              <th>Stamgroep</th>
              <th>Leeftijd</th>
              <th>Contract</th>
            </tr>
          </thead>
          <tbody>
            {data?.map((k) => (
              <tr key={k.id}>
                <td style={{ fontWeight: 600 }}>
                  {k.voornaam} {k.achternaam}
                </td>
                <td>{groepNaam(k.stamgroepId)}</td>
                <td>{LEEFTIJDSGROEP_LABEL[k.leeftijdsgroep] ?? '—'}</td>
                <td>{k.contracttype === Contracttype.Weken40 ? '40 wkn' : '49 wkn'}</td>
              </tr>
            ))}
            {data?.length === 0 && (
              <tr>
                <td colSpan={4} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                  Niemand ingepland op deze dag.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
