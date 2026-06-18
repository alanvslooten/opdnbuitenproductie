import { useState } from 'react';
import { useStamgroepen, useStamgroepMutaties } from '../../api/queries';
import type { StamgroepDto } from '../../types';

function GroepRij({ groep }: { groep: StamgroepDto }) {
  const { bewerken, verwijderen } = useStamgroepMutaties();
  const [naam, setNaam] = useState(groep.naam);
  const [max, setMax] = useState(groep.maxKinderen);

  const gewijzigd = naam !== groep.naam || max !== groep.maxKinderen;

  return (
    <tr>
      <td>
        <input className="inp" style={{ width: 160 }} value={naam} onChange={(e) => setNaam(e.target.value)} />
      </td>
      <td>
        <input className="inp" style={{ width: 80 }} type="number" min={1} max={16} value={max} onChange={(e) => setMax(Number(e.target.value))} />
      </td>
      <td style={{ color: 'var(--text2)' }}>{groep.aantalKinderen}</td>
      <td style={{ textAlign: 'right' }}>
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 5 }}>
          <button
            onClick={() => bewerken.mutate({ id: groep.id, invoer: { naam, maxKinderen: max } })}
            disabled={!gewijzigd || bewerken.isPending}
            className="btn btn-green btn-xs"
          >
            Bewaar
          </button>
          <button
            onClick={() => {
              if (confirm(`Groep "${groep.naam}" verwijderen?`)) verwijderen.mutate(groep.id);
            }}
            className="btn btn-rose btn-xs"
          >
            <i className="ti ti-trash" />
          </button>
        </div>
        {bewerken.isError && <div style={{ fontSize: 10, color: 'var(--rose)' }}>{bewerken.error.message}</div>}
        {verwijderen.isError && <div style={{ fontSize: 10, color: 'var(--rose)' }}>{verwijderen.error.message}</div>}
      </td>
    </tr>
  );
}

export function GroepenSectie() {
  const { data, isLoading, error } = useStamgroepen();
  const { aanmaken } = useStamgroepMutaties();
  const [naam, setNaam] = useState('');
  const [max, setMax] = useState(12);

  return (
    <div>
      <form
        onSubmit={(e) => {
          e.preventDefault();
          aanmaken.mutate(
            { naam, maxKinderen: max },
            {
              onSuccess: () => {
                setNaam('');
                setMax(12);
              },
            },
          );
        }}
        className="card"
        style={{ marginBottom: 16 }}
      >
        <div className="card-b" style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-end', gap: 12 }}>
          <div className="fld" style={{ marginBottom: 0, width: 200 }}>
            <label>Naam</label>
            <input required value={naam} onChange={(e) => setNaam(e.target.value)} placeholder="Nieuwe groep" />
          </div>
          <div className="fld" style={{ marginBottom: 0, width: 110 }}>
            <label>Max kinderen</label>
            <input type="number" min={1} max={16} value={max} onChange={(e) => setMax(Number(e.target.value))} />
          </div>
          <button type="submit" disabled={aanmaken.isPending} className="btn btn-primary btn-sm">
            <i className="ti ti-plus" /> Groep
          </button>
          {aanmaken.isError && <span style={{ fontSize: 12, color: 'var(--rose)' }}>{aanmaken.error.message}</span>}
        </div>
      </form>

      {isLoading && <div className="loader"><i className="ti ti-loader" /> Laden…</div>}
      {error && <p style={{ color: 'var(--rose)' }}>Kon de groepen niet laden.</p>}

      {data && (
        <div className="tbl-wrap">
          <table className="tbl">
            <thead>
              <tr>
                <th>Naam</th>
                <th>Max kinderen</th>
                <th>Nu geplaatst</th>
                <th style={{ textAlign: 'right' }}>Acties</th>
              </tr>
            </thead>
            <tbody>
              {data.map((g) => (
                <GroepRij key={g.id} groep={g} />
              ))}
              {data.length === 0 && (
                <tr>
                  <td colSpan={4} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                    Nog geen groepen.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
