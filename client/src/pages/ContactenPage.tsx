import { useState, type FormEvent } from 'react';
import { useContact, useContacten, useContactMutaties } from '../api/queries';
import { ApiFout } from '../api/client';
import { datumNl, vandaagIso } from '../datum';
import {
  RondleidingStatus,
  RONDLEIDING_STATUS_LABEL,
  WACHTLIJST_STATUS_LABEL,
  type ContactDto,
  type ContactInvoer,
} from '../types';

const LEEG_FORM: ContactInvoer = {
  voornaam: '',
  achternaam: '',
  telefoon: null,
  email: null,
  isIntern: false,
  aantekeningen: null,
};

export function ContactenPage() {
  const { data, isLoading, error } = useContacten();
  const { verwijderen } = useContactMutaties();
  const [zoek, setZoek] = useState('');
  const [detailId, setDetailId] = useState<string | null>(null);
  const [form, setForm] = useState<{ id?: string; invoer: ContactInvoer } | null>(null);
  const [fout, setFout] = useState<string | null>(null);

  const term = zoek.trim().toLowerCase();
  const zichtbaar = (data ?? []).filter(
    (c) => !term || c.volledigeNaam.toLowerCase().includes(term),
  );

  async function verwijder(c: ContactDto) {
    setFout(null);
    if (!confirm(`Contact "${c.volledigeNaam}" verwijderen? Gekoppelde kinderen/inschrijvingen blijven bestaan.`)) return;
    try {
      await verwijderen.mutateAsync(c.id);
    } catch (err) {
      setFout(err instanceof ApiFout ? err.message : 'Verwijderen mislukt.');
    }
  }

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Contacten</h1>
          <p>Ouders, verzorgers en voogden — met rondleidingen, voorstellen en geplaatste kinderen</p>
        </div>
        <div className="ph-actions">
          <button className="btn btn-primary btn-sm" onClick={() => setForm({ invoer: { ...LEEG_FORM } })}>
            <i className="ti ti-user-plus" /> Nieuw contact
          </button>
        </div>
      </div>

      {fout && (
        <div className="alert alert-bad">
          <i className="ti ti-alert-circle" />
          <span>{fout}</span>
        </div>
      )}

      <div className="filters">
        <div className="search-box" style={{ maxWidth: 280 }}>
          <i className="ti ti-search" />
          <input placeholder="Zoek op naam…" value={zoek} onChange={(e) => setZoek(e.target.value)} />
        </div>
      </div>

      {isLoading && (
        <div className="loader">
          <i className="ti ti-loader" /> Laden…
        </div>
      )}
      {error && <p style={{ color: 'var(--rose)' }}>Kon contacten niet laden.</p>}

      <div className="tbl-wrap">
        <table className="tbl">
          <thead>
            <tr>
              <th>Naam</th>
              <th>Type</th>
              <th>Telefoon</th>
              <th title="Rondleidingen">Rondl.</th>
              <th title="Wachtlijst-inschrijvingen">Insch.</th>
              <th title="Geplaatste kinderen">Kind.</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {zichtbaar.map((c) => (
              <tr key={c.id}>
                <td style={{ fontWeight: 600 }}>
                  <button
                    onClick={() => setDetailId(c.id)}
                    style={{ background: 'none', border: 'none', padding: 0, font: 'inherit', fontWeight: 600, color: 'var(--blue)', cursor: 'pointer' }}
                  >
                    {c.volledigeNaam}
                  </button>
                </td>
                <td>
                  <span className={`badge ${c.isIntern ? 'b-violet' : 'b-gray'}`}>{c.isIntern ? 'Intern' : 'Extern'}</span>
                </td>
                <td>{c.telefoon ?? '—'}</td>
                <td>{c.aantalRondleidingen}</td>
                <td>{c.aantalInschrijvingen}</td>
                <td>{c.aantalGeplaatsteKinderen}</td>
                <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                  <button onClick={() => setForm({ id: c.id, invoer: invoerVan(c) })} className="btn btn-outline btn-xs" style={{ marginRight: 6 }}>
                    <i className="ti ti-pencil" />
                  </button>
                  <button onClick={() => verwijder(c)} className="btn btn-rose btn-xs">
                    <i className="ti ti-trash" />
                  </button>
                </td>
              </tr>
            ))}
            {data && zichtbaar.length === 0 && (
              <tr>
                <td colSpan={7} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                  Geen contacten gevonden.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {detailId && <ContactDetailModal id={detailId} onSluit={() => setDetailId(null)} />}
      {form && <ContactFormModal start={form} onSluit={() => setForm(null)} />}
    </div>
  );
}

function invoerVan(c: ContactDto): ContactInvoer {
  return {
    voornaam: c.voornaam,
    achternaam: c.achternaam,
    telefoon: c.telefoon,
    email: c.email,
    isIntern: c.isIntern,
    aantekeningen: c.aantekeningen,
  };
}

function ContactFormModal({ start, onSluit }: { start: { id?: string; invoer: ContactInvoer }; onSluit: () => void }) {
  const { aanmaken, bewerken } = useContactMutaties();
  const [m, setM] = useState<ContactInvoer>(start.invoer);
  const [fout, setFout] = useState<string | null>(null);
  const bezig = aanmaken.isPending || bewerken.isPending;

  async function verstuur(e: FormEvent) {
    e.preventDefault();
    setFout(null);
    try {
      if (start.id) await bewerken.mutateAsync({ id: start.id, invoer: m });
      else await aanmaken.mutateAsync(m);
      onSluit();
    } catch (err) {
      setFout(err instanceof ApiFout ? err.message : 'Opslaan mislukt.');
    }
  }

  return (
    <div className="overlay on" onClick={onSluit}>
      <form className="modal" style={{ maxWidth: 460 }} onClick={(e) => e.stopPropagation()} onSubmit={verstuur}>
        <div className="modal-h">
          <h2><i className="ti ti-user" style={{ color: 'var(--primary)' }} /> {start.id ? 'Contact bewerken' : 'Nieuw contact'}</h2>
          <button type="button" className="xbtn" onClick={onSluit}><i className="ti ti-x" /></button>
        </div>
        <div className="modal-b">
          {fout && <div className="alert alert-bad" style={{ marginBottom: 12 }}><i className="ti ti-alert-circle" /><span>{fout}</span></div>}
          <div className="frow" style={{ gridTemplateColumns: '1fr 1fr' }}>
            <div className="fld"><label>Voornaam</label><input required value={m.voornaam} onChange={(e) => setM({ ...m, voornaam: e.target.value })} /></div>
            <div className="fld"><label>Achternaam</label><input required value={m.achternaam} onChange={(e) => setM({ ...m, achternaam: e.target.value })} /></div>
            <div className="fld"><label>Telefoon</label><input value={m.telefoon ?? ''} onChange={(e) => setM({ ...m, telefoon: e.target.value || null })} /></div>
            <div className="fld"><label>E-mail</label><input type="email" value={m.email ?? ''} onChange={(e) => setM({ ...m, email: e.target.value || null })} /></div>
          </div>
          <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 12, fontWeight: 600, color: 'var(--text2)', margin: '6px 0 12px' }}>
            <input type="checkbox" checked={m.isIntern} onChange={(e) => setM({ ...m, isIntern: e.target.checked })} />
            Intern contact (bestaande band met de opvang)
          </label>
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Aantekeningen</label>
            <textarea value={m.aantekeningen ?? ''} onChange={(e) => setM({ ...m, aantekeningen: e.target.value || null })} />
          </div>
        </div>
        <div className="modal-f" style={{ position: 'static' }}>
          <button type="button" onClick={onSluit} className="btn btn-outline btn-sm">Annuleren</button>
          <button type="submit" disabled={bezig} className="btn btn-primary btn-sm">{bezig ? 'Opslaan…' : 'Opslaan'}</button>
        </div>
      </form>
    </div>
  );
}

