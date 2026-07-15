# Dagplaatsing — technisch ontwerp (v3, de spil)

Status: **fundament geïmplementeerd** (entiteit + migratie + domeinlogica + tests). Wiring in planning/BKR/UI volgt als aparte increments — zie "Vervolgstappen".

## Het probleem

Het systeem koppelt een kind hard en enkelvoudig aan één stamgroep (`Kind.StamgroepId`) en telt per groep. Daaruit vloeien vijf van de zwaarste v3-feedbackpunten voort: groepsgrootte per dag, BKR-telling per dag, flexibele groepsplaatsing, kind-dat-later-start en ruildagen.

Belangrijke nuance uit de reconciliatie (15-07): de telling draait technisch **al per dag** — `Aanwezigheid.AanwezigOp(kinderen, datum, vakanties)` bepaalt per datum wie er is, op basis van `GewensteOpvangdagen`, `Startdatum`/`Einddatum`, leeftijd en 40-wekencontract. Wat ontbreekt is de mogelijkheid om een kind op een **specifieke dag** van zijn vaste groep/patroon te laten afwijken. "Casey start pas in oktober" wordt al correct afgevangen door `Kind.Startdatum`.

## De oplossing: thuisgroep + dagafwijking (strangler, niet big-bang)

We scheiden twee begrippen:

- **Thuisgroep** — `Kind.StamgroepId`, blijft ongewijzigd. Vast pedagogisch anker: oudergegevens, mentor, observaties. Ook de default voor de dagindeling.
- **Dagplaatsing** — een expliciete **afwijking per dag**: "op datum D staat kind X op groep G" (of "is kind X afwezig op datum D"). Alleen uitzonderingen worden opgeslagen; het reguliere patroon blijft de `GewensteOpvangdagen` op de thuisgroep. Zo is de dataset klein en eindig (geen rij per kind per dag tot in de eeuwigheid), terwijl per-dag flexibiliteit volledig mogelijk wordt.

Dit is bewust een **strangler-fig**-aanpak: de nieuwe laag zit bovenop het bestaande, werkende model in plaats van het in één klap te vervangen. De bestaande app blijft draaien; features verhuizen incrementeel naar de dagindeling.

### Semantiek van een dagplaatsing

Een `Dagplaatsing` heeft `KindId`, `Datum`, een **nullable** `StamgroepId` en een `Soort`:

- `StamgroepId != null` → het kind staat die dag op **die** groep (aanwezig).
- `StamgroepId == null` → het kind is die dag **afwezig** (heft een reguliere opvangdag op).

`Soort` (`Ruildag`, `ExtraDag`, `Incidenteel`, `Afwezig`) is informatief voor UI/historie; de telling kijkt alleen naar `StamgroepId`. Unieke sleutel `(KindId, Datum)`: hooguit één afwijking per kind per dag.

### De rekenregel (kern)

Voor een groep G op datum D bepaalt de **effectieve** indeling wie meetelt:

1. **Kind mét een dagplaatsing voor D:** de afwijking beslist volledig. Aanwezig op `StamgroepId`; bij `null` afwezig. Het reguliere patroon wordt voor die datum genegeerd.
2. **Kind zónder dagplaatsing voor D:** het reguliere patroon (`Aanwezigheid.IsKindAanwezigOp`) beslist, met de thuisgroep als groep.

Zo vallen de use-cases vanzelf op hun plek:

| Use-case | Modellering | Effect op telling |
|---|---|---|
| Incidenteel bij andere groep | 1 dagplaatsing (D, andere groep, `Incidenteel`) | thuisgroep −1, andere groep +1 op D |
| Ruildag (Di → Do) | 2 dagplaatsingen: (Di, `null`/afwezig) + (Do, thuisgroep, `Ruildag`) | Di −1, Do +1 die week |
| Extra dag | 1 dagplaatsing (D, groep, `ExtraDag`) | groep +1 op D |
| Kind afwezig | 1 dagplaatsing (D, `null`, `Afwezig`) | groep −1 op D |
| Kind start later | al gedekt door `Kind.Startdatum` | geen dagplaatsing nodig |

Omdat een afwijking een kind ook op een groep kan zetten die niet zijn thuisgroep is, moet de telling per dag over **alle** kinderen kijken (niet vooraf op thuisgroep filteren) en per kind de effectieve groep bepalen. Dat is precies wat de nieuwe `Dagindeling`-service doet.

## Geïmplementeerd in dit increment

- `Domain/Enums/DagplaatsingSoort.cs` — Ruildag / ExtraDag / Incidenteel / Afwezig.
- `Domain/Entiteiten/Dagplaatsing.cs` — de afwijkingsentiteit (`TenantEntiteit`).
- `Domain/Services/Dagindeling.cs` — pure rekenlogica: `EffectieveGroepOp(...)`, `OpGroepOpDag(...)`, `SamenstellingOpGroepOpDag(...)` (input voor de BKR). Bouwt voort op `Aanwezigheid`.
- EF-config + `DbSet<Dagplaatsing>` + tenant-queryfilter + migratie `Dagplaatsing`.
- Unit-tests voor alle rekenregels en randgevallen.

Niet-brekend: bestaande tellingen blijven werken; de nieuwe tabel is leeg tot er afwijkingen worden aangemaakt, en `Dagindeling` valt zonder afwijkingen exact samen met `Aanwezigheid`.

## Vervolgstappen (aparte increments)

1. **Wiring telling** — `WeekplanningBouwer` en de BKR-projectie via `Dagindeling` laten lopen i.p.v. het thuisgroep-gefilterde `Aanwezigheid`, zodat afwijkingen de groepsgrootte en BKR beïnvloeden.
2. **API + UI** — endpoints om een dagplaatsing te maken/verwijderen (ruildag, incidenteel, afwezig) en de weekplanning die laat aanmaken; plaatsingsoverzicht "waar is er plek" per groep/dag.
3. **Openstaande voorstellen meetellen** (H7-risico) — bij de BKR-projectie ook de nog-niet-geaccepteerde voorstellen als voorlopige dagplaatsingen meewegen om stille overschrijding te voorkomen.
