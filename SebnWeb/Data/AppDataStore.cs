using System.Text.Json;
using System.Text.Json.Serialization;
using SebnWeb.Models;

namespace SebnWeb.Data;

public class UtilisateurRecord
{
    public int IdUtilisateur { get; set; }
    public string Login { get; set; } = "";
    public string MotDePasseHash { get; set; } = "";
    public RoleUtilisateur Role { get; set; }
    public string NomAffichage { get; set; } = "";
}

public class Database
{
    public List<UtilisateurRecord> Utilisateurs { get; set; } = new();
    public List<Camera> Cameras { get; set; } = new();
    public List<Poste> Postes { get; set; } = new();
    public List<Operateur> Operateurs { get; set; } = new();
    public List<Anomalie> Anomalies { get; set; } = new();
    public int NextAnomalieId { get; set; } = 1;
}

/// <summary>
/// Couche d'accès aux données (backend). Stockage JSON local -
/// aucune installation de serveur de base de données requise.
/// </summary>
public class AppDataStore
{
    private readonly string _cheminFichier;
    private readonly object _verrou = new();
    private Database _db = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppDataStore(IWebHostEnvironment env)
    {
        var dossier = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dossier);
        _cheminFichier = Path.Combine(dossier, "sebn_detection.json");
        ChargerOuInitialiser();
    }

    private void ChargerOuInitialiser()
    {
        lock (_verrou)
        {
            if (File.Exists(_cheminFichier))
            {
                var json = File.ReadAllText(_cheminFichier);
                _db = JsonSerializer.Deserialize<Database>(json, JsonOptions) ?? new Database();
            }
            else
            {
                _db = SeedGenerator.Generer();
                Sauvegarder();
            }
        }
    }

    private void Sauvegarder()
    {
        var json = JsonSerializer.Serialize(_db, JsonOptions);
        File.WriteAllText(_cheminFichier, json);
    }

    // ---------------- Authentification ----------------

    public Utilisateur? VerifierUtilisateur(string login, string mdpHash)
    {
        lock (_verrou)
        {
            var rec = _db.Utilisateurs.FirstOrDefault(u => u.Login == login && u.MotDePasseHash == mdpHash);
            if (rec == null) return null;
            return UtilisateurFactory.Creer(rec.Role, rec.IdUtilisateur, rec.Login, rec.MotDePasseHash, rec.NomAffichage);
        }
    }

    public List<UtilisateurRecord> ListeUtilisateurs()
    {
        lock (_verrou) return _db.Utilisateurs.ToList();
    }

    // ---------------- Anomalies ----------------

    public int InsererAnomalie(string typeAnomalie, string classeYolo, double confiance,
        string imagePreuve, string idPoste, string matriculeOp)
    {
        lock (_verrou)
        {
            var anomalie = new Anomalie
            {
                IdAnomalie = _db.NextAnomalieId++,
                DateHeure = DateTime.Now,
                TypeAnomalie = typeAnomalie,
                ClasseYolo = classeYolo,
                Confiance = confiance,
                ImagePreuve = imagePreuve,
                Statut = StatutAnomalie.NonTraitee,
                IdPoste = idPoste,
                MatriculeOp = matriculeOp
            };
            _db.Anomalies.Add(anomalie);
            Sauvegarder();
            return anomalie.IdAnomalie;
        }
    }

    public List<Anomalie> RecupererHistorique(string? type = null, StatutAnomalie? statut = null,
        string? idPoste = null, int limite = 300)
    {
        lock (_verrou)
        {
            var q = _db.Anomalies.AsEnumerable();
            if (!string.IsNullOrEmpty(type) && type != "Tous") q = q.Where(a => a.TypeAnomalie == type);
            if (statut.HasValue) q = q.Where(a => a.Statut == statut.Value);
            if (!string.IsNullOrEmpty(idPoste) && idPoste != "Tous") q = q.Where(a => a.IdPoste == idPoste);
            return q.OrderByDescending(a => a.DateHeure).Take(limite).ToList();
        }
    }

    public void CloturerAnomalie(int idAnomalie)
    {
        lock (_verrou)
        {
            var a = _db.Anomalies.FirstOrDefault(x => x.IdAnomalie == idAnomalie);
            a?.Cloturer();
            Sauvegarder();
        }
    }

    // ---------------- Référentiels ----------------

    public List<Poste> ListePostes()
    {
        lock (_verrou) return _db.Postes.ToList();
    }

    public Operateur? TrouverOperateur(string matricule)
    {
        lock (_verrou) return _db.Operateurs.FirstOrDefault(o => o.MatriculeOp == matricule);
    }

    public string LigneProduction(string idPoste)
    {
        lock (_verrou) return _db.Postes.FirstOrDefault(p => p.IdPoste == idPoste)?.LigneProduction ?? idPoste;
    }

    // ---------------- Statistiques ----------------

    public (int total, int nonTraitees, int aujourdhui, double tauxConformite) StatsGenerales()
    {
        lock (_verrou)
        {
            int total = _db.Anomalies.Count;
            int nonTraitees = _db.Anomalies.Count(a => a.Statut == StatutAnomalie.NonTraitee);
            int aujourdhui = _db.Anomalies.Count(a => a.DateHeure.Date == DateTime.Today);
            double taux = total == 0 ? 100.0 : Math.Round(100.0 - (nonTraitees * 100.0 / total), 1);
            return (total, nonTraitees, aujourdhui, taux);
        }
    }

    public Dictionary<string, int> RepartitionParType()
    {
        lock (_verrou)
            return _db.Anomalies.GroupBy(a => a.TypeAnomalie).ToDictionary(g => g.Key, g => g.Count());
    }

    public Dictionary<string, int> RepartitionParPoste()
    {
        lock (_verrou)
            return _db.Anomalies
                .GroupBy(a => LigneProduction(a.IdPoste))
                .ToDictionary(g => g.Key, g => g.Count());
    }

    public Dictionary<string, int> EvolutionJournaliere(int jours = 14)
    {
        lock (_verrou)
        {
            var depuis = DateTime.Today.AddDays(-jours);
            return _db.Anomalies
                .Where(a => a.DateHeure.Date >= depuis)
                .GroupBy(a => a.DateHeure.ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }

    public List<(string classe, int total)> TopDefauts(int limite = 5)
    {
        lock (_verrou)
            return _db.Anomalies.GroupBy(a => a.ClasseYolo)
                .Select(g => (g.Key, g.Count()))
                .OrderByDescending(x => x.Item2)
                .Take(limite)
                .ToList();
    }
}
