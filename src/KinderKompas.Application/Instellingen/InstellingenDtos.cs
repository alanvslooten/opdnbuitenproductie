using FluentValidation;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Instellingen;

/// <summary>
/// Leesmodel van de gedragsinstellingen van de organisatie (fase 9c). De verborgen
/// meldingsoorten komen als nummers over de lijn (zoals de enum), passend bij de rest
/// van het API-contract.
/// </summary>
public sealed record InstellingenDto(
    IReadOnlyList<int> VerborgenMeldingsoorten,
    int ObservatieBinnenkortDrempelDagen,
    int KindBinnenkortVierDrempelDagen,
    string? StandaardObservatietekst,
    int PrioriteitInternGewicht,
    int PrioriteitPerMaandGewicht);

/// <summary>Invoermodel om de gedragsinstellingen bij te werken (volledige vervanging).</summary>
public sealed record InstellingenInvoer(
    IReadOnlyList<int> VerborgenMeldingsoorten,
    int ObservatieBinnenkortDrempelDagen,
    int KindBinnenkortVierDrempelDagen,
    string? StandaardObservatietekst,
    int PrioriteitInternGewicht,
    int PrioriteitPerMaandGewicht);

/// <summary>
/// Validatie voor de instellingen. Drempels in een redelijk dagbereik, gewichten
/// niet-negatief en begrensd, mailtekst beperkt in lengte, en elke opgegeven
/// meldingsoort moet een bestaande enum-waarde zijn.
/// </summary>
public sealed class InstellingenInvoerValidator : AbstractValidator<InstellingenInvoer>
{
    public InstellingenInvoerValidator()
    {
        RuleFor(x => x.ObservatieBinnenkortDrempelDagen)
            .InclusiveBetween(1, 365)
            .WithMessage("De observatie-drempel ligt tussen 1 en 365 dagen.");

        RuleFor(x => x.KindBinnenkortVierDrempelDagen)
            .InclusiveBetween(1, 365)
            .WithMessage("De 'binnenkort 4'-drempel ligt tussen 1 en 365 dagen.");

        RuleFor(x => x.PrioriteitInternGewicht)
            .InclusiveBetween(0, 100_000)
            .WithMessage("Het interne-gewicht ligt tussen 0 en 100.000 punten.");

        RuleFor(x => x.PrioriteitPerMaandGewicht)
            .InclusiveBetween(0, 100_000)
            .WithMessage("Het gewicht per maand ligt tussen 0 en 100.000 punten.");

        RuleFor(x => x.StandaardObservatietekst)
            .MaximumLength(4000)
            .WithMessage("De standaard observatietekst mag maximaal 4000 tekens zijn.");

        RuleForEach(x => x.VerborgenMeldingsoorten)
            .Must(waarde => Enum.IsDefined((MeldingSoort)waarde))
            .WithMessage("Onbekende meldingsoort.");
    }
}

/// <summary>Leesmodel van de locatiegegevens van de organisatie.</summary>
public sealed record LocatieDto(string Naam, string Lrknummer);

/// <summary>Invoermodel om de locatiegegevens bij te werken.</summary>
public sealed record LocatieInvoer(string Naam, string Lrknummer);

/// <summary>
/// Validatie voor de locatiegegevens. Het LRK-nummer (Landelijk Register Kinderopvang)
/// is numeriek; de naam is verplicht.
/// </summary>
public sealed class LocatieInvoerValidator : AbstractValidator<LocatieInvoer>
{
    public LocatieInvoerValidator()
    {
        RuleFor(x => x.Naam)
            .NotEmpty().WithMessage("De organisatie moet een naam hebben.")
            .MaximumLength(200);

        RuleFor(x => x.Lrknummer)
            .NotEmpty().WithMessage("Het LRK-nummer is verplicht.")
            .MaximumLength(50)
            .Matches(@"^\d+$").WithMessage("Het LRK-nummer bestaat uit cijfers.");
    }
}

/// <summary>Projecteert en wijzigt de <see cref="OrganisatieInstellingen"/>-entiteit.</summary>
public static class InstellingenMapper
{
    public static InstellingenDto NaarDto(OrganisatieInstellingen i) => new(
        i.VerborgenSoorten().Select(s => (int)s).OrderBy(x => x).ToList(),
        i.ObservatieBinnenkortDrempelDagen,
        i.KindBinnenkortVierDrempelDagen,
        i.StandaardObservatietekst,
        i.PrioriteitInternGewicht,
        i.PrioriteitPerMaandGewicht);

    public static void PasInvoerToe(OrganisatieInstellingen i, InstellingenInvoer invoer)
    {
        i.ZetVerborgenSoorten(invoer.VerborgenMeldingsoorten.Select(w => (MeldingSoort)w));
        i.ObservatieBinnenkortDrempelDagen = invoer.ObservatieBinnenkortDrempelDagen;
        i.KindBinnenkortVierDrempelDagen = invoer.KindBinnenkortVierDrempelDagen;
        i.StandaardObservatietekst = string.IsNullOrWhiteSpace(invoer.StandaardObservatietekst)
            ? null
            : invoer.StandaardObservatietekst;
        i.PrioriteitInternGewicht = invoer.PrioriteitInternGewicht;
        i.PrioriteitPerMaandGewicht = invoer.PrioriteitPerMaandGewicht;
    }
}
