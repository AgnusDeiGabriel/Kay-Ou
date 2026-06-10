using Microsoft.EntityFrameworkCore;
using KayAyiti.Data;
using KayAyiti.DTOs;
using KayAyiti.Helpers;
using KayAyiti.Models;

namespace KayAyiti.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest req);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest req);
    Task<ApiResponse<AuthResponse>> RefreshAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<ApiResponse<bool>> CheckTokenAsync(string token);
    Task<ApiResponse<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest req);
    Task<AuthResponse> FindOrCreateSocialUserAsync(string provider, string providerId, string email, string name);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;

    public AuthService(AppDbContext db, JwtHelper jwt, IConfiguration config, IEmailService email)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
        _email = email;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email.ToLower()))
            return ApiResponse<AuthResponse>.Fail("Email sa a deja itilize.");

        var smtpConfigured = !string.IsNullOrEmpty(_config["Email:SmtpPass"]);
        var verificationToken = smtpConfigured ? Guid.NewGuid().ToString("N") : (string?)null;

        var user = new User
        {
            Nom           = req.Nom.Trim(),
            Email         = req.Email.ToLower().Trim(),
            Telefon       = req.Telefon.Trim(),
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role          = "User",
            EmailVerified = !smtpConfigured,
            EmailVerificationToken  = verificationToken,
            EmailVerificationExpiry = smtpConfigured ? DateTime.UtcNow.AddHours(24) : null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (smtpConfigured && verificationToken != null)
            await _email.SendVerificationEmailAsync(user.Email, user.Nom, verificationToken);

        var message = smtpConfigured
            ? "Kont kreye! Tcheke email ou pou verifye kont ou."
            : "Kont kreye avèk siksè!";

        return ApiResponse<AuthResponse>.Ok(
            await BuildAuthResponse(user),
            message);
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Email oswa modpas pa kòrèk.");

        if (!user.IsActive)
            return ApiResponse<AuthResponse>.Fail("Kont ou dezaktive. Kontakte admin.");

        if (!user.EmailVerified)
            return ApiResponse<AuthResponse>.Fail("Verifye email ou anvan ou konekte. Tcheke bwat email ou.");

        return ApiResponse<AuthResponse>.Ok(
            await BuildAuthResponse(user),
            "Koneksyon reyisi!");
    }

    public async Task<ApiResponse<bool>> CheckTokenAsync(string token)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        if (user is null)
            return ApiResponse<bool>.Fail("Lyen sa a pa valid oswa deja itilize.");
        if (user.EmailVerificationExpiry < DateTime.UtcNow)
            return ApiResponse<bool>.Fail("Lyen an ekspire. Kreye kont ou ankò.");
        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == req.Token);

        if (user is null)
            return ApiResponse<AuthResponse>.Fail("Lyen sa a pa valid oswa deja itilize.");

        if (user.EmailVerificationExpiry < DateTime.UtcNow)
            return ApiResponse<AuthResponse>.Fail("Lyen an ekspire. Kreye kont ou ankò.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Modpas ou pa kòrèk.");

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationExpiry = null;

        await _db.SaveChangesAsync();

        return ApiResponse<AuthResponse>.Ok(
            await BuildAuthResponse(user),
            "Email verifye! Ou konekte otomatikman.");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (token is null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            return ApiResponse<AuthResponse>.Fail("Refresh token pa valid.");

        // Revoke old token
        token.IsRevoked = true;

        // Issue new tokens
        var response = await BuildAuthResponse(token.User);
        await _db.SaveChangesAsync();

        return ApiResponse<AuthResponse>.Ok(response, "Token renouvle.");
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (token is null) return false;

        token.IsRevoked = true;
        await _db.SaveChangesAsync();
        return true;
    }

    // ─── Private ─────────────────────────────────────────────────────────────

    private async Task<AuthResponse> BuildAuthResponse(User user)
    {
        var rawRefreshToken = _jwt.GenerateRefreshToken();
        var days = int.Parse(_config["Jwt:RefreshTokenDays"] ?? "30");

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = rawRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(days)
        });

        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = _jwt.GenerateAccessToken(user),
            RefreshToken = rawRefreshToken,
            User = MapUser(user)
        };
    }

    public async Task<AuthResponse> FindOrCreateSocialUserAsync(
        string provider, string providerId, string email, string name)
    {
        // Chèche pa provider ID
        User? user = provider switch
        {
            "Google"   => await _db.Users.FirstOrDefaultAsync(u => u.GoogleId   == providerId),
            "Facebook" => await _db.Users.FirstOrDefaultAsync(u => u.FacebookId == providerId),
            "Apple"    => await _db.Users.FirstOrDefaultAsync(u => u.AppleId    == providerId),
            _          => null
        };

        // Si pa jwenn pa ID, chèche pa email
        user ??= await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());

        if (user is null)
        {
            user = new User
            {
                Nom           = name.Trim(),
                Email         = email.ToLower().Trim(),
                Telefon       = string.Empty,
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                Role          = "User",
                EmailVerified = true,
                IsActive      = true
            };
            _db.Users.Add(user);
        }
        else if (!user.EmailVerified)
        {
            user.EmailVerified = true;
        }

        // Asosye provider ID
        switch (provider)
        {
            case "Google":   user.GoogleId   = providerId; break;
            case "Facebook": user.FacebookId = providerId; break;
            case "Apple":    user.AppleId    = providerId; break;
        }

        await _db.SaveChangesAsync();
        return await BuildAuthResponse(user);
    }

    public static UserDto MapUser(User u) => new()
    {
        Id        = u.Id,
        Nom       = u.Nom,
        Email     = u.Email,
        Telefon   = u.Telefon,
        Role      = u.Role,
        IsActive  = u.IsActive,
        CreatedAt = u.CreatedAt,
        AvatarUrl = u.AvatarUrl
    };
}