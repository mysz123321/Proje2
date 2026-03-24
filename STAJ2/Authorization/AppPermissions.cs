namespace STAJ2.Authorization;

public enum AppPermissions
{
    None, // Herkese açık endpointler için
    User_Read,
    User_ManageRoles,
    User_ManageComputers,
    User_ManageTags,
    Role_Manage,
    Computer_Read,
    Computer_Delete,
    Computer_Rename,
    Computer_SetThreshold,
    Computer_AssignTag,
    Tag_Manage
}