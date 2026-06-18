namespace KinderKompas.Domain.Enums;

/// <summary>
/// Waarom een medewerker in het auto-rooster-voorstel staat: vanuit het vaste
/// basisrooster, of bijgeplaatst vanuit de beschikbaarheidslaag om een BKR-tekort
/// (door uitval/ziekte) op te vullen.
/// </summary>
public enum RoosterBron
{
    /// <summary>Vaste werkdag in de eigen thuisgroep (eerste roosterlaag).</summary>
    Vast = 0,

    /// <summary>Bijgeplaatst vanuit de beschikbaarheidslaag om de BKR te halen.</summary>
    Beschikbaar = 1
}
