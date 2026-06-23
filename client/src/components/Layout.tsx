import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { Meldingenbel } from './Meldingenbel';
import { wisselThema, huidigThema, type Thema } from '../thema';
import { Capabilities, ROL_WEERGAVE } from '../types';

// Elke link is gekoppeld aan de capability die het bijbehorende endpoint vereist
// (ongewijzigd t.o.v. de vorige navigatie) en gegroepeerd in secties + een
// Tabler-icoon, zodat de look overeenkomt met het bijgevoegde ontwerp.
type Link = { naar: string; label: string; cap: string; ofCap?: string; icon: string; sectie: string };

const links: Link[] = [
  { naar: '/dashboard', label: 'Dashboard', cap: Capabilities.DashboardZien, icon: 'ti-dashboard', sectie: 'Overzicht' },
  { naar: '/planning', label: 'Weekplanning', cap: Capabilities.PlanningZien, icon: 'ti-calendar-week', sectie: 'Kinderen' },
  { naar: '/maandplanning', label: 'Maandplanning', cap: Capabilities.PlanningZien, icon: 'ti-calendar-month', sectie: 'Kinderen' },
  { naar: '/dagfilter', label: 'Dagfilter', cap: Capabilities.PlanningZien, icon: 'ti-filter', sectie: 'Kinderen' },
  { naar: '/kinderen', label: 'Kinderen', cap: Capabilities.KinderenBeheren, ofCap: Capabilities.KinderenLezen, icon: 'ti-mood-kid', sectie: 'Kinderen' },
  { naar: '/observaties', label: 'Observaties', cap: Capabilities.ObservatiesVersturen, icon: 'ti-clipboard-check', sectie: 'Kinderen' },
  { naar: '/stamgroepen', label: 'Stamgroepen', cap: Capabilities.KinderenBeheren, icon: 'ti-layout-grid', sectie: 'Kinderen' },
  { naar: '/bkr', label: 'BKR Calculator', cap: Capabilities.DashboardZien, icon: 'ti-calculator', sectie: 'Overzicht' },
  { naar: '/contacten', label: 'Contacten', cap: Capabilities.WachtlijstBeheren, icon: 'ti-address-book', sectie: 'Contacten' },
  { naar: '/wachtlijst', label: 'Wachtlijst', cap: Capabilities.WachtlijstBeheren, icon: 'ti-list-numbers', sectie: 'Contacten' },
  { naar: '/rooster', label: 'Rooster', cap: Capabilities.RoosterBeheren, icon: 'ti-clock', sectie: 'Personeel' },
  { naar: '/verlof', label: 'Verlof & Ziekte', cap: Capabilities.RoosterBeheren, icon: 'ti-beach', sectie: 'Personeel' },
  { naar: '/medewerkers', label: 'Medewerkers', cap: Capabilities.MedewerkersBeheren, icon: 'ti-users', sectie: 'Personeel' },
  { naar: '/instellingen', label: 'Instellingen', cap: Capabilities.InstellingenBeheren, icon: 'ti-settings', sectie: 'Beheer' },
  { naar: '/groepsportaal', label: 'Groepsportaal', cap: Capabilities.GroepsportaalGebruiken, icon: 'ti-device-tablet', sectie: 'Portalen' },
  { naar: '/thuisportaal', label: 'Mijn portaal', cap: Capabilities.ThuisportaalGebruiken, icon: 'ti-home', sectie: 'Portalen' },
];

const SECTIE_VOLGORDE = ['Overzicht', 'Kinderen', 'Contacten', 'Personeel', 'Beheer', 'Portalen'];

function initialen(naam: string | null): string {
  if (!naam) return '?';
  const delen = naam.trim().split(/\s+/).slice(0, 2);
  return delen.map((d) => d[0]?.toUpperCase() ?? '').join('') || '?';
}

export function Layout() {
  const { gebruikersnaam, rol, stamgroepNaam, weergavenaam, uitloggen, heeft } = useAuth();
  const location = useLocation();
  const [open, setOpen] = useState(false);
  const [thema, setThemaState] = useState<Thema>(huidigThema());

  const zichtbareLinks = links.filter((l) => heeft(l.cap) || (l.ofCap != null && heeft(l.ofCap)));
  const actief = zichtbareLinks.find((l) => location.pathname.startsWith(l.naar));
  const titel = actief?.label ?? 'KinderKompas';
  // Een groepsportaal-account is geen persoon maar een groep-tablet: toon de
  // groepsnaam + "Groepsportaal". Anders de echte naam + precieze functietitel.
  const isGroepsportaal = rol === 'Groepsportaal';
  const naamWeergave = isGroepsportaal
    ? (stamgroepNaam ?? 'Groepsportaal')
    : (weergavenaam ?? gebruikersnaam ?? 'Gebruiker');
  const rolLabel = isGroepsportaal
    ? 'Groepsportaal'
    : rol
      ? (ROL_WEERGAVE[rol] ?? rol)
      : heeft(Capabilities.DashboardZien)
        ? 'Beheer'
        : 'Medewerker';

  function toggleThema() {
    setThemaState(wisselThema());
  }

  return (
    <div className="app-shell">
      <div className={`sb-overlay${open ? ' on' : ''}`} onClick={() => setOpen(false)} />

      {/* SIDEBAR */}
      <nav className={`sidebar${open ? ' open' : ''}`}>
        <div className="sb-brand">
          <div className="sb-brand-mark">
            <i className="ti ti-building-community" />
          </div>
          <div className="sb-brand-text">
            <h1>KinderKompas</h1>
            <span>Kinderdagverblijf</span>
          </div>
        </div>

        <div className="sb-nav">
          {SECTIE_VOLGORDE.map((sectie) => {
            const inSectie = zichtbareLinks.filter((l) => l.sectie === sectie);
            if (inSectie.length === 0) return null;
            return (
              <div key={sectie}>
                <div className="nav-sec">{sectie}</div>
                {inSectie.map((l) => (
                  <NavLink
                    key={l.naar}
                    to={l.naar}
                    onClick={() => setOpen(false)}
                    className={({ isActive }) => `nav-item${isActive ? ' on' : ''}`}
                  >
                    <i className={`ti ${l.icon}`} />
                    {l.label}
                  </NavLink>
                ))}
              </div>
            );
          })}
        </div>

        <div className="sb-bottom">
          <div className="sb-user">
            <div className="sb-av">{initialen(naamWeergave)}</div>
            <div className="sb-user-info">
              <p>{naamWeergave}</p>
              <span>{rolLabel}</span>
            </div>
            <button className="sb-logout" onClick={uitloggen} title="Uitloggen">
              <i className="ti ti-logout" />
            </button>
          </div>
        </div>
      </nav>

      {/* MAIN */}
      <div className="main">
        <div className="topbar">
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexShrink: 0 }}>
            <button className="hamburger" onClick={() => setOpen((o) => !o)}>
              <i className="ti ti-menu-2" />
            </button>
            <div className="topbar-title">{titel}</div>
          </div>
          <div className="topbar-actions">
            <button className="btn-icon" onClick={toggleThema} title="Licht / donker">
              <i className={`ti ${thema === 'dark' ? 'ti-moon' : 'ti-sun'}`} />
            </button>
            {heeft(Capabilities.DashboardZien) && <Meldingenbel />}
          </div>
        </div>

        <div className="page">
          <Outlet />
        </div>
      </div>
    </div>
  );
}
