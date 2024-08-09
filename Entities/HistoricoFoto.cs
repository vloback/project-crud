namespace Montreal.Entities
{
    public class HistoricoFoto
    {
        public Guid Id { get; set; }
        public Guid PessoaId { get; private set; }
        public byte[] Foto { get; private set; }
        public DateTime DataAlteracao { get; private set; }
        public bool Principal { get; private set; }
        
        public Pessoa Pessoa { get; private set; }

        private HistoricoFoto() { }

        public HistoricoFoto(Guid pessoaId, byte[] foto, DateTime dataAlteracao, bool principal)
        {
            Id = Guid.NewGuid();
            PessoaId = pessoaId;
            Foto = foto;
            DataAlteracao = dataAlteracao;
            Principal = principal;
        }

        public void DesativaUltimaFoto()
        {
            DataAlteracao = DateTime.Now;
            Principal = false;
        }
    }
}
