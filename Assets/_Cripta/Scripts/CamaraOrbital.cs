using UnityEngine;

namespace Cripta
{
    public class CamaraOrbital : MonoBehaviour
    {
        [Header("Objetivo")]
        public Transform objetivo;
        public float alturaFoco = 1.6f;

        [Header("Orbita")]
        public float distancia = 5.5f;
        public float sensibilidad = 2.2f;
        public float inclinacionMinima = -25f;
        public float inclinacionMaxima = 60f;

        float yaw;
        float pitch = 15f;

        readonly RaycastHit[] impactos = new RaycastHit[16];

        void Start()
        {
            if (objetivo != null) yaw = objetivo.eulerAngles.y;
            BloquearCursor(true);
        }

        void Update()
        {
            if (Entrada.EscapePresionado()) BloquearCursor(false);
            else if (Entrada.ClicPresionado() && Cursor.lockState != CursorLockMode.Locked) BloquearCursor(true);
        }

        void LateUpdate()
        {
            if (objetivo == null) return;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mirada = Entrada.Mirada();
                yaw += mirada.x * sensibilidad;
                pitch -= mirada.y * sensibilidad;
                pitch = Mathf.Clamp(pitch, inclinacionMinima, inclinacionMaxima);
            }

            Quaternion rotacion = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 foco = objetivo.position + Vector3.up * alturaFoco;
            Vector3 hacia = rotacion * Vector3.back;
            float distanciaFinal = distancia;

            int n = Physics.RaycastNonAlloc(foco, hacia, impactos, distancia, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (impactos[i].collider.transform.IsChildOf(objetivo)) continue;
                if (impactos[i].distance < distanciaFinal) distanciaFinal = impactos[i].distance - 0.25f;
            }
            distanciaFinal = Mathf.Max(distanciaFinal, 1.2f);

            transform.position = foco + hacia * distanciaFinal;
            transform.rotation = rotacion;
        }

        static void BloquearCursor(bool bloquear)
        {
            Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloquear;
        }
    }
}
