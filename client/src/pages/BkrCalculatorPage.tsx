import { useEffect, useState } from 'react';
import { useBkrBerekening } from '../api/queries';
import type { BkrBerekenResultaatDto } from '../types';

// De BKR-snelrekenaar (calculator): een vrije "wat-als" voor de beheerder. De
// rekenregels zitten NIET hier maar in de geteste domein-rekenkern achter
// /api/bkr/bereken; deze pagina is puur invoer + weergave. Bij elke wijziging
// wordt (kort gedebounced) opnieuw berekend, zodat het net zo "live" voelt als
// het oorspronkelijke ontwerp.

interface Band {
  sleutel: 'nulTotEen' | 'eenTotTwee' | 'tweeTotDrie' | 'drieTotVier';
  label: string;
  emoji: string;
  hint: string;
}

const BANDEN: Band[] = [
  { sleutel: 'nulTotEen', label: '0–1 jaar', emoji: '🍼', hint: 'max 3/b' },
  { sleutel: 'eenTotTwee', label: '1–2 jaar', emoji: '🧸', hint: 'max 5/b' },
  { sleutel: 'tweeTotDrie', label: '2–3 jaar', emoji: '🌱', hint: 'max 8/b' },
  { sleutel: 'drieTotVier', label: '3–4 jaar', emoji: '🌟', hint: 'max 8/b' },
];

const STATUS_ALERT: Record<string, { klasse: string; icoon: string }> = {
  ok: { klasse: 'alert-ok', icoon: 'ti-circle-check' },
  driehuurs: { klasse: 'alert-warn', icoon: 'ti-alert-triangle' },
  overschreden: { klasse: 'alert-bad', icoon: 'ti-alert-circle' },
  leeg: { klasse: 'alert-info', icoon: 'ti-info-circle' },
};

export function BkrCalculatorPage() {
  const [aantallen, setAantallen] = useState({
    nulTotEen: 6,
    eenTotTwee: 8,
    tweeTotDrie: 10,
    drieTotVier: 8,
  });
  const [aanwezig, setAanwezig] = useState(4);
  const [resultaat, setResultaat] = useState<BkrBerekenResultaatDto | null>(null);

  const berekening = useBkrBerekening();
  const bereken = berekening.mutate;

  // Korte debounce zodat tikken niet bij elke toetsaanslag de API raakt.
  useEffect(() => {
    const id = setTimeout(() => {
      bereken(
        { ...aantallen, aanwezigePmers: aanwezig },
        { onSuccess: setResultaat },
      );
    }, 200);
    return () => clearTimeout(id);
  }, [aantallen, aanwezig, bereken]);

  function zet(sleutel: Band['sleutel'], waarde: string) {
    const n = Math.max(0, parseInt(waarde, 10) || 0);
    setAantallen((a) => ({ ...a, [sleutel]: n }));
  }

  const alert = resultaat ? (STATUS_ALERT[resultaat.status] ?? STATUS_ALERT.leeg) : null;

  return (
    <div className="view on">
      <div className="ph">
        <div>
          <h1>BKR Calculator</h1>
          <p>Besluit kwaliteit kinderopvang — vereiste beroepskracht-kindratio</p>
        </div>
      </div>

      <div className="alert alert-info">
        <i className="ti ti-book" />
        <span>
          <strong>Normen:</strong> 0-1jr → max 3 · 1-2jr → max 5 · 2-3jr → max 8 · 3-4jr → max 8 per begeleider.{' '}
          <strong>3-uursregeling:</strong> max 3 uur per dag afwijken, altijd minimaal 1 begeleider aanwezig.
        </span>
      </div>

      <div className="card">
        <div className="card-b">
          <div className="bkr-inputs">
            {BANDEN.map((b) => (
              <div className="bkr-ig" key={b.sleutel}>
                <label>
                  {b.label} {b.emoji}
                </label>
                <input
                  type="number"
                  min={0}
                  value={aantallen[b.sleutel]}
                  onChange={(e) => zet(b.sleutel, e.target.value)}
                />
                <div className="bkr-hint">{b.hint}</div>
              </div>
            ))}
          </div>

          <div className="bkr-result">
            <div>
              <div className="bkr-big">{resultaat?.vereisteHoeveelheidPmers ?? 0}</div>
              <div style={{ fontSize: 11, color: 'var(--text3)' }}>Vereiste begeleiders</div>
            </div>
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 11, color: 'var(--text3)', lineHeight: 1.9 }}>
                {resultaat && resultaat.onderdelen.length > 0 ? (
                  resultaat.onderdelen.map((o) => (
                    <div key={o.label}>
                      {o.label}: {o.aantalKinderen} kinderen → {o.vereistePmers} begeleider(s)
                    </div>
                  ))
                ) : (
                  <div>Geen kinderen opgegeven.</div>
                )}
              </div>
            </div>
          </div>

          <div style={{ marginTop: 12, display: 'flex', alignItems: 'center', gap: 14, flexWrap: 'wrap' }}>
            <div>
              <div
                style={{
                  fontSize: 9,
                  fontWeight: 700,
                  color: 'var(--text3)',
                  textTransform: 'uppercase',
                  marginBottom: 4,
                }}
              >
                Aanwezig personeel
              </div>
              <input
                type="number"
                min={0}
                value={aanwezig}
                onChange={(e) => setAanwezig(Math.max(0, parseInt(e.target.value, 10) || 0))}
                style={{
                  width: 65,
                  padding: 7,
                  background: 'var(--surface2)',
                  border: '1.5px solid var(--border2)',
                  borderRadius: 7,
                  fontSize: 19,
                  fontWeight: 800,
                  textAlign: 'center',
                  color: 'var(--text)',
                  outline: 'none',
                }}
              />
            </div>
            <div style={{ flex: 1, minWidth: 220 }}>
              {resultaat && alert && resultaat.status !== 'leeg' && (
                <div className={`alert ${alert.klasse}`} style={{ margin: 0 }}>
                  <i className={`ti ${alert.icoon}`} />
                  <span>{resultaat.melding}</span>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
