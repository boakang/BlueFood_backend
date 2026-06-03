using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace BlueFood_Api.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public AdminUsersController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("BlueFoodDb");
        }

        [HttpGet("pending")]
        public IActionResult GetPendingUsers()
        {
            var authError = EnsureAdminAccess();
            if (authError is not null) return authError;

            var rows = new List<object>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"SELECT UserId, Username, Email, Role, Status, CreatedAt
                                       FROM scm.Users
                                       WHERE Status = 'Pending'
                                       ORDER BY CreatedAt DESC, UserId DESC", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new
                {
                    userId = reader.GetInt32(0),
                    username = reader.GetString(1),
                    email = reader.IsDBNull(2) ? null : reader.GetString(2),
                    role = reader.IsDBNull(3) ? null : reader.GetString(3),
                    status = reader.GetString(4),
                    createdAt = reader.GetDateTime(5)
                });
            }

            return Ok(rows);
        }

        [HttpPost("{username}/approve")]
        public IActionResult ApproveUser(string username)
        {
            var authError = EnsureAdminAccess();
            if (authError is not null) return authError;

            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Username is required");

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"UPDATE scm.Users
                                       SET Status = 'Active', ActivationToken = NULL
                                       WHERE Username = @Username AND Status = 'Pending'", conn);
            cmd.Parameters.AddWithValue("@Username", username);
            var affected = cmd.ExecuteNonQuery();

            if (affected == 0)
                return NotFound("Pending user not found");

            return Ok(new { message = "Account approved" });
        }

        private IActionResult? EnsureAdminAccess()
        {
            if (!Request.Headers.TryGetValue("X-BlueFood-UserId", out var userIdValue) || !int.TryParse(userIdValue.ToString(), out var userId))
            {
                return Unauthorized("Missing user token");
            }

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT Role FROM scm.Users WHERE UserId = @UserId", conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            var role = cmd.ExecuteScalar()?.ToString();

            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return null;
        }
    }
}