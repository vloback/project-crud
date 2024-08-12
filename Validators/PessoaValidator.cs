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
                .Must(CpfValido)
                .WithMessage("Por favor, informar um CPF válido.");

            RuleFor(c => c.DataNascimento)
                .NotEmpty()
                .Must(dataNascimento => dataNascimento > new DateOnly(1900, 1, 1) && dataNascimento < DateOnly.FromDateTime(DateTime.Now))
                .WithMessage("Por favor, informar uma data válida.");

            RuleFor(c => c.Sexo)
                .NotEmpty()
                .Must(sexo => sexo == "Masculino" ||  sexo == "Feminino" || sexo == "Outro")
                .WithMessage("As opções válidas são: Masculino, Feminino, Outro.");

            RuleFor(p => p.Foto)
                .Must(foto => foto == null || foto.Length == 0 || foto.Length <= 1 * 1024 * 1024)
                .WithMessage("O tamanho da foto não pode ser maior que 1MB.");
        }

        private bool CpfValido(string cpf)
        {
            // Remove caracteres não numéricos
            cpf = new string(cpf.Where(char.IsDigit).ToArray());

            // Verifica se todos os dígitos são iguais
            if (cpf.All(c => c == cpf[0]))
                return false;

            // Valida os dois dígitos verificadores
            var cpfTemp = cpf.Substring(0, 9);
            var soma = 0;

            for (var i = 0; i < 9; i++)
                soma += int.Parse(cpfTemp[i].ToString()) * (10 - i);

            var resto= soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            var digito = resto.ToString();
            cpfTemp += digito;
            soma = 0;

            for (var i = 0; i < 10; i++)
                soma += int.Parse(cpfTemp[i].ToString()) * (11 - i);

            resto = soma % 11;
            if (resto < 2)
                resto = 0;
            else
                resto = 11 - resto;

            digito += resto.ToString();

            return cpf.EndsWith(digito);
        }
    }
}
