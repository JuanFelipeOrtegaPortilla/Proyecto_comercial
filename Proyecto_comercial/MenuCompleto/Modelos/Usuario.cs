using System;

namespace CooperativaCarchi.Modelos
{
    public class Usuario : Entidad
    {
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string NombreUsuario { get; set; }
        public string Correo { get; set; }
        public Rol Rol { get; set; }
        public bool Activo { get; set; }
    }
}
