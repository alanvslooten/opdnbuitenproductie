import { useState, type FormEvent } from 'react';
import { useStamgroepen, useStamgroepMutaties } from '../api/queries';
import { ApiFout } from '../api/client';

export function StamgroepenPage() {
  const { data, isLoading, error } = useStamgroepen();
  const { aanmaken, verwijderen } = useStamgroepMutaties();

  const [naam, setNaam] = useState('');
  const [maxKinderen, setMaxKinderen] = useState(12);
  const [fout, setFout] = useState<string | null>(null);
  // Verwijder-bevestiging: doelgroep + ingevoerd wachtwoord.
  const [doel, setDoel] = useState<{ id: string; naam: string } | null>(null);
  const [wachtwoord, setWachtwoord] = useState('');
  const [verwijderFout, setVerwijderFout] = useState<string | null>(null);

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

  function startVerwijderen(id: string, groepNaam: string) {
    setVerwijderFout(null);
    setWachtwoord('');
    setDoel({ id, naam: groepNaam });
  }

  async function bevestigVerwijderen(e: FormEvent) {
    e.preventDefault();
    if (!doel) return;
    setVerwijderFout(null);
    try {
      await verwijderen.mutateAsync({ id: doel.id, wachtwoord });
      setDoel(null);
    } catch (err) {
      setVerwijderFout(err instanceof ApiFout ? err.message : 'Verwijderen mislukt.');
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
                  <button onClick={() => startVerwijderen(g.id, g.naam)} className="btn btn-rose btn-xs">
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

      {doel && (
        <div className="overlay on" onClick={() => setDoel(null)}>
          <form className="modal" style={{ maxWidth: 420 }} onClick={(e) => e.stopPropagation()} onSubmit={bevestigVerwijderen}>
            <div className="modal-h">
              <h2>
                <i className="ti ti-alert-triangle" style={{ color: 'var(--rose)' }} /> Stamgroep verwijderen
              </h2>
              <button type="button" className="xbtn" onClick={() => setDoel(null)}>
                <i className="ti ti-x" />
              </button>
            </div>
            <div className="modal-b">
              <p style={{ fontSize: 13, marginBottom: 12 }}>
                Weet je zeker dat je <strong>{doel.naam}</strong> wilt verwijderen? Dit kan niet ongedaan
                worden gemaakt. Bevestig met je wachtwoord.
              </p>
              {verwijderFout && (
                <div className="alert alert-bad" style={{ marginBottom: 12 }}>
                  <i className="ti ti-alert-circle" />
                  <span>{verwijderFout}</span>
                </div>
              )}
              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Wachtwoord</label>
                <input
                  type="password"
                  autoFocus
                  value={wachtwoord}
                  onChange={(e) => setWachtwoord(e.target.value)}
                  required
                />
              </div>
            </div>
            <div className="modal-f" style={{ position: 'static' }}>
              <button type="button" onClick={() => setDoel(null)} className="btn btn-outline btn-sm">
                Annuleren
              </button>
              <button type="submit" disabled={verwijderen.isPending} className="btn btn-rose btn-sm">
                <i className="ti ti-trash" /> Definitief verwijderen
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
