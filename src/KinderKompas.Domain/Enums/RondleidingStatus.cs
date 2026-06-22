namespace KinderKompas.Domain.Enums;

/// <summary>De status van een rondleiding bij een contact (onderdeel van de Contacten-module).</summary>
public enum RondleidingStatus
{
    /// <summary>Ingepland, nog niet geweest.</summary>
    Gepland = 0,

    /// <summary>De rondleiding heeft plaatsgevonden.</summary>
    Gehad = 1,

    /// <summary>De rondleiding is geannuleerd.</summary>
    Geannuleerd = 2
}
