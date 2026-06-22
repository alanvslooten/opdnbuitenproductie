using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Een contact in de Contacten-module: een ouder/verzorger/voogd (of gezin) dat met
/// de opvang in contact staat. Het is het CRM-knooppunt waaraan de historie hangt:
/// rondleidingen, wachtlijst-inschrijvingen (met hun voorstellen) en geplaatste
/// kinderen. Eén contact kan dus meerdere kinderen en inschrijvingen omvatten.
/// </summary>
public class Contact : TenantEntiteit
{
    public required string Voornaam { get; set; }
    public required string Achternaam { get; set; }

    public string? Telefoon { get; set; }
    public string? Email { get; set; }

    /// <summary>
    /// Intern = heeft al een band met de opvang (bv. al een kind geplaatst, broertje/
    /// zusje op de wachtlijst); extern = nieuwe aanmelding zonder bestaande band.
    /// </summary>
    public bool IsIntern { get; set; }

    /// <summary>Vrije aantekeningen over het contact.</summary>
    public string? Aantekeningen { get; set; }

    public Organisatie? Organisatie { get; set; }

    public ICollection<Rondleiding> Rondleidingen { get; set; } = new List<Rondleiding>();
    public ICollection<WachtlijstInschrijving> Inschrijvingen { get; set; } = new List<WachtlijstInschrijving>();
    public ICollection<Kind> Kinderen { get; set; } = new List<Kind>();

    public string VolledigeNaam => $"{Voornaam} {Achternaam}";
}
