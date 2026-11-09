using System;

namespace CooperativaCarchi.Modelos
{
    public class CuentaContable : Entidad
    {
        public int IdEmpresa { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public int Grupo { get; set; }
        public string CuentaPadre { get; set; }
        public int Nivel { get { return (Codigo ?? "").Split('.').Length; } }
        public string GrupoExcel { get; set; }
        public string Naturaleza { get; set; }
        public string Tipo { get; set; }
        public decimal DebeInicial { get; set; }
        public decimal HaberInicial { get; set; }
        public bool Activa { get; set; }
        public bool TieneMovimientos { get; internal set; }
    }
}
