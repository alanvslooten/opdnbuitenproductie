namespace KinderKompas.Domain.Exceptions;

/// <summary>
/// Gegooid wanneer een groepssamenstelling het wettelijk maximum aantal kinderen
/// overschrijdt (bijv. te grote groep of te veel baby's in een gemengde groep).
/// De BKR is dan niet meer geldig te berekenen — dit is een harde validatiefout,
/// geen waarschuwing, want zo'n groep mag wettelijk niet bestaan.
/// </summary>
public sealed class GroepOverschrijdtMaximumException : Exception
{
    public GroepOverschrijdtMaximumException(string message) : base(message)
    {
    }
}
