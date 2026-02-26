using Microsoft.AspNetCore.Authorization;

namespace STAJ2.Authorization;

// Bu attribute'u Controller metodlarının tepesinde kullanacağız
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(policy: permission)
    {
    }
}