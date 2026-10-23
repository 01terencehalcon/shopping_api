using System.Security.Claims;

namespace ClothesShopping.Api.Security
{
    /// <summary>
    /// Shared helper for extracting identity information from JWT claims.
    /// </summary>
    public static class ClaimsHelper
    {
        /// <summary>
        /// Reads the user ID from the ClaimTypes.NameIdentifier claim.
        /// Throws <see cref="UnauthorizedAccessException"/> when the claim is absent or invalid.
        /// </summary>
        public static int CurrentUserId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("Invalid token: missing user ID.");
            return userId;
        }

        /// <summary>
        /// Returns true when the caller has role 1 (admin).
        /// </summary>
        public static bool IsAdmin(ClaimsPrincipal user)
        {
            var roleClaim = user.FindFirst("user_role_id");
            return roleClaim?.Value == "1";
        }
    }
}
