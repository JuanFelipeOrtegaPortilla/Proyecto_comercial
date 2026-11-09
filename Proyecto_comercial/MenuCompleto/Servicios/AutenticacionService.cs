using CooperativaCarchi.Modelos;
namespace CooperativaCarchi.Servicios
{
    public class AutenticacionService
    {
        public Usuario IniciarSesion(string usuario, string clave) { return new UsuarioService().Autenticar(usuario, clave); }
    }
}
