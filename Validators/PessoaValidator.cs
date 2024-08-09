using FluentValidation;
using Montreal.Entities;

namespace Montreal.Validators
{
    public class PessoaValidator : AbstractValidator<Pessoa>
    {
        public PessoaValidator()
        {
            RuleFor(c => c.Nome)
                .NotEmpty()
                .Length(3, 20)
                .WithMessage("Nome deve ter pelo menos 3 caracteres e máximo 20.");

            RuleFor(c => c.Sobrenome)
                .NotEmpty()
                .Length(3, 100)
                .WithMessage("Sobrenome deve ter pelo menos 3 caracteres e máximo 100.");

            RuleFor(c => c.CPF)
                .NotEmpty()
                .Length(11)
                .WithMessage("Por favor, informar um CPF válido.");

            RuleFor(c => c.DataNascimento)
                .NotEmpty()
                .Must(dataNascimento => dataNascimento > new DateTime(1900, 1, 1) && dataNascimento < DateTime.Now)
                .WithMessage("Por favor, informar uma data válida.");

            RuleFor(c => c.Sexo)
                .NotEmpty()
                .Must(sexo => sexo == "Masculino" ||  sexo == "Feminino" || sexo == "Outro")
                .WithMessage("As opções válidas são: Masculino, Feminino, Outro.");

            RuleFor(p => p.Foto)
                .Must(foto => foto == null || foto.Length == 0 || foto.Length <= 1 * 1024 * 1024)
                .WithMessage("O tamanho da foto não pode ser maior que 1MB.");
        }
    }
}
