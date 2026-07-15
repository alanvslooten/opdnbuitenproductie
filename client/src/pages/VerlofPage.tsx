import { useState, type FormEvent } from 'react';
import {
  useMedewerkers,
  useVerlof,
  useVerlofsaldo,
  useVerlofMutaties,
  useZiekmeldingen,
  useZiekmeldingMutaties,
} from '../api/queries';
import { ApiFout } from '../api/client';
import {
  VERLOFCATEGORIE_LABEL,
  VERLOFSTATUS_LABEL,
  VerlofStatus,
  type VerlofaanvraagDto,
} from '../types';
import { korteDatum, vandaagIso } from '../datum';

const STATUS_CHIP = ['chip-need', 'chip-done', 'b-gray'];

export function VerlofPage() {
  const { data: medewerkers } = useMedewerkers();
  const [medewerkerId, setMedewerkerId] = useState('');
  const [statusFilter, setStatusFilter] = useState<number | ''>('');
  const [periode, setPeriode] = useState<'aankomend' | 'verleden'>('aankomend');
  const [fout, setFout] = useState<string | null>(null);

  const { data: saldi } = useVerlofsaldo(medewerkerId || undefined);
  const { data: aanvragen } = useVerlof(statusFilter === '' ? undefined : statusFilter, medewerkerId || undefined);
  const { data: ziekmeldingen } = useZiekmeldingen(medewerkerId || undefined);
  const verlof = useVerlofMutaties();
  const ziekte = useZiekmeldingMutaties();

  const [aanvraag, setAanvraag] = useState({ begindatum: vandaagIso(), einddatum: vandaagIso(), aantalUren: 8, categorie: 0, reden: '' });
  const [saldoForm, setSaldoForm] = useState({ categorie: 0, toegekendeUren: 0, vervaldatum: '' });
  const [ziek, setZiek] = useState({ begindatum: vandaagIso(), einddatum: '' });

  // Aankomend vs. verleden: verstreken aanvragen verdwijnen niet, maar schuiven naar
  // het 'verleden'-tabblad; het actieve overzicht toont standaard alleen aankomend.
  const vandaag = vandaagIso();
  const zichtbareAanvragen = (aanvragen ?? []).filter((a) =>
    periode === 'aankomend' ? a.einddatum.slice(0, 10) >= vandaag : a.einddatum.slice(0, 10) < vandaag,
  );

  async function wrap(fn: () => Promise<unknown>, melding: string) {
    setFout(null);
    try {
      await fn();
    } catch (err) {
      setFout(err instanceof ApiFout ? err.message : melding);
    }
  }

  async function dienAanvraagIn(e: FormEvent) {
    e.preventDefault();
    if (!medewerkerId) {
      setFout('Kies eerst een medewerker.');
      return;
    }
    await wrap(
      () =>
        verlof.aanvragen.mutateAsync({
          medewerkerId,
          begindatum: aanvraag.begindatum,
          einddatum: aanvraag.einddatum,
          aantalUren: aanvraag.aantalUren,
          categorie: aanvraag.categorie,
          reden: aanvraag.reden || null,
        }),
      'Aanvragen mislukt.',
    );
  }

  async function stelSaldoIn(e: FormEvent) {
    e.preventDefault();
    if (!medewerkerId) {
      setFout('Kies eerst een medewerker.');
      return;
    }
    await wrap(
      () =>
        verlof.saldoInstellen.mutateAsync({
          medewerkerId,
          categorie: saldoForm.categorie,
          toegekendeUren: saldoForm.toegekendeUren,
          vervaldatum: saldoForm.vervaldatum || null,
        }),
      'Saldo instellen mislukt.',
    );
  }

  async function meldZiek(e: FormEvent) {
    e.preventDefault();
    if (!medewerkerId) {
      setFout('Kies eerst een medewerker.');
      return;
    }
    await wrap(
      () => ziekte.registreren.mutateAsync({ medewerkerId, begindatum: ziek.begindatum, einddatum: ziek.einddatum || null }),
      'Ziekmelden mislukt.',
    );
  }

  function afkeurMet(a: VerlofaanvraagDto) {
    const notitie = prompt('Reden van afkeuring (optioneel):') ?? null;
    void wrap(() => verlof.afkeuren.mutateAsync({ id: a.id, notitie }), 'Afkeuren mislukt.');
  }

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Verlof &amp; ziekte</h1>
          <p>Saldi, aanvragen en ziekmeldingen per medewerker</p>
        </div>
      </div>

      {fout && (
        <div className="alert alert-bad">
          <i className="ti ti-alert-circle" />
          <span>{fout}</span>
        </div>
      )}

      <div className="fld" style={{ maxWidth: 320 }}>
        <label>Medewerker</label>
        <select value={medewerkerId} onChange={(e) => setMedewerkerId(e.target.value)}>
          <option value="">— kies medewerker —</option>
          {medewerkers?.map((m) => (
            <option key={m.id} value={m.id}>
              {m.voornaam} {m.achternaam}
            </option>
          ))}
        </select>
      </div>

      {medewerkerId && (
        <>
          {/* Saldo */}
          <div className="card" style={{ marginBottom: 16 }}>
            <div className="card-h">
              <h3>
                <i className="ti ti-wallet" style={{ color: 'var(--green)' }} /> Verlofsaldo
              </h3>
            </div>
            <div className="card-b">
              <div className="g2" style={{ marginBottom: 12 }}>
                {saldi?.map((s) => (
                  <div key={s.categorie} style={{ border: '1px solid var(--border)', borderRadius: 'var(--r-sm)', padding: 12, fontSize: 12 }}>
                    <div style={{ fontWeight: 600 }}>{VERLOFCATEGORIE_LABEL[s.categorie]}</div>
                    <div style={{ marginTop: 4, color: 'var(--text2)' }}>
                      Toegekend {s.toegekend}u · gebruikt {s.gebruikt}u · gereserveerd {s.gereserveerd}u
                    </div>
                    <div style={{ marginTop: 4 }}>
                      <span style={{ fontWeight: 700, color: 'var(--green)' }}>{s.resterend}u resterend</span>
                      <span style={{ color: 'var(--text3)' }}> ({s.resterendNaReservering}u na reservering)</span>
                    </div>
                    {s.vervaldatum && <div style={{ fontSize: 10, color: 'var(--text3)' }}>vervalt {korteDatum(s.vervaldatum)}</div>}
                  </div>
                ))}
              </div>
              <form onSubmit={stelSaldoIn} style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-end', gap: 8 }}>
                <select className="inp" value={saldoForm.categorie} onChange={(e) => setSaldoForm((f) => ({ ...f, categorie: Number(e.target.value) }))}>
                  {VERLOFCATEGORIE_LABEL.map((l, i) => (
                    <option key={i} value={i}>
                      {l}
                    </option>
                  ))}
                </select>
                <input
                  className="inp"
                  style={{ width: 140 }}
                  type="number"
                  min={0}
                  step={0.25}
                  value={saldoForm.toegekendeUren}
                  onChange={(e) => setSaldoForm((f) => ({ ...f, toegekendeUren: Number(e.target.value) }))}
                  placeholder="toegekend (u)"
                />
                <input className="inp" type="date" value={saldoForm.vervaldatum} onChange={(e) => setSaldoForm((f) => ({ ...f, vervaldatum: e.target.value }))} />
                <button type="submit" className="btn btn-outline btn-sm">
                  Saldo instellen
                </button>
              </form>
            </div>
          </div>

          {/* Verlof aanvragen */}
          <div className="card" style={{ marginBottom: 16 }}>
            <div className="card-h">
              <h3>
                <i className="ti ti-beach" style={{ color: 'var(--amber)' }} /> Verlof aanvragen
              </h3>
            </div>
            <div className="card-b">
              <form onSubmit={dienAanvraagIn} style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-end', gap: 10 }}>
                <div className="fld" style={{ marginBottom: 0 }}>
                  <label>Van</label>
                  <input type="date" value={aanvraag.begindatum} onChange={(e) => setAanvraag((f) => ({ ...f, begindatum: e.target.value }))} />
                </div>
                <div className="fld" style={{ marginBottom: 0 }}>
                  <label>Tot</label>
                  <input type="date" value={aanvraag.einddatum} onChange={(e) => setAanvraag((f) => ({ ...f, einddatum: e.target.value }))} />
                </div>
                <div className="fld" style={{ marginBottom: 0, width: 100 }}>
                  <label>Uren</label>
                  <input type="number" min={0.25} step={0.25} value={aanvraag.aantalUren} onChange={(e) => setAanvraag((f) => ({ ...f, aantalUren: Number(e.target.value) }))} />
                </div>
                <div className="fld" style={{ marginBottom: 0 }}>
                  <label>Categorie</label>
                  <select value={aanvraag.categorie} onChange={(e) => setAanvraag((f) => ({ ...f, categorie: Number(e.target.value) }))}>
                    {VERLOFCATEGORIE_LABEL.map((l, i) => (
                      <option key={i} value={i}>
                        {l}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="fld" style={{ marginBottom: 0, flex: 1, minWidth: 160 }}>
                  <label>Reden</label>
                  <input value={aanvraag.reden} onChange={(e) => setAanvraag((f) => ({ ...f, reden: e.target.value }))} />
                </div>
                <button type="submit" className="btn btn-primary btn-sm">
                  Aanvragen
                </button>
              </form>
            </div>
          </div>
        </>
      )}

      {/* Archief */}
      <div className="ph" style={{ marginBottom: 10, gap: 8, flexWrap: 'wrap' }}>
        <h1 style={{ fontSize: 15 }}>Verlofarchief</h1>
        <div className="seg" role="tablist" style={{ display: 'inline-flex', gap: 4 }}>
          <button
            type="button"
            className={`btn btn-sm ${periode === 'aankomend' ? 'btn-primary' : 'btn-outline'}`}
            onClick={() => setPeriode('aankomend')}
          >
            Aankomend
          </button>
          <button
            type="button"
            className={`btn btn-sm ${periode === 'verleden' ? 'btn-primary' : 'btn-outline'}`}
            onClick={() => setPeriode('verleden')}
          >
            Verleden
          </button>
        </div>
        <select className="inp" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value === '' ? '' : Number(e.target.value))}>
          <option value="">Alle statussen</option>
          {VERLOFSTATUS_LABEL.map((l, i) => (
            <option key={i} value={i}>
              {l}
            </option>
          ))}
        </select>
      </div>
      <div className="tbl-wrap" style={{ marginBottom: 16 }}>
        <table className="tbl">
          <thead>
            <tr>
              <th>Medewerker</th>
              <th>Periode</th>
              <th>Uren</th>
              <th>Categorie</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {zichtbareAanvragen.length === 0 && (
              <tr>
                <td colSpan={6} style={{ textAlign: 'center', color: 'var(--text3)', padding: '18px 0' }}>
                  Geen {periode === 'aankomend' ? 'aankomende' : 'verstreken'} aanvragen.
                </td>
              </tr>
            )}
            {zichtbareAanvragen.map((a) => (
              <tr key={a.id} style={{ verticalAlign: 'top' }}>
                <td>{a.medewerkerNaam}</td>
                <td>
                  {korteDatum(a.begindatum)} – {korteDatum(a.einddatum)}
                  {a.reden && <div style={{ fontSize: 10, color: 'var(--text3)' }}>{a.reden}</div>}
                </td>
                <td>{a.aantalUren}u</td>
                <td>{VERLOFCATEGORIE_LABEL[a.categorie]}</td>
                <td>
                  <span className={`chip ${STATUS_CHIP[a.status]}`}>{VERLOFSTATUS_LABEL[a.status]}</span>
                  {a.beoordelingsNotitie && <div style={{ fontSize: 10, color: 'var(--text3)' }}>{a.beoordelingsNotitie}</div>}
                </td>
                <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                  {a.status === VerlofStatus.Openstaand && (
                    <div style={{ display: 'flex', gap: 5, justifyContent: 'flex-end' }}>
                      <button onClick={() => wrap(() => verlof.goedkeuren.mutateAsync(a.id), 'Goedkeuren mislukt.')} className="btn btn-green btn-xs">
                        Goedkeuren
                      </button>
                      <button onClick={() => afkeurMet(a)} className="btn btn-amber btn-xs">
                        Afkeuren
                      </button>
                      <button onClick={() => wrap(() => verlof.intrekken.mutateAsync(a.id), 'Intrekken mislukt.')} className="btn btn-rose btn-xs">
                        Intrekken
                      </button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {aanvragen?.length === 0 && (
              <tr>
                <td colSpan={6} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                  Geen aanvragen.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Ziekte */}
      {medewerkerId && (
        <div className="card">
          <div className="card-h">
            <h3>
              <i className="ti ti-mood-sick" style={{ color: 'var(--rose)' }} /> Ziekmeldingen
            </h3>
          </div>
          <div className="card-b">
            <form onSubmit={meldZiek} style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-end', gap: 10, marginBottom: 12 }}>
              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Eerste ziektedag</label>
                <input type="date" value={ziek.begindatum} onChange={(e) => setZiek((f) => ({ ...f, begindatum: e.target.value }))} />
              </div>
              <div className="fld" style={{ marginBottom: 0 }}>
                <label>Hersteld op (optioneel)</label>
                <input type="date" value={ziek.einddatum} onChange={(e) => setZiek((f) => ({ ...f, einddatum: e.target.value }))} />
              </div>
              <button type="submit" className="btn btn-rose btn-sm">
                Ziekmelden
              </button>
            </form>
            <div className="tbl-wrap">
              <table className="tbl">
                <thead>
                  <tr>
                    <th>Periode</th>
                    <th>Status</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {ziekmeldingen?.map((z) => (
                    <tr key={z.id}>
                      <td>
                        {korteDatum(z.begindatum)} – {z.einddatum ? korteDatum(z.einddatum) : 'lopend'}
                      </td>
                      <td>
                        {z.einddatum ? (
                          <span style={{ color: 'var(--text3)' }}>hersteld</span>
                        ) : (
                          <span style={{ fontWeight: 600, color: 'var(--rose)' }}>ziek</span>
                        )}
                      </td>
                      <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                        <div style={{ display: 'flex', gap: 5, justifyContent: 'flex-end' }}>
                          {!z.einddatum && (
                            <button
                              onClick={() => {
                                const d = prompt('Hersteldatum (jjjj-mm-dd):', vandaagIso());
                                if (d) void wrap(() => ziekte.herstel.mutateAsync({ id: z.id, einddatum: d }), 'Herstel mislukt.');
                              }}
                              className="btn btn-green btn-xs"
                            >
                              Beter melden
                            </button>
                          )}
                          <button onClick={() => wrap(() => ziekte.verwijderen.mutateAsync(z.id), 'Verwijderen mislukt.')} className="btn btn-rose btn-xs">
                            <i className="ti ti-trash" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                  {ziekmeldingen?.length === 0 && (
                    <tr>
                      <td colSpan={3} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                        Geen ziekmeldingen.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
