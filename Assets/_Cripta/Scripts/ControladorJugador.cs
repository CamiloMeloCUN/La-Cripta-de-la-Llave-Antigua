using UnityEngine;

namespace Cripta
{
    [RequireComponent(typeof(CharacterController))]
    public class ControladorJugador : MonoBehaviour
    {
        [Header("Movimiento")]
        public float velocidadCaminar = 4.5f;
        public float velocidadCorrer = 7.5f;
        public float alturaSalto = 1.3f;
        public float gravedad = -22f;
        public float suavizadoGiro = 0.10f;

        [Header("Referencias")]
        public Transform referenciaCamara;
        public AudioSource fuentePasos;
        public Animator animador;

        [Header("Animacion")]
        public float suavizadoAnimacion = 0.12f;

        static readonly int HashVelocidad = Animator.StringToHash("Velocidad");
        static readonly int HashEnSuelo = Animator.StringToHash("EnSuelo");
        static readonly int HashSaltar = Animator.StringToHash("Saltar");

        CharacterController cc;
        float velocidadVertical;
        float velocidadGiro;
        float velocidadAnimada;

        public bool EnMovimiento { get; private set; }
        public bool Corriendo { get; private set; }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (animador == null) animador = GetComponentInChildren<Animator>();
            if (animador != null) animador.applyRootMotion = false;
        }

        void Update()
        {
            Vector2 eje = Entrada.Movimiento();
            Vector3 direccion = Vector3.zero;

            if (eje.sqrMagnitude > 0.01f)
            {
                Vector3 frente = referenciaCamara != null ? referenciaCamara.forward : Vector3.forward;
                Vector3 derecha = referenciaCamara != null ? referenciaCamara.right : Vector3.right;
                frente.y = 0f;
                derecha.y = 0f;
                frente.Normalize();
                derecha.Normalize();

                direccion = (frente * eje.y + derecha * eje.x).normalized;

                float anguloObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;
                float angulo = Mathf.SmoothDampAngle(transform.eulerAngles.y, anguloObjetivo,
                                                     ref velocidadGiro, suavizadoGiro);
                transform.rotation = Quaternion.Euler(0f, angulo, 0f);
            }

            bool enSuelo = cc.isGrounded;
            bool salto = false;

            if (enSuelo && velocidadVertical < 0f) velocidadVertical = -2f;

            if (enSuelo && Entrada.SaltoPresionado())
            {
                velocidadVertical = Mathf.Sqrt(alturaSalto * -2f * gravedad);
                salto = true;
            }

            velocidadVertical += gravedad * Time.deltaTime;

            Corriendo = Entrada.CorrerMantenido();
            float velocidad = Corriendo ? velocidadCorrer : velocidadCaminar;

            Vector3 desplazamiento = direccion * velocidad + Vector3.up * velocidadVertical;
            cc.Move(desplazamiento * Time.deltaTime);

            EnMovimiento = direccion.sqrMagnitude > 0.01f && enSuelo;

            ActualizarAnimacion(direccion, enSuelo, salto);
            ActualizarPasos();
        }

        void ActualizarAnimacion(Vector3 direccion, bool enSuelo, bool salto)
        {
            if (animador == null || animador.runtimeAnimatorController == null) return;

            float objetivo = 0f;
            if (direccion.sqrMagnitude > 0.01f) objetivo = Corriendo ? 1f : 0.5f;

            velocidadAnimada = Mathf.MoveTowards(velocidadAnimada, objetivo,
                                                 Time.deltaTime / Mathf.Max(0.01f, suavizadoAnimacion));

            animador.SetFloat(HashVelocidad, velocidadAnimada);
            animador.SetBool(HashEnSuelo, enSuelo);

            if (salto) animador.SetTrigger(HashSaltar);
        }

        void ActualizarPasos()
        {
            if (fuentePasos == null || fuentePasos.clip == null) return;

            if (EnMovimiento && !fuentePasos.isPlaying)
            {
                fuentePasos.loop = true;
                fuentePasos.Play();
            }
            else if (!EnMovimiento && fuentePasos.isPlaying)
            {
                fuentePasos.Stop();
            }
        }
    }
}
