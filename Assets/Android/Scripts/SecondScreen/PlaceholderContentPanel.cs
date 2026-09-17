using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Empty "coming soon" content page for a ContentTabStripPanel tab whose real panel doesn't exist yet
    /// (currently: Map). Builds its UI lazily in Start() the first time its GameObject is activated, same
    /// as every other second-screen panel - ContentTabStripPanel creates and immediately deactivates
    /// non-default tabs, so this only runs once the player actually switches to this tab.
    /// </summary>
    public class PlaceholderContentPanel : MonoBehaviour
    {
        public string Message = "Coming soon";

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

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rootGO.transform, false);
            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = Message;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 40;
            label.color = new Color(1f, 1f, 1f, 0.6f);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }
    }
}
