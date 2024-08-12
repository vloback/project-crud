namespace Montreal.Requests.Pessoas
{
    public record AddPessoaRequest(string Nome, string Sobrenome, string CPF, DateOnly DataNascimento, string Sexo, byte[] Foto);
}
