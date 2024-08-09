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
    public static class AuthController
    {
        public static void AddRouteAuth(this WebApplication app)
        {
            var rotaAuth = app.MapGroup(prefix: "login").WithTags("Autenticacao");

            rotaAuth.MapPost(pattern: "", handler: async (CheckLoginRequest login, AppDbContext context, CancellationToken ct) =>
            {
                var usuario = await context.Usuarios.SingleOrDefaultAsync(u => u.NomeUsuario == login.NomeUsuario && u.Senha == login.Senha, ct);

                if (usuario == null)
                    return Results.Conflict(error: "Usuário ou senha inválidos.");

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
                    Audience = "https://localhost:7223",
                    Issuer = "https://localhost:7223",
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return Results.Ok(new { token = tokenHandler.WriteToken(token) });
            });
        }
    }
}