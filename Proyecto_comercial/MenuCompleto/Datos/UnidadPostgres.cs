using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using CooperativaCarchi.Modelos;
using Npgsql;

namespace CooperativaCarchi.Datos
{
    // Adaptador transaccional del mantenimiento existente. PostgreSQL es la fuente de verdad:
    // cada operación lee de nuevo y solo guarda las filas modificadas; no hay caché persistente.
    // Adecuado para este catálogo pequeño. Para grandes volúmenes sustituir por consultas por entidad.
    internal sealed class UnidadPostgres : IDisposable
    {
        [ThreadStatic] private static UnidadPostgres actual;
        private readonly NpgsqlConnection conexion;
        private readonly NpgsqlTransaction transaccion;
        private readonly List<Mapa> mapas = new List<Mapa>();
        private readonly Dictionary<string, Dictionary<int, string>> originales = new Dictionary<string, Dictionary<int, string>>();
        private class Mapa
        {
            internal string Tabla, Clave; internal Type Tipo; internal IList Lista;
            internal string[] Propiedades, Columnas;
        }
        private UnidadPostgres()
        {
            string cs = Environment.GetEnvironmentVariable("CARCHI_POSTGRES_CONNECTION");
            if (string.IsNullOrWhiteSpace(cs)) cs = ConfigurationManager.ConnectionStrings["Contabilidad"] == null ? null : ConfigurationManager.ConnectionStrings["Contabilidad"].ConnectionString;
            if (string.IsNullOrWhiteSpace(cs)) throw new InvalidOperationException("Configure la conexión local de PostgreSQL antes de activar este modo.");
            conexion = new NpgsqlConnection(cs);
            try
            {
                conexion.Open(); transaccion = conexion.BeginTransaction(IsolationLevel.ReadCommitted);
                // El mismo cerrojo se utiliza en los triggers SQL: protege varios procesos web.
                EjecutarSql("SELECT pg_advisory_xact_lock(72410301)");
                Mapear<Usuario>("usuarios", "id_usuario", DatosTemporales.Usuarios,
                    "Id,Nombres,Apellidos,NombreUsuario,Correo,Activo", "id_usuario,nombres,apellidos,nombre_usuario,correo,estado");
                Mapear<Empresa>("empresas", "id_empresa", DatosTemporales.Empresas,
                    "Id,RUC,RazonSocial,NombreComercial,Direccion,Telefono,Correo,RepresentanteLegal,FechaRegistro,Activa",
                    "id_empresa,ruc,razon_social,nombre_comercial,direccion,telefono,correo,representante_legal,fecha_registro,estado");
                Mapear<CuentaContable>("plan_cuentas", "id_cuenta", DatosTemporales.Cuentas,
                    "Id,IdEmpresa,Codigo,Nombre,Grupo,CuentaPadre,GrupoExcel,Naturaleza,Tipo,DebeInicial,HaberInicial,Activa,TieneMovimientos",
                    "id_cuenta,id_empresa,codigo_cuenta,nombre_cuenta,grupo_contable,codigo_cuenta_padre,grupo_excel,naturaleza,tipo_cuenta,debe_inicial,haber_inicial,estado,tiene_movimientos");
                Mapear<TarifaIVA>("tarifas_iva", "id_tarifa", DatosTemporales.Tarifas,
                    "Id,IdEmpresa,Porcentaje,FechaInicio,FechaFin,Descripcion,Utilizada,UltimaFechaUso",
                    "id_tarifa,id_empresa,porcentaje,fecha_inicio,fecha_fin,descripcion,utilizada,ultima_fecha_uso");
                using (var comprobar = Comando("SELECT to_regclass('public.transacciones') IS NOT NULL AND to_regclass('public.lineas_asiento') IS NOT NULL"))
                    DatosTemporales.ModuloContableInstalado = (bool)comprobar.ExecuteScalar();
                DatosTemporales.Transacciones.Clear(); DatosTemporales.Lineas.Clear();
                if (DatosTemporales.ModuloContableInstalado)
                {
                    Mapear<Transaccion>("transacciones", "id_transaccion", DatosTemporales.Transacciones,
                        "Id,IdEmpresa,Fecha,Glosa,Tipo,Estado,Tratamiento,Subtotal,IdTarifa,PorcentajeAplicado,IVA,Total,CuentaBase,CuentaIVA,Contrapartida",
                        "id_transaccion,id_empresa,fecha,glosa,tipo,estado,tratamiento,subtotal,id_tarifa,porcentaje_aplicado,iva,total,cuenta_base,cuenta_iva,contrapartida");
                    Mapear<LineaAsiento>("lineas_asiento", "id_linea", DatosTemporales.Lineas,
                        "Id,IdTransaccion,IdCuenta,Debe,Haber", "id_linea,id_transaccion,id_cuenta,debe,haber");
                }
                foreach (var m in mapas) Cargar(m);
                DatosTemporales.Claves.Clear();
                using (var cmd = Comando("SELECT u.id_usuario,u.password_hash,r.nombre_rol FROM usuarios u JOIN usuario_roles ur ON ur.id_usuario=u.id_usuario JOIN roles r ON r.id_rol=ur.id_rol"))
                using (var lector = cmd.ExecuteReader())
                    while (lector.Read())
                    {
                        int id = lector.GetInt32(0); DatosTemporales.Claves[id] = lector.GetString(1);
                        DatosTemporales.Usuarios.Single(x => x.Id == id).Rol = (Rol)Enum.Parse(typeof(Rol), lector.GetString(2));
                    }
                if (DatosTemporales.Usuarios.Count != DatosTemporales.Claves.Count) throw new InvalidOperationException("Hay usuarios sin rol. Revise la integridad de la base de datos.");
                foreach (var m in mapas) originales[m.Tabla] = m.Lista.Cast<Entidad>().ToDictionary(x => x.Id, x => Firma(m, x));
            }
            catch { Dispose(); throw; }
        }
        internal static T Ejecutar<T>(Func<T> accion)
        {
            if (actual != null) return accion();
            try
            {
                using (var unidad = new UnidadPostgres())
                {
                    actual = unidad;
                    try { T resultado = accion(); unidad.Guardar(); unidad.transaccion.Commit(); return resultado; }
                    finally { actual = null; }
                }
            }
            catch (PostgresException ex)
            {
                string mensaje = ex.SqlState == "23505" ? "Ya existe un registro con ese identificador, usuario o código." :
                    ex.SqlState == "23503" ? "El registro tiene dependencias o hace referencia a un registro inexistente." :
                    ex.SqlState == "23514" || ex.SqlState == "23P01" ? "La operación incumple las reglas de integridad: revise jerarquías, vigencias y dependencias." :
                    "No se pudo completar la operación de PostgreSQL. Verifique el esquema y la configuración local.";
                throw new InvalidOperationException(mensaje);
            }
            catch (NpgsqlException) { throw new InvalidOperationException("No se pudo conectar con PostgreSQL. Verifique el servidor, la base de datos y la configuración local."); }
        }
        internal static int SiguienteId(string tabla)
        {
            var m = actual.mapas.Single(x => x.Tabla == tabla);
            using (var cmd = actual.Comando("SELECT nextval(pg_get_serial_sequence(@tabla,@clave))"))
            { cmd.Parameters.AddWithValue("tabla", m.Tabla); cmd.Parameters.AddWithValue("clave", m.Clave); return Convert.ToInt32(cmd.ExecuteScalar()); }
        }
        private void Mapear<T>(string tabla, string clave, IList lista, string propiedades, string columnas)
        { mapas.Add(new Mapa { Tabla = tabla, Clave = clave, Lista = lista, Tipo = typeof(T), Propiedades = propiedades.Split(','), Columnas = columnas.Split(',') }); }
        private NpgsqlCommand Comando(string sql) { return new NpgsqlCommand(sql, conexion, transaccion); }
        private void EjecutarSql(string sql) { using (var c = Comando(sql)) c.ExecuteNonQuery(); }
        private static PropertyInfo Propiedad(Mapa m, string nombre) { return m.Tipo.GetProperty(nombre, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); }
        private void Cargar(Mapa m)
        {
            m.Lista.Clear();
            using (var c = Comando("SELECT " + string.Join(",", m.Columnas) + " FROM " + m.Tabla))
            using (var r = c.ExecuteReader())
                while (r.Read())
                {
                    var item = Activator.CreateInstance(m.Tipo);
                    for (int i = 0; i < m.Propiedades.Length; i++)
                    {
                        var p = Propiedad(m, m.Propiedades[i]); object valor = r.IsDBNull(i) ? null : r.GetValue(i);
                        if (valor == null && p.PropertyType == typeof(string)) valor = "";
                        p.SetValue(item, valor, null);
                    }
                    m.Lista.Add(item);
                }
        }
        private string Firma(Mapa m, Entidad fila)
        {
            var valores = m.Propiedades.Select(p => Propiedad(m, p).GetValue(fila, null)).ToList();
            if (fila is Usuario) { valores.Add(((Usuario)fila).Rol); valores.Add(DatosTemporales.Claves[fila.Id]); }
            return new JavaScriptSerializer().Serialize(valores);
        }
        private void Guardar()
        {
            // Bajas de hijos antes de padres. Las FK RESTRICT impiden pérdidas accidentales.
            foreach (var m in mapas.AsEnumerable().Reverse())
                foreach (int id in originales[m.Tabla].Keys.Except(m.Lista.Cast<Entidad>().Select(x => x.Id)))
                {
                    if (m.Tabla == "usuarios") using (var c = Comando("DELETE FROM usuario_roles WHERE id_usuario=@id")) { c.Parameters.AddWithValue("id", id); c.ExecuteNonQuery(); }
                    using (var c = Comando("DELETE FROM " + m.Tabla + " WHERE " + m.Clave + "=@id")) { c.Parameters.AddWithValue("id", id); c.ExecuteNonQuery(); }
                }
            foreach (var m in mapas)
                foreach (var fila in m.Lista.Cast<Entidad>().OrderBy(x => x is CuentaContable ? ((CuentaContable)x).Nivel : 0))
                {
                    string previa;
                    if (originales[m.Tabla].TryGetValue(fila.Id, out previa) && previa == Firma(m, fila)) continue;
                    var columnas = m.Columnas.ToList(); var valores = m.Propiedades.Select(p => Propiedad(m, p).GetValue(fila, null)).ToList();
                    if (fila is Usuario) { columnas.Add("password_hash"); valores.Add(DatosTemporales.Claves[fila.Id]); }
                    // NULL para claves opcionales, no una cadena vacía que rompa unicidad/FK.
                    for (int i = 0; i < columnas.Count; i++) if ((columnas[i] == "ruc" || columnas[i] == "codigo_cuenta_padre") && string.IsNullOrEmpty(valores[i] as string)) valores[i] = null;
                    for (int i = 0; i < columnas.Count; i++)
                        if ((columnas[i] == "cuenta_base" || columnas[i] == "contrapartida") && valores[i] is int && (int)valores[i] == 0) valores[i] = null;
                    string sql = "INSERT INTO " + m.Tabla + " (" + string.Join(",", columnas) + ") VALUES (" + string.Join(",", valores.Select((v, i) => "@p" + i)) + ") ON CONFLICT (" + m.Clave + ") DO UPDATE SET " +
                        string.Join(",", columnas.Skip(1).Select(c => c + "=EXCLUDED." + c));
                    using (var c = Comando(sql))
                    {
                        for (int i = 0; i < valores.Count; i++) c.Parameters.AddWithValue("p" + i, valores[i] ?? DBNull.Value);
                        c.ExecuteNonQuery();
                    }
                    if (fila is Usuario)
                    {
                        using (var c = Comando("INSERT INTO usuario_roles(id_usuario,id_rol) SELECT @id,id_rol FROM roles WHERE nombre_rol=@rol ON CONFLICT(id_usuario) DO UPDATE SET id_rol=EXCLUDED.id_rol"))
                        { c.Parameters.AddWithValue("id", fila.Id); c.Parameters.AddWithValue("rol", ((Usuario)fila).Rol.ToString()); c.ExecuteNonQuery(); }
                    }
                }
        }
        public void Dispose()
        { if (transaccion != null) transaccion.Dispose(); if (conexion != null) conexion.Dispose(); }
    }
}
