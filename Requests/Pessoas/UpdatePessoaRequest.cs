namespace Montreal.Requests.Pessoas
{
    public record UpdatePessoaRequest(string Nome, string Sobrenome, string CPF, DateOnly DataNascimento, string Sexo, byte[] Foto);
}
