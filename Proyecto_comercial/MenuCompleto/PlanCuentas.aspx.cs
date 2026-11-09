using System;
using System.Linq;
using CooperativaCarchi.Modelos;
using CooperativaCarchi.Servicios;
namespace CooperativaCarchi
{
    public partial class PlanCuentas : PaginaBase
    {
        private readonly PlanCuentasService servicio = new PlanCuentasService();
        private int Seleccion { get { return (int?)ViewState["Seleccion"] ?? 0; } set { ViewState["Seleccion"] = value; } }
        private int EmpresaId { get { return string.IsNullOrEmpty(EmpresaLista.SelectedValue) ? 0 : Entero(EmpresaLista.SelectedValue); } }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                EmpresaLista.DataSource = new EmpresaService().Seleccionar(Actor); EmpresaLista.DataTextField = "RazonSocial"; EmpresaLista.DataValueField = "Id"; EmpresaLista.DataBind();
                Limpiar(sender, e); Cargar();
            }
        }
        protected override void OnPreRender(EventArgs e)
        { Guardar.Enabled = Actualizar.Enabled = Eliminar.Enabled = EsAdmin && EmpresaId > 0; base.OnPreRender(e); }
        private void Cargar()
        {
            Tabla.DataSource = EmpresaId == 0 ? null : servicio.Seleccionar(Actor, EmpresaId, Buscar.Text, Entero(FiltroGrupo.SelectedValue)); Tabla.DataBind();
        }
        private CuentaContable Leer(bool nueva)
        {
            return new CuentaContable { Id = nueva ? 0 : Seleccion, IdEmpresa = EmpresaId, Codigo = Codigo.Text.Trim(), Nombre = NombreCuenta.Text.Trim(), Grupo = Entero(Grupo.SelectedValue),
                CuentaPadre = Padre.Text.Trim(), GrupoExcel = GrupoExcel.SelectedValue, Naturaleza = Naturaleza.SelectedValue, Tipo = Tipo.SelectedValue,
                DebeInicial = Importe(Debe.Text), HaberInicial = Importe(Haber.Text), Activa = Activo.Checked };
        }
        protected void GuardarRegistro(object sender, EventArgs e)
        { Ejecutar(Mensaje, delegate { servicio.Guardar(Actor, Leer(true)); Limpiar(sender, e); Cargar(); }); }
        protected void ActualizarRegistro(object sender, EventArgs e)
        { Ejecutar(Mensaje, delegate { if (Seleccion == 0) throw new InvalidOperationException("Seleccione una cuenta."); servicio.Guardar(Actor, Leer(false)); Cargar(); }); }
        protected void EliminarRegistro(object sender, EventArgs e)
        { Ejecutar(Mensaje, delegate { servicio.Eliminar(Actor, Seleccion); Limpiar(sender, e); Cargar(); }); }
        protected void SeleccionarRegistro(object sender, EventArgs e)
        {
            Ejecutar(Mensaje, delegate
            {
                int id = Convert.ToInt32(Tabla.SelectedDataKey.Value); var c = servicio.Seleccionar(Actor, EmpresaId).FirstOrDefault(x => x.Id == id);
                if (c == null) throw new InvalidOperationException("La cuenta ya no existe.");
                Seleccion = c.Id; IdRegistro.Text = c.Id.ToString(); Codigo.Text = c.Codigo; NombreCuenta.Text = c.Nombre; Grupo.SelectedValue = c.Grupo.ToString();
                Padre.Text = c.CuentaPadre; Nivel.Text = c.Nivel.ToString(); GrupoExcel.SelectedValue = c.GrupoExcel ?? ""; Naturaleza.SelectedValue = c.Naturaleza; Tipo.SelectedValue = c.Tipo;
                Debe.Text = Numero(c.DebeInicial); Haber.Text = Numero(c.HaberInicial); Activo.Checked = c.Activa;
            });
        }
        protected void Limpiar(object sender, EventArgs e)
        { Seleccion = 0; IdRegistro.Text = Codigo.Text = NombreCuenta.Text = Padre.Text = Nivel.Text = ""; Debe.Text = Haber.Text = "0"; Grupo.SelectedValue = "1"; GrupoExcel.SelectedValue = ""; Naturaleza.SelectedValue = "Deudora"; Tipo.SelectedValue = "Grupo"; Activo.Checked = true; Mensaje.Text = ""; }
        protected void BuscarRegistros(object sender, EventArgs e) { Ejecutar(Mensaje, Cargar); }
        protected void CambiarEmpresa(object sender, EventArgs e) { Ejecutar(Mensaje, delegate { Limpiar(sender, e); Cargar(); }); }
    }
}
