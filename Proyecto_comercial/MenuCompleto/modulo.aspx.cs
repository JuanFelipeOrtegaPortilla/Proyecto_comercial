using System;
using System.Collections.Generic;

namespace CooperativaCarchi
{
    public partial class modulo : PaginaBase
    {
        protected ModuloInfo Actual;
        protected ModuloInfo Anterior;
        protected ModuloInfo Siguiente;
        protected int Total;

        protected void Page_Load(object sender, EventArgs e)
        {
            Actual = ModulosCatalogo.Buscar(Request.QueryString["id"]);

            if (Actual == null)
            {
                Response.Redirect("menu.aspx", true);
                return;
            }

            if (ModulosCatalogo.Disponible(Actual.Id))
            {
                Response.Redirect(ModulosCatalogo.Ruta(Actual.Id), true);
                return;
            }

            List<ModuloInfo> todos = ModulosCatalogo.Todos();
            Total = todos.Count;

            int i = todos.IndexOf(Actual);
            Anterior = i > 0 ? todos[i - 1] : null;
            Siguiente = i < todos.Count - 1 ? todos[i + 1] : null;

            Title = Actual.Titulo + " | Cooperativa Carchi";
        }
    }
}