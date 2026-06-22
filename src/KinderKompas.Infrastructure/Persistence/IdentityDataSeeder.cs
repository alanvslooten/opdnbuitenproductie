using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;
using KinderKompas.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KinderKompas.Infrastructure.Persistence;

/// <summary>
/// Seedt bij het opstarten de rollen en een verstandige set accounts: de
/// Beheerder (Gail), een paar medewerkers, en het gedeelde Groepsportaal-account.
/// Idempotent: bestaat een account al, dan wordt het overgeslagen.
///
/// 2FA is VERPLICHT voor Beheerder en Groepsportaal: voor die accounts wordt bij
/// het aanmaken meteen een authenticator-sleutel gezet en 2FA ingeschakeld. De
/// koppel-URI wordt (alleen in Development) gelogd zodat je hem in een
/// authenticator-app kunt scannen/invoeren.
/// </summary>
public sealed class IdentityDataSeeder
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _rollen;
    private readonly KinderKompasDbContext _db;
    private readonly ITokenService _tokens;
    private readonly ILogger<IdentityDataSeeder> _log;

    public IdentityDataSeeder(
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> rollen,
        KinderKompasDbContext db,
        ITokenService tokens,
        ILogger<IdentityDataSeeder> log)
    {
        _users = users;
        _rollen = rollen;
        _db = db;
        _tokens = tokens;
        _log = log;
    }

    private sealed record AccountSeed(
        string Gebruikersnaam,
        string Email,
        string Wachtwoord,
        Rol Rol,
        bool TweeFactorVerplicht,
        string? Voornaam,
        string? Achternaam,
        Weekdag VasteWerkdagen = Weekdag.Geen,
        Weekdag Beschikbaarheidsdagen = Weekdag.Geen,
        Guid? VasteStamgroepId = null);

    private static readonly AccountSeed[] Accounts =
    {
        new("gail", "gail@opdnbuiten.nl", "Beheerder!2026", Rol.Beheerder, true, "Gail", "Beheerder",
            Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag, Weekdag.Donderdag, SeedConstanten.StamgroepBengeltjesId),
        new("sanne", "sanne@opdnbuiten.nl", "Senior!2026", Rol.Senior, false, "Sanne", "Senior",
            Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag, Weekdag.Donderdag | Weekdag.Vrijdag, SeedConstanten.StamgroepBengeltjesId),
        new("jasper", "jasper@opdnbuiten.nl", "Junior!2026", Rol.Junior, false, "Jasper", "Junior",
            Weekdag.Maandag | Weekdag.Dinsdag | Weekdag.Woensdag, Weekdag.Donderdag, SeedConstanten.StamgroepBoefjesId),
        // Eén Groepsportaal-account per stamgroep (eigen tablet): alle portaal-data is
        // tot die groep beperkt via ApplicationUser.StamgroepId (feedback Erik V2).
        new("groepsportaal-bengeltjes", "bengeltjes@opdnbuiten.nl", "Portaal!2026", Rol.Groepsportaal, true,
            null, null, VasteStamgroepId: SeedConstanten.StamgroepBengeltjesId),
        new("groepsportaal-boefjes", "boefjes@opdnbuiten.nl", "Portaal!2026", Rol.Groepsportaal, true,
            null, null, VasteStamgroepId: SeedConstanten.StamgroepBoefjesId),
    };

    public async Task SeedAsync(bool isDevelopment, bool tweeFactorVerplichten, CancellationToken ct = default)
    {
        await SeedRollenAsync();

        foreach (AccountSeed account in Accounts)
        {
            await SeedAccountAsync(account, isDevelopment, ct);
        }

        await ReconcileTweeFactorAsync(tweeFactorVerplichten, ct);
        await SeedDemoKindAsync(ct);
        await BackfillRoosterbasisAsync(ct);
    }

    /// <summary>
    /// Brengt de 2FA-status van de verplichte-2FA-accounts (Beheerder, Groepsportaal)
    /// in lijn met de config <c>Auth:TweeFactorVerplichten</c>. Werkt óók op reeds
    /// bestaande accounts in de database, zodat 2FA voor een demo uitgezet (of weer
    /// aangezet) kan worden zonder de accounts opnieuw aan te maken. Idempotent.
    /// </summary>
    private async Task ReconcileTweeFactorAsync(bool verplichten, CancellationToken ct)
    {
        foreach (AccountSeed account in Accounts)
        {
            if (!account.TweeFactorVerplicht)
            {
                continue;
            }

            ApplicationUser? gebruiker = await _users.FindByNameAsync(account.Gebruikersnaam);
            if (gebruiker is null || await _users.GetTwoFactorEnabledAsync(gebruiker) == verplichten)
            {
                continue;
            }

            // Bij (her)inschakelen: zorg dat er een authenticator-sleutel is.
            if (verplichten && string.IsNullOrEmpty(await _users.GetAuthenticatorKeyAsync(gebruiker)))
            {
                await _users.ResetAuthenticatorKeyAsync(gebruiker);
            }

            await _users.SetTwoFactorEnabledAsync(gebruiker, verplichten);
            _log.LogWarning("2FA voor '{Gebruiker}' staat nu {Status} (config Auth:TweeFactorVerplichten).",
                account.Gebruikersnaam, verplichten ? "AAN" : "UIT");
        }
    }

    /// <summary>
    /// Vult voor reeds bestaande medewerkers (uit eerdere fasen) de roosterbasis aan
    /// als die nog ontbreekt: vaste thuisgroep en beschikbaarheidsdagen volgens de
    /// seed-definitie. Idempotent: medewerkers die al een vaste groep hebben blijven
    /// ongemoeid. Zo is het auto-rooster van fase 5 ook op een bestaande database te tonen.
    /// </summary>
    private async Task BackfillRoosterbasisAsync(CancellationToken ct)
    {
        foreach (AccountSeed account in Accounts)
        {
            if (account.Voornaam is null || account.VasteStamgroepId is null)
            {
                continue;
            }

            Medewerker? medewerker = await _db.Medewerkers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    m => m.Voornaam == account.Voornaam && m.Achternaam == account.Achternaam, ct);

            if (medewerker is null || medewerker.VasteStamgroepId is not null)
            {
                continue;
            }

            medewerker.VasteStamgroepId = account.VasteStamgroepId;
            medewerker.VasteWerkdagen = account.VasteWerkdagen;
            medewerker.Beschikbaarheidsdagen = account.Beschikbaarheidsdagen;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Seedt één demo-kind met oudercontact, zodat het privacy-onderscheid op het
    /// kinderen-endpoint zichtbaar is. Idempotent: bestaat er al een kind, dan niets.
    /// </summary>
    private async Task SeedDemoKindAsync(CancellationToken ct)
    {
        bool bestaat = await _db.Kinderen.IgnoreQueryFilters().AnyAsync(ct);
        if (bestaat)
        {
            return;
        }

        // Sanne (Senior) als mentor, zodat de mentor-zichtbaarheid van observaties
        // direct te demonstreren is: zij ziet Fenna wél, andere medewerkers niet.
        Medewerker? mentor = await _db.Medewerkers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Voornaam == "Sanne" && m.Achternaam == "Senior", ct);

        _db.Kinderen.Add(new Kind
        {
            OrganisatieId = SeedConstanten.OrganisatieId,
            StamgroepId = SeedConstanten.StamgroepBengeltjesId,
            Voornaam = "Fenna",
            Achternaam = "de Vries",
            Geboortedatum = new DateOnly(2023, 4, 1),
            Startdatum = new DateOnly(2023, 7, 1),
            Contracttype = Contracttype.Weken49,
            GewensteOpvangdagen = Weekdag.Maandag | Weekdag.Dinsdag,
            MentorId = mentor?.Id,
            Oudercontacten =
            {
                new Oudercontact("Mark de Vries", "0612345678", "mark@example.nl"),
                new Oudercontact("Petra de Vries", "0698765432", "petra@example.nl"),
            },
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedRollenAsync()
    {
        foreach (Rol rol in Enum.GetValues<Rol>())
        {
            string naam = rol.ToString();
            if (!await _rollen.RoleExistsAsync(naam))
            {
                await _rollen.CreateAsync(new IdentityRole(naam));
            }
        }
    }

    private async Task SeedAccountAsync(AccountSeed account, bool isDevelopment, CancellationToken ct)
    {
        if (await _users.FindByNameAsync(account.Gebruikersnaam) is not null)
        {
            return;
        }

        Guid? medewerkerId = null;
        if (account.Voornaam is not null && account.Achternaam is not null)
        {
            var medewerker = new Medewerker
            {
                OrganisatieId = SeedConstanten.OrganisatieId,
                Voornaam = account.Voornaam,
                Achternaam = account.Achternaam,
                Rol = account.Rol,
                VasteWerkdagen = account.VasteWerkdagen,
                Beschikbaarheidsdagen = account.Beschikbaarheidsdagen,
                VasteStamgroepId = account.VasteStamgroepId,
                Contracturen = 24m,
            };
            _db.Medewerkers.Add(medewerker);
            await _db.SaveChangesAsync(ct);
            medewerkerId = medewerker.Id;
        }

        var gebruiker = new ApplicationUser
        {
            UserName = account.Gebruikersnaam,
            Email = account.Email,
            EmailConfirmed = true,
            OrganisatieId = SeedConstanten.OrganisatieId,
            MedewerkerId = medewerkerId,
            // Een Groepsportaal-account hangt vast aan zijn stamgroep; persoonlijke
            // accounts niet (die scopen via hun eigen medewerker/portaal).
            StamgroepId = account.Rol == Rol.Groepsportaal ? account.VasteStamgroepId : null,
        };

        IdentityResult aangemaakt = await _users.CreateAsync(gebruiker, account.Wachtwoord);
        if (!aangemaakt.Succeeded)
        {
            _log.LogError("Aanmaken account {Gebruiker} mislukt: {Fouten}",
                account.Gebruikersnaam, string.Join("; ", aangemaakt.Errors.Select(e => e.Description)));
            return;
        }

        await _users.AddToRoleAsync(gebruiker, account.Rol.ToString());

        // Domein <-> account koppelen (beide richtingen).
        if (medewerkerId is { } mid)
        {
            Medewerker? medewerker = await _db.Medewerkers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.Id == mid, ct);
            if (medewerker is not null)
            {
                medewerker.IdentityUserId = gebruiker.Id;
                await _db.SaveChangesAsync(ct);
            }
        }

        if (account.TweeFactorVerplicht)
        {
            await _users.ResetAuthenticatorKeyAsync(gebruiker);
            await _users.SetTwoFactorEnabledAsync(gebruiker, true);

            if (isDevelopment)
            {
                string? sleutel = await _users.GetAuthenticatorKeyAsync(gebruiker);
                _log.LogWarning(
                    "2FA verplicht voor '{Gebruiker}'. Authenticator-sleutel (alleen dev): {Sleutel}",
                    account.Gebruikersnaam, sleutel);
            }
        }

        _log.LogInformation("Account '{Gebruiker}' geseed met rol {Rol} (2FA: {Tfa}).",
            account.Gebruikersnaam, account.Rol, account.TweeFactorVerplicht);
    }
}
