import { useRechtenMatrix, useRechtenMutatie } from '../../api/queries';
import { Capabilities, ROL_LABEL } from '../../types';

const BEHEERDER = 0;
// De rechten die de Beheerder niet kan verliezen (spiegelt RechtenVangrail in de backend).
const BESCHERMD: string[] = [Capabilities.InstellingenBeheren, Capabilities.DashboardZien];

export function RechtenSectie() {
  const { data, isLoading, error } = useRechtenMatrix();
  const muteer = useRechtenMutatie();

  if (isLoading) return <div className="loader"><i className="ti ti-loader" /> Laden…</div>;
  if (error || !data) return <p style={{ color: 'var(--rose)' }}>Kon de rechten niet laden.</p>;

  // Per rol een set toegekende sleutels.
  const perRol = new Map<number, Set<string>>();
  for (const r of data.rollen) perRol.set(r.rol, new Set(r.capabilities));

  const isVergrendeld = (rol: number, sleutel: string) => rol === BEHEERDER && BESCHERMD.includes(sleutel);

  const toggle = (rol: number, sleutel: string) => {
    const huidig = new Set(perRol.get(rol) ?? []);
    if (huidig.has(sleutel)) huidig.delete(sleutel);
    else huidig.add(sleutel);
    muteer.mutate({ rol, capabilities: [...huidig] });
  };

  return (
    <div>
      <div className="alert alert-info">
        <i className="ti ti-info-circle" />
        <span>
          Vink per rol aan welke rechten gelden. Een wijziging geldt bij de eerstvolgende keer dat de betreffende
          gebruiker inlogt. De Beheerder houdt altijd toegang tot instellingen en dashboard (vergrendeld).
        </span>
      </div>

      {muteer.isError && (
        <div className="alert alert-bad">
          <i className="ti ti-alert-circle" />
          <span>{muteer.error.message}</span>
        </div>
      )}

      <div className="tbl-wrap">
        <table className="tbl">
          <thead>
            <tr>
              <th>Recht</th>
              {ROL_LABEL.map((label, rol) => (
                <th key={rol} style={{ textAlign: 'center' }}>
                  {label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.capabilities.map((cap) => (
              <tr key={cap.sleutel}>
                <td>
                  <div style={{ fontWeight: 600 }}>{cap.omschrijving}</div>
                  <div style={{ fontSize: 10, color: 'var(--text3)' }}>{cap.sleutel}</div>
                </td>
                {ROL_LABEL.map((_, rol) => {
                  const aan = perRol.get(rol)?.has(cap.sleutel) ?? false;
                  const slot = isVergrendeld(rol, cap.sleutel);
                  return (
                    <td key={rol} style={{ textAlign: 'center' }}>
                      <input
                        type="checkbox"
                        checked={aan}
                        disabled={slot || muteer.isPending}
                        title={slot ? 'Vergrendeld: de Beheerder kan dit recht niet verliezen' : undefined}
                        onChange={() => toggle(rol, cap.sleutel)}
                      />
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
