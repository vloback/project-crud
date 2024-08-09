namespace Montreal.Requests.Usuarios
{
    public record UpdateUsuarioRequest(string NomeUsuario, string Senha, string Role);
}
