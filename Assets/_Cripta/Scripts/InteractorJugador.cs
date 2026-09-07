using UnityEngine;

namespace Cripta
{
    public class InteractorJugador : MonoBehaviour
    {
        public float radioBusqueda = 4f;
        public InventarioJugador inventario;

        readonly Collider[] buffer = new Collider[32];

        public Interactuable Actual { get; private set; }

        void Update()
        {
            Actual = null;

            if (inventario != null && inventario.NivelCompletado) return;

            float mejorDistancia = float.MaxValue;
            Vector3 centro = transform.position + Vector3.up;

            int n = Physics.OverlapSphereNonAlloc(centro, radioBusqueda, buffer, ~0,
                                                  QueryTriggerInteraction.Collide);

            for (int i = 0; i < n; i++)
            {
                if (buffer[i] == null) continue;

                Interactuable candidato = buffer[i].GetComponentInParent<Interactuable>();
                if (candidato == null || !candidato.Activo) continue;

                float distancia = Vector3.Distance(centro, candidato.transform.position);
                if (distancia <= candidato.Radio && distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    Actual = candidato;
                }
            }

            if (Actual != null && Entrada.InteraccionPresionada())
            {
                Actual.Interactuar(inventario);
            }
        }
    }
}
