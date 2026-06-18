import { useState, type FormEvent } from 'react';
import { useStamgroepen, useStamgroepMutaties } from '../api/queries';
import { ApiFout } from '../api/client';

export function StamgroepenPage() {
  const { data, isLoading, error } = useStamgroepen();
  const { aanmaken, verwijderen } = useStamgroepMutaties();

  const [naam, setNaam] = useState('');
  const [maxKinderen, setMaxKinderen] = useState(12);
  const [fout, setFout] = useState<string | null>(null);

  async function voegToe(e: FormEvent) {
    e.preventDefault();
    setFout(null);
    try {
      await aanmaken.mutateAsync({ naam, maxKinderen });
      setNaam('');
      setMaxKinderen(12);
    } catch (err) {
      setFout(err instanceof ApiFout ? err.message : 'Aanmaken mislukt.');
    }
  }

  async function verwijder(id: string, naam: string) {
    setFout(null);
    if (!confirm(`Stamgroep "${naam}" verwijderen?`)) return;
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
          <h1>Stamgroepen</h1>
          <p>Groepen en hun maximale bezetting</p>
        </div>
      </div>

      {fout && (
        <div className="alert alert-bad">
          <i className="ti ti-alert-circle" />
          <span>{fout}</span>
        </div>
      )}

      <form onSubmit={voegToe} className="card" style={{ marginBottom: 16 }}>
        <div className="card-b" style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-end', gap: 12 }}>
          <div className="fld" style={{ marginBottom: 0, flex: 1, minWidth: 180 }}>
            <label>Naam</label>
            <input value={naam} onChange={(e) => setNaam(e.target.value)} required />
          </div>
          <div className="fld" style={{ marginBottom: 0, width: 120 }}>
            <label>Max. kinderen</label>
            <input
              type="number"
              min={1}
              max={16}
              value={maxKinderen}
              onChange={(e) => setMaxKinderen(Number(e.target.value))}
            />
          </div>
          <button type="submit" disabled={aanmaken.isPending} className="btn btn-primary btn-sm">
            <i className="ti ti-plus" /> Toevoegen
          </button>
        </div>
      </form>

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon stamgroepen niet laden.</p>}

      <div className="tbl-wrap">
        <table className="tbl">
          <thead>
            <tr>
              <th>Naam</th>
              <th>Bezetting</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {data?.map((g) => (
              <tr key={g.id}>
                <td style={{ fontWeight: 600 }}>{g.naam}</td>
                <td>
                  <span
                    style={
                      g.aantalKinderen >= g.maxKinderen
                        ? { fontWeight: 700, color: 'var(--rose)' }
                        : undefined
                    }
                  >
                    {g.aantalKinderen} / {g.maxKinderen}
                  </span>
                </td>
                <td style={{ textAlign: 'right' }}>
                  <button onClick={() => verwijder(g.id, g.naam)} className="btn btn-rose btn-xs">
                    <i className="ti ti-trash" />
                  </button>
                </td>
              </tr>
            ))}
            {data?.length === 0 && (
              <tr>
                <td colSpan={3} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                  Nog geen stamgroepen.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
