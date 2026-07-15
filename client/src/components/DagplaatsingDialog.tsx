import { useState } from 'react';
import { useDagplaatsingMutaties } from '../api/queries';
import { ApiFout } from '../api/client';
import {
  DagplaatsingSoort,
  DAGPLAATSING_SOORT_LABEL,
  type DagplaatsingDto,
  type StamgroepDto,
} from '../types';
import { korteDatum } from '../datum';

const AFWEZIG = 'afwezig';

/**
 * Zet, wijzigt of verwijdert een dagafwijking (dagplaatsing) voor één kind op één dag:
 * het kind incidenteel op een andere groep, een ruildag/extra dag, of afwezig. De vaste
 * thuisgroep verandert niet; alleen deze dag wijkt af. Bij opslaan/verwijderen verversen
 * de planning- en BKR-weergaven vanzelf (query-invalidatie in de mutatie-hook).
 */
export function DagplaatsingDialog({
  kindId,
  kindNaam,
  datum,
  huidigeGroepId,
  bestaand,
  stamgroepen,
  onSluit,
}: {
  kindId: string;
  kindNaam: string;
  datum: string;
  huidigeGroepId: string;
  bestaand: DagplaatsingDto | undefined;
  stamgroepen: StamgroepDto[];
  onSluit: () => void;
}) {
  // Beginwaarde: de bestaande afwijking, anders de groep waar het kind nu staat.
  const beginGroep = bestaand ? (bestaand.stamgroepId ?? AFWEZIG) : huidigeGroepId;
  const [groepKeuze, setGroepKeuze] = useState<string>(beginGroep);
  const [soort, setSoort] = useState<number>(bestaand?.soort ?? DagplaatsingSoort.Incidenteel);
  const [notitie, setNotitie] = useState(bestaand?.notitie ?? '');

  const { zet, verwijder } = useDagplaatsingMutaties();
  const afwezig = groepKeuze === AFWEZIG;
  const fout = zet.error ?? verwijder.error;
  const bezig = zet.isPending || verwijder.isPending;

  function opslaan() {
    zet.mutate(
      {
        kindId,
        datum,
        stamgroepId: afwezig ? null : groepKeuze,
        // Afwezig dwingt de soort af; anders de gekozen soort.
        soort: afwezig ? DagplaatsingSoort.Afwezig : soort,
        notitie: notitie.trim() ? notitie.trim() : null,
      },
      { onSuccess: onSluit },
    );
  }

  function afwijkingWeghalen() {
    if (!bestaand) return;
    verwijder.mutate(bestaand.id, { onSuccess: onSluit });
  }

  return (
    <div className="overlay on" onClick={onSluit}>
      <div className="modal" style={{ maxWidth: 440 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-h">
          <h2>
            <i className="ti ti-calendar-cog" /> Dagplaatsing
          </h2>
          <button type="button" className="xbtn" onClick={onSluit}>
            <i className="ti ti-x" />
          </button>
        </div>
        <div className="modal-b">
          <p style={{ fontSize: 13, marginBottom: 12 }}>
            <strong>{kindNaam}</strong> op <strong>{korteDatum(datum)}</strong>. De vaste thuisgroep
            verandert niet — alleen deze dag wijkt af.
          </p>

          {fout instanceof ApiFout && (
            <div className="alert alert-bad" style={{ marginBottom: 12 }}>
              <i className="ti ti-alert-circle" />
              <span>{fout.message}</span>
            </div>
          )}

          <div className="fld">
            <label>Deze dag op</label>
            <select value={groepKeuze} onChange={(e) => setGroepKeuze(e.target.value)}>
              {stamgroepen.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.naam}
                </option>
              ))}
              <option value={AFWEZIG}>— Afwezig —</option>
            </select>
          </div>

          {!afwezig && (
            <div className="fld">
              <label>Soort</label>
              <select value={soort} onChange={(e) => setSoort(Number(e.target.value))}>
                <option value={DagplaatsingSoort.Incidenteel}>
                  {DAGPLAATSING_SOORT_LABEL[DagplaatsingSoort.Incidenteel]}
                </option>
                <option value={DagplaatsingSoort.Ruildag}>{DAGPLAATSING_SOORT_LABEL[DagplaatsingSoort.Ruildag]}</option>
                <option value={DagplaatsingSoort.ExtraDag}>{DAGPLAATSING_SOORT_LABEL[DagplaatsingSoort.ExtraDag]}</option>
              </select>
            </div>
          )}

          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Notitie (optioneel)</label>
            <input
              type="text"
              maxLength={500}
              value={notitie}
              placeholder="bijv. geruild met dinsdag"
              onChange={(e) => setNotitie(e.target.value)}
            />
          </div>
        </div>
        <div className="modal-f" style={{ position: 'static', justifyContent: 'space-between' }}>
          <div>
            {bestaand && (
              <button type="button" onClick={afwijkingWeghalen} disabled={bezig} className="btn btn-outline btn-sm">
                <i className="ti ti-arrow-back-up" /> Terug naar regulier
              </button>
            )}
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <button type="button" onClick={onSluit} className="btn btn-outline btn-sm">
              Annuleren
            </button>
            <button type="button" onClick={opslaan} disabled={bezig} className="btn btn-primary btn-sm">
              <i className="ti ti-check" /> Opslaan
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
