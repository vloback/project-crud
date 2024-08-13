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
using Microsoft.AspNetCore.Http;
using SQLitePCL;
using Microsoft.AspNetCore.Authorization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Linq;

namespace Montreal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PessoasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IValidator<Pessoa> _validator;

        public PessoasController(AppDbContext context, IValidator<Pessoa> validator)
        {
            _context = context;
            _validator = validator;
        }

        [HttpGet]
        [Authorize(Policy = "UsuarioPolicy")]
        public async Task<IActionResult> BuscaTodasPessoas(CancellationToken ct = default)
        {
            var pessoas = await _context.Pessoas
                .Select(pessoa => new PessoaDto(pessoa.Id, pessoa.Nome, pessoa.Sobrenome, pessoa.CPF, pessoa.DataNascimento, pessoa.Sexo))
                .ToListAsync(ct);

            if (pessoas.Any())
            {
                return Ok(pessoas);
            }
            else
            {
                return NotFound("Não há pessoas registradas na base!");
            }
        }

        [HttpGet("BuscaPessoasPor")]
        [Authorize(Policy = "UsuarioPolicy")]
        public async Task<IActionResult> BuscaPessoasPorFiltros(string? nome, string? cpf, DateTime? dataNascimento, string? sexo, int pageNumber = 1, int pageSize = 10,
            CancellationToken ct = default)
        {
            var pessoas = await _context.Pessoas
                .Where(p => (string.IsNullOrEmpty(nome) || p.Nome.Contains(nome)) &&
                            (string.IsNullOrEmpty(cpf) || p.CPF == cpf) &&
                            (!dataNascimento.HasValue || p.DataNascimento == DateOnly.FromDateTime(dataNascimento.Value)) &&
                            (string.IsNullOrEmpty(sexo) || p.Sexo == sexo))
                .Select(pessoa => new PessoaDto(pessoa.Id, pessoa.Nome, pessoa.Sobrenome, pessoa.CPF, pessoa.DataNascimento, pessoa.Sexo))
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            if (pessoas.Any())
            {
                return Ok(pessoas);
            }
            else
            {
                return NotFound("Não há nenhuma pessoa com os filtros informados!");
            }
        }

        [HttpGet("BuscaFotoMaisRecente/{pessoaId}")]
        [Authorize(Policy = "UsuarioPolicy")]
        public async Task<IActionResult> BuscaFotoMaisRecente(Guid pessoaId, CancellationToken ct = default)
        {
            var fotoMaisRecente = await _context.HistoricoFoto.FirstOrDefaultAsync(foto => foto.PessoaId == pessoaId && foto.Principal, ct);

            if (fotoMaisRecente != null)
            {
                var retornaFoto = new HistoricoFotoDto(fotoMaisRecente.Foto);
                return Ok(retornaFoto);
            }
            else
            {
                return NotFound("Essa pessoa não possui uma foto registrada!");
            }
        }

        [HttpPost]
        [Authorize(Policy = "GerentePolicy")]
        public async Task<IActionResult> AdicionaPessoa(AddPessoaRequest request, CancellationToken ct = default)
        {
            var cpf = request.CPF;

            // Remove non-numeric caracteres
            cpf = new string(cpf.Where(char.IsDigit).ToArray());

            var jaExiste = await _context.Pessoas.AnyAsync(pessoa => pessoa.CPF == cpf, ct);

            if (jaExiste)
                return Conflict(error: "Essa pessoa já existe!");

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
            var result = _validator.Validate(novaPessoa);
            var error = result.Errors.Select(e => e.ErrorMessage);
            if (!result.IsValid)
            {
                return Conflict(error);
            }
            await _context.Pessoas.AddAsync(novaPessoa, ct);

            if (jpgBytes != null)
            {
                var fotoPrincipal = new HistoricoFoto(novaPessoa.Id, jpgBytes, DateTime.Now, true);
                await _context.HistoricoFoto.AddAsync(fotoPrincipal, ct);
            }

            await _context.SaveChangesAsync(ct);

            return Ok("Pessoa adicionada com sucesso!");
        }

        [HttpPut("{pessoaId}")]
        [Authorize(Policy = "GerentePolicy")]
        public async Task<IActionResult> AlteraPessoa(Guid pessoaId, UpdatePessoaRequest request, CancellationToken ct = default)
        {
            var cpf = request.CPF;

            // Remove non-numeric caracteres
            cpf = new string(cpf.Where(char.IsDigit).ToArray());

            var pessoa = await _context.Pessoas.SingleOrDefaultAsync(pessoa => pessoa.Id == pessoaId, ct);
            HistoricoFoto novaFotoPrincipal = null;

            if (pessoa == null)
                return NotFound();

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

                var ultimaFoto = await _context.HistoricoFoto.SingleOrDefaultAsync(ultimaFoto => ultimaFoto.Principal && ultimaFoto.PessoaId == pessoa.Id, ct);

                if (ultimaFoto != null)
                {
                    ultimaFoto.DesativaUltimaFoto();
                }

                novaFotoPrincipal = new HistoricoFoto(pessoa.Id, jpgBytes, DateTime.Now, true);
            }

            // Valida atualização da pessoa
            var result = _validator.Validate(pessoa);
            var error = result.Errors.Select(e => e.ErrorMessage);
            if (!result.IsValid)
            {
                return Conflict(error);
            }

            if (novaFotoPrincipal != null)
            {
                await _context.HistoricoFoto.AddAsync(novaFotoPrincipal, ct);
            }

            await _context.SaveChangesAsync(ct);
            return Ok("Pessoa atualizada com sucesso!");
        }

        [HttpDelete("{pessoaId}")]
        [Authorize(Policy = "GerentePolicy")]
        public async Task<IActionResult> RemovePessoa(Guid pessoaId, CancellationToken ct = default)
        {
            var pessoa = await _context.Pessoas.SingleOrDefaultAsync(pessoa => pessoa.Id == pessoaId, ct);

            if (pessoa == null)
                return NotFound("Usuário não encontrado!");

            var pessoaFotos = await _context.HistoricoFoto.Where(foto => foto.PessoaId == pessoaId).ToListAsync(ct);

            _context.HistoricoFoto.RemoveRange(pessoaFotos);
            _context.Remove(pessoa);

            await _context.SaveChangesAsync(ct);

            return Ok("Pessoa removida com sucesso!");
        }
    }
}
