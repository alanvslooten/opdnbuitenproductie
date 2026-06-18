import { useEffect, useMemo, useState } from 'react';
import { useStamgroepen, useVoorstelAnalyse, useVoorstelMutaties } from '../api/queries';
import { ApiFout } from '../api/client';
import { korteDatum } from '../datum';
import {
  LEEFTIJDSGROEP_LABEL,
  WEEKDAGEN,
  type VoorstelDagAnalyseDto,
  type WachtlijstInschrijvingDto,
} from '../types';

/**
 * De voorstel-pop-up: eerst een controle-overzicht (beschikbare plekken, BKR-impact
 * via de Domain-calculator, groepsgrootte-check) en per dag een door de planner
 * invulbare startdatum. Pas daarna "Verstuur voorstel". Een subset van de openstaande
 * dagen kiezen maakt het een deelvoorstel; de overige dagen blijven op de wachtlijst.
 */
export function VoorstelDialog({
  inschrijving,
  onClose,
}: {
  inschrijving: WachtlijstInschrijvingDto;
  onClose: () => void;
}) {
  const { data: groepen } = useStamgroepen();
  const [stamgroepId, setStamgroepId] = useState<string | undefined>(
    inschrijving.gewensteStamgroepId ?? undefined,
  );
  const [startdatum, setStartdatum] = useState(inschrijving.gewensteStartdatum);
  const [gekozenDagen, setGekozenDagen] = useState<number>(inschrijving.openstaandeDagen);
  const [perDagDatum, setPerDagDatum] = useState<Record<number, string>>({});
  const [notitie, setNotitie] = useState('');
  const [foutmelding, setFoutmelding] = useState<string | null>(null);

  // Default de stamgroep naar de eerste beschikbare als er geen voorkeur is.
  useEffect(() => {
    if (!stamgroepId && groepen && groepen.length > 0) setStamgroepId(groepen[0].id);
  }, [groepen, stamgroepId]);

  const { data: analyse, isLoading, error } = useVoorstelAnalyse(inschrijving.id, stamgroepId, startdatum);

  // Seed de per-dag-datums zodra de analyse binnen is (default: eerste vrije dag of peildatum).
  useEffect(() => {
    if (!analyse) return;
    setPerDagDatum((huidig) => {
      const next = { ...huidig };
      for (const dag of analyse.dagen) {
        if (!next[dag.weekdag]) next[dag.weekdag] = dag.eersteVrijeDatum ?? dag.peildatum;
      }
      return next;
    });
  }, [analyse]);

  const { versturen } = useVoorstelMutaties(inschrijving.id);

  const isDeelvoorstel = analyse ? gekozenDagen !== analyse.openstaandeDagen : false;
  const aantalGekozen = WEEKDAGEN.filter((d) => (gekozenDagen & d.vlag) !== 0).length;

  function wisselDag(vlag: number) {
    setGekozenDagen((d) => (d & vlag ? d & ~vlag : d | vlag));
  }

  async function verstuur() {
    setFoutmelding(null);
    if (!stamgroepId || aantalGekozen === 0) {
      setFoutmelding('Kies een stamgroep en minstens één dag.');
      return;
    }
    const dagData = WEEKDAGEN.filter((d) => (gekozenDagen & d.vlag) !== 0).map((d) => ({
      weekdag: d.vlag,
      voorgesteldeDatum: perDagDatum[d.vlag] ?? startdatum,
    }));
    try {
      await versturen.mutateAsync({
        stamgroepId,
        voorgesteldeStartdatum: startdatum,
        dagen: gekozenDagen,
        dagData,
        notitie: notitie.trim() || null,
      });
      onClose();
    } catch (e) {
      setFoutmelding(e instanceof ApiFout ? e.message : 'Versturen mislukt.');
    }
  }

  const groepGekozen = useMemo(() => groepen?.find((g) => g.id === stamgroepId), [groepen, stamgroepId]);

  return (
    <div className="overlay on" onClick={onClose}>
      <div className="modal" style={{ maxWidth: 760 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-h">
          <h2>
            <i className="ti ti-send" /> Voorstel voor {inschrijving.voornaam} {inschrijving.achternaam}
          </h2>
          <button className="xbtn" onClick={onClose} aria-label="Sluiten">
            <i className="ti ti-x" />
          </button>
        </div>

        <div className="modal-b" style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          {/* Aanvraag ouder */}
          <section
            style={{ background: 'var(--surface2)', border: '1px solid var(--border)', borderRadius: 'var(--r-sm)', padding: 12 }}
          >
            <h3 style={{ fontSize: 12, fontWeight: 700, marginBottom: 6, color: 'var(--text2)' }}>Aanvraag ouder</h3>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: '4px 16px', fontSize: 11, color: 'var(--text2)' }}>
              <div>
                <span style={{ color: 'var(--text3)' }}>Gewenste start:</span> {korteDatum(inschrijving.gewensteStartdatum)}
              </div>
              <div>
                <span style={{ color: 'var(--text3)' }}>Gewenste dagen:</span>{' '}
                {WEEKDAGEN.filter((d) => (inschrijving.gewensteOpvangdagen & d.vlag) !== 0).map((d) => d.korte).join(', ')}
              </div>
              <div>
                <span style={{ color: 'var(--text3)' }}>Contract:</span> {inschrijving.contracttype} weken
              </div>
            </div>
          </section>

          {/* Keuzes: stamgroep + startdatum */}
          <section className="frow">
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Stamgroep</label>
              <select value={stamgroepId ?? ''} onChange={(e) => setStamgroepId(e.target.value)}>
                {groepen?.map((g) => (
                  <option key={g.id} value={g.id}>
                    {g.naam} (max {g.maxKinderen})
                  </option>
                ))}
              </select>
            </div>
            <div className="fld" style={{ marginBottom: 0 }}>
              <label>Reken vanaf (peildatum)</label>
              <input type="date" value={startdatum} onChange={(e) => setStartdatum(e.target.value)} />
            </div>
          </section>

          {/* Groepsgrootte-check */}
          {analyse && (
            <div className={`alert ${analyse.groepBlijftOnderMax ? 'alert-ok' : 'alert-bad'}`} style={{ marginBottom: 0 }}>
              <i className={`ti ${analyse.groepBlijftOnderMax ? 'ti-circle-check' : 'ti-alert-triangle'}`} />
              <span>
                Groepsgrootte: {analyse.aantalGeplaatstNu}/{analyse.maxKinderen} geplaatst —{' '}
                {analyse.groepBlijftOnderMax
                  ? `er is plaats voor dit kind (blijft ≤ ${analyse.maxKinderen}).`
                  : `de groep zit vol; plaatsing zou het maximum overschrijden.`}
                {analyse.kandidaatLeeftijdsgroep !== null && (
                  <span style={{ color: 'var(--text3)', marginLeft: 4 }}>
                    Leeftijd op start: {LEEFTIJDSGROEP_LABEL[analyse.kandidaatLeeftijdsgroep]}.
                  </span>
                )}
              </span>
            </div>
          )}

          {analyse?.kandidaatBuitenOpvangleeftijd && (
            <div className="alert alert-warn" style={{ marginBottom: 0 }}>
              <i className="ti ti-alert-triangle" />
              <span>Let op: het kind valt op de gekozen startdatum buiten de opvangleeftijd (0-4 jaar).</span>
            </div>
          )}

          {/* Per-dag analyse + keuze */}
          <section>
            <h3 style={{ fontSize: 12, fontWeight: 700, marginBottom: 8, color: 'var(--text2)' }}>
              Per dag: plek &amp; BKR-impact{' '}
              <span style={{ fontWeight: 400, color: 'var(--text3)' }}>(vink de dagen aan die je voorstelt)</span>
            </h3>
            {isLoading && <p style={{ fontSize: 12, color: 'var(--text3)' }}>Analyse laden…</p>}
            {error && <p style={{ fontSize: 12, color: 'var(--rose)' }}>Kon de analyse niet laden.</p>}
            {analyse && analyse.dagen.length === 0 && (
              <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen openstaande dagen meer om voor te stellen.</p>
            )}
            {analyse && analyse.dagen.length > 0 && (
              <div className="tbl-wrap">
                <table className="tbl">
                  <thead>
                    <tr>
                      <th>Voorstellen?</th>
                      <th>Dag</th>
                      <th>Plek</th>
                      <th>BKR (pm'ers)</th>
                      <th>Startdatum dag</th>
                    </tr>
                  </thead>
                  <tbody>
                    {analyse.dagen.map((dag) => (
                      <DagRij
                        key={dag.weekdag}
                        dag={dag}
                        gekozen={(gekozenDagen & dag.weekdag) !== 0}
                        onToggle={() => wisselDag(dag.weekdag)}
                        datum={perDagDatum[dag.weekdag] ?? dag.eersteVrijeDatum ?? dag.peildatum}
                        onDatum={(v) => setPerDagDatum((h) => ({ ...h, [dag.weekdag]: v }))}
                      />
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          {/* Notitie */}
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Notitie (optioneel)</label>
            <textarea
              value={notitie}
              onChange={(e) => setNotitie(e.target.value)}
              rows={2}
              placeholder="Bijv. telefonisch toegelicht aan ouder…"
            />
          </div>

          {isDeelvoorstel && aantalGekozen > 0 && (
            <div className="alert alert-info" style={{ marginBottom: 0 }}>
              <i className="ti ti-info-circle" />
              <span>
                Deelvoorstel: je stelt {aantalGekozen} van de{' '}
                {WEEKDAGEN.filter((d) => (analyse!.openstaandeDagen & d.vlag) !== 0).length} openstaande dagen voor.
                De overige dagen blijven op de wachtlijst staan.
              </span>
            </div>
          )}

          {foutmelding && <p style={{ fontSize: 12, color: 'var(--rose)' }}>{foutmelding}</p>}
        </div>

        <div className="modal-f">
          <button onClick={onClose} className="btn btn-outline btn-sm">
            Annuleren
          </button>
          <button onClick={verstuur} disabled={versturen.isPending || aantalGekozen === 0 || !groepGekozen} className="btn btn-green btn-sm">
            {versturen.isPending ? 'Versturen…' : `Verstuur voorstel (${aantalGekozen} dag${aantalGekozen === 1 ? '' : 'en'})`}
          </button>
        </div>
      </div>
    </div>
  );
}

function DagRij({
  dag,
  gekozen,
  onToggle,
  datum,
  onDatum,
}: {
  dag: VoorstelDagAnalyseDto;
  gekozen: boolean;
  onToggle: () => void;
  datum: string;
  onDatum: (v: string) => void;
}) {
  const dagLabel = WEEKDAGEN.find((d) => d.vlag === dag.weekdag)?.lange ?? '?';
  return (
    <tr>
      <td>
        <input type="checkbox" checked={gekozen} onChange={onToggle} style={{ width: 16, height: 16 }} />
      </td>
      <td style={{ fontWeight: 600 }}>{dagLabel}</td>
      <td>
        {dag.plekVrijOpStart ? (
          <span style={{ color: 'var(--green)' }}>vrij ({dag.aantalAanwezigNu} aanwezig)</span>
        ) : dag.eersteVrijeDatum ? (
          <span style={{ color: 'var(--amber)' }}>vol — vrij vanaf {korteDatum(dag.eersteVrijeDatum)}</span>
        ) : (
          <span style={{ color: 'var(--rose)' }}>geen plek binnen 2 jaar</span>
        )}
      </td>
      <td>
        {dag.bkrOverschrijdtNa ? (
          <span style={{ color: 'var(--rose)' }} title={dag.melding ?? undefined}>
            ⚠ boven max
          </span>
        ) : (
          <span style={{ color: dag.extraPmerNodig ? 'var(--amber)' : 'var(--text2)' }}>
            {dag.vereistePmersNu ?? '—'} → {dag.vereistePmersNa ?? '—'}
            {dag.extraPmerNodig && <span style={{ marginLeft: 4, fontSize: 10 }}>(+1 pm'er)</span>}
          </span>
        )}
      </td>
      <td>
        <input type="date" value={datum} onChange={(e) => onDatum(e.target.value)} disabled={!gekozen} style={{ maxWidth: 150 }} />
      </td>
    </tr>
  );
}
