using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Cripta.Herramientas
{
    public static class ConstructorNivel
    {
        const string RAIZ = "Assets/_Cripta";
        const string RUTA_MATERIALES = RAIZ + "/Arte/Materiales";
        const string RUTA_TEXTURAS = RAIZ + "/Arte/Texturas";
        const string RUTA_AUDIO = RAIZ + "/Audio";
        const string RUTA_ESCENAS = RAIZ + "/Escenas";
        const string RUTA_ESCENA = RUTA_ESCENAS + "/Cripta.unity";

        const float TILE = 3f;
        const float ALTO_MURO = 4f;
        const float GROSOR_PISO = 0.4f;

        static readonly string[] MAPA = {
            "###########################",
            "###########################",
            "#######.............#######",
            "#######......K......#######",
            "#######.............#######",
            "#########.#######.#########",
            "#########.#######.......###",
            "#########.#############.###",
            "#########.#############.###",
            "#########.#############.###",
            "#########.#############R###",
            "#########.#############.###",
            "#########.#############.###",
            "#########.#############.###",
            "#########.#############.###",
            "##....#.................###",
            "##T...#.......T############",
            "##.S...........#####.....##",
            "##....#.........D.....X..##",
            "####################.....##",
            "###########################"
        };

        static int Filas { get { return MAPA.Length; } }
        static int Columnas { get { return MAPA[0].Length; } }

        [MenuItem("Cripta/1. Construir nivel", false, 10)]
        public static void Construir()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AsegurarCarpetas();
            Dictionary<string, Material> mat = CrearMateriales();

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigurarEntorno();

            GameObject raizNivel = new GameObject("Nivel");
            GameObject grupoPisos = NuevoHijo("Pisos", raizNivel.transform);
            GameObject grupoMuros = NuevoHijo("Muros", raizNivel.transform);
            GameObject grupoInteractivos = new GameObject("Elementos interactivos");
            GameObject grupoZonas = new GameObject("Zonas");

            ConstruirGeometria(grupoPisos.transform, grupoMuros.transform, mat);

            GameObject jugador = ConstruirJugador(mat);
            ConstruirCamara(jugador);
            ConstruirIluminacionGeneral();

            ConstruirInteractivos(grupoInteractivos.transform, mat);
            ConstruirZonas(grupoZonas.transform);
            ConstruirSistemas(jugador);

            VincularRecursos(false);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena, RUTA_ESCENA);
            RegistrarEnBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Cripta] Nivel construido y guardado en " + RUTA_ESCENA +
                      ". Pulsa Play para recorrerlo.");
        }

        [MenuItem("Cripta/2. Vincular texturas y sonidos", false, 11)]
        public static void VincularDesdeMenu()
        {
            VincularRecursos(true);
            AssetDatabase.SaveAssets();
            Debug.Log("[Cripta] Texturas y sonidos revinculados. Guarda la escena para conservar los cambios.");
        }

        static void AsegurarCarpetas()
        {
            AsegurarCarpeta(RAIZ);
            AsegurarCarpeta(RAIZ + "/Arte");
            AsegurarCarpeta(RUTA_MATERIALES);
            AsegurarCarpeta(RUTA_TEXTURAS);
            AsegurarCarpeta(RUTA_AUDIO);
            AsegurarCarpeta(RUTA_ESCENAS);
        }

        static void AsegurarCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta)) return;

            string[] partes = ruta.Split('/');
            string acumulado = partes[0];
            for (int i = 1; i < partes.Length; i++)
            {
                string siguiente = acumulado + "/" + partes[i];
                if (!AssetDatabase.IsValidFolder(siguiente))
                    AssetDatabase.CreateFolder(acumulado, partes[i]);
                acumulado = siguiente;
            }
        }

        static Shader ShaderBase()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("HDRP/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Diffuse");
            return s;
        }

        static Dictionary<string, Material> CrearMateriales()
        {
            Dictionary<string, Material> mapa = new Dictionary<string, Material>();

            mapa["Piso"] = CrearMaterial("Piso", new Color(0.30f, 0.28f, 0.25f), 0.05f, TILE * 0.5f);
            mapa["Muro"] = CrearMaterial("Muro", new Color(0.38f, 0.36f, 0.33f), 0.05f, TILE * 0.5f);
            mapa["Puerta"] = CrearMaterial("Puerta", new Color(0.32f, 0.20f, 0.11f), 0.15f, 1f);
            mapa["Llave"] = CrearMaterial("Llave", new Color(0.83f, 0.68f, 0.25f), 0.85f, 1f);
            mapa["Antorcha"] = CrearMaterial("Antorcha", new Color(0.22f, 0.16f, 0.11f), 0.10f, 1f);
            mapa["Jugador"] = CrearMaterial("Jugador", new Color(0.55f, 0.58f, 0.68f), 0.25f, 1f);
            mapa["Llama"] = CrearMaterialEmisivo("Llama", new Color(1f, 0.55f, 0.15f), new Color(1f, 0.52f, 0.12f) * 4f);

            return mapa;
        }

        static Material CrearMaterial(string nombre, Color color, float suavidad, float tiling)
        {
            string ruta = RUTA_MATERIALES + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;

            m = new Material(ShaderBase());
            AplicarColor(m, color);
            AplicarSuavidad(m, suavidad);
            AplicarTiling(m, tiling);
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        static Material CrearMaterialEmisivo(string nombre, Color color, Color emision)
        {
            string ruta = RUTA_MATERIALES + "/" + nombre + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;

            m = new Material(ShaderBase());
            AplicarColor(m, color);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emision);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        static void AplicarColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        static void AplicarSuavidad(Material m, float v)
        {
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", v);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", v);
        }

        static void AplicarTiling(Material m, float t)
        {
            Vector2 escala = new Vector2(t, t);
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", escala);
            if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", escala);
        }

        static void ConfigurarEntorno()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.13f, 0.13f, 0.17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.04f, 0.04f, 0.06f);
            RenderSettings.fogDensity = 0.016f;
        }

        static void ConstruirIluminacionGeneral()
        {
            GameObject go = new GameObject("Luz general");
            go.transform.rotation = Quaternion.Euler(52f, -32f, 0f);

            Light luz = go.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = new Color(0.55f, 0.62f, 0.80f);
            luz.intensity = 0.28f;
            luz.shadows = LightShadows.Soft;
        }

        static Vector3 Centro(int fila, int columna, float y)
        {
            return new Vector3(columna * TILE, y, (Filas - 1 - fila) * TILE);
        }

        static char Tile(int fila, int columna)
        {
            if (fila < 0 || fila >= Filas || columna < 0 || columna >= Columnas) return '#';
            return MAPA[fila][columna];
        }

        static bool EsTransitable(int fila, int columna)
        {
            return Tile(fila, columna) != '#';
        }

        static void ConstruirGeometria(Transform pisos, Transform muros, Dictionary<string, Material> mat)
        {
            for (int f = 0; f < Filas; f++)
            {
                for (int c = 0; c < Columnas; c++)
                {
                    if (EsTransitable(f, c))
                    {
                        Bloque("Piso_" + f + "_" + c, pisos,
                                                 Centro(f, c, -GROSOR_PISO * 0.5f),
                                                 new Vector3(TILE, GROSOR_PISO, TILE),
                                                 mat["Piso"]);
                    }
                    else if (TieneVecinoTransitable(f, c))
                    {
                        Bloque("Muro_" + f + "_" + c, muros,
                               Centro(f, c, ALTO_MURO * 0.5f),
                               new Vector3(TILE, ALTO_MURO, TILE),
                               mat["Muro"]);
                    }
                }
            }
        }

        static bool TieneVecinoTransitable(int fila, int columna)
        {
            for (int df = -1; df <= 1; df++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (df == 0 && dc == 0) continue;
                    if (EsTransitable(fila + df, columna + dc)) return true;
                }
            }
            return false;
        }

        static GameObject Bloque(string nombre, Transform padre, Vector3 posicion, Vector3 escala, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(padre, true);
            go.transform.position = posicion;
            go.transform.localScale = escala;

            Renderer r = go.GetComponent<Renderer>();
            if (r != null && material != null) r.sharedMaterial = material;

            return go;
        }

        static GameObject NuevoHijo(string nombre, Transform padre)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            return go;
        }

        static void QuitarColisionador(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }

        static Vector2Int BuscarTile(char simbolo)
        {
            for (int f = 0; f < Filas; f++)
                for (int c = 0; c < Columnas; c++)
                    if (MAPA[f][c] == simbolo) return new Vector2Int(f, c);
            return new Vector2Int(-1, -1);
        }

        static GameObject ConstruirJugador(Dictionary<string, Material> mat)
        {
            Vector2Int t = BuscarTile('S');
            GameObject jugador = new GameObject("Jugador");
            jugador.transform.position = Centro(t.x, t.y, 0.1f);

            CharacterController cc = jugador.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.4f;
            cc.skinWidth = 0.03f;

            GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cuerpo.name = "Cuerpo";
            QuitarColisionador(cuerpo);
            cuerpo.transform.SetParent(jugador.transform, false);
            cuerpo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            cuerpo.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            cuerpo.GetComponent<Renderer>().sharedMaterial = mat["Jugador"];

            GameObject cabeza = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cabeza.name = "Cabeza";
            QuitarColisionador(cabeza);
            cabeza.transform.SetParent(jugador.transform, false);
            cabeza.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            cabeza.transform.localScale = Vector3.one * 0.42f;
            cabeza.GetComponent<Renderer>().sharedMaterial = mat["Jugador"];

            GameObject frente = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frente.name = "Marca frontal";
            QuitarColisionador(frente);
            frente.transform.SetParent(jugador.transform, false);
            frente.transform.localPosition = new Vector3(0f, 1.72f, 0.24f);
            frente.transform.localScale = new Vector3(0.14f, 0.10f, 0.16f);
            frente.GetComponent<Renderer>().sharedMaterial = mat["Llave"];

            ControladorJugador control = jugador.AddComponent<ControladorJugador>();
            InventarioJugador inventario = jugador.AddComponent<InventarioJugador>();
            InteractorJugador interactor = jugador.AddComponent<InteractorJugador>();
            interactor.inventario = inventario;

            AudioSource pasos = jugador.AddComponent<AudioSource>();
            pasos.playOnAwake = false;
            pasos.loop = true;
            pasos.spatialBlend = 0f;
            pasos.volume = 0.5f;
            control.fuentePasos = pasos;

            return jugador;
        }

        static void ConstruirCamara(GameObject jugador)
        {
            GameObject go = new GameObject("Camara orbital");
            go.tag = "MainCamera";

            Camera cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.05f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 220f;
            cam.fieldOfView = 65f;

            go.AddComponent<AudioListener>();

            CamaraOrbital orbital = go.AddComponent<CamaraOrbital>();
            orbital.objetivo = jugador.transform;

            ControladorJugador control = jugador.GetComponent<ControladorJugador>();
            if (control != null) control.referenciaCamara = go.transform;
        }

        static void ConstruirInteractivos(Transform padre, Dictionary<string, Material> mat)
        {
            for (int f = 0; f < Filas; f++)
            {
                for (int c = 0; c < Columnas; c++)
                {
                    char simbolo = MAPA[f][c];
                    if (simbolo == 'T') CrearAntorcha(padre, f, c, mat, "Antorcha");
                    else if (simbolo == 'R') CrearAntorcha(padre, f, c, mat, "Antorcha de recompensa (camino B)");
                    else if (simbolo == 'K') CrearLlave(padre, f, c, mat);
                    else if (simbolo == 'D') CrearPuerta(padre, f, c, mat);
                    else if (simbolo == 'X') CrearMeta(padre, f, c);
                }
            }
        }

        static Vector3 DesplazamientoHaciaMuro(int fila, int columna)
        {
            if (!EsTransitable(fila - 1, columna)) return new Vector3(0f, 0f, TILE * 0.36f);
            if (!EsTransitable(fila + 1, columna)) return new Vector3(0f, 0f, -TILE * 0.36f);
            if (!EsTransitable(fila, columna - 1)) return new Vector3(-TILE * 0.36f, 0f, 0f);
            if (!EsTransitable(fila, columna + 1)) return new Vector3(TILE * 0.36f, 0f, 0f);
            return Vector3.zero;
        }

        static void CrearAntorcha(Transform padre, int fila, int columna,
                                  Dictionary<string, Material> mat, string nombre)
        {
            GameObject raiz = new GameObject(nombre + " (" + fila + "," + columna + ")");
            raiz.transform.SetParent(padre, true);
            raiz.transform.position = Centro(fila, columna, 0f) + DesplazamientoHaciaMuro(fila, columna);

            GameObject mango = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mango.name = "Mango";
            mango.transform.SetParent(raiz.transform, false);
            mango.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            mango.transform.localScale = new Vector3(0.14f, 0.55f, 0.14f);
            mango.GetComponent<Renderer>().sharedMaterial = mat["Antorcha"];
            QuitarColisionador(mango);

            GameObject llama = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            llama.name = "Llama";
            llama.transform.SetParent(raiz.transform, false);
            llama.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            llama.transform.localScale = Vector3.zero;
            llama.GetComponent<Renderer>().sharedMaterial = mat["Llama"];
            QuitarColisionador(llama);

            GameObject luzGO = NuevoHijo("Luz", raiz.transform);
            luzGO.transform.localPosition = new Vector3(0f, 2.05f, 0f);
            Light luz = luzGO.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.62f, 0.28f);
            luz.range = 16f;
            luz.intensity = 0f;
            luz.shadows = LightShadows.None;

            SphereCollider zona = raiz.AddComponent<SphereCollider>();
            zona.isTrigger = true;
            zona.radius = 1.2f;
            zona.center = new Vector3(0f, 1.2f, 0f);

            AudioSource fuego = raiz.AddComponent<AudioSource>();
            fuego.playOnAwake = false;
            fuego.loop = true;
            fuego.spatialBlend = 1f;
            fuego.minDistance = 2f;
            fuego.maxDistance = 18f;
            fuego.volume = 0.7f;

            Antorcha antorcha = raiz.AddComponent<Antorcha>();
            antorcha.luz = luz;
            antorcha.llama = llama.transform;
            antorcha.fuenteFuego = fuego;
        }

        static void CrearLlave(Transform padre, int fila, int columna, Dictionary<string, Material> mat)
        {
            GameObject raiz = new GameObject("Llave antigua");
            raiz.transform.SetParent(padre, true);
            raiz.transform.position = Centro(fila, columna, 1.15f);

            GameObject vastago = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vastago.name = "Vastago";
            vastago.transform.SetParent(raiz.transform, false);
            vastago.transform.localPosition = new Vector3(0f, 0f, 0f);
            vastago.transform.localScale = new Vector3(0.07f, 0.07f, 0.55f);
            vastago.GetComponent<Renderer>().sharedMaterial = mat["Llave"];
            QuitarColisionador(vastago);

            GameObject anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            anillo.name = "Anillo";
            anillo.transform.SetParent(raiz.transform, false);
            anillo.transform.localPosition = new Vector3(0f, 0f, -0.34f);
            anillo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            anillo.transform.localScale = new Vector3(0.24f, 0.03f, 0.24f);
            anillo.GetComponent<Renderer>().sharedMaterial = mat["Llave"];
            QuitarColisionador(anillo);

            GameObject diente = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diente.name = "Diente";
            diente.transform.SetParent(raiz.transform, false);
            diente.transform.localPosition = new Vector3(0.08f, 0f, 0.22f);
            diente.transform.localScale = new Vector3(0.16f, 0.06f, 0.08f);
            diente.GetComponent<Renderer>().sharedMaterial = mat["Llave"];
            QuitarColisionador(diente);

            GameObject luzGO = NuevoHijo("Destello", raiz.transform);
            Light destello = luzGO.AddComponent<Light>();
            destello.type = LightType.Point;
            destello.color = new Color(1f, 0.86f, 0.45f);
            destello.range = 9f;
            destello.intensity = 1.6f;
            destello.shadows = LightShadows.None;

            SphereCollider zona = raiz.AddComponent<SphereCollider>();
            zona.isTrigger = true;
            zona.radius = 0.9f;

            AudioSource sonido = raiz.AddComponent<AudioSource>();
            sonido.playOnAwake = false;
            sonido.spatialBlend = 0.6f;

            LlaveAntigua llave = raiz.AddComponent<LlaveAntigua>();
            llave.fuenteRecoger = sonido;
        }

        static void CrearPuerta(Transform padre, int fila, int columna, Dictionary<string, Material> mat)
        {
            GameObject raiz = new GameObject("Puerta de salida (gate)");
            raiz.transform.SetParent(padre, true);
            raiz.transform.position = Centro(fila, columna, 0f) + new Vector3(0f, 0f, TILE * 0.5f);

            GameObject pivote = NuevoHijo("Pivote", raiz.transform);

            GameObject hoja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hoja.name = "Hoja";
            hoja.transform.SetParent(pivote.transform, false);
            hoja.transform.localPosition = new Vector3(0f, 1.75f, -TILE * 0.5f);
            hoja.transform.localScale = new Vector3(0.24f, 3.5f, TILE * 0.98f);
            hoja.GetComponent<Renderer>().sharedMaterial = mat["Puerta"];

            GameObject marco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marco.name = "Dintel";
            marco.transform.SetParent(raiz.transform, false);
            marco.transform.localPosition = new Vector3(0f, 3.75f, -TILE * 0.5f);
            marco.transform.localScale = new Vector3(0.45f, 0.5f, TILE);
            marco.GetComponent<Renderer>().sharedMaterial = mat["Puerta"];

            BoxCollider zona = raiz.AddComponent<BoxCollider>();
            zona.isTrigger = true;
            zona.size = new Vector3(5f, 4f, 4f);
            zona.center = new Vector3(0f, 2f, -TILE * 0.5f);

            AudioSource abrir = raiz.AddComponent<AudioSource>();
            abrir.playOnAwake = false;
            abrir.spatialBlend = 0.7f;

            AudioSource trabada = raiz.AddComponent<AudioSource>();
            trabada.playOnAwake = false;
            trabada.spatialBlend = 0.7f;

            PuertaGate puerta = raiz.AddComponent<PuertaGate>();
            puerta.pivote = pivote.transform;
            puerta.fuenteAbrir = abrir;
            puerta.fuenteTrabada = trabada;
        }

        static void CrearMeta(Transform padre, int fila, int columna)
        {
            GameObject raiz = new GameObject("Meta del nivel");
            raiz.transform.SetParent(padre, true);
            raiz.transform.position = Centro(fila, columna, 2f);

            BoxCollider box = raiz.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(TILE, 4f, TILE);

            AudioSource final = raiz.AddComponent<AudioSource>();
            final.playOnAwake = false;
            final.spatialBlend = 0f;

            MetaNivel meta = raiz.AddComponent<MetaNivel>();
            meta.fuenteFinal = final;
        }

        struct DefinicionZona
        {
            public string nombre;
            public int fila0, col0, fila1, col1;
            public string mensaje;

            public DefinicionZona(string nombre, int fila0, int col0, int fila1, int col1, string mensaje)
            {
                this.nombre = nombre;
                this.fila0 = fila0;
                this.col0 = col0;
                this.fila1 = fila1;
                this.col1 = col1;
                this.mensaje = mensaje;
            }
        }

        static void ConstruirZonas(Transform padre)
        {
            DefinicionZona[] zonas = {
                new DefinicionZona("Zona 1 - Entrada", 15, 2, 18, 5,
                    "Zona 1 - Entrada de la cripta."),
                new DefinicionZona("Zona 2 - Bifurcacion", 15, 7, 18, 14,
                    "Zona 2 - Bifurcacion. Camino A: corto y directo. Camino B: mas largo, con recompensa."),
                new DefinicionZona("Zona 3 - Camara de la llave", 2, 7, 4, 19,
                    "Zona 3 - Camara de la llave."),
                new DefinicionZona("Zona 4 - Salida", 17, 20, 19, 24,
                    "Zona 4 - Salida de la cripta.")
            };

            foreach (DefinicionZona z in zonas)
            {
                GameObject go = new GameObject(z.nombre);
                go.transform.SetParent(padre, true);

                Vector3 a = Centro(z.fila0, z.col0, 0f);
                Vector3 b = Centro(z.fila1, z.col1, 0f);
                Vector3 centro = (a + b) * 0.5f;
                centro.y = 1.6f;
                go.transform.position = centro;

                BoxCollider box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(Mathf.Abs(a.x - b.x) + TILE, 3.2f, Mathf.Abs(a.z - b.z) + TILE);

                ZonaTrigger trigger = go.AddComponent<ZonaTrigger>();
                trigger.mensaje = z.mensaje;
                trigger.duracion = 3.5f;
                trigger.unaSolaVez = true;
            }
        }

        static void ConstruirSistemas(GameObject jugador)
        {
            GameObject go = new GameObject("Sistemas");
            HUDNivel hud = go.AddComponent<HUDNivel>();
            hud.inventario = jugador.GetComponent<InventarioJugador>();
            hud.interactor = jugador.GetComponent<InteractorJugador>();
        }

        static T Buscar<T>(string nombre, string carpeta) where T : UnityEngine.Object
        {
            if (!AssetDatabase.IsValidFolder(carpeta)) return null;

            string[] guids = AssetDatabase.FindAssets(nombre + " t:" + typeof(T).Name, new[] { carpeta });
            for (int i = 0; i < guids.Length; i++)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
                string archivo = System.IO.Path.GetFileNameWithoutExtension(ruta).ToLowerInvariant();
                if (archivo.Contains(nombre.ToLowerInvariant()))
                    return AssetDatabase.LoadAssetAtPath<T>(ruta);
            }
            return null;
        }

        static void AsignarTextura(string materialNombre, string texturaNombre)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(RUTA_MATERIALES + "/" + materialNombre + ".mat");
            if (m == null) return;

            Texture2D tex = Buscar<Texture2D>(texturaNombre, RUTA_TEXTURAS);
            if (tex == null) return;

            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            EditorUtility.SetDirty(m);
        }

        static T[] TodosEnEscena<T>() where T : Component
        {
            List<T> encontrados = new List<T>();

            Scene escena = SceneManager.GetActiveScene();
            if (!escena.IsValid() || !escena.isLoaded) return encontrados.ToArray();

            GameObject[] raices = escena.GetRootGameObjects();
            for (int i = 0; i < raices.Length; i++)
            {
                encontrados.AddRange(raices[i].GetComponentsInChildren<T>(true));
            }

            return encontrados.ToArray();
        }

        static void VincularRecursos(bool avisar)
        {
            AsignarTextura("Piso", "piso");
            AsignarTextura("Muro", "muro");
            AsignarTextura("Puerta", "puerta");
            AsignarTextura("Llave", "llave");
            AsignarTextura("Antorcha", "antorcha");
            AsignarTextura("Jugador", "jugador");

            AudioClip fuego = Buscar<AudioClip>("fuego", RUTA_AUDIO);
            AudioClip llave = Buscar<AudioClip>("llave", RUTA_AUDIO);
            AudioClip puerta = Buscar<AudioClip>("puerta", RUTA_AUDIO);
            AudioClip cerradura = Buscar<AudioClip>("cerradura", RUTA_AUDIO);
            AudioClip pasos = Buscar<AudioClip>("pasos", RUTA_AUDIO);
            AudioClip final = Buscar<AudioClip>("final", RUTA_AUDIO);

            foreach (Antorcha a in TodosEnEscena<Antorcha>())
                if (a.fuenteFuego != null) a.fuenteFuego.clip = fuego;

            foreach (LlaveAntigua l in TodosEnEscena<LlaveAntigua>())
                if (l.fuenteRecoger != null) l.fuenteRecoger.clip = llave;

            foreach (PuertaGate p in TodosEnEscena<PuertaGate>())
            {
                if (p.fuenteAbrir != null) p.fuenteAbrir.clip = puerta;
                if (p.fuenteTrabada != null) p.fuenteTrabada.clip = cerradura;
            }

            foreach (ControladorJugador c in TodosEnEscena<ControladorJugador>())
                if (c.fuentePasos != null) c.fuentePasos.clip = pasos;

            foreach (MetaNivel m in TodosEnEscena<MetaNivel>())
                if (m.fuenteFinal != null) m.fuenteFinal.clip = final;

            if (avisar)
            {
                int faltan = 0;
                if (fuego == null) faltan++;
                if (llave == null) faltan++;
                if (puerta == null) faltan++;
                if (cerradura == null) faltan++;
                if (faltan > 0)
                    Debug.LogWarning("[Cripta] Faltan " + faltan +
                                     " clips de audio en " + RUTA_AUDIO +
                                     ". Revisa los nombres indicados en LEEME.txt.");
            }
        }

        static void RegistrarEnBuildSettings()
        {
            List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool existe = false;
            for (int i = 0; i < lista.Count; i++)
                if (lista[i].path == RUTA_ESCENA) existe = true;

            if (!existe)
            {
                lista.Insert(0, new EditorBuildSettingsScene(RUTA_ESCENA, true));
                EditorBuildSettings.scenes = lista.ToArray();
            }
        }
    }
}
