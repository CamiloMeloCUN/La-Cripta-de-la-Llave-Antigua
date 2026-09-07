using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cripta
{
    public class Antorcha : Interactuable
    {
        static readonly List<Antorcha> registro = new List<Antorcha>();
        static int total;

        public static int Total { get { return total; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarRegistro()
        {
            registro.Clear();
            total = 0;
        }

        [Header("Referencias")]
        public Light luz;
        public Transform llama;
        public AudioSource fuenteFuego;

        [Header("Ajustes")]
        public float intensidadFinal = 4f;
        public float escalaLlama = 0.45f;
        public float duracionEncendido = 0.7f;

        [Header("Encendido en cadena")]
        public bool enciendeLasDemas;
        public float retrasoCadena = 0.35f;
        public string mensajeCadena = "El fuego se propaga: la cripta entera se ilumina.";

        [Header("Reposo y desaparicion")]
        public bool desapareceAlUsarse;
        public float intensidadReposo = 1.6f;
        public float velocidadGiro = 80f;
        public float amplitudFlote = 0.15f;
        public float velocidadFlote = 2f;
        public float duracionDesvanecido = 0.6f;

        bool encendida;
        bool estable;
        bool desapareciendo;
        bool contada;
        Vector3 origen;

        public bool Encendida { get { return encendida; } }

        void Awake()
        {
            if (luz != null) luz.intensity = 0f;
            if (llama != null) llama.localScale = Vector3.zero;
        }

        void Start()
        {
            origen = transform.position;

            if (desapareceAlUsarse)
            {
                if (llama != null) llama.localScale = Vector3.one * escalaLlama * 0.7f;
                if (luz != null) luz.intensity = intensidadReposo;
            }
        }

        void OnEnable()
        {
            if (!registro.Contains(this)) registro.Add(this);

            if (!contada)
            {
                contada = true;
                total++;
            }
        }

        void OnDisable()
        {
            registro.Remove(this);
        }

        void Update()
        {
            if (desapareceAlUsarse && !desapareciendo)
            {
                transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);
                transform.position = origen + Vector3.up * Mathf.Sin(Time.time * velocidadFlote) * amplitudFlote;

                if (luz != null) luz.intensity = intensidadReposo * (0.9f + Mathf.Sin(Time.time * 6f) * 0.1f);
                return;
            }

            if (!estable) return;

            float parpadeo = 0.9f + Mathf.Sin(Time.time * 11f) * 0.1f;
            if (luz != null) luz.intensity = intensidadFinal * parpadeo;
            if (llama != null) llama.localScale = Vector3.one * escalaLlama * (0.92f + Mathf.Sin(Time.time * 9f) * 0.08f);
        }

        public override string Mensaje(InventarioJugador inventario)
        {
            if (encendida) return string.Empty;
            return enciendeLasDemas ? "[E]  Encender el brasero" : "[E]  Encender la antorcha";
        }

        public override void Interactuar(InventarioJugador inventario)
        {
            if (encendida) return;

            if (desapareceAlUsarse)
            {
                encendida = true;
                Activo = false;

                if (inventario != null) inventario.RegistrarAntorcha();
                if (fuenteFuego != null && fuenteFuego.clip != null) fuenteFuego.Play();
                if (enciendeLasDemas && !string.IsNullOrEmpty(mensajeCadena)) HUDNivel.Mostrar(mensajeCadena, 3.5f);

                StartCoroutine(CadenaYDesvanecido(inventario));
                return;
            }

            Encender(inventario);

            if (enciendeLasDemas)
            {
                if (!string.IsNullOrEmpty(mensajeCadena)) HUDNivel.Mostrar(mensajeCadena, 3.5f);
                StartCoroutine(EncenderLasDemas(inventario));
            }
        }

        public void Encender(InventarioJugador inventario)
        {
            if (encendida) return;

            encendida = true;
            Activo = false;

            if (inventario != null) inventario.RegistrarAntorcha();
            if (fuenteFuego != null && fuenteFuego.clip != null) fuenteFuego.Play();

            StartCoroutine(AnimarEncendido());
        }

        IEnumerator CadenaYDesvanecido(InventarioJugador inventario)
        {
            if (enciendeLasDemas) yield return StartCoroutine(EncenderLasDemas(inventario));

            yield return StartCoroutine(AnimarDesvanecido());
        }

        IEnumerator EncenderLasDemas(InventarioJugador inventario)
        {
            for (int i = 0; i < registro.Count; i++)
            {
                Antorcha otra = registro[i];
                if (otra == null || otra == this || otra.Encendida) continue;

                otra.Encender(inventario);

                if (retrasoCadena > 0f) yield return new WaitForSeconds(retrasoCadena);
            }
        }

        IEnumerator AnimarDesvanecido()
        {
            desapareciendo = true;

            Vector3 escalaInicial = transform.localScale;
            float intensidadInicial = luz != null ? luz.intensity : 0f;
            float t = 0f;

            while (t < duracionDesvanecido)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / duracionDesvanecido);

                transform.localScale = escalaInicial * k;
                transform.Rotate(Vector3.up, 620f * Time.deltaTime, Space.World);
                transform.position += Vector3.up * 1.1f * Time.deltaTime;

                if (luz != null) luz.intensity = intensidadInicial * k;

                yield return null;
            }

            gameObject.SetActive(false);
        }

        IEnumerator AnimarEncendido()
        {
            float t = 0f;
            while (t < duracionEncendido)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracionEncendido));
                if (luz != null) luz.intensity = intensidadFinal * k;
                if (llama != null) llama.localScale = Vector3.one * escalaLlama * k;
                yield return null;
            }
            estable = true;
        }
    }
}
