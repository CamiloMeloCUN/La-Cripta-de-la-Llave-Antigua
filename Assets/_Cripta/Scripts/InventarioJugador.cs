using UnityEngine;

namespace Cripta
{
    public class InventarioJugador : MonoBehaviour
    {
        public bool TieneLlave { get; private set; }
        public int AntorchasEncendidas { get; private set; }
        public bool NivelCompletado { get; private set; }

        public void RecogerLlave()
        {
            TieneLlave = true;
            HUDNivel.Mostrar("Has obtenido la llave antigua. Vuelve a la puerta de salida.", 4f);
        }

        public void RegistrarAntorcha()
        {
            AntorchasEncendidas++;
        }

        public void CompletarNivel()
        {
            NivelCompletado = true;
        }
    }
}
