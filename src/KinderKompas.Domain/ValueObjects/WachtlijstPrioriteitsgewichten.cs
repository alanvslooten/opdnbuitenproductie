using KinderKompas.Domain.Services;

namespace KinderKompas.Domain.ValueObjects;

/// <summary>
/// De configureerbare gewichten van de wachtlijst-prioriteitsberekening (fase 9c).
/// De berekening zelf blijft in <see cref="WachtlijstPrioriteit"/>; dit waarde-object
/// voert alleen de — door de Beheerder instelbare — gewichten in. <see cref="Standaard"/>
/// spiegelt de code-constanten zodat het gedrag zonder instelling identiek blijft.
/// </summary>
public sealed record WachtlijstPrioriteitsgewichten(int PuntenIntern, int PuntenPerMaandWachtend)
{
    public static WachtlijstPrioriteitsgewichten Standaard =>
        new(WachtlijstPrioriteit.PuntenIntern, WachtlijstPrioriteit.PuntenPerMaandWachtend);
}
