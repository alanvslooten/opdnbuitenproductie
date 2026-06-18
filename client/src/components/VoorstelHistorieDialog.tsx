import { useState } from 'react';
import { useVoorstelhistorie, useVoorstelMutaties } from '../api/queries';
import { ApiFout } from '../api/client';
import { korteDatum } from '../datum';
import {
  VOORSTEL_STATUS_LABEL,
  VoorstelStatus,
  WEEKDAGEN,
  type VoorstelDto,
  type WachtlijstInschrijvingDto,
} from '../types';

/** Toont de voorstelhistorie van een inschrijving en laat verstuurde voorstellen accepteren/afwijzen. */
export function VoorstelHistorieDialog({
  inschrijving,
  onClose,
}: {
  inschrijving: WachtlijstInschrijvingDto;
  onClose: () => void;
}) {
  const { data: historie, isLoading } = useVoorstelhistorie(inschrijving.id);
  const { accepteren, afwijzen } = useVoorstelMutaties(inschrijving.id);
  const [fout, setFout] = useState<string | null>(null);

  async function doe(actie: 'accepteren' | 'afwijzen', voorstelId: string) {
    setFout(null);
    try {
      if (actie === 'accepteren') await accepteren.mutateAsync(voorstelId);
      else await afwijzen.mutateAsync(voorstelId);
    } catch (e) {
      setFout(e instanceof ApiFout ? e.message : 'Actie mislukt.');
    }
  }

  const bezig = accepteren.isPending || afwijzen.isPending;

  return (
    <div className="overlay on" onClick={onClose}>
      <div className="modal" style={{ maxWidth: 620 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-h">
          <h2>
            <i className="ti ti-history" /> Voorstelhistorie — {inschrijving.voornaam} {inschrijving.achternaam}
          </h2>
          <button className="xbtn" onClick={onClose} aria-label="Sluiten">
            <i className="ti ti-x" />
          </button>
        </div>

        <div className="modal-b" style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {fout && <p style={{ fontSize: 12, color: 'var(--rose)' }}>{fout}</p>}
          {isLoading && <p style={{ fontSize: 12, color: 'var(--text3)' }}>Laden…</p>}
          {historie && historie.length === 0 && (
            <p style={{ fontSize: 12, color: 'var(--text3)' }}>Nog geen voorstellen verstuurd.</p>
          )}
          {historie?.map((v) => (
            <VoorstelKaart key={v.id} voorstel={v} bezig={bezig} onActie={doe} />
          ))}
        </div>

        <div className="modal-f">
          <button onClick={onClose} className="btn btn-outline btn-sm">
            Sluiten
          </button>
        </div>
      </div>
    </div>
  );
}

function VoorstelKaart({
  voorstel,
  bezig,
  onActie,
}: {
  voorstel: VoorstelDto;
  bezig: boolean;
  onActie: (actie: 'accepteren' | 'afwijzen', voorstelId: string) => void;
}) {
  const kleur =
    voorstel.status === VoorstelStatus.Geaccepteerd
      ? 'b-green'
      : voorstel.status === VoorstelStatus.Afgewezen
        ? 'b-red'
        : voorstel.status === VoorstelStatus.Ingetrokken
          ? 'b-gray'
          : 'b-primary';

  return (
    <div style={{ border: '1px solid var(--border)', borderRadius: 'var(--r-sm)', padding: 12, fontSize: 12 }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <span className={`badge ${kleur}`}>{VOORSTEL_STATUS_LABEL[voorstel.status]}</span>
          {voorstel.isDeelvoorstel && <span className="badge b-gold">deelvoorstel</span>}
        </div>
        <span style={{ fontSize: 10, color: 'var(--text3)' }}>verstuurd {korteDatum(voorstel.verstuurdOp.slice(0, 10))}</span>
      </div>

      <ul style={{ listStyle: 'none', marginBottom: 8, color: 'var(--text2)' }}>
        {voorstel.dagen.map((d) => (
          <li key={d.weekdag}>
            {WEEKDAGEN.find((w) => w.vlag === d.weekdag)?.lange ?? '?'}: start {korteDatum(d.voorgesteldeDatum)}
          </li>
        ))}
      </ul>
      {voorstel.notitie && <p style={{ marginBottom: 8, fontSize: 11, fontStyle: 'italic', color: 'var(--text3)' }}>"{voorstel.notitie}"</p>}

      {voorstel.status === VoorstelStatus.Verstuurd && (
        <div style={{ display: 'flex', gap: 8 }}>
          <button disabled={bezig} onClick={() => onActie('accepteren', voorstel.id)} className="btn btn-green btn-xs">
            <i className="ti ti-check" /> Geaccepteerd
          </button>
          <button disabled={bezig} onClick={() => onActie('afwijzen', voorstel.id)} className="btn btn-outline btn-xs">
            Afgewezen
          </button>
        </div>
      )}
    </div>
  );
}
