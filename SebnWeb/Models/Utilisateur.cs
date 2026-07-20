using System.Security.Cryptography;
using System.Text;

namespace SebnWeb.Models;

/// <summary>
/// Classe abstraite - factorise le comportement commun des 4 acteurs authentifiés
/// (Superviseur Production, Auditeur Qualité, Administrateur PIT, Direction).
/// Correspond directement au diagramme de classes du Chapitre 4 du rapport.
/// </summary>
public abstract class Utilisateur
{
    public int IdUtilisateur { get; set; }
    public string Login { get; set; } = "";
    public string MotDePasseHash { get; set; } = "";
    public RoleUtilisateur Role { get; protected set; }
    public string NomAffichage { get; set; } = "";

    public static string Hacher(string motDePasse)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(motDePasse));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool VerifierMotDePasse(string mdpSaisi) => Hacher(mdpSaisi) == MotDePasseHash;

    public abstract string ConsulterDashboard();
}

public class SuperviseurProduction : Utilisateur
{
    public SuperviseurProduction() => Role = RoleUtilisateur.SuperviseurProd;

    public override string ConsulterDashboard() => "Dashboard : conformité de la ligne";

    public bool ValiderRepriseDuTravail(Anomalie anomalie) => anomalie.Statut == StatutAnomalie.Corrigee;

    public void CloturerAnomalie(Anomalie anomalie) => anomalie.Cloturer();
}

public class SuperviseurQualite : Utilisateur
{
    public SuperviseurQualite() => Role = RoleUtilisateur.Qualite;

    public override string ConsulterDashboard() => "Dashboard : indicateurs qualité (KPI)";
}

public class SuperviseurPIT : Utilisateur
{
    public SuperviseurPIT() => Role = RoleUtilisateur.PitAdmin;

    public override string ConsulterDashboard() => "Dashboard : administration système";
}

public class Direction : Utilisateur
{
    public Direction() => Role = RoleUtilisateur.Direction;

    public override string ConsulterDashboard() => "Dashboard : synthèse stratégique";
}

public static class UtilisateurFactory
{
    public static Utilisateur Creer(RoleUtilisateur role, int id, string login, string mdpHash, string nomAffichage)
    {
        Utilisateur u = role switch
        {
            RoleUtilisateur.SuperviseurProd => new SuperviseurProduction(),
            RoleUtilisateur.Qualite => new SuperviseurQualite(),
            RoleUtilisateur.PitAdmin => new SuperviseurPIT(),
            RoleUtilisateur.Direction => new Direction(),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        u.IdUtilisateur = id;
        u.Login = login;
        u.MotDePasseHash = mdpHash;
        u.NomAffichage = nomAffichage;
        return u;
    }
}
