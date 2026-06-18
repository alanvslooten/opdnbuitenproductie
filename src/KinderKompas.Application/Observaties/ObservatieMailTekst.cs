namespace KinderKompas.Application.Observaties;

/// <summary>
/// Stelt de standaard-mailtekst samen waarmee een observatie naar de ouder gaat.
/// Eén plek voor de bewoording, zodat ze consistent is en later (fase 9) instelbaar
/// kan worden.
/// </summary>
public static class ObservatieMailTekst
{
    public static string Onderwerp(string kindNaam) =>
        $"Observatie van {kindNaam} — Op d'n Buiten";

    public static string Bericht(string kindVoornaam) =>
        $"""
        Beste ouder/verzorger,

        In de bijlage vind je de nieuwe observatie van {kindVoornaam}. We bespreken
        deze graag met je tijdens een volgend oudergesprek. Heb je tussentijds vragen,
        loop dan gerust even binnen.

        Met vriendelijke groet,
        Team Op d'n Buiten
        """;

    /// <summary>
    /// De berichttekst op basis van het door de Beheerder ingestelde sjabloon (fase 9c).
    /// Is er geen sjabloon ingesteld (null/leeg), dan geldt de ingebouwde standaardtekst.
    /// In het sjabloon wordt de plaatshouder <c>{voornaam}</c> vervangen door de voornaam.
    /// </summary>
    public static string Bericht(string kindVoornaam, string? sjabloon) =>
        string.IsNullOrWhiteSpace(sjabloon)
            ? Bericht(kindVoornaam)
            : sjabloon.Replace("{voornaam}", kindVoornaam, StringComparison.OrdinalIgnoreCase);
}
