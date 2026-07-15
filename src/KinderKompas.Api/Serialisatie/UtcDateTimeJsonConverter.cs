using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KinderKompas.Api.Serialisatie;

/// <summary>
/// Serialiseert elke <see cref="DateTime"/> als UTC-ISO-8601 mét 'Z'-achtervoegsel
/// (bijv. <c>2026-07-15T09:26:00.000Z</c>), zodat de browser hem als UTC leest en
/// naar de lokale tijd van de gebruiker omrekent.
///
/// Achtergrond: al onze tijdstempels worden als UTC opgeslagen
/// (<see cref="DateTime.UtcNow"/>), maar door <c>Npgsql.EnableLegacyTimestampBehavior</c>
/// komen ze uit de database terug met <see cref="DateTimeKind.Unspecified"/>. Zonder
/// deze converter serialiseert System.Text.Json ze dan zónder 'Z', waarna
/// <c>new Date("…T09:26:00")</c> in de browser de UTC-waarde als lokale tijd
/// interpreteert — de in-/uitkloktijd "sprong terug" met het tijdzoneverschil.
///
/// Deze converter geldt automatisch ook voor <see cref="Nullable{DateTime}"/>.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string UtcFormaat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        // Inkomende waarden (bijv. een gecorrigeerde uitkloktijd) normaliseren naar UTC.
        => reader.GetDateTime().ToUniversalTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        DateTime utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            // Unspecified: onze opgeslagen waarden zijn UTC (legacy Npgsql-gedrag),
            // dus we markeren ze als zodanig zonder de klokwaarde te verschuiven.
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
        writer.WriteStringValue(utc.ToString(UtcFormaat, CultureInfo.InvariantCulture));
    }
}
