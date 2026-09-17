using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaggerfallWorkshop.Game;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Persistent Display 2 strip showing one button per PlayerActivate interaction mode, mirroring
    /// GameManager.Instance.PlayerActivate.ChangeInteractionMode(...) calls used by HUDLarge's own
    /// crosshair mode buttons (Assets/Scripts/Game/UserInterface/HUDLarge.cs:403-436). Highlights
    /// whichever mode is currently active, including changes made elsewhere (e.g. HUDLarge itself).
    /// </summary>
    public class InteractModeStripPanel : MonoBehaviour
    {
        static readonly PlayerActivateModes[] Modes =
        {
            PlayerActivateModes.Steal,
            PlayerActivateModes.Grab,
            PlayerActivateModes.Info,
            PlayerActivateModes.Talk,
        };

        static readonly string[] Labels = { "Steal", "Interact", "Examine", "Talk" };

        static readonly Color IdleColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        static readonly Color ActiveColor = new Color(0.85f, 0.7f, 0.15f, 1f);

        readonly Image[] buttonImages = new Image[Modes.Length];
        PlayerActivateModes lastKnownMode;
        bool hasKnownMode;

        void Start()
        {
            BuildUI();
            RefreshHighlight();
        }

        void Update()
        {
            // PlayerActivate exposes no change event, and HUDLarge's own crosshair buttons can change
            // the mode independently of this panel, so poll for external changes each frame.
            if (GameManager.Instance.PlayerActivate == null)
                return;

            if (!hasKnownMode || GameManager.Instance.PlayerActivate.CurrentMode != lastKnownMode)
                RefreshHighlight();
        }

        void BuildUI()
        {
            // A root Canvas always fills the entire screen regardless of its own RectTransform's anchors
            // - Unity only honors anchors on children of a canvas. So the actual size-restricted panel
            // content has to live on a child RectTransform, not directly on this Canvas GameObject.
            GameObject rootGO = new GameObject("Root");
            rootGO.transform.SetParent(transform, false);
            RectTransform panelRect = rootGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0.88f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            rootGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            HorizontalLayoutGroup layout = rootGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20f;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (int i = 0; i < Modes.Length; i++)
            {
                PlayerActivateModes mode = Modes[i];

                GameObject buttonGO = new GameObject("ModeButton_" + mode);
                buttonGO.transform.SetParent(rootGO.transform, false);

                Image image = buttonGO.AddComponent<Image>();
                image.color = IdleColor;
                buttonImages[i] = image;

                Button button = buttonGO.AddComponent<Button>();
                button.onClick.AddListener(() => OnModeButtonClicked(mode));

                GameObject labelGO = new GameObject("Label");
                labelGO.transform.SetParent(buttonGO.transform, false);
                TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
                label.text = Labels[i];
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 32;
                label.color = Color.white;
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }
        }

        void OnModeButtonClicked(PlayerActivateModes mode)
        {
            // Don't let Display 2 change gameplay-affecting state while the game is paused or a modal
            // window (dialogue, spellbook, death screen, etc.) is open on Display 1 - our panel lives on
            // a separate Canvas/EventSystem entirely and isn't blocked by DFU's own window-stack pausing.
            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            if (GameManager.Instance.PlayerActivate == null)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            GameManager.Instance.PlayerActivate.ChangeInteractionMode(mode);
            RefreshHighlight();
        }

        void RefreshHighlight()
        {
            if (GameManager.Instance.PlayerActivate == null)
                return;

            lastKnownMode = GameManager.Instance.PlayerActivate.CurrentMode;
            hasKnownMode = true;

            for (int i = 0; i < Modes.Length; i++)
                buttonImages[i].color = (Modes[i] == lastKnownMode) ? ActiveColor : IdleColor;
        }
    }
}
