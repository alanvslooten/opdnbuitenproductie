using KinderKompas.Domain.Meldingen;

namespace KinderKompas.Application.Meldingen;

/// <summary>
/// Het publicatiepunt voor het actiecentrum (fase 9): een module roept dit aan op een
/// betekenisvol moment met een <see cref="MeldingGebeurtenis"/>. De implementatie
/// (Infrastructure) bouwt via de <c>MeldingFabriek</c> een melding en zet die weg,
/// met deduplicatie zodat herhaalde triggers het actiecentrum niet vol spammen.
///
/// Controllers/use-cases hangen NOOIT rechtstreeks aan de DbContext voor meldingen;
/// ze publiceren een event en blijven zo los van de meldingen-opslag.
/// </summary>
public interface IMeldingDispatcher
{
    Task PubliceerAsync(MeldingGebeurtenis gebeurtenis, CancellationToken ct = default);
}
