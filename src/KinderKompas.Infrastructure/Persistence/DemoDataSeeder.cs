using System.Text;
using KinderKompas.Application.Abstractions;
using KinderKompas.Domain.Entiteiten;
using KinderKompas.Domain.Enums;
using KinderKompas.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KinderKompas.Infrastructure.Persistence;

/// <summary>
/// Vult de seed-organisatie met een rijke demo-dataset over álle modules heen:
/// medewerkers, kinderen, werkrooster, verlof/ziekte/saldo, wachtlijst met
/// voorstellen, observaties (incl. echte mini-PDF's), urenregistratie en meldingen.
/// Bedoeld om de applicatie overtuigend te kunnen demonstreren.
///
/// Volledig idempotent: elke sectie controleert eerst of de data er al is, zodat
/// herhaald opstarten geen dubbele rijen oplevert. Draait ná de
/// <see cref="IdentityDataSeeder"/>, zodat de basisaccounts en -groepen er al zijn.
///
/// Alle data hangt expliciet aan <see cref="SeedConstanten.OrganisatieId"/>: bij het
/// seeden is er geen ingelogde gebruiker, dus de tenant-context kan niet uit claims
/// komen. Reads gebruiken daarom <c>IgnoreQueryFilters()</c>.
/// </summary>
public sealed class DemoDataSeeder
{
    private static readonly Guid OrgId = SeedConstanten.OrganisatieId;
    private static readonly Guid Bengeltjes = SeedConstanten.StamgroepBengeltjesId;
    private static readonly Guid Boefjes = SeedConstanten.StamgroepBoefjesId;

    private const Weekdag Ma = Weekdag.Maandag;
    private const Weekdag Di = Weekdag.Dinsdag;
    private const Weekdag Wo = Weekdag.Woensdag;
    private const Weekdag Do = Weekdag.Donderdag;
    private const Weekdag Vr = Weekdag.Vrijdag;

    private static readonly (Weekdag Vlag, int Offset)[] Werkweek =
    {
        (Ma, 0), (Di, 1), (Wo, 2), (Do, 3), (Vr, 4),
    };

    private readonly KinderKompasDbContext _db;
    private readonly IBestandsopslag _opslag;
    private readonly ILogger<DemoDataSeeder> _log;

    public DemoDataSeeder(KinderKompasDbContext db, IBestandsopslag opslag, ILogger<DemoDataSeeder> log)
    {
        _db = db;
        _opslag = opslag;
        _log = log;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        DateOnly vandaag = DateOnly.FromDateTime(DateTime.UtcNow);

        Dictionary<string, Medewerker> mw = await SeedMedewerkersAsync(ct);
        await SeedKinderenAsync(mw, vandaag, ct);
        await SeedVerlofEnZiekteAsync(mw, vandaag, ct);
        await SeedRoosterAsync(mw, vandaag, ct);
        await SeedWachtlijstAsync(vandaag, ct);
        await SeedObservatiesAsync(vandaag, ct);
        await SeedUrenAsync(mw, vandaag, ct);
        await SeedMeldingenAsync(ct);
        await SeedContactenAsync(vandaag, ct);

        _log.LogInformation("Demo-dataset geseed (idempotent).");
    }

