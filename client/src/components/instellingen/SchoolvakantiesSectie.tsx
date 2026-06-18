import { useState } from 'react';
import { useSchoolvakanties, useSchoolvakantieMutaties } from '../../api/queries';
import { korteDatum, vandaagIso } from '../../datum';
import type { SchoolvakantieInvoer } from '../../types';

const LEEG: SchoolvakantieInvoer = {
  naam: '',
  schooljaar: new Date().getFullYear(),
  begindatum: vandaagIso(),
  einddatum: vandaagIso(),
};

export function SchoolvakantiesSectie() {
  const { data, isLoading, error } = useSchoolvakanties();
  const { aanmaken, verwijderen } = useSchoolvakantieMutaties();
  const [form, setForm] = useState<SchoolvakantieInvoer>(LEEG);

  return (
    <div>
      <form
        onSubmit={(e) => {
          e.preventDefault();
          aanmaken.mutate(form, { onSuccess: () => setForm({ ...LEEG }) });
        }}
        className="card"
        style={{ marginBottom: 16 }}
      >
        <div className="card-b" style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'flex-end', gap: 10 }}>
          <div className="fld" style={{ marginBottom: 0, flex: 1, minWidth: 180 }}>
            <label>Naam</label>
            <input required value={form.naam} onChange={(e) => setForm({ ...form, naam: e.target.value })} placeholder="Zomervakantie" />
          </div>
          <div className="fld" style={{ marginBottom: 0, width: 110 }}>
            <label>Schooljaar</label>
            <input type="number" value={form.schooljaar} onChange={(e) => setForm({ ...form, schooljaar: Number(e.target.value) })} />
          </div>
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Van</label>
            <input type="date" value={form.begindatum} onChange={(e) => setForm({ ...form, begindatum: e.target.value })} />
          </div>
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Tot en met</label>
            <input type="date" value={form.einddatum} onChange={(e) => setForm({ ...form, einddatum: e.target.value })} />
          </div>
          <button type="submit" disabled={aanmaken.isPending} className="btn btn-primary btn-sm">
            <i className="ti ti-plus" /> Vakantie toevoegen
          </button>
          {aanmaken.isError && <span style={{ fontSize: 12, color: 'var(--rose)' }}>{aanmaken.error.message}</span>}
        </div>
      </form>

      {isLoading && <div className="loader"><i className="ti ti-loader" /> Laden…</div>}
      {error && <p style={{ color: 'var(--rose)' }}>Kon de schoolvakanties niet laden.</p>}

      {data && (
        <div className="tbl-wrap">
          <table className="tbl">
            <thead>
              <tr>
                <th>Naam</th>
                <th>Schooljaar</th>
                <th>Periode</th>
                <th style={{ textAlign: 'right' }}>Actie</th>
              </tr>
            </thead>
            <tbody>
              {data.map((v) => (
                <tr key={v.id}>
                  <td style={{ fontWeight: 600 }}>{v.naam}</td>
                  <td style={{ color: 'var(--text2)' }}>{v.schooljaarLabel}</td>
                  <td style={{ color: 'var(--text2)' }}>
                    {korteDatum(v.begindatum)} – {korteDatum(v.einddatum)}
                  </td>
                  <td style={{ textAlign: 'right' }}>
                    <button
                      onClick={() => {
                        if (confirm(`Vakantie "${v.naam}" verwijderen?`)) verwijderen.mutate(v.id);
                      }}
                      className="btn btn-rose btn-xs"
                    >
                      <i className="ti ti-trash" />
                    </button>
                  </td>
                </tr>
              ))}
              {data.length === 0 && (
                <tr>
                  <td colSpan={4} style={{ textAlign: 'center', color: 'var(--text3)', padding: '24px 0' }}>
                    Nog geen schoolvakanties.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
