using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Warehouse.Backend.Contracts;
using Warehouse.Backend.Data;

namespace Warehouse.Backend.Security;

public sealed class DemoIdentityService
{
    private readonly WarehouseAuthOptions _options;
    private readonly IDbContextFactory<WarehouseDbContext> _dbContextFactory;

    public DemoIdentityService(IConfiguration configuration, IDbContextFactory<WarehouseDbContext> dbContextFactory)
    {
        _options = configuration.GetSection(WarehouseAuthOptions.SectionName).Get<WarehouseAuthOptions>() ?? new WarehouseAuthOptions();
        _dbContextFactory = dbContextFactory;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(item => item.Username == request.Username, cancellationToken);

        if (user is null || !user.IsActive || !PasswordHasher.VerifyPassword(request.Password, user.PasswordSalt, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var refreshToken = CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            Username = user.Username,
            TokenHash = refreshToken.Hash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });

        await db.SaveChangesAsync(cancellationToken);
        return new LoginResponseDto(CreateAccessToken(user), refreshToken.RawToken, ToCurrentUser(user));
    }

    public async Task<LoginResponseDto?> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tokenHash = HashToken(request.RefreshToken);
        var token = await db.RefreshTokens
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= DateTimeOffset.UtcNow || token.User is null || !token.User.IsActive)
        {
            return null;
        }

        var user = token.User;
        var nextToken = CreateRefreshToken();
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.ReplacedByTokenHash = nextToken.Hash;

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            Username = user.Username,
            TokenHash = nextToken.Hash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });

        await db.SaveChangesAsync(cancellationToken);
        return new LoginResponseDto(CreateAccessToken(user), nextToken.RawToken, ToCurrentUser(user));
    }

    public async Task<CurrentUserDto?> ToCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var username = principal.FindFirstValue(ClaimTypes.Name) ?? principal.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(item => item.Username == username, cancellationToken);
        return user is null ? null : ToCurrentUser(user);
    }

    public async Task<IReadOnlyList<UserProfileDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users
            .OrderBy(item => item.FullName)
            .Select(item => new UserProfileDto(item.Username, item.FullName, item.Role, item.IsActive, item.LastLoginAt, item.CreatedAt, item.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserProfileDto?> GetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(item => item.Username == username, cancellationToken);
        return user is null ? null : ToProfile(user);
    }

    public async Task<UserProfileDto?> UpsertUserAsync(UpsertUserRequestDto request, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(item => item.Username == request.Username, cancellationToken);
        var isNew = user is null;

        if (user is null)
        {
            user = new AppUserEntity
            {
                Username = request.Username,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
        }

        user.FullName = request.FullName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var (salt, hash) = PasswordHasher.HashPassword(request.Password);
            user.PasswordSalt = salt;
            user.PasswordHash = hash;
        }
        else if (isNew)
        {
            var (salt, hash) = PasswordHasher.HashPassword("ChangeMe123!");
            user.PasswordSalt = salt;
            user.PasswordHash = hash;
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToProfile(user);
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(string username, UpdateProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(item => item.Username == username, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        user.FullName = request.FullName;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || !PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordSalt, user.PasswordHash))
            {
                return null;
            }

            var (salt, hash) = PasswordHasher.HashPassword(request.NewPassword);
            user.PasswordSalt = salt;
            user.PasswordHash = hash;
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToProfile(user);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users.AnyAsync(item => item.Username == username, cancellationToken);
    }

    private string CreateAccessToken(AppUserEntity user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new("full_name", user.FullName),
            new(ClaimTypes.Role, user.Role),
            new(ClaimTypes.NameIdentifier, user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static CurrentUserDto ToCurrentUser(AppUserEntity user) => new(user.Username, user.FullName, user.Role, user.IsActive, user.LastLoginAt);

    private static UserProfileDto ToProfile(AppUserEntity user) => new(user.Username, user.FullName, user.Role, user.IsActive, user.LastLoginAt, user.CreatedAt, user.UpdatedAt);

    private static (string RawToken, string Hash) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        var rawToken = Convert.ToBase64String(bytes);
        return (rawToken, HashToken(rawToken));
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }
}
