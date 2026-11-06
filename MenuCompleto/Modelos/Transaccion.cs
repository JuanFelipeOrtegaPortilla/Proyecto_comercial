using System;
namespace CooperativaCarchi.Modelos
{
    public class Transaccion : Entidad
    {
        public int IdEmpresa { get; set; }
        public DateTime Fecha { get; set; }
        public string Glosa { get; set; }
        public string Tipo { get; set; }
        public string Estado { get; internal set; }
        public string Tratamiento { get; set; }
        public decimal Subtotal { get; set; }
        public int? IdTarifa { get; internal set; }
        public decimal PorcentajeAplicado { get; internal set; }
        public decimal IVA { get; internal set; }
        public decimal Total { get; internal set; }
        public int CuentaBase { get; set; }
        public int? CuentaIVA { get; set; }
        public int Contrapartida { get; set; }
    }
    [Serializable]
    public class LineaAsiento : Entidad
    {
        public int IdTransaccion { get; set; }
        public int IdCuenta { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
    }
    public class FilaLibro
    {
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string Glosa { get; set; }
        public string Codigo { get; set; }
        public string Cuenta { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public decimal Saldo { get; set; }
    }
}
