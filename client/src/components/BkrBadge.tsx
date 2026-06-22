import type { BkrDagDto } from '../types';

/** Toont de BKR-uitkomst van een dag als gekleurde badge. */
export function BkrBadge({ bkr }: { bkr: BkrDagDto }) {
  if (bkr.aantalKinderen === 0) {
    return <span style={{ fontSize: 11, color: 'var(--text3)' }}>—</span>;
  }

  if (bkr.overschrijdt) {
    return (
      <span className="badge b-red" title={bkr.melding ?? 'Groep boven het wettelijk maximum'}>
        <i className="ti ti-alert-triangle" /> vol ({bkr.aantalKinderen})
      </span>
    );
  }

  return (
    <span
      className="badge b-green"
      title={`${bkr.aantalKinderen} kinderen · ${bkr.vereisteHoeveelheidPmers} begeleider(s) vereist`}
    >
      {bkr.aantalKinderen}k · {bkr.vereisteHoeveelheidPmers} b
    </span>
  );
}
