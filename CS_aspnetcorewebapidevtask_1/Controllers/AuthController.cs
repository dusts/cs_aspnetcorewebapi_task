using CS_aspnetcorewebapidevtask_1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CS_aspnetcorewebapidevtask_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid registration details." });
            }

            var user = new User { UserName = model.Username };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User"); // Default to "User" role
                return Ok(new { Message = "User registered successfully." });
            }

            return BadRequest(new { Message = "Registration failed.", Errors = result.Errors.Select(e => e.Description) });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid login details." });
            }

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid username or password." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { Message = "Invalid username or password." });
            }

            // Generate JWT token
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            // Add roles to claims
            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("X7kPq9mW3zT2rY6nL8vJ5tF4hB2dC9xA")); // Match Program.cs
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "me",
                audience: "CS_checkers",
                claims: claims,
                expires: DateTime.Now.AddHours(1), // Token expiration
                signingCredentials: creds
            );

            return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }
}
