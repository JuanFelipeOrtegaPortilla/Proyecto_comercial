using System;
using System.Linq;
using CooperativaCarchi.Modelos;
using CooperativaCarchi.Servicios;
namespace CooperativaCarchi
{
    public partial class Registro : PaginaBase
    {
        protected override bool Publica { get { return true; } }
        private readonly UsuarioService servicio = new UsuarioService();
        private int Seleccion { get { return (int?)ViewState["Seleccion"] ?? 0; } set { ViewState["Seleccion"] = value; } }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) { Limpiar(sender, e); Cargar(); }
        }
        protected override void OnPreRender(EventArgs e)
        {
            Tabla.Visible = Buscar.Visible = BuscarBoton.Visible = EsAdmin;
            RolUsuario.Enabled = Activo.Enabled = Actualizar.Visible = Eliminar.Visible = EsAdmin;
            if (!EsAdmin) RolUsuario.SelectedValue = servicio.RequiereAdministrador && Request.IsLocal ? "Administrador" : "Usuario";
            base.OnPreRender(e);
        }
        private void Cargar()
        {
            if (EsAdmin) { Tabla.DataSource = servicio.Seleccionar(Actor).Where(x => (x.NombreUsuario + " " + x.Nombres + " " + x.Apellidos).IndexOf(Buscar.Text, StringComparison.OrdinalIgnoreCase) >= 0).ToList(); Tabla.DataBind(); }
        }
        private Usuario Leer(bool nuevo)
        {
            if (Clave.Text != Confirmacion.Text) throw new InvalidOperationException("Las contraseñas no coinciden.");
            return new Usuario { Id = nuevo ? 0 : Seleccion, Nombres = Nombres.Text.Trim(), Apellidos = Apellidos.Text.Trim(),
                NombreUsuario = UsuarioNombre.Text.Trim(), Correo = Correo.Text.Trim(), Rol = (Rol)Enum.Parse(typeof(Rol), RolUsuario.SelectedValue), Activo = Activo.Checked };
        }
        protected void GuardarRegistro(object sender, EventArgs e)
        {
            Ejecutar(Mensaje, delegate
            {
                var u = Leer(true);
                if (EsAdmin) servicio.Guardar(Actor, u, Clave.Text);
                else if (servicio.RequiereAdministrador) servicio.CrearAdministradorInicial(u, Clave.Text, Request.IsLocal);
                else servicio.RegistrarPublico(u, Clave.Text);
                Limpiar(sender, e); Cargar();
            });
        }
        protected void ActualizarRegistro(object sender, EventArgs e)
        {
            Ejecutar(Mensaje, delegate { SoloAdmin(); if (Seleccion == 0) throw new InvalidOperationException("Seleccione un usuario."); servicio.Guardar(Actor, Leer(false), Clave.Text); Limpiar(sender, e); Cargar(); });
        }
        protected void EliminarRegistro(object sender, EventArgs e)
        { Ejecutar(Mensaje, delegate { servicio.Eliminar(Actor, Seleccion); Limpiar(sender, e); Cargar(); }); }
        protected void SeleccionarRegistro(object sender, EventArgs e)
        {
            Ejecutar(Mensaje, delegate
            {
                SoloAdmin(); int id = Convert.ToInt32(Tabla.SelectedDataKey.Value); var u = servicio.Seleccionar(Actor).FirstOrDefault(x => x.Id == id);
                if (u == null) throw new InvalidOperationException("El usuario ya no existe.");
                Seleccion = u.Id; IdRegistro.Text = u.Id.ToString(); Nombres.Text = u.Nombres; Apellidos.Text = u.Apellidos;
                UsuarioNombre.Text = u.NombreUsuario; Correo.Text = u.Correo; RolUsuario.SelectedValue = u.Rol.ToString(); Activo.Checked = u.Activo;
                Clave.Text = Confirmacion.Text = "";
            });
        }
        protected void Limpiar(object sender, EventArgs e)
        { Seleccion = 0; IdRegistro.Text = Nombres.Text = Apellidos.Text = UsuarioNombre.Text = Correo.Text = Clave.Text = Confirmacion.Text = ""; Activo.Checked = true; RolUsuario.SelectedValue = "Usuario"; Mensaje.Text = ""; }
        protected void BuscarRegistros(object sender, EventArgs e) { Ejecutar(Mensaje, Cargar); }
    }
}
