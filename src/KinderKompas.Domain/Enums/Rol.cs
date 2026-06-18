namespace KinderKompas.Domain.Enums;

/// <summary>
/// Functionele rol van een medewerker. Stuurt later (fase 3) de autorisatie
/// aan. Het Groepsportaal is een gedeeld account op de tablet op locatie en
/// geen persoon, maar wordt hier als rol meegenomen voor uniforme afhandeling.
/// </summary>
public enum Rol
{
    Beheerder = 0,
    Hulpbeheerder = 1,
    Senior = 2,
    Junior = 3,
    Groepsportaal = 4
}
