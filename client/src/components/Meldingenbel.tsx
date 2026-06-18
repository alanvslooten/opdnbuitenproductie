import { useNavigate } from 'react-router-dom';
import { useMeldingTellingen } from '../api/queries';

/**
 * Het belletje in de topbar: linkt naar het actiecentrum en toont een rode stip
 * zodra er openstaande to-do's of ongelezen meldingen zijn. Ververst zichzelf
 * elke minuut. (Stijl past bij de nieuwe topbar; data/route-logica ongewijzigd.)
 */
export function Meldingenbel() {
  const navigate = useNavigate();
  const { data } = useMeldingTellingen();
  const openToDos = data?.openToDos ?? 0;
  const ongelezen = data?.ongelezen ?? 0;
  const aantal = openToDos || ongelezen;

  return (
    <button
      className="btn-icon"
      onClick={() => navigate('/meldingen')}
      title={`${openToDos} open to-do's · ${ongelezen} ongelezen`}
    >
      <i className="ti ti-bell" />
      {aantal > 0 && (
        <span
          style={{
            position: 'absolute',
            top: 4,
            right: 4,
            minWidth: 14,
            height: 14,
            padding: '0 3px',
            fontSize: 8,
            fontWeight: 700,
            lineHeight: '14px',
            textAlign: 'center',
            color: '#fff',
            background: openToDos > 0 ? 'var(--rose)' : 'var(--amber)',
            borderRadius: 8,
            border: '2px solid var(--surface)',
          }}
        >
          {aantal > 99 ? '99+' : aantal}
        </span>
      )}
    </button>
  );
}
