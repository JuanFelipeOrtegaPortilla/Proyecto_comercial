using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CooperativaCarchi.Datos;
using CooperativaCarchi.Modelos;

namespace CooperativaCarchi.Servicios
{
    public class PlanCuentasService
    {
        public List<CuentaContable> Seleccionar(int actor, int empresa, string buscar = "", int grupo = 0)
        {
            return DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, false); Reglas.Empresa(empresa);
                return DatosTemporales.Cuentas.Where(x => x.IdEmpresa == empresa && (grupo == 0 || x.Grupo == grupo) &&
                    (x.Codigo.IndexOf(buscar, StringComparison.OrdinalIgnoreCase) >= 0 || x.Nombre.IndexOf(buscar, StringComparison.OrdinalIgnoreCase) >= 0)).OrderBy(x => x.Codigo).Select(x => x.Copia<CuentaContable>()).ToList();
            });
        }
        public void Guardar(int actor, CuentaContable c)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, true); Reglas.Empresa(c.IdEmpresa); Reglas.Texto(c.Nombre, "Nombre de cuenta");
                Reglas.Exigir(c.Codigo != null && Regex.IsMatch(c.Codigo, @"^[1-6](\.[1-9][0-9]*)*$"), "Código jerárquico inválido: use grupos del 1 al 6 y segmentos sin ceros iniciales.");
                Reglas.Exigir(c.Grupo == int.Parse(c.Codigo.Substring(0, 1)), "El grupo debe coincidir con el primer segmento del código.");
                string padre = c.Codigo.Contains('.') ? c.Codigo.Substring(0, c.Codigo.LastIndexOf('.')) : "";
                Reglas.Exigir((c.CuentaPadre ?? "") == padre, "La cuenta padre debe ser el nivel inmediatamente superior.");
                if (padre != "") Reglas.Exigir(DatosTemporales.Cuentas.Any(x => x.IdEmpresa == c.IdEmpresa && x.Codigo == padre && x.Tipo == "Grupo"), "La cuenta padre debe existir y ser de tipo Grupo.");
                Reglas.Exigir(!DatosTemporales.Cuentas.Any(x => x.Id != c.Id && x.IdEmpresa == c.IdEmpresa && x.Codigo == c.Codigo), "Código duplicado en la empresa.");
                Reglas.Exigir(c.Tipo == "Grupo" || c.Tipo == "Movimiento", "Tipo de cuenta inválido.");
                Reglas.Exigir(c.Naturaleza == "Deudora" || c.Naturaleza == "Acreedora", "Naturaleza inválida.");
                Reglas.Exigir(c.DebeInicial >= 0 && c.HaberInicial >= 0, "Los saldos no pueden ser negativos.");
                Reglas.Exigir(c.Tipo != "Grupo" || (c.DebeInicial == 0 && c.HaberInicial == 0), "Las cuentas de agrupación no admiten saldos de movimientos.");
                Reglas.Exigir(c.DebeInicial == decimal.Round(c.DebeInicial, 2) && c.HaberInicial == decimal.Round(c.HaberInicial, 2), "Use dos decimales en los saldos.");
                string[] excel = { "", "AC", "ANC", "PC", "PNC", "PAT", "ING", "GAD", "GVE", "GFI", "COS", "TRA" };
                int[] equivalencias = { 0, 1, 1, 2, 2, 3, 4, 5, 5, 5, 5, 6 };
                int indice = Array.IndexOf(excel, c.GrupoExcel ?? "");
                Reglas.Exigir(indice >= 0 && (indice == 0 || equivalencias[indice] == c.Grupo), "La clasificación del Excel no corresponde al grupo contable.");
                var actual = DatosTemporales.Cuentas.FirstOrDefault(x => x.Id == c.Id);
                if (c.Id != 0)
                {
                    Reglas.Exigir(actual != null, "La cuenta ya no existe.");
                    bool dependencias = DatosTemporales.Cuentas.Any(x => x.IdEmpresa == actual.IdEmpresa && x.CuentaPadre == actual.Codigo);
                    Reglas.Exigir(!dependencias || (actual.Codigo == c.Codigo && actual.IdEmpresa == c.IdEmpresa && c.Tipo == "Grupo"), "Una cuenta con subcuentas debe conservar empresa, código y tipo Grupo.");
                    Reglas.Exigir(!(actual.TieneMovimientos || DatosTemporales.Lineas.Any(x => x.IdCuenta == actual.Id)) || (actual.Codigo == c.Codigo && actual.IdEmpresa == c.IdEmpresa && c.Tipo == "Movimiento" && actual.DebeInicial == c.DebeInicial && actual.HaberInicial == c.HaberInicial), "Conserve la estructura y saldos de una cuenta utilizada.");
                    c.TieneMovimientos = actual.TieneMovimientos;
                    DatosTemporales.Cuentas[DatosTemporales.Cuentas.IndexOf(actual)] = c.Copia<CuentaContable>();
                }
                else { c.Id = DatosTemporales.SiguienteId("plan_cuentas"); DatosTemporales.Cuentas.Add(c.Copia<CuentaContable>()); }
            });
        }
        public void Eliminar(int actor, int id)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, true); var c = DatosTemporales.Cuentas.FirstOrDefault(x => x.Id == id);
                Reglas.Exigir(c != null, "Seleccione una cuenta existente.");
                Reglas.Exigir(!c.TieneMovimientos && !DatosTemporales.Lineas.Any(x => x.IdCuenta == c.Id) && !DatosTemporales.Cuentas.Any(x => x.IdEmpresa == c.IdEmpresa && x.CuentaPadre == c.Codigo), "La cuenta tiene subcuentas o movimientos relacionados."); DatosTemporales.Cuentas.Remove(c);
            });
        }
    }
}
