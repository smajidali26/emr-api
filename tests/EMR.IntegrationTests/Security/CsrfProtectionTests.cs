using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using EMR.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EMR.IntegrationTests.Security;

/// <summary>
/// Integration tests for CSRF protection middleware
/// SECURITY TEST: Task #2661 - Validates CSRF token validation for state-changing requests
/// </summary>
public class CsrfProtectionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CsrfProtectionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates a client with test authentication configured
    /// </summary>
    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Add test authentication scheme
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return client;
    }

    [Fact]
    public async Task GetRequest_WithoutCsrfToken_ShouldSucceed()
    {
        // Arrange
        var client = CreateAuthenticatedClient();

        // Act - GET requests don't require CSRF token
        var response = await client.GetAsync("/api/auth/me");

        // Assert - Should not fail due to CSRF (may fail for other reasons like auth)
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCsrfToken_ShouldReturnTokenAndSetCookie()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/csrf-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrEmpty();
        content.HeaderName.Should().Be("X-CSRF-Token");

        // Verify cookie was set
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().Contain(c => c.Contains("XSRF-TOKEN"));
    }

    [Fact]
    public async Task PostRequest_WithValidCsrfToken_ShouldSucceed()
    {
        // Arrange - Create a client with cookie handling
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // First, get a CSRF token (this sets the cookie)
        var csrfResponse = await client.GetAsync("/api/auth/csrf-token");
        var csrfContent = await csrfResponse.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        var csrfToken = csrfContent!.Token;

        // Extract the CSRF cookie from the response
        string? csrfCookie = null;
        if (csrfResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                if (cookie.StartsWith("XSRF-TOKEN="))
                {
                    // Extract just the cookie value without attributes
                    csrfCookie = cookie.Split(';')[0];
                    break;
                }
            }
        }

        // Create request with CSRF token header AND cookie
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/patients")
        {
            Content = new StringContent("{\"firstName\":\"Test\",\"lastName\":\"User\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-CSRF-Token", csrfToken);
        if (csrfCookie != null)
        {
            request.Headers.Add("Cookie", csrfCookie);
        }

        // Act
        var response = await client.SendAsync(request);

        // Assert - Should not fail due to CSRF validation
        // May fail for other reasons (validation, auth, etc.) but not 403 for CSRF
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            errorContent.Should().NotContain("CSRF validation failed",
                "Request with valid CSRF token should not fail CSRF validation");
        }
    }

    [Fact]
    public async Task PostRequest_WithoutCsrfToken_ForAuthenticatedUser_ShouldReturnForbidden()
    {
        // Arrange
        var client = CreateAuthenticatedClient();

        // Create request without CSRF token
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/patients")
        {
            Content = new StringContent("{\"firstName\":\"Test\",\"lastName\":\"User\"}", Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert - Should fail with 403 Forbidden due to missing CSRF token
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("CSRF");
    }

    [Fact]
    public async Task PostRequest_WithInvalidCsrfToken_ShouldReturnForbidden()
    {
        // Arrange
        var client = CreateAuthenticatedClient();

        // Create request with invalid CSRF token
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/patients")
        {
            Content = new StringContent("{\"firstName\":\"Test\",\"lastName\":\"User\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-CSRF-Token", "invalid-token-12345");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("CSRF");
    }

    [Fact]
    public async Task PutRequest_WithoutCsrfToken_ForAuthenticatedUser_ShouldReturnForbidden()
    {
        // Arrange
        var client = CreateAuthenticatedClient();

        // Create PUT request without CSRF token
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/patients/00000000-0000-0000-0000-000000000001")
        {
            Content = new StringContent("{\"firstName\":\"Updated\"}", Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatchRequest_WithoutCsrfToken_ForAuthenticatedUser_ShouldReturnForbidden()
    {
        // Arrange
        var client = CreateAuthenticatedClient();

        // Create PATCH request without CSRF token
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/patients/00000000-0000-0000-0000-000000000001")
        {
            Content = new StringContent("{\"firstName\":\"Updated\"}", Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteRequest_WithoutCsrfToken_ForAuthenticatedUser_ShouldReturnForbidden()
    {
        // Arrange
        var client = CreateAuthenticatedClient();

        // Create DELETE request without CSRF token
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/patients/00000000-0000-0000-0000-000000000001");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/refresh")]
    [InlineData("/api/auth/logout")]
    [InlineData("/health")]
    public async Task ExemptPaths_WithoutCsrfToken_ShouldNotReturnCsrfError(string path)
    {
        // Arrange
        var client = CreateAuthenticatedClient();

        // Create POST request without CSRF token to exempt path
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert - Should not fail with CSRF error (may fail for other reasons)
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("CSRF",
                $"Exempt path {path} should not require CSRF token");
        }
    }

    [Fact]
    public async Task PostRequest_FromUnauthenticatedUser_ShouldNotRequireCsrf()
    {
        // Arrange - Use client without authentication
        var client = _factory.CreateClient();

        // Create POST request without CSRF token (unauthenticated)
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = new StringContent(
                "{\"email\":\"test@example.com\",\"azureAdB2CId\":\"test-id\",\"roles\":[\"Patient\"]}",
                Encoding.UTF8,
                "application/json")
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert - Unauthenticated requests should not fail with CSRF error
        // (May fail with 401 Unauthorized or other errors, but not CSRF 403)
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("CSRF validation failed",
                "Unauthenticated requests should not require CSRF token");
        }
    }

    private record CsrfTokenResponse(string Token, string HeaderName);
}

/// <summary>
/// Test authentication handler for simulating authenticated users in tests
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Doctor")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
