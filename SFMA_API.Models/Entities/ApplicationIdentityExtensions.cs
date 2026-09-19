using Microsoft.AspNetCore.Identity;

namespace SFMA_API.Models.Entities
{
    public class ApplicationUserRole : IdentityUserRole<string>
    {
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ApplicationRole Role { get; set; } = null!;
    }

    public class ApplicationUserClaim : IdentityUserClaim<string>
    {
        public bool Active { get; set; } = true;
        public virtual ApplicationUser User { get; set; } = null!;
    }

    public class ApplicationRoleClaim : IdentityRoleClaim<string>
    {
        public bool Active { get; set; } = true;
        public virtual ApplicationRole Role { get; set; } = null!;
    }
}
