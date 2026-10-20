using Duende.IdentityServer.Models;

namespace Identity.API;

public static class Config
{
    public const string QuizApiScope = "quiz_api";

    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope(QuizApiScope, "Quiz Examination API")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource(QuizApiScope, "Quiz Examination API")
        {
            Scopes = { QuizApiScope }
        }
    ];

    public static IEnumerable<Client> Clients =>
    [
        // Machine-to-machine client, used to test token issuance without a login UI
        new Client
        {
            ClientId = "quiz_m2m",
            ClientName = "Quiz Machine-to-Machine Client",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedScopes = { QuizApiScope }
        }
    ];
}