function ContactDetailModal({ id, onSluit }: { id: string; onSluit: () => void }) {
  const { data: c, isLoading } = useContact(id);
  const { rondleidingToevoegen, rondleidingVerwijderen } = useContactMutaties();
  const [rlDatum, setRlDatum] = useState(vandaagIso());
  const [rlStatus, setRlStatus] = useState<number>(RondleidingStatus.Gepland);
  const [rlNotitie, setRlNotitie] = useState('');

  async function voegRondleidingToe(e: FormEvent) {
    e.preventDefault();
    await rondleidingToevoegen.mutateAsync({ id, invoer: { datum: rlDatum, status: rlStatus, notitie: rlNotitie || null } });
    setRlNotitie('');
  }

  return (
    <div className="overlay on" onClick={onSluit}>
      <div className="modal" style={{ maxWidth: 560 }} onClick={(e) => e.stopPropagation()}>
        <div className="modal-h">
          <h2><i className="ti ti-user" style={{ color: 'var(--primary)' }} /> {c ? `${c.voornaam} ${c.achternaam}` : 'Contact'}</h2>
          <button className="xbtn" onClick={onSluit}><i className="ti ti-x" /></button>
        </div>
        <div className="modal-b">
          {isLoading || !c ? (
            <p style={{ color: 'var(--text3)' }}>Laden…</p>
          ) : (
            <>
              <div className="g2" style={{ marginBottom: 8 }}>
                <Veld label="Type" waarde={c.isIntern ? 'Intern' : 'Extern'} />
                <Veld label="Telefoon" waarde={c.telefoon ?? '—'} />
                <Veld label="E-mail" waarde={c.email ?? '—'} />
              </div>
              {c.aantekeningen && (
                <p style={{ fontSize: 12, color: 'var(--text2)', background: 'var(--surface2)', padding: '8px 10px', borderRadius: 6, marginBottom: 12 }}>
                  {c.aantekeningen}
                </p>
              )}

              <Sectie titel="Geplaatste kinderen" icon="ti-mood-kid">
                {c.geplaatsteKinderen.length === 0 ? <Leeg /> : c.geplaatsteKinderen.map((k) => (
                  <Rij key={k.id} links={k.naam} rechts={k.stamgroepNaam} />
                ))}
              </Sectie>

              <Sectie titel="Wachtlijst-inschrijvingen" icon="ti-list-numbers">
                {c.inschrijvingen.length === 0 ? <Leeg /> : c.inschrijvingen.map((i) => (
                  <Rij key={i.id} links={`${i.kindNaam} · start ${datumNl(i.gewensteStartdatum)}`}
                    rechts={`${WACHTLIJST_STATUS_LABEL[i.status] ?? '?'} · ${i.aantalVoorstellen} voorstel(len)`} />
                ))}
              </Sectie>

              <Sectie titel="Rondleidingen" icon="ti-walk">
                {c.rondleidingen.length === 0 ? <Leeg /> : c.rondleidingen.map((r) => (
                  <div key={r.id} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '4px 0', borderBottom: '1px solid var(--border)' }}>
                    <span style={{ fontSize: 12 }}>
                      {datumNl(r.datum)} · <span className="badge b-gray">{RONDLEIDING_STATUS_LABEL[r.status] ?? '?'}</span>
                      {r.notitie && <span style={{ color: 'var(--text3)' }}> — {r.notitie}</span>}
                    </span>
                    <button onClick={() => rondleidingVerwijderen.mutate(r.id)} className="btn btn-ghost btn-xs" title="Verwijderen">
                      <i className="ti ti-trash" />
                    </button>
                  </div>
                ))}
                <form onSubmit={voegRondleidingToe} style={{ display: 'flex', gap: 6, alignItems: 'flex-end', marginTop: 10, flexWrap: 'wrap' }}>
                  <div className="fld" style={{ marginBottom: 0 }}>
                    <label>Datum</label>
                    <input type="date" value={rlDatum} onChange={(e) => setRlDatum(e.target.value)} />
                  </div>
                  <div className="fld" style={{ marginBottom: 0 }}>
                    <label>Status</label>
                    <select value={rlStatus} onChange={(e) => setRlStatus(Number(e.target.value))}>
                      {RONDLEIDING_STATUS_LABEL.map((l, i) => <option key={i} value={i}>{l}</option>)}
                    </select>
                  </div>
                  <div className="fld" style={{ marginBottom: 0, flex: 1, minWidth: 120 }}>
                    <label>Notitie</label>
                    <input value={rlNotitie} onChange={(e) => setRlNotitie(e.target.value)} />
                  </div>
                  <button type="submit" disabled={rondleidingToevoegen.isPending} className="btn btn-primary btn-xs">
                    <i className="ti ti-plus" /> Toevoegen
                  </button>
                </form>
              </Sectie>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

function Sectie({ titel, icon, children }: { titel: string; icon: string; children: React.ReactNode }) {
  return (
    <div style={{ marginBottom: 14 }}>
      <h3 style={{ fontSize: 12, fontWeight: 700, marginBottom: 6 }}>
        <i className={`ti ${icon}`} style={{ color: 'var(--primary)' }} /> {titel}
      </h3>
      {children}
    </div>
  );
}

function Rij({ links, rechts }: { links: string; rechts: string }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '4px 0', borderBottom: '1px solid var(--border)', fontSize: 12 }}>
      <span>{links}</span>
      <span style={{ color: 'var(--text3)' }}>{rechts}</span>
    </div>
  );
}

function Leeg() {
  return <p style={{ fontSize: 12, color: 'var(--text3)' }}>Geen.</p>;
}

function Veld({ label, waarde }: { label: string; waarde: string }) {
  return (
    <div>
      <div style={{ fontSize: 9, fontWeight: 700, color: 'var(--text3)', textTransform: 'uppercase', marginBottom: 2 }}>{label}</div>
      <div style={{ fontSize: 13, color: 'var(--text)' }}>{waarde || '—'}</div>
    </div>
  );
}
