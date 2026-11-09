using System;
using System.Linq;
using CooperativaCarchi.Modelos;
using CooperativaCarchi.Servicios;
using EmpresaModelo = CooperativaCarchi.Modelos.Empresa;
namespace CooperativaCarchi
{
    public partial class EmpresaPagina : PaginaBase
    {
        private readonly EmpresaService servicio = new EmpresaService();
        private readonly TarifaIVAService iva = new TarifaIVAService();
        private int Seleccion { get { return (int?)ViewState["Seleccion"] ?? 0; } set { ViewState["Seleccion"] = value; } }
        private int TarifaSeleccion { get { return (int?)ViewState["Tarifa"] ?? 0; } set { ViewState["Tarifa"] = value; } }
        protected void Page_Load(object sender, EventArgs e)
        { if (!IsPostBack) { Limpiar(sender, e); Cargar(); } }
        protected override void OnPreRender(EventArgs e)
        {
            Guardar.Enabled = Actualizar.Enabled = Eliminar.Enabled = EsAdmin;
            GuardarTarifa.Enabled = ActualizarTarifa.Enabled = EliminarTarifa.Enabled = EsAdmin && Seleccion > 0;
            base.OnPreRender(e);
        }
        private void Cargar() { Tabla.DataSource = servicio.Seleccionar(Actor, Buscar.Text); Tabla.DataBind(); CargarTarifas(); }
        private void CargarTarifas()
        {
            var empresa = servicio.Seleccionar(Actor).FirstOrDefault(x => x.Id == Seleccion);
            EmpresaIVA.Text = empresa == null ? "Seleccione una empresa existente." : Server.HtmlEncode(empresa.RazonSocial + " · ID " + empresa.Id);
            Tarifas.DataSource = empresa == null ? null : iva.Seleccionar(Actor, empresa.Id); Tarifas.DataBind();
        }
        private EmpresaModelo Leer(bool nueva)
        {
            return new EmpresaModelo { Id = nueva ? 0 : Seleccion, RUC = Ruc.Text.Trim(), RazonSocial = RazonSocial.Text.Trim(), NombreComercial = NombreComercial.Text.Trim(),
                Direccion = Direccion.Text.Trim(), Telefono = Telefono.Text.Trim(), Correo = Correo.Text.Trim(), RepresentanteLegal = Representante.Text.Trim(), FechaRegistro = Fecha(FechaRegistro.Text), Activa = Activo.Checked };
        }
        protected void GuardarRegistro(object sender, EventArgs e)
        { Ejecutar(Mensaje, delegate { var empresa = Leer(true); servicio.Guardar(Actor, empresa); Seleccion = empresa.Id; IdRegistro.Text = empresa.Id.ToString(); LimpiarTarifa(sender, e); Cargar(); }); }
        protected void ActualizarRegistro(object sender, EventArgs e)
        { Ejecutar(Mensaje, delegate { if (Seleccion == 0) throw new InvalidOperationException("Seleccione una empresa."); servicio.Guardar(Actor, Leer(false)); Cargar(); }); }
        protected void EliminarRegistro(object sender, EventArgs e)
        { Ejecutar(Mensaje, delegate { servicio.Eliminar(Actor, Seleccion); Limpiar(sender, e); Cargar(); }); }
        protected void SeleccionarRegistro(object sender, EventArgs e)
        {
            Ejecutar(Mensaje, delegate
            {
                int id = Convert.ToInt32(Tabla.SelectedDataKey.Value); var empresa = servicio.Seleccionar(Actor).FirstOrDefault(x => x.Id == id);
                if (empresa == null) throw new InvalidOperationException("La empresa ya no existe.");
                Seleccion = empresa.Id; IdRegistro.Text = empresa.Id.ToString(); Ruc.Text = empresa.RUC; RazonSocial.Text = empresa.RazonSocial; NombreComercial.Text = empresa.NombreComercial;
                Direccion.Text = empresa.Direccion; Telefono.Text = empresa.Telefono; Correo.Text = empresa.Correo; Representante.Text = empresa.RepresentanteLegal; FechaRegistro.Text = Dia(empresa.FechaRegistro); Activo.Checked = empresa.Activa;
                LimpiarTarifa(sender, e); CargarTarifas();
            });
        }
        protected void Limpiar(object sender, EventArgs e)
        {
            Seleccion = 0; IdRegistro.Text = Ruc.Text = RazonSocial.Text = NombreComercial.Text = Direccion.Text = Telefono.Text = Correo.Text = Representante.Text = "";
            FechaRegistro.Text = Dia(DateTime.Today); Activo.Checked = true; Mensaje.Text = ""; LimpiarTarifa(sender, e); CargarTarifas();
        }
        protected void BuscarRegistros(object sender, EventArgs e) { Ejecutar(Mensaje, Cargar); }
        private TarifaIVA LeerTarifa(bool nueva)
        {
            return new TarifaIVA { Id = nueva ? 0 : TarifaSeleccion, IdEmpresa = Seleccion, Porcentaje = Importe(Porcentaje.Text), FechaInicio = Fecha(InicioIVA.Text),
                FechaFin = string.IsNullOrWhiteSpace(FinIVA.Text) ? (DateTime?)null : Fecha(FinIVA.Text), Descripcion = DescripcionIVA.Text.Trim() };
        }
        protected void InsertarTarifa(object sender, EventArgs e)
        { Ejecutar(Mensaje, delegate { iva.Guardar(Actor, LeerTarifa(true)); LimpiarTarifa(sender, e); CargarTarifas(); }); }
        protected void ModificarTarifa(object sender, EventArgs e)
        { Ejecutar(Mensaje, delegate { if (TarifaSeleccion == 0) throw new InvalidOperationException("Seleccione una tarifa."); iva.Guardar(Actor, LeerTarifa(false)); CargarTarifas(); }); }
        protected void BorrarTarifa(object sender, EventArgs e)
        { Ejecutar(Mensaje, delegate { iva.Eliminar(Actor, TarifaSeleccion); LimpiarTarifa(sender, e); CargarTarifas(); }); }
        protected void SeleccionarTarifa(object sender, EventArgs e)
        {
            Ejecutar(Mensaje, delegate
            {
                int id = Convert.ToInt32(Tarifas.SelectedDataKey.Value); var t = iva.Seleccionar(Actor, Seleccion).FirstOrDefault(x => x.Id == id);
                if (t == null) throw new InvalidOperationException("La tarifa ya no existe.");
                TarifaSeleccion = t.Id; IdTarifa.Text = t.Id.ToString(); Porcentaje.Text = Numero(t.Porcentaje); InicioIVA.Text = Dia(t.FechaInicio);
                FinIVA.Text = t.FechaFin.HasValue ? Dia(t.FechaFin.Value) : ""; DescripcionIVA.Text = t.Descripcion;
            });
        }
        protected void LimpiarTarifa(object sender, EventArgs e) { TarifaSeleccion = 0; IdTarifa.Text = Porcentaje.Text = InicioIVA.Text = FinIVA.Text = DescripcionIVA.Text = ""; }
    }
}
