using System;
using System.Web.UI;
using CooperativaCarchi.Servicios;
namespace CooperativaCarchi {
 public partial class Sitio : MasterPage {
 protected bool EsAdministrador;
 protected string ModoAlmacenamiento { get { return CooperativaCarchi.Datos.DatosTemporales.UsaPostgres ? "PostgreSQL | almacenamiento persistente" : "Demostración en memoria | los datos se pierden al reiniciar"; } }

 protected void Page_Load(object sender, EventArgs e) {
 var id = Session["UsuarioId"]; var u = id is int ? new UsuarioService().ConsultarSesion((int)id) : null;
 Identidad.Text = u == null ? "Acceso público" : u.Nombres + " " + u.Apellidos + " | " + u.Rol;
 EsAdministrador = u != null && u.Rol == CooperativaCarchi.Modelos.Rol.Administrador; Salir.Visible = u != null;
 }
 protected void CerrarSesion(object sender, EventArgs e) { Session.Clear(); Session.Abandon(); Response.Redirect("Login.aspx", true); }
 }
}