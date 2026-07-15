import { useMemo, useState } from 'react';
import { useKennisbank, useKennisbankDocument, useKennisbankMutaties } from '../api/queries';
import { useAuth } from '../auth/AuthContext';
import { Capabilities, type KennisbankItemDto } from '../types';
import { korteDatum } from '../datum';

const LEEG = { titel: '', categorie: '', inhoud: '' };

/**
 * Interne kennisbank: read-only naslag voor medewerkers (protocollen, pedagogisch
 * beleidsplan, kledingvoorschriften). Iedereen leest hetzelfde; de beheerder onderhoudt
 * de documenten. Ook thuis inzichtelijk.
 */
export function KennisbankPage() {
  const { heeft } = useAuth();
  const magBeheren = heeft(Capabilities.InstellingenBeheren);
  const { data: lijst, isLoading } = useKennisbank();
  const [gekozenId, setGekozenId] = useState<string | null>(null);
  const { data: document } = useKennisbankDocument(gekozenId ?? undefined);
  const { toevoegen, bijwerken, verwijderen } = useKennisbankMutaties();

  const [bewerkt, setBewerkt] = useState<null | { id: string | null; titel: string; categorie: string; inhoud: string }>(null);

  // Documenten gegroepeerd per categorie voor een overzichtelijke lijst.
  const perCategorie = useMemo(() => {
    const groepen = new Map<string, KennisbankItemDto[]>();
    for (const item of lijst ?? []) {
      const key = item.categorie ?? 'Overig';
      (groepen.get(key) ?? groepen.set(key, []).get(key)!).push(item);
    }
    return [...groepen.entries()];
  }, [lijst]);

  async function bewaar() {
    if (!bewerkt) return;
    const invoer = { titel: bewerkt.titel, categorie: bewerkt.categorie || null, inhoud: bewerkt.inhoud };
    if (bewerkt.id) await bijwerken.mutateAsync({ id: bewerkt.id, invoer });
    else await toevoegen.mutateAsync(invoer);
    setBewerkt(null);
  }

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Kennisbank</h1>
          <p>Protocollen, beleid en afspraken — voor iedereen gelijk, ook thuis</p>
        </div>
        {magBeheren && (
          <button className="btn btn-primary btn-sm" onClick={() => setBewerkt({ id: null, ...LEEG })}>
            <i className="ti ti-plus" /> Nieuw document
          </button>
        )}
      </div>

      {isLoading && <div className="loader"><i className="ti ti-loader" /> Laden…</div>}

      <div style={{ display: 'grid', gridTemplateColumns: '260px 1fr', gap: 16, alignItems: 'start' }}>
        <div className="tbl-wrap" style={{ padding: 8 }}>
          {(lijst?.length ?? 0) === 0 && !isLoading && (
            <p style={{ fontSize: 12, color: 'var(--text3)', padding: 8 }}>Nog geen documenten.</p>
          )}
          {perCategorie.map(([categorie, items]) => (
            <div key={categorie} style={{ marginBottom: 10 }}>
              <div style={{ fontSize: 10, fontWeight: 700, textTransform: 'uppercase', color: 'var(--text3)', padding: '4px 8px' }}>
                {categorie}
              </div>
              {items.map((item) => (
                <button
                  key={item.id}
                  onClick={() => setGekozenId(item.id)}
                  className="lnk"
                  style={{
                    display: 'block', width: '100%', textAlign: 'left', padding: '6px 8px', border: 'none',
                    background: gekozenId === item.id ? 'var(--surface2)' : 'none', borderRadius: 6,
                    cursor: 'pointer', color: 'var(--text)', fontSize: 13,
                  }}
                >
                  {item.titel}
                </button>
              ))}
            </div>
          ))}
        </div>

        <div className="tbl-wrap" style={{ padding: 16, minHeight: 200 }}>
          {document ? (
            <>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 8 }}>
                <div>
                  <h2 style={{ fontSize: 18 }}>{document.titel}</h2>
                  <p style={{ fontSize: 10, color: 'var(--text3)' }}>
                    {document.categorie ?? 'Overig'} · bijgewerkt {korteDatum(document.gewijzigdOp.slice(0, 10))}
                  </p>
                </div>
                {magBeheren && (
                  <div style={{ display: 'flex', gap: 6 }}>
                    <button
                      className="btn btn-outline btn-xs"
                      onClick={() => setBewerkt({ id: document.id, titel: document.titel, categorie: document.categorie ?? '', inhoud: document.inhoud })}
                    >
                      <i className="ti ti-edit" /> Bewerken
                    </button>
                    <button
                      className="btn btn-rose btn-xs"
                      onClick={async () => { await verwijderen.mutateAsync(document.id); setGekozenId(null); }}
                    >
                      <i className="ti ti-trash" />
                    </button>
                  </div>
                )}
              </div>
              <div style={{ whiteSpace: 'pre-wrap', fontSize: 13, lineHeight: 1.6, marginTop: 12 }}>{document.inhoud}</div>
            </>
          ) : (
            <p style={{ fontSize: 13, color: 'var(--text3)' }}>Kies links een document om te lezen.</p>
          )}
        </div>
      </div>

      {bewerkt && (
        <div className="overlay on" onClick={() => setBewerkt(null)}>
          <div className="modal" style={{ maxWidth: 640 }} onClick={(e) => e.stopPropagation()}>
            <div className="modal-h">
              <h2><i className="ti ti-book" /> {bewerkt.id ? 'Document bewerken' : 'Nieuw document'}</h2>
              <button className="xbtn" onClick={() => setBewerkt(null)}><i className="ti ti-x" /></button>
            </div>
            <div className="modal-b">
              <div className="frow" style={{ gridTemplateColumns: '2fr 1fr' }}>
                <div className="fld">
                  <label>Titel</label>
                  <input value={bewerkt.titel} onChange={(e) => setBewerkt({ ...bewerkt, titel: e.target.value })} />
                </div>
                <div className="fld">
                  <label>Categorie</label>
                  <input value={bewerkt.categorie} placeholder="Protocollen…" onChange={(e) => setBewerkt({ ...bewerkt, categorie: e.target.value })} />
                </div>
              </div>
              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Inhoud</label>
                <textarea rows={12} value={bewerkt.inhoud} onChange={(e) => setBewerkt({ ...bewerkt, inhoud: e.target.value })} />
              </div>
            </div>
            <div className="modal-f" style={{ position: 'static' }}>
              <button className="btn btn-outline btn-sm" onClick={() => setBewerkt(null)}>Annuleren</button>
              <button
                className="btn btn-primary btn-sm"
                disabled={!bewerkt.titel.trim() || !bewerkt.inhoud.trim() || toevoegen.isPending || bijwerken.isPending}
                onClick={bewaar}
              >
                <i className="ti ti-check" /> Opslaan
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
