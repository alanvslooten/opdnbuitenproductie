import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

// Achtergrondbeeld voor het linkerpaneel (zelfde sfeer als het bijgevoegde ontwerp).
const SFEERBEELD =
  'https://images.unsplash.com/photo-1567405258710-35a7015252c0?q=80&w=1400&auto=format&fit=crop';

export function LoginPage() {
  const { login, login2fa } = useAuth();
  const navigate = useNavigate();

  const [gebruikersnaam, setGebruikersnaam] = useState('');
  const [wachtwoord, setWachtwoord] = useState('');
  const [code, setCode] = useState('');
  const [tweeFactor, setTweeFactor] = useState(false);
  const [fout, setFout] = useState<string | null>(null);
  const [bezig, setBezig] = useState(false);

  async function verstuur(e: FormEvent) {
    e.preventDefault();
    setFout(null);
    setBezig(true);
    try {
      if (tweeFactor) {
        await login2fa(gebruikersnaam, wachtwoord, code);
      } else {
        const nodig = await login(gebruikersnaam, wachtwoord);
        if (nodig) {
          setTweeFactor(true);
          return;
        }
      }
      navigate('/planning');
    } catch (err) {
      setFout(err instanceof Error ? err.message : 'Inloggen mislukt.');
    } finally {
      setBezig(false);
    }
  }

  return (
    <div className="login-screen">
      <div className="login-split">
        <div className="login-left">
          <div
            className="login-slide on"
            style={{ backgroundImage: `url(${SFEERBEELD})` }}
          />
          <div className="login-left-overlay" />
          <div className="login-left-content">
            <h1>Kinderzorg op één plek</h1>
            <p>Wachtlijst, planning, observaties en personeelsbeheer — geïntegreerd en intuïtief.</p>
            <div className="login-tags">
              <span className="login-tag">🎯 Wachtlijstbeheer</span>
              <span className="login-tag">📋 BKR-normen</span>
              <span className="login-tag">👶 Observaties</span>
              <span className="login-tag">📅 Roosters</span>
            </div>
          </div>
        </div>

        <div className="login-right">
          <form className="login-form-wrap" onSubmit={verstuur}>
            <div className="login-logo">
              <div className="login-logo-mark">
                <i className="ti ti-building-community" />
              </div>
              <div>
                <h1>KinderKompas</h1>
                <span>KDV Beheersysteem</span>
              </div>
            </div>
            <h2>Inloggen</h2>
            <p className="login-sub">
              {tweeFactor ? 'Voer je tweestapscode in' : 'Toegang tot uw dashboard'}
            </p>

            <div className="fld">
              <label>Gebruikersnaam</label>
              <input
                value={gebruikersnaam}
                onChange={(e) => setGebruikersnaam(e.target.value)}
                disabled={tweeFactor}
                autoComplete="username"
                placeholder="gebruikersnaam"
              />
            </div>

            <div className="fld">
              <label>Wachtwoord</label>
              <input
                type="password"
                value={wachtwoord}
                onChange={(e) => setWachtwoord(e.target.value)}
                disabled={tweeFactor}
                autoComplete="current-password"
                placeholder="••••••••"
              />
            </div>

            {tweeFactor && (
              <div className="fld">
                <label>2FA-code</label>
                <input
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  inputMode="numeric"
                  autoFocus
                  style={{ letterSpacing: '.3em' }}
                  placeholder="000000"
                />
              </div>
            )}

            <button type="submit" className="btn btn-primary btn-full" disabled={bezig}>
              <i className={`ti ${tweeFactor ? 'ti-shield-check' : 'ti-login'}`} />
              {bezig ? 'Bezig…' : tweeFactor ? 'Bevestig code' : 'Inloggen'}
            </button>
          </form>
        </div>
      </div>

      {fout && (
        <div className="overlay on" onClick={() => setFout(null)}>
          <div className="modal" style={{ maxWidth: 380 }} onClick={(e) => e.stopPropagation()}>
            <div className="modal-h">
              <h2>
                <i className="ti ti-alert-triangle" style={{ color: 'var(--rose)' }} /> Inloggen mislukt
              </h2>
              <button type="button" className="xbtn" onClick={() => setFout(null)}>
                <i className="ti ti-x" />
              </button>
            </div>
            <div className="modal-b">
              <p style={{ fontSize: 13 }}>{fout}</p>
              <p style={{ fontSize: 12, color: 'var(--text3)', marginTop: 6 }}>
                Controleer je gebruikersnaam en wachtwoord en probeer het opnieuw.
              </p>
            </div>
            <div className="modal-f" style={{ position: 'static' }}>
              <button type="button" className="btn btn-primary btn-sm" onClick={() => setFout(null)} autoFocus>
                Opnieuw proberen
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
