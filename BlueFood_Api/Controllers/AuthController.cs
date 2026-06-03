using Microsoft.AspNetCore.Mvc;
using BlueFood_Api.Models;
using System.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace BlueFood_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public AuthController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("BlueFoodDb");
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("Missing fields");

            var role = NormalizeRole(req.Role);
            var requiresApproval = RequiresApproval(role);
            var status = requiresApproval ? "Pending" : "Active";
            var activationToken = requiresApproval ? Guid.NewGuid().ToString("N") : null;
            var hash = HashPassword(req.Password);
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM scm.Users WHERE Username = @Username", conn);
                checkCmd.Parameters.AddWithValue("@Username", req.Username);
                int exists = (int)checkCmd.ExecuteScalar();
                if (exists > 0)
                    return Conflict("Username already exists");

                EnsureUserApprovalColumns(conn);

                var cmd = new SqlCommand(@"INSERT INTO scm.Users (Username, PasswordHash, Email, Role, Status, ActivationToken, CreatedAt)
                                           VALUES (@Username, @PasswordHash, @Email, @Role, @Status, @ActivationToken, SYSDATETIME())", conn);
                cmd.Parameters.AddWithValue("@Username", req.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", hash);
                cmd.Parameters.AddWithValue("@Email", req.Email);
                cmd.Parameters.AddWithValue("@Role", role);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.Add("@ActivationToken", SqlDbType.NVarChar, 100).Value = (object?)activationToken ?? DBNull.Value;
                cmd.ExecuteNonQuery();
            }

            if (requiresApproval)
            {
                return Ok(new
                {
                    message = "Tài khoản đã được tạo ở trạng thái Pending, chờ admin duyệt.",
                    status,
                    role,
                    activationToken,
                    activationUrl = $"http://localhost:5173/dashboard/admin-users"
                });
            }

            return Ok(new
            {
                message = "Register success",
                status,
                role
            });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Missing fields");

            string hash = HashPassword(req.Password);
            int userId = 0;
            string? status = null;
            string? role = null;
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                EnsureUserApprovalColumns(conn);

                var cmd = new SqlCommand("SELECT UserId, Status, Role FROM scm.Users WHERE Username = @Username AND PasswordHash = @PasswordHash", conn);
                cmd.Parameters.AddWithValue("@Username", req.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", hash);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return Unauthorized("Invalid username or password");
                userId = reader.GetInt32(0);
                status = reader.IsDBNull(1) ? null : reader.GetString(1);
                role = reader.IsDBNull(2) ? null : reader.GetString(2);
            }
            if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                return StatusCode(403, "Tài khoản đang chờ admin duyệt");

            // Trả về JWT token và username
            return Ok(new { token = userId.ToString(), username = req.Username, role, status });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logout success" });
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private static string NormalizeRole(string? role)
        {
            var normalized = (role ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(normalized) ? "User" : normalized;
        }

        private static bool RequiresApproval(string role)
        {
            return !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureUserApprovalColumns(SqlConnection conn)
        {
            var migrationCmd = new SqlCommand(@"
IF COL_LENGTH('scm.Users', 'Role') IS NULL
    ALTER TABLE scm.Users ADD Role nvarchar(50) NULL;

IF COL_LENGTH('scm.Users', 'Status') IS NULL
    ALTER TABLE scm.Users ADD Status nvarchar(20) NOT NULL CONSTRAINT DF_Users_Status DEFAULT('Active');

IF COL_LENGTH('scm.Users', 'ActivationToken') IS NULL
    ALTER TABLE scm.Users ADD ActivationToken nvarchar(100) NULL;

IF COL_LENGTH('scm.Users', 'CreatedAt') IS NULL
    ALTER TABLE scm.Users ADD CreatedAt datetime2(3) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME();

EXEC(N'UPDATE scm.Users
      SET Status = ISNULL(NULLIF(Status, ''''), ''Active'')
      WHERE Status IS NULL OR LTRIM(RTRIM(Status)) = '''';');

EXEC(N'UPDATE scm.Users
      SET Role = ISNULL(NULLIF(Role, ''''), ''User'')
      WHERE Role IS NULL OR LTRIM(RTRIM(Role)) = '''';');
", conn);
            migrationCmd.ExecuteNonQuery();
        }
    }
}
