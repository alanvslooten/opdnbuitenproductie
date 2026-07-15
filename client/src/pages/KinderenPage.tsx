import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useKinderen, useKindMutaties, useMedewerkers, useStamgroepen } from '../api/queries';
import { ApiFout } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { opvangdagenTekst } from '../components/OpvangdagenKiezer';
import { BevestigWachtwoordDialog } from '../components/BevestigWachtwoordDialog';
import { datumNl } from '../datum';
import { Capabilities, Contracttype, type KindDto } from '../types';

export function KinderenPage() {
  const { heeft } = useAuth();
  const magBeheren = heeft(Capabilities.KinderenBeheren);
  const [stamgroepFilter, setStamgroepFilter] = useState<string>('');
  const { data: groepen } = useStamgroepen();
  const { data: medewerkers } = useMedewerkers();
  const { data, isLoading, error } = useKinderen(stamgroepFilter || undefined);
  const { verwijderen } = useKindMutaties();
  const [detail, setDetail] = useState<KindDto | null>(null);
  const [verwijderDoel, setVerwijderDoel] = useState<KindDto | null>(null);
  const [verwijderFout, setVerwijderFout] = useState<string | null>(null);

  const groepNaam = (id: string) => groepen?.find((g) => g.id === id)?.naam ?? '—';
  const mentorNaam = (id: string | null) => {
    const m = medewerkers?.find((x) => x.id === id);
    return m ? `${m.voornaam} ${m.achternaam}` : '—';
  };

  async function bevestigVerwijderen(wachtwoord: string) {
    if (!verwijderDoel) return;
    setVerwijderFout(null);
    try {
      await verwijderen.mutateAsync({ id: verwijderDoel.id, wachtwoord });
      setVerwijderDoel(null);
    } catch (err) {
      setVerwijderFout(err instanceof ApiFout ? err.message : 'Verwijderen mislukt.');
    }
  }

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Kinderen</h1>
          <p>{magBeheren ? 'Inschrijvingen, opvangdagen en contracten' : 'Inzien (alleen-lezen)'}</p>
        </div>
        {magBeheren && (
          <div className="ph-actions">
            <Link to="/kinderen/nieuw" className="btn btn-primary btn-sm">
              <i className="ti ti-user-plus" /> Kind toevoegen
            </Link>
          </div>
        )}
      </div>


      <div className="filters">
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

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon kinderen niet laden.</p>}

      <div className="tbl-wrap">
        <table className="tbl">
          <thead>
            <tr>
              <th>Naam</th>
              <th>Stamgroep</th>
              <th>Opvangdagen</th>
              <th>Contract</th>
              <th>Tot</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {data?.map((k) => (
              <tr key={k.id}>
                <td style={{ fontWeight: 600 }}>
                  <button
                    onClick={() => setDetail(k)}
                    style={{ background: 'none', border: 'none', padding: 0, font: 'inherit', fontWeight: 600, color: 'var(--blue)', cursor: 'pointer' }}
                    title="Details bekijken"
                  >
                    {k.voornaam} {k.achternaam}
                  </button>
                  {k.wordtBinnenkortVier && (
                    <span className="badge b-gold" style={{ marginLeft: 8 }} title="Wordt binnenkort 4 en stroomt uit">
                      bijna 4
                    </span>
                  )}
                </td>
                <td>{groepNaam(k.stamgroepId)}</td>
                <td>{opvangdagenTekst(k.gewensteOpvangdagen)}</td>
                <td>{k.contracttype === Contracttype.Weken40 ? '40 wkn' : '49 wkn'}</td>
                <td style={{ color: 'var(--text3)' }}>{datumNl(k.effectieveEinddatum)}</td>
                <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                  {magBeheren ? (
                    <>
                      <Link to={`/kinderen/${k.id}`} className="btn btn-outline btn-xs" style={{ marginRight: 6 }}>
                        <i className="ti ti-pencil" /> Bewerken
                      </Link>
                      <button onClick={() => { setVerwijderFout(null); setVerwijderDoel(k); }} className="btn btn-rose btn-xs">
                        <i className="ti ti-trash" />
                      </button>
                    </>
                  ) : (
                    <button onClick={() => setDetail(k)} className="btn btn-outline btn-xs">
                      <i className="ti ti-eye" /> Bekijken
                    </button>
                  )}
                </td>
              </tr>
            ))}
            {data?.length === 0 && (
              <tr>
                <td colSpan={6} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                  Geen kinderen gevonden.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {detail && (
        <KindDetail
          kind={detail}
          groepNaam={groepNaam(detail.stamgroepId)}
          mentorNaam={mentorNaam(detail.mentorId)}
          magBeheren={magBeheren}
          onSluit={() => setDetail(null)}
        />
      )}

      {verwijderDoel && (
        <BevestigWachtwoordDialog
          titel="Kind verwijderen"
          bericht={`Weet je zeker dat je ${verwijderDoel.voornaam} ${verwijderDoel.achternaam} wilt verwijderen? Dit kan niet ongedaan worden gemaakt. Bevestig met je wachtwoord.`}
          bezig={verwijderen.isPending}
          fout={verwijderFout}
          onBevestig={bevestigVerwijderen}
          onSluit={() => setVerwijderDoel(null)}
        />
      )}
    </div>
  );
}

