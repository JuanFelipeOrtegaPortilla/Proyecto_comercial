using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using CooperativaCarchi.Modelos;
using CooperativaCarchi.Servicios;
namespace CooperativaCarchi
{
    public partial class Transacciones : PaginaBase
    {
        private readonly TransaccionService servicio = new TransaccionService();
        private int Seleccion { get { return (int?)ViewState["Transaccion"] ?? 0; } set { ViewState["Transaccion"] = value; } }
        private int EmpresaId { get { return string.IsNullOrEmpty(EmpresaLista.SelectedValue) ? 0 : Entero(EmpresaLista.SelectedValue); } }
        private List<LineaAsiento> Borrador { get { return (List<LineaAsiento>)ViewState["Lineas"] ?? new List<LineaAsiento>(); } set { ViewState["Lineas"] = value; } }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) Ejecutar(Mensaje, () => {
                EmpresaLista.DataSource = new EmpresaService().Seleccionar(Actor); EmpresaLista.DataTextField = "RazonSocial"; EmpresaLista.DataValueField = "Id"; EmpresaLista.DataBind();
                CargarCuentas(); Limpiar(sender, e); Cargar();
            });
        }
        protected override void OnPreRender(EventArgs e)
        {
            bool editar = EsAdmin && EmpresaId > 0;
            bool confirmada = (bool?)ViewState["Confirmada"] ?? false;
            Guardar.Enabled = editar && Seleccion == 0;
            Actualizar.Enabled = Eliminar.Enabled = Confirmar.Enabled = editar && Seleccion > 0 && !confirmada;
            Agregar.Enabled = editar && !confirmada;
            base.OnPreRender(e);
        }
        private List<CuentaContable> Cuentas() { return EmpresaId == 0 ? new List<CuentaContable>() : new PlanCuentasService().Seleccionar(Actor, EmpresaId).Where(x => x.Activa && x.Tipo == "Movimiento").ToList(); }
        private void CargarCuentas()
        {
            var cuentas = Cuentas().Select(x => new { Id = x.Id, Texto = x.Codigo + " - " + x.Nombre }).ToList();
            foreach (var lista in new[] { CuentaBase, CuentaIVA, Contrapartida, CuentaLinea })
            { lista.DataSource = cuentas; lista.DataTextField = "Texto"; lista.DataValueField = "Id"; lista.DataBind(); lista.Items.Insert(0, new ListItem("Seleccione una cuenta", "")); }
        }
        private void Cargar() { Tabla.DataSource = EmpresaId == 0 ? null : servicio.Seleccionar(Actor, EmpresaId); Tabla.DataBind(); MostrarLineas(); }
        private void MostrarLineas()
        {
            var cuentas = Cuentas(); Lineas.DataSource = Borrador.Select(l => new { Codigo = cuentas.Where(c => c.Id == l.IdCuenta).Select(c => c.Codigo).FirstOrDefault() ?? l.IdCuenta.ToString(), Nombre = cuentas.Where(c => c.Id == l.IdCuenta).Select(c => c.Nombre).FirstOrDefault() ?? "Cuenta no disponible", l.Debe, l.Haber }).ToList(); Lineas.DataBind();
        }
        protected void CambiarEmpresa(object sender, EventArgs e) { Ejecutar(Mensaje, () => { Limpiar(sender, e); CargarCuentas(); Cargar(); }); }
        protected void Limpiar(object sender, EventArgs e)
        {
            Seleccion = 0; ViewState["Confirmada"] = false; IdRegistro.Text = Glosa.Text = Resumen.Text = ""; FechaOperacion.Text = Dia(DateTime.Today); Subtotal.Text = Debe.Text = Haber.Text = "0";
            Tipo.SelectedValue = "Manual"; Tratamiento.SelectedValue = "Gravado"; Borrador = new List<LineaAsiento>();
            foreach (var lista in new[] { CuentaBase, CuentaIVA, Contrapartida, CuentaLinea }) if (lista.Items.Count > 0) lista.SelectedIndex = 0;
            MostrarLineas();
        }
        protected void AgregarLinea(object sender, EventArgs e)
        {
            Ejecutar(Mensaje, () => {
                SoloAdmin(); if (Tipo.SelectedValue != "Manual") throw new InvalidOperationException("Seleccione Manual para ingresar líneas. Compra y Venta las generan al guardar.");
                var lineas = Borrador; if (lineas.Count >= 100) throw new InvalidOperationException("Máximo 100 líneas.");
                decimal debe = Importe(Debe.Text), haber = Importe(Haber.Text); int cuenta = Entero(CuentaLinea.SelectedValue);
                if (!Cuentas().Any(x => x.Id == cuenta) || debe < 0 || haber < 0 || (debe > 0) == (haber > 0) || debe != TarifaIVAService.Redondear(debe) || haber != TarifaIVAService.Redondear(haber)) throw new InvalidOperationException("Seleccione cuenta y un importe positivo con dos decimales solo en Debe o Haber.");
                lineas.Add(new LineaAsiento { IdCuenta = cuenta, Debe = debe, Haber = haber }); Borrador = lineas; MostrarLineas(); Debe.Text = Haber.Text = "0";
            });
        }
        protected void QuitarLinea(object sender, GridViewDeleteEventArgs e)
        { Ejecutar(Mensaje, () => { SoloAdmin(); var filas = Borrador; if (e.RowIndex < 0 || e.RowIndex >= filas.Count) throw new InvalidOperationException("Seleccione una línea válida."); filas.RemoveAt(e.RowIndex); Borrador = filas; MostrarLineas(); }); }
        private Transaccion Leer(bool nueva)
        {
            bool manual = Tipo.SelectedValue == "Manual";
            return new Transaccion { Id = nueva ? 0 : Seleccion, IdEmpresa = EmpresaId, Fecha = Fecha(FechaOperacion.Text), Glosa = Glosa.Text.Trim(), Tipo = Tipo.SelectedValue, Tratamiento = Tratamiento.SelectedValue,
                Subtotal = manual ? 0 : Importe(Subtotal.Text), CuentaBase = manual ? 0 : Entero(CuentaBase.SelectedValue),
                CuentaIVA = string.IsNullOrEmpty(CuentaIVA.SelectedValue) ? (int?)null : Entero(CuentaIVA.SelectedValue), Contrapartida = manual ? 0 : Entero(Contrapartida.SelectedValue) };
        }
        private void Mostrar(Transaccion t)
        {
            Seleccion = t.Id; ViewState["Confirmada"] = t.Estado == "Confirmada"; IdRegistro.Text = t.Id.ToString(); FechaOperacion.Text = Dia(t.Fecha); Glosa.Text = t.Glosa; Tipo.SelectedValue = t.Tipo; Tratamiento.SelectedValue = t.Tratamiento; Subtotal.Text = Numero(t.Subtotal);
            Elegir(CuentaBase, t.CuentaBase); Elegir(CuentaIVA, t.CuentaIVA ?? 0); Elegir(Contrapartida, t.Contrapartida);
            Borrador = servicio.Detalle(Actor, t.Id); MostrarLineas();
            Resumen.Text = Server.HtmlEncode(t.Estado + " | IVA aplicado: " + Numero(t.PorcentajeAplicado) + "% | IVA: " + Numero(t.IVA) + " | Total: " + Numero(t.Total) + " | Debe: " + Numero(Borrador.Sum(x => x.Debe)) + " | Haber: " + Numero(Borrador.Sum(x => x.Haber)));
        }
        private static void Elegir(DropDownList lista, int id) { lista.SelectedIndex = 0; var item = lista.Items.FindByValue(id.ToString()); if (item != null) lista.SelectedValue = item.Value; }
        protected void SeleccionarRegistro(object sender, EventArgs e)
        { Ejecutar(Mensaje, () => { int id = Convert.ToInt32(Tabla.SelectedDataKey.Value); var t = servicio.Seleccionar(Actor, EmpresaId).FirstOrDefault(x => x.Id == id); if (t == null) throw new InvalidOperationException("La transacción ya no existe."); Mostrar(t); }); }
        private void GuardarOperacion(bool nueva)
        {
            if (!nueva && Seleccion == 0) throw new InvalidOperationException("Seleccione una transacción.");
            var t = Leer(nueva); servicio.Guardar(Actor, t, Borrador); Cargar(); Mostrar(servicio.Seleccionar(Actor, EmpresaId).Single(x => x.Id == t.Id));
        }
        protected void GuardarRegistro(object sender, EventArgs e) { Ejecutar(Mensaje, () => GuardarOperacion(true)); }
        protected void ActualizarRegistro(object sender, EventArgs e) { Ejecutar(Mensaje, () => GuardarOperacion(false)); }
        protected void EliminarRegistro(object sender, EventArgs e) { Ejecutar(Mensaje, () => { servicio.Eliminar(Actor, Seleccion); Limpiar(sender, e); Cargar(); }); }
        protected void ConfirmarRegistro(object sender, EventArgs e) { Ejecutar(Mensaje, () => { servicio.Confirmar(Actor, Seleccion); Cargar(); Mostrar(servicio.Seleccionar(Actor, EmpresaId).Single(x => x.Id == Seleccion)); }); }
    }
}
