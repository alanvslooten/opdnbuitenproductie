// DTO-types die het API-contract van fase 4 spiegelen. Enums komen als getallen
// over de lijn (System.Text.Json serialiseert ze standaard numeriek).

export type Iso = string; // "yyyy-MM-dd" voor DateOnly

/** Contracttype: waarde = aantal weken (komt zo uit de enum). */
export const Contracttype = { Weken49: 49, Weken40: 40 } as const;
export type ContracttypeWaarde = (typeof Contracttype)[keyof typeof Contracttype];

/** Weekdag bit-vlaggen (Ma=1, Di=2, Wo=4, Do=8, Vr=16). */
export const Weekdag = {
  Maandag: 1,
  Dinsdag: 2,
  Woensdag: 4,
  Donderdag: 8,
  Vrijdag: 16,
} as const;

export const WEEKDAGEN: { vlag: number; korte: string; lange: string }[] = [
  { vlag: Weekdag.Maandag, korte: 'Ma', lange: 'Maandag' },
  { vlag: Weekdag.Dinsdag, korte: 'Di', lange: 'Dinsdag' },
  { vlag: Weekdag.Woensdag, korte: 'Wo', lange: 'Woensdag' },
  { vlag: Weekdag.Donderdag, korte: 'Do', lange: 'Donderdag' },
  { vlag: Weekdag.Vrijdag, korte: 'Vr', lange: 'Vrijdag' },
];

/** Leeftijdsgroep 0-3 (0=0-1, 1=1-2, 2=2-3, 3=3-4 jaar). */
export const LEEFTIJDSGROEP_LABEL = ['0-1 jr', '1-2 jr', '2-3 jr', '3-4 jr'];

export interface OudercontactDto {
  naam: string;
  telefoon: string;
  email: string;
}

export interface StamgroepDto {
  id: string;
  naam: string;
  maxKinderen: number;
  aantalKinderen: number;
}

export interface KindDto {
  id: string;
  voornaam: string;
  achternaam: string;
  geboortedatum: Iso;
  stamgroepId: string;
  startdatum: Iso;
  einddatum: Iso | null;
  effectieveEinddatum: Iso;
  contracttype: number;
  gewensteOpvangdagen: number;
  wordtBinnenkortVier: boolean;
  mentorId: string | null;
  oudercontacten: OudercontactDto[];
}

export interface KindInvoer {
  voornaam: string;
  achternaam: string;
  geboortedatum: Iso;
  stamgroepId: string;
  startdatum: Iso;
  einddatum: Iso | null;
  contracttype: number;
  gewensteOpvangdagen: number;
  mentorId: string | null;
  oudercontacten: OudercontactDto[];
}

export interface SchoolvakantieDto {
  id: string;
  naam: string;
  schooljaar: number;
  schooljaarLabel: string;
  begindatum: Iso;
  einddatum: Iso;
}

export interface BkrDagDto {
  aantalKinderen: number;
  vereisteHoeveelheidPmers: number | null;
  overschrijdt: boolean;
  melding: string | null;
}

// --- BKR-snelrekenaar (calculator) ---

export interface BkrBerekenInvoer {
  nulTotEen: number;
  eenTotTwee: number;
  tweeTotDrie: number;
  drieTotVier: number;
  aanwezigePmers: number;
}

export interface BkrOnderdeelDto {
  label: string;
  aantalKinderen: number;
  vereistePmers: number;
}

/** status: 'leeg' | 'ok' | 'driehuurs' | 'overschreden'. */
export interface BkrBerekenResultaatDto {
  totaalKinderen: number;
  vereisteHoeveelheidPmers: number;
  aanwezigePmers: number;
  onderdelen: BkrOnderdeelDto[];
  status: string;
  melding: string;
}

export interface AanwezigKindDto {
  id: string;
  voornaam: string;
  achternaam: string;
  stamgroepId: string;
  leeftijdsgroep: number;
  contracttype: number;
}

