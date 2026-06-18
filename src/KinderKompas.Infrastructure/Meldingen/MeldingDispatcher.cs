using KinderKompas.Application.Meldingen;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Meldingen;
using KinderKompas.Domain.Services;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Infrastructure.Meldingen;

/// <summary>
/// Persisteert meldingen voor het actiecentrum. De event→melding-vertaling leeft in de
/// pure <see cref="MeldingFabriek"/>; deze klasse voegt alleen de opslag + deduplicatie
/// toe. Dedup-regel: bestaat er al een nog-niet-afgehandelde melding met dezelfde
/// sleutel, dan wordt die ververst (tekst bijgewerkt en teruggezet op ongelezen) i.p.v.
/// een nieuwe rij. De tenant-sleutel wordt automatisch door de DbContext gezet.
/// </summary>
public sealed class MeldingDispatcher : IMeldingDispatcher
{
    private readonly KinderKompasDbContext _db;

    public MeldingDispatcher(KinderKompasDbContext db)
    {
        _db = db;
    }

    public async Task PubliceerAsync(MeldingGebeurtenis gebeurtenis, CancellationToken ct = default)
    {
        Melding nieuw = MeldingFabriek.Maak(gebeurtenis);

        if (nieuw.DeduplicatieSleutel is { } sleutel)
        {
            Melding? bestaand = await _db.Meldingen.FirstOrDefaultAsync(
                m => m.DeduplicatieSleutel == sleutel && m.Status != MeldingStatus.Afgehandeld, ct);

            if (bestaand is not null)
            {
                // Ververs de bestaande melding i.p.v. dubbel toe te voegen, en haal 'm
                // terug naar ongelezen zodat de heropleving in het actiecentrum opvalt.
                bestaand.Titel = nieuw.Titel;
                bestaand.Tekst = nieuw.Tekst;
                bestaand.Status = MeldingStatus.Ongelezen;
                await _db.SaveChangesAsync(ct);
                return;
            }
        }

        _db.Meldingen.Add(nieuw);
        await _db.SaveChangesAsync(ct);
    }
}
