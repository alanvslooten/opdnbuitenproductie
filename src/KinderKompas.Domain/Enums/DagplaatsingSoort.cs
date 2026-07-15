namespace KinderKompas.Domain.Enums;

/// <summary>
/// Het soort dagafwijking (<see cref="Entiteiten.Dagplaatsing"/>) t.o.v. het reguliere
/// opvangpatroon van een kind. Informatief voor UI en historie; de dag- en BKR-telling
/// kijkt alleen naar de (nullable) groep van de afwijking, niet naar deze soort.
/// </summary>
public enum DagplaatsingSoort
{
    /// <summary>Ruildag: het kind komt op een andere dag/groep dan gewoonlijk (vervangt een reguliere dag).</summary>
    Ruildag = 0,

    /// <summary>Extra dag bovenop het reguliere patroon.</summary>
    ExtraDag = 1,

    /// <summary>Incidenteel op een andere groep dan de thuisgroep op een reguliere opvangdag.</summary>
    Incidenteel = 2,

    /// <summary>Afwezig: het kind is deze dag niet aanwezig (heft een reguliere opvangdag op).</summary>
    Afwezig = 3,
}
