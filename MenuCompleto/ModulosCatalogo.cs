using System;
using System.Collections.Generic;
using System.Linq;

namespace CooperativaCarchi
{
    public class ModuloInfo
    {
        public string Id { get; set; }
        public int Numero { get; set; }
        public string Titulo { get; set; }
        public string Hoja { get; set; }
        public string Resumen { get; set; }
        public string Fuente { get; set; }
        public string Destino { get; set; }
        public string Regla { get; set; }
        public string Icono { get; set; }
        public string[] Puntos { get; set; }
        public string GrupoId { get; set; }
        public string GrupoTitulo { get; set; }
    }

    public class GrupoModulos
    {
        public string Id { get; set; }
        public string Letra { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public List<ModuloInfo> Modulos { get; set; }
    }

    // Catalogo unico de modulos. Refleja las hojas del Excel
    // y el flujo de la hoja "Guia de integracion software".
    public static class ModulosCatalogo
    {
        private static readonly List<GrupoModulos> grupos = Construir();

        public static List<GrupoModulos> ObtenerGrupos()
        {
            return grupos;
        }

        public static List<ModuloInfo> Todos()
        {
            return grupos.SelectMany(g => g.Modulos).ToList();
        }

        public static ModuloInfo Buscar(string id)
        {
            return Todos().FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public static bool Disponible(string id) { return id == "plan" || id == "iva" || id == "transacciones" || id == "diario" || id == "mayor"; }
        public static string Ruta(string id)
        {
            if (id == "transacciones") return "Transacciones.aspx";
            if (id == "diario") return "LibroDiario.aspx";
            if (id == "mayor") return "LibroMayor.aspx";
            if (id == "plan") return "PlanCuentas.aspx";
            if (id == "iva") return "Empresa.aspx";
            return "modulo.aspx?id=" + System.Web.HttpUtility.UrlEncode(id);
        }

        private static List<GrupoModulos> Construir()
        {
            var lista = new List<GrupoModulos>();

            lista.Add(G("entradas", "A", "Datos de entrada",
                "Donde nace todo el ciclo. Se digita una sola vez.",
                M(1, "transacciones", "Transacciones", "Transacciones",
                    "Fuente unica de datos: operaciones de diciembre, registro maestro DAT y detalle MOV.",
                    "Celdas amarillas de la hoja Transacciones (A78:H199 y J78:S98)",
                    "Plan de Cuentas, Kardex e IVA",
                    "Solo se editan las celdas amarillas. Cada dato se identifica con DAT-xxx o MOV-xxx, nunca por numero de fila.",
                    "<path d='M4 8h14m-4-4 4 4-4 4'/><path d='M20 16H6m4-4-4 4 4 4'/>",
                    "Operaciones del 1 al 31 de diciembre",
                    "IVA 15% sin retenciones de impuesto a la renta ni de IVA",
                    "Inventario permanente",
                    "10 controles con diferencia esperada 0,00"),
                M(2, "plan", "Plan de Cuentas", "Plan de Cuentas",
                    "44 cuentas agrupadas por tipo, con saldos iniciales y parametros del caso.",
                    "Transacciones A78:H199 (identificadores DAT-xxx)",
                    "Diario, IVA y Balance de comprobacion",
                    "Los saldos iniciales y parametros se leen desde Transacciones. No se editan aqui.",
                    "<path d='M8 6h13M8 12h13M8 18h13'/><path d='M3 6h.01M3 12h.01M3 18h.01'/>",
                    "Grupos: AC, ANC, PC, PNC, PAT, ING, GAD, GVE, GFI, COS y TRA",
                    "Debe inicial y haber inicial de cada cuenta",
                    "Parametros: IVA, depreciaciones, aporte patronal y provision"),
                M(3, "origen", "Origen de valores", "Origen de valores",
                    "Trazabilidad: de que hoja y celda sale cada importe del ciclo.",
                    "Todas las hojas del ciclo contable",
                    "Auditoria y consulta",
                    "Las entradas se registran solo en Transacciones. Azul con fondo amarillo indica dato de entrada.",
                    "<circle cx='11' cy='11' r='6'/><path d='m20 20-4.2-4.2'/>",
                    "Cada importe indica su hoja y celda de origen",
                    "Ayuda a explicar cualquier cifra a un auditor")));

            lista.Add(G("registro", "B", "Registro contable",
                "Calculos automaticos a partir de los datos de entrada.",
                M(4, "kardex", "Kardex", "Kardex",
                    "Control de existencias por promedio ponderado de tres articulos.",
                    "Transacciones J78:S98 (identificadores MOV-xxx)",
                    "IVA y Diario",
                    "Las entradas van al costo de compra y las salidas al costo promedio vigente.",
                    "<path d='M21 8 12 3 3 8v8l9 5 9-5Z'/><path d='m3 8 9 5 9-5M12 13v8'/>",
                    "Refrigeradora Ecasa 12 pies, televisor Sony 14 pulgadas y cocina Ecasa 4 quemadores",
                    "Saldo inicial, compra, devolucion de compra y ventas",
                    "Cada movimiento muestra unidades, costo unitario y valor total"),
                M(5, "iva", "IVA", "IVA",
                    "Liquidacion del IVA al 15% por cada operacion de compra, venta o devolucion.",
                    "Kardex y parametros del Plan de Cuentas",
                    "Diario (IVA compras e IVA ventas)",
                    "La base imponible sale de compras, ventas y devoluciones. La tarifa se toma del Plan de Cuentas.",
                    "<path d='m19 5-14 14'/><circle cx='7' cy='7' r='2.5'/><circle cx='17' cy='17' r='2.5'/>",
                    "Columnas: base, IVA 15% y total neto",
                    "Retenciones de renta e IVA marcadas como no aplica"),
                M(6, "diario", "Libro Diario", "Diario",
                    "29 asientos con apertura, operaciones y ajustes, siempre con debe y haber.",
                    "Plan de Cuentas, IVA y Kardex",
                    "Libro Mayor y Balance de comprobacion",
                    "Asientos automaticos. Cada debe y haber es rastreable hasta su origen.",
                    "<path d='M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3Z'/><path d='M5 17a3 3 0 0 1 3-3h11'/>",
                    "Asiento 1: apertura con saldos iniciales",
                    "Columnas: asiento, dia, tipo, codigo, cuenta, debe, haber y glosa"),
                M(7, "mayor", "Libro Mayor", "Mayor",
                    "Movimientos agrupados por cuenta con saldo deudor neto acumulado.",
                    "Libro Diario",
                    "Balance de comprobacion",
                    "Agrupacion por codigo de cuenta y asiento.",
                    "<rect x='3' y='4' width='18' height='16' rx='2'/><path d='M3 10h18M9 4v16'/>",
                    "Cada linea apunta a su asiento del Diario",
                    "El saldo se acumula linea a linea")));

            lista.Add(G("balances", "C", "Balances y ajustes",
                "Saldos antes y despues de los ajustes de cierre.",
                M(8, "balance", "Balance de Comprobacion Ajustado", "Balance Comprobacion Ajustado",
                    "Saldos previos, ajustes y saldos ajustados de todas las cuentas.",
                    "Libro Diario y Libro Mayor",
                    "Estado de resultados y Situacion financiera",
                    "Toma los saldos y aplica los ajustes de cierre.",
                    "<path d='M12 4v16M6 20h12M5 8h14'/><path d='m5 8-3 6a3 3 0 0 0 6 0Z'/><path d='m19 8-3 6a3 3 0 0 0 6 0Z'/>",
                    "Debe y haber previos, saldo deudor y saldo acreedor",
                    "Ajustes: publicidad pendiente, comisiones por cobrar, provision y depreciaciones")));

            lista.Add(G("estados", "D", "Estados financieros",
                "Informacion financiera derivada del balance ajustado.",
                M(9, "resultados", "Estado de Resultados", "Resultados",
                    "Ventas, costo de ventas, gastos y utilidad del ejercicio.",
                    "Balance de comprobacion ajustado",
                    "Cierre y Postcierre",
                    "No incluye participacion de trabajadores ni impuesto a la renta, porque el enunciado no los contempla.",
                    "<path d='M4 20V10M10 20V4M16 20v-7M3 20h18'/>",
                    "Utilidad bruta, utilidad operativa y utilidad del ejercicio",
                    "Detalle de gastos de administracion"),
                M(10, "situacion", "Situacion Financiera", "Situacion financiera",
                    "Activo, pasivo y patrimonio con el control de la ecuacion contable.",
                    "Balance de comprobacion ajustado",
                    "Cierre y Postcierre",
                    "Activo = Pasivo + Patrimonio. La diferencia debe ser 0,00.",
                    "<path d='M3 10 12 4l9 6'/><path d='M5 10v8M10 10v8M14 10v8M19 10v8M3 20h18'/>",
                    "Activo corriente y no corriente",
                    "Pasivo corriente y no corriente",
                    "Patrimonio con la utilidad del ejercicio")));

            lista.Add(G("cierre", "E", "Cierre del ejercicio",
                "Cuentas de resultado en cero y balance final.",
                M(11, "cierre", "Asientos de Cierre", "Cierre",
                    "Cierre de ingresos, gastos y costos contra la cuenta Resumen de perdidas y ganancias.",
                    "Estado de resultados y balance ajustado",
                    "Balance posterior al cierre",
                    "Los asientos 30 y 31 dejan en cero las cuentas de resultado.",
                    "<rect x='5' y='11' width='14' height='9' rx='2'/><path d='M8 11V8a4 4 0 0 1 8 0v3'/>",
                    "Asiento 30: cierre de ingresos",
                    "Asiento 31: cierre de gastos y costos"),
                M(12, "postcierre", "Balance Postcierre", "Postcierre",
                    "Balance final con las cuentas que permanecen abiertas.",
                    "Cierre y balance ajustado",
                    "Apertura del siguiente periodo",
                    "Total debe igual a total haber.",
                    "<circle cx='12' cy='12' r='9'/><path d='m8 12 3 3 5-6'/>",
                    "Solo cuentas de situacion financiera",
                    "Incluye la utilidad del ejercicio en patrimonio")));

            lista.Add(G("apoyo", "F", "Apoyo y desarrollo",
                "Herramientas para decidir y para integrar el software.",
                M(13, "ia", "IA toma decisiones", "IA toma decisiones",
                    "Espacio reservado para recomendaciones a partir de los estados financieros.",
                    "Estados financieros",
                    "Gerencia",
                    "La hoja esta vacia en el Excel. Queda lista para una fase futura.",
                    "<path d='M12 3v4M12 17v4M3 12h4M17 12h4'/><path d='m7 7 2 2M15 15l2 2M17 7l-2 2M9 15l-2 2'/>",
                    "Modulo reservado",
                    "Pensado para analizar liquidez, margen y utilidad"),
                M(14, "guia", "Guia de integracion", "Guia de integracion software",
                    "Pasos y entidades sugeridas para pasar el Excel a base de datos.",
                    "Hoja Guia de integracion software",
                    "Equipo de desarrollo",
                    "Usar DAT-xxx y MOV-xxx como claves externas. No usar el numero de fila.",
                    "<path d='M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1'/><path d='M14 10a4 4 0 0 0-5.7 0l-3 3A4 4 0 0 0 11 18.7l1-1'/>",
                    "Entidad Transaccion con su DetalleTransaccion",
                    "Entidad AsientoDetalle para la trazabilidad contable",
                    "Entidad InventarioMovimiento para las existencias")));

            return lista;
        }

        private static GrupoModulos G(string id, string letra, string titulo, string descripcion, params ModuloInfo[] modulos)
        {
            foreach (var m in modulos)
            {
                m.GrupoId = id;
                m.GrupoTitulo = titulo;
            }

            return new GrupoModulos
            {
                Id = id,
                Letra = letra,
                Titulo = titulo,
                Descripcion = descripcion,
                Modulos = modulos.ToList()
            };
        }

        private static ModuloInfo M(int numero, string id, string titulo, string hoja, string resumen,
            string fuente, string destino, string regla, string icono, params string[] puntos)
        {
            return new ModuloInfo
            {
                Numero = numero,
                Id = id,
                Titulo = titulo,
                Hoja = hoja,
                Resumen = resumen,
                Fuente = fuente,
                Destino = destino,
                Regla = regla,
                Icono = icono,
                Puntos = puntos
            };
        }
    }
}