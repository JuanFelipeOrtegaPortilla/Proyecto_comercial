<%@ Application Language="C#" %>
<script runat="server">
void Application_Error(object sender, EventArgs e)
{
    Exception error = Server.GetLastError();
    while (error != null && error.InnerException != null) error = error.InnerException;
    if (error is InvalidOperationException &&
        (error.Message.Contains("PostgreSQL") || error.Message.Contains("Almacenamiento debe")))
    {
        Server.ClearError();
        Response.Redirect("~/EstadoConexion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }
}
</script>