export interface PlanningBegeleiderDto {
  medewerkerId: string;
  naam: string;
  taakomschrijving: string | null;
}

export interface DagPlanningDto {
  datum: Iso;
  dag: number;
  isSchoolvakantie: boolean;
  kinderen: AanwezigKindDto[];
  bkr: BkrDagDto;
  begeleiders: PlanningBegeleiderDto[];
}

export interface DagFilterDto {
  kinderen: AanwezigKindDto[];
  begeleiders: PlanningBegeleiderDto[];
}

export interface StamgroepWeekDto {
  stamgroepId: string;
  naam: string;
  maxKinderen: number;
  dagen: DagPlanningDto[];
}

export interface WeekplanningDto {
  weekBegin: Iso;
  stamgroepen: StamgroepWeekDto[];
}

export interface MaandPlanningDto {
  jaar: number;
  maand: number;
  weken: WeekplanningDto[];
}

export interface AuthResponse {
  vereistTweeFactor: boolean;
  accessToken: string | null;
  refreshToken: string | null;
  verlooptOpUtc: string | null;
  rol: string | null;
  capabilities: string[];
  stamgroepNaam: string | null;
  weergavenaam: string | null;
}

// --- Fase 6: wachtlijst & plaatsing ---

export const WachtlijstStatus = { Wachtend: 0, Geplaatst: 1, Ingetrokken: 2 } as const;
export const WACHTLIJST_STATUS_LABEL = ['Wachtend', 'Geplaatst', 'Ingetrokken'];

export const VoorstelStatus = { Verstuurd: 0, Geaccepteerd: 1, Afgewezen: 2, Ingetrokken: 3 } as const;
export const VOORSTEL_STATUS_LABEL = ['Verstuurd', 'Geaccepteerd', 'Afgewezen', 'Ingetrokken'];

export interface WachtlijstInschrijvingDto {
  id: string;
  voornaam: string;
  achternaam: string;
  geboortedatum: Iso;
  inschrijfdatumWachtlijst: Iso;
  gewensteStartdatum: Iso;
  gewensteOpvangdagen: number;
  openstaandeDagen: number;
  reedsGeplaatsteDagen: number;
  contracttype: number;
  gewensteStamgroepId: string | null;
  isIntern: boolean;
  handmatigBovenaan: boolean;
  status: number;
  notitie: string | null;
  prioriteitsscore: number;
  prioriteitOnderdelen: string[];
  wordtBinnenkortVier: boolean;
  oudercontact: OudercontactDto | null;
}

export interface WachtlijstInvoer {
  voornaam: string;
  achternaam: string;
  geboortedatum: Iso;
  inschrijfdatumWachtlijst: Iso;
  gewensteStartdatum: Iso;
  gewensteOpvangdagen: number;
  contracttype: number;
  gewensteStamgroepId: string | null;
  isIntern: boolean;
  handmatigBovenaan: boolean;
  notitie: string | null;
  oudercontact: OudercontactDto | null;
}

export interface VoorstelDagAnalyseDto {
  weekdag: number;
  peildatum: Iso;
  aantalAanwezigNu: number;
  vereistePmersNu: number | null;
  aantalAanwezigNa: number;
  vereistePmersNa: number | null;
  extraPmerNodig: boolean;
  plekVrijOpStart: boolean;
  eersteVrijeDatum: Iso | null;
  bkrOverschrijdtNa: boolean;
  melding: string | null;
}

export interface VoorstelAnalyseDto {
  inschrijvingId: string;
  kindNaam: string;
  gewensteStartdatum: Iso;
  gewensteOpvangdagen: number;
  openstaandeDagen: number;
  contracttype: number;
  stamgroepId: string;
  stamgroepNaam: string;
  maxKinderen: number;
  aantalGeplaatstNu: number;
  groepBlijftOnderMax: boolean;
  kandidaatBuitenOpvangleeftijd: boolean;
  kandidaatLeeftijdsgroep: number | null;
  dagen: VoorstelDagAnalyseDto[];
}

