import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, apiBlob } from './client';
import type {
  ContactDto,
  ContactDetailDto,
  ContactInvoer,
  UrenoverzichtDto,
  UrencorrectieInvoer,
  RondleidingDto,
  RondleidingInvoer,
  DagFilterDto,
  DagplaatsingDto,
  DagplaatsingInvoer,
  MaandPlanningDto,
  BkrBerekenInvoer,
  BkrBerekenResultaatDto,
  BeschikbaarheidDto,
  DashboardDto,
  DienstInvoer,
  InstellingenDto,
  InstellingenInvoer,
  KennisbankItemDto,
  KennisbankDocumentDto,
  KennisbankBijlageDto,
  KennisbankInvoer,
  LocatieDto,
  MeldingDto,
  MeldingTellingenDto,
  RechtenMatrixDto,
  RolRechtenDto,
  SchoolvakantieInvoer,
  GroepsportaalDagDto,
  GroepsportaalDashboardDto,
  InklokInvoer,
  KindDto,
  KindInvoer,
  MedewerkerDto,
  MedewerkerInvoer,
  PortaalMedewerkerDto,
  RoosterWeekDto,
  VerstuurdRoosterDto,
  SchoolvakantieDto,
  StamgroepDto,
  ThuisRoosterDto,
  ThuisVerlofInvoer,
  UrenregistratieDto,
  VerlofAanvraagInvoer,
  VerlofaanvraagDto,
  VerlofsaldoDto,
  VerlofsaldoInvoer,
  VoorstelAnalyseDto,
  VoorstelDto,
  VoorstelInvoer,
  WachtlijstInschrijvingDto,
  WachtlijstInvoer,
  WeekplanningDto,
  ZiekmeldingDto,
  ZiekmeldingInvoer,
} from '../types';

// --- Stamgroepen ---
export function useStamgroepen() {
  return useQuery({
    queryKey: ['stamgroepen'],
    queryFn: () => api<StamgroepDto[]>('/api/stamgroepen'),
  });
}

