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
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, JwtHelper jwt, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email.ToLower()))
            return ApiResponse<AuthResponse>.Fail("Email sa a deja itilize.");

        var user = new User
        {
            Nom = req.Nom.Trim(),
            Email = req.Email.ToLower().Trim(),
            Telefon = req.Telefon.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = "User"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return ApiResponse<AuthResponse>.Ok(
            await BuildAuthResponse(user),
            "Kont kreye avèk siksè!");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Email oswa modpas pa kòrèk.");

        if (!user.IsActive)
            return ApiResponse<AuthResponse>.Fail("Kont ou dezaktive. Kontakte admin.");

        return ApiResponse<AuthResponse>.Ok(
            await BuildAuthResponse(user),
            "Koneksyon reyisi!");
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

    public static UserDto MapUser(User u) => new()
    {
        Id = u.Id,
        Nom = u.Nom,
        Email = u.Email,
        Telefon = u.Telefon,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };
}