using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SakuraSushi_API.DataContext;
using SakuraSushi_API.Request;

namespace SakuraSushi_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
    {
        private SakuraSushiContext _context;
        private IConfiguration _configuration;

        public UserController (SakuraSushiContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("/api/Auth/SignIn")]
        public IActionResult SignIn([FromBody] LoginReq req)
        {
            var password = HashPassword(req.password);
            var query = _context.Users.FirstOrDefault(s => s.Username.Equals(req.username));

            if (query != null)
            {
                if (query.PasswordHash.Equals(password))
                {
                    return Ok(new { token = generateToken(query.Id), expiredAt = DateTime.Now.AddMinutes(60) });
                } else
                {
                    return BadRequest("Password is wrong!");
                }
            }

            return NotFound("User not found");
        }

        [Authorize]
        [HttpGet("/api/Auth/me")]
        public IActionResult profile()
        {
            var user = User.Claims.FirstOrDefault(s => s.Type == "user_id");
            if (user == null)
            {
                return Unauthorized();
            }

            var query = _context.Users.FirstOrDefault(s => s.Id.ToString() == user.Value);
            if (query != null)
            {
                return Ok(new
                {
                    fullname = query.FullName,
                    username = query.Username,
                    email = query.Email,
                    phoneNumber = query.PhoneNumber,
                    role = query.Role
                });
            }

            return Unauthorized();
        }
        string HashPassword(string password)
        {
            var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }

        private string generateToken(Guid userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: new[]
                {
                    new Claim("user_id", userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                },
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
