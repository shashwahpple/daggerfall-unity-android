using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Display 2 view of the active quest log - DaggerfallQuestJournalWindow's ActiveQuests mode only.
    /// FinishedQuests/Notebook/Messages are the player's own notebook (with their own add/move/remove
    /// actions) and are out of scope here.
    ///
    /// Tapping a quest entry offers to travel to its last-mentioned location, faithfully reproducing
    /// DaggerfallQuestJournalWindow.HandleQuestClicks/GetLastPlaceMentionedInMessage/CreateDialogBox -
    /// those are protected instance methods on that window and not otherwise reusable, the same reason
    /// EquipmentActions exists as its own extraction rather than calling into DaggerfallInventoryWindow.
    /// By design, that confirmation dialog (and the Travel Map window it can open) renders on Display 1
    /// like any other classic UI window, not on this panel - a deliberate companion-screen tradeoff, not
    /// a bug, per the "reduce Display 1, don't eliminate it" brief this tab was built under.
    /// </summary>
    public class JournalPanel : MonoBehaviour
    {
        const float RefreshInterval = 2f;

        /// <summary>
        /// Top edge of this panel's content area, as an anchor relative to its own transform. Defaults to
        /// the standard top-level convention (leaving 0.80-1.0 for a tab strip above), but CharacterPanel
        /// overrides this before Start() runs, since the nested Skills/Journal strip sits higher up than
        /// the outer strip would.
        /// </summary>
        public float ContentAnchorMaxY = 0.80f;

        RectTransform content;
        float refreshTimer;

        void Start()
        {
            BuildUI();
            Refresh();
        }

        void Update()
        {
            // Active quests can change from gameplay (new quest, log update, quest completed) while this
            // panel is visible - poll periodically rather than refreshing once.
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= RefreshInterval)
            {
                refreshTimer = 0f;
                Refresh();
            }
        }

        void BuildUI()
        {
            // A root Canvas always fills the entire screen regardless of its own RectTransform's anchors
            // - Unity only honors anchors on children of a canvas. So the actual size-restricted panel
            // content has to live on a child RectTransform, not directly on this Canvas GameObject.
            GameObject rootGO = new GameObject("Root");
            rootGO.transform.SetParent(transform, false);
            RectTransform panelRect = rootGO.AddComponent<RectTransform>();
            // Top of this local rect is reserved for the nested Skills/Journal tab strip - see
            // CharacterPanel.
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = new Vector2(1f, ContentAnchorMaxY);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            rootGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(rootGO.transform, false);
            RectTransform viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(20f, 20f);
            viewportRect.offsetMax = new Vector2(-20f, -20f);
            Image viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f); // Mask requires a Graphic to clip against
            Mask mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            content = contentGO.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);

            VerticalLayoutGroup layout = contentGO.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10f;
            layout.padding = new RectOffset(24, 24, 24, 24);

            ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = rootGO.AddComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        void Refresh()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            System.Collections.Generic.List<Message> questMessages = QuestMachine.Instance.GetAllQuestLogMessages();
            if (questMessages.Count == 0)
            {
                CreateRow("No active quests.", null);
                return;
            }

            foreach (Message message in questMessages)
                CreateRow(TokensToString(message.GetTextTokens()), message);
        }

        static string TokensToString(TextFile.Token[] tokens)
        {
            StringBuilder sb = new StringBuilder();
            foreach (TextFile.Token token in tokens)
            {
                if (token.formatting == TextFile.Formatting.NewLine)
                    sb.Append('\n');
                else if (!string.IsNullOrEmpty(token.text))
                    sb.Append(token.text);
            }
            return sb.ToString().Trim();
        }

        void CreateRow(string text, Message message)
        {
            GameObject rowGO = new GameObject("Entry");
            rowGO.transform.SetParent(content, false);

            Image rowImage = rowGO.AddComponent<Image>();
            rowImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Auto-sizes this row to its (possibly multi-line) text, same VerticalLayoutGroup +
            // ContentSizeFitter idiom the outer Content list above uses to size itself to its rows.
            VerticalLayoutGroup rowLayout = rowGO.AddComponent<VerticalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.padding = new RectOffset(20, 20, 16, 16);

            ContentSizeFitter rowFitter = rowGO.AddComponent<ContentSizeFitter>();
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.fontSize = 26f;
            label.color = Color.white;
            label.enableWordWrapping = true;
            // Text inset via margin, not rowLayout.padding alone - same fix as SkillsPanel.CreateRow found
            // necessary: a small residual clip on the leading glyph survives relying on the layout group's
            // own padding by itself.
            label.margin = new Vector4(32f, 0f, 20f, 0f);

            if (message != null)
            {
                Button button = rowGO.AddComponent<Button>();
                button.onClick.AddListener(() => OnQuestEntryClicked(message));
            }
        }

        void OnQuestEntryClicked(Message message)
        {
            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            // Faithful copy of DaggerfallQuestJournalWindow.HandleQuestClicks/GetLastPlaceMentionedInMessage.
            Place place = GetLastPlaceMentionedInMessage(message);
            if (place == null ||
                string.IsNullOrEmpty(place.SiteDetails.locationName) ||
                place.SiteDetails.locationName == GameManager.Instance.PlayerGPS.CurrentLocation.Name)
                return;

            string findPlaceName = place.SiteDetails.locationName;
            if (!DaggerfallUI.Instance.DfTravelMapWindow.CanFindPlace(place.SiteDetails.regionName, findPlaceName))
                return;

            // Workaround for quests compiled before DFU 0.15.0 or later - same as the original window.
            int regionIndex = MapsFile.PatchRegionIndex(place.SiteDetails.regionIndex, place.SiteDetails.regionName);

            string entryStr = string.Format(
                TextManager.Instance.GetLocalizedText("locationInRegionProvince"),
                TextManager.Instance.GetLocalizedLocationName(place.SiteDetails.mapId, findPlaceName),
                TextManager.Instance.GetLocalizedRegionName(regionIndex));

            DaggerfallMessageBox dialogBox = CreateConfirmDialog(entryStr, "confirmFind");
            dialogBox.OnButtonClick += (sender, button) => FindPlace_OnButtonClick(sender, button, place);
            DaggerfallUI.UIManager.PushWindow(dialogBox);
        }

        static void FindPlace_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons button, Place place)
        {
            sender.CloseWindow();
            if (button == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                DaggerfallUI.Instance.DfTravelMapWindow.GotoPlace(place);
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenTravelMapWindow);
            }
        }

        static DaggerfallMessageBox CreateConfirmDialog(string entryStr, string baseKey)
        {
            string heading = TextManager.Instance.GetLocalizedText(baseKey + "Head");
            string action = TextManager.Instance.GetLocalizedText(baseKey);
            string explanation = TextManager.Instance.GetLocalizedText(baseKey + "2");
            TextFile.Token[] tokens = new TextFile.Token[] {
                TextFile.CreateTextToken(heading), TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter), TextFile.NewLineToken,
                TextFile.CreateTextToken(action), TextFile.NewLineToken, TextFile.NewLineToken,
                new TextFile.Token() { text = entryStr, formatting = TextFile.Formatting.TextHighlight }, TextFile.CreateFormatToken(TextFile.Formatting.JustifyCenter), TextFile.NewLineToken,
                TextFile.CreateTextToken(explanation), TextFile.CreateFormatToken(TextFile.Formatting.EndOfRecord)
            };

            DaggerfallMessageBox dialogBox = new DaggerfallMessageBox(DaggerfallUI.UIManager);
            dialogBox.SetHighlightColor(Color.white);
            dialogBox.SetTextTokens(tokens);
            dialogBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            dialogBox.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
            return dialogBox;
        }

        static Place GetLastPlaceMentionedInMessage(Message message)
        {
            QuestMacroHelper helper = new QuestMacroHelper();
            QuestResource[] resources = helper.GetMessageResources(message);
            if (resources == null || resources.Length == 0)
                return null;

            Place lastPlace = null;
            foreach (QuestResource resource in resources)
            {
                if (resource is Place place)
                    lastPlace = place;
            }

            return lastPlace;
        }
    }
}
