namespace KinderKompas.Domain.ValueObjects;

/// <summary>
/// De uitkomst van de wachtlijst-prioriteitsberekening voor één inschrijving op
/// één peildatum: de totale score plus een leesbare onderbouwing per onderdeel.
/// Net als <see cref="BkrUitkomst"/> bewust transparant, zodat de planner (en de
/// ouder) kan zien waaróm een kind op een bepaalde plek staat.
///
/// <para><see cref="HandmatigBovenaan"/> staat los van de score: een handmatig
/// bovenaan gezet kind (bijv. een personeelskind) gaat altijd vóór, ongeacht de
/// score. De sortering leeft in de Application-laag.</para>
/// </summary>
public sealed record WachtlijstPrioriteitsuitkomst
{
    /// <summary>De berekende prioriteitsscore (hoger = eerder aan de beurt).</summary>
    public required int Score { get; init; }

    /// <summary>Of het kind handmatig bovenaan is gezet en de score overstijgt.</summary>
    public required bool HandmatigBovenaan { get; init; }

    /// <summary>Leesbare opbouw van de score (welk onderdeel hoeveel punten gaf).</summary>
    public required IReadOnlyList<string> Onderdelen { get; init; }
}
