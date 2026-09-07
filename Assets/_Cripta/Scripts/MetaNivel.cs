using UnityEngine;

namespace Cripta
{
    public class MetaNivel : MonoBehaviour
    {
        public AudioSource fuenteFinal;

        bool completado;

        void OnTriggerEnter(Collider other)
        {
            if (completado) return;

            InventarioJugador inventario = other.GetComponentInParent<InventarioJugador>();
            if (inventario == null) return;

            completado = true;
            inventario.CompletarNivel();

            if (fuenteFinal != null && fuenteFinal.clip != null) fuenteFinal.Play();
        }
    }
}
