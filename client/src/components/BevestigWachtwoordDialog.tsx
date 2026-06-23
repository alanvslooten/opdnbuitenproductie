import { useState, type FormEvent } from 'react';

/**
 * Herbruikbare 2-stapsbevestiging voor kritieke acties (bv. verwijderen): vraagt het
 * wachtwoord van de ingelogde beheerder en roept onBevestig aan. De aanroeper doet de
 * mutatie en kan via `fout` een serverfout tonen.
 */
export function BevestigWachtwoordDialog({
  titel,
  bericht,
  bevestigLabel = 'Definitief verwijderen',
  bezig,
  fout,
  onBevestig,
  onSluit,
}: {
  titel: string;
  bericht: string;
  bevestigLabel?: string;
  bezig: boolean;
  fout: string | null;
  onBevestig: (wachtwoord: string) => void;
  onSluit: () => void;
}) {
  const [wachtwoord, setWachtwoord] = useState('');

  function verstuur(e: FormEvent) {
    e.preventDefault();
    onBevestig(wachtwoord);
  }

  return (
    <div className="overlay on" onClick={onSluit}>
      <form className="modal" style={{ maxWidth: 420 }} onClick={(e) => e.stopPropagation()} onSubmit={verstuur}>
        <div className="modal-h">
          <h2>
            <i className="ti ti-alert-triangle" style={{ color: 'var(--rose)' }} /> {titel}
          </h2>
          <button type="button" className="xbtn" onClick={onSluit}>
            <i className="ti ti-x" />
          </button>
        </div>
        <div className="modal-b">
          <p style={{ fontSize: 13, marginBottom: 12 }}>{bericht}</p>
          {fout && (
            <div className="alert alert-bad" style={{ marginBottom: 12 }}>
              <i className="ti ti-alert-circle" />
              <span>{fout}</span>
            </div>
          )}
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Wachtwoord</label>
            <input type="password" autoFocus value={wachtwoord} onChange={(e) => setWachtwoord(e.target.value)} required />
          </div>
        </div>
        <div className="modal-f" style={{ position: 'static' }}>
          <button type="button" onClick={onSluit} className="btn btn-outline btn-sm">
            Annuleren
          </button>
          <button type="submit" disabled={bezig} className="btn btn-rose btn-sm">
            <i className="ti ti-trash" /> {bevestigLabel}
          </button>
        </div>
      </form>
    </div>
  );
}
