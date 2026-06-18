namespace KinderKompas.Domain.Enums;

/// <summary>
/// De status van één plaatsingsvoorstel binnen de voorstelhistorie van een
/// wachtlijst-inschrijving. Een voorstel start als <see cref="Verstuurd"/> en
/// wordt daarna beantwoord. Alleen een <see cref="Geaccepteerd"/> voorstel haalt
/// de voorgestelde dagen van de wachtlijst af.
/// </summary>
public enum VoorstelStatus
{
    /// <summary>Naar de ouder verstuurd, in afwachting van een reactie.</summary>
    Verstuurd = 0,

    /// <summary>Door de ouder geaccepteerd; de voorgestelde dagen zijn geplaatst.</summary>
    Geaccepteerd = 1,

    /// <summary>Door de ouder afgewezen; de dagen blijven op de wachtlijst staan.</summary>
    Afgewezen = 2,

    /// <summary>Door de organisatie ingetrokken voordat de ouder reageerde.</summary>
    Ingetrokken = 3
}
