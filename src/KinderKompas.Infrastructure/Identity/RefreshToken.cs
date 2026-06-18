namespace KinderKompas.Infrastructure.Identity;

/// <summary>
/// Een refresh-token waarmee de SPA een nieuw, kortlevend JWT kan ophalen zonder
/// dat de gebruiker opnieuw hoeft in te loggen. De token-waarde wordt GEHASHT
/// opgeslagen (<see cref="TokenHash"/>): lekt de database, dan zijn de tokens
/// niet bruikbaar. Rotatie: bij gebruik wordt de token ingetrokken en vervangen.
/// Leeft in Infrastructure omdat het puur een auth-/sessieconstruct is.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>SHA-256-hash van de uitgegeven token-waarde (niet de waarde zelf).</summary>
    public required string TokenHash { get; set; }

    public required string ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    public Guid OrganisatieId { get; set; }

    public DateTime AangemaaktOp { get; set; }
    public DateTime VerlooptOp { get; set; }

    /// <summary>Gezet zodra de token is gebruikt of expliciet is ingetrokken.</summary>
    public DateTime? IngetrokkenOp { get; set; }

    /// <summary>Hash van de opvolger-token na rotatie (voor hergebruik-detectie).</summary>
    public string? VervangenDoorTokenHash { get; set; }

    public bool IsActief => IngetrokkenOp is null && DateTime.UtcNow < VerlooptOp;
}