export function useStamgroepMutaties() {
  const qc = useQueryClient();
  const invalideer = () => qc.invalidateQueries({ queryKey: ['stamgroepen'] });

  const aanmaken = useMutation({
    mutationFn: (invoer: { naam: string; maxKinderen: number }) =>
      api<StamgroepDto>('/api/stamgroepen', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const bewerken = useMutation({
    mutationFn: ({ id, invoer }: { id: string; invoer: { naam: string; maxKinderen: number } }) =>
      api<StamgroepDto>(`/api/stamgroepen/${id}`, { method: 'PUT', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const verwijderen = useMutation({
    // Verwijderen vereist het wachtwoord van de beheerder ter bevestiging.
    mutationFn: ({ id, wachtwoord }: { id: string; wachtwoord: string }) =>
      api<void>(`/api/stamgroepen/${id}`, { method: 'DELETE', body: JSON.stringify({ wachtwoord }) }),
    onSuccess: invalideer,
  });
  return { aanmaken, bewerken, verwijderen };
}

// --- Kinderen ---
export function useKinderen(stamgroepId?: string) {
  const query = stamgroepId ? `?stamgroepId=${stamgroepId}` : '';
  return useQuery({
    queryKey: ['kinderen', stamgroepId ?? 'alle'],
    queryFn: () => api<KindDto[]>(`/api/kinderen${query}`),
  });
}

export function useKind(id: string | undefined) {
  return useQuery({
    queryKey: ['kind', id],
    queryFn: () => api<KindDto>(`/api/kinderen/${id}`),
    enabled: !!id,
  });
}

export function useKindMutaties() {
  const qc = useQueryClient();
  const invalideer = () => {
    qc.invalidateQueries({ queryKey: ['kinderen'] });
    qc.invalidateQueries({ queryKey: ['weekplanning'] });
    qc.invalidateQueries({ queryKey: ['stamgroepen'] });
  };

  const aanmaken = useMutation({
    mutationFn: (invoer: KindInvoer) =>
      api<KindDto>('/api/kinderen', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const bewerken = useMutation({
    mutationFn: ({ id, invoer }: { id: string; invoer: KindInvoer }) =>
      api<KindDto>(`/api/kinderen/${id}`, { method: 'PUT', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const verwijderen = useMutation({
    // Verwijderen vereist het wachtwoord van de beheerder ter bevestiging (2-stapscheck).
    mutationFn: ({ id, wachtwoord }: { id: string; wachtwoord: string }) =>
      api<void>(`/api/kinderen/${id}`, { method: 'DELETE', body: JSON.stringify({ wachtwoord }) }),
    onSuccess: invalideer,
  });
  return { aanmaken, bewerken, verwijderen };
}

// --- Planning ---
export function useWeekplanning(datum: string) {
  return useQuery({
    queryKey: ['weekplanning', datum],
    queryFn: () => api<WeekplanningDto>(`/api/planning/week?datum=${datum}`),
  });
}

export function useMaandplanning(datum: string) {
  return useQuery({
    queryKey: ['maandplanning', datum],
    queryFn: () => api<MaandPlanningDto>(`/api/planning/maand?datum=${datum}`),
  });
}

export function useDagplanning(datum: string, stamgroepId?: string) {
  const params = new URLSearchParams({ datum });
  if (stamgroepId) params.set('stamgroepId', stamgroepId);
  return useQuery({
    queryKey: ['dagplanning', datum, stamgroepId ?? 'alle'],
    queryFn: () => api<DagFilterDto>(`/api/planning/dag?${params.toString()}`),
  });
}

// --- Dagplaatsingen (dagafwijkingen: ruildag, incidenteel, extra dag, afwezig) ---
export function useDagplaatsingen(van: string, tot: string) {
  return useQuery({
    queryKey: ['dagplaatsingen', van, tot],
    queryFn: () => api<DagplaatsingDto[]>(`/api/dagplaatsingen?van=${van}&tot=${tot}`),
  });
}

export function useDagplaatsingMutaties() {
  const qc = useQueryClient();
  // Een dagafwijking verschuift de planning en BKR per dag: alle afgeleide views verversen.
  const invalideer = () => {
    qc.invalidateQueries({ queryKey: ['dagplaatsingen'] });
    qc.invalidateQueries({ queryKey: ['weekplanning'] });
    qc.invalidateQueries({ queryKey: ['maandplanning'] });
    qc.invalidateQueries({ queryKey: ['dagplanning'] });
  };
  return {
    zet: useMutation({
      mutationFn: (invoer: DagplaatsingInvoer) =>
        api<DagplaatsingDto>('/api/dagplaatsingen', { method: 'POST', body: JSON.stringify(invoer) }),
      onSuccess: invalideer,
    }),
    verwijder: useMutation({
      mutationFn: (id: string) => api<void>(`/api/dagplaatsingen/${id}`, { method: 'DELETE' }),
      onSuccess: invalideer,
    }),
  };
}

// --- Schoolvakanties ---
export function useSchoolvakanties() {
  return useQuery({
    queryKey: ['schoolvakanties'],
    queryFn: () => api<SchoolvakantieDto[]>('/api/schoolvakanties'),
  });
}

// --- Contacten (CRM) ---
export function useContacten() {
  return useQuery({
    queryKey: ['contacten'],
    queryFn: () => api<ContactDto[]>('/api/contacten'),
  });
}

export function useContact(id: string | undefined) {
  return useQuery({
    queryKey: ['contact', id],
    enabled: !!id,
    queryFn: () => api<ContactDetailDto>(`/api/contacten/${id}`),
  });
}

export function useContactMutaties() {
  const qc = useQueryClient();
  const invalideer = () => {
    qc.invalidateQueries({ queryKey: ['contacten'] });
    qc.invalidateQueries({ queryKey: ['contact'] });
  };
  const aanmaken = useMutation({
    mutationFn: (invoer: ContactInvoer) =>
      api<ContactDto>('/api/contacten', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const bewerken = useMutation({
    mutationFn: ({ id, invoer }: { id: string; invoer: ContactInvoer }) =>
      api<ContactDto>(`/api/contacten/${id}`, { method: 'PUT', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const verwijderen = useMutation({
    // 2-stapscheck: bevestigen met het beheerder-wachtwoord.
    mutationFn: ({ id, wachtwoord }: { id: string; wachtwoord: string }) =>
      api<void>(`/api/contacten/${id}`, { method: 'DELETE', body: JSON.stringify({ wachtwoord }) }),
    onSuccess: invalideer,
  });
  const rondleidingToevoegen = useMutation({
    mutationFn: ({ id, invoer }: { id: string; invoer: RondleidingInvoer }) =>
      api<RondleidingDto>(`/api/contacten/${id}/rondleidingen`, { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const rondleidingVerwijderen = useMutation({
    mutationFn: (rondleidingId: string) =>
      api<void>(`/api/contacten/rondleidingen/${rondleidingId}`, { method: 'DELETE' }),
    onSuccess: invalideer,
  });
  return { aanmaken, bewerken, verwijderen, rondleidingToevoegen, rondleidingVerwijderen };
}

// --- Wachtlijst ---
export function useWachtlijst(toonGeplaatst = false) {
  return useQuery({
    queryKey: ['wachtlijst', toonGeplaatst],
    queryFn: () =>
      api<WachtlijstInschrijvingDto[]>(`/api/wachtlijst?toonGeplaatst=${toonGeplaatst}`),
  });
}

export function useWachtlijstInschrijving(id: string | undefined) {
  return useQuery({
    queryKey: ['wachtlijst-inschrijving', id],
    queryFn: () => api<WachtlijstInschrijvingDto>(`/api/wachtlijst/${id}`),
    enabled: !!id,
  });
}

export function useWachtlijstMutaties() {
  const qc = useQueryClient();
  const invalideer = () => qc.invalidateQueries({ queryKey: ['wachtlijst'] });

  const aanmaken = useMutation({
    mutationFn: (invoer: WachtlijstInvoer) =>
      api<WachtlijstInschrijvingDto>('/api/wachtlijst', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const bewerken = useMutation({
    mutationFn: ({ id, invoer }: { id: string; invoer: WachtlijstInvoer }) =>
      api<WachtlijstInschrijvingDto>(`/api/wachtlijst/${id}`, { method: 'PUT', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const verwijderen = useMutation({
    mutationFn: (id: string) => api<void>(`/api/wachtlijst/${id}`, { method: 'DELETE' }),
    onSuccess: invalideer,
  });
  const bovenaan = useMutation({
    mutationFn: ({ id, bovenaan }: { id: string; bovenaan: boolean }) =>
      api<WachtlijstInschrijvingDto>(`/api/wachtlijst/${id}/bovenaan`, {
        method: 'POST',
        body: JSON.stringify({ bovenaan }),
      }),
    onSuccess: invalideer,
  });
  return { aanmaken, bewerken, verwijderen, bovenaan };
}

// --- Voorstellen ---
export function useVoorstelAnalyse(
  inschrijvingId: string,
  stamgroepId: string | undefined,
  startdatum: string | undefined,
) {
  const params = new URLSearchParams();
  if (stamgroepId) params.set('stamgroepId', stamgroepId);
  if (startdatum) params.set('startdatum', startdatum);
  const qs = params.toString();
  return useQuery({
    queryKey: ['voorstel-analyse', inschrijvingId, stamgroepId ?? '', startdatum ?? ''],
    queryFn: () =>
      api<VoorstelAnalyseDto>(`/api/wachtlijst/${inschrijvingId}/voorstel-analyse${qs ? `?${qs}` : ''}`),
    enabled: !!inschrijvingId && !!stamgroepId,
  });
}

export function useVoorstelhistorie(inschrijvingId: string | undefined) {
  return useQuery({
    queryKey: ['voorstelhistorie', inschrijvingId],
    queryFn: () => api<VoorstelDto[]>(`/api/wachtlijst/${inschrijvingId}/voorstellen`),
    enabled: !!inschrijvingId,
  });
}

export function useVoorstelMutaties(inschrijvingId: string) {
  const qc = useQueryClient();
  const invalideer = () => {
    qc.invalidateQueries({ queryKey: ['wachtlijst'] });
    qc.invalidateQueries({ queryKey: ['voorstelhistorie', inschrijvingId] });
  };

  const versturen = useMutation({
    mutationFn: (invoer: VoorstelInvoer) =>
      api<VoorstelDto>(`/api/wachtlijst/${inschrijvingId}/voorstellen`, {
        method: 'POST',
        body: JSON.stringify(invoer),
      }),
    onSuccess: invalideer,
  });
  const accepteren = useMutation({
    mutationFn: (voorstelId: string) =>
      api<WachtlijstInschrijvingDto>(`/api/wachtlijst/voorstellen/${voorstelId}/accepteren`, { method: 'POST' }),
    onSuccess: invalideer,
  });
  const afwijzen = useMutation({
    mutationFn: (voorstelId: string) =>
      api<VoorstelDto>(`/api/wachtlijst/voorstellen/${voorstelId}/afwijzen`, { method: 'POST' }),
    onSuccess: invalideer,
  });
  return { versturen, accepteren, afwijzen };
}

// --- Medewerkers ---
export function useMedewerkers() {
  return useQuery({
    queryKey: ['medewerkers'],
    queryFn: () => api<MedewerkerDto[]>('/api/medewerkers'),
  });
}

export function useMedewerkerUren(id: string | undefined, van?: string, tot?: string) {
  const p = new URLSearchParams();
  if (van) p.set('van', van);
  if (tot) p.set('tot', tot);
  const qs = p.toString();
  return useQuery({
    queryKey: ['medewerker-uren', id, van ?? '', tot ?? ''],
    enabled: !!id,
    queryFn: () => api<UrenoverzichtDto>(`/api/medewerkers/${id}/uren${qs ? `?${qs}` : ''}`),
  });
}

export function useUrencorrectie() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ registratieId, invoer }: { registratieId: string; invoer: UrencorrectieInvoer }) =>
      api<UrenregistratieDto>(`/api/medewerkers/uren/${registratieId}/corrigeer`, {
        method: 'PUT',
        body: JSON.stringify(invoer),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['medewerker-uren'] });
      qc.invalidateQueries({ queryKey: ['groep-uren'] });
    },
  });
}

export function useMedewerkerMutaties() {
  const qc = useQueryClient();
  const invalideer = () => {
    qc.invalidateQueries({ queryKey: ['medewerkers'] });
    qc.invalidateQueries({ queryKey: ['rooster'] });
  };

  const aanmaken = useMutation({
    mutationFn: (invoer: MedewerkerInvoer) =>
      api<MedewerkerDto>('/api/medewerkers', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const bewerken = useMutation({
    mutationFn: ({ id, invoer }: { id: string; invoer: MedewerkerInvoer }) =>
      api<MedewerkerDto>(`/api/medewerkers/${id}`, { method: 'PUT', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const verwijderen = useMutation({
    mutationFn: (id: string) => api<void>(`/api/medewerkers/${id}`, { method: 'DELETE' }),
    onSuccess: invalideer,
  });
  return { aanmaken, bewerken, verwijderen };
}

// --- Verlof ---
export function useVerlof(status?: number, medewerkerId?: string) {
  const params = new URLSearchParams();
  if (status !== undefined) params.set('status', String(status));
  if (medewerkerId) params.set('medewerkerId', medewerkerId);
  const qs = params.toString();
  return useQuery({
    queryKey: ['verlof', status ?? 'alle', medewerkerId ?? 'alle'],
    queryFn: () => api<VerlofaanvraagDto[]>(`/api/verlof${qs ? `?${qs}` : ''}`),
  });
}

export function useVerlofsaldo(medewerkerId: string | undefined) {
  return useQuery({
    queryKey: ['verlofsaldo', medewerkerId],
    queryFn: () => api<VerlofsaldoDto[]>(`/api/verlof/saldo?medewerkerId=${medewerkerId}`),
    enabled: !!medewerkerId,
  });
}

export function useVerlofMutaties() {
  const qc = useQueryClient();
  const invalideer = () => {
    qc.invalidateQueries({ queryKey: ['verlof'] });
    qc.invalidateQueries({ queryKey: ['verlofsaldo'] });
    qc.invalidateQueries({ queryKey: ['rooster'] });
  };

  const aanvragen = useMutation({
    mutationFn: (invoer: VerlofAanvraagInvoer) =>
      api<VerlofaanvraagDto>('/api/verlof', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const goedkeuren = useMutation({
    mutationFn: (id: string) => api<VerlofaanvraagDto>(`/api/verlof/${id}/goedkeuren`, { method: 'POST' }),
    onSuccess: invalideer,
  });
  const afkeuren = useMutation({
    mutationFn: ({ id, notitie }: { id: string; notitie: string | null }) =>
      api<VerlofaanvraagDto>(`/api/verlof/${id}/afkeuren`, { method: 'POST', body: JSON.stringify({ notitie }) }),
    onSuccess: invalideer,
  });
  const intrekken = useMutation({
    mutationFn: (id: string) => api<void>(`/api/verlof/${id}`, { method: 'DELETE' }),
    onSuccess: invalideer,
  });
  const saldoInstellen = useMutation({
    mutationFn: (invoer: VerlofsaldoInvoer) =>
      api<VerlofsaldoDto>('/api/verlof/saldo', { method: 'PUT', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  return { aanvragen, goedkeuren, afkeuren, intrekken, saldoInstellen };
}

// --- Ziekmeldingen ---
export function useZiekmeldingen(medewerkerId?: string) {
  const qs = medewerkerId ? `?medewerkerId=${medewerkerId}` : '';
  return useQuery({
    queryKey: ['ziekmeldingen', medewerkerId ?? 'alle'],
    queryFn: () => api<ZiekmeldingDto[]>(`/api/ziekmeldingen${qs}`),
  });
}

export function useZiekmeldingMutaties() {
  const qc = useQueryClient();
  const invalideer = () => {
    qc.invalidateQueries({ queryKey: ['ziekmeldingen'] });
    qc.invalidateQueries({ queryKey: ['rooster'] });
  };

  const registreren = useMutation({
    mutationFn: (invoer: ZiekmeldingInvoer) =>
      api<ZiekmeldingDto>('/api/ziekmeldingen', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const herstel = useMutation({
    mutationFn: ({ id, einddatum }: { id: string; einddatum: string }) =>
      api<ZiekmeldingDto>(`/api/ziekmeldingen/${id}/herstel`, { method: 'POST', body: JSON.stringify({ einddatum }) }),
    onSuccess: invalideer,
  });
  const verwijderen = useMutation({
    mutationFn: (id: string) => api<void>(`/api/ziekmeldingen/${id}`, { method: 'DELETE' }),
    onSuccess: invalideer,
  });
  return { registreren, herstel, verwijderen };
}

// --- Rooster ---
export function useRooster(datum: string) {
  return useQuery({
    queryKey: ['rooster', datum],
    queryFn: () => api<RoosterWeekDto>(`/api/rooster?datum=${datum}`),
  });
}

export function useVerstuurdeRoosters(van?: string, tot?: string) {
  const p = new URLSearchParams();
  if (van) p.set('van', van);
  if (tot) p.set('tot', tot);
  const qs = p.toString();
  return useQuery({
    queryKey: ['verstuurde-roosters', van ?? '', tot ?? ''],
    queryFn: () => api<VerstuurdRoosterDto[]>(`/api/rooster/verstuurd${qs ? `?${qs}` : ''}`),
  });
}

export function useRoosterMutaties() {
  const qc = useQueryClient();
  const invalideer = () => {
    qc.invalidateQueries({ queryKey: ['rooster'] });
    qc.invalidateQueries({ queryKey: ['verstuurde-roosters'] });
  };

  const genereren = useMutation({
    mutationFn: (datum: string) =>
      api<RoosterWeekDto>(`/api/rooster/genereer?datum=${datum}`, { method: 'POST' }),
    onSuccess: invalideer,
  });
  const versturen = useMutation({
    mutationFn: (roosterweekId: string) =>
      api<RoosterWeekDto>(`/api/rooster/${roosterweekId}/versturen`, { method: 'POST' }),
    onSuccess: invalideer,
  });
  const herroepen = useMutation({
    mutationFn: (roosterweekId: string) =>
      api<RoosterWeekDto>(`/api/rooster/${roosterweekId}/herroepen`, { method: 'POST' }),
    onSuccess: invalideer,
  });
  const dienstBijwerken = useMutation({
    mutationFn: ({ id, invoer }: { id: string; invoer: DienstInvoer }) =>
      api<void>(`/api/rooster/dienst/${id}`, { method: 'PUT', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const dienstToevoegen = useMutation({
    mutationFn: (invoer: { medewerkerId: string; stamgroepId: string; datum: string }) =>
      api<void>('/api/rooster/dienst', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const dienstVerwijderen = useMutation({
    mutationFn: (id: string) => api<void>(`/api/rooster/dienst/${id}`, { method: 'DELETE' }),
    onSuccess: invalideer,
  });
  return { genereren, versturen, herroepen, dienstBijwerken, dienstToevoegen, dienstVerwijderen };
}

// === Fase 8 — Thuis-portaal ===
export function useThuisRooster(datum: string) {
  return useQuery({
    queryKey: ['thuis-rooster', datum],
    queryFn: () => api<ThuisRoosterDto>(`/api/thuisportaal/rooster?datum=${datum}`),
  });
}

export function useBeschikbaarheid() {
  return useQuery({
    queryKey: ['thuis-beschikbaarheid'],
    queryFn: () => api<BeschikbaarheidDto>('/api/thuisportaal/beschikbaarheid'),
  });
}

export function useThuisVerlof() {
  return useQuery({
    queryKey: ['thuis-verlof'],
    queryFn: () => api<VerlofaanvraagDto[]>('/api/thuisportaal/verlof'),
  });
}

export function useThuisSaldo() {
  return useQuery({
    queryKey: ['thuis-saldo'],
    queryFn: () => api<VerlofsaldoDto[]>('/api/thuisportaal/saldo'),
  });
}

export function useThuisUren(van?: string, tot?: string) {
  const params = new URLSearchParams();
  if (van) params.set('van', van);
  if (tot) params.set('tot', tot);
  const qs = params.toString();
  return useQuery({
    queryKey: ['thuis-uren', van ?? '', tot ?? ''],
    queryFn: () => api<UrenregistratieDto[]>(`/api/thuisportaal/uren${qs ? `?${qs}` : ''}`),
  });
}

export function useThuisUrenoverzicht(van?: string, tot?: string) {
  const params = new URLSearchParams();
  if (van) params.set('van', van);
  if (tot) params.set('tot', tot);
  const qs = params.toString();
  return useQuery({
    queryKey: ['thuis-urenoverzicht', van ?? '', tot ?? ''],
    queryFn: () => api<UrenoverzichtDto>(`/api/thuisportaal/urenoverzicht${qs ? `?${qs}` : ''}`),
  });
}

export function useThuisMutaties() {
  const qc = useQueryClient();

  const beschikbaarheidBijwerken = useMutation({
    mutationFn: (beschikbaarheidsdagen: number) =>
      api<BeschikbaarheidDto>('/api/thuisportaal/beschikbaarheid', {
        method: 'PUT',
        body: JSON.stringify({ beschikbaarheidsdagen }),
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['thuis-beschikbaarheid'] }),
  });
  const verlofAanvragen = useMutation({
    mutationFn: (invoer: ThuisVerlofInvoer) =>
      api<VerlofaanvraagDto>('/api/thuisportaal/verlof', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['thuis-verlof'] });
      qc.invalidateQueries({ queryKey: ['thuis-saldo'] });
    },
  });
  const verlofIntrekken = useMutation({
    mutationFn: (id: string) => api<void>(`/api/thuisportaal/verlof/${id}`, { method: 'DELETE' }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['thuis-verlof'] });
      qc.invalidateQueries({ queryKey: ['thuis-saldo'] });
    },
  });
  return { beschikbaarheidBijwerken, verlofAanvragen, verlofIntrekken };
}

// === Fase 8 — Groepsportaal ===
export function useGroepsportaalDashboard(datum: string) {
  return useQuery({
    queryKey: ['groep-dashboard', datum],
    queryFn: () => api<GroepsportaalDashboardDto>(`/api/groepsportaal/dashboard?datum=${datum}`),
  });
}

export function useGroepsportaalDienst(datum: string) {
  return useQuery({
    queryKey: ['groep-dienst', datum],
    queryFn: () => api<GroepsportaalDagDto>(`/api/groepsportaal/dienst?datum=${datum}`),
  });
}

export function useGroepsportaalMedewerkers() {
  return useQuery({
    queryKey: ['groep-medewerkers'],
    queryFn: () => api<PortaalMedewerkerDto[]>('/api/groepsportaal/medewerkers'),
  });
}

export function useGroepsportaalKinderen(stamgroepId?: string) {
  const qs = stamgroepId ? `?stamgroepId=${stamgroepId}` : '';
  return useQuery({
    queryKey: ['groep-kinderen', stamgroepId ?? 'alle'],
    queryFn: () => api<KindDto[]>(`/api/groepsportaal/kinderen${qs}`),
  });
}

export function useGroepsportaalUren(datum: string) {
  return useQuery({
    queryKey: ['groep-uren', datum],
    queryFn: () => api<UrenregistratieDto[]>(`/api/groepsportaal/urenregistratie?datum=${datum}`),
  });
}

export function useGroepsportaalMutaties() {
  const qc = useQueryClient();
  const invalideer = () => qc.invalidateQueries({ queryKey: ['groep-uren'] });

  const inklokken = useMutation({
    mutationFn: (invoer: InklokInvoer) =>
      api<UrenregistratieDto>('/api/groepsportaal/inklokken', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const uitklokken = useMutation({
    // uitgeklokt: optionele ISO-tijd (UTC); zonder telt "nu".
    mutationFn: ({ id, uitgeklokt }: { id: string; uitgeklokt?: string }) =>
      api<UrenregistratieDto>(`/api/groepsportaal/uitklokken/${id}`, {
        method: 'POST',
        body: JSON.stringify({ uitgeklokt: uitgeklokt ?? null }),
      }),
    onSuccess: invalideer,
  });
  return { inklokken, uitklokken };
}

// === Fase 9 — dashboard & actiecentrum ===
export function useDashboard(datum?: string) {
  const qs = datum ? `?datum=${datum}` : '';
  return useQuery({
    queryKey: ['dashboard', datum ?? 'vandaag'],
    queryFn: () => api<DashboardDto>(`/api/dashboard${qs}`),
  });
}

// --- BKR-snelrekenaar (calculator) ---
export function useBkrBerekening() {
  return useMutation({
    mutationFn: (invoer: BkrBerekenInvoer) =>
      api<BkrBerekenResultaatDto>('/api/bkr/bereken', {
        method: 'POST',
        body: JSON.stringify(invoer),
      }),
  });
}

export function useMeldingTellingen() {
  return useQuery({
    queryKey: ['melding-tellingen'],
    queryFn: () => api<MeldingTellingenDto>('/api/meldingen/tellingen'),
    refetchInterval: 60_000, // elke minuut vernieuwen voor het belletje
  });
}

export function useMeldingen(toonAfgehandeld = false, alleenToDos = false) {
  const params = new URLSearchParams();
  if (toonAfgehandeld) params.set('toonAfgehandeld', 'true');
  if (alleenToDos) params.set('alleenToDos', 'true');
  const qs = params.toString();
  return useQuery({
    queryKey: ['meldingen', toonAfgehandeld, alleenToDos],
    queryFn: () => api<MeldingDto[]>(`/api/meldingen${qs ? `?${qs}` : ''}`),
  });
}

export function useMeldingMutaties() {
  const qc = useQueryClient();
  const invalideer = () => {
    qc.invalidateQueries({ queryKey: ['meldingen'] });
    qc.invalidateQueries({ queryKey: ['melding-tellingen'] });
    qc.invalidateQueries({ queryKey: ['dashboard'] });
  };

  const gelezen = useMutation({
    mutationFn: (id: string) => api<MeldingDto>(`/api/meldingen/${id}/gelezen`, { method: 'POST' }),
    onSuccess: invalideer,
  });
  const allesGelezen = useMutation({
    mutationFn: () => api<void>('/api/meldingen/alles-gelezen', { method: 'POST' }),
    onSuccess: invalideer,
  });
  const afhandelen = useMutation({
    mutationFn: (id: string) => api<MeldingDto>(`/api/meldingen/${id}/afhandelen`, { method: 'POST' }),
    onSuccess: invalideer,
  });
  return { gelezen, allesGelezen, afhandelen };
}

// === Fase 9c — instellingen ===
export function useInstellingen() {
  return useQuery({
    queryKey: ['instellingen'],
    queryFn: () => api<InstellingenDto>('/api/instellingen'),
  });
}

export function useInstellingenMutatie() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (invoer: InstellingenInvoer) =>
      api<InstellingenDto>('/api/instellingen', { method: 'PUT', body: JSON.stringify(invoer) }),
    onSuccess: () => {
      // De instellingen sturen meerdere modules; ververs die mee.
      qc.invalidateQueries({ queryKey: ['instellingen'] });
      qc.invalidateQueries({ queryKey: ['dashboard'] });
      qc.invalidateQueries({ queryKey: ['meldingen'] });
      qc.invalidateQueries({ queryKey: ['melding-tellingen'] });
      qc.invalidateQueries({ queryKey: ['wachtlijst'] });
    },
  });
}

export function useLocatie() {
  return useQuery({
    queryKey: ['locatie'],
    queryFn: () => api<LocatieDto>('/api/instellingen/locatie'),
  });
}

export function useLocatieMutatie() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (invoer: LocatieDto) =>
      api<LocatieDto>('/api/instellingen/locatie', { method: 'PUT', body: JSON.stringify(invoer) }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['locatie'] }),
  });
}

export function useRechtenMatrix() {
  return useQuery({
    queryKey: ['rechten'],
    queryFn: () => api<RechtenMatrixDto>('/api/instellingen/rechten'),
  });
}

export function useRechtenMutatie() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ rol, capabilities }: { rol: number; capabilities: string[] }) =>
      api<RolRechtenDto>(`/api/instellingen/rechten/${rol}`, {
        method: 'PUT',
        body: JSON.stringify({ capabilities }),
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['rechten'] }),
  });
}

export function useSchoolvakantieMutaties() {
  const qc = useQueryClient();
  const invalideer = () => qc.invalidateQueries({ queryKey: ['schoolvakanties'] });

  const aanmaken = useMutation({
    mutationFn: (invoer: SchoolvakantieInvoer) =>
      api<SchoolvakantieDto>('/api/schoolvakanties', { method: 'POST', body: JSON.stringify(invoer) }),
    onSuccess: invalideer,
  });
  const verwijderen = useMutation({
    mutationFn: (id: string) => api<void>(`/api/schoolvakanties/${id}`, { method: 'DELETE' }),
    onSuccess: invalideer,
  });
  return { aanmaken, verwijderen };
}

// --- Kennisbank ---
export function useKennisbank() {
  return useQuery({
    queryKey: ['kennisbank'],
    queryFn: () => api<KennisbankItemDto[]>('/api/kennisbank'),
  });
}

export function useKennisbankDocument(id: string | undefined) {
  return useQuery({
    enabled: !!id,
    queryKey: ['kennisbank', id],
    queryFn: () => api<KennisbankDocumentDto>(`/api/kennisbank/${id}`),
  });
}

export function useKennisbankMutaties() {
  const qc = useQueryClient();
  const invalideer = () => qc.invalidateQueries({ queryKey: ['kennisbank'] });
  return {
    toevoegen: useMutation({
      mutationFn: (invoer: KennisbankInvoer) =>
        api<KennisbankDocumentDto>('/api/kennisbank', { method: 'POST', body: JSON.stringify(invoer) }),
      onSuccess: invalideer,
    }),
    bijwerken: useMutation({
      mutationFn: ({ id, invoer }: { id: string; invoer: KennisbankInvoer }) =>
        api<KennisbankDocumentDto>(`/api/kennisbank/${id}`, { method: 'PUT', body: JSON.stringify(invoer) }),
      onSuccess: invalideer,
    }),
    verwijderen: useMutation({
      mutationFn: (id: string) => api<void>(`/api/kennisbank/${id}`, { method: 'DELETE' }),
      onSuccess: invalideer,
    }),
    uploadBijlage: useMutation({
      mutationFn: ({ documentId, bestand }: { documentId: string; bestand: File }) => {
        const fd = new FormData();
        fd.append('bestand', bestand);
        return api<KennisbankBijlageDto>(`/api/kennisbank/${documentId}/bijlagen`, { method: 'POST', body: fd });
      },
      onSuccess: invalideer,
    }),
    verwijderBijlage: useMutation({
      mutationFn: (bijlageId: string) => api<void>(`/api/kennisbank/bijlagen/${bijlageId}`, { method: 'DELETE' }),
      onSuccess: invalideer,
    }),
  };
}

/** Download een kennisbank-bijlage (met auth) en biedt hem als bestand aan. */
export async function downloadKennisbankBijlage(bijlageId: string, bestandsNaam: string): Promise<void> {
  const blob = await apiBlob(`/api/kennisbank/bijlagen/${bijlageId}/download`);
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = bestandsNaam;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}
