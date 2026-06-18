namespace KinderKompas.Application.Observaties;

/// <summary>
/// Verstuurt een afgeronde observatie (met PDF-bijlage) naar de ouder. In deze fase
/// is de implementatie een stub die de verzending logt/markeert; de echte
/// e-mailkoppeling (bijv. via een Azure-maildienst) volgt later. De abstractie staat
/// er nu al zodat de verzendactie en de opslag volledig werken.
/// </summary>
public interface IObservatieMailer
{
    Task VerstuurAsync(ObservatieMail mail, CancellationToken ct = default);
}

/// <summary>De inhoud van een te versturen observatie-mail, inclusief de PDF-bijlage.</summary>
/// <param name="NaarEmail">Het e-mailadres van de ouder/verzorger.</param>
/// <param name="KindNaam">Volledige naam van het kind (voor aanhef/onderwerp).</param>
/// <param name="Onderwerp">Onderwerpregel.</param>
/// <param name="Tekst">De (standaard) berichttekst.</param>
/// <param name="BestandsNaam">Bestandsnaam van de PDF-bijlage.</param>
/// <param name="Bijlage">De PDF-inhoud.</param>
public sealed record ObservatieMail(
    string NaarEmail,
    string KindNaam,
    string Onderwerp,
    string Tekst,
    string BestandsNaam,
    Stream Bijlage);
