using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreGrid.Api.Identity;

// Real implementation of IIdentityDirectory against ThunderID's management
// API (doc/setup/ThunderID.md). Ported from the confirmed-working client in
// the sibling OpenSchool project (backend/internal/thunderid/client.go),
// adjusted for CoreGrid's single organisation unit — there is no
// organisation-creation call here, only user creation + role assignment —
// and CoreGrid's CoreGridUser attribute set (email/given_name/family_name/
// password, no username).
public class ThunderIdIdentityDirectory(HttpClient httpClient, IConfiguration configuration) : IIdentityDirectory
{
    public async Task<string> ProvisionAdministratorAsync(
        string email,
        string givenName,
        string familyName,
        string password,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var ouId = RequireConfig("ThunderID:OuId");
        var userType = configuration["ThunderID:UserType"] ?? "CoreGridUser";
        var administratorRoleId = RequireConfig("ThunderID:RoleIds:Administrator");

        var userId = await CreateUserAsync(accessToken, ouId, userType, email, givenName, familyName, password, cancellationToken);
        await AssignRoleAsync(accessToken, administratorRoleId, userId, cancellationToken);

        return userId;
    }

    // No token caching — this mirrors OpenSchool's own client, which
    // re-fetches a token on every management call rather than caching one.
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var clientId = RequireConfig("ThunderID:ScimClientId");
        var clientSecret = RequireConfig("ThunderID:ScimClientSecret");
        var resource = RequireConfig("ThunderID:Resource");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = "user-management",
                ["resource"] = resource,
            }),
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"ThunderID token request failed ({(int)response.StatusCode}): {body}");
        }

        var token = JsonSerializer.Deserialize<TokenResponse>(body, JsonOptions);
        return token?.AccessToken
            ?? throw new InvalidOperationException("ThunderID token response had no access_token.");
    }

    private async Task<string> CreateUserAsync(
        string accessToken,
        string ouId,
        string userType,
        string email,
        string givenName,
        string familyName,
        string password,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users")
        {
            Content = JsonBody(new CreateUserRequest(
                ouId,
                userType,
                new Dictionary<string, string>
                {
                    ["email"] = email,
                    ["given_name"] = givenName,
                    ["family_name"] = familyName,
                    ["password"] = password,
                })),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"ThunderID user creation failed ({(int)response.StatusCode}): {body}");
        }

        var user = JsonSerializer.Deserialize<ThunderIdUser>(body, JsonOptions);
        return user?.Id ?? throw new InvalidOperationException("ThunderID user creation response had no id.");
    }

    private async Task AssignRoleAsync(string accessToken, string roleId, string userId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/roles/{roleId}/assignments/add")
        {
            Content = JsonBody(new AssignmentsRequest([new Assignment("user", userId)])),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"ThunderID role assignment failed ({(int)response.StatusCode}): {body}");
        }
    }

    private string RequireConfig(string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Missing required configuration '{key}' — see doc/setup/ThunderID.md.")
            : value;
    }

    private static StringContent JsonBody<T>(T value) =>
        new(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
    private record ThunderIdUser(string Id);
    private record CreateUserRequest(string OuId, string Type, Dictionary<string, string> Attributes);
    private record AssignmentsRequest(IReadOnlyList<Assignment> Assignments);
    private record Assignment(string Type, string Id);
}
