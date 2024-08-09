namespace Montreal.Requests.Pessoas
{
    public record AddPessoaRequest(string Nome, string Sobrenome, string CPF, DateTime DataNascimento, string Sexo, byte[] Foto);
}
