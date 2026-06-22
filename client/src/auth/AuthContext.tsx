import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, zetTokens, heeftTokens, bijSessieEinde } from '../api/client';
import type { AuthResponse } from '../types';

interface MeRespons {
  gebruikersnaam: string | null;
  rollen: string[];
  stamgroepNaam: string | null;
  weergavenaam: string | null;
  capabilities: string[];
}

interface AuthState {
  ingelogd: boolean;
  gebruikersnaam: string | null;
  rol: string | null;
  stamgroepNaam: string | null;
  weergavenaam: string | null;
  capabilities: string[];
}

interface AuthContextWaarde extends AuthState {
  klaar: boolean;
  /** Geeft true als 2FA nog nodig is (vervolgstap login2fa). */
  login: (gebruikersnaam: string, wachtwoord: string) => Promise<boolean>;
  login2fa: (gebruikersnaam: string, wachtwoord: string, code: string) => Promise<void>;
  uitloggen: () => void;
  heeft: (capability: string) => boolean;
}

const AuthContext = createContext<AuthContextWaarde | null>(null);

const LEEG: AuthState = {
  ingelogd: false,
  gebruikersnaam: null,
  rol: null,
  stamgroepNaam: null,
  weergavenaam: null,
  capabilities: [],
};

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(LEEG);
  const [klaar, setKlaar] = useState(false);

  useEffect(() => {
    bijSessieEinde(() => setState(LEEG));

    // Herstel de sessie bij het laden als er nog tokens zijn.
    if (!heeftTokens()) {
      setKlaar(true);
      return;
    }
    api<MeRespons>('/api/auth/me')
      .then((me) =>
        setState({
          ingelogd: true,
          gebruikersnaam: me.gebruikersnaam,
          rol: me.rollen[0] ?? null,
          stamgroepNaam: me.stamgroepNaam,
          weergavenaam: me.weergavenaam,
          capabilities: me.capabilities,
        }),
      )
      .catch(() => {
        zetTokens(null);
        setState(LEEG);
      })
      .finally(() => setKlaar(true));
  }, []);

  function pasResponsToe(resp: AuthResponse, gebruikersnaam: string) {
    if (!resp.accessToken || !resp.refreshToken) {
      throw new Error('Geen tokens ontvangen.');
    }
    zetTokens({ accessToken: resp.accessToken, refreshToken: resp.refreshToken });
    // De ingetypte gebruikersnaam meteen tonen (de login-respons bevat hem niet),
    // zodat de zijbalk direct de juiste naam + rol toont i.p.v. "Gebruiker".
    setState({
      ingelogd: true,
      gebruikersnaam,
      rol: resp.rol,
      stamgroepNaam: resp.stamgroepNaam,
      weergavenaam: resp.weergavenaam,
      capabilities: resp.capabilities,
    });
  }

  const waarde = useMemo<AuthContextWaarde>(
    () => ({
      ...state,
      klaar,
      heeft: (cap) => state.capabilities.includes(cap),
      async login(gebruikersnaam, wachtwoord) {
        const resp = await api<AuthResponse>('/api/auth/login', {
          method: 'POST',
          body: JSON.stringify({ gebruikersnaam, wachtwoord }),
        });
        if (resp.vereistTweeFactor) return true;
        pasResponsToe(resp, gebruikersnaam);
        return false;
      },
      async login2fa(gebruikersnaam, wachtwoord, code) {
        const resp = await api<AuthResponse>('/api/auth/login-2fa', {
          method: 'POST',
          body: JSON.stringify({ gebruikersnaam, wachtwoord, code }),
        });
        pasResponsToe(resp, gebruikersnaam);
      },
      uitloggen() {
        zetTokens(null);
        setState(LEEG);
      },
    }),
    [state, klaar],
  );

  return <AuthContext.Provider value={waarde}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextWaarde {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth moet binnen AuthProvider gebruikt worden.');
  return ctx;
}
