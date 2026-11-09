using System;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using CooperativaCarchi.Datos;
using CooperativaCarchi.Modelos;

namespace CooperativaCarchi.Servicios
{
    internal static class Reglas
    {
        internal static void Exigir(bool condicion, string mensaje)
        { if (!condicion) throw new InvalidOperationException(mensaje); }
        internal static Usuario Acceso(int actor, bool admin)
        {
            var u = DatosTemporales.Usuarios.FirstOrDefault(x => x.Id == actor && x.Activo);
            Exigir(u != null && (!admin || u.Rol == Rol.Administrador), "No tiene autorización para esta operación.");
            return u;
        }
        internal static void Texto(string valor, string campo)
        { Exigir(!string.IsNullOrWhiteSpace(valor) && valor.Length <= 250, campo + " es obligatorio (máximo 250 caracteres)."); }
        internal static void Correo(string valor, bool obligatorio)
        {
            if (!obligatorio && string.IsNullOrWhiteSpace(valor)) return;
            try { Exigir(new MailAddress(valor).Address == valor, "Correo inválido."); }
            catch (ArgumentException) { throw new InvalidOperationException("Correo inválido."); }
            catch (FormatException) { throw new InvalidOperationException("Correo inválido."); }
        }
        internal static void Empresa(int id)
        { Exigir(DatosTemporales.Empresas.Any(x => x.Id == id), "Seleccione una empresa existente."); }
        internal static void Ruc(string ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc)) return;
            Exigir(Regex.IsMatch(ruc, @"^\d{13}$"), "El RUC debe tener 13 dígitos.");
            int provincia = int.Parse(ruc.Substring(0, 2)), tercero = ruc[2] - '0';
            Exigir((provincia >= 1 && provincia <= 24) || provincia == 30, "Provincia del RUC inválida.");
            int[] pesos; int modulo, posicion;
            if (tercero < 6) { pesos = new[] { 2,1,2,1,2,1,2,1,2 }; modulo = 10; posicion = 9; }
            else if (tercero == 6) { pesos = new[] { 3,2,7,6,5,4,3,2 }; modulo = 11; posicion = 8; }
            else { Exigir(tercero == 9, "Tipo de RUC inválido."); pesos = new[] { 4,3,2,7,6,5,4,3,2 }; modulo = 11; posicion = 9; }
            int suma = 0;
            for (int i = 0; i < pesos.Length; i++) { int n = (ruc[i] - '0') * pesos[i]; suma += modulo == 10 && n > 9 ? n - 9 : n; }
            int digito = (modulo - suma % modulo) % modulo;
            Exigir(digito == ruc[posicion] - '0' && int.Parse(ruc.Substring(tercero == 6 ? 9 : 10)) > 0,
                "Dígito verificador o establecimiento del RUC inválido.");
        }
    }
}
