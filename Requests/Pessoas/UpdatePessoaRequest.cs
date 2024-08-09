namespace Montreal.Requests.Pessoas
{
    public record UpdatePessoaRequest(string Nome, string Sobrenome, string CPF, DateTime DataNascimento, string Sexo, byte[] Foto);
}
