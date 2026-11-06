using System;
using System.Linq;
using System.Web.UI.WebControls;
using CooperativaCarchi.Servicios;
namespace CooperativaCarchi
{
    public abstract class LibroContable : PaginaBase
    {
        protected Label Mensaje, Resumen;
        protected DropDownList EmpresaLista, CuentaLista;
        protected TextBox Desde, Hasta;
        protected Button Consultar;
        protected GridView Tabla;
        protected abstract bool EsMayor { get; }
        private int EmpresaId { get { return string.IsNullOrEmpty(EmpresaLista.SelectedValue) ? 0 : Entero(EmpresaLista.SelectedValue); } }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) Ejecutar(Mensaje, () => {
                EmpresaLista.DataSource = new EmpresaService().Seleccionar(Actor); EmpresaLista.DataTextField = "RazonSocial"; EmpresaLista.DataValueField = "Id"; EmpresaLista.DataBind();
                Desde.Text = Dia(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)); Hasta.Text = Dia(DateTime.Today); CargarCuentas();
            });
            CuentaLista.Enabled = EsMayor;
        }
        private void CargarCuentas()
        {
            CuentaLista.Items.Clear();
            if (EmpresaId > 0) foreach (var c in new PlanCuentasService().Seleccionar(Actor, EmpresaId).Where(x => x.Tipo == "Movimiento")) CuentaLista.Items.Add(new ListItem(c.Codigo + " - " + c.Nombre, c.Id.ToString()));
        }
        protected void CambiarEmpresa(object sender, EventArgs e) { Ejecutar(Mensaje, () => { CargarCuentas(); Tabla.DataSource = null; Tabla.DataBind(); Resumen.Text = ""; }); }
        protected void ConsultarLibro(object sender, EventArgs e)
        {
            Ejecutar(Mensaje, () => {
                var servicio = new TransaccionService();
                var filas = EsMayor ? servicio.Mayor(Actor, EmpresaId, Entero(CuentaLista.SelectedValue), Fecha(Desde.Text), Fecha(Hasta.Text)) : servicio.Diario(Actor, EmpresaId, Fecha(Desde.Text), Fecha(Hasta.Text));
                Tabla.DataSource = filas; Tabla.DataBind();
                Resumen.Text = "Debe: " + Numero(filas.Sum(x => x.Debe)) + " | Haber: " + Numero(filas.Sum(x => x.Haber)) + (EsMayor ? " | Saldo final: " + Numero(filas.Last().Saldo) : " | Diferencia: " + Numero(filas.Sum(x => x.Debe - x.Haber)));
            });
        }
    }
}
