// Light/dark-thema beheren via het data-theme-attribuut op <html>.
// De begininstelling wordt al in index.html vóór paint gezet; dit zijn de runtime-helpers.

export type Thema = 'light' | 'dark';

const SLEUTEL = 'kk-theme';

export function huidigThema(): Thema {
  const t = document.documentElement.getAttribute('data-theme');
  return t === 'dark' ? 'dark' : 'light';
}

export function zetThema(thema: Thema): void {
  document.documentElement.setAttribute('data-theme', thema);
  try {
    localStorage.setItem(SLEUTEL, thema);
  } catch {
    // localStorage kan geblokkeerd zijn; thema werkt dan alleen voor deze sessie.
  }
}

export function wisselThema(): Thema {
  const nieuw: Thema = huidigThema() === 'dark' ? 'light' : 'dark';
  zetThema(nieuw);
  return nieuw;
}