export interface VoorstelDagDto {
  weekdag: number;
  voorgesteldeDatum: Iso;
}

export interface VoorstelDagInvoer {
  weekdag: number;
  voorgesteldeDatum: Iso;
}

export interface VoorstelInvoer {
  stamgroepId: string;
  voorgesteldeStartdatum: Iso;
  dagen: number;
  dagData: VoorstelDagInvoer[];
  notitie: string | null;
}

export interface VoorstelDto {
  id: string;
  inschrijvingId: string;
  verstuurdOp: string;
  voorgesteldeStamgroepId: string;
  voorgesteldeDagen: number;
  isDeelvoorstel: boolean;
  status: number;
  beantwoordOp: string | null;
  notitie: string | null;
  dagen: VoorstelDagDto[];
}

// === Sectie E — Contacten (CRM) ===

/** Rondleiding-status: 0=Gepland, 1=Gehad, 2=Geannuleerd. */
export const RondleidingStatus = { Gepland: 0, Gehad: 1, Geannuleerd: 2 } as const;
export const RONDLEIDING_STATUS_LABEL = ['Gepland', 'Gehad', 'Geannuleerd'];

export interface ContactDto {
  id: string;
  voornaam: string;
  achternaam: string;
  volledigeNaam: string;
  telefoon: string | null;
  email: string | null;
  isIntern: boolean;
  aantekeningen: string | null;
  aantalRondleidingen: number;
  aantalInschrijvingen: number;
  aantalGeplaatsteKinderen: number;
}

export interface RondleidingDto {
  id: string;
  datum: Iso;
  status: number;
  notitie: string | null;
}

export interface ContactInschrijvingDto {
  id: string;
  kindNaam: string;
  gewensteStartdatum: Iso;
  status: number;
  aantalVoorstellen: number;
}

export interface ContactKindDto {
  id: string;
  naam: string;
  stamgroepNaam: string;
}

export interface ContactDetailDto {
  id: string;
  voornaam: string;
  achternaam: string;
  telefoon: string | null;
  email: string | null;
  isIntern: boolean;
  aantekeningen: string | null;
  rondleidingen: RondleidingDto[];
  inschrijvingen: ContactInschrijvingDto[];
  geplaatsteKinderen: ContactKindDto[];
}

export interface ContactInvoer {
  voornaam: string;
  achternaam: string;
  telefoon: string | null;
  email: string | null;
  isIntern: boolean;
  aantekeningen: string | null;
}

export interface RondleidingInvoer {
  datum: Iso;
  status: number;
  notitie: string | null;
}

// === Fase 5 — werkrooster, medewerkers, verlof ===

/** Rol numeriek (zoals de enum over de lijn komt). */
export const ROL_LABEL = ['Beheerder', 'Hulpbeheerder', 'Senior', 'Junior', 'Groepsportaal'];

/**
 * Leesbaar rol-label voor de zijbalk, gesleuteld op de rol-NAAM zoals die in de
 * auth-respons/claim staat (enum-naam, bv. "Senior"). Erik wil de functietitel
 * zien i.p.v. een kale rolnaam.
 */
export const ROL_WEERGAVE: Record<string, string> = {
  Beheerder: 'Beheerder',
  Hulpbeheerder: 'Hulpbeheerder',
  Senior: 'Senior Medewerker',
  Junior: 'Junior Medewerker',
  Groepsportaal: 'Groepsportaal',
};

/** Verlofcategorie: 0=Vakantieuren, 1=Verlofbudget. */
export const VerlofCategorie = { Vakantieuren: 0, Verlofbudget: 1 } as const;
export const VERLOFCATEGORIE_LABEL = ['Vakantieuren', 'Verlofbudget'];

/** Verlofstatus: 0=Openstaand, 1=Goedgekeurd, 2=Afgekeurd. */
export const VerlofStatus = { Openstaand: 0, Goedgekeurd: 1, Afgekeurd: 2 } as const;
export const VERLOFSTATUS_LABEL = ['Openstaand', 'Goedgekeurd', 'Afgekeurd'];

