using FluentValidation;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Wachtlijst;

/// <summary>
/// De controle-analyse die de voorstel-pop-up vóór het versturen toont: per
/// (openstaande) gewenste dag de huidige bezetting, de BKR-impact (huidig én mét
/// dit kind erbij, beide rechtstreeks uit de Domain-calculator), of er plek is en
/// zo niet wanneer er een plek vrijkomt. Plus de groepsgrootte-check op
/// stamgroepniveau.
/// </summary>
public sealed record VoorstelAnalyseDto(
    Guid InschrijvingId,
    string KindNaam,
    DateOnly GewensteStartdatum,
    Weekdag GewensteOpvangdagen,
    Weekdag OpenstaandeDagen,
    Contracttype Contracttype,
    Guid StamgroepId,
    string StamgroepNaam,
    int MaxKinderen,
    int AantalGeplaatstNu,
    bool GroepBlijftOnderMax,
    bool KandidaatBuitenOpvangleeftijd,
    Leeftijdsgroep? KandidaatLeeftijdsgroep,
    // Aantal voorlopige plaatsingen uit nog openstaande voorstellen dat is meegeteld
    // in de bezetting/BKR (>0 = let op dubbele plaatsing / stille BKR-overschrijding).
    int OpenVoorstellenMeegeteld,
    IReadOnlyList<VoorstelDagAnalyseDto> Dagen);

/// <summary>De BKR- en plek-analyse voor één gewenste opvangdag.</summary>
public sealed record VoorstelDagAnalyseDto(
    Weekdag Weekdag,
    DateOnly Peildatum,
    int AantalAanwezigNu,
    int? VereistePmersNu,
    int AantalAanwezigNa,
    int? VereistePmersNa,
    bool ExtraPmerNodig,
    bool PlekVrijOpStart,
    DateOnly? EersteVrijeDatum,
    bool BkrOverschrijdtNa,
    string? Melding);

/// <summary>Eén voorgestelde dag binnen een voorstel: weekdag + concrete startdatum (door de planner ingevuld).</summary>
public sealed record VoorstelDagInvoer(Weekdag Weekdag, DateOnly VoorgesteldeDatum);

/// <summary>
/// Invoermodel voor het versturen van een (deel)voorstel. <see cref="Dagen"/> is de
/// subset van de openstaande gewenste dagen; is dat een echte subset, dan is het een
/// deelvoorstel en blijven de overige dagen op de wachtlijst.
/// </summary>
public sealed record VoorstelInvoer(
    Guid StamgroepId,
    DateOnly VoorgesteldeStartdatum,
    Weekdag Dagen,
    IReadOnlyList<VoorstelDagInvoer> DagData,
    string? Notitie);

/// <summary>Leesmodel van één voorgestelde dag in de historie.</summary>
public sealed record VoorstelDagDto(Weekdag Weekdag, DateOnly VoorgesteldeDatum);

/// <summary>Leesmodel van één voorstel uit de voorstelhistorie van een inschrijving.</summary>
public sealed record VoorstelDto(
    Guid Id,
    Guid InschrijvingId,
    DateTime VerstuurdOp,
    Guid VoorgesteldeStamgroepId,
    string? VoorgesteldeStamgroepNaam,
    Weekdag VoorgesteldeDagen,
    bool IsDeelvoorstel,
    VoorstelStatus Status,
    DateTime? BeantwoordOp,
    string? Notitie,
    IReadOnlyList<VoorstelDagDto> Dagen);

/// <summary>Validatie voor voorstel-invoer (vorm; de dagen-subset-controle gebeurt in de controller).</summary>
public sealed class VoorstelInvoerValidator : AbstractValidator<VoorstelInvoer>
{
    public VoorstelInvoerValidator()
    {
        RuleFor(x => x.StamgroepId)
            .NotEqual(Guid.Empty).WithMessage("Kies een stamgroep voor het voorstel.");

        RuleFor(x => x.VoorgesteldeStartdatum)
            .NotEqual(default(DateOnly)).WithMessage("Een voorgestelde startdatum is verplicht.");

        RuleFor(x => x.Dagen)
            .NotEqual(Weekdag.Geen).WithMessage("Kies minstens één dag om voor te stellen.");

        RuleFor(x => x.DagData)
            .NotEmpty().WithMessage("Vul per voorgestelde dag een startdatum in.");

        RuleForEach(x => x.DagData).ChildRules(dag =>
        {
            dag.RuleFor(d => d.VoorgesteldeDatum)
                .NotEqual(default(DateOnly)).WithMessage("Vul voor elke dag een datum in.");
        });
    }
}
