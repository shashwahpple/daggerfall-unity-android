using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Detects the game scene loading on a device with a second physical display (e.g. the AYN Thor)
    /// and sets up a persistent Display 2 UI: activates the display and spawns each second-screen panel
    /// on its own Canvas under one shared Display 2 camera.
    ///
    /// Deliberately does NOT touch the scene's single shared EventSystem/StandaloneInputModule. Swapping
    /// it for InputSystemUIInputModule does fix Display 2 touch routing, but that EventSystem is also
    /// relied on by this fork's Android soft-keyboard bridge (TouchscreenKeyboardManager.cs selects a
    /// hidden TMP_InputField to trigger TouchScreenKeyboard.Open, which routes through the same
    /// EventSystem) - swapping it broke text entry during character creation for the whole game. Instead,
    /// SecondScreenTouchDispatcher reads Display 2 touches directly from the Input System's Touchscreen
    /// devices (confirmed on-device: the AYN Thor exposes each screen as its own Touchscreen device with
    /// correct per-touch displayIndex) and raycasts against these panels' own GraphicRaycasters, bypassing
    /// the shared EventSystem entirely.
    /// </summary>
    public static class SecondScreenBootstrap
    {
        const string GameSceneName = "DaggerfallUnityGame";

        static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            if (subscribed)
                return;
            subscribed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameSceneName)
                return;

            // No-op on every normal single-display Android device - this is Thor-specific.
            if (Display.displays.Length < 2)
                return;

            // Guard against duplicate setup if the game scene is loaded more than once in one process
            // (e.g. quitting to the title screen and starting a new game).
            if (Object.FindObjectOfType<SecondScreenManager>() != null)
                return;

            new GameObject("SecondScreenManager").AddComponent<SecondScreenManager>();

            // Claims Display 2 in Android's window model so tapping it doesn't hand controller
            // focus to Android's SECONDARY_HOME resolution - see SecondScreenDisplayClaim.cs.
            SecondScreenDisplayClaim.Claim();
        }
    }

    public class SecondScreenManager : MonoBehaviour
    {
        void Awake()
        {
            Display.displays[1].Activate();

            Camera secondScreenCamera = CreateSecondScreenCamera();
            CreatePanel<InteractModeStripPanel>(secondScreenCamera, "InteractModeStripPanel");

            PaperDollPanel doll = CreatePanel<PaperDollPanel>(secondScreenCamera, "PaperDollPanel");
            EquipmentPanel equipment = CreatePanel<EquipmentPanel>(secondScreenCamera, "EquipmentPanel");

            SpellListPanel spellList = CreatePanel<SpellListPanel>(secondScreenCamera, "SpellListPanel");

            // Placeholder until Map's real content panel exists - that's a separate deferred phase.
            PlaceholderContentPanel mapPage = CreatePanel<PlaceholderContentPanel>(secondScreenCamera, "MapPage");
            mapPage.Message = "Map (coming soon)";

            // Created last so its tabs can reference every page's GameObject above, but still before
            // SecondScreenTouchDispatcher's one-time GraphicRaycaster scan below, which needs every
            // page's raycaster present (even ones this hides immediately) since it never rescans.
            ContentTabStripPanel tabStrip = CreatePanel<ContentTabStripPanel>(secondScreenCamera, "ContentTabStripPanel");
            tabStrip.AddTab("Inventory", doll.gameObject, equipment.gameObject);
            tabStrip.AddTab("Map", mapPage.gameObject);
            tabStrip.AddTab("Spell List", spellList.gameObject);
            tabStrip.BuildAndShowFirstTab();

            // Confirmed on-device: the AYN Thor exposes each screen as a separate Touchscreen device with
            // correct per-touch displayIndex, so route Display 2 taps by reading that directly instead of
            // touching the shared EventSystem/StandaloneInputModule (see SecondScreenTouchDispatcher).
            gameObject.AddComponent<SecondScreenTouchDispatcher>();
        }

        Camera CreateSecondScreenCamera()
        {
            GameObject camGO = new GameObject("Camera_SecondScreen");
            camGO.transform.SetParent(transform, false);

            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.targetDisplay = 1;

            // Restrict to the UI layer only. Unlike the isolated test scene, this camera lives in the
            // real streaming-world game scene, so it can't rely on being spatially far from world
            // geometry to avoid rendering it - the world streams in dynamically at arbitrary positions.
            cam.cullingMask = 1 << LayerMask.NameToLayer("UI");

            // No AudioListener here - the main Display 1 camera already has one, and Unity only
            // supports a single active AudioListener per scene.

            return cam;
        }

        T CreatePanel<T>(Camera camera, string name) where T : Component
        {
            GameObject canvasGO = new GameObject(name);
            canvasGO.transform.SetParent(transform, false);
            // New GameObjects default to the Default layer, not UI - without this the camera's
            // UI-only cullingMask renders nothing but its black clear color.
            canvasGO.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            return canvasGO.AddComponent<T>();
        }
    }
}
