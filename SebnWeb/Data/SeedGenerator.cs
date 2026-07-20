using SebnWeb.Models;

namespace SebnWeb.Data;

/// <summary>
/// Génère un jeu de données simulé réaliste, en l'absence de données réelles
/// SEBN pour raisons de confidentialité industrielle (voir Chapitre 2 du rapport).
/// </summary>
public static class SeedGenerator
{
    private static readonly string[] Prenoms = { "Ahmed", "Mohamed", "Fatma", "Ines", "Youssef", "Amira",
        "Sami", "Rania", "Karim", "Nour", "Bilel", "Salma", "Anis", "Emna", "Walid", "Sirine" };
    private static readonly string[] Noms = { "Ben Ali", "Trabelsi", "Jendoubi", "Gharbi", "Chaabane",
        "Mansouri", "Belhaj", "Kefi", "Bouzid", "Hamdi", "Sassi", "Riahi" };
    private static readonly string[] Equipes = { "Équipe A", "Équipe B", "Équipe C" };

    private static readonly Dictionary<string, string[]> ClassesParType = new()
    {
        ["Qualité"] = new[] { "connecteur_manquant", "fil_mal_positionne", "defaut_couleur", "sertissage_defectueux" },
        ["Production"] = new[] { "cable_mal_clipse", "sous_ensemble_incomplet" },
        ["5S"] = new[] { "outil_hors_zone", "poste_desordre" },
    };

    public static Database Generer()
    {
        var rnd = new Random(42);
        var db = new Database();

        // Utilisateurs
        db.Utilisateurs = new List<UtilisateurRecord>
        {
            new() { IdUtilisateur = 1, Login = "superviseur", MotDePasseHash = Utilisateur.Hacher("sebn2026"),
                    Role = RoleUtilisateur.SuperviseurProd, NomAffichage = "Mehdi Trabelsi" },
            new() { IdUtilisateur = 2, Login = "qualite", MotDePasseHash = Utilisateur.Hacher("sebn2026"),
                    Role = RoleUtilisateur.Qualite, NomAffichage = "Ines Gharbi" },
            new() { IdUtilisateur = 3, Login = "admin", MotDePasseHash = Utilisateur.Hacher("sebn2026"),
                    Role = RoleUtilisateur.PitAdmin, NomAffichage = "Sami Bouzid" },
            new() { IdUtilisateur = 4, Login = "direction", MotDePasseHash = Utilisateur.Hacher("sebn2026"),
                    Role = RoleUtilisateur.Direction, NomAffichage = "Nadia Mansouri" },
        };

        // Caméras & Postes
        db.Cameras = new List<Camera>
        {
            new() { IdCamera = "CAM-01", StatutConnexion = StatutConnexion.Active },
            new() { IdCamera = "CAM-02", StatutConnexion = StatutConnexion.Active },
            new() { IdCamera = "CAM-03", StatutConnexion = StatutConnexion.Active },
            new() { IdCamera = "CAM-04", StatutConnexion = StatutConnexion.HorsLigne },
        };
        db.Postes = new List<Poste>
        {
            new() { IdPoste = "P01", LigneProduction = "Ligne El Fejja 01", IdCamera = "CAM-01" },
            new() { IdPoste = "P02", LigneProduction = "Ligne El Fejja 02", IdCamera = "CAM-02" },
            new() { IdPoste = "P03", LigneProduction = "Ligne El Fejja 03", IdCamera = "CAM-03" },
            new() { IdPoste = "P04", LigneProduction = "Ligne El Fejja 04", IdCamera = "CAM-04" },
        };

        // Opérateurs
        for (int i = 1; i <= 12; i++)
        {
            db.Operateurs.Add(new Operateur
            {
                MatriculeOp = $"OP{100 + i}",
                PrenomOp = Prenoms[rnd.Next(Prenoms.Length)],
                NomOp = Noms[rnd.Next(Noms.Length)],
                Equipe = Equipes[i % 3]
            });
        }

        // Anomalies (420 sur 30 jours)
        int id = 1;
        var typesPonderes = new List<(string type, int poids)> { ("Qualité", 45), ("Production", 30), ("5S", 25) };
        for (int n = 0; n < 420; n++)
        {
            int joursEcart = rnd.Next(0, 30);
            int heure = 6 + rnd.Next(0, 16);
            int minute = rnd.Next(0, 60);
            var date = DateTime.Now.AddDays(-joursEcart).Date.AddHours(heure).AddMinutes(minute);

            string type = TirageSelonPoids(rnd, typesPonderes);
            var classes = ClassesParType[type];
            string classe = classes[rnd.Next(classes.Length)];
            double confiance = Math.Round(0.62 + rnd.NextDouble() * (0.98 - 0.62), 2);
            var poste = db.Postes[rnd.Next(db.Postes.Count)];
            var operateur = db.Operateurs[rnd.Next(db.Operateurs.Count)];

            double probaCorrigee = joursEcart > 2 ? 0.95 : 0.55;
            var statut = rnd.NextDouble() < probaCorrigee ? StatutAnomalie.Corrigee : StatutAnomalie.NonTraitee;

            db.Anomalies.Add(new Anomalie
            {
                IdAnomalie = id,
                DateHeure = date,
                TypeAnomalie = type,
                ClasseYolo = classe,
                Confiance = confiance,
                ImagePreuve = $"captures/anomalie_{id:D4}.jpg",
                Statut = statut,
                IdPoste = poste.IdPoste,
                MatriculeOp = operateur.MatriculeOp
            });
            id++;
        }
        db.NextAnomalieId = id;
        db.Anomalies = db.Anomalies.OrderBy(a => a.DateHeure).ToList();

        return db;
    }

    private static string TirageSelonPoids(Random rnd, List<(string type, int poids)> options)
    {
        int total = options.Sum(o => o.poids);
        int tirage = rnd.Next(total);
        int cumul = 0;
        foreach (var (type, poids) in options)
        {
            cumul += poids;
            if (tirage < cumul) return type;
        }
        return options[^1].type;
    }
}
