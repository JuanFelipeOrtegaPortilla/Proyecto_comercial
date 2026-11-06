using System;

namespace CooperativaCarchi.Modelos
{
    [Serializable]
    public abstract class Entidad
    {
        public int Id { get; set; }
        public T Copia<T>() where T : Entidad { return (T)MemberwiseClone(); }
    }
}