/** Roosterstatus: 0=Concept, 1=Verstuurd. */
export const RoosterStatus = { Concept: 0, Verstuurd: 1 } as const;

/** Dienstsoort: 0=Regulier, 1=Vroege (openen), 2=Late (sluiten). */
export const Dienstsoort = { Regulier: 0, Vroege: 1, Late: 2 } as const;
export const DIENSTSOORT_LABEL = ['Regulier', 'Vroege dienst', 'Late dienst'];
export const DIENSTSOORT_KORT = ['', 'Vroeg', 'Laat'];

/** BKR-indicatorkleur: 0=Groen, 1=Oranje, 2=Rood. */
export const BkrIndicatorKleur = { Groen: 0, Oranje: 1, Rood: 2 } as const;

/** Roostercelkleur: 0=Leeg, 1=Standaard, 2=VerlofAangevraagd, 3=VerlofGoedgekeurd, 4=Ziek. */
export const RoosterCelKleur = {
  Leeg: 0,
  Standaard: 1,
  VerlofAangevraagd: 2,
  VerlofGoedgekeurd: 3,
  Ziek: 4,
} as const;

export interface MedewerkerDto {
  id: string;
  voornaam: string;
  achternaam: string;
  rol: number;
  vasteWerkdagen: number;
  beschikbaarheidsdagen: number;
  contracturen: number;
  vasteStamgroepId: string | null;
  vasteStamgroepNaam: string | null;
  telefoon: string | null;
  email: string | null;
  noodcontactNaam: string | null;
  noodcontactTelefoon: string | null;
  contractVast: boolean;
  contractbegindatum: Iso | null;
  contracteinddatum: Iso | null;
  resterendeContractmaanden: number | null;
}

export interface MedewerkerInvoer {
  voornaam: string;
  achternaam: string;
  rol: number;
  vasteWerkdagen: number;
  beschikbaarheidsdagen: number;
  contracturen: number;
  vasteStamgroepId: string | null;
  telefoon?: string | null;
  email?: string | null;
  noodcontactNaam?: string | null;
  noodcontactTelefoon?: string | null;
  contractVast?: boolean;
  contractbegindatum?: Iso | null;
  contracteinddatum?: Iso | null;
}

export interface UrenWeekDto {
  weekBegin: Iso;
  gewerkteUren: number;
  aantalSessies: number;
}

export interface UrenoverzichtDto {
  van: Iso;
  tot: Iso;
  gewerkteUren: number;
  verwachteUren: number;
  meerMinderUren: number;
  aantalSessies: number;
  perWeek: UrenWeekDto[];
}

export interface UrencorrectieInvoer {
  ingeklokt: string;
  uitgeklokt: string | null;
}

export interface VerlofaanvraagDto {
  id: string;
  medewerkerId: string;
  medewerkerNaam: string;
  begindatum: Iso;
  einddatum: Iso;
  aantalUren: number;
  categorie: number;
  status: number;
  reden: string | null;
  beoordelingsNotitie: string | null;
  aangevraagdOp: string;
  beoordeeldOp: string | null;
}

export interface VerlofAanvraagInvoer {
  medewerkerId: string;
  begindatum: Iso;
  einddatum: Iso;
  aantalUren: number;
  categorie: number;
  reden: string | null;
}

export interface VerlofsaldoDto {
  medewerkerId: string;
  categorie: number;
  toegekend: number;
  gebruikt: number;
  gereserveerd: number;
  resterend: number;
  resterendNaReservering: number;
  vervaldatum: Iso | null;
}

export interface VerlofsaldoInvoer {
  medewerkerId: string;
  categorie: number;
  toegekendeUren: number;
  vervaldatum: Iso | null;
}

export interface ZiekmeldingDto {
  id: string;
  medewerkerId: string;
  medewerkerNaam: string;
  begindatum: Iso;
  einddatum: Iso | null;
}

export interface ZiekmeldingInvoer {
  medewerkerId: string;
  begindatum: Iso;
  einddatum: Iso | null;
}

