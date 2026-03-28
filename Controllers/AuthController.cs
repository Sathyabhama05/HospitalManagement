using Microsoft.AspNetCore.Mvc;
using HospitalManagement.DTOs.Auth;
using HospitalManagement.Services;

namespace HospitalManagement.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger      = logger;
        }

        // ------------------------------------------
        // POST /api/auth/register
        // ------------------------------------------
        /// <summary>Register a new user (Patient by default)</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _authService.RegisterAsync(dto);
                return Ok(result);
            }
            catch (Exception ex) when (ex.Message.Contains("already registered"))
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // ------------------------------------------
        // POST /api/auth/login
        // ------------------------------------------
        /// <summary>Login and receive JWT access + refresh token</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // ------------------------------------------
        // POST /api/auth/refresh-token
        // ------------------------------------------
        /// <summary>Get a new access token using a refresh token</summary>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}