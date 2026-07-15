using KinderKompas.Domain.Common;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een medewerker van de organisatie. De koppeling met een Identity-account
/// (login, 2FA) wordt in fase 3 ingevuld; tot die tijd blijft
/// <see cref="IdentityUserId"/> leeg en bestaat de medewerker als pure
/// stamgegevens-record voor planning en rooster.
/// </summary>
public class Medewerker : TenantEntiteit
{
    public required string Voornaam { get; set; }
    public required string Achternaam { get; set; }

    public Rol Rol { get; set; }

    /// <summary>
    /// Of deze medewerker meetelt voor de beroepskracht-kindratio (BKR). Standaard true
    /// voor gediplomeerde krachten; voor een <see cref="Rol.Stagiair"/> meestal false,
    /// maar een BBL-laatstejaars kan wél meetellen — daarom een aparte, instelbare vlag.
    /// </summary>
    public bool TeltMeeVoorBkr { get; set; } = true;

    /// <summary>
    /// Vaste werkdagen: de eerste roosterlaag. Deze dagen staan automatisch in
    /// elke week ingepland, tenzij er goedgekeurd verlof of ziekte tegenover staat.
    /// </summary>
    public Weekdag VasteWerkdagen { get; set; }

    /// <summary>
    /// Beschikbaarheidsdagen: de tweede roosterlaag. Geen vaste inzet, maar wél
    /// inzetbaar wanneer het auto-rooster extra bezetting nodig heeft (ziekte/uitval
    /// of een BKR-tekort op een vaste dag).
    /// </summary>
    public Weekdag Beschikbaarheidsdagen { get; set; }

    /// <summary>Contracturen per week.</summary>
    public decimal Contracturen { get; set; }

    // --- Contactgegevens (F-22) ---
    public string? Telefoon { get; set; }
    public string? Email { get; set; }
    public string? NoodcontactNaam { get; set; }
    public string? NoodcontactTelefoon { get; set; }

    // --- Contract (F-22): vast vs. tijdelijk + looptijd ---
    /// <summary>True = vast contract; false = tijdelijk.</summary>
    public bool ContractVast { get; set; }
    public DateOnly? Contractbegindatum { get; set; }
    /// <summary>Einddatum bij een tijdelijk contract; null bij een vast contract.</summary>
    public DateOnly? Contracteinddatum { get; set; }

    /// <summary>Resterende contractmaanden t.o.v. een peildatum (tijdelijk contract), of null.</summary>
    public int? ResterendeContractmaanden(DateOnly peildatum)
    {
        if (ContractVast || Contracteinddatum is not { } eind || eind < peildatum)
        {
            return null;
        }
        int maanden = (eind.Year - peildatum.Year) * 12 + eind.Month - peildatum.Month;
        if (eind.Day < peildatum.Day)
        {
            maanden--;
        }
        return Math.Max(0, maanden);
    }

    /// <summary>
    /// De vaste thuisgroep waarin deze medewerker standaard wordt ingepland.
    /// Optioneel: een flex-/invalkracht hoeft geen vaste groep te hebben.
    /// </summary>
    public Guid? VasteStamgroepId { get; set; }
    public Stamgroep? VasteStamgroep { get; set; }

    /// <summary>FK naar de ASP.NET Core Identity-gebruiker. Null tot fase 3.</summary>
    public string? IdentityUserId { get; set; }

    public Organisatie? Organisatie { get; set; }

    /// <summary>
    /// Of deze medewerker op een weekdag standaard wordt ingepland (vaste werkdag).
    /// </summary>
    public bool WerktVastOp(Weekdag dag) => VasteWerkdagen.HasFlag(dag);

    /// <summary>Of deze medewerker op een weekdag inzetbaar is bij uitval/tekort.</summary>
    public bool IsBeschikbaarOp(Weekdag dag) => Beschikbaarheidsdagen.HasFlag(dag);
}
