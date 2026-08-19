using HabitTracker.Application.Features.Auth;
using HabitTracker.Infrastructure.Auth;
using HabitTracker.Infrastructure.Persistence;
using HabitTracker.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FluentValidation;
using Asp.Versioning;


namespace HabitTracker.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IConfiguration _config;
        private readonly IValidator<RegisterRequest> _registerValidator;

        public AuthController(AppDbContext db, IPasswordHasher<User> passwordHasher, IJwtTokenGenerator tokenGenerator, IConfiguration config, IValidator<RegisterRequest> registerValidator)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
            _config = config;
            _registerValidator = registerValidator;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result != PasswordVerificationResult.Success)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }
            var (token, expiresAt) = _tokenGenerator.GenerateToken(user);
            return Ok(new
            {
                token,
                expiresAt
            });
        }

        [HttpPost ("logout")]
        public async Task<IActionResult> LogOut(RefreshRequest request)
        {
            var hash = RefreshTokenGenerator.Hash(request.RefreshToken);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (token is null)
            {
                return NotFound(new { message = "Refresh token not found." });
            }
            else { 
                token.IsRevoked = true;
                await _db.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
        {
            var incomingHash = RefreshTokenGenerator.Hash(request.RefreshToken);

            var storedToken = await _db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == incomingHash);

            if(storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            storedToken.IsRevoked = true;

            var response = await IssueTokenAsync(storedToken.User);
            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(RegisterRequest request)
        {
            if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            {
                return Conflict(new { message = "Email is already in use." });
            }

            var validationResult = await _registerValidator.ValidateAsync(request);

            if(!validationResult.IsValid)
            {
                return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok(new { message = "User registered successfully." });
        }

        private async Task<AuthResponse> IssueTokenAsync(User user)
        { 
            var tokenResult = _tokenGenerator.GenerateToken(user);
            var rawRefreshToken = RefreshTokenGenerator.GenerateRefreshToken();
            var refreshDays = double.Parse(_config["Jwt:RefreshTokenExpiryDays"]!);

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = RefreshTokenGenerator.Hash(rawRefreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            });

            await _db.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = tokenResult.token,
                AccessTokenExpiresAt = tokenResult.ExpiresAt,
                RefreshToken = rawRefreshToken
            };
        }
    }
}
