using UnityEngine;

namespace Cripta
{
    public abstract class Interactuable : MonoBehaviour
    {
        [Header("Interaccion")]
        [SerializeField] protected float radio = 2.8f;

        public float Radio { get { return radio; } }
        public bool Activo = true;

        public abstract string Mensaje(InventarioJugador inventario);

        public abstract void Interactuar(InventarioJugador inventario);
    }
}
