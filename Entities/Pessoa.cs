using Montreal.Entities;
using System.ComponentModel.DataAnnotations;

namespace Montreal.Entities
{
    public class Pessoa
    {
        public Guid Id { get; init; }
        public string Nome { get; private set; }
        public string Sobrenome { get; private set; }
        public string CPF { get; private set; }
        public DateOnly DataNascimento { get; private set; }
        public string Sexo { get; private set; }
        public byte[]? Foto { get; private set; }
        public ICollection<HistoricoFoto> Fotos { get; private set; } = new List<HistoricoFoto>();

        private Pessoa() { }

        public Pessoa(string nome, string sobrenome, string cpf, DateOnly dataNascimento, string sexo, byte[] foto)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            Sobrenome = sobrenome;
            CPF = cpf;
            DataNascimento = dataNascimento;
            Sexo = sexo;
            Foto = foto;
        }

        public void AtualizarPessoa(string nome, string sobrenome, string cpf, DateOnly dataNascimento, string sexo)
        {
            Nome = nome;
            Sobrenome = sobrenome;
            CPF = cpf;
            DataNascimento = dataNascimento;
            Sexo = sexo;
        }

        public void AtualizarFoto(byte[] foto)
        {
            Foto = foto;
        }
    }
}
