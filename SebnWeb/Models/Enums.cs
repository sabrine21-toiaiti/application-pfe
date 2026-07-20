namespace SebnWeb.Models;

public enum RoleUtilisateur
{
    SuperviseurProd,
    Qualite,
    PitAdmin,
    Direction
}

public enum StatutAnomalie
{
    NonTraitee,
    Corrigee
}

public enum StatutConnexion
{
    Active,
    HorsLigne
}

public static class EnumLabels
{
    public static string Libelle(this RoleUtilisateur role) => role switch
    {
        RoleUtilisateur.SuperviseurProd => "Superviseur Production",
        RoleUtilisateur.Qualite => "Auditeur Qualité",
        RoleUtilisateur.PitAdmin => "Administrateur PIT",
        RoleUtilisateur.Direction => "Direction Générale",
        _ => role.ToString()
    };

    public static string Libelle(this StatutAnomalie statut) => statut switch
    {
        StatutAnomalie.NonTraitee => "Non traitée",
        StatutAnomalie.Corrigee => "Corrigée",
        _ => statut.ToString()
    };
}
