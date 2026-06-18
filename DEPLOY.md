# KinderKompas op Render hosten

Deze repo is klaar voor Render met drie onderdelen, die de meegeleverde
`render.yaml` (Blueprint) in één keer aanmaakt:

| Onderdeel | Render-type | Wat |
|---|---|---|
| `kinderkompas-db` | PostgreSQL (free) | De database |
| `kinderkompas-api` | Web Service (Docker) | De .NET 10 API |
| `kinderkompas-client` | Static Site (free) | De React-frontend (CDN) |

De frontend praat via een **rewrite** (`/api/*` → de API) met de backend, dus
er is geen CORS-configuratie nodig en de client gebruikt gewoon relatieve paden.

---

## 1. Eenmalig: repo naar GitHub

Render deployt vanuit een Git-repo. Deze map is nog geen git-repo, dus:

```bash
cd C:/Users/alanv/OPDNBUITEN
git init
git add .
git commit -m "KinderKompas: Render-ready (Postgres + Docker + static site)"
# Maak een lege repo op GitHub en koppel:
git remote add origin https://github.com/<jij>/kinderkompas.git
git branch -M main
git push -u origin main
```

> De oude SQL Server-migraties staan veiligheidshalve in
> `_sqlserver-migraties-backup/` (buiten de build). Je kunt die map negeren of
> verwijderen; productie draait op de nieuwe Postgres-migratie.

## 2. Blueprint aanmaken op Render

1. [dashboard.render.com](https://dashboard.render.com) → **New** → **Blueprint**.
2. Kies je GitHub-repo. Render leest `render.yaml` en toont db + API + static site.
3. Klik **Apply**. Render maakt de database, bouwt de Docker-image en de frontend.

Wat de Blueprint automatisch regelt:
- `ConnectionStrings__KinderKompas` → gekoppeld aan de Render-database.
- `Jwt__Key` → Render genereert een sterke, willekeurige waarde.
- `Jwt__Issuer` / `Jwt__Audience` → vaste waarden.
- Migraties + seed draaien automatisch bij de eerste start van de API.

## 3. Frontend aan de juiste API koppelen

De static site stuurt `/api/*` door naar `https://kinderkompas-api.onrender.com`.
Krijgt je API een **andere** URL (bv. omdat de naam al bezet was), pas dan in
`render.yaml` de `destination` van de eerste rewrite-regel aan en push opnieuw,
óf pas het in het dashboard aan onder de static site → **Redirects/Rewrites**.

## 4. Inloggen als Beheerder (2FA)

De Beheerder (`gail`) en het Groepsportaal-account hebben **verplichte 2FA**. De
authenticator-sleutel wordt **alleen in Development** gelogd. Voor productie:

1. Zet bij `kinderkompas-api` tijdelijk env var `ASPNETCORE_ENVIRONMENT=Development`
   en deploy. (Bij de **eerste** seed wordt het account aangemaakt.)
2. Open **Logs** van de API en zoek de regel met de authenticator-sleutel voor
   `gail`. Voer die in je authenticator-app in (TOTP).
3. Zet `ASPNETCORE_ENVIRONMENT` terug op `Production` en deploy opnieuw.

Seed-accounts (wachtwoorden uit de seed):
- `gail` / `Beheerder!2026` — Beheerder (2FA)
- `sanne` / `Senior!2026` — Senior (geen 2FA)
- `jasper` / `Junior!2026` — Junior (geen 2FA)
- `groepsportaal` / `Portaal!2026` — Groepsportaal (2FA)

> Wijzig deze wachtwoorden voor echt gebruik.

## 5. Belangrijk om te weten (free tier)

- **Bestand-uploads** (observatie-PDF's) gaan naar de container-schijf en zijn
  **vluchtig**: bij elke deploy weg. Voor blijvende opslag: voeg een **Render Disk**
  toe aan de API, gemount op `/app/App_Data/bestanden` (betaald), of stap over op
  object storage (S3/Azure Blob). De map is al via `Bestandsopslag__Root` ingesteld.
- **API spindown**: een gratis web service slaapt na ~15 min inactiviteit; de
  eerste request daarna duurt ~30-60s (cold start).
- **Gratis Postgres** verloopt na 90 dagen — daarna upgraden of opnieuw aanmaken.

---

## Lokaal ontwikkelen (na de Postgres-overstap)

De app gebruikt nu PostgreSQL i.p.v. SQL Server, ook lokaal. Start een lokale
Postgres met Docker:

```bash
docker run --name kk-postgres -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=kinderkompas -p 5432:5432 -d postgres:16
```

Zet daarna je lokale secrets (in `src/KinderKompas.Api`):

```bash
cd src/KinderKompas.Api
dotnet user-secrets set "ConnectionStrings:KinderKompas" "Host=localhost;Port=5432;Database=kinderkompas;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:Key" "een-lange-willekeurige-sleutel-van-minstens-32-tekens"
dotnet run
```

De API past bij het starten zelf de migraties toe en seedt de demodata.

## Migraties beheren

```bash
# Nieuwe migratie toevoegen (bouw eerst, draai dan zonder herbouw — WDAC-vriendelijk):
dotnet build src/KinderKompas.Api/KinderKompas.Api.csproj
dotnet ef migrations add <Naam> \
  --project src/KinderKompas.Infrastructure \
  --startup-project src/KinderKompas.Api --no-build
```

Toepassen hoeft niet handmatig: de API draait `Database.Migrate()` bij het opstarten.
