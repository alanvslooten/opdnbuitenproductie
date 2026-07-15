using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Services;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Eén dienst: een medewerker die op een concrete dag in een stamgroep is
/// ingepland. De cel-kleur in het rooster (standaard/verlof/ziek) wordt NIET hier
/// opgeslagen maar afgeleid uit verlof- en ziektegegevens; deze entiteit legt
/// puur de inzet, de taak en de urencorrectie vast.
/// </summary>
public class Roosterdienst : TenantEntiteit
{
    public Guid RoosterweekId { get; set; }
    public Roosterweek? Roosterweek { get; set; }

    public Guid MedewerkerId { get; set; }
    public Medewerker? Medewerker { get; set; }

    public Guid StamgroepId { get; set; }
    public Stamgroep? Stamgroep { get; set; }

    /// <summary>De concrete opvangdag van deze dienst (ma-vr binnen de roosterweek).</summary>
    public DateOnly Datum { get; set; }

    /// <summary>Optionele taakomschrijving voor deze medewerker op deze dag (zichtbaar in het portaal).</summary>
    public string? Taakomschrijving { get; set; }

    /// <summary>Soort dienst: regulier, vroege (openen) of late (sluiten) dienst.</summary>
    public Dienstsoort Dienstsoort { get; set; } = Dienstsoort.Regulier;

    /// <summary>
    /// Begintijd van de dienst. Null = de standaardtijd van de <see cref="Dienstsoort"/>
    /// (zie <see cref="EffectieveBegintijd"/>); een expliciete waarde overschrijft die.
    /// </summary>
    public TimeOnly? Begintijd { get; set; }

    /// <summary>Eindtijd van de dienst. Null = de standaardtijd van de <see cref="Dienstsoort"/>.</summary>
    public TimeOnly? Eindtijd { get; set; }

    /// <summary>De effectieve begintijd: de expliciete waarde of anders de soort-standaard.</summary>
    public TimeOnly EffectieveBegintijd => Begintijd ?? Diensttijden.Standaard(Dienstsoort).Begin;

    /// <summary>De effectieve eindtijd: de expliciete waarde of anders de soort-standaard.</summary>
    public TimeOnly EffectieveEindtijd => Eindtijd ?? Diensttijden.Standaard(Dienstsoort).Eind;

    /// <summary>De (onbetaalde) pauze van deze dienst op basis van de duur.</summary>
    public TimeSpan Pauze => Diensttijden.Pauze(EffectieveBegintijd, EffectieveEindtijd);

    /// <summary>
    /// De netto geplande uren: de dienstduur minus de onbetaalde pauze, plus de
    /// handmatige urencorrectie. Dit is de "gepland"-kant van meer-/minderwerk.
    /// </summary>
    public decimal GeplandeUren =>
        Diensttijden.NettoUren(EffectieveBegintijd, EffectieveEindtijd) + UrencorrectieUren;

    /// <summary>
    /// Urenregistratie als plus/min ten opzichte van de standaard dienstduur,
    /// geteld in kwartieren (1 = +15 min, -2 = -30 min). Zo blijft het in hele
    /// kwartieren zonder afrondingsproblemen op decimalen.
    /// </summary>
    public int UrencorrectieKwartieren { get; set; }

    /// <summary>De urencorrectie omgerekend naar uren (kwartieren / 4).</summary>
    public decimal UrencorrectieUren => UrencorrectieKwartieren / 4m;
}
