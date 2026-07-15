import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { Layout } from './components/Layout';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { MeldingenPage } from './pages/MeldingenPage';
import { InstellingenPage } from './pages/InstellingenPage';
import { WeekplanningPage } from './pages/WeekplanningPage';
import { MaandplanningPage } from './pages/MaandplanningPage';
import { PlaatsingsoverzichtPage } from './pages/PlaatsingsoverzichtPage';
import { DagfilterPage } from './pages/DagfilterPage';
import { KinderenPage } from './pages/KinderenPage';
import { KindFormPage } from './pages/KindFormPage';
import { StamgroepenPage } from './pages/StamgroepenPage';
import { ContactenPage } from './pages/ContactenPage';
import { WachtlijstPage } from './pages/WachtlijstPage';
import { WachtlijstFormPage } from './pages/WachtlijstFormPage';
import { RoosterPage } from './pages/RoosterPage';
import { MedewerkersPage } from './pages/MedewerkersPage';
import { VerlofPage } from './pages/VerlofPage';
import { ObservatiesPage } from './pages/ObservatiesPage';
import { BkrCalculatorPage } from './pages/BkrCalculatorPage';
import { GroepsportaalPage } from './pages/GroepsportaalPage';
import { ThuisportaalPage } from './pages/ThuisportaalPage';
import { KennisbankPage } from './pages/KennisbankPage';
import { KlokPage } from './pages/KlokPage';
import { PubliekAanmeldPage } from './pages/PubliekAanmeldPage';
import { PubliekRondleidingPage } from './pages/PubliekRondleidingPage';
import { Capabilities } from './types';

export default function App() {
  const { ingelogd, klaar, heeft } = useAuth();

  if (!klaar) {
    return (
      <div className="loader" style={{ minHeight: '100vh' }}>
        <i className="ti ti-loader" /> Laden…
      </div>
    );
  }

  if (!ingelogd) {
    return (
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        {/* Publieke formulieren: zonder login bereikbaar. */}
        <Route path="/aanmelden" element={<PubliekAanmeldPage />} />
        <Route path="/rondleiding" element={<PubliekRondleidingPage />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    );
  }

  // Slimme startpagina: het dashboard voor de back-office, anders het groepsportaal
  // (tablet) en daarna het thuis-portaal (medewerker). Zo landt elke rol op een
  // pagina die hij mag zien.
  const startpagina = heeft(Capabilities.DashboardZien)
    ? '/dashboard'
    : heeft(Capabilities.KinderenBeheren) || heeft(Capabilities.PlanningZien)
      ? '/planning'
      : heeft(Capabilities.GroepsportaalGebruiken)
        ? '/groepsportaal'
        : heeft(Capabilities.ThuisportaalGebruiken)
          ? '/thuisportaal'
          : '/thuisportaal';

  return (
    <Routes>
      {/* Publieke formulieren blijven ook voor een ingelogde beheerder bereikbaar
          (om te delen/previewen), standalone buiten de app-chrome. */}
      <Route path="/aanmelden" element={<PubliekAanmeldPage />} />
      <Route path="/rondleiding" element={<PubliekRondleidingPage />} />
      <Route element={<Layout />}>
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/meldingen" element={<MeldingenPage />} />
        <Route path="/instellingen" element={<InstellingenPage />} />
        <Route path="/planning" element={<WeekplanningPage />} />
        <Route path="/maandplanning" element={<MaandplanningPage />} />
        <Route path="/dagfilter" element={<DagfilterPage />} />
        <Route path="/kinderen" element={<KinderenPage />} />
        <Route path="/kinderen/nieuw" element={<KindFormPage />} />
        <Route path="/kinderen/:id" element={<KindFormPage />} />
        <Route path="/observaties" element={<ObservatiesPage />} />
        <Route path="/stamgroepen" element={<StamgroepenPage />} />
        <Route path="/bkr" element={<BkrCalculatorPage />} />
        <Route path="/rooster" element={<RoosterPage />} />
        <Route path="/medewerkers" element={<MedewerkersPage />} />
        <Route path="/verlof" element={<VerlofPage />} />
        <Route path="/contacten" element={<ContactenPage />} />
        <Route path="/plaatsingsoverzicht" element={<PlaatsingsoverzichtPage />} />
        <Route path="/wachtlijst" element={<WachtlijstPage />} />
        <Route path="/wachtlijst/nieuw" element={<WachtlijstFormPage />} />
        <Route path="/wachtlijst/:id" element={<WachtlijstFormPage />} />
        <Route path="/kennisbank" element={<KennisbankPage />} />
        <Route path="/groepsportaal" element={<GroepsportaalPage />} />
        <Route path="/klok" element={<KlokPage />} />
        <Route path="/thuisportaal" element={<ThuisportaalPage />} />
        <Route path="*" element={<Navigate to={startpagina} replace />} />
      </Route>
    </Routes>
  );
}
