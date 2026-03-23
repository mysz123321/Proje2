namespace STAJ2.Authorization;

public static class EndpointPermissionRegistry
{
    // Artık tek bir Enum yerine string dizisi tutuyoruz. Böylece bir endpoint'e birden fazla yetkiyle erişilebilir.
    private static readonly Dictionary<string, string[]> _permissions = new(StringComparer.OrdinalIgnoreCase)
    {
        // ==========================================
        // --- ADMIN CONTROLLER YETKİLERİ ---
        // ==========================================
        // Kullanıcı listesini görmek için bu 4 yetkiden herhangi biri yeterli olmalı
        { "Admin_GetAllUsers", new[] { "User.Read", "User.ManageRoles", "User.ManageComputers", "User.ManageTags" } },
        { "Admin_DeleteUser", new[] { "User.ManageRoles" } },
        { "Admin_ChangeUserRoles", new[] { "User.ManageRoles" } },

        { "Admin_GetPendingRequests", new[] { "User.Manage" } },
        { "Admin_RejectRequest", new[] { "User.Manage" } },
        { "Admin_ApproveRequest", new[] { "User.Manage" } },
        
        // Kullanıcı yetkilerini çeken endpoint, kullanıcının rol, cihaz veya etiket atamasını yapan herkes tarafından okunabilmeli!
        { "Admin_GetUserAccess", new[] { "User.ManageRoles", "User.ManageComputers", "User.ManageTags" } },
        { "Admin_AssignComputers", new[] { "User.ManageComputers" } },
        { "Admin_AssignTags", new[] { "User.ManageTags" } },
        
        // Cihazları atama yaparken listelemek için Tag yöneten veya User-Computer yöneten herkes erişebilmeli
        { "Admin_GetAllComputersForAssignment", new[] { "User.ManageComputers", "Tag.Manage" } }, 
        
        // Roller listesini, rol ataması yapacak olan "User.ManageRoles" yetkilisi de görebilmeli
        { "Admin_GetRoles", new[] { "Role.Manage", "User.ManageRoles" } },
        { "Admin_CreateRole", new[] { "Role.Manage" } },
        { "Admin_UpdateRolePermissions", new[] { "Role.Manage" } },
        { "Admin_DeleteRole", new[] { "Role.Manage" } },
        
        // Etiketler listesini, etiket ataması yapacak olan "User.ManageTags" yetkilisi de görebilmeli
        { "Admin_GetTags", new[] { "Tag.Manage", "User.ManageTags" } },
        { "Admin_CreateTag", new[] { "Tag.Manage" } },
        { "Admin_DeleteTag", new[] { "Tag.Manage" } },
        { "Admin_AssignComputersToTag", new[] { "Tag.Manage" } },
        { "Admin_GetTagAssignedComputerIds", new[] { "Tag.Manage" } },

        // ==========================================
        // --- COMPUTER CONTROLLER YETKİLERİ ---
        // ==========================================
        { "Computer_GetComputer", new[] { "Computer.Read" } },
        { "Computer_GetComputerDisks", new[] { "Computer.Read" } },
        { "Computer_UpdateThresholds", new[] { "Computer.SetThreshold" } },
        { "Computer_UpdateComputerTags", new[] { "Computer.AssignTag" } },
        { "Computer_UpdateDisplayName", new[] { "Computer.Rename" } },
        { "Computer_GetMetricsHistory", new[] { "Computer.Read" } },
        { "Computer_GetAllComputers", new[] { "Computer.Read" } },
        { "Computer_DeleteComputer", new[] { "Computer.Delete" } },
        { "Computer_GetMyTags", new[] { "Tag.Manage" } }
    };

    // Geri dönüş tipini string[] yaptık
    public static string[]? GetRequiredPermissions(string controllerName, string actionName)
    {
        string key = $"{controllerName}_{actionName}";
        if (_permissions.TryGetValue(key, out var perms))
        {
            return perms;
        }

        return null;
    }
}