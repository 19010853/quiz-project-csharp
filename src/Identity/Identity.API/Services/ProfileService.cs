using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.API.Models;
using Microsoft.AspNetCore.Identity;

namespace Identity.API.Services;

public class ProfileService(UserManager<ApplicationUser> userManager) : IProfileService
{
    public async Task GetProfileDataAsync(ProfileDataRequestContext context, CancellationToken ct)
    {
        var subjectId = GetSubjectId(context.Subject);

        var user = await userManager.FindByIdAsync(subjectId)
            ?? throw new ArgumentException("Invalid subject identifier");

        context.IssuedClaims = GetClaimsFromUser(user).ToList();
    }

    public async Task IsActiveAsync(IsActiveContext context, CancellationToken ct)
    {
        var subjectId = GetSubjectId(context.Subject);
        var user = await userManager.FindByIdAsync(subjectId);

        context.IsActive = false;

        if (user is null)
            return;

        if (userManager.SupportsUserSecurityStamp)
        {
            var securityStamp = context.Subject.Claims
                .Where(c => c.Type == "security_stamp")
                .Select(c => c.Value)
                .SingleOrDefault();

            if (securityStamp is not null)
            {
                var dbSecurityStamp = await userManager.GetSecurityStampAsync(user);
                if (dbSecurityStamp != securityStamp)
                    return;
            }
        }

        context.IsActive =
            !user.LockoutEnabled ||
            !user.LockoutEnd.HasValue ||
            user.LockoutEnd <= DateTimeOffset.UtcNow;
    }

    private static string GetSubjectId(ClaimsPrincipal? subject)
    {
        ArgumentNullException.ThrowIfNull(subject);

        return subject.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject)?.Value
            ?? throw new ArgumentException("Subject identifier claim is missing");
    }

    private IEnumerable<Claim> GetClaimsFromUser(ApplicationUser user)
    {
        var userName = user.UserName ?? string.Empty;

        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, user.Id),
            new(JwtClaimTypes.PreferredUserName, userName),
            new(JwtRegisteredClaimNames.UniqueName, userName),
            new(JwtClaimTypes.Name, userName)
        };

        if (!string.IsNullOrWhiteSpace(user.FirstName))
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));

        if (!string.IsNullOrWhiteSpace(user.LastName))
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));

        if (userManager.SupportsUserEmail && !string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(JwtClaimTypes.Email, user.Email));
            claims.Add(new Claim(JwtClaimTypes.EmailVerified,
                user.EmailConfirmed ? "true" : "false", ClaimValueTypes.Boolean));
        }

        if (userManager.SupportsUserPhoneNumber && !string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            claims.Add(new Claim(JwtClaimTypes.PhoneNumber, user.PhoneNumber));
            claims.Add(new Claim(JwtClaimTypes.PhoneNumberVerified,
                user.PhoneNumberConfirmed ? "true" : "false", ClaimValueTypes.Boolean));
        }

        return claims;
    }
}
