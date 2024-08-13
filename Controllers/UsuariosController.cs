using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Montreal.Data;
using Montreal.Entities;
using Montreal.Entities.Dto;
using Montreal.Migrations;
using Montreal.Requests.Pessoas;
using Montreal.Requests.Usuarios;
using System.ComponentModel.DataAnnotations;
using Usuario = Montreal.Entities.Usuario;

namespace Montreal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IValidator<Usuario> _validator;

        public UsuariosController(AppDbContext context, IValidator<Usuario> validator)
        {
            _context = context;
            _validator = validator;
        }

        [HttpGet]
        [Authorize(Policy = "UsuarioPolicy")]
        public async Task<IActionResult> BuscaTodosUsuarios(CancellationToken ct = default)
        {
            var usuarios = await _context.Usuarios
               .Select(usuario => new UsuarioDto(usuario.Id, usuario.NomeUsuario, usuario.Role))
               .ToListAsync(ct);
            return Ok(usuarios);
        }

        [HttpPost]
        [Authorize(Policy = "GerentePolicy")]
        public async Task<IActionResult> AdicionaUsuario(AddUsuarioRequest request , CancellationToken ct = default)
        {
            var jaExiste = await _context.Usuarios.AnyAsync(usuario => usuario.NomeUsuario == request.NomeUsuario, ct);

            if (jaExiste)
                return Conflict(error: "Esse usuário já existe!");

            var novoUsuario = new Usuario(request.NomeUsuario, request.Senha, request.Role);

            var result = _validator.Validate(novoUsuario);

            var error = result.Errors.Select(e => e.ErrorMessage);

            if (!result.IsValid)
            {
                return Conflict(error);
            }

            await _context.Usuarios.AddAsync(novoUsuario, ct);
            await _context.SaveChangesAsync(ct);

            return Ok("Usuário adicionado com sucesso!");
        }

        [HttpPut("{usuarioId}")]
        [Authorize(Policy = "GerentePolicy")]
        public async Task<IActionResult> AlteraUsuario(Guid usuarioId, UpdateUsuarioRequest request, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.SingleOrDefaultAsync(usuario => usuario.Id == usuarioId, ct);

            if (usuario == null)
                return NotFound();

            usuario.AtualizarUsuario(request.NomeUsuario, request.Senha, request.Role);

            var result = _validator.Validate(usuario);

            var error = result.Errors.Select(e => e.ErrorMessage);

            if (!result.IsValid)
            {
                return Conflict(error);
            }

            await _context.SaveChangesAsync(ct);
            return Ok("Usuário atualizado com sucesso!");
        }

        [HttpDelete("{usuarioId}")]
        [Authorize(Policy = "GerentePolicy")]
        public async Task<IActionResult> RemoveUsuario(Guid usuarioId, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios.SingleOrDefaultAsync(usuario => usuario.Id == usuarioId, ct);

            if (usuario == null)
                return NotFound();

            _context.Remove(usuario);

            await _context.SaveChangesAsync(ct);
            return Ok("Usuário removido com sucesso!");
        }
    }
}
