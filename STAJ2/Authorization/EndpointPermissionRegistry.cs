namespace STAJ2.Authorization;

public static class EndpointPermissionRegistry
{
    // Key: "ControllerName_ActionName" -> Value: AppPermissions Enum
    private static readonly Dictionary<string, AppPermissions> _permissions = new(StringComparer.OrdinalIgnoreCase)
    {
        // ==========================================
        // --- ADMIN CONTROLLER YETKİLERİ ---
        // ==========================================
        { "Admin_GetAllUsers", AppPermissions.User_Read },
        { "Admin_DeleteUser", AppPermissions.User_ManageRoles },
        { "Admin_ChangeUserRoles", AppPermissions.User_ManageRoles },
        { "Admin_GetPendingRequests", AppPermissions.Role_Manage },
        { "Admin_RejectRequest", AppPermissions.Role_Manage },
        { "Admin_ApproveRequest", AppPermissions.Role_Manage },
        { "Admin_GetUserAccess", AppPermissions.User_ManageRoles },
        { "Admin_AssignComputers", AppPermissions.User_ManageComputers },
        { "Admin_AssignTags", AppPermissions.User_ManageTags },
        { "Admin_GetAllComputersForAssignment", AppPermissions.User_ManageComputers },
        { "Admin_GetRoles", AppPermissions.Role_Manage },
        { "Admin_CreateRole", AppPermissions.Role_Manage },
        { "Admin_UpdateRolePermissions", AppPermissions.Role_Manage },
        { "Admin_DeleteRole", AppPermissions.Role_Manage },
        { "Admin_GetTags", AppPermissions.Tag_Manage },
        { "Admin_CreateTag", AppPermissions.Tag_Manage },
        { "Admin_DeleteTag", AppPermissions.Tag_Manage },
        { "Admin_AssignComputersToTag", AppPermissions.Tag_Manage },
        { "Admin_GetTagAssignedComputerIds", AppPermissions.Tag_Manage },

        // ==========================================
        // --- COMPUTER CONTROLLER YETKİLERİ ---
        // ==========================================
        { "Computer_GetComputer", AppPermissions.Computer_Read },
        { "Computer_GetComputerDisks", AppPermissions.Computer_Read },
        { "Computer_UpdateThresholds", AppPermissions.Computer_SetThreshold },
        { "Computer_UpdateComputerTags", AppPermissions.Computer_AssignTag },
        { "Computer_UpdateDisplayName", AppPermissions.Computer_Rename },
        { "Computer_GetMetricsHistory", AppPermissions.Computer_Read },
        { "Computer_GetAllComputers", AppPermissions.Computer_Read },
        { "Computer_DeleteComputer", AppPermissions.Computer_Delete },
        { "Computer_GetMyTags", AppPermissions.Tag_Manage }
    };

    public static AppPermissions? GetRequiredPermission(string controllerName, string actionName)
    {
        string key = $"{controllerName}_{actionName}";
        if (_permissions.TryGetValue(key, out var permission))
        {
            return permission;
        }

        // Eğer listeye eklenmemişse null döner (Bu sayede filter tarafında "yetki istemiyor" olarak algılanır)
        return null;
    }
}