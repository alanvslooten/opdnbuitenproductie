using KinderKompas.Domain.Enums;
using KinderKompas.Domain.Meldingen;
using KinderKompas.Domain.Services;

namespace KinderKompas.Domain.Tests.Meldingen;

/// <summary>
/// Bewijst de event→melding-vertaling (fase 9): elke gebeurtenis levert de juiste
/// soort, het juiste to-do/informatief-onderscheid, een deep-link naar de bron en
/// een stabiele deduplicatiesleutel zodat herhaalde triggers niet gaan spammen.
/// </summary>
public class MeldingFabriekTests
{
    [Fact]
    public void Nieuwe_wachtlijstaanmelding_is_informatief_met_bronlink()
    {
        Guid id = Guid.NewGuid();

        var melding = MeldingFabriek.Maak(new NieuweWachtlijstaanmelding(id, "Tijn Bakker"));

        Assert.Equal(MeldingSoort.NieuweWachtlijstaanmelding, melding.Soort);
        Assert.False(melding.VereistActie);
        Assert.False(melding.IsOpenToDo);
        Assert.Equal(MeldingStatus.Ongelezen, melding.Status);
        Assert.Equal("WachtlijstInschrijving", melding.BronType);
        Assert.Equal(id, melding.BronId);
        Assert.Contains("Tijn Bakker", melding.Tekst);
        Assert.Equal($"wachtlijst-nieuw:{id}", melding.DeduplicatieSleutel);
    }

    [Fact]
    public void Verlofaanvraag_is_een_open_todo()
    {
        Guid id = Guid.NewGuid();

        var melding = MeldingFabriek.Maak(new VerlofAangevraagd(
            id, "Sanne de Vries", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5), 24m));

        Assert.Equal(MeldingSoort.Verlofaanvraag, melding.Soort);
        Assert.True(melding.VereistActie);
        Assert.True(melding.IsOpenToDo);
        Assert.Equal(id, melding.BronId);
        Assert.Equal($"verlof:{id}", melding.DeduplicatieSleutel);
    }

    [Fact]
    public void Ziekmelding_vraagt_om_invaller_check()
    {
        Guid id = Guid.NewGuid();

        var melding = MeldingFabriek.Maak(new Ziekgemeld(id, "Noor Jansen", new DateOnly(2026, 7, 2)));

        Assert.Equal(MeldingSoort.Ziekmelding, melding.Soort);
        Assert.True(melding.VereistActie);
        Assert.Contains("invaller", melding.Tekst, StringComparison.OrdinalIgnoreCase);
        Assert.Equal($"ziek:{id}", melding.DeduplicatieSleutel);
    }

    [Fact]
    public void Volledige_plaatsing_triggert_contract_todo()
    {
        Guid id = Guid.NewGuid();
        DateOnly start = new(2026, 9, 1);

        var melding = MeldingFabriek.Maak(new PlaatsingGeaccepteerd(id, "Liv Smit", start, VolledigGeplaatst: true));

        Assert.Equal(MeldingSoort.VoorstelGeaccepteerd, melding.Soort);
        Assert.True(melding.VereistActie);
        Assert.Contains("Portabase", melding.Tekst);
        Assert.Contains("volledig", melding.Tekst, StringComparison.OrdinalIgnoreCase);
        // De sleutel bevat de startdatum, zodat een tweede deelplaatsing een eigen to-do krijgt.
        Assert.Equal($"plaatsing:{id}:2026-09-01", melding.DeduplicatieSleutel);
    }

    [Fact]
    public void Deelplaatsing_meldt_deels_geplaatst()
    {
        var melding = MeldingFabriek.Maak(
            new PlaatsingGeaccepteerd(Guid.NewGuid(), "Liv Smit", new DateOnly(2026, 9, 1), VolledigGeplaatst: false));

        Assert.Contains("deels", melding.Tekst, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bkr_overschrijding_is_een_waarschuwing_todo()
    {
        Guid groep = Guid.NewGuid();

        var melding = MeldingFabriek.Maak(
            new BkrOverschrijdingGesignaleerd(groep, "Boefjes", new DateOnly(2026, 7, 1), 12, 3));

        Assert.Equal(MeldingSoort.BkrWaarschuwing, melding.Soort);
        Assert.True(melding.VereistActie);
        Assert.Equal(groep, melding.BronId);
        Assert.Equal($"bkr:{groep}:2026-07-01", melding.DeduplicatieSleutel);
    }

    [Fact]
    public void Observatieherinnering_verwijst_naar_kind_en_mijlpaal()
    {
        Guid kind = Guid.NewGuid();

        var melding = MeldingFabriek.Maak(new Observatieherinnering(kind, "Mees Visser", 24));

        Assert.Equal(MeldingSoort.Observatieherinnering, melding.Soort);
        Assert.Equal("Kind", melding.BronType);
        Assert.Equal(kind, melding.BronId);
        Assert.Equal($"observatie:{kind}:24", melding.DeduplicatieSleutel);
    }

    [Fact]
    public void MarkeerGelezen_en_HandelAf_volgen_de_levensloop()
    {
        var melding = MeldingFabriek.Maak(new VerlofAangevraagd(
            Guid.NewGuid(), "Sanne", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2), 8m));

        melding.MarkeerGelezen();
        Assert.Equal(MeldingStatus.Gelezen, melding.Status);

        DateTime nu = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        melding.HandelAf(nu);
        Assert.Equal(MeldingStatus.Afgehandeld, melding.Status);
        Assert.Equal(nu, melding.AfgehandeldOp);
        Assert.False(melding.IsOpenToDo);
    }
}
