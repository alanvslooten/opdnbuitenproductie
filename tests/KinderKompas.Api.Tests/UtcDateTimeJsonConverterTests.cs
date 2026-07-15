using System.Text.Json;
using KinderKompas.Api.Serialisatie;

namespace KinderKompas.Api.Tests;

public class UtcDateTimeJsonConverterTests
{
    private static readonly JsonSerializerOptions Opties = MaakOpties();

    private static JsonSerializerOptions MaakOpties()
    {
        var o = new JsonSerializerOptions();
        o.Converters.Add(new UtcDateTimeJsonConverter());
        return o;
    }

    [Fact]
    public void Unspecified_datetime_wordt_als_utc_met_Z_geserialiseerd()
    {
        // Zo komt een tijdstempel uit Postgres terug bij legacy-timestampgedrag:
        // de waarde is UTC, maar de Kind is Unspecified.
        var uitDb = new DateTime(2026, 7, 15, 9, 26, 0, DateTimeKind.Unspecified);

        string json = JsonSerializer.Serialize(uitDb, Opties);

        // De klokwaarde blijft 09:26 (niet verschoven) en krijgt het 'Z'-achtervoegsel,
        // zodat de browser hem als UTC leest en naar lokale tijd omrekent.
        Assert.Equal("\"2026-07-15T09:26:00.000Z\"", json);
    }

    [Fact]
    public void Utc_datetime_behoudt_klokwaarde_en_krijgt_Z()
    {
        var utc = new DateTime(2026, 7, 15, 9, 26, 0, DateTimeKind.Utc);

        string json = JsonSerializer.Serialize(utc, Opties);

        Assert.Equal("\"2026-07-15T09:26:00.000Z\"", json);
    }

    [Fact]
    public void Local_datetime_wordt_naar_utc_omgerekend()
    {
        var lokaal = new DateTime(2026, 7, 15, 9, 26, 0, DateTimeKind.Local);
        DateTime verwachtUtc = lokaal.ToUniversalTime();

        string json = JsonSerializer.Serialize(lokaal, Opties);

        string verwacht = $"\"{verwachtUtc:yyyy-MM-dd'T'HH:mm:ss.fff}Z\"";
        Assert.Equal(verwacht, json);
    }

    [Fact]
    public void Nullable_datetime_gebruikt_dezelfde_converter()
    {
        DateTime? uitDb = new DateTime(2026, 7, 15, 9, 26, 0, DateTimeKind.Unspecified);

        string json = JsonSerializer.Serialize(uitDb, Opties);

        Assert.Equal("\"2026-07-15T09:26:00.000Z\"", json);
    }

    [Fact]
    public void Null_nullable_datetime_blijft_null()
    {
        DateTime? leeg = null;

        string json = JsonSerializer.Serialize(leeg, Opties);

        Assert.Equal("null", json);
    }

    [Fact]
    public void Ingelezen_waarde_wordt_naar_utc_genormaliseerd()
    {
        // Een client stuurt een gecorrigeerde tijd met offset; die moet UTC worden.
        DateTime waarde = JsonSerializer.Deserialize<DateTime>("\"2026-07-15T11:26:00+02:00\"", Opties);

        Assert.Equal(DateTimeKind.Utc, waarde.Kind);
        Assert.Equal(new DateTime(2026, 7, 15, 9, 26, 0, DateTimeKind.Utc), waarde);
    }
}
