using FluentValidation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Montreal.Data;
using Montreal.Entities;
using Montreal.Entities.Dto;
using Montreal.Requests.Pessoas;
using Montreal.Requests.Usuarios;

namespace Montreal.Controllers
{
    public static class UsuariosController
    {
        public static void AddRoutesUsuarios(this WebApplication app)
        {
            var rotasUsuarios = app.MapGroup(prefix: "usuarios").WithTags("Usuarios");

            //Adiciona um novo usuario
            
            rotasUsuarios.MapPost(pattern: "", handler: async (AddUsuarioRequest request, AppDbContext context, CancellationToken ct, [FromServices] IValidator<Usuario> validator) =>
            {
                var jaExiste = await context.Usuarios.AnyAsync(usuario => usuario.NomeUsuario == request.NomeUsuario, ct);

                if (jaExiste)
                    return Results.Conflict(error: "Esse usuário já existe!");

                var novoUsuario = new Usuario(request.NomeUsuario, request.Senha, request.Role);

                var result = validator.Validate(novoUsuario);

                var error = result.Errors.Select(e => e.ErrorMessage);

                if (!result.IsValid)
                {
                    return Results.Conflict(error);
                }

                await context.Usuarios.AddAsync(novoUsuario, ct);
                await context.SaveChangesAsync(ct);

                return Results.Ok("Usuário adicionado com sucesso!");
            }).RequireAuthorization("GerentePolicy");

            // Busca todos usuários
            rotasUsuarios.MapGet(pattern: "", handler: async (AppDbContext context, CancellationToken ct) =>
            {
                var usuarios = await context.Usuarios
                .Select(usuario => new UsuarioDto(usuario.Id, usuario.NomeUsuario, usuario.Role))
                .ToListAsync(ct);
                return usuarios;
            }).RequireAuthorization("UsarioPolicy");

            // Atualiza um usuário
            rotasUsuarios.MapPut(pattern: "{id}", handler: async (Guid id, UpdateUsuarioRequest request, AppDbContext context, CancellationToken ct) =>
            {
                var usuario = await context.Usuarios.SingleOrDefaultAsync(usuario => usuario.Id == id, ct);

                if (usuario == null)
                    return Results.NotFound();

                usuario.AtualizarUsuario(request.NomeUsuario, request.Senha, request.Role);

                await context.SaveChangesAsync(ct);
                return Results.Ok("Usuário atualizado com sucesso!");
            }).RequireAuthorization("GerentePolicy");

            // Remove um usuário
            rotasUsuarios.MapDelete(pattern: "{id}", handler: async (Guid id, AppDbContext context, CancellationToken ct) =>
            {
                var usuario = await context.Usuarios.SingleOrDefaultAsync(usuario => usuario.Id == id, ct);

                if (usuario == null)
                    return Results.NotFound();

                context.Remove(usuario);

                await context.SaveChangesAsync(ct);
                return Results.Ok("Usuário removido com sucesso!");
            }).RequireAuthorization("GerentePolicy");
        }
    }
}