export interface RoosterCelDto {
  datum: Iso;
  dag: number;
  kleur: number;
  dienstId: string | null;
  taakomschrijving: string | null;
  urencorrectieKwartieren: number;
  dienstsoort: number;
}

export interface RoosterMedewerkerRijDto {
  medewerkerId: string;
  naam: string;
  cellen: RoosterCelDto[];
}

export interface RoosterDagIndicatorDto {
  datum: Iso;
  dag: number;
  aantalKinderen: number;
  nodigPmers: number | null;
  ingeplandPmers: number;
  overschrijdt: boolean;
  kleur: number;
}

export interface RoosterGroepDto {
  stamgroepId: string;
  naam: string;
  indicatoren: RoosterDagIndicatorDto[];
  rijen: RoosterMedewerkerRijDto[];
}

export interface VerstuurdRoosterDto {
  id: string;
  weekBegin: Iso;
  verstuurdOp: string | null;
  aantalDiensten: number;
}

export interface RoosterWeekDto {
  weekBegin: Iso;
  bestaat: boolean;
  roosterweekId: string | null;
  status: number | null;
  verstuurdOp: string | null;
  groepen: RoosterGroepDto[];
}

export interface DienstInvoer {
  taakomschrijving: string | null;
  urencorrectieKwartieren: number;
  dienstsoort: number;
}

// === Fase 8 — portalen (groepsportaal & thuis-portaal) ===

/** Capability-sleutels (spiegelt KinderKompas.Domain.Autorisatie.Capabilities). */
export const Capabilities = {
  OudergegevensZien: 'MagOudergegevensZien',
  KinderenBeheren: 'MagKinderenBeheren',
  KinderenLezen: 'MagKinderenLezen',
  PlanningZien: 'MagPlanningZien',
  WachtlijstBeheren: 'MagWachtlijstBeheren',
  RoosterBeheren: 'MagRoosterBeheren',
  RoosterVersturen: 'MagRoosterVersturen',
  ObservatiesVersturen: 'MagObservatiesVersturen',
  MedewerkersBeheren: 'MagMedewerkersBeheren',
  InstellingenBeheren: 'MagInstellingenBeheren',
  DashboardZien: 'MagDashboardZien',
  GroepsportaalGebruiken: 'MagGroepsportaalGebruiken',
  ThuisportaalGebruiken: 'MagThuisportaalGebruiken',
} as const;

// --- Thuis-portaal ---

export interface ThuisRoosterDagDto {
  datum: Iso;
  dag: number;
  stamgroepId: string;
  stamgroepNaam: string;
  taakomschrijving: string | null;
  urencorrectieKwartieren: number;
  dienstsoort: number;
}

export interface ThuisRoosterDto {
  weekBegin: Iso;
  verstuurd: boolean;
  verstuurdOp: string | null;
  dagen: ThuisRoosterDagDto[];
}

export interface BeschikbaarheidDto {
  medewerkerId: string;
  vasteWerkdagen: number;
  beschikbaarheidsdagen: number;
}

export interface ThuisVerlofInvoer {
  begindatum: Iso;
  einddatum: Iso;
  aantalUren: number;
  categorie: number;
  reden: string | null;
}

// --- Groepsportaal ---

export interface UrenregistratieDto {
  id: string;
  medewerkerId: string;
  medewerkerNaam: string;
  datum: Iso;
  roosterdienstId: string | null;
  stamgroepId: string | null;
  stamgroepNaam: string | null;
  ingeklokt: string;
  uitgeklokt: string | null;
  isOpen: boolean;
  gewerkteKwartieren: number;
}

export interface DagdienstDto {
  dienstId: string;
  medewerkerId: string;
  medewerkerNaam: string;
  stamgroepId: string;
  stamgroepNaam: string;
  taakomschrijving: string | null;
  dienstsoort: number;
}

export interface GroepsportaalDagDto {
  datum: Iso;
  roosterVerstuurd: boolean;
  diensten: DagdienstDto[];
}

