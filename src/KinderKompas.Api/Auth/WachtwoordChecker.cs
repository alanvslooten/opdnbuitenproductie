using KinderKompas.Application.Abstractions;
using KinderKompas.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KinderKompas.Api.Auth;

/// <summary>Bevestiging met wachtwoord voor een kritieke actie (2-stapscheck).</summary>
public sealed record BevestigInvoer(string? Wachtwoord);

/// <summary>
/// Verifieert wachtwoorden voor bevestigingsacties: de 2-stapscheck bij kritieke
/// verwijderingen (huidige beheerder) én de identiteitscheck bij in-/uitklokken op het
/// Groepsportaal (de medewerker bevestigt met het eigen account-wachtwoord — geen pincode).
/// </summary>
public sealed class WachtwoordChecker
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ICurrentUser _huidigeGebruiker;

    public WachtwoordChecker(UserManager<ApplicationUser> users, ICurrentUser huidigeGebruiker)
    {
        _users = users;
        _huidigeGebruiker = huidigeGebruiker;
    }

    /// <summary>Klopt het wachtwoord van de ingelogde gebruiker?</summary>
    public async Task<bool> KloptHuidigeGebruikerAsync(string? wachtwoord)
    {
        if (string.IsNullOrEmpty(wachtwoord) || _huidigeGebruiker.UserId is not { } uid)
        {
            return false;
        }
        ApplicationUser? gebruiker = await _users.FindByIdAsync(uid);
        return gebruiker is not null && await _users.CheckPasswordAsync(gebruiker, wachtwoord);
    }

    /// <summary>
    /// Klopt het account-wachtwoord van de gegeven medewerker? Resultaat:
    /// - true  → wachtwoord klopt;
    /// - false → wachtwoord onjuist;
    /// - null  → de medewerker heeft (nog) geen gekoppeld account, dus geen verificatie mogelijk.
    /// </summary>
    public async Task<bool?> KloptVoorMedewerkerAsync(Guid medewerkerId, string? wachtwoord)
    {
        ApplicationUser? gebruiker = await _users.Users
            .FirstOrDefaultAsync(u => u.MedewerkerId == medewerkerId);
        if (gebruiker is null)
        {
            return null;
        }
        return !string.IsNullOrEmpty(wachtwoord) && await _users.CheckPasswordAsync(gebruiker, wachtwoord);
    }
}
