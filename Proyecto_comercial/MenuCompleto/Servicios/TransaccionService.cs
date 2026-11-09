using System;
using System.Collections.Generic;
using System.Linq;
using CooperativaCarchi.Datos;
using CooperativaCarchi.Modelos;
namespace CooperativaCarchi.Servicios
{
    public class TransaccionService
    {
        private static void Acceso(int actor, bool editar)
        {
            Reglas.Acceso(actor, editar);
            Reglas.Exigir(!DatosTemporales.UsaPostgres || DatosTemporales.ModuloContableInstalado,
                "Ejecute Sql/04_modulos_contables.sql en la base conectada antes de utilizar los módulos contables.");
        }
        public List<Transaccion> Seleccionar(int actor, int empresa)
        {
            return DatosTemporales.Ejecutar(() => { Acceso(actor, false); Reglas.Empresa(empresa);
                return DatosTemporales.Transacciones.Where(x => x.IdEmpresa == empresa).OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).Select(x => x.Copia<Transaccion>()).ToList(); });
        }
        public List<LineaAsiento> Detalle(int actor, int id)
        {
            return DatosTemporales.Ejecutar(() => { Acceso(actor, false); Buscar(id);
                return DatosTemporales.Lineas.Where(x => x.IdTransaccion == id).OrderBy(x => x.Id).Select(x => x.Copia<LineaAsiento>()).ToList(); });
        }
        private static Transaccion Buscar(int id)
        { var t = DatosTemporales.Transacciones.FirstOrDefault(x => x.Id == id); Reglas.Exigir(t != null, "Seleccione una transacción existente."); return t; }
        private static CuentaContable Cuenta(int empresa, int id)
        {
            var c = DatosTemporales.Cuentas.FirstOrDefault(x => x.IdEmpresa == empresa && x.Id == id && x.Activa && x.Tipo == "Movimiento");
            Reglas.Exigir(c != null, "Las cuentas deben estar activas, ser de movimiento y pertenecer a la empresa."); return c;
        }
        public void Guardar(int actor, Transaccion entrada, List<LineaAsiento> detalle)
        {
            DatosTemporales.Ejecutar(() => {
                Acceso(actor, true); Reglas.Empresa(entrada.IdEmpresa); Reglas.Texto(entrada.Glosa, "Glosa");
                Reglas.Exigir(entrada.Fecha != DateTime.MinValue, "Ingrese una fecha válida.");
                var t = entrada.Copia<Transaccion>(); t.Fecha = t.Fecha.Date;
                if (t.Id != 0) { var previa = Buscar(t.Id); Reglas.Exigir(previa.Estado == "Borrador" && previa.IdEmpresa == t.IdEmpresa, "Un asiento confirmado es inmutable. No cambie la empresa de una transacción."); }
                Reglas.Exigir(t.Tipo == "Manual" || t.Tipo == "Compra" || t.Tipo == "Venta", "Tipo de operación inválido.");
                var lineas = new List<LineaAsiento>();
                if (t.Tipo == "Manual")
                {
                    t.Tratamiento = "NoObjeto"; t.IdTarifa = null; t.Subtotal = t.IVA = t.Total = t.PorcentajeAplicado = 0;
                    t.CuentaBase = t.Contrapartida = 0; t.CuentaIVA = null;
                    Reglas.Exigir(detalle != null && detalle.Count >= 2 && detalle.Count <= 100, "El asiento manual debe tener entre 2 y 100 líneas.");
                    lineas = detalle.Select(x => x.Copia<LineaAsiento>()).ToList();
                }
                else
                {
                    TratamientoIVA tratamiento;
                    Reglas.Exigir(Enum.TryParse(t.Tratamiento, out tratamiento) && Enum.IsDefined(typeof(TratamientoIVA), tratamiento), "Tratamiento inválido.");
                    Reglas.Exigir(t.Subtotal > 0 && t.Subtotal == TarifaIVAService.Redondear(t.Subtotal), "Ingrese un subtotal positivo con dos decimales.");
                    var calculo = new TarifaIVAService().Calcular(actor, t.IdEmpresa, t.Fecha, t.Subtotal, tratamiento);
                    t.IdTarifa = calculo.IdTarifa; t.PorcentajeAplicado = calculo.PorcentajeAplicado; t.IVA = calculo.IVA; t.Total = calculo.Total;
                    var baseCuenta = Cuenta(t.IdEmpresa, t.CuentaBase); Cuenta(t.IdEmpresa, t.Contrapartida);
                    Reglas.Exigir(t.CuentaBase != t.Contrapartida && (t.Tipo != "Venta" || baseCuenta.Grupo == 4) &&
                        (t.Tipo != "Compra" || baseCuenta.Grupo == 1 || baseCuenta.Grupo == 5), "La base de una venta debe ser de Ingresos; la compra debe ser de Activo o Gastos. Use contrapartidas diferentes.");
                    bool compra = t.Tipo == "Compra";
                    lineas.Add(new LineaAsiento { IdCuenta = t.CuentaBase, Debe = compra ? t.Subtotal : 0, Haber = compra ? 0 : t.Subtotal });
                    if (t.IVA > 0)
                    {
                        Reglas.Exigir(t.CuentaIVA.HasValue && t.CuentaIVA != t.CuentaBase && t.CuentaIVA != t.Contrapartida, "Seleccione una cuenta IVA distinta a las otras cuentas.");
                        var cuentaIVA = Cuenta(t.IdEmpresa, t.CuentaIVA.Value);
                        Reglas.Exigir(cuentaIVA.Grupo == (compra ? 1 : 2), "El IVA de compras debe pertenecer a Activo y el de ventas a Pasivo.");
                        lineas.Add(new LineaAsiento { IdCuenta = t.CuentaIVA.Value, Debe = compra ? t.IVA : 0, Haber = compra ? 0 : t.IVA });
                    }
                    else t.CuentaIVA = null;
                    lineas.Add(new LineaAsiento { IdCuenta = t.Contrapartida, Debe = compra ? 0 : t.Total, Haber = compra ? t.Total : 0 });
                }
                foreach (var l in lineas)
                {
                    Cuenta(t.IdEmpresa, l.IdCuenta);
                    Reglas.Exigir(l.Debe >= 0 && l.Haber >= 0 && (l.Debe > 0) != (l.Haber > 0) &&
                        l.Debe == TarifaIVAService.Redondear(l.Debe) && l.Haber == TarifaIVAService.Redondear(l.Haber), "Cada línea debe tener un importe positivo en Debe o Haber, con dos decimales.");
                }
                Reglas.Exigir(lineas.Sum(x => x.Debe) == lineas.Sum(x => x.Haber), "El asiento debe cuadrar: Debe y Haber deben ser iguales.");
                // Validaciones completadas antes de modificar el estado compartido.
                if (t.Id == 0) t.Id = DatosTemporales.SiguienteId("transacciones");
                t.Estado = "Borrador";
                foreach (var l in lineas) { l.Id = DatosTemporales.SiguienteId("lineas_asiento"); l.IdTransaccion = t.Id; }
                DatosTemporales.Lineas.RemoveAll(x => x.IdTransaccion == t.Id); DatosTemporales.Lineas.AddRange(lineas);
                DatosTemporales.Transacciones.RemoveAll(x => x.Id == t.Id); DatosTemporales.Transacciones.Add(t); entrada.Id = t.Id;
            });
        }
        public void Eliminar(int actor, int id)
        {
            DatosTemporales.Ejecutar(() => { Acceso(actor, true); var t = Buscar(id);
                Reglas.Exigir(t.Estado == "Borrador", "No se pueden eliminar asientos confirmados.");
                DatosTemporales.Lineas.RemoveAll(x => x.IdTransaccion == id); DatosTemporales.Transacciones.Remove(t); });
        }
        public void Confirmar(int actor, int id)
        {
            DatosTemporales.Ejecutar(() => {
                Acceso(actor, true); var t = Buscar(id); Reglas.Exigir(t.Estado == "Borrador", "La transacción ya fue confirmada.");
                var detalle = DatosTemporales.Lineas.Where(x => x.IdTransaccion == id).ToList();
                Reglas.Exigir(detalle.Count >= 2 && detalle.Sum(x => x.Debe) == detalle.Sum(x => x.Haber), "Asiento incompleto o descuadrado.");
                foreach (var l in detalle) Cuenta(t.IdEmpresa, l.IdCuenta);
                if (t.IdTarifa.HasValue)
                {
                    var calculo = new TarifaIVAService().Calcular(actor, t.IdEmpresa, t.Fecha, t.Subtotal, (TratamientoIVA)Enum.Parse(typeof(TratamientoIVA), t.Tratamiento));
                    Reglas.Exigir(calculo.IdTarifa == t.IdTarifa && calculo.PorcentajeAplicado == t.PorcentajeAplicado && calculo.IVA == t.IVA, "La tarifa cambió. Actualice el borrador antes de confirmar.");
                    new TarifaIVAService().ConfirmarUso(actor, calculo);
                }
                foreach (var l in detalle) Cuenta(t.IdEmpresa, l.IdCuenta).TieneMovimientos = true;
                t.Estado = "Confirmada";
            });
        }
        public List<FilaLibro> Diario(int actor, int empresa, DateTime desde, DateTime hasta)
        {
            return DatosTemporales.Ejecutar(() => {
                Acceso(actor, false); Reglas.Empresa(empresa); Reglas.Exigir(desde.Date <= hasta.Date, "Período inválido.");
                return (from t in DatosTemporales.Transacciones where t.IdEmpresa == empresa && t.Estado == "Confirmada" && t.Fecha >= desde.Date && t.Fecha <= hasta.Date
                    join l in DatosTemporales.Lineas on t.Id equals l.IdTransaccion join c in DatosTemporales.Cuentas on l.IdCuenta equals c.Id
                    orderby t.Fecha, t.Id, l.Id select new FilaLibro { Numero = t.Id, Fecha = t.Fecha, Glosa = t.Glosa, Codigo = c.Codigo, Cuenta = c.Nombre, Debe = l.Debe, Haber = l.Haber }).ToList();
            });
        }
        public List<FilaLibro> Mayor(int actor, int empresa, int cuenta, DateTime desde, DateTime hasta)
        {
            return DatosTemporales.Ejecutar(() => {
                Acceso(actor, false); var c = DatosTemporales.Cuentas.FirstOrDefault(x => x.Id == cuenta && x.IdEmpresa == empresa && x.Tipo == "Movimiento");
                Reglas.Exigir(c != null, "Seleccione una cuenta de movimiento de la empresa.");
                var movimientos = Diario(actor, empresa, DateTime.MinValue, hasta).Where(x => x.Codigo == c.Codigo).ToList();
                Reglas.Exigir(desde.Date <= hasta.Date, "Período inválido.");
                decimal saldo = c.DebeInicial - c.HaberInicial + movimientos.Where(x => x.Fecha < desde.Date).Sum(x => x.Debe - x.Haber);
                var filas = new List<FilaLibro> { new FilaLibro { Fecha = desde.Date, Codigo = c.Codigo, Cuenta = c.Nombre, Glosa = "Saldo anterior (incluye saldo inicial del plan)", Saldo = saldo } };
                foreach (var l in movimientos.Where(x => x.Fecha >= desde.Date)) { saldo += l.Debe - l.Haber; l.Saldo = saldo; filas.Add(l); }
                return filas;
            });
        }
    }
}
