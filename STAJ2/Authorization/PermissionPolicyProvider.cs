using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace STAJ2.Authorization;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Önce varsayılan policy'lere bak (Örn: Authorize etiketi yalın kullanıldıysa)
        var policy = await base.GetPolicyAsync(policyName);
        if (policy != null) return policy;

        // Bulamazsa, gelen policyName bizim Permission'ımız demektir. Yeni bir policy oluştur.
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName)) // Bizim yazdığımız Requirement'ı ekle
            .Build();
    }
}