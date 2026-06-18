using System.Security.Cryptography;
using System.Text;

namespace KinderKompas.Infrastructure.Persistence;

/// <summary>
/// Maakt een stabiele Guid uit een tekstsleutel. Gebruikt voor seed-data
/// (capabilities en de rol-capability-mapping): EF Core HasData eist constante,
/// deterministische waarden, anders verschilt elke migratie. Dezelfde sleutel
/// levert altijd dezelfde Guid, dus we hoeven niet tientallen GUID's met de hand
/// te beheren en kunnen capabilities toevoegen zonder bestaande te verschuiven.
/// </summary>
public static class DeterministischeGuid
{
    public static Guid Maak(string sleutel)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(sleutel));
        return new Guid(hash);
    }
}
