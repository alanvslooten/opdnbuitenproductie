import { useMemo, useRef, useState } from 'react';
import {
  downloadKennisbankBijlage,
  useKennisbank,
  useKennisbankDocument,
  useKennisbankMutaties,
  useMedewerkers,
} from '../api/queries';
import { useAuth } from '../auth/AuthContext';
import { Capabilities, type KennisbankItemDto } from '../types';
import { korteDatum } from '../datum';

function bestandsgrootte(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

interface BewerkState {
  id: string | null;
  titel: string;
  categorie: string;
  inhoud: string;
  toegewezen: string[]; // medewerker-ids; leeg = voor iedereen
}

const LEEG: Omit<BewerkState, 'id'> = { titel: '', categorie: '', inhoud: '', toegewezen: [] };

/**
 * Interne kennisbank: read-only naslag voor medewerkers (protocollen, pedagogisch
 * beleidsplan, kledingvoorschriften). De beheerder onderhoudt de documenten en kan een
 * document aan specifieke medewerkers toewijzen (leeg = voor iedereen). Ook thuis inzichtelijk.
 */
export function KennisbankPage() {
  const { heeft } = useAuth();
  const magBeheren = heeft(Capabilities.InstellingenBeheren);
  const { data: lijst, isLoading } = useKennisbank();
  const { data: medewerkers } = useMedewerkers();
  const [gekozenId, setGekozenId] = useState<string | null>(null);
  const { data: document } = useKennisbankDocument(gekozenId ?? undefined);
  const { toevoegen, bijwerken, verwijderen, uploadBijlage, verwijderBijlage } = useKennisbankMutaties();

  const [bewerkt, setBewerkt] = useState<BewerkState | null>(null);
  const bestandKiezer = useRef<HTMLInputElement>(null);

  function kiesBestand(documentId: string, bestand: File | undefined) {
    if (bestand) uploadBijlage.mutate({ documentId, bestand });
    if (bestandKiezer.current) bestandKiezer.current.value = '';
  }

  const naamVan = (id: string) => {
    const m = medewerkers?.find((x) => x.id === id);
    return m ? `${m.voornaam} ${m.achternaam}` : 'medewerker';
  };

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
    const invoer = {
      titel: bewerkt.titel,
      categorie: bewerkt.categorie || null,
      inhoud: bewerkt.inhoud,
      toegewezenMedewerkerIds: bewerkt.toegewezen,
    };
    if (bewerkt.id) await bijwerken.mutateAsync({ id: bewerkt.id, invoer });
    else await toevoegen.mutateAsync(invoer);
    setBewerkt(null);
  }

  function toggleMedewerker(id: string) {
    setBewerkt((b) =>
      b ? { ...b, toegewezen: b.toegewezen.includes(id) ? b.toegewezen.filter((x) => x !== id) : [...b.toegewezen, id] } : b,
    );
  }

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Kennisbank</h1>
          <p>Protocollen, beleid en afspraken — voor iedereen of toegewezen aan medewerkers</p>
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
                    display: 'flex', alignItems: 'center', gap: 6, width: '100%', textAlign: 'left', padding: '6px 8px', border: 'none',
                    background: gekozenId === item.id ? 'var(--surface2)' : 'none', borderRadius: 6,
                    cursor: 'pointer', color: 'var(--text)', fontSize: 13,
                  }}
                >
                  <span style={{ flex: 1 }}>{item.titel}</span>
                  {item.aantalBijlagen > 0 && (
                    <i className="ti ti-paperclip" style={{ fontSize: 12, color: 'var(--text3)' }} title={`${item.aantalBijlagen} bijlage(n)`} />
                  )}
                  {item.toegewezenMedewerkerIds.length > 0 && (
                    <i className="ti ti-user-check" style={{ fontSize: 12, color: 'var(--violet)' }} title="Toegewezen aan specifieke medewerkers" />
                  )}
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
                      onClick={() => setBewerkt({
                        id: document.id, titel: document.titel, categorie: document.categorie ?? '',
                        inhoud: document.inhoud, toegewezen: document.toegewezenMedewerkerIds,
                      })}
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

              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 8 }}>
                {document.toegewezenMedewerkerIds.length === 0 ? (
                  <span className="badge b-gray"><i className="ti ti-users" /> Voor iedereen</span>
                ) : (
                  document.toegewezenMedewerkerIds.map((id) => (
                    <span key={id} className="badge b-violet"><i className="ti ti-user" /> {naamVan(id)}</span>
                  ))
                )}
              </div>

              <div style={{ whiteSpace: 'pre-wrap', fontSize: 13, lineHeight: 1.6, marginTop: 14 }}>{document.inhoud}</div>

              <div style={{ marginTop: 18, borderTop: '1px solid var(--border)', paddingTop: 12 }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 8, marginBottom: 8 }}>
                  <h3 style={{ fontSize: 13 }}><i className="ti ti-paperclip" /> Bijlagen</h3>
                  {magBeheren && (
                    <>
                      <input
                        ref={bestandKiezer}
                        type="file"
                        style={{ display: 'none' }}
                        onChange={(e) => kiesBestand(document.id, e.target.files?.[0])}
                      />
                      <button
                        className="btn btn-outline btn-xs"
                        disabled={uploadBijlage.isPending}
                        onClick={() => bestandKiezer.current?.click()}
                      >
                        <i className="ti ti-upload" /> {uploadBijlage.isPending ? 'Uploaden…' : 'Bijlage toevoegen'}
                      </button>
                    </>
                  )}
                </div>

                {document.bijlagen.length === 0 ? (
                  <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen bijlagen.</p>
                ) : (
                  document.bijlagen.map((b) => (
                    <div
                      key={b.id}
                      style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 0', borderBottom: '1px solid var(--border)', fontSize: 12 }}
                    >
                      <i className="ti ti-file" style={{ color: 'var(--text3)' }} />
                      <button
                        className="lnk"
                        style={{ flex: 1, textAlign: 'left', background: 'none', border: 'none', color: 'var(--blue)', cursor: 'pointer', fontSize: 12 }}
                        onClick={() => downloadKennisbankBijlage(b.id, b.bestandsNaam)}
                        title="Downloaden"
                      >
                        {b.bestandsNaam}
                      </button>
                      <span style={{ color: 'var(--text3)' }}>{bestandsgrootte(b.bestandsGrootte)}</span>
                      {magBeheren && (
                        <button
                          className="btn btn-ghost btn-xs"
                          title="Bijlage verwijderen"
                          onClick={() => verwijderBijlage.mutate(b.id)}
                        >
                          <i className="ti ti-trash" style={{ color: 'var(--rose)' }} />
                        </button>
                      )}
                    </div>
                  ))
                )}
              </div>
            </>
          ) : (
            <p style={{ fontSize: 13, color: 'var(--text3)' }}>Kies links een document om te lezen.</p>
          )}
        </div>
      </div>

      {bewerkt && (
        <div className="overlay on" onClick={() => setBewerkt(null)}>
          <div className="modal" style={{ width: 'min(780px, 94vw)', maxWidth: 'none' }} onClick={(e) => e.stopPropagation()}>
            <div className="modal-h">
              <h2><i className="ti ti-book" /> {bewerkt.id ? 'Document bewerken' : 'Nieuw document'}</h2>
              <button className="xbtn" onClick={() => setBewerkt(null)}><i className="ti ti-x" /></button>
            </div>
            <div className="modal-b">
              <div className="frow" style={{ gridTemplateColumns: '2fr 1fr' }}>
                <div className="fld">
                  <label>Titel</label>
                  <input autoFocus value={bewerkt.titel} onChange={(e) => setBewerkt({ ...bewerkt, titel: e.target.value })} />
                </div>
                <div className="fld">
                  <label>Categorie</label>
                  <input value={bewerkt.categorie} placeholder="Protocollen…" onChange={(e) => setBewerkt({ ...bewerkt, categorie: e.target.value })} />
                </div>
              </div>

              <div className="fld">
                <label>Inhoud</label>
                <textarea
                  className="xl"
                  rows={12}
                  value={bewerkt.inhoud}
                  onChange={(e) => setBewerkt({ ...bewerkt, inhoud: e.target.value })}
                />
              </div>

              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Zichtbaar voor</label>
                <p style={{ fontSize: 11, color: 'var(--text3)', margin: '0 0 8px' }}>
                  {bewerkt.toegewezen.length === 0
                    ? 'Niemand geselecteerd = zichtbaar voor iedereen. Vink medewerkers aan om het document alleen aan hen toe te wijzen.'
                    : `Toegewezen aan ${bewerkt.toegewezen.length} medewerker(s). De beheerder ziet het altijd.`}
                </p>
                <div
                  style={{
                    display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: 4,
                    maxHeight: 160, overflowY: 'auto', border: '1px solid var(--border)', borderRadius: 'var(--r-sm)', padding: 8,
                  }}
                >
                  {(medewerkers ?? []).length === 0 && (
                    <span style={{ fontSize: 12, color: 'var(--text3)' }}>Geen medewerkers gevonden.</span>
                  )}
                  {(medewerkers ?? []).map((m) => (
                    <label key={m.id} style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, cursor: 'pointer' }}>
                      <input
                        type="checkbox"
                        checked={bewerkt.toegewezen.includes(m.id)}
                        onChange={() => toggleMedewerker(m.id)}
                        style={{ width: 'auto' }}
                      />
                      {m.voornaam} {m.achternaam}
                    </label>
                  ))}
                </div>
                {bewerkt.toegewezen.length > 0 && (
                  <button
                    type="button"
                    className="btn btn-ghost btn-xs"
                    style={{ marginTop: 6 }}
                    onClick={() => setBewerkt({ ...bewerkt, toegewezen: [] })}
                  >
                    <i className="ti ti-users" /> Voor iedereen maken
                  </button>
                )}
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
