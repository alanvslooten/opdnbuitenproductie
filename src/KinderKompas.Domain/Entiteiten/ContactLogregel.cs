using KinderKompas.Domain.Common;

namespace KinderKompas.Domain.Entiteiten;

/// <summary>
/// Eén regel in het wijzigingslogboek van een contact (v3): een korte, menselijke
/// beschrijving van een relevante gebeurtenis — bijvoorbeeld "Gewenste dagen gewijzigd
/// naar Ma, Wo" of "Voorstel verstuurd voor Boefjes". Zo ziet de planner in één oogopslag
/// de geschiedenis ("vier keer van dag gewisseld"). Het tijdstip is <see cref="Entiteit.AangemaaktOp"/>.
/// </summary>
public class ContactLogregel : TenantEntiteit
{
    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }

    /// <summary>Korte, leesbare omschrijving van de gebeurtenis.</summary>
    public required string Omschrijving { get; set; }
}
