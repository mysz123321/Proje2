using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;

namespace STAJ2.Seed
{
    public static class PermissionSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Eğer tabloda hiç kayıt yoksa çalıştır
            if (!context.EndpointPermissions.Any())
            {
                var permissions = new List<EndpointPermission>
                {
                    // --- ADMIN CONTROLLER ---
                    new EndpointPermission { ControllerName = "Admin", ActionName = "GetAllUsers", RequiredPermission = "User.Read" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "ChangeUserRoles", RequiredPermission = "User.ManageRoles" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "PendingRequests", RequiredPermission = "User.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "RejectRequest", RequiredPermission = "User.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "ApproveRequest", RequiredPermission = "User.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "CreateTag", RequiredPermission = "Tag.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "DeleteTag", RequiredPermission = "Tag.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "GetAllPermissions", RequiredPermission = "Role.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "GetRolePermissions", RequiredPermission = "Role.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "UpdateRolePermissions", RequiredPermission = "Role.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "GetUserAccess", RequiredPermission = "User.ManageComputers,User.ManageTags" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "AssignComputers", RequiredPermission = "User.ManageComputers" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "AssignTags", RequiredPermission = "User.ManageTags" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "AssignComputersToTag", RequiredPermission = "Tag.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "GetAllComputersForAssignment", RequiredPermission = "User.ManageComputers,Tag.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "GetTagAssignedComputerIds", RequiredPermission = "Tag.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "CreateRole", RequiredPermission = "Role.Manage" },
                    new EndpointPermission { ControllerName = "Admin", ActionName = "DeleteRole", RequiredPermission = "Role.Manage" },

                    // --- COMPUTER CONTROLLER ---
                    new EndpointPermission { ControllerName = "Computer", ActionName = "GetComputer", RequiredPermission = "Computer.Read" },
                    new EndpointPermission { ControllerName = "Computer", ActionName = "GetComputerDisks", RequiredPermission = "Computer.Read" },
                    new EndpointPermission { ControllerName = "Computer", ActionName = "UpdateThresholds", RequiredPermission = "Computer.SetThreshold" },
                    new EndpointPermission { ControllerName = "Computer", ActionName = "UpdateComputerTags", RequiredPermission = "Computer.AssignTag" },
                    new EndpointPermission { ControllerName = "Computer", ActionName = "UpdateDisplayName", RequiredPermission = "Computer.Rename" },
                    new EndpointPermission { ControllerName = "Computer", ActionName = "GetMetricsHistory", RequiredPermission = "Computer.Filter" },
                    new EndpointPermission { ControllerName = "Computer", ActionName = "GetAllComputers", RequiredPermission = "Computer.Read" },
                    new EndpointPermission { ControllerName = "Computer", ActionName = "DeleteComputer", RequiredPermission = "Computer.Delete" }
                };

                context.EndpointPermissions.AddRange(permissions);
                context.SaveChanges();
            }
        }
    }
}