import { useEffect, useState } from 'react';
import { useLocatie, useLocatieMutatie } from '../../api/queries';
import type { LocatieDto } from '../../types';

export function LocatieSectie() {
  const { data, isLoading, error } = useLocatie();
  const opslaan = useLocatieMutatie();
  const [form, setForm] = useState<LocatieDto | null>(null);

  useEffect(() => {
    if (data) setForm(data);
  }, [data]);

  if (isLoading) return <div className="loader"><i className="ti ti-loader" /> Laden…</div>;
  if (error || !form) return <p style={{ color: 'var(--rose)' }}>Kon de locatiegegevens niet laden.</p>;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        opslaan.mutate(form);
      }}
      className="card"
      style={{ maxWidth: 460 }}
    >
      <div className="card-h">
        <h3>
          <i className="ti ti-building-community" style={{ color: 'var(--primary)' }} /> Locatie
        </h3>
      </div>
      <div className="card-b">
        <div className="fld">
          <label>Naam van de organisatie</label>
          <input value={form.naam} onChange={(e) => setForm({ ...form, naam: e.target.value })} />
        </div>
        <div className="fld" style={{ marginBottom: 0 }}>
          <label>LRK-nummer</label>
          <input value={form.lrknummer} onChange={(e) => setForm({ ...form, lrknummer: e.target.value })} inputMode="numeric" />
          <span style={{ marginTop: 4, display: 'block', fontSize: 10, color: 'var(--text3)' }}>
            Landelijk Register Kinderopvang — alleen cijfers.
          </span>
        </div>
      </div>
      <div className="modal-f" style={{ position: 'static', borderRadius: 0, justifyContent: 'flex-start', alignItems: 'center' }}>
        <button type="submit" disabled={opslaan.isPending} className="btn btn-primary btn-sm">
          {opslaan.isPending ? 'Opslaan…' : 'Opslaan'}
        </button>
        {opslaan.isSuccess && <span style={{ fontSize: 12, color: 'var(--green)' }}>Opgeslagen ✓</span>}
        {opslaan.isError && <span style={{ fontSize: 12, color: 'var(--rose)' }}>{opslaan.error.message}</span>}
      </div>
    </form>
  );
}
