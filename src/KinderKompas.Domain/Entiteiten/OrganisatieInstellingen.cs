using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// De per-organisatie instelbare gedragsinstellingen (fase 9c). Eén rij per
/// organisatie (tenant-ready). Sterk getypeerd i.p.v. een generieke key-value-store,
/// zodat elke instelling z'n eigen validatie en betekenis houdt. De waarden hier
/// STUREN het gedrag van de modules aan (meldingen-zichtbaarheid, observatie-drempels,
/// mailtekst, wachtlijst-prioriteit); de defaults spiegelen de bestaande code-constanten.
/// </summary>
public class OrganisatieInstellingen : TenantEntiteit
{
    /// <summary>Default 'binnenkort'-drempel voor het uitstromen naar 4 jaar (zie <see cref="Kind.WordtBinnenkortVier"/>).</summary>
    public const int StandaardKindBinnenkortVierDagen = 90;

    /// <summary>
    /// De meldingsoorten die in het actiecentrum verborgen worden (UI-filter). De
    /// dispatcher blijft ze WÉL aanmaken — uitzetten verbergt alleen, zodat de historie
    /// volledig blijft. Opgeslagen als komma-gescheiden soort-nummers.
    /// </summary>
    public string VerborgenMeldingsoorten { get; set; } = "";

    /// <summary>Marge (dagen) waarbinnen een observatiemoment als "binnenkort" geldt (fase 7).</summary>
    public int ObservatieBinnenkortDrempelDagen { get; set; } = Observatieschema.StandaardBinnenkortDrempelDagen;

    /// <summary>Marge (dagen) waarbinnen een kind als "wordt binnenkort 4" geldt (uitstroom).</summary>
    public int KindBinnenkortVierDrempelDagen { get; set; } = StandaardKindBinnenkortVierDagen;

    /// <summary>
    /// De standaard mailtekst (body) bij een observatie naar de ouder. Mag de
    /// plaatshouder <c>{voornaam}</c> bevatten. Null = de ingebouwde standaardtekst.
    /// </summary>
    public string? StandaardObservatietekst { get; set; }

    /// <summary>Wachtlijst-prioriteit: bonuspunten voor een interne aanvraag (broertje/zusje, doorstroom).</summary>
    public int PrioriteitInternGewicht { get; set; } = WachtlijstPrioriteit.PuntenIntern;

    /// <summary>Wachtlijst-prioriteit: punten per volledige maand anciënniteit op de wachtlijst.</summary>
    public int PrioriteitPerMaandGewicht { get; set; } = WachtlijstPrioriteit.PuntenPerMaandWachtend;

    /// <summary>De verborgen meldingsoorten als verzameling.</summary>
    public IReadOnlySet<MeldingSoort> VerborgenSoorten()
    {
        var set = new HashSet<MeldingSoort>();
        foreach (string deel in VerborgenMeldingsoorten.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(deel, out int waarde) && Enum.IsDefined((MeldingSoort)waarde))
            {
                set.Add((MeldingSoort)waarde);
            }
        }
        return set;
    }

    /// <summary>Of een meldingsoort in het actiecentrum getoond mag worden.</summary>
    public bool IsSoortZichtbaar(MeldingSoort soort) => !VerborgenSoorten().Contains(soort);

    /// <summary>Stelt de verborgen meldingsoorten in (genormaliseerd: uniek en gesorteerd).</summary>
    public void ZetVerborgenSoorten(IEnumerable<MeldingSoort> soorten)
    {
        ArgumentNullException.ThrowIfNull(soorten);
        VerborgenMeldingsoorten = string.Join(',',
            soorten.Distinct().OrderBy(s => (int)s).Select(s => ((int)s).ToString()));
    }
}
