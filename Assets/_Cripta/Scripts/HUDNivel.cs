using UnityEngine;

namespace Cripta
{
    public class HUDNivel : MonoBehaviour
    {
        public InventarioJugador inventario;
        public InteractorJugador interactor;

        [TextArea]
        public string ayudaControles = "WASD mover     Shift correr     Espacio saltar     E interactuar     Esc liberar el cursor";

        public float segundosAyuda = 9f;
        public float margen = 12f;

        [Header("Colores")]
        public Color colorTexto = new Color(0.92f, 0.90f, 0.80f);
        public Color colorTitulo = new Color(1f, 0.94f, 0.72f);
        public Color colorFondo = new Color(0f, 0f, 0f, 0.62f);

        [Header("Tamanos de letra")]
        public int tamanoPanel = 16;
        public int tamanoAviso = 20;
        public int tamanoTitulo = 34;

        static string mensaje = string.Empty;
        static float mensajeHasta = -1f;

        float tiempoInicio;

        GUIStyle estiloTexto;
        GUIStyle estiloAviso;
        GUIStyle estiloTitulo;
        Texture2D fondo;

        public static void Mostrar(string texto, float segundos)
        {
            mensaje = texto;
            mensajeHasta = Time.time + segundos;
        }

        void Awake()
        {
            mensaje = string.Empty;
            mensajeHasta = -1f;
            tiempoInicio = Time.time;
        }

        void OnDestroy()
        {
            LiberarFondo();
        }

        void OnValidate()
        {
            tamanoPanel = Mathf.Clamp(tamanoPanel, 8, 72);
            tamanoAviso = Mathf.Clamp(tamanoAviso, 8, 72);
            tamanoTitulo = Mathf.Clamp(tamanoTitulo, 8, 120);

            estiloTexto = null;
            estiloAviso = null;
            estiloTitulo = null;
            LiberarFondo();
        }

        void LiberarFondo()
        {
            if (fondo == null) return;

            if (Application.isPlaying) Destroy(fondo);
            else DestroyImmediate(fondo);

            fondo = null;
        }

        void PrepararFondo()
        {
            if (fondo != null) return;

            fondo = new Texture2D(1, 1);
            fondo.hideFlags = HideFlags.HideAndDontSave;
            fondo.SetPixel(0, 0, colorFondo);
            fondo.Apply();
        }

        void PrepararEstilos()
        {
            if (estiloTexto != null) return;

            estiloTexto = new GUIStyle(GUI.skin.label);
            estiloTexto.fontSize = tamanoPanel;
            estiloTexto.wordWrap = false;
            estiloTexto.padding = new RectOffset(0, 0, 0, 0);
            estiloTexto.normal.textColor = colorTexto;
            estiloTexto.hover.textColor = colorTexto;
            estiloTexto.active.textColor = colorTexto;

            estiloAviso = new GUIStyle(estiloTexto);
            estiloAviso.fontSize = tamanoAviso;
            estiloAviso.alignment = TextAnchor.MiddleCenter;
            estiloAviso.wordWrap = true;

            estiloTitulo = new GUIStyle(estiloAviso);
            estiloTitulo.fontSize = tamanoTitulo;
            estiloTitulo.fontStyle = FontStyle.Bold;
            estiloTitulo.normal.textColor = colorTitulo;
            estiloTitulo.hover.textColor = colorTitulo;
            estiloTitulo.active.textColor = colorTitulo;
        }

        void DibujarFondo(Rect caja)
        {
            PrepararFondo();
            if (fondo != null) GUI.DrawTexture(caja, fondo);
        }

        void DibujarCaja(string texto, GUIStyle estilo, float centroX, float y,
                         float anchoMaximo, bool alinearPorAbajo)
        {
            GUIContent contenido = new GUIContent(texto);

            float ancho = Mathf.Min(estilo.CalcSize(contenido).x, anchoMaximo);
            float alto = estilo.CalcHeight(contenido, ancho);
            float altoCaja = alto + margen * 2f;
            float superior = alinearPorAbajo ? y - altoCaja : y;

            Rect caja = new Rect(centroX - ancho * 0.5f - margen, superior,
                                 ancho + margen * 2f, altoCaja);

            DibujarFondo(caja);
            GUI.Label(new Rect(caja.x + margen, caja.y + margen, ancho, alto), contenido, estilo);
        }

        string TextoObjetivo()
        {
            if (inventario == null) return "-";
            if (inventario.NivelCompletado) return "Recorrido completado";
            if (inventario.TieneLlave) return "Regresar y abrir la puerta de salida";
            return "Encontrar la llave antigua";
        }

        void DibujarPanelEstado()
        {
            string[] lineas = {
                "Objetivo:  " + TextoObjetivo(),
                "Llave antigua:  " + ((inventario != null && inventario.TieneLlave) ? "si" : "no"),
                "Antorchas encendidas:  " + (inventario != null ? inventario.AntorchasEncendidas : 0) + " / " + Antorcha.Total
            };

            float ancho = 0f;
            for (int i = 0; i < lineas.Length; i++)
                ancho = Mathf.Max(ancho, estiloTexto.CalcSize(new GUIContent(lineas[i])).x);

            float altoLinea = estiloTexto.lineHeight + 6f;

            Rect panel = new Rect(16f, 16f, ancho + margen * 2f, altoLinea * lineas.Length + margen * 2f);
            DibujarFondo(panel);

            for (int i = 0; i < lineas.Length; i++)
            {
                Rect linea = new Rect(panel.x + margen, panel.y + margen + altoLinea * i, ancho, altoLinea);
                GUI.Label(linea, lineas[i], estiloTexto);
            }
        }

        void OnGUI()
        {
            PrepararFondo();
            PrepararEstilos();
            DibujarPanelEstado();

            float centro = Screen.width * 0.5f;
            float anchoMaximo = Mathf.Max(240f, Screen.width - 100f);

            if (interactor != null && interactor.Actual != null)
            {
                string aviso = interactor.Actual.Mensaje(inventario);
                if (!string.IsNullOrEmpty(aviso))
                    DibujarCaja(aviso, estiloAviso, centro, Screen.height - 55f, anchoMaximo, true);
            }

            if (Time.time - tiempoInicio < segundosAyuda)
                DibujarCaja(ayudaControles, estiloAviso, centro, Screen.height - 125f, anchoMaximo, true);

            if (Time.time < mensajeHasta && !string.IsNullOrEmpty(mensaje))
                DibujarCaja(mensaje, estiloAviso, centro, Screen.height - 195f, anchoMaximo, true);

            if (inventario != null && inventario.NivelCompletado)
                DibujarCaja("NIVEL COMPLETADO", estiloTitulo, centro, Screen.height * 0.38f, anchoMaximo, false);
        }
    }
}
