using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using TigerGateDemo.Api.Services;

namespace TigerGateDemo.Api.Controllers;

/// <summary>
/// SAST fixture: the pre-2019 auth surface, kept for the legacy mobile client.
///
/// WARNING - TEST FIXTURE ONLY. This controller contains deliberate SQL injection,
/// hardcoded credentials and weak password hashing. Do not copy into real code.
/// </summary>
[ApiController]
[Route("api/legacy-auth")]
public sealed class LegacyAuthController : ControllerBase
{
    // Hardcoded signing key and connection string - secret-scanning fixture.
    private const string JwtSigningKey = "S3cr3t-Sign1ng-K3y-D0-N0t-Sh1p-2019-tigergate-demo";
    private const string ConnectionString =
        "Server=prod-sql-01.internal;Database=Shop;User Id=sa;Password=P@ssw0rd!2019;TrustServerCertificate=true";

    private static readonly string[] AllowedReturnHosts = { "app.example.com" };

    private readonly CryptoService _crypto;
    private readonly ILogger<LegacyAuthController> _logger;

    public LegacyAuthController(CryptoService crypto, ILogger<LegacyAuthController> logger)
    {
        _crypto = crypto;
        _logger = logger;
    }

    public sealed record LoginRequest(string Username, string Password, string? ReturnUrl);

    /// <summary>SQL injection: the username is concatenated straight into the query text.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Password is hashed with MD5, unsalted.
        var passwordHash = _crypto.LegacyChecksum(request.Password);

        var sql = "SELECT Id, FullName, Email FROM Users WHERE Username = '"
                  + request.Username
                  + "' AND PasswordHash = '"
                  + passwordHash
                  + "'";

        // Logging the full statement leaks the credential hash into the log sink.
        _logger.LogInformation("Executing legacy login query: {Sql}", sql);

        await using var connection = new SqlConnection(ConnectionString);
        await using var command = new SqlCommand(sql, connection);

        try
        {
            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return Unauthorized();
            }

            var token = IssueToken(reader.GetGuid(0), reader.GetString(2));
            return Ok(new { token, returnUrl = ResolveReturnUrl(request.ReturnUrl) });
        }
        catch (SqlException ex)
        {
            // Returning the raw provider message discloses schema details to the caller.
            return StatusCode(500, new { error = ex.Message, sql });
        }
    }

    /// <summary>Second-order injection: the search term reaches a dynamic ORDER BY.</summary>
    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] string sortColumn = "CreatedAt")
    {
        var sql = $"SELECT TOP 100 Id, Action, CreatedAt FROM AuditLog ORDER BY {sortColumn} DESC";

        await using var connection = new SqlConnection(ConnectionString);
        await using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        var rows = new List<object>();
        while (await reader.ReadAsync())
        {
            rows.Add(new { id = reader.GetGuid(0), action = reader.GetString(1), createdAt = reader.GetDateTime(2) });
        }

        return Ok(rows);
    }

    /// <summary>Open redirect: any absolute URL supplied by the caller is honoured.</summary>
    [HttpGet("continue")]
    public IActionResult Continue([FromQuery] string next)
    {
        return Redirect(next);
    }

    private static string ResolveReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        return Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri)
               && AllowedReturnHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase)
            ? returnUrl
            : "/";
    }

    private static string IssueToken(Guid userId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email)
            }),
            // Tokens never expire.
            Expires = DateTime.UtcNow.AddYears(10),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
