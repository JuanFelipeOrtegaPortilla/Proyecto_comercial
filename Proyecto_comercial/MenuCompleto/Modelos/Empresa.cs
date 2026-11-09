using System;

namespace CooperativaCarchi.Modelos
{
    public class Empresa : Entidad
    {
        public string RUC { get; set; }
        public string RazonSocial { get; set; }
        public string NombreComercial { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string RepresentanteLegal { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Activa { get; set; }
        public string Moneda { get { return "USD"; } }
    }
}
