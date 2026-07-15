import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiFout } from '../api/client';

/** Publiek rondleiding-aanvraagformulier (geen login), naar het anonieme rate-limited endpoint. */
export function PubliekRondleidingPage() {
  const [f, setF] = useState({
    ouderVoornaam: '', ouderAchternaam: '', telefoon: '', email: '', voorkeurDatum: '', opmerking: '',
  });
  const [fout, setFout] = useState<string | null>(null);
  const [bezig, setBezig] = useState(false);
  const [klaar, setKlaar] = useState(false);

  async function verstuur(e: FormEvent) {
    e.preventDefault();
    setFout(null);
    setBezig(true);
    try {
      await api('/api/publiek/rondleiding', {
        method: 'POST',
        body: JSON.stringify({
          ouderVoornaam: f.ouderVoornaam,
          ouderAchternaam: f.ouderAchternaam,
          telefoon: f.telefoon,
          email: f.email,
          voorkeurDatum: f.voorkeurDatum,
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
    <div style={{ maxWidth: 520, margin: '0 auto', padding: '32px 16px' }}>
      <h1 style={{ fontSize: 22, marginBottom: 4 }}>Rondleiding aanvragen</h1>
      <p style={{ color: 'var(--text3)', marginBottom: 20 }}>Op d'n Buiten — kinderopvang</p>

      {klaar ? (
        <div className="alert alert-ok">
          <i className="ti ti-circle-check" />
          <span>Bedankt! Uw aanvraag is ontvangen. We nemen contact met u op om een afspraak te maken.</span>
        </div>
      ) : (
        <form onSubmit={verstuur} className="tbl-wrap" style={{ padding: 20 }}>
          {fout && (
            <div className="alert alert-bad" style={{ marginBottom: 14 }}>
              <i className="ti ti-alert-circle" />
              <span>{fout}</span>
            </div>
          )}

          <div className="frow" style={{ gridTemplateColumns: '1fr 1fr' }}>
            <div className="fld">
              <label>Voornaam</label>
              <input required value={f.ouderVoornaam} onChange={(e) => setF({ ...f, ouderVoornaam: e.target.value })} />
            </div>
            <div className="fld">
              <label>Achternaam</label>
              <input required value={f.ouderAchternaam} onChange={(e) => setF({ ...f, ouderAchternaam: e.target.value })} />
            </div>
            <div className="fld">
              <label>Telefoon</label>
              <input required value={f.telefoon} onChange={(e) => setF({ ...f, telefoon: e.target.value })} />
            </div>
            <div className="fld">
              <label>E-mail</label>
              <input type="email" required value={f.email} onChange={(e) => setF({ ...f, email: e.target.value })} />
            </div>
          </div>
          <div className="fld">
            <label>Voorkeursdatum</label>
            <input type="date" required value={f.voorkeurDatum} onChange={(e) => setF({ ...f, voorkeurDatum: e.target.value })} />
          </div>
          <div className="fld">
            <label>Opmerking (optioneel)</label>
            <textarea rows={3} value={f.opmerking} onChange={(e) => setF({ ...f, opmerking: e.target.value })} />
          </div>

          <button type="submit" className="btn btn-primary" disabled={bezig}>
            <i className="ti ti-send" /> Aanvraag versturen
          </button>
        </form>
      )}

      <p style={{ marginTop: 16, fontSize: 12 }}>
        <Link to="/aanmelden">Direct aanmelden voor de wachtlijst</Link>
      </p>
    </div>
  );
}
