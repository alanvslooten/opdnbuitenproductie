using KinderKompas.Application.Portaal;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;

namespace KinderKompas.Application.Tests.Portaal;

/// <summary>
/// Het eigen-rooster-leesmodel van het thuis-portaal: pas zichtbaar ná versturen,
/// en alleen de meegegeven (eigen) diensten — collega's komen er niet in voor.
/// </summary>
public class ThuisRoosterBouwerTests
{
    private static readonly DateOnly WeekBegin = new(2026, 6, 15); // maandag
    private static readonly Guid Groep = Guid.NewGuid();
    private static readonly Dictionary<Guid, string> GroepNamen = new() { [Groep] = "Bengeltjes" };

    private static Roosterdienst Dienst(DateOnly datum, string? taak = null, int correctie = 0) => new()
    {
        Id = Guid.NewGuid(),
        StamgroepId = Groep,
        Datum = datum,
        Taakomschrijving = taak,
        UrencorrectieKwartieren = correctie,
    };

    [Fact]
    public void Conceptweek_GeeftGeenDiensten()
    {
        var week = new Roosterweek { WeekBegin = WeekBegin, Status = RoosterStatus.Concept };

        ThuisRoosterDto dto = ThuisRoosterBouwer.Bouw(
            WeekBegin, week, new[] { Dienst(WeekBegin) }, GroepNamen);

        Assert.False(dto.Verstuurd);
        Assert.Empty(dto.Dagen);
    }

    [Fact]
    public void GeenWeek_GeeftGeenDiensten()
    {
        ThuisRoosterDto dto = ThuisRoosterBouwer.Bouw(WeekBegin, null, Array.Empty<Roosterdienst>(), GroepNamen);

        Assert.False(dto.Verstuurd);
        Assert.Empty(dto.Dagen);
    }

    [Fact]
    public void VerstuurdeWeek_GeeftEigenDienstenMetGroepnaamEnTaak()
    {
        var week = new Roosterweek
        {
            WeekBegin = WeekBegin,
            Status = RoosterStatus.Verstuurd,
            VerstuurdOp = new DateTime(2026, 6, 14, 10, 0, 0, DateTimeKind.Utc),
        };
        Roosterdienst maandag = Dienst(WeekBegin, "Buitenspelen", correctie: 2);

        ThuisRoosterDto dto = ThuisRoosterBouwer.Bouw(WeekBegin, week, new[] { maandag }, GroepNamen);

        Assert.True(dto.Verstuurd);
        Assert.Equal(week.VerstuurdOp, dto.VerstuurdOp);
        ThuisRoosterDagDto dag = Assert.Single(dto.Dagen);
        Assert.Equal(Weekdag.Maandag, dag.Dag);
        Assert.Equal("Bengeltjes", dag.StamgroepNaam);
        Assert.Equal("Buitenspelen", dag.Taakomschrijving);
        Assert.Equal(2, dag.UrencorrectieKwartieren);
    }

    [Fact]
    public void Diensten_WordenOpDatumGesorteerd()
    {
        var week = new Roosterweek { WeekBegin = WeekBegin, Status = RoosterStatus.Verstuurd };
        Roosterdienst woensdag = Dienst(WeekBegin.AddDays(2));
        Roosterdienst maandag = Dienst(WeekBegin);

        ThuisRoosterDto dto = ThuisRoosterBouwer.Bouw(
            WeekBegin, week, new[] { woensdag, maandag }, GroepNamen);

        Assert.Collection(dto.Dagen,
            d => Assert.Equal(Weekdag.Maandag, d.Dag),
            d => Assert.Equal(Weekdag.Woensdag, d.Dag));
    }
}
