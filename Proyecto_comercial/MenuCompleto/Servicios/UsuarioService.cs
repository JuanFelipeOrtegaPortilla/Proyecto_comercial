using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using CooperativaCarchi.Datos;
using CooperativaCarchi.Modelos;

namespace CooperativaCarchi.Servicios
{
    public class UsuarioService
    {
        public bool RequiereAdministrador { get { return DatosTemporales.Ejecutar(() => DatosTemporales.Usuarios.Count == 0); } }
        public Usuario ConsultarSesion(int id)
        { return DatosTemporales.Ejecutar(delegate { var u = DatosTemporales.Usuarios.FirstOrDefault(x => x.Id == id && x.Activo); return u == null ? null : u.Copia<Usuario>(); }); }
        public List<Usuario> Seleccionar(int actor)
        { return DatosTemporales.Ejecutar(delegate { Reglas.Acceso(actor, true); return DatosTemporales.Usuarios.Select(x => x.Copia<Usuario>()).ToList(); }); }
        // Solo debe invocarse desde una solicitud local durante la primera instalación.
        internal void CrearAdministradorInicial(Usuario usuario, string clave, bool solicitudLocal)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Exigir(solicitudLocal && DatosTemporales.Usuarios.Count == 0, "La configuración inicial solo está disponible localmente y una vez.");
                usuario.Rol = Rol.Administrador; usuario.Activo = true; Insertar(usuario, clave);
            });
        }
        public void RegistrarPublico(Usuario usuario, string clave)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Exigir(DatosTemporales.Usuarios.Count > 0, "Primero configure el administrador desde el servidor local.");
                usuario.Rol = Rol.Usuario; usuario.Activo = true; Insertar(usuario, clave);
            });
        }
        public void Guardar(int actor, Usuario usuario, string clave)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, true);
                if (usuario.Id == 0) { Insertar(usuario, clave); return; }
                var actual = DatosTemporales.Usuarios.FirstOrDefault(x => x.Id == usuario.Id);
                Reglas.Exigir(actual != null, "El usuario ya no existe."); Validar(usuario);
                ProtegerUltimo(actual, usuario.Activo && usuario.Rol == Rol.Administrador);
                if (!string.IsNullOrEmpty(clave)) { ValidarClave(clave); DatosTemporales.Claves[usuario.Id] = Hash(clave); }
                DatosTemporales.Usuarios[DatosTemporales.Usuarios.IndexOf(actual)] = usuario.Copia<Usuario>();
            });
        }
        public void Eliminar(int actor, int id)
        {
            DatosTemporales.Ejecutar(delegate {
                Reglas.Acceso(actor, true); var u = DatosTemporales.Usuarios.FirstOrDefault(x => x.Id == id);
                Reglas.Exigir(u != null, "Seleccione un usuario existente."); ProtegerUltimo(u, false);
                DatosTemporales.Usuarios.Remove(u); DatosTemporales.Claves.Remove(id);
            });
        }
        private static void ProtegerUltimo(Usuario actual, bool sigueAdmin)
        { Reglas.Exigir(!(actual.Activo && actual.Rol == Rol.Administrador && !sigueAdmin && DatosTemporales.Usuarios.Count(x => x.Activo && x.Rol == Rol.Administrador) == 1), "No puede eliminar, desactivar o degradar al último administrador activo."); }
        private static void Validar(Usuario u)
        {
            Reglas.Texto(u.Nombres, "Nombres"); Reglas.Texto(u.Apellidos, "Apellidos"); Reglas.Texto(u.NombreUsuario, "Usuario"); Reglas.Correo(u.Correo, true);
            Reglas.Exigir(Enum.IsDefined(typeof(Rol), u.Rol), "Rol inválido.");
            Reglas.Exigir(!DatosTemporales.Usuarios.Any(x => x.Id != u.Id && string.Equals(x.NombreUsuario, u.NombreUsuario.Trim(), StringComparison.OrdinalIgnoreCase)), "El nombre de usuario ya existe.");
            u.NombreUsuario = u.NombreUsuario.Trim();
        }
        private static void Insertar(Usuario u, string clave)
        { Validar(u); ValidarClave(clave); string hash = Hash(clave); u.Id = DatosTemporales.SiguienteId("usuarios"); DatosTemporales.Usuarios.Add(u.Copia<Usuario>()); DatosTemporales.Claves.Add(u.Id, hash); }
        private static void ValidarClave(string clave)
        { Reglas.Exigir(clave != null && clave.Length >= 10 && clave.Length <= 128, "La contraseña debe tener entre 10 y 128 caracteres."); }
        private static string Hash(string clave)
        {
            byte[] sal = new byte[24]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(sal);
            using (var kdf = new Rfc2898DeriveBytes(clave, sal, 120000, HashAlgorithmName.SHA256))
                return Convert.ToBase64String(sal) + ":" + Convert.ToBase64String(kdf.GetBytes(32));
        }
        internal Usuario Autenticar(string nombre, string clave)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrEmpty(clave) || clave.Length > 128) return null;
            return DatosTemporales.Ejecutar(delegate {
                var u = DatosTemporales.Usuarios.FirstOrDefault(x => x.Activo && string.Equals(x.NombreUsuario, nombre.Trim(), StringComparison.OrdinalIgnoreCase));
                if (u == null) return null;
                var partes = DatosTemporales.Claves[u.Id].Split(':');
                using (var kdf = new Rfc2898DeriveBytes(clave, Convert.FromBase64String(partes[0]), 120000, HashAlgorithmName.SHA256))
                {
                    byte[] a = kdf.GetBytes(32), b = Convert.FromBase64String(partes[1]); int diferencia = 0;
                    for (int i = 0; i < a.Length; i++) diferencia |= a[i] ^ b[i];
                    return diferencia == 0 ? u.Copia<Usuario>() : null;
                }
            });
        }
    }
}