function KindDetail({
  kind,
  groepNaam,
  mentorNaam,
  magBeheren,
  onSluit,
}: {
  kind: KindDto;
  groepNaam: string;
  mentorNaam: string;
  magBeheren: boolean;
  onSluit: () => void;
}) {
  return (
    <div className="overlay on" onClick={onSluit}>
      <div className="modal" style={{ maxWidth: 480 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-h">
          <h2>
            <i className="ti ti-mood-kid" style={{ color: 'var(--primary)' }} /> {kind.voornaam} {kind.achternaam}
          </h2>
          <button className="xbtn" onClick={onSluit}>
            <i className="ti ti-x" />
          </button>
        </div>
        <div className="modal-b">
          <div className="g2" style={{ marginBottom: 14 }}>
            <Veld label="Stamgroep" waarde={groepNaam} />
            <Veld label="Mentor" waarde={mentorNaam} />
            <Veld label="Geboortedatum" waarde={datumNl(kind.geboortedatum)} />
            <Veld label="Startdatum" waarde={datumNl(kind.startdatum)} />
            <Veld label="Einddatum" waarde={datumNl(kind.effectieveEinddatum)} />
            <Veld label="Contract" waarde={kind.contracttype === Contracttype.Weken40 ? '40 weken' : '49 weken'} />
            <Veld label="Opvangdagen" waarde={opvangdagenTekst(kind.gewensteOpvangdagen)} />
          </div>
          <h3 style={{ fontSize: 12, fontWeight: 700, marginBottom: 8 }}>
            <i className="ti ti-phone" style={{ color: 'var(--primary)' }} /> Ouders / verzorgers / voogden
          </h3>
          {kind.oudercontacten.length > 0 ? (
            kind.oudercontacten.map((oc, i) => (
              <div key={i} className="g2" style={{ marginBottom: 10 }}>
                <Veld
                  label={i === 0 ? 'Naam (primair)' : 'Naam'}
                  waarde={oc.rol ? `${oc.naam} (${oc.rol})` : oc.naam}
                />
                <Veld label="Telefoon" waarde={oc.telefoon} />
                <Veld label="E-mail" waarde={oc.email} />
              </div>
            ))
          ) : (
            <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen oudercontact vastgelegd.</p>
          )}
        </div>
        {magBeheren && (
          <div className="modal-f" style={{ position: 'static' }}>
            <Link to={`/kinderen/${kind.id}`} className="btn btn-primary btn-sm">
              <i className="ti ti-pencil" /> Bewerken
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}

function Veld({ label, waarde }: { label: string; waarde: string }) {
  return (
    <div>
      <div style={{ fontSize: 9, fontWeight: 700, color: 'var(--text3)', textTransform: 'uppercase', marginBottom: 2 }}>{label}</div>
      <div style={{ fontSize: 13, color: 'var(--text)' }}>{waarde || '—'}</div>
    </div>
  );
}
