import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useWachtlijst, useWachtlijstMutaties } from '../api/queries';
import { VoorstelDialog } from '../components/VoorstelDialog';
import { VoorstelHistorieDialog } from '../components/VoorstelHistorieDialog';
import { opvangdagenTekst } from '../components/OpvangdagenKiezer';
import { korteDatum } from '../datum';
import { WACHTLIJST_STATUS_LABEL, type WachtlijstInschrijvingDto } from '../types';

// Rendabiliteits-indicatie: meer gewenste opvangdagen = meer omzet. Puur weergave —
// beïnvloedt de prioriteitsvolgorde NIET (feedback Erik).
function rendabiliteit(dagenBits: number): { dagen: number; euro: string } {
  const dagen = [1, 2, 4, 8, 16].filter((b) => (dagenBits & b) !== 0).length;
  const euro = dagen >= 4 ? '€€€' : dagen >= 3 ? '€€' : '€';
  return { dagen, euro };
}

export function WachtlijstPage() {
  const [toonGeplaatst, setToonGeplaatst] = useState(false);
  const [alleenVoorstel, setAlleenVoorstel] = useState(false);
  const { data, isLoading, error } = useWachtlijst(toonGeplaatst);
  const { bovenaan, verwijderen } = useWachtlijstMutaties();

  const rijen = (data ?? []).filter((w) => !alleenVoorstel || w.heeftOpenVoorstel);

  const [voorstelVoor, setVoorstelVoor] = useState<WachtlijstInschrijvingDto | null>(null);
  const [historieVoor, setHistorieVoor] = useState<WachtlijstInschrijvingDto | null>(null);

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Wachtlijst</h1>
          <p>Prioriteit op basis van intern, anciënniteit en langst wachtend</p>
        </div>
        <div className="ph-actions" style={{ alignItems: 'center' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, color: 'var(--text2)' }}>
            <input type="checkbox" checked={toonGeplaatst} onChange={(e) => setToonGeplaatst(e.target.checked)} />
            Toon geplaatste
          </label>
          <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, color: 'var(--text2)' }}>
            <input type="checkbox" checked={alleenVoorstel} onChange={(e) => setAlleenVoorstel(e.target.checked)} />
            Alleen met voorstel
          </label>
          <Link to="/wachtlijst/nieuw" className="btn btn-primary btn-sm">
            <i className="ti ti-plus" /> Inschrijving
          </Link>
        </div>
      </div>

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon de wachtlijst niet laden.</p>}

      {data && (
        <div className="tbl-wrap">
          <table className="tbl">
            <thead>
              <tr>
                <th>#</th>
                <th>Kind</th>
                <th>Type</th>
                <th>Gewenst vanaf</th>
                <th>Openstaande dagen</th>
                <th title="Rendabiliteit: meer gewenste dagen = meer omzet (beïnvloedt de volgorde niet)">Rendabiliteit</th>
                <th title="Prioriteitsscore">Score</th>
                <th>Status</th>
                <th style={{ textAlign: 'right' }}>Acties</th>
              </tr>
            </thead>
            <tbody>
              {rijen.map((w, i) => (
                <tr key={w.id} style={{ verticalAlign: 'top' }}>
                  <td style={{ color: 'var(--text3)' }}>{i + 1}</td>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontWeight: 600 }}>
                      {w.handmatigBovenaan && <span title="Handmatig bovenaan (personeelskind)">📌</span>}
                      {w.voornaam} {w.achternaam}
                    </div>
                    <div style={{ fontSize: 10, color: 'var(--text3)' }}>
                      geb. {korteDatum(w.geboortedatum)}
                      {w.wordtBinnenkortVier && <span style={{ marginLeft: 4, color: 'var(--amber)' }}>• wordt binnenkort 4</span>}
                    </div>
                  </td>
                  <td>
                    {w.isIntern ? (
                      <span className="badge b-violet">intern</span>
                    ) : (
                      <span className="badge b-gray">extern</span>
                    )}
                  </td>
                  <td style={{ color: 'var(--text2)' }}>{korteDatum(w.gewensteStartdatum)}</td>
                  <td>
                    <span>{opvangdagenTekst(w.openstaandeDagen)}</span>
                    {w.reedsGeplaatsteDagen !== 0 && (
                      <div style={{ fontSize: 10, color: 'var(--green)' }}>geplaatst: {opvangdagenTekst(w.reedsGeplaatsteDagen)}</div>
                    )}
                  </td>
                  <td>
                    {(() => {
                      const r = rendabiliteit(w.gewensteOpvangdagen);
                      return (
                        <span
                          className="badge b-green"
                          title={`${r.dagen} gewenste dag(en) — meer dagen = hogere omzet (indicatie; beïnvloedt de volgorde niet)`}
                        >
                          {r.dagen}d · {r.euro}
                        </span>
                      );
                    })()}
                  </td>
                  <td>
                    <span style={{ fontWeight: 700 }} title={w.prioriteitOnderdelen.join('\n')}>
                      {w.prioriteitsscore}
                    </span>
                  </td>
                  <td style={{ color: 'var(--text2)' }}>
                    {w.heeftOpenVoorstel ? (
                      <span className="badge b-gold" title="Er staat een verstuurd voorstel open">
                        <i className="ti ti-send" /> voorstel verstuurd
                      </span>
                    ) : (
                      WACHTLIJST_STATUS_LABEL[w.status]
                    )}
                  </td>
                  <td>
                    <div style={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'flex-end', gap: 5 }}>
                      <button onClick={() => setVoorstelVoor(w)} disabled={w.openstaandeDagen === 0} className="btn btn-green btn-xs">
                        <i className="ti ti-send" /> Voorstel
                      </button>
                      <button onClick={() => setHistorieVoor(w)} className="btn btn-outline btn-xs">
                        <i className="ti ti-history" /> Historie
                      </button>
                      <Link to={`/wachtlijst/${w.id}`} className="btn btn-outline btn-xs">
                        <i className="ti ti-pencil" />
                      </Link>
                      <button
                        onClick={() => bovenaan.mutate({ id: w.id, bovenaan: !w.handmatigBovenaan })}
                        className="btn btn-outline btn-xs"
                        title={w.handmatigBovenaan ? 'Niet meer bovenaan' : 'Zet bovenaan (personeelskind)'}
                      >
                        <i className={`ti ${w.handmatigBovenaan ? 'ti-pin-filled' : 'ti-pin'}`} />
                      </button>
                      <button
                        onClick={() => {
                          if (confirm(`${w.voornaam} ${w.achternaam} van de wachtlijst verwijderen?`)) {
                            verwijderen.mutate(w.id);
                          }
                        }}
                        className="btn btn-rose btn-xs"
                      >
                        <i className="ti ti-trash" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {rijen.length === 0 && (
                <tr>
                  <td colSpan={9} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                    {alleenVoorstel
                      ? 'Geen inschrijvingen met een openstaand voorstel.'
                      : toonGeplaatst
                        ? 'Geen inschrijvingen.'
                        : 'Geen wachtende inschrijvingen.'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <p style={{ marginTop: 12, fontSize: 10, color: 'var(--text3)' }}>
        Volgorde: handmatig bovenaan (📌) eerst, dan prioriteitsscore (intern + anciënniteit), dan langst wachtend.
        Beweeg over de score voor de onderbouwing.
      </p>

      {voorstelVoor && <VoorstelDialog inschrijving={voorstelVoor} onClose={() => setVoorstelVoor(null)} />}
      {historieVoor && <VoorstelHistorieDialog inschrijving={historieVoor} onClose={() => setHistorieVoor(null)} />}
    </div>
  );
}
