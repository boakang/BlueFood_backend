using Microsoft.AspNetCore.Mvc;
using BlueFood_Api.Models;
using System.Data.SqlClient;
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

            var hash = HashPassword(req.Password);
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM scm.Users WHERE Username = @Username", conn);
                checkCmd.Parameters.AddWithValue("@Username", req.Username);
                int exists = (int)checkCmd.ExecuteScalar();
                if (exists > 0)
                    return Conflict("Username already exists");

                var cmd = new SqlCommand("INSERT INTO scm.Users (Username, PasswordHash, Email) VALUES (@Username, @PasswordHash, @Email)", conn);
                cmd.Parameters.AddWithValue("@Username", req.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", hash);
                cmd.Parameters.AddWithValue("@Email", req.Email);
                cmd.ExecuteNonQuery();
            }
            return Ok(new { message = "Register success" });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Missing fields");

            string hash = HashPassword(req.Password);
            int userId = 0;
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT UserId FROM scm.Users WHERE Username = @Username AND PasswordHash = @PasswordHash", conn);
                cmd.Parameters.AddWithValue("@Username", req.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", hash);
                var result = cmd.ExecuteScalar();
                if (result == null)
                    return Unauthorized("Invalid username or password");
                userId = (int)result;
            }
            // Trả về JWT token và username
            return Ok(new { token = userId.ToString(), username = req.Username });
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
    }
}
