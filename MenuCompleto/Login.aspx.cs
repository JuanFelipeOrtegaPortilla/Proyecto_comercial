using System;
using CooperativaCarchi.Servicios;
namespace CooperativaCarchi
{
    public partial class Login : PaginaBase
    {
        protected override bool Publica { get { return true; } }
        protected void IniciarSesion(object sender, EventArgs e)
        {
            Ejecutar(Mensaje, delegate
            {
                var hasta = Session["BloqueadoHasta"] as DateTime?;
                if (hasta.HasValue && hasta.Value > DateTime.UtcNow) throw new InvalidOperationException("Demasiados intentos. Espere un minuto.");
                var usuario = new AutenticacionService().IniciarSesion(UsuarioNombre.Text, Clave.Text);
                if (usuario == null)
                {
                    int fallos = (Session["Fallos"] as int? ?? 0) + 1; Session["Fallos"] = fallos;
                    if (fallos >= 5) { Session["BloqueadoHasta"] = DateTime.UtcNow.AddMinutes(1); Session["Fallos"] = 0; }
                    throw new InvalidOperationException("Usuario o contraseña incorrectos.");
                }
                Session.Clear(); Session["UsuarioId"] = usuario.Id;
                Session["Csrf"] = Guid.NewGuid().ToString("N");
                Response.Redirect("Inicio.aspx", true);
            });
        }
    }
}
