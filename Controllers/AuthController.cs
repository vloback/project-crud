using FluentValidation;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Montreal.Data;
using Montreal.Entities;
using Montreal.Requests.Login;
using Montreal.Requests.Usuarios;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Montreal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Logar(CheckLoginRequest login , CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.SingleOrDefaultAsync(u => u.NomeUsuario == login.NomeUsuario && u.Senha == login.Senha, ct);

            if (usuario == null)
                return Conflict(error: "Usuário ou senha inválidos.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("MySuperSecretKeyForJwt12345!@#$%");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                        new Claim(ClaimTypes.Name, usuario.NomeUsuario),
                        new Claim(ClaimTypes.Role, usuario.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Audience = "https://localhost:7223/",
                Issuer = "https://localhost:7223/",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { token = tokenHandler.WriteToken(token) });
        }
    }
}