export interface GroepsportaalDashboardDto {
  datum: Iso;
  stamgroepNaam: string | null;
  kinderenInGroep: number;
  aanwezigVandaag: number;
  medewerkersVandaag: number;
  ingeklokt: number;
  observatiesOpen: number;
}

export interface PortaalMedewerkerDto {
  id: string;
  naam: string;
}

export interface InklokInvoer {
  medewerkerId: string;
  roosterdienstId: string | null;
  stamgroepId: string | null;
  wachtwoord?: string | null;
}

// === Fase 9 — actiecentrum, dashboard & instellingen ===

/** Meldingsoort (0..5), zoals de enum over de lijn komt. */
export const MeldingSoort = {
  BkrWaarschuwing: 0,
  Observatieherinnering: 1,
  Verlofaanvraag: 2,
  Ziekmelding: 3,
  VoorstelGeaccepteerd: 4,
  NieuweWachtlijstaanmelding: 5,
} as const;

export const MELDING_SOORT_LABEL = [
  'BKR-waarschuwing',
  'Observatie',
  'Verlofaanvraag',
  'Ziekmelding',
  'Plaatsing',
  'Wachtlijst',
];

export const MELDING_SOORT_ICOON = ['⚠️', '📋', '🌴', '🤒', '✅', '📝'];

/** Meldingstatus: 0=Ongelezen, 1=Gelezen, 2=Afgehandeld. */
export const MeldingStatus = { Ongelezen: 0, Gelezen: 1, Afgehandeld: 2 } as const;

export interface MeldingDto {
  id: string;
  soort: number;
  status: number;
  vereistActie: boolean;
  isOpenToDo: boolean;
  titel: string;
  tekst: string;
  bronType: string | null;
  bronId: string | null;
  aangemaaktOp: string;
  afgehandeldOp: string | null;
}

export interface MeldingTellingenDto {
  ongelezen: number;
  openToDos: number;
}

export interface DashboardGroepDto {
  stamgroepId: string;
  naam: string;
  aantalKinderen: number;
  vereistePmers: number | null;
  ingeplandePmers: number;
  bovenMaximum: boolean;
  onderbezet: boolean;
}

export interface BkrBadgeDto {
  isOpvangdag: boolean;
  overschrijding: boolean;
  aantalGroepenInOrde: number;
  aantalGroepen: number;
}

export interface ActiviteitDto {
  id: string;
  soort: number;
  titel: string;
  tekst: string;
  op: string;
}

export interface DashboardDto {
  datum: Iso;
  isOpvangdag: boolean;
  totaalKinderenVandaag: number;
  roosterVerstuurd: boolean;
  totaalMedewerkersVandaag: number;
  aantalKinderenBinnenkortVier: number;
  bkr: BkrBadgeDto;
  groepen: DashboardGroepDto[];
  wachtlijst: { aantalWachtend: number };
  observaties: { overschreden: number; binnenkort: number };
  actiecentrum: { openToDos: number; ongelezenMeldingen: number };
  recenteActiviteit: ActiviteitDto[];
}

// --- Instellingen (9c) ---

export interface InstellingenDto {
  verborgenMeldingsoorten: number[];
  observatieBinnenkortDrempelDagen: number;
  kindBinnenkortVierDrempelDagen: number;
  standaardObservatietekst: string | null;
  prioriteitInternGewicht: number;
  prioriteitPerMaandGewicht: number;
}

export type InstellingenInvoer = InstellingenDto;

export interface LocatieDto {
  naam: string;
  lrknummer: string;
}

export interface CapabilityInfoDto {
  sleutel: string;
  omschrijving: string;
}

export interface RolRechtenDto {
  rol: number;
  capabilities: string[];
}

export interface RechtenMatrixDto {
  capabilities: CapabilityInfoDto[];
  rollen: RolRechtenDto[];
}

export interface SchoolvakantieInvoer {
  naam: string;
  schooljaar: number;
  begindatum: Iso;
  einddatum: Iso;
}
