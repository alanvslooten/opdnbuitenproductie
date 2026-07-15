namespace KinderKompas.Domain.Enums;

/// <summary>
/// De status van één observatiemoment van een kind op een peildatum. De status
/// volgt uit de vervaldatum (afgeleid van geboortedatum + mijlpaal) ten opzichte
/// van de peildatum, en of het moment al is afgevinkt.
/// </summary>
public enum ObservatieStatus
{
    /// <summary>De vervaldatum ligt verder weg dan de "binnenkort"-drempel.</summary>
    NogNietAanDeBeurt = 0,

    /// <summary>De vervaldatum valt binnen de drempel (default 30 dagen) of is vandaag.</summary>
    Binnenkort = 1,

    /// <summary>De vervaldatum ligt in het verleden en het moment is niet afgevinkt.</summary>
    Overschreden = 2,

    /// <summary>Het moment is afgevinkt (er is een afgeronde observatie).</summary>
    Afgerond = 3,

    /// <summary>
    /// De vervaldatum viel vóór de opvang-startdatum van het kind: het moment lag
    /// vóór de opvang de verantwoordelijkheid kreeg. Telt NIET als overschreden
    /// (een kind dat op 7 maanden start hoort geen "overschreden" 6-maandenmoment
    /// te krijgen).
    /// </summary>
    VoorStartdatum = 4,
}
