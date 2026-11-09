using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using CooperativaCarchi.Modelos;
using CooperativaCarchi.Servicios;

namespace CooperativaCarchi
{
    public class PaginaBase : Page
    {
        protected virtual bool Publica { get { return false; } }
        protected Usuario Autenticado { get; private set; }
        protected int Actor { get { return Autenticado == null ? 0 : Autenticado.Id; } }
        protected bool EsAdmin { get { return Autenticado != null && Autenticado.Rol == Rol.Administrador; } }
        protected override void OnInit(EventArgs e)
        {
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.Charset = "utf-8";
            if (Session["Csrf"] == null) Session["Csrf"] = Guid.NewGuid().ToString("N");
            ViewStateUserKey = (string)Session["Csrf"];
            if (IsPostBack && Request.Form["csrf"] != (string)Session["Csrf"]) throw new HttpException(403, "Solicitud inválida. Recargue la página.");
            object id = Session["UsuarioId"];
            if (id is int) Autenticado = new UsuarioService().ConsultarSesion((int)id);
            if (!Publica && Autenticado == null) { Response.Redirect("Login.aspx", true); return; }
            Response.Cache.SetCacheability(HttpCacheability.NoCache); Response.Cache.SetNoStore();
            base.OnInit(e);
        }
        protected override void Render(HtmlTextWriter writer)
        {
            if (Form != null) ClientScript.RegisterHiddenField("csrf", (string)Session["Csrf"]);
            base.Render(writer);
        }
        protected void Ejecutar(Label mensaje, Action accion)
        {
            try { accion(); mensaje.Text = "Operación completada."; mensaje.CssClass = "mensaje exito"; }
            catch (InvalidOperationException ex) { mensaje.Text = Server.HtmlEncode(ex.Message); mensaje.CssClass = "mensaje error"; }
            catch (FormatException) { mensaje.Text = "Revise las fechas y los números ingresados."; mensaje.CssClass = "mensaje error"; }
            catch (OverflowException) { mensaje.Text = "El número ingresado es demasiado grande."; mensaje.CssClass = "mensaje error"; }
        }
        protected static int Entero(string valor) { return int.Parse(valor, CultureInfo.InvariantCulture); }
        protected static decimal Importe(string valor)
        {
            decimal n;
            if (!decimal.TryParse(valor, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out n))
                throw new InvalidOperationException("Ingrese números con punto decimal y sin separadores de miles, por ejemplo 1000.50.");
            return n;
        }
        protected static DateTime Fecha(string valor) { return DateTime.ParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture); }
        protected static string Numero(decimal valor) { return valor.ToString("0.##", CultureInfo.InvariantCulture); }
        protected static string Dia(DateTime valor) { return valor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
        protected void SoloAdmin() { if (!EsAdmin) throw new InvalidOperationException("Solo el administrador puede modificar registros."); }
    }
}
