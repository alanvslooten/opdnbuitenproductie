using KinderKompas.Application.Observaties;
using Microsoft.Extensions.Logging;

namespace KinderKompas.Infrastructure.Observaties;

/// <summary>
/// Stub-implementatie van <see cref="IObservatieMailer"/>: verstuurt nog geen echte
/// e-mail, maar logt de verzending zodat de verzendactie + opslag end-to-end werken.
/// Wordt later vervangen door een echte maildienst (bijv. via Azure Communication
/// Services) zonder dat de aanroepers wijzigen.
/// </summary>
public sealed class ObservatieMailerStub : IObservatieMailer
{
    private readonly ILogger<ObservatieMailerStub> _log;

    public ObservatieMailerStub(ILogger<ObservatieMailerStub> log)
    {
        _log = log;
    }

    public Task VerstuurAsync(ObservatieMail mail, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mail);

        _log.LogInformation(
            "[STUB-MAIL] Observatie van {Kind} zou worden verstuurd naar {Email}. " +
            "Onderwerp: '{Onderwerp}', bijlage: '{Bestand}'.",
            mail.KindNaam, mail.NaarEmail, mail.Onderwerp, mail.BestandsNaam);

        return Task.CompletedTask;
    }
}
