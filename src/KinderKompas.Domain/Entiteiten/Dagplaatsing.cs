using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een afwijking van het reguliere opvangpatroon van een kind op één specifieke dag —
/// de kern van de v3-dagplaatsing. De <b>thuisgroep</b> (<see cref="Kind.StamgroepId"/>)
/// blijft het vaste pedagogische anker (oudergegevens, mentor, observaties); een
/// dagplaatsing legt vast waar het kind op een concrete <see cref="Datum"/> daadwerkelijk
/// staat als dat afwijkt.
///
/// Alleen uitzonderingen worden opgeslagen; het reguliere patroon blijft de
/// <see cref="Kind.GewensteOpvangdagen"/> op de thuisgroep. Zo blijft de dataset klein
/// terwijl ruildagen, incidentele plaatsing op een andere groep, extra dagen en
/// afwezigheid per dag mogelijk worden.
///
/// Semantiek van <see cref="StamgroepId"/>:
///   - niet-null → het kind staat die dag op DIE groep (aanwezig);
///   - null      → het kind is die dag AFWEZIG (heft een reguliere opvangdag op).
///
/// Zie <see cref="Services.Dagindeling"/> voor de rekenregel en docs/dagplaatsing-ontwerp.md.
/// </summary>
public class Dagplaatsing : TenantEntiteit
{
    public Guid KindId { get; set; }
    public Kind? Kind { get; set; }

    /// <summary>De dag waarop deze afwijking geldt.</summary>
    public DateOnly Datum { get; set; }

    /// <summary>
    /// De groep waar het kind die dag staat. <c>null</c> betekent afwezig
    /// (het kind komt die dag niet).
    /// </summary>
    public Guid? StamgroepId { get; set; }
    public Stamgroep? Stamgroep { get; set; }

    /// <summary>Het soort afwijking (informatief voor UI/historie).</summary>
    public DagplaatsingSoort Soort { get; set; }

    /// <summary>Optionele toelichting (bijv. "geruild met dinsdag i.v.m. afspraak").</summary>
    public string? Notitie { get; set; }

    /// <summary>Of het kind deze dag aanwezig is volgens deze afwijking.</summary>
    public bool IsAanwezig => StamgroepId is not null;
}
