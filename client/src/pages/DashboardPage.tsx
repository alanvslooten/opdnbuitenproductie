import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useDashboard } from '../api/queries';
import { useAuth } from '../auth/AuthContext';
import { korteDatum } from '../datum';
import { Capabilities, MELDING_SOORT_ICOON } from '../types';

type Variant = 'primary' | 'violet' | 'blue' | 'green' | 'pink' | 'gold';

function StatKaart({
  variant,
  icon,
  num,
  label,
  sub,
  naar,
}: {
  variant: Variant;
  icon: string;
  num: ReactNode;
  label: string;
  sub?: ReactNode;
  naar?: string;
}) {
  const inhoud = (
    <>
      <div className="sc-icon">
        <i className={`ti ${icon}`} />
      </div>
      <div className="sc-num">{num}</div>
      <div className="sc-lbl">{label}</div>
      {sub && <div className="sc-sub">{sub}</div>}
    </>
  );
  const klasse = `sc v-${variant}`;
  return naar ? (
    <Link to={naar} className={klasse}>
      {inhoud}
    </Link>
  ) : (
    <div className={klasse}>{inhoud}</div>
  );
}

function tijdstip(iso: string): string {
  return new Date(iso).toLocaleString('nl-NL', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function begroeting(): string {
  const u = new Date().getHours();
  if (u < 6) return 'Goedenacht';
  if (u < 12) return 'Goedemorgen';
  if (u < 18) return 'Goedemiddag';
  return 'Goedenavond';
}

export function DashboardPage() {
  const { data, isLoading, error } = useDashboard();
  const { heeft } = useAuth();

  if (isLoading)
    return (
      <div className="loader">
        <i className="ti ti-loader" />
        Laden…
      </div>
    );
  if (error || !data) return <p style={{ color: 'var(--rose)' }}>Kon het dashboard niet laden.</p>;

  const bkr = data.bkr;
  const bkrKlasse = !bkr.isOpvangdag ? 'b-gray' : bkr.overschrijding ? 'b-red' : 'b-green';
  const bkrTekst = !bkr.isOpvangdag
    ? 'Geen opvangdag vandaag'
    : bkr.overschrijding
      ? 'Let op — BKR-signaal in een groep'
      : 'Alle groepen binnen de BKR';

  return (
    <div className="view">
      {/* Begroeting + datum + BKR */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginBottom: 16,
          flexWrap: 'wrap',
          gap: 8,
        }}
      >
        <div>
          <h1 style={{ fontSize: 18, fontWeight: 700, letterSpacing: '-.3px' }}>{begroeting()}</h1>
          <p style={{ fontSize: 11, color: 'var(--text3)', marginTop: 2 }}>{korteDatum(data.datum)}</p>
        </div>
        <div
          className={`badge ${bkrKlasse}`}
          style={{ padding: '6px 12px', fontSize: 11 }}
          title={`${bkr.aantalGroepenInOrde} / ${bkr.aantalGroepen} groepen in orde`}
        >
          <i className="ti ti-shield-check" />
          {bkrTekst} · {bkr.aantalGroepenInOrde}/{bkr.aantalGroepen}
        </div>
      </div>

      {/* Snelle acties */}
      <div className="quick-actions">
        {heeft(Capabilities.KinderenBeheren) && (
          <Link to="/kinderen/nieuw" className="qa-btn">
            <i className="ti ti-user-plus" />
            <div>
              <div>Kind toevoegen</div>
              <span>Nieuw kind inschrijven</span>
            </div>
          </Link>
        )}
        {heeft(Capabilities.ObservatiesVersturen) && (
          <Link to="/observaties" className="qa-btn">
            <i className="ti ti-clipboard-check" />
            <div>
              <div>Observaties</div>
              <span>Bevindingen bijhouden</span>
            </div>
          </Link>
        )}
        {heeft(Capabilities.WachtlijstBeheren) && (
          <Link to="/wachtlijst/nieuw" className="qa-btn">
            <i className="ti ti-list-numbers" />
            <div>
              <div>Wachtlijst</div>
              <span>Aanmelding toevoegen</span>
            </div>
          </Link>
        )}
      </div>

      {/* Stat-kaarten */}
      <div className="stats">
        <StatKaart variant="primary" icon="ti-mood-kid" num={data.totaalKinderenVandaag} label="Kinderen vandaag" />
        <StatKaart
          variant="blue"
          icon="ti-users"
          num={data.totaalMedewerkersVandaag}
          label="Medewerkers aanwezig"
          naar="/rooster"
          sub={
            !data.roosterVerstuurd ? (
              <span style={{ color: 'var(--amber)' }}>
                <i className="ti ti-alert-triangle" /> rooster niet verstuurd
              </span>
            ) : undefined
          }
        />
        <StatKaart
          variant="gold"
          icon="ti-list-numbers"
          num={data.wachtlijst.aantalWachtend}
          label="Wachtend"
          naar="/wachtlijst"
        />
        <StatKaart
          variant="pink"
          icon="ti-clipboard-check"
          num={data.observaties.overschreden}
          label="Observaties overschreden"
          naar="/observaties"
          sub={<span>{data.observaties.binnenkort} binnenkort</span>}
        />
        <StatKaart
          variant="violet"
          icon="ti-arrow-up-right"
          num={data.aantalKinderenBinnenkortVier}
          label="Binnenkort 4 (uitstroom)"
        />
        <StatKaart
          variant="gold"
          icon="ti-bell"
          num={data.actiecentrum.openToDos}
          label="Open to-do's"
          naar="/meldingen"
          sub={<span>{data.actiecentrum.ongelezenMeldingen} ongelezen</span>}
        />
      </div>

      {/* Groepen + activiteit */}
      <div className="dash-grid">
        <div className="card">
          <div className="card-h">
            <h3>
              <i className="ti ti-layout-grid" style={{ color: 'var(--primary)' }} />
              Groepen vandaag
            </h3>
          </div>
          <div className="tbl-wrap" style={{ border: 'none', boxShadow: 'none' }}>
            <table className="tbl">
              <thead>
                <tr>
                  <th>Groep</th>
                  <th>Kinderen</th>
                  <th title="Vereiste begeleiders">Nodig</th>
                  <th title="Ingeplande begeleiders">Ingepland</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {data.groepen.map((g) => (
                  <tr key={g.stamgroepId}>
                    <td style={{ fontWeight: 600 }}>{g.naam}</td>
                    <td>{g.aantalKinderen}</td>
                    <td>{g.vereistePmers ?? '—'}</td>
                    <td>{g.ingeplandePmers}</td>
                    <td>
                      {g.bovenMaximum ? (
                        <span className="badge b-red">boven max</span>
                      ) : g.onderbezet ? (
                        <span className="badge b-gold">onderbezet</span>
                      ) : (
                        <span className="badge b-green">ok</span>
                      )}
                    </td>
                  </tr>
                ))}
                {data.groepen.length === 0 && (
                  <tr>
                    <td colSpan={5} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                      Geen groepen.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card">
          <div className="card-h">
            <h3>
              <i className="ti ti-timeline" style={{ color: 'var(--pink)' }} />
              Recente activiteit
            </h3>
            <Link to="/meldingen" className="btn btn-outline btn-sm">
              Actiecentrum
            </Link>
          </div>
          <div className="card-b">
            {data.recenteActiviteit.map((a) => (
              <div key={a.id} className="tl-item">
                <span style={{ fontSize: 16, lineHeight: 1 }}>{MELDING_SOORT_ICOON[a.soort]}</span>
                <div className="tl-body">
                  <h5>{a.titel}</h5>
                  <p style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{a.tekst}</p>
                </div>
                <span style={{ flexShrink: 0, fontSize: 10, color: 'var(--text3)' }}>{tijdstip(a.op)}</span>
              </div>
            ))}
            {data.recenteActiviteit.length === 0 && (
              <div className="empty">
                <i className="ti ti-inbox" />
                <p>Nog geen activiteit.</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
