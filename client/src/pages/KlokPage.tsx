import { useState } from 'react';
import { DienstVanDeDag, Inklokken } from './GroepsportaalPage';
import { korteDatum, vandaagIso, verschuifDagen } from '../datum';

/**
 * Klokken als eigen submenu-item (v3): het rooster/de dienst van de dag met daarop de
 * in-/uitklok-actie — losgetrokken uit het groepsportaal-dashboard. Klokken gebeurt op de
 * dienst van de medewerker; een spontane dienst blijft een expliciete handeling in de
 * inklok-kaart.
 */
export function KlokPage() {
  const [datum, setDatum] = useState(vandaagIso());

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Klokken</h1>
          <p>Dienst starten en beëindigen — op je eigen dienst van de dag</p>
        </div>
        <div className="wk-nav" style={{ marginBottom: 0 }}>
          <button onClick={() => setDatum(verschuifDagen(datum, -1))}>
            <i className="ti ti-chevron-left" />
          </button>
          <span style={{ minWidth: 120 }}>{korteDatum(datum)}</span>
          <button onClick={() => setDatum(verschuifDagen(datum, 1))}>
            <i className="ti ti-chevron-right" />
          </button>
        </div>
      </div>

      <DienstVanDeDag datum={datum} />
      <Inklokken datum={datum} />
    </div>
  );
}
