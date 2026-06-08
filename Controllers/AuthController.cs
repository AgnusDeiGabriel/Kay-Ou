using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KayAyiti.DTOs;
using KayAyiti.Services;
using System.Security.Claims;
using System.Text.Json;

namespace KayAyiti.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Kreye kont nouvo itilizatè</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var result = await _auth.RegisterAsync(req);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Login epi resevwa JWT token</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _auth.LoginAsync(req);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Renouvle access token avèk refresh token</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req)
    {
        var result = await _auth.RefreshAsync(req.RefreshToken);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Tcheke si token valid (pou paj verify.html la)</summary>
    [HttpGet("verify-email")]
    public async Task<IActionResult> CheckToken([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(ApiResponse<bool>.Fail("Token obligatwa."));
        var result = await _auth.CheckTokenAsync(token);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Verifye email avèk token + modpas</summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest req)
    {
        var result = await _auth.VerifyEmailAsync(req);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Dekonekte (revoke refresh token)</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest req)
    {
        await _auth.RevokeTokenAsync(req.RefreshToken);
        return Ok(ApiResponse<bool>.Ok(true, "Dekoneksyon reyisi."));
    }

    // ─── Config piblik (Client IDs pou frontend) ────────────────────────────

    [HttpGet("config")]
    [AllowAnonymous]
    public IActionResult GetConfig([FromServices] IConfiguration cfg) =>
        Ok(ApiResponse<object>.Ok(new
        {
            googleClientId = cfg["OAuth:Google:ClientId"] ?? ""
        }));

    /// <summary>Verifye Google ID token (Google Identity Services popup)</summary>
    [HttpPost("google-token")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleToken([FromBody] GoogleTokenRequest req)
    {
        try
        {
            var clientId = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["OAuth:Google:ClientId"];

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(req.Credential, settings);

            var auth = await _auth.FindOrCreateSocialUserAsync(
                "Google", payload.Subject, payload.Email, payload.Name ?? payload.Email);

            return Ok(ApiResponse<AuthResponse>.Ok(auth, "Koneksyon Google reyisi!"));
        }
        catch (InvalidJwtException)
        {
            return BadRequest(ApiResponse<bool>.Fail("Token Google pa valid."));
        }
    }

    // ─── OAuth Social Login ──────────────────────────────────────────────────

    private static readonly HashSet<string> _providers =
        new(StringComparer.OrdinalIgnoreCase) { "Google", "Facebook", "Apple" };

    /// <summary>Retounen lis providers ki aktif</summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    public IActionResult GetProviders([FromServices] IAuthenticationSchemeProvider schemes)
    {
        var active = _providers
            .Where(p => schemes.GetSchemeAsync(p).Result != null)
            .Select(p => p.ToLower())
            .ToList();
        return Ok(ApiResponse<List<string>>.Ok(active));
    }

    /// <summary>Kòmanse OAuth flow pou yon provider (Google/Facebook/Apple)</summary>
    [HttpGet("signin/{provider}")]
    [AllowAnonymous]
    public IActionResult SignIn(string provider,
        [FromServices] IAuthenticationSchemeProvider schemes)
    {
        if (!_providers.Contains(provider))
            return BadRequest("Provider pa valid.");

        if (schemes.GetSchemeAsync(provider).Result == null)
            return BadRequest($"{provider} pa konfigire ankò.");

        var props = new AuthenticationProperties
        {
            RedirectUri = $"/api/auth/callback/{provider}"
        };
        return Challenge(props, provider);
    }

    /// <summary>Callback OAuth — jwenn itilizatè, voye JWT, redirije sou frontend</summary>
    [HttpGet("callback/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthCallback(string provider)
    {
        var result = await HttpContext.AuthenticateAsync("External");
        if (!result.Succeeded)
            return Redirect("/?oauth=error");

        var claims     = result.Principal!.Claims.ToList();
        var email      = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name       = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var providerId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerId))
            return Redirect("/?oauth=error&reason=no_email");

        await HttpContext.SignOutAsync("External");

        var auth        = await _auth.FindOrCreateSocialUserAsync(provider, providerId, email, name ?? email);
        var userJson    = Uri.EscapeDataString(JsonSerializer.Serialize(auth.User));
        var tokenEnc    = Uri.EscapeDataString(auth.AccessToken);
        var refreshEnc  = Uri.EscapeDataString(auth.RefreshToken);

        return Redirect($"/?ka_token={tokenEnc}&ka_refresh={refreshEnc}&ka_user={userJson}");
    }

    /// <summary>Wè pwofil itilizatè konekte a</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var nom = User.FindFirst(ClaimTypes.Name)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(ApiResponse<object>.Ok(new { userId, email, nom, role }));
    }
}