    // ── Medewerkers ──────────────────────────────────────────────────────────
    private async Task<Dictionary<string, Medewerker>> SeedMedewerkersAsync(CancellationToken ct)
    {
        // (sleutel, voornaam, achternaam, rol, werkdagen, beschikbaarheid, uren, groep)
        var definities = new (string Sleutel, string Vn, string An, Rol Rol, Weekdag Werk, Weekdag Besch, decimal Uren, Guid Groep)[]
        {
            // Bestaande accounts: worden gevonden, niet overschreven.
            ("gail", "Gail", "Beheerder", Rol.Beheerder, Ma | Di | Wo, Do, 24m, Bengeltjes),
            ("sanne", "Sanne", "Senior", Rol.Senior, Ma | Di | Wo, Do | Vr, 24m, Bengeltjes),
            ("jasper", "Jasper", "Junior", Rol.Junior, Ma | Di | Wo, Do, 24m, Boefjes),
            // Nieuwe demo-medewerkers (zonder login).
            ("linda", "Linda", "Bos", Rol.Senior, Ma | Di | Do | Vr, Wo, 32m, Boefjes),
            ("mo", "Mo", "Haddad", Rol.Junior, Wo | Do | Vr, Ma, 24m, Bengeltjes),
            ("priya", "Priya", "Singh", Rol.Hulpbeheerder, Ma | Di | Wo | Do | Vr, Weekdag.Geen, 36m, Boefjes),
            ("tom", "Tom", "Vermeer", Rol.Junior, Ma | Di | Vr, Wo | Do, 24m, Bengeltjes),
            ("esra", "Esra", "Yilmaz", Rol.Senior, Di | Wo | Do, Ma | Vr, 28m, Boefjes),
        };

        DateOnly vandaag = DateOnly.FromDateTime(DateTime.UtcNow);
        var resultaat = new Dictionary<string, Medewerker>();
        int i = 0;
        foreach (var d in definities)
        {
            i++;
            Medewerker? m = await _db.Medewerkers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Voornaam == d.Vn && x.Achternaam == d.An, ct);
            if (m is null)
            {
                m = new Medewerker
                {
                    OrganisatieId = OrgId,
                    Voornaam = d.Vn,
                    Achternaam = d.An,
                    Rol = d.Rol,
                    VasteWerkdagen = d.Werk,
                    Beschikbaarheidsdagen = d.Besch,
                    Contracturen = d.Uren,
                    VasteStamgroepId = d.Groep,
                };
                _db.Medewerkers.Add(m);
                await _db.SaveChangesAsync(ct);
            }

            // Backfill van de F-22-velden (contact, contract, pincode) — idempotent:
            // alleen wanneer er nog geen pincode staat.
            if (string.IsNullOrEmpty(m.Pincode))
            {
                bool vast = d.Rol is Rol.Beheerder or Rol.Hulpbeheerder or Rol.Senior;
                m.Telefoon = $"06{10_000_000 + i:00000000}";
                m.Email = $"{d.Vn.ToLowerInvariant()}@opdnbuiten.nl";
                m.NoodcontactNaam = $"Noodcontact {d.Vn}";
                m.NoodcontactTelefoon = $"06{90_000_000 + i:00000000}";
                m.ContractVast = vast;
                m.Contractbegindatum = vandaag.AddMonths(-12 - i);
                m.Contracteinddatum = vast ? null : vandaag.AddMonths(6 + i);
                m.Pincode = (1000 + i * 111).ToString();
                await _db.SaveChangesAsync(ct);
            }
            resultaat[d.Sleutel] = m;
        }
        return resultaat;
    }

    // ── Kinderen ─────────────────────────────────────────────────────────────
    private async Task SeedKinderenAsync(Dictionary<string, Medewerker> mw, DateOnly vandaag, CancellationToken ct)
    {
        Guid sanne = mw["sanne"].Id, linda = mw["linda"].Id, esra = mw["esra"].Id, tom = mw["tom"].Id;

        // Geboortedata relatief aan vandaag, zodat leeftijden 0-4 mooi spreiden en
        // sommige kinderen "bijna vier" zijn (dashboard-signaal) of observatiemomenten hebben.
        var kinderen = new (string Vn, string An, DateOnly Gb, DateOnly Start, Contracttype Ct, Weekdag Dagen, Guid Groep, Guid? Mentor, string Ouder, string Tel, string Mail)[]
        {
            ("Sven", "Bakker", vandaag.AddMonths(-45), vandaag.AddMonths(-40), Contracttype.Weken49, Ma | Di | Do, Bengeltjes, sanne, "Karin Bakker", "0612000001", "karin.bakker@example.nl"),
            ("Noor", "de Wit", vandaag.AddMonths(-30), vandaag.AddMonths(-24), Contracttype.Weken40, Ma | Di | Wo, Bengeltjes, sanne, "Joost de Wit", "0612000002", "joost@example.nl"),
            ("Liam", "Jansen", vandaag.AddMonths(-20), vandaag.AddMonths(-16), Contracttype.Weken49, Do | Vr, Boefjes, linda, "Eva Jansen", "0612000003", "eva.jansen@example.nl"),
            ("Mila", "Visser", vandaag.AddMonths(-14), vandaag.AddMonths(-10), Contracttype.Weken49, Ma | Wo | Vr, Boefjes, linda, "Sam Visser", "0612000004", "sam@example.nl"),
            ("Daan", "Smit", vandaag.AddMonths(-8), vandaag.AddMonths(-5), Contracttype.Weken40, Di | Do, Bengeltjes, esra, "Iris Smit", "0612000005", "iris.smit@example.nl"),
            ("Saar", "Mulder", vandaag.AddMonths(-46), vandaag.AddMonths(-42), Contracttype.Weken49, Ma | Di | Wo | Do | Vr, Boefjes, tom, "Bram Mulder", "0612000006", "bram@example.nl"),
            ("Finn", "Meijer", vandaag.AddMonths(-3), vandaag.AddMonths(-1), Contracttype.Weken49, Wo | Do, Bengeltjes, sanne, "Lotte Meijer", "0612000007", "lotte@example.nl"),
            ("Tess", "Bos", vandaag.AddMonths(-36), vandaag.AddMonths(-30), Contracttype.Weken40, Ma | Vr, Boefjes, esra, "Niels Bos", "0612000008", "niels.bos@example.nl"),
            ("Luuk", "Kuijpers", vandaag.AddMonths(-26), vandaag.AddMonths(-20), Contracttype.Weken49, Di | Wo | Do, Bengeltjes, tom, "Anne Kuijpers", "0612000009", "anne@example.nl"),
            ("Roos", "Hendriks", vandaag.AddMonths(-18), vandaag.AddMonths(-14), Contracttype.Weken49, Ma | Di, Boefjes, linda, "Tim Hendriks", "0612000010", "tim.hendriks@example.nl"),
            ("Bram", "Willems", vandaag.AddMonths(-11), vandaag.AddMonths(-8), Contracttype.Weken40, Do | Vr, Bengeltjes, esra, "Maud Willems", "0612000011", "maud@example.nl"),
            ("Evi", "Peters", vandaag.AddMonths(-43), vandaag.AddMonths(-38), Contracttype.Weken49, Ma | Wo, Boefjes, tom, "Ruben Peters", "0612000012", "ruben.peters@example.nl"),
        };

        bool ietsToegevoegd = false;
        foreach (var k in kinderen)
        {
            if (await _db.Kinderen.IgnoreQueryFilters().AnyAsync(x => x.Voornaam == k.Vn && x.Achternaam == k.An, ct))
            {
                continue;
            }
            _db.Kinderen.Add(new Kind
            {
                OrganisatieId = OrgId,
                Voornaam = k.Vn,
                Achternaam = k.An,
                Geboortedatum = k.Gb,
                Startdatum = k.Start,
                Contracttype = k.Ct,
                GewensteOpvangdagen = k.Dagen,
                StamgroepId = k.Groep,
                MentorId = k.Mentor,
                Oudercontacten = { new Oudercontact(k.Ouder, k.Tel, k.Mail) },
            });
            ietsToegevoegd = true;
        }
        if (ietsToegevoegd)
        {
            await _db.SaveChangesAsync(ct);
        }
    }

    // ── Verlof, ziekte en saldo ──────────────────────────────────────────────
    private async Task SeedVerlofEnZiekteAsync(Dictionary<string, Medewerker> mw, DateOnly vandaag, CancellationToken ct)
    {
        // Saldo: per medewerker per categorie (uniek). Idempotent per (medewerker, categorie).
        foreach (Medewerker m in mw.Values)
        {
            await ZorgVoorSaldoAsync(m.Id, VerlofCategorie.Vakantieuren, Math.Round(m.Contracturen * 5m, 2),
                new DateOnly(vandaag.Year, 12, 31), ct);
            await ZorgVoorSaldoAsync(m.Id, VerlofCategorie.Verlofbudget, 40m, null, ct);
        }

        if (!await _db.Verlofaanvragen.IgnoreQueryFilters().AnyAsync(ct))
        {
            _db.Verlofaanvragen.Add(new Verlofaanvraag
            {
                OrganisatieId = OrgId,
                MedewerkerId = mw["sanne"].Id,
                Begindatum = vandaag.AddDays(-20),
                Einddatum = vandaag.AddDays(-16),
                AantalUren = 24m,
                Categorie = VerlofCategorie.Vakantieuren,
                Status = VerlofStatus.Goedgekeurd,
                Reden = "Meivakantie met het gezin.",
                BeoordelingsNotitie = "Akkoord, prettige vakantie!",
                BeoordeeldOp = DateTime.UtcNow.AddDays(-25),
            });
            _db.Verlofaanvragen.Add(new Verlofaanvraag
            {
                OrganisatieId = OrgId,
                MedewerkerId = mw["linda"].Id,
                Begindatum = vandaag.AddDays(14),
                Einddatum = vandaag.AddDays(18),
                AantalUren = 25.6m,
                Categorie = VerlofCategorie.Vakantieuren,
                Status = VerlofStatus.Openstaand,
                Reden = "Lang weekend weg.",
            });
            _db.Verlofaanvragen.Add(new Verlofaanvraag
            {
                OrganisatieId = OrgId,
                MedewerkerId = mw["mo"].Id,
                Begindatum = vandaag.AddDays(30),
                Einddatum = vandaag.AddDays(30),
                AantalUren = 8m,
                Categorie = VerlofCategorie.Verlofbudget,
                Status = VerlofStatus.Openstaand,
                Reden = "Tandartsbezoek.",
            });
            await _db.SaveChangesAsync(ct);
        }

        if (!await _db.Ziekmeldingen.IgnoreQueryFilters().AnyAsync(ct))
        {
            _db.Ziekmeldingen.Add(new Ziekmelding
            {
                OrganisatieId = OrgId,
                MedewerkerId = mw["jasper"].Id,
                Begindatum = vandaag.AddDays(-2),
                Einddatum = null, // nog ziek (open einde)
            });
            _db.Ziekmeldingen.Add(new Ziekmelding
            {
                OrganisatieId = OrgId,
                MedewerkerId = mw["tom"].Id,
                Begindatum = vandaag.AddDays(-40),
                Einddatum = vandaag.AddDays(-38),
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task ZorgVoorSaldoAsync(Guid medewerkerId, VerlofCategorie categorie, decimal uren, DateOnly? vervalt, CancellationToken ct)
    {
        if (await _db.Verlofsaldi.IgnoreQueryFilters()
                .AnyAsync(s => s.MedewerkerId == medewerkerId && s.Categorie == categorie, ct))
        {
            return;
        }
        _db.Verlofsaldi.Add(new Verlofsaldo
        {
            OrganisatieId = OrgId,
            MedewerkerId = medewerkerId,
            Categorie = categorie,
            ToegekendeUren = uren,
            Vervaldatum = vervalt,
        });
        await _db.SaveChangesAsync(ct);
    }

    // ── Werkrooster ──────────────────────────────────────────────────────────
    private async Task SeedRoosterAsync(Dictionary<string, Medewerker> mw, DateOnly vandaag, CancellationToken ct)
    {
        DateOnly maandag = vandaag.AddDays(-(((int)vandaag.DayOfWeek + 6) % 7));

        // Deze week: verstuurd. Volgende week: concept.
        await ZorgVoorRoosterweekAsync(maandag, RoosterStatus.Verstuurd, DateTime.UtcNow.AddDays(-3), mw, ct);
        await ZorgVoorRoosterweekAsync(maandag.AddDays(7), RoosterStatus.Concept, null, mw, ct);
    }

    private async Task ZorgVoorRoosterweekAsync(
        DateOnly weekBegin, RoosterStatus status, DateTime? verstuurdOp,
        Dictionary<string, Medewerker> mw, CancellationToken ct)
    {
        if (await _db.Roosterweken.IgnoreQueryFilters().AnyAsync(r => r.WeekBegin == weekBegin, ct))
        {
            return;
        }

        var week = new Roosterweek
        {
            OrganisatieId = OrgId,
            WeekBegin = weekBegin,
            Status = status,
            VerstuurdOp = verstuurdOp,
        };
        _db.Roosterweken.Add(week);
        await _db.SaveChangesAsync(ct);

        foreach (Medewerker m in mw.Values)
        {
            if (m.VasteStamgroepId is not { } groep)
            {
                continue;
            }
            foreach (var (vlag, offset) in Werkweek)
            {
                if ((m.VasteWerkdagen & vlag) == 0)
                {
                    continue;
                }
                _db.Roosterdiensten.Add(new Roosterdienst
                {
                    OrganisatieId = OrgId,
                    RoosterweekId = week.Id,
                    MedewerkerId = m.Id,
                    StamgroepId = groep,
                    Datum = weekBegin.AddDays(offset),
                    UrencorrectieKwartieren = 0,
                });
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    // ── Wachtlijst + voorstellen ─────────────────────────────────────────────
    private async Task SeedWachtlijstAsync(DateOnly vandaag, CancellationToken ct)
    {
        if (await _db.Wachtlijstinschrijvingen.IgnoreQueryFilters().AnyAsync(ct))
        {
            return;
        }

        // 1) Intern broertje, hoge prioriteit, nog wachtend.
        _db.Wachtlijstinschrijvingen.Add(new WachtlijstInschrijving
        {
            OrganisatieId = OrgId,
            Voornaam = "Lars",
            Achternaam = "Bakker",
            Geboortedatum = vandaag.AddMonths(-6),
            Oudercontact = new Oudercontact("Karin Bakker", "0612000001", "karin.bakker@example.nl"),
            InschrijfdatumWachtlijst = vandaag.AddDays(-120),
            GewensteStartdatum = vandaag.AddDays(20),
            GewensteOpvangdagen = Ma | Di | Do,
            Contracttype = Contracttype.Weken49,
            GewensteStamgroepId = Bengeltjes,
            IsIntern = true,
            Status = WachtlijstStatus.Wachtend,
            Notitie = "Broertje van Sven; graag dezelfde dagen.",
        });

        // 2) Externe aanmelding, langer wachtend.
        _db.Wachtlijstinschrijvingen.Add(new WachtlijstInschrijving
        {
            OrganisatieId = OrgId,
            Voornaam = "Julia",
            Achternaam = "Vos",
            Geboortedatum = vandaag.AddMonths(-3),
            Oudercontact = new Oudercontact("Sanne Vos", "0612000020", "sanne.vos@example.nl"),
            InschrijfdatumWachtlijst = vandaag.AddDays(-200),
            GewensteStartdatum = vandaag.AddDays(45),
            GewensteOpvangdagen = Wo | Vr,
            Contracttype = Contracttype.Weken40,
            GewensteStamgroepId = Boefjes,
            IsIntern = false,
            Status = WachtlijstStatus.Wachtend,
        });

        // 3) Recente aanmelding, geen voorkeursgroep.
        _db.Wachtlijstinschrijvingen.Add(new WachtlijstInschrijving
        {
            OrganisatieId = OrgId,
            Voornaam = "Gijs",
            Achternaam = "Dekker",
            Geboortedatum = vandaag.AddMonths(-1),
            Oudercontact = new Oudercontact("Marit Dekker", "0612000021", "marit.dekker@example.nl"),
            InschrijfdatumWachtlijst = vandaag.AddDays(-15),
            GewensteStartdatum = vandaag.AddDays(90),
            GewensteOpvangdagen = Ma | Di | Wo | Do | Vr,
            Contracttype = Contracttype.Weken49,
            IsIntern = false,
            Status = WachtlijstStatus.Wachtend,
        });

        // 4) Aanmelding met een openstaand voorstel (deelvoorstel: 2 van de 3 dagen).
        var metVoorstel = new WachtlijstInschrijving
        {
            OrganisatieId = OrgId,
            Voornaam = "Olivia",
            Achternaam = "Brouwer",
            Geboortedatum = vandaag.AddMonths(-9),
            Oudercontact = new Oudercontact("Daan Brouwer", "0612000022", "daan.brouwer@example.nl"),
            InschrijfdatumWachtlijst = vandaag.AddDays(-80),
            GewensteStartdatum = vandaag.AddDays(10),
            GewensteOpvangdagen = Ma | Wo | Vr,
            Contracttype = Contracttype.Weken49,
            GewensteStamgroepId = Bengeltjes,
            IsIntern = false,
            Status = WachtlijstStatus.Wachtend,
            Notitie = "Voorstel verstuurd voor maandag en woensdag.",
        };
        _db.Wachtlijstinschrijvingen.Add(metVoorstel);
        await _db.SaveChangesAsync(ct);

        DateOnly maandag = vandaag.AddDays(-(((int)vandaag.DayOfWeek + 6) % 7)).AddDays(7);
        var voorstel = new Voorstel
        {
            OrganisatieId = OrgId,
            WachtlijstInschrijvingId = metVoorstel.Id,
            VerstuurdOp = DateTime.UtcNow.AddDays(-2),
            VoorgesteldeStamgroepId = Bengeltjes,
            VoorgesteldeDagen = Ma | Wo,
            Status = VoorstelStatus.Verstuurd,
            Notitie = "Vrijdag is helaas nog vol; maandag en woensdag kunnen wel.",
        };
        _db.Voorstellen.Add(voorstel);
        await _db.SaveChangesAsync(ct);

        _db.VoorstelDagen.Add(new VoorstelDag
        {
            OrganisatieId = OrgId,
            VoorstelId = voorstel.Id,
            Weekdag = Ma,
            VoorgesteldeDatum = maandag,
        });
        _db.VoorstelDagen.Add(new VoorstelDag
        {
            OrganisatieId = OrgId,
            VoorstelId = voorstel.Id,
            Weekdag = Wo,
            VoorgesteldeDatum = maandag.AddDays(2),
        });
        await _db.SaveChangesAsync(ct);
    }

    // ── Observaties (met echte mini-PDF's) ───────────────────────────────────
    private async Task SeedObservatiesAsync(DateOnly vandaag, CancellationToken ct)
    {
        if (await _db.Observaties.IgnoreQueryFilters().AnyAsync(ct))
        {
            return;
        }

        // Twee kinderen die oud genoeg zijn voor meerdere mijlpalen.
        await ObservatiesVoorKindAsync("Sven", "Bakker",
            verzonden: new[] { 6, 12, 18, 24, 30, 36 }, concept: new[] { 42 }, ct);
        await ObservatiesVoorKindAsync("Fenna", "de Vries",
            verzonden: new[] { 6, 12 }, concept: new[] { 18 }, ct);
    }

    private async Task ObservatiesVoorKindAsync(string vn, string an, int[] verzonden, int[] concept, CancellationToken ct)
    {
        Kind? kind = await _db.Kinderen.IgnoreQueryFilters()
            .FirstOrDefaultAsync(k => k.Voornaam == vn && k.Achternaam == an, ct);
        if (kind is null)
        {
            return;
        }

        foreach (int maanden in verzonden)
        {
            await VoegObservatieToeAsync(kind, maanden, isVerzonden: true, ct);
        }
        foreach (int maanden in concept)
        {
            await VoegObservatieToeAsync(kind, maanden, isVerzonden: false, ct);
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task VoegObservatieToeAsync(Kind kind, int maanden, bool isVerzonden, CancellationToken ct)
    {
        string bestandsnaam = $"observatie-{kind.Voornaam}-{maanden}mnd.pdf";
        byte[] pdf = MaakMiniPdf($"Observatie {kind.Voornaam} {kind.Achternaam} - {maanden} maanden");
        string sleutel;
        await using (var stroom = new MemoryStream(pdf))
        {
            sleutel = await _opslag.OpslaanAsync("observaties", bestandsnaam, stroom, ct);
        }

        _db.Observaties.Add(new Observatie
        {
            OrganisatieId = OrgId,
            KindId = kind.Id,
            MijlpaalMaanden = maanden,
            BestandsNaam = bestandsnaam,
            BestandsSleutel = sleutel,
            ContentType = "application/pdf",
            BestandsGrootte = pdf.Length,
            VerzondenOp = isVerzonden ? DateTime.UtcNow.AddDays(-maanden) : null,
            VerzondenNaarEmail = isVerzonden ? kind.Oudercontacten.FirstOrDefault()?.Email : null,
        });
    }

    // ── Urenregistratie ──────────────────────────────────────────────────────
    private async Task SeedUrenAsync(Dictionary<string, Medewerker> mw, DateOnly vandaag, CancellationToken ct)
    {
        if (await _db.Urenregistraties.IgnoreQueryFilters().AnyAsync(ct))
        {
            return;
        }

        DateTime vandaagUtc = DateTime.UtcNow.Date;

        // Sanne: gisteren netjes in- en uitgeklokt.
        _db.Urenregistraties.Add(new Urenregistratie
        {
            OrganisatieId = OrgId,
            MedewerkerId = mw["sanne"].Id,
            StamgroepId = Bengeltjes,
            Datum = vandaag.AddDays(-1),
            Ingeklokt = vandaagUtc.AddDays(-1).AddHours(7).AddMinutes(28),
            Uitgeklokt = vandaagUtc.AddDays(-1).AddHours(15).AddMinutes(2),
        });

        // Sanne: vandaag ingeklokt, nog niet uit (open dienst).
        _db.Urenregistraties.Add(new Urenregistratie
        {
            OrganisatieId = OrgId,
            MedewerkerId = mw["sanne"].Id,
            StamgroepId = Bengeltjes,
            Datum = vandaag,
            Ingeklokt = vandaagUtc.AddHours(7).AddMinutes(31),
            Uitgeklokt = null,
        });

        // Linda: gisteren gewerkt op de Boefjes.
        _db.Urenregistraties.Add(new Urenregistratie
        {
            OrganisatieId = OrgId,
            MedewerkerId = mw["linda"].Id,
            StamgroepId = Boefjes,
            Datum = vandaag.AddDays(-1),
            Ingeklokt = vandaagUtc.AddDays(-1).AddHours(8).AddMinutes(2),
            Uitgeklokt = vandaagUtc.AddDays(-1).AddHours(16).AddMinutes(11),
        });

        await _db.SaveChangesAsync(ct);
    }

    // ── Meldingen / actiecentrum ─────────────────────────────────────────────
    private async Task SeedMeldingenAsync(CancellationToken ct)
    {
        if (await _db.Meldingen.IgnoreQueryFilters().AnyAsync(ct))
        {
            return;
        }

        _db.Meldingen.Add(Melding("bkr-bengeltjes", MeldingSoort.BkrWaarschuwing, true,
            "BKR-aandacht op de Bengeltjes",
            "Donderdag staan er volgens de planning te weinig beroepskrachten op de Bengeltjes. Controleer de bezetting."));

        _db.Meldingen.Add(Melding("obs-sven-42", MeldingSoort.Observatieherinnering, true,
            "Observatie bijna verlopen",
            "De observatie van Sven Bakker (42 maanden) staat klaar als concept en moet binnenkort verstuurd worden."));

        _db.Meldingen.Add(Melding("verlof-linda", MeldingSoort.Verlofaanvraag, true,
            "Nieuwe verlofaanvraag",
            "Linda Bos heeft verlof aangevraagd. Beoordeel de aanvraag in het verlofoverzicht."));

        _db.Meldingen.Add(Melding("ziek-jasper", MeldingSoort.Ziekmelding, false,
            "Ziekmelding ontvangen",
            "Jasper Junior heeft zich ziek gemeld. De planning is mogelijk geraakt."));

        _db.Meldingen.Add(Melding("wachtlijst-gijs", MeldingSoort.NieuweWachtlijstaanmelding, false,
            "Nieuwe wachtlijstaanmelding",
            "Gijs Dekker is aangemeld voor de wachtlijst."));

        await _db.SaveChangesAsync(ct);
    }

    private static Melding Melding(string dedup, MeldingSoort soort, bool vereistActie, string titel, string tekst) => new()
    {
        OrganisatieId = OrgId,
        Soort = soort,
        Status = MeldingStatus.Ongelezen,
        VereistActie = vereistActie,
        Titel = titel,
        Tekst = tekst,
        DeduplicatieSleutel = dedup,
    };

    // ── Mini-PDF-generator (geldige, kleine PDF met één tekstregel) ───────────
    private static byte[] MaakMiniPdf(string tekst)
    {
        string veilig = tekst.Replace("\\", @"\\").Replace("(", @"\(").Replace(")", @"\)");
        string stroom = $"BT /F1 14 Tf 36 150 Td ({veilig}) Tj ET";

        var objecten = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {stroom.Length} >>\nstream\n{stroom}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        var offsets = new int[objecten.Length];
        for (int i = 0; i < objecten.Length; i++)
        {
            offsets[i] = sb.Length;
            sb.Append(i + 1).Append(" 0 obj\n").Append(objecten[i]).Append("\nendobj\n");
        }

        int xref = sb.Length;
        sb.Append("xref\n0 ").Append(objecten.Length + 1).Append('\n');
        sb.Append("0000000000 65535 f \n");
        foreach (int off in offsets)
        {
            sb.Append(off.ToString("D10")).Append(" 00000 n \n");
        }
        sb.Append("trailer\n<< /Size ").Append(objecten.Length + 1).Append(" /Root 1 0 R >>\nstartxref\n")
          .Append(xref).Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    // ── Contacten (CRM) ──────────────────────────────────────────────────────
    private async Task SeedContactenAsync(DateOnly vandaag, CancellationToken ct)
    {
        if (await _db.Contacten.IgnoreQueryFilters().AnyAsync(ct))
        {
            return;
        }

        // 1) Per wachtlijst-inschrijving met oudercontact een contact afleiden + koppelen,
        //    met een rondleiding in de historie.
        List<WachtlijstInschrijving> inschrijvingen = await _db.Wachtlijstinschrijvingen
            .IgnoreQueryFilters()
            .Where(w => w.Oudercontact != null)
            .ToListAsync(ct);
        foreach (WachtlijstInschrijving insch in inschrijvingen)
        {
            Oudercontact oc = insch.Oudercontact!;
            (string vn, string an) = SplitNaam(oc.Naam);
            var contact = new Contact
            {
                OrganisatieId = OrgId,
                Voornaam = vn,
                Achternaam = an,
                Telefoon = oc.Telefoon,
                Email = oc.Email,
                IsIntern = insch.IsIntern,
                Aantekeningen = insch.IsIntern ? "Bestaand gezin bij de opvang." : null,
            };
            _db.Contacten.Add(contact);
            insch.Contact = contact;
            _db.Rondleidingen.Add(new Rondleiding
            {
                OrganisatieId = OrgId,
                Contact = contact,
                Datum = insch.InschrijfdatumWachtlijst.AddDays(7),
                Status = RondleidingStatus.Gehad,
                Notitie = "Rondleiding gehad bij aanmelding.",
            });
        }

        // 2) Een paar geplaatste kinderen aan een contact koppelen, zodat de
        //    contacthistorie ook "geplaatste kinderen" toont.
        List<Kind> kinderen = await _db.Kinderen.IgnoreQueryFilters()
            .Where(k => k.ContactId == null)
            .OrderBy(k => k.Achternaam)
            .Take(3)
            .ToListAsync(ct);
        foreach (Kind kind in kinderen)
        {
            Oudercontact? oc = kind.Oudercontacten.FirstOrDefault();
            (string vn, string an) = oc is not null ? SplitNaam(oc.Naam) : ("Ouder van", kind.Voornaam);
            var contact = new Contact
            {
                OrganisatieId = OrgId,
                Voornaam = vn,
                Achternaam = an,
                Telefoon = oc?.Telefoon,
                Email = oc?.Email,
                IsIntern = true,
                Aantekeningen = $"Ouder/verzorger van {kind.Voornaam}.",
            };
            _db.Contacten.Add(contact);
            kind.Contact = contact;
            _db.Rondleidingen.Add(new Rondleiding
            {
                OrganisatieId = OrgId,
                Contact = contact,
                Datum = vandaag.AddMonths(-6),
                Status = RondleidingStatus.Gehad,
                Notitie = "Rondleiding gehad vóór plaatsing.",
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Splitst een volledige naam in voor- en achternaam (alles na de eerste spatie = achternaam).</summary>
    private static (string Voornaam, string Achternaam) SplitNaam(string volledig)
    {
        string naam = volledig.Trim();
        int spatie = naam.IndexOf(' ');
        return spatie < 0 ? (naam, "—") : (naam[..spatie], naam[(spatie + 1)..]);
    }
}
