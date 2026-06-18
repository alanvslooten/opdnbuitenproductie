import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMeldingen, useMeldingMutaties } from '../api/queries';
import {
  MELDING_SOORT_ICOON,
  MELDING_SOORT_LABEL,
  MeldingStatus,
  type MeldingDto,
} from '../types';

/** Tijdstip compact tonen ("15 jun 14:32"). */
function tijdstip(iso: string): string {
  return new Date(iso).toLocaleString('nl-NL', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/** Deep-link naar de bron van een melding, indien er een logische plek voor is. */
function bronLink(melding: MeldingDto): string | null {
  switch (melding.bronType) {
    case 'WachtlijstInschrijving':
      return melding.bronId ? `/wachtlijst/${melding.bronId}` : '/wachtlijst';
    case 'Verlofaanvraag':
    case 'Ziekmelding':
      return '/verlof';
    case 'Kind':
      return '/observaties';
    case 'Stamgroep':
      return '/rooster';
    default:
      return null;
  }
}

export function MeldingenPage() {
  const [toonAfgehandeld, setToonAfgehandeld] = useState(false);
  const [alleenToDos, setAlleenToDos] = useState(false);
  const { data, isLoading, error } = useMeldingen(toonAfgehandeld, alleenToDos);
  const { gelezen, allesGelezen, afhandelen } = useMeldingMutaties();

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Actiecentrum</h1>
          <p>Meldingen en openstaande to-do's</p>
        </div>
        <div className="ph-actions" style={{ alignItems: 'center' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, color: 'var(--text2)' }}>
            <input type="checkbox" checked={alleenToDos} onChange={(e) => setAlleenToDos(e.target.checked)} />
            Alleen to-do's
          </label>
          <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, color: 'var(--text2)' }}>
            <input type="checkbox" checked={toonAfgehandeld} onChange={(e) => setToonAfgehandeld(e.target.checked)} />
            Toon afgehandeld
          </label>
          <button onClick={() => allesGelezen.mutate()} className="btn btn-outline btn-sm">
            <i className="ti ti-checks" /> Alles gelezen
          </button>
        </div>
      </div>

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon het actiecentrum niet laden.</p>}

      {data && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {data.map((m) => {
            const link = bronLink(m);
            const afgehandeld = m.status === MeldingStatus.Afgehandeld;
            const ongelezen = m.status === MeldingStatus.Ongelezen;
            return (
              <div
                key={m.id}
                className="card"
                style={{
                  display: 'flex',
                  alignItems: 'flex-start',
                  gap: 12,
                  padding: 14,
                  opacity: afgehandeld ? 0.6 : 1,
                  borderColor: ongelezen ? 'var(--blue)' : undefined,
                  background: ongelezen ? 'var(--blue-xs)' : undefined,
                }}
              >
                <span style={{ fontSize: 18, lineHeight: 1 }} title={MELDING_SOORT_LABEL[m.soort]}>
                  {MELDING_SOORT_ICOON[m.soort]}
                </span>
                <div style={{ minWidth: 0, flex: 1 }}>
                  <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 8 }}>
                    <span style={{ fontWeight: 700, fontSize: 13 }}>{m.titel}</span>
                    {m.vereistActie && !afgehandeld && <span className="badge b-gold">to-do</span>}
                    {ongelezen && <span style={{ width: 8, height: 8, borderRadius: '50%', background: 'var(--blue)' }} title="Ongelezen" />}
                    {afgehandeld && <span className="badge b-gray">afgehandeld</span>}
                  </div>
                  <p style={{ fontSize: 12, color: 'var(--text2)', marginTop: 2 }}>{m.tekst}</p>
                  <p style={{ fontSize: 10, color: 'var(--text3)', marginTop: 2 }}>{tijdstip(m.aangemaaktOp)}</p>
                </div>
                <div style={{ display: 'flex', flexShrink: 0, flexDirection: 'column', alignItems: 'flex-end', gap: 6 }}>
                  {link && (
                    <Link to={link} className="btn btn-outline btn-xs">
                      Bekijk
                    </Link>
                  )}
                  {ongelezen && (
                    <button onClick={() => gelezen.mutate(m.id)} className="btn btn-outline btn-xs">
                      Gelezen
                    </button>
                  )}
                  {m.vereistActie && !afgehandeld && (
                    <button onClick={() => afhandelen.mutate(m.id)} className="btn btn-green btn-xs">
                      Afhandelen
                    </button>
                  )}
                </div>
              </div>
            );
          })}
          {data.length === 0 && (
            <div className="empty">
              <i className="ti ti-inbox" />
              <p>{alleenToDos ? "Geen openstaande to-do's." : 'Geen meldingen.'}</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
