using UnityEngine;

namespace Cripta
{
    public class ZonaTrigger : MonoBehaviour
    {
        public string mensaje = string.Empty;
        public float duracion = 3.5f;
        public bool unaSolaVez = true;

        bool usado;

        void OnTriggerEnter(Collider other)
        {
            if (usado && unaSolaVez) return;
            if (other.GetComponentInParent<InventarioJugador>() == null) return;
            if (string.IsNullOrEmpty(mensaje)) return;

            usado = true;
            HUDNivel.Mostrar(mensaje, duracion);
        }
    }
}
