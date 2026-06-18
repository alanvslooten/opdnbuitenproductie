import { WEEKDAGEN } from '../types';

/** Chip-rij voor de gewenste opvangdagen; werkt met de bit-vlag-waarde. */
export function OpvangdagenKiezer({
  waarde,
  onChange,
}: {
  waarde: number;
  onChange: (nieuw: number) => void;
}) {
  function wissel(vlag: number) {
    onChange(waarde & vlag ? waarde & ~vlag : waarde | vlag);
  }

  return (
    <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
      {WEEKDAGEN.map((d) => {
        const aan = (waarde & d.vlag) !== 0;
        return (
          <button
            key={d.vlag}
            type="button"
            onClick={() => wissel(d.vlag)}
            className={`fchip${aan ? ' on' : ''}`}
          >
            {d.korte}
          </button>
        );
      })}
    </div>
  );
}

/** Leesbare samenvatting van een opvangdagen-vlag, bv. "Ma, Di, Wo". */
export function opvangdagenTekst(waarde: number): string {
  const dagen = WEEKDAGEN.filter((d) => (waarde & d.vlag) !== 0).map((d) => d.korte);
  return dagen.length ? dagen.join(', ') : '—';
}
