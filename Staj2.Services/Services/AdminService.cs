using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models;

namespace Staj2.Services.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IMailSender _mail;

    public AdminService(AppDbContext db , IConfiguration config, IMailSender mail)
    {
        _db = db;
        _config = config;
        _mail = mail;
    }

    public async Task<object> GetRolesAsync()
    {
        return await _db.Roles.Select(r => new { r.Id, r.Name }).ToListAsync();
    }

    public async Task<object> GetAllPermissionsAsync()
    {
        return await _db.Permissions
            .Select(p => new { p.Id, p.Name, p.Description }) // Description buraya geri eklendi
            .ToListAsync();
    }

    public async Task<List<int>> GetRolePermissionsAsync(int roleId)
    {
        return await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();
    }

    public async Task<string?> CreateRoleAsync(CreateRoleRequest request, int? currentUserId)
    {
        // 1. Rol adı daha önce kullanılmış mı kontrolü
        if (await _db.Roles.AnyAsync(r => r.Name.ToLower() == request.Name.ToLower() && !r.IsDeleted))
            return "Bu rol adı zaten kullanılıyor.";

        // 2. Yeni Rolü Oluşturma
        var newRole = new Role
        {
            Name = request.Name,
            CreatedAt = DateTime.Now,
            CreatedBy = currentUserId,
            IsDeleted = false
        };

        _db.Roles.Add(newRole);
        await _db.SaveChangesAsync(); // Id'nin oluşması için önce rolü kaydediyoruz

        // 3. Yetkileri (Permissions) Atama
        if (request.PermissionIds != null && request.PermissionIds.Any())
        {
            foreach (var permId in request.PermissionIds)
            {
                newRole.RolePermissions.Add(new RolePermission
                {
                    RoleId = newRole.Id,
                    PermissionId = permId
                });
            }
            await _db.SaveChangesAsync();
        }

        return null; // Null dönmesi işlemin başarılı olduğunu gösterir
    }

    public async Task<string?> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request, int? currentUserId)
    {
        var role = await _db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Id == roleId);
        if (role == null) return "Rol bulunamadı."; // NotFound durumu

        // İşlemi Yapanı ve Tarihi Güncelle
        role.UpdatedAt = DateTime.Now;
        role.UpdatedBy = currentUserId;

        // Eski yetkileri tamamen temizle
        role.RolePermissions.Clear();

        // Gelen yeni yetki ID'lerini veritabanından doğrula ve ekle
        var validPermissions = await _db.Permissions.Where(p => request.PermissionIds.Contains(p.Id)).ToListAsync();

        foreach (var perm in validPermissions)
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = perm.Id
            });
        }

        await _db.SaveChangesAsync();
        return null; // Başarılı
    }

    public async Task<string?> DeleteRoleAsync(int id, int? currentUserId)
    {
        var role = await _db.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return "Rol bulunamadı.";

        var adminRoleName = _config["AppDefaults:AdminRoleName"] ?? "Yönetici";
        if (role.Name == adminRoleName) return $"Sistem varsayılan '{adminRoleName}' rolü silinemez.";
        if (role.Users.Any(u => !u.IsDeleted)) return "Bu role sahip aktif kullanıcılar var! Silmek için önce o kullanıcıların rolünü değiştirin.";

        role.IsDeleted = true;
        role.DeletedAt = DateTime.Now;
        role.DeletedBy = currentUserId;

        // Role ait yetkilerin (RolePermission) silinmesi
        var rolePermissions = await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
        rolePermissions.ForEach(rp => {
            rp.IsDeleted = true;
            rp.DeletedAt = DateTime.Now;
            rp.DeletedBy = currentUserId;
        });

        // Role ait kullanıcı bağlarının (UserRole) silinmesi
        var userRoles = await _db.UserRoles.Where(ur => ur.RoleId == id).ToListAsync();
        userRoles.ForEach(ur => {
            ur.IsDeleted = true;
            ur.DeletedAt = DateTime.Now;
            ur.DeletedBy = currentUserId;
        });

        await _db.SaveChangesAsync();
        return null;
    }

    // --- KULLANICI YÖNETİMİ İŞ MANTIKLARI ---

    public async Task<object> GetAllUsersAsync()
    {
        return await _db.Users.Include(u => u.Roles)
                          .OrderBy(u => u.Username)
                          .Select(u => new { u.Id, u.Username, u.Email, Roles = u.Roles.Select(r => r.Name).ToList() })
                          .ToListAsync();
    }

    public async Task<string?> DeleteUserAsync(int id, int? currentUserId) // Parametre eklendi
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return "Kullanıcı bulunamadı.";

        user.IsDeleted = true;
        user.DeletedAt = DateTime.Now;
        user.DeletedBy = currentUserId; // Eklendi

        // Kullanıcıya ait Rol bağlantılarını sil
        var userRoles = await _db.UserRoles.Where(ur => ur.UserId == id).ToListAsync();
        userRoles.ForEach(ur => {
            ur.IsDeleted = true;
            ur.DeletedAt = DateTime.Now;
            ur.DeletedBy = currentUserId; // Eklendi
        });

        // Kullanıcıya ait Cihaz erişimlerini sil
        var userComputerAccesses = await _db.UserComputerAccesses.Where(uca => uca.UserId == id).ToListAsync();
        userComputerAccesses.ForEach(uca => {
            uca.IsDeleted = true;
            uca.DeletedAt = DateTime.Now;
            uca.DeletedBy = currentUserId; // Eklendi
        });

        // Kullanıcıya ait Etiket erişimlerini sil
        var userTagAccesses = await _db.UserTagAccesses.Where(uta => uta.UserId == id).ToListAsync();
        userTagAccesses.ForEach(uta => {
            uta.IsDeleted = true;
            uta.DeletedAt = DateTime.Now;
            uta.DeletedBy = currentUserId; // Eklendi
        });

        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> ChangeUserRolesAsync(int userId, ChangeRolesRequest request)
    {
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            return "Kullanıcı bulunamadı."; // NotFound

        var newRoles = await _db.Roles.Where(r => request.NewRoleIds.Contains(r.Id)).ToListAsync();
        if (newRoles.Count == 0)
            return "En az bir geçerli rol seçilmelidir."; // BadRequest

        user.Roles.Clear();
        foreach (var role in newRoles)
            user.Roles.Add(role);

        await _db.SaveChangesAsync();

        return null; // Başarılı
    }

    // --- KAYIT İSTEKLERİ YÖNETİMİ İŞ MANTIKLARI ---

    public async Task<object> GetPendingRequestsAsync()
    {
        return await _db.RegistrationRequests.Where(x => x.Status == RegistrationStatus.Pending).ToListAsync();
    }

    public async Task<(bool IsSuccess, string? ErrorMessage)> RejectRequestAsync(RejectRegistrationRequest request, int? adminId)
    {
        if (!string.IsNullOrEmpty(request.RejectionReason) && request.RejectionReason.Length > 200)
            return (false, "Ret gerekçesi 200 karakterden uzun olamaz.");

        var registration = await _db.RegistrationRequests.FindAsync(request.RequestId);
        if (registration == null)
            return (false, "Talep bulunamadı.");

        if (registration.Status != RegistrationStatus.Pending)
            return (false, "Bu talep zaten işleme alınmış.");

        if (adminId.HasValue)
            registration.RejectedBy = adminId.Value;

        registration.Status = RegistrationStatus.Rejected;
        registration.RejectedAt = DateTime.Now;
        registration.RejectionReason = request.RejectionReason;

        await _db.SaveChangesAsync();

        // --- MAİL İŞLEMİ CONTROLLER'DAN BURAYA TAŞINDI ---
        try
        {
            await _mail.SendAsync(
                registration.Email!,
                "Kayıt Talebiniz Reddedildi",
                $"Merhaba {registration.Username},\n\nTalebiniz maalesef onaylanmadı.\nSebep: {request.RejectionReason ?? "Belirtilmedi"}"
            );
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nMAIL GÖNDERİM HATASI: {ex.Message}\n");
            Console.ResetColor();
        }

        return (true, null); // Mail gitmese bile veritabanı işlemi başarılı olduğu için True dönüyoruz.
    }

    public async Task<(bool IsSuccess, string? ErrorMessage)> ApproveRequestAsync(int id, ChangeRoleRequest? req, int? adminId)
    {
        var request = await _db.RegistrationRequests.FindAsync(id);
        if (request == null)
            return (false, "Talep bulunamadı.");

        if (request.Status != RegistrationStatus.Pending)
            return (false, "Bu talep zaten işlenmiş.");

        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);
        if (existingUser != null)
            return (false, $"Bu kullanıcı zaten sistemde kayıtlı! (Kullanıcı: {existingUser.Username})");

        int finalAdminId = adminId ?? 0;
        if (finalAdminId == 0)
        {
            var firstUser = await _db.Users.FirstOrDefaultAsync();
            if (firstUser != null) finalAdminId = firstUser.Id;
        }
        request.ApprovedByUserId = finalAdminId;

        if (req != null && req.NewRoleId > 0)
            request.RequestedRoleId = req.NewRoleId;

        // Token Oluştur ve Hashle
        var token = Guid.NewGuid().ToString("N");
        string tokenHash;
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            tokenHash = Convert.ToHexString(bytes);
        }

        var setupToken = new PasswordSetupToken
        {
            RegistrationRequestId = request.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.Now.AddHours(24),
            IsUsed = false
        };
        _db.PasswordSetupTokens.Add(setupToken);

        request.Status = RegistrationStatus.Approved;
        request.ApprovedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        // --- LİNK OLUŞTURMA VE MAİL İŞLEMİ CONTROLLER'DAN BURAYA TAŞINDI ---
        var baseUrl = _config["App:FrontendBaseUrl"] ?? "http://localhost:5267";
        var frontendLink = $"{baseUrl}/set-password.html?token={token}";

        try
        {
            await _mail.SendAsync(
                request.Email!,
                "Hoşgeldiniz",
                $"Hesabınız onaylandı. Şifrenizi belirlemek için tıklayın: {frontendLink}"
            );
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nMAIL GÖNDERİM HATASI: {ex.Message}\n");
            Console.ResetColor();
        }

        return (true, null); // Mail gitmese bile veritabanı işlemi başarılı olduğu için True dönüyoruz.
    }
    // --- KULLANICI CİHAZ VE ETİKET ATAMA YÖNETİMİ İŞ MANTIKLARI ---

    public async Task<object?> GetUserAccessAsync(int userId)
    {
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null; // Controller'da NotFound döneceğiz

        var computerIds = await _db.UserComputerAccesses.Where(x => x.UserId == userId).Select(x => x.ComputerId).ToListAsync();
        var tagIds = await _db.UserTagAccesses.Where(x => x.UserId == userId).Select(x => x.TagId).ToListAsync();

        var adminRoleName = _config["AppDefaults:AdminRoleName"] ?? "Yönetici";

        if (computerIds.Count == 0 && tagIds.Count == 0 && user.Roles.Any(r => r.Name == adminRoleName))
        {
            computerIds = await _db.Computers.Select(x => x.Id).ToListAsync();
            tagIds = await _db.Tags.Select(x => x.Id).ToListAsync();
        }

        return new { computerIds, tagIds };
    }

    public async Task<string?> AssignComputersAsync(int userId, AssignComputersRequest req)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return "Kullanıcı bulunamadı.";

        var existing = await _db.UserComputerAccesses.Where(x => x.UserId == userId).ToListAsync();
        _db.UserComputerAccesses.RemoveRange(existing);

        foreach (var cid in req.ComputerIds)
            _db.UserComputerAccesses.Add(new UserComputerAccess { UserId = userId, ComputerId = cid });

        await _db.SaveChangesAsync();
        return null; // Başarılı
    }

    public async Task<string?> AssignTagsAsync(int userId, AssignTagsRequest req)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return "Kullanıcı bulunamadı.";

        var existing = await _db.UserTagAccesses.Where(x => x.UserId == userId).ToListAsync();
        _db.UserTagAccesses.RemoveRange(existing);

        foreach (var tid in req.TagIds)
            _db.UserTagAccesses.Add(new UserTagAccess { UserId = userId, TagId = tid });

        await _db.SaveChangesAsync();
        return null; // Başarılı
    }

    public async Task<object> GetAllComputersForAssignmentAsync()
    {
        return await _db.Computers
            .Select(c => new
            {
                id = c.Id,
                displayName = c.DisplayName,
                machineName = c.MachineName,
                ipAddress = c.IpAddress,
                isDeleted = c.IsDeleted,
                lastSeen = c.LastSeen
            })
            .ToListAsync();
    }

    // --- ETİKET YÖNETİMİ İŞ MANTIKLARI ---

    public async Task<object> GetTagsAsync()
    {
        return await _db.Tags.ToListAsync();
    }

    public async Task<(bool IsSuccess, string? ErrorMessage, object? CreatedTag)> CreateTagAsync(TagCreateRequest request, int? userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "İsim boş olamaz.", null);

        if (request.Name.Length > 200)
            return (false, "Etiket ismi 200 karakterden uzun olamaz.", null);

        if (await _db.Tags.AnyAsync(t => t.Name == request.Name))
            return (false, "Bu etiket zaten var.", null);

        // 1. Etiketi oluştur
        var tag = new Tag { Name = request.Name.Trim() };
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();

        // 2. OTOMATİK ATAMA
        if (userId.HasValue)
        {
            _db.UserTagAccesses.Add(new UserTagAccess
            {
                UserId = userId.Value,
                TagId = tag.Id
            });
            await _db.SaveChangesAsync();
        }

        return (true, null, new { id = tag.Id, name = tag.Name });
    }

    public async Task<string?> DeleteTagAsync(int id, int? currentUserId) // Parametre eklendi
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag == null) return "Etiket bulunamadı.";

        tag.IsDeleted = true;
        tag.DeletedAt = DateTime.Now;
        tag.DeletedBy = currentUserId; // Eklendi

        // Etikete ait Cihaz bağlantılarını (ComputerTag) sil
        var computerTags = await _db.ComputerTags.Where(ct => ct.TagId == id).ToListAsync();
        computerTags.ForEach(ct => {
            ct.IsDeleted = true;
            ct.DeletedAt = DateTime.Now;
            ct.DeletedBy = currentUserId; // Eklendi
        });

        // Etikete ait Kullanıcı erişimlerini (UserTagAccess) sil
        var userTagAccesses = await _db.UserTagAccesses.Where(uta => uta.TagId == id).ToListAsync();
        userTagAccesses.ForEach(uta => {
            uta.IsDeleted = true;
            uta.DeletedAt = DateTime.Now;
            uta.DeletedBy = currentUserId; // Eklendi
        });

        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> AssignComputersToTagAsync(int tagId, AssignComputersToTagRequest req)
    {
        var tag = await _db.Tags.Include(t => t.Computers).FirstOrDefaultAsync(t => t.Id == tagId);
        if (tag == null) return "Etiket bulunamadı.";

        tag.Computers.Clear();

        var computers = await _db.Computers.Where(c => req.ComputerIds.Contains(c.Id)).ToListAsync();
        foreach (var c in computers) tag.Computers.Add(c);

        await _db.SaveChangesAsync();
        return null; // Başarılı
    }

    public async Task<List<int>> GetTagAssignedComputerIdsAsync(int tagId)
    {
        return await _db.Tags
            .Where(t => t.Id == tagId)
            .SelectMany(t => t.Computers.Select(c => c.Id))
            .ToListAsync();
    }
}