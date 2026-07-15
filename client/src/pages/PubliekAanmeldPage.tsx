import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiFout } from '../api/client';
import { Contracttype, WEEKDAGEN } from '../types';

/**
 * Publiek wachtlijst-aanmeldformulier: ouders melden hun kind zelf aan (geen login).
 * Verstuurt naar het anonieme, rate-limited endpoint; de opvang handelt de aanvraag
 * daarna af (prioriteit, groep, contact).
 */
export function PubliekAanmeldPage() {
  const [f, setF] = useState({
    voornaam: '', achternaam: '', geboortedatum: '', gewensteStartdatum: '',
    dagen: 0, contracttype: Contracttype.Weken49 as number,
    ouderNaam: '', ouderTelefoon: '', ouderEmail: '', opmerking: '',
  });
  const [fout, setFout] = useState<string | null>(null);
  const [bezig, setBezig] = useState(false);
  const [klaar, setKlaar] = useState(false);

  function toggleDag(vlag: number) {
    setF((s) => ({ ...s, dagen: s.dagen ^ vlag }));
  }

  async function verstuur(e: FormEvent) {
    e.preventDefault();
    setFout(null);
    setBezig(true);
    try {
      await api('/api/publiek/aanmelden', {
        method: 'POST',
        body: JSON.stringify({
          voornaam: f.voornaam,
          achternaam: f.achternaam,
          geboortedatum: f.geboortedatum,
          gewensteStartdatum: f.gewensteStartdatum,
          gewensteOpvangdagen: f.dagen,
          contracttype: f.contracttype,
          ouderNaam: f.ouderNaam,
          ouderTelefoon: f.ouderTelefoon,
          ouderEmail: f.ouderEmail,
          opmerking: f.opmerking || null,
        }),
      });
      setKlaar(true);
    } catch (err) {
      setFout(err instanceof ApiFout ? err.message : 'Er ging iets mis. Probeer het later opnieuw.');
    } finally {
      setBezig(false);
    }
  }

  return (
    <div style={{ maxWidth: 560, margin: '0 auto', padding: '32px 16px' }}>
      <h1 style={{ fontSize: 22, marginBottom: 4 }}>Aanmelden voor de wachtlijst</h1>
      <p style={{ color: 'var(--text3)', marginBottom: 20 }}>Op d'n Buiten — kinderopvang</p>

      {klaar ? (
        <div className="alert alert-ok">
          <i className="ti ti-circle-check" />
          <span>Bedankt! Uw aanmelding is ontvangen. We nemen contact met u op.</span>
        </div>
      ) : (
        <form onSubmit={verstuur} className="tbl-wrap" style={{ padding: 20 }}>
          {fout && (
            <div className="alert alert-bad" style={{ marginBottom: 14 }}>
              <i className="ti ti-alert-circle" />
              <span>{fout}</span>
            </div>
          )}

          <h3 style={{ fontSize: 13, marginBottom: 8 }}>Gegevens kind</h3>
          <div className="frow" style={{ gridTemplateColumns: '1fr 1fr' }}>
            <div className="fld">
              <label>Voornaam</label>
              <input required value={f.voornaam} onChange={(e) => setF({ ...f, voornaam: e.target.value })} />
            </div>
            <div className="fld">
              <label>Achternaam</label>
              <input required value={f.achternaam} onChange={(e) => setF({ ...f, achternaam: e.target.value })} />
            </div>
            <div className="fld">
              <label>Geboortedatum</label>
              <input type="date" required value={f.geboortedatum} onChange={(e) => setF({ ...f, geboortedatum: e.target.value })} />
            </div>
            <div className="fld">
              <label>Gewenste startdatum</label>
              <input type="date" required value={f.gewensteStartdatum} onChange={(e) => setF({ ...f, gewensteStartdatum: e.target.value })} />
            </div>
          </div>

          <div className="fld">
            <label>Gewenste opvangdagen</label>
            <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
              {WEEKDAGEN.map((d) => (
                <button
                  type="button"
                  key={d.vlag}
                  onClick={() => toggleDag(d.vlag)}
                  className={`btn btn-sm ${(f.dagen & d.vlag) !== 0 ? 'btn-primary' : 'btn-outline'}`}
                >
                  {d.korte}
                </button>
              ))}
            </div>
          </div>

          <div className="fld">
            <label>Contractvorm</label>
            <select value={f.contracttype} onChange={(e) => setF({ ...f, contracttype: Number(e.target.value) })}>
              <option value={Contracttype.Weken49}>Hele jaar (49 weken)</option>
              <option value={Contracttype.Weken40}>Schoolweken (40 weken)</option>
            </select>
          </div>

          <h3 style={{ fontSize: 13, margin: '10px 0 8px' }}>Uw gegevens</h3>
          <div className="fld">
            <label>Naam ouder/verzorger</label>
            <input required value={f.ouderNaam} onChange={(e) => setF({ ...f, ouderNaam: e.target.value })} />
          </div>
          <div className="frow" style={{ gridTemplateColumns: '1fr 1fr' }}>
            <div className="fld">
              <label>Telefoon</label>
              <input required value={f.ouderTelefoon} onChange={(e) => setF({ ...f, ouderTelefoon: e.target.value })} />
            </div>
            <div className="fld">
              <label>E-mail</label>
              <input type="email" required value={f.ouderEmail} onChange={(e) => setF({ ...f, ouderEmail: e.target.value })} />
            </div>
          </div>
          <div className="fld">
            <label>Opmerking (optioneel)</label>
            <textarea rows={3} value={f.opmerking} onChange={(e) => setF({ ...f, opmerking: e.target.value })} />
          </div>

          <button type="submit" className="btn btn-primary" disabled={bezig || f.dagen === 0}>
            <i className="ti ti-send" /> Aanmelding versturen
          </button>
        </form>
      )}

      <p style={{ marginTop: 16, fontSize: 12 }}>
        <Link to="/rondleiding">Liever eerst een rondleiding aanvragen?</Link>
      </p>
    </div>
  );
}
