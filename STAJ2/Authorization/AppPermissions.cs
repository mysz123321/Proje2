namespace STAJ2.Authorization;

public enum AppPermissions
{
    None, // Herkese açık endpointler için
    Computer_Read,
    Computer_Delete,
    Computer_Rename,
    Computer_SetThreshold,
    Role_Manage,
    User_Manage,
    Tag_Manage,
    Computer_AssignTag,
    Computer_Filter,
    User_Read,
    User_ManageRoles,
    User_ManageComputers,
    User_ManageTags
}