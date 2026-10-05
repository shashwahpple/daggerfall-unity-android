using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaggerfallWorkshop.Game;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Display 2 "Home" tab. Phase 1 of the deferred travel-map work (see project memory): rather than
    /// rebuilding the travel map's zoom/pan/search/filter UI in uGUI, this just opens the real
    /// DaggerfallTravelMapWindow on Display 1 - same "render on Display 1 for this interaction" tradeoff
    /// JournalPanel's travel-confirm flow already uses. DaggerfallUIMessages.dfuiOpenTravelMapWindow's
    /// handler (DaggerfallUI.cs) already does the full precondition check (indoors, enemies nearby,
    /// sunlight damage warning) that the real HUD's own map button goes through, so this button is just
    /// that same call.
    /// </summary>
    public class HomePanel : MonoBehaviour
    {
        void Start()
        {
            BuildUI();
        }

        void BuildUI()
        {
            // A root Canvas always fills the entire screen regardless of its own RectTransform's anchors
            // - Unity only honors anchors on children of a canvas. So the actual size-restricted panel
            // content has to live on a child RectTransform, not directly on this Canvas GameObject.
            GameObject rootGO = new GameObject("Root");
            rootGO.transform.SetParent(transform, false);
            RectTransform panelRect = rootGO.AddComponent<RectTransform>();
            // Vertical space above this is reserved for ContentTabStripPanel and InteractModeStripPanel -
            // see SecondScreenManager.
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = new Vector2(1f, 0.80f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            rootGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject buttonGO = new GameObject("FastTravelButton");
            buttonGO.transform.SetParent(rootGO.transform, false);
            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(420f, 120f);

            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);

            Button button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(OnFastTravelClicked);

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform, false);
            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "Fast Travel";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 36;
            label.color = Color.white;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        void OnFastTravelClicked()
        {
            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenTravelMapWindow);
        }
    }
}
