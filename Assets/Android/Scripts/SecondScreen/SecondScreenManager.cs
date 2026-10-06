using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

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

            HomePanel homePage = CreatePanel<HomePanel>(secondScreenCamera, "HomePage");

            CharacterPanel character = CreatePanel<CharacterPanel>(secondScreenCamera, "CharacterPanel");

            // Created last so its tabs can reference every page's GameObject above, but still before
            // SecondScreenTouchDispatcher's one-time GraphicRaycaster scan below, which needs every
            // page's raycaster present (even ones this hides immediately) since it never rescans.
            ContentTabStripPanel tabStrip = CreatePanel<ContentTabStripPanel>(secondScreenCamera, "ContentTabStripPanel");
            tabStrip.AddTab("Inventory", doll.gameObject, equipment.gameObject);
            tabStrip.AddTab("Home", homePage.gameObject);
            tabStrip.AddTab("Spell List", spellList.gameObject);
            tabStrip.AddTab("Character", character.gameObject);
            // Home has no real-window equivalent, so it gets no second-tap action.
            tabStrip.SetSecondTapAction("Inventory", OnInventorySecondTap);
            tabStrip.SetSecondTapAction("Spell List", OnSpellListSecondTap);
            tabStrip.SetSecondTapAction("Character", OnCharacterSecondTap);
            // Home is the default every time this is rebuilt (every scene load) - never persisted.
            tabStrip.BuildAndShowFirstTab("Home");

            // Not a tab - a global overlay that shows/hides itself over whatever tab is active whenever
            // Display 1 needs text input (see KeyboardPanel). Created last and given a high Canvas sorting
            // order so it draws above every panel above regardless of creation order.
            CreatePanel<KeyboardPanel>(secondScreenCamera, "KeyboardPanel");

            // Confirmed on-device: the AYN Thor exposes each screen as a separate Touchscreen device with
            // correct per-touch displayIndex, so route Display 2 taps by reading that directly instead of
            // touching the shared EventSystem/StandaloneInputModule (see SecondScreenTouchDispatcher).
            gameObject.AddComponent<SecondScreenTouchDispatcher>();
        }

        // Second-tap handlers for the outer ContentTabStripPanel - tapping an already-selected tab again
        // opens the matching real Display 1 window, toggling it closed on a third tap. Same
        // open-if-nothing-blocking / close-if-already-on-top pattern HomePanel's Local Map button uses,
        // and the same messages AynThorDPadHandler already posts for Inventory and Character.

        void OnInventorySecondTap()
        {
            if (DaggerfallUI.UIManager.TopWindow == DaggerfallUI.Instance.InventoryWindow)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.UIManager.PopWindow();
                return;
            }

            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenInventoryWindow);
        }

        void OnCharacterSecondTap()
        {
            // DaggerfallUI doesn't expose a public reference to its character sheet window instance (only
            // Inventory/Automap/ExteriorAutomap have one) - a type check is enough since there's only ever
            // one character sheet window instance and PopWindow() doesn't need the exact reference anyway.
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallCharacterSheetWindow)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.UIManager.PopWindow();
                return;
            }

            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenCharacterSheetWindow);
        }

        void OnSpellListSecondTap()
        {
            if (DaggerfallUI.UIManager.TopWindow is DaggerfallSpellBookWindow)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.UIManager.PopWindow();
                return;
            }

            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            // dfuiOpenSpellBookWindow's handler (DaggerfallUI.cs) already validates this itself (blocks
            // mid-cast-animation, shows "noSpellbook" HUD text if the player doesn't own one) - no extra
            // guard needed here.
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenSpellBookWindow);
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
