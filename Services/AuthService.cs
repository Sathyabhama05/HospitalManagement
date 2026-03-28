using HospitalManagement.DTOs.Auth;
using HospitalManagement.Entities;
using HospitalManagement.Repositories;
using HospitalManagement.Helpers;

namespace HospitalManagement.Services
{
    public interface IAuthService
    {
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    }

    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepo;
        private readonly JwtHelper _jwtHelper;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAuthRepository authRepo,
            JwtHelper jwtHelper,
            IConfiguration config,
            ILogger<AuthService> logger)
        {
            _authRepo  = authRepo;
            _jwtHelper = jwtHelper;
            _config    = config;
            _logger    = logger;
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var newUserId = await _authRepo.RegisterUserAsync(
                dto.FullName,
                dto.Email,
                dto.Phone,
                passwordHash,
                dto.RoleId
            );

            _logger.LogInformation("New user registered: {Email} | Role: {RoleId}", dto.Email, dto.RoleId);

            var roleMap = new Dictionary<int, string> { { 1, "Admin" }, { 2, "Doctor" }, { 3, "Patient" } };
            roleMap.TryGetValue(dto.RoleId, out var roleName);

            return new RegisterResponseDto
            {
                UserId   = newUserId,
                FullName = dto.FullName,
                Email    = dto.Email,
                Role     = roleName ?? "Patient",
                Message  = "Registration successful."
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _authRepo.GetUserByEmailAsync(dto.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid email or password.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated. Contact admin.");

            var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!passwordValid)
                throw new UnauthorizedAccessException("Invalid email or password.");

            var accessToken  = _jwtHelper.GenerateAccessToken(user);
            var refreshToken = _jwtHelper.GenerateRefreshToken();

            var refreshExpiry = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"]);
            await _authRepo.SaveRefreshTokenAsync(user.UserId, refreshToken, DateTime.UtcNow.AddDays(refreshExpiry));

            _logger.LogInformation("User logged in: {Email}", dto.Email);

            return new AuthResponseDto
            {
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn    = _jwtHelper.GetAccessTokenExpirySeconds(),
                User = new UserInfoDto
                {
                    UserId   = user.UserId,
                    FullName = user.FullName,
                    Email    = user.Email,
                    Role     = user.RoleName
                }
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var (user, token) = await _authRepo.GetRefreshTokenAsync(refreshToken);

            if (user == null || token == null)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            if (token.ExpiresAt < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token has expired. Please login again.");

            var newAccessToken  = _jwtHelper.GenerateAccessToken(user);
            var newRefreshToken = _jwtHelper.GenerateRefreshToken();

            var refreshExpiry = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"]);
            await _authRepo.SaveRefreshTokenAsync(user.UserId, newRefreshToken, DateTime.UtcNow.AddDays(refreshExpiry));

            return new AuthResponseDto
            {
                AccessToken  = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn    = _jwtHelper.GetAccessTokenExpirySeconds(),
                User = new UserInfoDto
                {
                    UserId = user.UserId,
                    Email  = user.Email,
                    Role   = user.RoleName
                }
            };
        }
    }
}