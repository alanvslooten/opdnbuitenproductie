namespace KinderKompas.Infrastructure.Persistence;

/// <summary>
/// Normaliseert een PostgreSQL-verbindingsstring. Render (en veel PaaS-aanbieders)
/// leveren de verbinding als URL — <c>postgres://gebruiker:wachtwoord@host:5432/db</c> —
/// terwijl Npgsql een key-value-string verwacht. Een verbinding die al in key-value-vorm
/// is (bevat <c>Host=</c>) wordt ongewijzigd teruggegeven.
/// </summary>
public static class PostgresVerbinding
{
    public static string Normaliseer(string ruw)
    {
        if (string.IsNullOrWhiteSpace(ruw))
        {
            return ruw;
        }

        bool isUrl = ruw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || ruw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
        if (!isUrl)
        {
            return ruw;
        }

        var uri = new Uri(ruw);
        string[] inlog = uri.UserInfo.Split(':', 2);
        string gebruiker = Uri.UnescapeDataString(inlog[0]);
        string wachtwoord = inlog.Length > 1 ? Uri.UnescapeDataString(inlog[1]) : string.Empty;
        string database = uri.AbsolutePath.Trim('/');
        int poort = uri.Port > 0 ? uri.Port : 5432;

        // SSL Mode=Require + Trust Server Certificate werkt zowel voor Render's interne
        // verbinding als voor externe verbindingen.
        return $"Host={uri.Host};Port={poort};Database={database};Username={gebruiker};"
            + $"Password={wachtwoord};SSL Mode=Require;Trust Server Certificate=true";
    }
}
