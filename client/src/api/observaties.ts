import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, apiBlob } from './client';
import type { Iso } from '../types';

// --- Types (spiegelen het fase-7 API-contract; enums numeriek over de lijn) ---

/** Status van een observatiemoment. Komt numeriek uit de ObservatieStatus-enum. */
export const ObservatieStatus = {
  NogNietAanDeBeurt: 0,
  Binnenkort: 1,
  Overschreden: 2,
  Afgerond: 3,
} as const;

export interface ObservatieDto {
  id: string;
  kindId: string;
  mijlpaalMaanden: number;
  bestandsNaam: string;
  bestandsGrootte: number;
  geuploadOp: string;
  verzondenOp: string | null;
  verzondenNaarEmail: string | null;
}

export interface ObservatiemomentDto {
  mijlpaalMaanden: number;
  isEindmoment: boolean;
  beschrijving: string;
  vervaldatum: Iso;
  status: number;
  observatie: ObservatieDto | null;
}

export interface KindObservatieschemaDto {
  kindId: string;
  voornaam: string;
  achternaam: string;
  stamgroepId: string;
  geboortedatum: Iso;
  vierdeVerjaardag: Iso;
  wordtBinnenkortVier: boolean;
  mentorId: string | null;
  aantalOverschreden: number;
  aantalBinnenkort: number;
  aantalAfgerond: number;
  momenten: ObservatiemomentDto[];
}

// --- Queries ---

export function useObservatieOverzicht(stamgroepId?: string) {
  const qs = stamgroepId ? `?stamgroepId=${stamgroepId}` : '';
  return useQuery({
    queryKey: ['observaties', stamgroepId ?? 'alle'],
    queryFn: () => api<KindObservatieschemaDto[]>(`/api/observaties${qs}`),
  });
}

export function useObservatieMutaties() {
  const qc = useQueryClient();
  const invalideer = () => qc.invalidateQueries({ queryKey: ['observaties'] });

  const afvinken = useMutation({
    mutationFn: ({ kindId, mijlpaalMaanden, bestand }: { kindId: string; mijlpaalMaanden: number; bestand: File }) => {
      const fd = new FormData();
      fd.append('mijlpaalMaanden', String(mijlpaalMaanden));
      fd.append('bestand', bestand);
      return api<ObservatieDto>(`/api/observaties/kind/${kindId}/afvinken`, { method: 'POST', body: fd });
    },
    onSuccess: invalideer,
  });

  const versturen = useMutation({
    mutationFn: (observatieId: string) =>
      api<ObservatieDto>(`/api/observaties/${observatieId}/versturen`, { method: 'POST' }),
    onSuccess: invalideer,
  });

  const ongedaanMaken = useMutation({
    mutationFn: (observatieId: string) => api<void>(`/api/observaties/${observatieId}`, { method: 'DELETE' }),
    onSuccess: invalideer,
  });

  return { afvinken, versturen, ongedaanMaken };
}

/** Opent de PDF van een observatie in een nieuw tabblad (met auth). */
export async function openObservatiePdf(observatieId: string): Promise<void> {
  const blob = await apiBlob(`/api/observaties/${observatieId}/bestand`);
  const url = URL.createObjectURL(blob);
  window.open(url, '_blank', 'noopener');
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}
