using System.Collections;
using UnityEngine;

namespace Cripta
{
    public class LlaveAntigua : Interactuable
    {
        [Header("Referencias")]
        public AudioSource fuenteRecoger;

        [Header("Ajustes")]
        public float velocidadGiro = 80f;
        public float amplitudFlote = 0.15f;
        public float velocidadFlote = 2f;
        public float duracionDesvanecido = 0.6f;

        Vector3 origen;
        bool recogida;

        void Start()
        {
            origen = transform.position;
        }

        void Update()
        {
            if (recogida) return;

            transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);
            transform.position = origen + Vector3.up * Mathf.Sin(Time.time * velocidadFlote) * amplitudFlote;
        }

        public override string Mensaje(InventarioJugador inventario)
        {
            return "[E]  Recoger la llave antigua";
        }

        public override void Interactuar(InventarioJugador inventario)
        {
            if (recogida) return;

            recogida = true;
            Activo = false;

            if (inventario != null) inventario.RecogerLlave();
            if (fuenteRecoger != null && fuenteRecoger.clip != null) fuenteRecoger.Play();

            StartCoroutine(AnimarDesvanecido());
        }

        IEnumerator AnimarDesvanecido()
        {
            Vector3 escalaInicial = transform.localScale;
            float t = 0f;

            while (t < duracionDesvanecido)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / duracionDesvanecido);
                transform.localScale = escalaInicial * k;
                transform.Rotate(Vector3.up, 620f * Time.deltaTime, Space.World);
                transform.position += Vector3.up * 1.1f * Time.deltaTime;
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
