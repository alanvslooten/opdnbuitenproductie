import { useState } from 'react';
import { useStamgroepen } from '../api/queries';
import { ApiFout } from '../api/client';
import {
  ObservatieStatus,
  openObservatiePdf,
  useObservatieMutaties,
  useObservatieOverzicht,
  type KindObservatieschemaDto,
  type ObservatiemomentDto,
} from '../api/observaties';
import { korteDatum } from '../datum';

const STATUS_STIJL: Record<number, { label: string; klasse: string }> = {
  [ObservatieStatus.NogNietAanDeBeurt]: { label: 'Nog niet', klasse: 'b-gray' },
  [ObservatieStatus.Binnenkort]: { label: 'Binnenkort', klasse: 'chip-need' },
  [ObservatieStatus.Overschreden]: { label: 'Overschreden', klasse: 'chip-over' },
  [ObservatieStatus.Afgerond]: { label: 'Afgerond', klasse: 'chip-done' },
};

const BANNER = 'https://images.unsplash.com/photo-1630476504743-a4d342f88760?q=80&w=1200&auto=format&fit=crop';

export function ObservatiesPage() {
  const [stamgroepFilter, setStamgroepFilter] = useState('');
  const { data: groepen } = useStamgroepen();
  const { data, isLoading, error } = useObservatieOverzicht(stamgroepFilter || undefined);
  const [fout, setFout] = useState<string | null>(null);

  const groepNaam = (id: string) => groepen?.find((g) => g.id === id)?.naam ?? '—';

  return (
    <div className="view">
      <div className="page-banner">
        <img src={BANNER} alt="" />
        <div className="page-banner-overlay">
          <div className="page-banner-text">
            <h1>
              <i className="ti ti-clipboard-check" /> Observaties
            </h1>
            <p>Elk kind, elk mijlpaalmoment — bijgehouden via de Piramide-PDF</p>
          </div>
        </div>
      </div>

      <div className="ph">
        <div />
        <div className="ph-actions">
          <select
            className="fchip"
            style={{ padding: '7px 12px' }}
            value={stamgroepFilter}
            onChange={(e) => setStamgroepFilter(e.target.value)}
          >
            <option value="">Alle stamgroepen</option>
            {groepen?.map((g) => (
              <option key={g.id} value={g.id}>
                {g.naam}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="alert alert-info">
        <i className="ti ti-info-circle" />
        <span>
          Observatiemomenten volgen uit de geboortedatum (6, 12, 18, 24, 30, 36, 42 maanden en het
          eindmoment op 3 jaar en 10 maanden). Vink af door de PDF uit Piramide te uploaden.
        </span>
      </div>

      {fout && (
        <div className="alert alert-bad">
          <i className="ti ti-alert-circle" />
          <span>{fout}</span>
        </div>
      )}
      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon observaties niet laden.</p>}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        {data?.map((kind) => (
          <KindKaart key={kind.kindId} kind={kind} groepNaam={groepNaam(kind.stamgroepId)} onFout={setFout} />
        ))}
        {data?.length === 0 && (
          <div className="empty">
            <i className="ti ti-mood-empty" />
            <p>Geen kinderen zichtbaar. Als medewerker zie je alleen de kinderen waarvan je mentor bent.</p>
          </div>
        )}
      </div>
    </div>
  );
}

function KindKaart({
  kind,
  groepNaam,
  onFout,
}: {
  kind: KindObservatieschemaDto;
  groepNaam: string;
  onFout: (f: string | null) => void;
}) {
  const { afvinken, versturen, ongedaanMaken } = useObservatieMutaties();
  const bezig = afvinken.isPending || versturen.isPending || ongedaanMaken.isPending;

  async function doe(actie: () => Promise<unknown>, foutmelding: string) {
    onFout(null);
    try {
      await actie();
    } catch (e) {
      onFout(e instanceof ApiFout ? e.message : foutmelding);
    }
  }

  return (
    <div className="card">
      <div className="card-h">
        <h3>
          <i className="ti ti-mood-kid" style={{ color: 'var(--primary)' }} />
          {kind.voornaam} {kind.achternaam}
          <span style={{ fontWeight: 400, color: 'var(--text3)', fontSize: 11 }}>· {groepNaam}</span>
          {kind.wordtBinnenkortVier && (
            <span className="badge b-gold" style={{ marginLeft: 4 }}>
              bijna 4
            </span>
          )}
        </h3>
        <div style={{ display: 'flex', gap: 5, flexWrap: 'wrap' }}>
          {kind.aantalOverschreden > 0 && <span className="chip chip-over">{kind.aantalOverschreden} overschreden</span>}
          {kind.aantalBinnenkort > 0 && <span className="chip chip-need">{kind.aantalBinnenkort} binnenkort</span>}
          <span className="chip chip-done">{kind.aantalAfgerond} afgerond</span>
        </div>
      </div>

      <div className="tbl-wrap" style={{ border: 'none', boxShadow: 'none', borderRadius: 0 }}>
        <table className="tbl">
          <tbody>
            {kind.momenten.map((m) => (
              <MomentRij
                key={m.mijlpaalMaanden}
                kindId={kind.kindId}
                moment={m}
                bezig={bezig}
                onUpload={(file) =>
                  doe(
                    () => afvinken.mutateAsync({ kindId: kind.kindId, mijlpaalMaanden: m.mijlpaalMaanden, bestand: file }),
                    'Upload mislukt.',
                  )
                }
                onVersturen={(obsId) => doe(() => versturen.mutateAsync(obsId), 'Versturen mislukt.')}
                onOngedaan={(obsId) => doe(() => ongedaanMaken.mutateAsync(obsId), 'Ongedaan maken mislukt.')}
                onPdf={(obsId) => doe(() => openObservatiePdf(obsId), 'PDF openen mislukt.')}
              />
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function MomentRij({
  kindId,
  moment,
  bezig,
  onUpload,
  onVersturen,
  onOngedaan,
  onPdf,
}: {
  kindId: string;
  moment: ObservatiemomentDto;
  bezig: boolean;
  onUpload: (file: File) => void;
  onVersturen: (observatieId: string) => void;
  onOngedaan: (observatieId: string) => void;
  onPdf: (observatieId: string) => void;
}) {
  const stijl = STATUS_STIJL[moment.status];
  const obs = moment.observatie;

  return (
    <tr>
      <td>
        <span style={{ fontWeight: 600 }}>{moment.beschrijving}</span>
        {moment.isEindmoment && (
          <span className="badge b-violet" style={{ marginLeft: 8 }}>
            eindobservatie
          </span>
        )}
      </td>
      <td style={{ color: 'var(--text3)' }}>verschuldigd {korteDatum(moment.vervaldatum)}</td>
      <td>
        <span className={`chip ${stijl.klasse}`}>{stijl.label}</span>
      </td>
      <td style={{ textAlign: 'right' }}>
        {obs ? (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 8 }}>
            <button onClick={() => onPdf(obs.id)} className="btn btn-outline btn-xs">
              <i className="ti ti-file-type-pdf" /> PDF
            </button>
            {obs.verzondenOp ? (
              <span style={{ fontSize: 10, color: 'var(--text3)' }}>verstuurd {korteDatum(obs.verzondenOp.slice(0, 10))}</span>
            ) : (
              <button disabled={bezig} onClick={() => onVersturen(obs.id)} className="btn btn-green btn-xs">
                <i className="ti ti-send" /> Versturen
              </button>
            )}
            <button disabled={bezig} onClick={() => onOngedaan(obs.id)} className="btn btn-rose btn-xs">
              <i className="ti ti-arrow-back-up" />
            </button>
          </div>
        ) : (
          <label
            className={`btn btn-primary btn-xs${bezig ? '' : ''}`}
            style={{ cursor: bezig ? 'not-allowed' : 'pointer', opacity: bezig ? 0.5 : 1, pointerEvents: bezig ? 'none' : 'auto' }}
          >
            <i className="ti ti-upload" /> PDF uploaden
            <input
              type="file"
              accept="application/pdf"
              style={{ display: 'none' }}
              data-kind={kindId}
              onChange={(e) => {
                const file = e.target.files?.[0];
                if (file) onUpload(file);
                e.target.value = '';
              }}
            />
          </label>
        )}
      </td>
    </tr>
  );
}
