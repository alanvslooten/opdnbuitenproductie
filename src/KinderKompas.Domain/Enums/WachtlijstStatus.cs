namespace KinderKompas.Domain.Enums;

/// <summary>
/// De status van een wachtlijst-inschrijving. Een inschrijving blijft
/// <see cref="Wachtend"/> zolang er nog gewenste dagen openstaan — ook na een
/// geaccepteerd <em>deelvoorstel</em> (de resterende dagen staan dan nog op de
/// wachtlijst). Pas als alle gewenste dagen gedekt zijn wordt het
/// <see cref="Geplaatst"/>.
/// </summary>
public enum WachtlijstStatus
{
    /// <summary>Staat (deels) op de wachtlijst; er zijn nog openstaande gewenste dagen.</summary>
    Wachtend = 0,

    /// <summary>Alle gewenste dagen zijn via (deel)voorstellen geplaatst.</summary>
    Geplaatst = 1,

    /// <summary>De inschrijving is ingetrokken (ouder heeft afgezien of dubbel ingeschreven).</summary>
    Ingetrokken = 2
}
