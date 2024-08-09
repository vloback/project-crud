namespace Montreal.Requests.Usuarios
{
    public record AddUsuarioRequest(string NomeUsuario, string Senha, string Role);
}
