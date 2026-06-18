import { useState } from 'react';
import { GedragSectie } from '../components/instellingen/GedragSectie';
import { LocatieSectie } from '../components/instellingen/LocatieSectie';
import { RechtenSectie } from '../components/instellingen/RechtenSectie';
import { SchoolvakantiesSectie } from '../components/instellingen/SchoolvakantiesSectie';
import { GroepenSectie } from '../components/instellingen/GroepenSectie';

const TABS = [
  { sleutel: 'gedrag', label: 'Gedrag' },
  { sleutel: 'locatie', label: 'Locatie' },
  { sleutel: 'rechten', label: 'Rechten' },
  { sleutel: 'vakanties', label: 'Schoolvakanties' },
  { sleutel: 'groepen', label: 'Groepen' },
] as const;

type TabSleutel = (typeof TABS)[number]['sleutel'];

export function InstellingenPage() {
  const [tab, setTab] = useState<TabSleutel>('gedrag');

  return (
    <div className="view">
      <div className="ph">
        <div>
          <h1>Instellingen</h1>
          <p>Gedrag, locatie, rechten, schoolvakanties en groepen</p>
        </div>
      </div>

      <div className="tabs">
        {TABS.map((t) => (
          <button key={t.sleutel} onClick={() => setTab(t.sleutel)} className={`tab${tab === t.sleutel ? ' on' : ''}`}>
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'gedrag' && <GedragSectie />}
      {tab === 'locatie' && <LocatieSectie />}
      {tab === 'rechten' && <RechtenSectie />}
      {tab === 'vakanties' && <SchoolvakantiesSectie />}
      {tab === 'groepen' && <GroepenSectie />}
    </div>
  );
}
