using System;
using System.Collections.Generic;
using CooperativaCarchi.Servicios;
using CooperativaCarchi.Modelos;
namespace CooperativaCarchi {
 public partial class PanelModulos : System.Web.UI.UserControl {
 protected List<GrupoModulos> Grupos;
 protected int Total;
 protected bool EsAdministrador;
 protected void Page_Load(object sender, EventArgs e) {
 Grupos = ModulosCatalogo.ObtenerGrupos(); Total = ModulosCatalogo.Todos().Count;
 var id = Session["UsuarioId"];
 var usuario = id is int ? new UsuarioService().ConsultarSesion((int)id) : null;
 EsAdministrador = usuario != null && usuario.Rol == Rol.Administrador;
 }
 protected string ResumenDisponible(string id) {
 switch(id) {
 case "transacciones": return "Borradores manuales, compras y ventas con IVA por vigencia; confirmación de asientos balanceados.";
 case "diario": return "Asientos confirmados por empresa y período, con sus líneas Debe y Haber.";
 case "mayor": return "Movimientos y saldo acumulado por cuenta, incluyendo saldos iniciales.";
 case "iva": return "Tarifas editables por empresa y vigencia, con historial y validación de períodos.";
 default: return "Consulta y mantenimiento del catálogo, la jerarquía y los saldos iniciales por empresa.";
 }
 }
 }
}
