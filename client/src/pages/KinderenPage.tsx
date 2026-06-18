import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useKinderen, useKindMutaties, useStamgroepen } from '../api/queries';
import { ApiFout } from '../api/client';
import { opvangdagenTekst } from '../components/OpvangdagenKiezer';
import { Contracttype } from '../types';

export function KinderenPage() {
  const [stamgroepFilter, setStamgroepFilter] = useState<string>('');
  const { data: groepen } = useStamgroepen();
  const { data, isLoading, error } = useKinderen(stamgroepFilter || undefined);
  const { verwijderen } = useKindMutaties();
  const [fout, setFout] = useState<string | null>(null);

  const groepNaam = (id: string) => groepen?.find((g) => g.id === id)?.naam ?? '—';

  async function verwijder(id: string, naam: string) {
    setFout(null);
    if (!confirm(`${naam} verwijderen?`)) return;
    try {
      await verwijderen.mutateAsync(id);
    } catch (err) {
      setFout(err instanceof ApiFout ? err.message : 'Verwijderen mislukt.');
    }
  }

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Kinderen</h1>
          <p>Inschrijvingen, opvangdagen en contracten</p>
        </div>
        <div className="ph-actions">
          <Link to="/kinderen/nieuw" className="btn btn-primary btn-sm">
            <i className="ti ti-user-plus" /> Kind toevoegen
          </Link>
        </div>
      </div>

      {fout && (
        <div className="alert alert-bad">
          <i className="ti ti-alert-circle" />
          <span>{fout}</span>
        </div>
      )}

      <div className="filters">
        <select
          className="fchip"
          style={{ padding: '7px 12px' }}
          value={stamgroepFilter}
          onChange={(e) => setStamgroepFilter(e.target.value)}
        >
          <option value="">Alle stamgroepen</option>
          {groepen?.map((g) => (
            <option key={g.id} value={g.id}>
              {g.naam}
            </option>
          ))}
        </select>
      </div>

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon kinderen niet laden.</p>}

      <div className="tbl-wrap">
        <table className="tbl">
          <thead>
            <tr>
              <th>Naam</th>
              <th>Stamgroep</th>
              <th>Opvangdagen</th>
              <th>Contract</th>
              <th>Tot</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {data?.map((k) => (
              <tr key={k.id}>
                <td style={{ fontWeight: 600 }}>
                  {k.voornaam} {k.achternaam}
                  {k.wordtBinnenkortVier && (
                    <span className="badge b-gold" style={{ marginLeft: 8 }} title="Wordt binnenkort 4 en stroomt uit">
                      bijna 4
                    </span>
                  )}
                </td>
                <td>{groepNaam(k.stamgroepId)}</td>
                <td>{opvangdagenTekst(k.gewensteOpvangdagen)}</td>
                <td>{k.contracttype === Contracttype.Weken40 ? '40 wkn' : '49 wkn'}</td>
                <td style={{ color: 'var(--text3)' }}>{k.effectieveEinddatum}</td>
                <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                  <Link to={`/kinderen/${k.id}`} className="btn btn-outline btn-xs" style={{ marginRight: 6 }}>
                    <i className="ti ti-pencil" /> Bewerken
                  </Link>
                  <button onClick={() => verwijder(k.id, k.voornaam)} className="btn btn-rose btn-xs">
                    <i className="ti ti-trash" />
                  </button>
                </td>
              </tr>
            ))}
            {data?.length === 0 && (
              <tr>
                <td colSpan={6} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                  Geen kinderen gevonden.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
