import type { AuthResponse } from '../types';

// Lichte fetch-wrapper met JWT-bearer en automatische token-refresh. De tokens
// leven in module-state (gespiegeld in localStorage), zodat zowel React-componenten
// als deze module ze delen zonder prop-drilling.

interface Tokens {
  accessToken: string;
  refreshToken: string;
}

const OPSLAG_SLEUTEL = 'kk_tokens';

// Basis-URL van de API. Leeg in dev (Vite proxyt /api naar de lokale API), in
// productie de absolute API-URL: via build-time env var VITE_API_BASE_URL, met
// de Render-API als terugval. Een static-site rewrite proxyt geen POST, dus
// praat de frontend in productie rechtstreeks met de API (CORS staat dit toe).
const API_BASIS = (
  import.meta.env.VITE_API_BASE_URL ??
  (import.meta.env.PROD ? 'https://kinderkompas-api.onrender.com' : '')
).replace(/\/+$/, '');

function apiUrl(pad: string): string {
  return API_BASIS + pad;
}

function laad(): Tokens | null {
  const ruw = localStorage.getItem(OPSLAG_SLEUTEL);
  return ruw ? (JSON.parse(ruw) as Tokens) : null;
}

let tokens: Tokens | null = laad();
let bijUitloggen: (() => void) | null = null;

export function zetTokens(t: Tokens | null): void {
  tokens = t;
  if (t) localStorage.setItem(OPSLAG_SLEUTEL, JSON.stringify(t));
  else localStorage.removeItem(OPSLAG_SLEUTEL);
}

export function heeftTokens(): boolean {
  return tokens !== null;
}

/** Callback die wordt aangeroepen als de sessie definitief verloopt (refresh faalt). */
export function bijSessieEinde(cb: () => void): void {
  bijUitloggen = cb;
}

export class ApiFout extends Error {
  constructor(
    public status: number,
    message: string,
    public probleem?: unknown,
  ) {
    super(message);
  }
}

async function ruwVerzoek(pad: string, opties: RequestInit): Promise<Response> {
  const headers = new Headers(opties.headers);
  if (tokens) headers.set('Authorization', `Bearer ${tokens.accessToken}`);
  // FormData zet zelf de juiste multipart-Content-Type (met boundary); dan niets forceren.
  if (opties.body && !headers.has('Content-Type') && !(opties.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }
  return fetch(apiUrl(pad), { ...opties, headers });
}

async function probeerVernieuwen(): Promise<boolean> {
  if (!tokens) return false;
  const res = await fetch(apiUrl('/api/auth/refresh'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken: tokens.refreshToken }),
  });
  if (!res.ok) return false;
  const data = (await res.json()) as AuthResponse;
  if (!data.accessToken || !data.refreshToken) return false;
  zetTokens({ accessToken: data.accessToken, refreshToken: data.refreshToken });
  return true;
}

export async function api<T>(pad: string, opties: RequestInit = {}): Promise<T> {
  let res = await ruwVerzoek(pad, opties);

  // Bij 401: één keer proberen te vernieuwen en het verzoek herhalen.
  if (res.status === 401 && tokens) {
    if (await probeerVernieuwen()) {
      res = await ruwVerzoek(pad, opties);
    } else {
      zetTokens(null);
      bijUitloggen?.();
    }
  }

  if (res.status === 204) return undefined as T;

  const tekst = await res.text();
  const data = tekst ? JSON.parse(tekst) : undefined;

  if (!res.ok) {
    const probleem = data as { title?: string; detail?: string; fout?: string; errors?: Record<string, string[]> } | undefined;
    const losseFouten = probleem?.errors
      ? Object.values(probleem.errors).flat().join(' ')
      : undefined;
    // 'fout' is de vorm die de auth-endpoints teruggeven (bv. onjuist wachtwoord).
    const bericht = losseFouten ?? probleem?.detail ?? probleem?.title ?? probleem?.fout ?? res.statusText;
    throw new ApiFout(res.status, bericht, data);
  }

  return data as T;
}

/** Haalt een binair bestand op (met dezelfde auth/refresh-logica) als Blob, bijv. een PDF. */
export async function apiBlob(pad: string): Promise<Blob> {
  let res = await ruwVerzoek(pad, {});
  if (res.status === 401 && tokens) {
    if (await probeerVernieuwen()) {
      res = await ruwVerzoek(pad, {});
    } else {
      zetTokens(null);
      bijUitloggen?.();
    }
  }
  if (!res.ok) throw new ApiFout(res.status, res.statusText);
  return res.blob();
}
