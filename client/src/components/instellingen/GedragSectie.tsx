import { useEffect, useState } from 'react';
import { useInstellingen, useInstellingenMutatie } from '../../api/queries';
import { MELDING_SOORT_LABEL, type InstellingenInvoer } from '../../types';

const SOORTEN = [0, 1, 2, 3, 4, 5];

export function GedragSectie() {
  const { data, isLoading, error } = useInstellingen();
  const opslaan = useInstellingenMutatie();
  const [form, setForm] = useState<InstellingenInvoer | null>(null);

  useEffect(() => {
    if (data) setForm(data);
  }, [data]);

  if (isLoading) return <div className="loader"><i className="ti ti-loader" /> Laden…</div>;
  if (error || !form) return <p style={{ color: 'var(--rose)' }}>Kon de instellingen niet laden.</p>;

  const verborgen = form.verborgenMeldingsoorten;
  const toggleSoort = (soort: number) =>
    setForm((f) =>
      f === null
        ? f
        : {
            ...f,
            verborgenMeldingsoorten: verborgen.includes(soort)
              ? verborgen.filter((s) => s !== soort)
              : [...verborgen, soort],
          },
    );

  const nummer = (veld: keyof InstellingenInvoer) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((f) => (f ? { ...f, [veld]: Number(e.target.value) } : f));

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        opslaan.mutate(form);
      }}
      style={{ maxWidth: 680 }}
    >
      <div className="setting-block">
        <div className="setting-block-h">
          <i className="ti ti-bell" /> Meldingen in het actiecentrum
        </div>
        <div style={{ padding: '12px 16px' }}>
          <p style={{ fontSize: 11, color: 'var(--text3)', marginBottom: 10 }}>
            Vink uit welke soorten je niet in het actiecentrum wilt zien. Ze worden nog wél vastgelegd
            (de historie blijft volledig), alleen verborgen.
          </p>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 8 }}>
            {SOORTEN.map((s) => (
              <label key={s} style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 12 }}>
                <input type="checkbox" checked={!verborgen.includes(s)} onChange={() => toggleSoort(s)} />
                {MELDING_SOORT_LABEL[s]}
              </label>
            ))}
          </div>
        </div>
      </div>

      <div className="setting-block">
        <div className="setting-block-h">
          <i className="ti ti-adjustments" /> Drempels
        </div>
        <div className="frow" style={{ padding: '12px 16px' }}>
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Observatie 'binnenkort' (dagen)</label>
            <input type="number" min={1} max={365} value={form.observatieBinnenkortDrempelDagen} onChange={nummer('observatieBinnenkortDrempelDagen')} />
          </div>
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>'Binnenkort 4 jaar' (dagen)</label>
            <input type="number" min={1} max={365} value={form.kindBinnenkortVierDrempelDagen} onChange={nummer('kindBinnenkortVierDrempelDagen')} />
          </div>
        </div>
      </div>

      <div className="setting-block">
        <div className="setting-block-h">
          <i className="ti ti-list-numbers" /> Wachtlijst-prioriteit
        </div>
        <div className="frow" style={{ padding: '12px 16px' }}>
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Bonus interne aanvraag</label>
            <input type="number" min={0} value={form.prioriteitInternGewicht} onChange={nummer('prioriteitInternGewicht')} />
          </div>
          <div className="fld" style={{ marginBottom: 0 }}>
            <label>Punten per maand wachten</label>
            <input type="number" min={0} value={form.prioriteitPerMaandGewicht} onChange={nummer('prioriteitPerMaandGewicht')} />
          </div>
        </div>
      </div>

      <div className="setting-block">
        <div className="setting-block-h">
          <i className="ti ti-mail" /> Standaard observatietekst
        </div>
        <div style={{ padding: '12px 16px' }}>
          <p style={{ fontSize: 11, color: 'var(--text3)', marginBottom: 8 }}>
            De mailtekst naar de ouder. Gebruik <code>{'{voornaam}'}</code> voor de voornaam van het kind. Laat leeg
            voor de ingebouwde standaardtekst.
          </p>
          <div className="fld" style={{ marginBottom: 0 }}>
            <textarea
              rows={6}
              value={form.standaardObservatietekst ?? ''}
              onChange={(e) => setForm((f) => (f ? { ...f, standaardObservatietekst: e.target.value } : f))}
              placeholder="Beste ouder/verzorger, in de bijlage vind je de observatie van {voornaam}…"
            />
          </div>
        </div>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <button type="submit" disabled={opslaan.isPending} className="btn btn-primary btn-sm">
          {opslaan.isPending ? 'Opslaan…' : 'Opslaan'}
        </button>
        {opslaan.isSuccess && <span style={{ fontSize: 12, color: 'var(--green)' }}>Opgeslagen ✓</span>}
        {opslaan.isError && <span style={{ fontSize: 12, color: 'var(--rose)' }}>Opslaan mislukt.</span>}
      </div>
    </form>
  );
}
