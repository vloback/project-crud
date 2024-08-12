namespace Montreal.Entities.Dto
{
    public record PessoaDto(Guid Id, string Nome, string Sobrenome, string CPF, DateOnly DataNascimento, string Sexo);
}
