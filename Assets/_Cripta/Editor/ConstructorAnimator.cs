using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cripta.Herramientas
{
    public static class ConstructorAnimator
    {
        const string RAIZ = "Assets/_Cripta";
        const string RUTA_ANIMACION = RAIZ + "/Animacion";
        const string RUTA_CONTROLADOR = RUTA_ANIMACION + "/JugadorAnimator.controller";

        const string BUSCA_PERSONAJE = "Paladin";
        const string BUSCA_IDLE = "idle";
        const string BUSCA_CAMINAR = "walking";
        const string BUSCA_CORRER = "running";
        const string BUSCA_SALTO = "jumping";

        [MenuItem("Cripta/3. Configurar personaje y animaciones", false, 12)]
        public static void Configurar()
        {
            string rutaPersonaje = BuscarModelo(BUSCA_PERSONAJE);
            if (rutaPersonaje == null)
            {
                Debug.LogError("[Cripta] No se encontro el FBX del personaje (busqueda: \"" +
                               BUSCA_PERSONAJE + "\"). Importalo en Assets y vuelve a ejecutar.");
                return;
            }

            string rutaIdle = BuscarModelo(BUSCA_IDLE);
            string rutaCaminar = BuscarModelo(BUSCA_CAMINAR);
            string rutaCorrer = BuscarModelo(BUSCA_CORRER);
            string rutaSalto = BuscarModelo(BUSCA_SALTO);

            if (rutaIdle == null || rutaCaminar == null || rutaCorrer == null || rutaSalto == null)
            {
                Debug.LogError("[Cripta] Faltan FBX de animacion. Se necesitan archivos cuyo nombre contenga: " +
                               BUSCA_IDLE + ", " + BUSCA_CAMINAR + ", " + BUSCA_CORRER + ", " + BUSCA_SALTO + ".");
                return;
            }

            AsegurarCarpeta(RUTA_ANIMACION);

            ConfigurarPersonaje(rutaPersonaje);
            Avatar avatar = AvatarDe(rutaPersonaje);

            if (avatar == null)
            {
                Debug.LogError("[Cripta] El personaje no genero un Avatar humanoide. Revisa el rig en el Inspector.");
                return;
            }

            ConfigurarAnimacion(rutaIdle, avatar, true);
            ConfigurarAnimacion(rutaCaminar, avatar, true);
            ConfigurarAnimacion(rutaCorrer, avatar, true);
            ConfigurarAnimacion(rutaSalto, avatar, false);

            AnimationClip clipIdle = ClipDe(rutaIdle);
            AnimationClip clipCaminar = ClipDe(rutaCaminar);
            AnimationClip clipCorrer = ClipDe(rutaCorrer);
            AnimationClip clipSalto = ClipDe(rutaSalto);

            if (clipIdle == null || clipCaminar == null || clipCorrer == null || clipSalto == null)
            {
                Debug.LogError("[Cripta] Alguno de los FBX no contiene un AnimationClip utilizable.");
                return;
            }

            AnimatorController controlador = CrearControlador(clipIdle, clipCaminar, clipCorrer, clipSalto);
            MontarEnEscena(rutaPersonaje, avatar, controlador);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Cripta] Personaje y animaciones configurados. Controlador en " + RUTA_CONTROLADOR);
        }

        static string BuscarModelo(string termino)
        {
            string[] guids = AssetDatabase.FindAssets(termino + " t:Model");
            for (int i = 0; i < guids.Length; i++)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
                string archivo = System.IO.Path.GetFileNameWithoutExtension(ruta).ToLowerInvariant();
                if (archivo.Contains(termino.ToLowerInvariant())) return ruta;
            }
            return null;
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

        static void ConfigurarPersonaje(string ruta)
        {
            ModelImporter importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (importador == null) return;

            bool cambio = false;

            if (importador.animationType != ModelImporterAnimationType.Human)
            {
                importador.animationType = ModelImporterAnimationType.Human;
                importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                cambio = true;
            }

            if (cambio) importador.SaveAndReimport();
        }

        static void ConfigurarAnimacion(string ruta, Avatar avatar, bool enBucle)
        {
            ModelImporter importador = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (importador == null) return;

            importador.animationType = ModelImporterAnimationType.Human;
            importador.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importador.sourceAvatar = avatar;
            importador.importAnimation = true;

            ModelImporterClipAnimation[] clips = importador.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].loopTime = enBucle;
                    clips[i].lockRootHeightY = true;
                    clips[i].keepOriginalPositionY = true;
                }
                importador.clipAnimations = clips;
            }

            importador.SaveAndReimport();
        }

        static Avatar AvatarDe(string ruta)
        {
            Object[] activos = AssetDatabase.LoadAllAssetsAtPath(ruta);
            for (int i = 0; i < activos.Length; i++)
            {
                Avatar avatar = activos[i] as Avatar;
                if (avatar != null) return avatar;
            }
            return null;
        }

        static AnimationClip ClipDe(string ruta)
        {
            Object[] activos = AssetDatabase.LoadAllAssetsAtPath(ruta);
            for (int i = 0; i < activos.Length; i++)
            {
                AnimationClip clip = activos[i] as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview__")) return clip;
            }
            return null;
        }

        static AnimatorController CrearControlador(AnimationClip idle, AnimationClip caminar,
                                                   AnimationClip correr, AnimationClip salto)
        {
            AssetDatabase.DeleteAsset(RUTA_CONTROLADOR);

            AnimatorController controlador = AnimatorController.CreateAnimatorControllerAtPath(RUTA_CONTROLADOR);
            controlador.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
            controlador.AddParameter("EnSuelo", AnimatorControllerParameterType.Bool);
            controlador.AddParameter("Saltar", AnimatorControllerParameterType.Trigger);

            BlendTree arbol;
            AnimatorState locomocion = controlador.CreateBlendTreeInController("Locomocion", out arbol, 0);
            arbol.blendType = BlendTreeType.Simple1D;
            arbol.blendParameter = "Velocidad";
            arbol.useAutomaticThresholds = false;
            arbol.AddChild(idle, 0f);
            arbol.AddChild(caminar, 0.5f);
            arbol.AddChild(correr, 1f);

            AnimatorStateMachine maquina = controlador.layers[0].stateMachine;
            maquina.defaultState = locomocion;

            AnimatorState estadoSalto = maquina.AddState("Salto");
            estadoSalto.motion = salto;

            AnimatorStateTransition haciaSalto = locomocion.AddTransition(estadoSalto);
            haciaSalto.hasExitTime = false;
            haciaSalto.duration = 0.08f;
            haciaSalto.AddCondition(AnimatorConditionMode.If, 0f, "Saltar");

            AnimatorStateTransition haciaSuelo = estadoSalto.AddTransition(locomocion);
            haciaSuelo.hasExitTime = false;
            haciaSuelo.duration = 0.15f;
            haciaSuelo.AddCondition(AnimatorConditionMode.If, 0f, "EnSuelo");

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        static void MontarEnEscena(string rutaPersonaje, Avatar avatar, AnimatorController controlador)
        {
            GameObject jugador = BuscarJugador();
            if (jugador == null)
            {
                Debug.LogWarning("[Cripta] No hay un objeto \"Jugador\" en la escena abierta. " +
                                 "Construye el nivel primero y vuelve a ejecutar este paso.");
                return;
            }

            Transform anterior = jugador.transform.Find("Modelo");
            if (anterior != null) Object.DestroyImmediate(anterior.gameObject);

            GameObject fuente = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPersonaje);
            GameObject modelo = PrefabUtility.InstantiatePrefab(fuente, jugador.transform) as GameObject;
            if (modelo == null) modelo = Object.Instantiate(fuente, jugador.transform);

            modelo.name = "Modelo";
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            modelo.transform.localScale = Vector3.one;

            Animator animador = modelo.GetComponent<Animator>();
            if (animador == null) animador = modelo.AddComponent<Animator>();

            animador.avatar = avatar;
            animador.runtimeAnimatorController = controlador;
            animador.applyRootMotion = false;

            string[] marcadores = { "Cuerpo", "Cabeza", "Marca frontal" };
            for (int i = 0; i < marcadores.Length; i++)
            {
                Transform marcador = jugador.transform.Find(marcadores[i]);
                if (marcador != null) marcador.gameObject.SetActive(false);
            }

            ControladorJugador control = jugador.GetComponent<ControladorJugador>();
            if (control != null) control.animador = animador;

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        static GameObject BuscarJugador()
        {
            Scene escena = SceneManager.GetActiveScene();
            if (!escena.IsValid() || !escena.isLoaded) return null;

            GameObject[] raices = escena.GetRootGameObjects();
            for (int i = 0; i < raices.Length; i++)
            {
                if (raices[i].name == "Jugador") return raices[i];

                ControladorJugador control = raices[i].GetComponentInChildren<ControladorJugador>(true);
                if (control != null) return control.gameObject;
            }
            return null;
        }
    }
}
