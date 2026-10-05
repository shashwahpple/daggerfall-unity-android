using UnityEngine;
using UnityEngine.SceneManagement;
using DaggerfallWorkshop.Game;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Drives the AYN Thor's D-Pad, confirmed on-device (2026-10-05) to report as a hat switch on
    /// Axis5 (horizontal: Right=-1, Left=+1) and Axis6 (vertical: Up=-1, Down=+1) - not as JoystickButtonN
    /// presses, so InputManager.SetBinding (AynThorGamepadPreset's usual mechanism) can't express it.
    /// Polls those two axes directly and posts the same DaggerfallUIMessages the Inventory/CharacterSheet/
    /// AutoMap/Transport Actions would, firing once per press via rising-edge detection (GetAxisRaw has no
    /// GetKeyDown-style "this frame" equivalent).
    ///
    /// Up=Inventory, Down=CharacterSheet, Right=AutoMap (local map), Left=Transport, per the AYN Thor
    /// control scheme (2026-10-05).
    /// </summary>
    public static class AynThorDPadHandlerBootstrap
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

            if (Object.FindObjectOfType<AynThorDPadHandler>() != null)
                return;

            new GameObject("AynThorDPadHandler").AddComponent<AynThorDPadHandler>();
        }
    }

    public class AynThorDPadHandler : MonoBehaviour
    {
        const float Threshold = 0.5f;

        bool upHeld, downHeld, leftHeld, rightHeld;

        void Update()
        {
            // Only meaningful once the Thor preset has been applied - avoids this axis pair doing
            // anything unexpected on a keyboard/mouse or other-controller session.
            if (!InputManager.Instance.EnableController)
                return;

            float horizontal = Input.GetAxisRaw("Axis5");
            float vertical = Input.GetAxisRaw("Axis6");

            HandleDirection(vertical <= -Threshold, ref upHeld, DaggerfallUIMessages.dfuiOpenInventoryWindow);
            HandleDirection(vertical >= Threshold, ref downHeld, DaggerfallUIMessages.dfuiOpenCharacterSheetWindow);
            HandleDirection(horizontal <= -Threshold, ref rightHeld, DaggerfallUIMessages.dfuiOpenAutomap);
            HandleDirection(horizontal >= Threshold, ref leftHeld, DaggerfallUIMessages.dfuiOpenTransportWindow);
        }

        void HandleDirection(bool pressed, ref bool held, string uiMessage)
        {
            if (pressed && !held)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.PostMessage(uiMessage);
            }
            held = pressed;
        }
    }
}
