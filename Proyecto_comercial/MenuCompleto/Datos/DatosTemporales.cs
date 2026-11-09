using System;
using System.Configuration;
using System.Collections.Generic;
using CooperativaCarchi.Modelos;

namespace CooperativaCarchi.Datos
{
    // Todo acceso, incluso relaciones entre colecciones, usa el mismo cerrojo.
    internal static class DatosTemporales
    {
        internal static readonly object Sync = new object();
        internal static readonly List<Usuario> Usuarios = new List<Usuario>();
        internal static readonly Dictionary<int, string> Claves = new Dictionary<int, string>();
        internal static readonly List<Empresa> Empresas = new List<Empresa>();
        internal static readonly List<CuentaContable> Cuentas = new List<CuentaContable>();
        internal static readonly List<TarifaIVA> Tarifas = new List<TarifaIVA>();
        internal static int UsuarioId, EmpresaId, CuentaId, TarifaId, TransaccionId, LineaId;
        internal static bool ModuloContableInstalado = true;
        internal static readonly List<Transaccion> Transacciones = new List<Transaccion>();
        internal static readonly List<LineaAsiento> Lineas = new List<LineaAsiento>();
        internal static bool UsaPostgres
        {
            get
            {
                string modo = ConfigurationManager.AppSettings["Almacenamiento"] ?? "Memoria";
                if (modo != "Memoria" && modo != "PostgreSQL") throw new InvalidOperationException("Almacenamiento debe ser Memoria o PostgreSQL.");
                return modo == "PostgreSQL";
            }
        }
        internal static T Ejecutar<T>(Func<T> accion)
        {
            lock (Sync)
            {
                if (!UsaPostgres) return accion();
                return UnidadPostgres.Ejecutar(accion);
            }
        }
        internal static void Ejecutar(Action accion) { Ejecutar<object>(() => { accion(); return null; }); }
        internal static int SiguienteId(string tabla)
        {
            if (UsaPostgres) return UnidadPostgres.SiguienteId(tabla);
            switch (tabla) { case "usuarios": return ++UsuarioId; case "empresas": return ++EmpresaId;
                case "transacciones": return ++TransaccionId; case "lineas_asiento": return ++LineaId;
                case "plan_cuentas": return ++CuentaId; case "tarifas_iva": return ++TarifaId; default: throw new InvalidOperationException("Entidad inválida."); }
        }
        static DatosTemporales()
        {
            Empresas.Add(new Empresa { Id = ++EmpresaId, RazonSocial = "Carchi Cía. Ltda.",
                FechaRegistro = DateTime.Today, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1", Nombre = "ACTIVO", CuentaPadre = "", Grupo = 1, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1", Nombre = "CORRIENTE", CuentaPadre = "1", Grupo = 1, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.1", Nombre = "Caja", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 5500m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.2", Nombre = "Caja chica", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.3", Nombre = "Bancos", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 86700m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.4", Nombre = "Inventario de mercaderías", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 32765m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.5", Nombre = "Clientes", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 9500m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.6", Nombre = "Provisión cuentas incobrables", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.7", Nombre = "Comisiones por cobrar", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.8", Nombre = "Anticipo de sueldos", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.9", Nombre = "IVA compras", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.10", Nombre = "Anticipo IVA retenido", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.10.1", Nombre = "Anticipo IVA retenido   30 %", CuentaPadre = "1.1.10", Grupo = 1, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.10.2", Nombre = "Anticipo IVA retenido   70 %", CuentaPadre = "1.1.10", Grupo = 1, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.10.3", Nombre = "Anticipo IVA retenido 100 %", CuentaPadre = "1.1.10", Grupo = 1, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.11", Nombre = "Anticipo retención en la fuente", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.11.1", Nombre = "Anticipo retención en la fuente 1 %", CuentaPadre = "1.1.11", Grupo = 1, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.11.2", Nombre = "Anticipo retención en la fuente 5 %", CuentaPadre = "1.1.11", Grupo = 1, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.12", Nombre = "Inventario suministros de oficina", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.1.13", Nombre = "Seguros prepagados", CuentaPadre = "1.1", Grupo = 1, GrupoExcel = "AC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.2", Nombre = "NO CORRIENTE", CuentaPadre = "1", Grupo = 1, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.2.1", Nombre = "Muebles de oficina", CuentaPadre = "1.2", Grupo = 1, GrupoExcel = "ANC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 4800m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.2.2", Nombre = "Depreciación acumulada muebles", CuentaPadre = "1.2", Grupo = 1, GrupoExcel = "ANC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 352m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.2.3", Nombre = "Equipos de computación", CuentaPadre = "1.2", Grupo = 1, GrupoExcel = "ANC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 3800m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.2.4", Nombre = "Depreciación acumulada equipos", CuentaPadre = "1.2", Grupo = 1, GrupoExcel = "ANC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 258m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.2.5", Nombre = "Vehículos", CuentaPadre = "1.2", Grupo = 1, GrupoExcel = "ANC", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "1.2.6", Nombre = "Depreciación acumulada de vehículos", CuentaPadre = "1.2", Grupo = 1, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2", Nombre = "PASIVO", CuentaPadre = "", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1", Nombre = "CORRIENTE", CuentaPadre = "2", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.1", Nombre = "Aporte individual IESS por pagar", CuentaPadre = "2.1", Grupo = 2, GrupoExcel = "PC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.2", Nombre = "Aporte patronal IESS por pagar", CuentaPadre = "2.1", Grupo = 2, GrupoExcel = "PC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.3", Nombre = "Retenciones judiciales", CuentaPadre = "2.1", Grupo = 2, GrupoExcel = "PC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.4", Nombre = "IVA ventas", CuentaPadre = "2.1", Grupo = 2, GrupoExcel = "PC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.5", Nombre = "Retención en la fuente por pagar", CuentaPadre = "2.1", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.5.1", Nombre = "Retención en la fuente por pagar 1 %", CuentaPadre = "2.1.5", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.5.2", Nombre = "Retención en la fuente por pagar 5 %", CuentaPadre = "2.1.5", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.5.3", Nombre = "Retención en la fuente por pagar 0.1 %", CuentaPadre = "2.1.5", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.6", Nombre = "IVA retenido por pagar", CuentaPadre = "2.1", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.6.1", Nombre = "IVA retenido por pagar  30 %", CuentaPadre = "2.1.6", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.6.2", Nombre = "IVA retenido por pagar  70 %", CuentaPadre = "2.1.6", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.6.3", Nombre = "IVA retenido por pagar 100 %", CuentaPadre = "2.1.6", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.7", Nombre = "Proveedores", CuentaPadre = "2.1", Grupo = 2, GrupoExcel = "PC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 10000m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.8", Nombre = "Cuentas por pagar", CuentaPadre = "2.1", Grupo = 2, GrupoExcel = "PC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.1.9", Nombre = "Documentos por pagar", CuentaPadre = "2.1", Grupo = 2, GrupoExcel = "PC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.2", Nombre = "NO CORRIENTE", CuentaPadre = "2", Grupo = 2, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "2.2.1", Nombre = "Préstamo bancario a largo plazo", CuentaPadre = "2.2", Grupo = 2, GrupoExcel = "PNC", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 6000m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3", Nombre = "PATRIMONIO", CuentaPadre = "", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.1", Nombre = "CAPITAL SOCIAL", CuentaPadre = "3", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.1.1", Nombre = "Capital", CuentaPadre = "3.1", Grupo = 3, GrupoExcel = "PAT", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 126455m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.2", Nombre = "RESERVAS", CuentaPadre = "3", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.2.1", Nombre = "Reserva legal", CuentaPadre = "3.2", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.2.2", Nombre = "Reserva facultativa", CuentaPadre = "3.2", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.2.3", Nombre = "Reserva estatutaria", CuentaPadre = "3.2", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.3", Nombre = "SUPERÁVIT", CuentaPadre = "3", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.3.1", Nombre = "Donaciones de capital", CuentaPadre = "3.3", Grupo = 3, GrupoExcel = "PAT", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.4", Nombre = "RESULTADOS", CuentaPadre = "3", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.4.1", Nombre = "Utilidad  no distribuida de ejercicios anteriores", CuentaPadre = "3.4", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.4.2", Nombre = "Pérdida acumulada de ejercicios anteriores", CuentaPadre = "3.4", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.4.3", Nombre = "Utilidad del ejercicio", CuentaPadre = "3.4", Grupo = 3, GrupoExcel = "PAT", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "3.4.4", Nombre = "Pérdida del ejercicio", CuentaPadre = "3.4", Grupo = 3, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "4", Nombre = "INGRESOS", CuentaPadre = "", Grupo = 4, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "4.1", Nombre = "OPERACIONALES", CuentaPadre = "4", Grupo = 4, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "4.1.1", Nombre = "Ventas", CuentaPadre = "4.1", Grupo = 4, GrupoExcel = "ING", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "4.1.2", Nombre = "Comisiones ganadas", CuentaPadre = "4.1", Grupo = 4, GrupoExcel = "ING", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "4.2", Nombre = "NO OPERACIONALES", CuentaPadre = "4", Grupo = 4, GrupoExcel = "", Naturaleza = "Acreedora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "4.2.1", Nombre = "Intereses ganados", CuentaPadre = "4.2", Grupo = 4, GrupoExcel = "ING", Naturaleza = "Acreedora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5", Nombre = "GASTOS", CuentaPadre = "", Grupo = 5, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1", Nombre = "ADMINISTRATIVOS", CuentaPadre = "5", Grupo = 5, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.1", Nombre = "Sueldos", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "GAD", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.2", Nombre = "Aporte patronal", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "GAD", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.3", Nombre = "Decimotercer sueldo", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "GAD", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.4", Nombre = "Arriendos pagados", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "GAD", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.5", Nombre = "Seguros pagados", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.6", Nombre = "Servicios básicos", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "GAD", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.6.1", Nombre = "Agua potable", CuentaPadre = "5.1.6", Grupo = 5, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.6.2", Nombre = "Luz eléctrica", CuentaPadre = "5.1.6", Grupo = 5, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.6.3", Nombre = "Teléfono", CuentaPadre = "5.1.6", Grupo = 5, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.7", Nombre = "Suministros de oficina", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "GAD", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.8", Nombre = "Cuentas incobrables", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "GAD", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.9", Nombre = "Depreciación muebles de oficina", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "GAD", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.10", Nombre = "Depreciación equipos de computación", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "GAD", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.1.11", Nombre = "Depreciación de vehículo", CuentaPadre = "5.1", Grupo = 5, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.2", Nombre = "DE VENTA", CuentaPadre = "5", Grupo = 5, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.2.1", Nombre = "Publicidad", CuentaPadre = "5.2", Grupo = 5, GrupoExcel = "GVE", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.3", Nombre = "FINANCIEROS", CuentaPadre = "5", Grupo = 5, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.3.1", Nombre = "Intereses pagados", CuentaPadre = "5.3", Grupo = 5, GrupoExcel = "GFI", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.3.2", Nombre = "Servicios bancarios", CuentaPadre = "5.3", Grupo = 5, GrupoExcel = "GFI", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.4", Nombre = "COSTO DE VENTAS", CuentaPadre = "5", Grupo = 5, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "5.4.1", Nombre = "Costo de ventas", CuentaPadre = "5.4", Grupo = 5, GrupoExcel = "COS", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "6", Nombre = "CUENTAS TRANSITORIAS", CuentaPadre = "", Grupo = 6, GrupoExcel = "", Naturaleza = "Deudora", Tipo = "Grupo", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
            Cuentas.Add(new CuentaContable { Id = ++CuentaId, IdEmpresa = 1, Codigo = "6.1", Nombre = "Resumen de pérdidas y ganancias", CuentaPadre = "6", Grupo = 6, GrupoExcel = "TRA", Naturaleza = "Deudora", Tipo = "Movimiento", DebeInicial = 0m, HaberInicial = 0m, Activa = true });
        }
    }
}
