using System.Collections;
using UnityEngine;

namespace Cripta
{
    public class PuertaGate : Interactuable
    {
        [Header("Referencias")]
        public Transform pivote;
        public AudioSource fuenteAbrir;
        public AudioSource fuenteTrabada;

        [Header("Ajustes")]
        public float anguloApertura = 92f;
        public float duracionApertura = 2f;
        public string mensajeBloqueo = "La puerta esta cerrada. Necesitas la llave antigua.";

        bool abierta;
        bool abriendo;

        public bool Abierta { get { return abierta; } }

        public override string Mensaje(InventarioJugador inventario)
        {
            if (abierta || abriendo) return string.Empty;
            bool conLlave = inventario != null && inventario.TieneLlave;
            return conLlave ? "[E]  Abrir la puerta con la llave" : "[E]  Intentar abrir la puerta";
        }

        public override void Interactuar(InventarioJugador inventario)
        {
            if (abierta || abriendo) return;

            if (inventario == null || !inventario.TieneLlave)
            {
                if (fuenteTrabada != null && fuenteTrabada.clip != null) fuenteTrabada.Play();
                HUDNivel.Mostrar(mensajeBloqueo, 2.5f);
                return;
            }

            abriendo = true;
            if (fuenteAbrir != null && fuenteAbrir.clip != null) fuenteAbrir.Play();
            StartCoroutine(AnimarApertura());
        }

        IEnumerator AnimarApertura()
        {
            if (pivote == null)
            {
                abierta = true;
                abriendo = false;
                Activo = false;
                yield break;
            }

            Quaternion inicio = pivote.localRotation;
            Quaternion fin = inicio * Quaternion.Euler(0f, anguloApertura, 0f);
            float t = 0f;

            while (t < duracionApertura)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracionApertura));
                pivote.localRotation = Quaternion.Slerp(inicio, fin, k);
                yield return null;
            }

            pivote.localRotation = fin;
            abierta = true;
            abriendo = false;
            Activo = false;
            HUDNivel.Mostrar("La puerta cede. La salida esta abierta.", 3f);
        }
    }
}
