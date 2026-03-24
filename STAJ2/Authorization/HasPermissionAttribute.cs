// STAJ2/Authorization/HasPermissionAttribute.cs
using System;

namespace STAJ2.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class HasPermissionAttribute : Attribute
{
    public AppPermissions[] Permissions { get; }

    // params sayesinde [HasPermission(AppPermissions.User_Read, AppPermissions.Role_Manage)] şeklinde çoklu yetki verilebilir
    public HasPermissionAttribute(params AppPermissions[] permissions)
    {
        Permissions = permissions;
    }
}