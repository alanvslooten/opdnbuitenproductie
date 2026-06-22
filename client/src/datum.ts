// Datum-hulpjes die met ISO-strings ("yyyy-MM-dd") werken, los van de lokale tijdzone.

export function vandaagIso(): string {
  const nu = new Date();
  const jaar = nu.getFullYear();
  const maand = String(nu.getMonth() + 1).padStart(2, '0');
  const dag = String(nu.getDate()).padStart(2, '0');
  return `${jaar}-${maand}-${dag}`;
}

/** De maandag van de week waarin de ISO-datum valt. */
export function weekBeginIso(iso: string): string {
  const d = new Date(`${iso}T00:00:00`);
  const dow = (d.getDay() + 6) % 7; // ma=0 ... zo=6
  d.setDate(d.getDate() - dow);
  return isoVan(d);
}

export function verschuifDagen(iso: string, dagen: number): string {
  const d = new Date(`${iso}T00:00:00`);
  d.setDate(d.getDate() + dagen);
  return isoVan(d);
}

function isoVan(d: Date): string {
  const jaar = d.getFullYear();
  const maand = String(d.getMonth() + 1).padStart(2, '0');
  const dag = String(d.getDate()).padStart(2, '0');
  return `${jaar}-${maand}-${dag}`;
}

/** "ma 15 jun" voor compacte weergave (week-/dagnavigatie). */
export function korteDatum(iso: string): string {
  const d = new Date(`${iso}T00:00:00`);
  return d.toLocaleDateString('nl-NL', { weekday: 'short', day: 'numeric', month: 'short' });
}

/** Europees datumformaat "DD-MM-JJJJ" voor lijsten en records. */
export function datumNl(iso: string): string {
  // iso = "yyyy-MM-dd"; los van tijdzone opsplitsen i.p.v. via Date (voorkomt verschuiving).
  const [jaar, maand, dag] = iso.slice(0, 10).split('-');
  return `${dag}-${maand}-${jaar}`;
}
