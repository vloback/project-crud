using FluentValidation;
using Montreal.Entities;

namespace Montreal.Validators
{
    public class UsuarioValidator : AbstractValidator<Usuario>
    {
        public UsuarioValidator()
        {
            RuleFor(c => c.NomeUsuario)
                .NotEmpty()
                .Must(nomeUsuario => nomeUsuario.Length >= 5)
                .WithMessage("Usuário deve ter pelo menos 5 caracteres.");

            RuleFor(c => c.Role)
                .NotEmpty()
                .Must(role => role == "Usuario" || role == "Gerente")
                .WithMessage("As opções válidas para Role são: Usuario, Gerente.");

            RuleFor(c => c.Senha)
                .NotEmpty()
                .Length(8, 12)
                .Matches(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,12}$")
                .WithMessage("Senha deve possuir entre 8 e 12 caracteres e ter ao menos uma letra e um número");
        }
    }
}
