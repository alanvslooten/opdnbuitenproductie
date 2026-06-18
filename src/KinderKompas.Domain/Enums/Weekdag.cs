namespace KinderKompas.Domain.Enums;

/// <summary>
/// Weekdagen als bit-vlaggen, zodat een verzameling dagen (gewenste
/// opvangdagen van een kind, vaste werkdagen van een medewerker) in één veld
/// past. Alleen opvangdagen ma t/m vr; weekend valt buiten de opvang.
/// </summary>
[Flags]
public enum Weekdag
{
    Geen = 0,
    Maandag = 1 << 0,
    Dinsdag = 1 << 1,
    Woensdag = 1 << 2,
    Donderdag = 1 << 3,
    Vrijdag = 1 << 4
}
