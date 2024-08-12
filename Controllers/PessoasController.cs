using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Montreal.Data;
using Montreal.Entities;
using Montreal.Requests.Pessoas;
using Montreal.Validators;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Drawing.Imaging;
using SixLabors.ImageSharp;
using System.IO;
using System.ComponentModel.DataAnnotations;
using Montreal.Entities.Dto;
using System.Drawing.Printing;

namespace Montreal.Controllers
{
    public static class PessoasController
    {
        public static void AddRoutesPessoas(this WebApplication app)
        {
            var rotasPessoas = app.MapGroup(prefix: "pessoas").WithTags("Pessoas");

            // Adiciona uma nova pessoa
            rotasPessoas.MapPost(pattern: "", handler: async (AddPessoaRequest request, AppDbContext context, CancellationToken ct, [FromServices] IValidator<Pessoa> validator) =>
            {
                var cpf = request.CPF;

                // Remove non-numeric caracteres
                cpf = new string(cpf.Where(char.IsDigit).ToArray());

                var jaExiste = await context.Pessoas.AnyAsync(pessoa => pessoa.CPF == cpf, ct);

                if (jaExiste)
                    return Results.Conflict(error: "Essa pessoa já existe!");

                // Conversão de imagem para .jpg
                byte[] jpgBytes = null;

                if (request.Foto != null && request.Foto.Length > 0)
                {
                    using (var ms = new MemoryStream(request.Foto))
                    {
                        using (var image = System.Drawing.Image.FromStream(ms))
                        {
                            using (var jpgStream = new MemoryStream())
                            {
                                image.Save(jpgStream, ImageFormat.Jpeg);
                                jpgBytes = jpgStream.ToArray();
                            }
                        }
                    }
                }

                var novaPessoa = new Pessoa(request.Nome, request.Sobrenome, cpf, request.DataNascimento, request.Sexo, jpgBytes);

                // Valida nova pessoa
                var result = validator.Validate(novaPessoa);
                var error = result.Errors.Select(e => e.ErrorMessage);
                if (!result.IsValid)
                {
                    return Results.Conflict(error);
                }
                await context.Pessoas.AddAsync(novaPessoa, ct);

                if (jpgBytes != null)
                {
                    var fotoPrincipal = new HistoricoFoto(novaPessoa.Id, jpgBytes, DateTime.Now, true);
                    await context.HistoricoFoto.AddAsync(fotoPrincipal, ct);
                }

                await context.SaveChangesAsync(ct);

                return Results.Ok("Pessoa adicionada com sucesso!");
            }).RequireAuthorization("GerentePolicy");

            // Busca todas as pessoas
            rotasPessoas.MapGet(pattern: "", handler: async (AppDbContext context, CancellationToken ct) =>
            {
                var pessoas = await context.Pessoas
                .Select(pessoa => new PessoaDto(pessoa.Id, pessoa.Nome, pessoa.Sobrenome, pessoa.CPF, pessoa.DataNascimento, pessoa.Sexo))
                .ToListAsync(ct);
                
                if (pessoas.Any())
                {
                    return Results.Ok(pessoas);
                }
                else
                {
                    return Results.NotFound("Não há pessoas registradas na base!");
                }
            }).RequireAuthorization("UsuarioPolicy");

            // Busca pessoa pelos campos Nome, CPF, Data de Nascimento e Sexo
            rotasPessoas.MapGet(pattern: "{Nome, CPF, Data de Nascimento, Sexo, pageNumber, pageSize}",
                handler: async (string? nome, string? cpf, DateTime? dataNascimento, string? Sexo, int pageNumber, int pageSize, AppDbContext context, CancellationToken ct) =>
            {
                var pessoas = await context.Pessoas
                .Where(p => (string.IsNullOrEmpty(nome) || p.Nome.Contains(nome)) &&
                            (string.IsNullOrEmpty(cpf) || p.CPF == cpf) &&
                            (!dataNascimento.HasValue || p.DataNascimento == DateOnly.FromDateTime(dataNascimento.Value)) &&
                            (string.IsNullOrEmpty(Sexo) || p.Sexo == Sexo))
                .Select(pessoa => new PessoaDto(pessoa.Id, pessoa.Nome, pessoa.Sobrenome, pessoa.CPF, pessoa.DataNascimento, pessoa.Sexo))
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

                if (pessoas.Any())
                {
                    return Results.Ok(pessoas);
                } else {
                    return Results.NotFound("Não há nenhuma pessoa com os filtros informados!");
                }
            }).RequireAuthorization("UsuarioPolicy");

            // Busca a foto mais recente da pessoa
            rotasPessoas.MapGet(pattern: "fotoMaisRecente/{pessoaId}", handler: async (Guid pessoaId, AppDbContext context, CancellationToken ct) =>
                {
                    var fotoMaisRecente = await context.HistoricoFoto.FirstOrDefaultAsync(foto => foto.PessoaId == pessoaId && foto.Principal, ct);

                    if (fotoMaisRecente != null)
                    {
                        var retornaFoto = new HistoricoFotoDto(fotoMaisRecente.Foto);
                        return Results.Ok(retornaFoto);
                    }
                    else {
                        return Results.NotFound("Essa pessoa não possui uma foto registrada!");
                    }
                }).RequireAuthorization("UsuarioPolicy");

            // Atualiza uma pessoa
            rotasPessoas.MapPut(pattern: "{id}", handler: async (Guid id, UpdatePessoaRequest request, AppDbContext context, CancellationToken ct, [FromServices] IValidator<Pessoa> validator) =>
            {
                var cpf = request.CPF;

                // Remove non-numeric caracteres
                cpf = new string(cpf.Where(char.IsDigit).ToArray());

                var pessoa = await context.Pessoas.SingleOrDefaultAsync(pessoa => pessoa.Id == id, ct);
                HistoricoFoto novaFotoPrincipal = null;

                if (pessoa == null)
                    return Results.NotFound();

                pessoa.AtualizarPessoa(request.Nome, request.Sobrenome, cpf, request.DataNascimento, request.Sexo);

                // Verifica se há uma nova foto para atualizar
                if (request.Foto != null && request.Foto.Length > 0)
                {
                    // Conversão de imagem para .jpg
                    byte[] jpgBytes;

                    using (var ms = new MemoryStream(request.Foto))
                    {
                        using (var image = System.Drawing.Image.FromStream(ms))
                        {
                            using (var jpgStream = new MemoryStream())
                            {
                                image.Save(jpgStream, ImageFormat.Jpeg);
                                jpgBytes = jpgStream.ToArray();
                            }
                        }
                    }

                    pessoa.AtualizarFoto(jpgBytes);

                    var ultimaFoto = await context.HistoricoFoto.SingleOrDefaultAsync(ultimaFoto => ultimaFoto.Principal && ultimaFoto.PessoaId == pessoa.Id, ct);

                    if (ultimaFoto != null)
                    {
                        ultimaFoto.DesativaUltimaFoto();
                    }

                    novaFotoPrincipal = new HistoricoFoto(pessoa.Id, jpgBytes, DateTime.Now, true);
                }

                // Valida atualização da pessoa
                var result = validator.Validate(pessoa);
                var error = result.Errors.Select(e => e.ErrorMessage);
                if (!result.IsValid)
                {
                    return Results.Conflict(error);
                }

                if (novaFotoPrincipal != null)
                {
                    await context.HistoricoFoto.AddAsync(novaFotoPrincipal, ct);
                }

                await context.SaveChangesAsync(ct);
                return Results.Ok("Pessoa atualizada com sucesso!");
            }).RequireAuthorization("GerentePolicy");

            // Remove uma pessoa
            rotasPessoas.MapDelete(pattern: "{id}", handler: async (Guid id, AppDbContext context, CancellationToken ct) =>
            {
                var pessoa = await context.Pessoas.SingleOrDefaultAsync(pessoa => pessoa.Id == id, ct);

                if (pessoa == null)
                    return Results.NotFound("Usuário não encontrado!");

                context.Remove(pessoa);

                await context.SaveChangesAsync(ct);
                return Results.Ok("Pessoa removida com sucesso!");
            }).RequireAuthorization("GerentePolicy");
        }
    }
}
