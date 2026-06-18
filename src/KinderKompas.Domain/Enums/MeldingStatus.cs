namespace KinderKompas.Domain.Enums;

/// <summary>
/// De levensloop van een melding in het actiecentrum. Een informatieve melding gaat
/// van <see cref="Ongelezen"/> naar <see cref="Gelezen"/>; een to-do (VereistActie)
/// wordt uiteindelijk <see cref="Afgehandeld"/> (afgevinkt).
/// </summary>
public enum MeldingStatus
{
    Ongelezen = 0,
    Gelezen = 1,
    Afgehandeld = 2,
}
