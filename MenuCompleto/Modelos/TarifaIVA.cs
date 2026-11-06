using System;

namespace CooperativaCarchi.Modelos
{
    public class TarifaIVA : Entidad
    {
        public int IdTarifa { get { return Id; } set { Id = value; } }
        public int IdEmpresa { get; set; }
        public decimal Porcentaje { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Descripcion { get; set; }
        public bool Utilizada { get; internal set; }
        internal DateTime? UltimaFechaUso { get; set; }
        public string Estado { get { return EstadoEn(DateTime.Today); } }
        public string EstadoEn(DateTime fecha)
        {
            return fecha.Date < FechaInicio.Date ? "Programado" :
                FechaFin.HasValue && fecha.Date > FechaFin.Value.Date ? "Histórico" : "Vigente";
        }
    }
    public enum TratamientoIVA { Gravado, TarifaCero, NoObjeto, Exento }
    // Instantánea inmutable para el futuro registro: nunca recalcularla al cambiar una tarifa.
    public sealed class ResultadoIVA
    {
        public int? IdTarifa { get; private set; }
        public int IdEmpresa { get; private set; }
        public DateTime Fecha { get; private set; }
        public TratamientoIVA Tratamiento { get; private set; }
        public decimal PorcentajeAplicado { get; private set; }
        public decimal Subtotal { get; private set; }
        public decimal IVA { get; private set; }
        public decimal Total { get { return Subtotal + IVA; } }
        internal ResultadoIVA(int empresa, DateTime fecha, TratamientoIVA tratamiento,
            int? tarifa, decimal porcentaje, decimal subtotal, decimal iva)
        {
            IdEmpresa = empresa; Fecha = fecha.Date; Tratamiento = tratamiento;
            IdTarifa = tarifa; PorcentajeAplicado = porcentaje; Subtotal = subtotal; IVA = iva;
        }
    }
}
