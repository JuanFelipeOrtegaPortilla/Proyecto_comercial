using System;
using System.Collections.Generic;
using System.Linq;
using CooperativaCarchi.Datos;
using CooperativaCarchi.Modelos;

namespace CooperativaCarchi.Servicios
{
    public class EmpresaService
    {
        public List<Empresa> Seleccionar(int actor, string buscar = "")
        { return DatosTemporales.Ejecutar(delegate { Reglas.Acceso(actor, false); return DatosTemporales.Empresas.Where(x => (x.RUC ?? "").IndexOf(buscar, StringComparison.OrdinalIgnoreCase) >= 0 || x.RazonSocial.IndexOf(buscar, StringComparison.OrdinalIgnoreCase) >= 0).Select(x => x.Copia<Empresa>()).ToList(); }); }
        public void Guardar(int actor, Empresa empresa)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, true); Reglas.Texto(empresa.RazonSocial, "Razón social");
                empresa.RUC = (empresa.RUC ?? "").Trim(); Reglas.Ruc(empresa.RUC); Reglas.Correo(empresa.Correo, false);
                Reglas.Exigir(empresa.RUC == "" || !DatosTemporales.Empresas.Any(x => x.Id != empresa.Id && x.RUC == empresa.RUC), "El RUC ya está registrado.");
                Reglas.Exigir(empresa.FechaRegistro != DateTime.MinValue, "Fecha de registro inválida.");
                if (empresa.Id == 0) { empresa.Id = DatosTemporales.SiguienteId("empresas"); DatosTemporales.Empresas.Add(empresa.Copia<Empresa>()); }
                else { int i = DatosTemporales.Empresas.FindIndex(x => x.Id == empresa.Id); Reglas.Exigir(i >= 0, "La empresa ya no existe."); DatosTemporales.Empresas[i] = empresa.Copia<Empresa>(); }
            });
        }
        public void Eliminar(int actor, int id)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, true); Reglas.Empresa(id);
                Reglas.Exigir(!DatosTemporales.Cuentas.Any(x => x.IdEmpresa == id) && !DatosTemporales.Tarifas.Any(x => x.IdEmpresa == id), "La empresa tiene cuentas o tarifas asociadas.");
                DatosTemporales.Empresas.RemoveAll(x => x.Id == id);
            });
        }
    }
}
