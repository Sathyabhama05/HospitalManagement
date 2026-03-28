using System.Data;
using System.Data.SqlClient;
using HospitalManagement.Models;
using Microsoft.Data.SqlClient;
namespace HospitalManagement.Repositories
{
    // INTERFACE
    public interface IAuthRepository
    {
        Task<int> RegisterUserAsync(string fullName, string email, string phone, string passwordHash, int roleId);
        Task<User> GetUserByEmailAsync(string email);
        Task SaveRefreshTokenAsync(int userId, string token, DateTime expiresAt);
        Task<(User user, RefreshToken refreshToken)> GetRefreshTokenAsync(string token);
    }

    // IMPLEMENTATION (ADO.NET only)
  
    public class AuthRepository : IAuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // ------------------------------------------
        // Register a new user
        // ------------------------------------------
        public async Task<int> RegisterUserAsync(string fullName, string email, string phone, string passwordHash, int roleId)
{
    using var connection = new SqlConnection(_connectionString);
    using var command = new SqlCommand("sp_RegisterUser", connection)
    {
        CommandType = CommandType.StoredProcedure
    };

    command.Parameters.AddWithValue("@FullName", fullName);
    command.Parameters.AddWithValue("@Email", email);
    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
    command.Parameters.AddWithValue("@Phone", (object)phone ?? DBNull.Value);

    // ← These two OUTPUT params are critical
    var userIdParam = new SqlParameter("@UserId", SqlDbType.Int)
    {
        Direction = ParameterDirection.Output
    };
    var messageParam = new SqlParameter("@Message", SqlDbType.NVarChar, 200)
    {
        Direction = ParameterDirection.Output
    };

    command.Parameters.Add(userIdParam);
    command.Parameters.Add(messageParam);

    await connection.OpenAsync();
    await command.ExecuteNonQueryAsync();

    var userId = (int)userIdParam.Value;

    if (userId == -1)
        throw new Exception(messageParam.Value.ToString());

    return userId;
}
        // Get user by email (used for login)
 
        public async Task<User> GetUserByEmailAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_LoginUser", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Email", email);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new User
            {
                UserId       = reader.GetInt32(reader.GetOrdinal("UserId")),
                FullName     = reader.GetString(reader.GetOrdinal("FullName")),
                Email        = reader.GetString(reader.GetOrdinal("Email")),
                Phone        = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                IsActive     = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                RoleId       = reader.GetInt32(reader.GetOrdinal("RoleId")),
                RoleName     = reader.GetString(reader.GetOrdinal("RoleName"))
            };
        }

        // Save refresh token after login

        public async Task SaveRefreshTokenAsync(int userId, string token, DateTime expiresAt)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_SaveRefreshToken", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Token", token);
            command.Parameters.AddWithValue("@ExpiresAt", expiresAt);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        // Validate refresh token
      
        public async Task<(User user, RefreshToken refreshToken)> GetRefreshTokenAsync(string token)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetRefreshToken", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Token", token);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return (null, null);

            var refreshToken = new RefreshToken
            {
                TokenId   = reader.GetInt32(reader.GetOrdinal("TokenId")),
                UserId    = reader.GetInt32(reader.GetOrdinal("UserId")),
                Token     = reader.GetString(reader.GetOrdinal("Token")),
                ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
                IsRevoked = reader.GetBoolean(reader.GetOrdinal("IsRevoked"))
            };

            var user = new User
            {
                UserId   = reader.GetInt32(reader.GetOrdinal("UserId")),
                Email    = reader.GetString(reader.GetOrdinal("Email")),
                RoleId   = reader.GetInt32(reader.GetOrdinal("RoleId")),
                RoleName = reader.GetString(reader.GetOrdinal("RoleName"))
            };

            return (user, refreshToken);
        }
    }
}