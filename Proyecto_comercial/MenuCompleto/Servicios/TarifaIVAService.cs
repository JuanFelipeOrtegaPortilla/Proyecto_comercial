using System;
using System.Collections.Generic;
using System.Linq;
using CooperativaCarchi.Datos;
using CooperativaCarchi.Modelos;

namespace CooperativaCarchi.Servicios
{
    public class TarifaIVAService
    {
        public List<TarifaIVA> Seleccionar(int actor, int empresa)
        { return DatosTemporales.Ejecutar(delegate { Reglas.Acceso(actor, false); Reglas.Empresa(empresa); return DatosTemporales.Tarifas.Where(x => x.IdEmpresa == empresa).OrderBy(x => x.FechaInicio).Select(x => x.Copia<TarifaIVA>()).ToList(); }); }
        public void Guardar(int actor, TarifaIVA t)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, true); Reglas.Empresa(t.IdEmpresa);
                Reglas.Exigir(t.Porcentaje >= 0 && t.Porcentaje <= 100, "El porcentaje debe estar entre 0 y 100.");
                Reglas.Exigir(t.FechaInicio != DateTime.MinValue, "Ingrese la fecha inicial.");
                t.FechaInicio = t.FechaInicio.Date; if (t.FechaFin.HasValue) t.FechaFin = t.FechaFin.Value.Date;
                Reglas.Exigir(!t.FechaFin.HasValue || t.FechaFin >= t.FechaInicio, "La fecha final no puede ser anterior a la inicial.");
                var actual = DatosTemporales.Tarifas.FirstOrDefault(x => x.Id == t.Id);
                Reglas.Exigir(t.Id == 0 || actual != null, "La tarifa ya no existe.");
                // Puede cerrarse una tarifa abierta para programar otra, sin excluir usos confirmados.
                Reglas.Exigir(actual == null || !actual.Utilizada ||
                    (actual.IdEmpresa == t.IdEmpresa && actual.Porcentaje == t.Porcentaje && actual.FechaInicio == t.FechaInicio &&
                    (!t.FechaFin.HasValue || t.FechaFin >= actual.UltimaFechaUso)),
                    "La tarifa fue utilizada: conserve empresa, porcentaje, inicio y todos los días con operaciones confirmadas.");
                if (actual != null) { t.Utilizada = actual.Utilizada; t.UltimaFechaUso = actual.UltimaFechaUso; }
                Reglas.Exigir(!DatosTemporales.Tarifas.Any(x => x.Id != t.Id && x.IdEmpresa == t.IdEmpresa &&
                    x.FechaInicio <= (t.FechaFin ?? DateTime.MaxValue) && t.FechaInicio <= (x.FechaFin ?? DateTime.MaxValue)),
                    "El período se superpone con otra tarifa de esta empresa. Los extremos son inclusivos; cierre la anterior el día previo.");
                if (actual == null) { t.Id = DatosTemporales.SiguienteId("tarifas_iva"); DatosTemporales.Tarifas.Add(t.Copia<TarifaIVA>()); }
                else DatosTemporales.Tarifas[DatosTemporales.Tarifas.IndexOf(actual)] = t.Copia<TarifaIVA>();
            });
        }
        public void Eliminar(int actor, int id)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, true); var t = DatosTemporales.Tarifas.FirstOrDefault(x => x.Id == id);
                Reglas.Exigir(t != null, "Seleccione una tarifa existente.");
                Reglas.Exigir(!t.Utilizada, "La tarifa tiene operaciones relacionadas y debe conservarse."); DatosTemporales.Tarifas.Remove(t);
            });
        }
        public TarifaIVA ObtenerAplicable(int actor, int empresa, DateTime fecha)
        {
            return DatosTemporales.Ejecutar(delegate { Reglas.Acceso(actor, false); Reglas.Empresa(empresa); return Buscar(empresa, fecha).Copia<TarifaIVA>(); });
        }
        private static TarifaIVA Buscar(int empresa, DateTime fecha)
        {
            var t = DatosTemporales.Tarifas.SingleOrDefault(x => x.IdEmpresa == empresa && x.FechaInicio <= fecha.Date && (!x.FechaFin.HasValue || x.FechaFin >= fecha.Date));
            Reglas.Exigir(t != null, "No hay una tarifa de IVA configurada para esa fecha y empresa. Configure el período antes de continuar."); return t;
        }
        public ResultadoIVA Calcular(int actor, int empresa, DateTime fecha, decimal subtotal, TratamientoIVA tratamiento)
        {
            return DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, false); Reglas.Empresa(empresa);
                Reglas.Exigir(subtotal >= 0 && Enum.IsDefined(typeof(TratamientoIVA), tratamiento), "Subtotal o tratamiento inválido.");
                TarifaIVA t = tratamiento == TratamientoIVA.Gravado || tratamiento == TratamientoIVA.TarifaCero ? Buscar(empresa, fecha) : null;
                Reglas.Exigir(tratamiento != TratamientoIVA.TarifaCero || t.Porcentaje == 0, "La tarifa configurada no corresponde al tratamiento tarifa cero.");
                decimal baseMonetaria = Redondear(subtotal), porcentaje = t == null ? 0 : t.Porcentaje;
                return new ResultadoIVA(empresa, fecha, tratamiento, t == null ? (int?)null : t.Id, porcentaje, baseMonetaria, Redondear(baseMonetaria * porcentaje / 100m));
            });
        }
        // Futura persistencia debe llamar a este método bajo la misma unidad de trabajo.
        // Una previsualización con Calcular no consume ni bloquea una tarifa.
        public void ConfirmarUso(int actor, ResultadoIVA resultado)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, true); Reglas.Exigir(resultado != null, "Resultado requerido."); Reglas.Empresa(resultado.IdEmpresa);
                if (!resultado.IdTarifa.HasValue) return;
                var t = Buscar(resultado.IdEmpresa, resultado.Fecha);
                Reglas.Exigir(t.Id == resultado.IdTarifa && t.Porcentaje == resultado.PorcentajeAplicado, "La configuración cambió. Vuelva a calcular antes de confirmar."); t.Utilizada = true;
                if (!t.UltimaFechaUso.HasValue || resultado.Fecha > t.UltimaFechaUso) t.UltimaFechaUso = resultado.Fecha;
            });
        }
        public static decimal Redondear(decimal importe) { return decimal.Round(importe, 2, MidpointRounding.AwayFromZero); }
    }
}
