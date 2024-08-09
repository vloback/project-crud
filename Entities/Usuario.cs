namespace Montreal.Entities
{
    public class Usuario
    {
        public Guid Id { get; init; }
        public string NomeUsuario { get; private set; }
        public string Senha { get; private set; }
        public string Role { get; private set; }

        private Usuario() { }

        public Usuario(string nomeUsuario, string senha, string role)
        {
            Id = Guid.NewGuid();
            NomeUsuario = nomeUsuario;
            Senha = senha;
            Role = role;
        }

        public void AtualizarUsuario(string nomeUsuario, string senha, string role)
        {
            NomeUsuario = nomeUsuario;
            Senha = senha;
            Role = role;
        }
    }
}
