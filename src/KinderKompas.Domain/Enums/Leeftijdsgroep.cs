namespace KinderKompas.Domain.Enums;

/// <summary>
/// De wettelijke leeftijdsgroepen 0-4 jaar zoals gebruikt voor de BKR
/// (beroepskracht-kindratio). Dit is een WETTELIJK begrip en staat los van de
/// fysieke stamgroep waarin een kind geplaatst is.
/// </summary>
public enum Leeftijdsgroep
{
    /// <summary>0 tot 1 jaar.</summary>
    NulTotEen = 0,

    /// <summary>1 tot 2 jaar.</summary>
    EenTotTwee = 1,

    /// <summary>2 tot 3 jaar.</summary>
    TweeTotDrie = 2,

    /// <summary>3 tot 4 jaar.</summary>
    DrieTotVier = 3
}
