using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaggerfallWorkshop.Game;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Display 2 "Home" tab. A grid of quick-access buttons that hand off to the real Display 1 windows
    /// for each action, rather than rebuilding their UI in uGUI - the same "render on Display 1 for this
    /// interaction" tradeoff JournalPanel's travel-confirm flow already uses.
    /// </summary>
    public class HomePanel : MonoBehaviour
    {
        static readonly Color IdleColor = new Color(0.25f, 0.25f, 0.25f, 0.95f);
        static readonly Color ActiveColor = new Color(0.85f, 0.7f, 0.15f, 1f);
        static readonly Color DisabledColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        Image footModeImage, horseModeImage, cartModeImage, shipModeImage;
        Button footModeButton, horseModeButton, cartModeButton, shipModeButton;

        void Start()
        {
            BuildUI();
        }

        void Update()
        {
            RefreshTransportModeHighlight();
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

            // Stacks the button grid and the Travel Options row below it, each centered at its own
            // natural width. childControlWidth/Height true so the parent actually applies each child's
            // own reported preferred size instead of just reading it - GridLayoutGroup auto-reports 910
            // wide for a 2-column fixed grid (2*440 cellSize + 30 spacing), and the Travel Options row
            // reports the same via its own LayoutElement below, so both end up exactly 910 wide without
            // duplicating that math here. Without control enabled, a child keeps its untouched default
            // RectTransform size (~100x100) and only LayoutGroup-driven children (like GridLayoutGroup's
            // own fixed-size cells) still render correctly - everything else, like the Travel Options
            // row's HorizontalLayoutGroup, would be squeezed into that leftover ~100px.
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(rootGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.1f);
            contentRect.anchorMax = new Vector2(0.9f, 0.9f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.spacing = 30f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            GameObject gridGO = new GameObject("ButtonGrid");
            gridGO.transform.SetParent(contentGO.transform, false);
            gridGO.AddComponent<RectTransform>();

            // Fixed 2-column grid, fixed cell size - comfortably fits the 5 buttons here (Travel
            // Options moved below, see AddTransportModeRow).
            GridLayoutGroup grid = gridGO.AddComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(30f, 30f);
            grid.cellSize = new Vector2(440f, 140f);

            AddButton(gridGO.transform, "Fast Travel", OnFastTravelClicked);
            AddButton(gridGO.transform, "Local Map", OnLocalMapClicked);
            AddButton(gridGO.transform, "Status", OnStatusClicked);
            AddButton(gridGO.transform, "Notes", OnNotesClicked);
            AddButton(gridGO.transform, "Keyboard", OnKeyboardClicked);

            AddTransportModeRow(contentGO.transform);
        }

        // A row of Foot/Horse/Cart/Ship keys below the main button grid, spanning the grid's own content
        // width (910 = 2*440 cellSize + 30 spacing) so it lines up with the buttons above it. 3 gaps of
        // 10px between 4 equal-width buttons comes out to exactly 220 each - half the width of the
        // buttons above. Sets TransportMode directly instead of opening the real DaggerfallTransportWindow,
        // per the same validation that window's own open message uses (see
        // TransportActions.CanChangeTransportMode()).
        void AddTransportModeRow(Transform parent)
        {
            GameObject rowGO = new GameObject("TravelOptionsRow");
            rowGO.transform.SetParent(parent, false);
            rowGO.AddComponent<RectTransform>();

            LayoutElement rowLayoutElement = rowGO.AddComponent<LayoutElement>();
            rowLayoutElement.preferredWidth = 910f;
            rowLayoutElement.preferredHeight = 100f;

            HorizontalLayoutGroup layout = rowGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            footModeButton = AddModeKey(rowGO.transform, "Foot", out footModeImage, () => OnTransportModeClicked(TransportModes.Foot));
            horseModeButton = AddModeKey(rowGO.transform, "Horse", out horseModeImage, () => OnTransportModeClicked(TransportModes.Horse));
            cartModeButton = AddModeKey(rowGO.transform, "Cart", out cartModeImage, () => OnTransportModeClicked(TransportModes.Cart));
            shipModeButton = AddModeKey(rowGO.transform, "Ship", out shipModeImage, () => OnTransportModeClicked(TransportModes.Ship));
        }

        Button AddModeKey(Transform parent, string label, out Image image, UnityEngine.Events.UnityAction onClick)
        {
            GameObject keyGO = new GameObject(label + "ModeButton");
            keyGO.transform.SetParent(parent, false);

            image = keyGO.AddComponent<Image>();
            image.color = IdleColor;

            Button button = keyGO.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(keyGO.transform, false);
            TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 22;
            labelText.color = Color.white;
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        void AddButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonGO = new GameObject(label.Replace(" ", "") + "Button");
            buttonGO.transform.SetParent(parent, false);

            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);

            Button button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform, false);
            TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 32;
            labelText.color = Color.white;
            RectTransform labelRect = labelText.rectTransform;
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
            // dfuiOpenTravelMapWindow's handler (DaggerfallUI.cs) already does the full precondition
            // check (indoors, enemies nearby, sunlight damage warning) the real HUD's own map button
            // goes through, so this is just that same call.
            DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenTravelMapWindow);
        }

        void OnLocalMapClicked()
        {
            // PushWindow has no dedup check, and DaggerfallAutomapWindow only closes itself by watching
            // its own specific keybind directly (not via any message-based toggle) - so a second press
            // of dfuiOpenAutomap while it's already open would stack a duplicate instead of closing it.
            // Close it ourselves when it's already the top window instead.
            //
            // The generic "don't act while something else is open" guard below only applies when we'd
            // be opening something new - closing the automap must stay allowed even though opening it
            // pauses the game and the automap itself counts toward WindowCount.
            bool automapOpen = DaggerfallUI.UIManager.TopWindow == DaggerfallUI.Instance.AutomapWindow ||
                                DaggerfallUI.UIManager.TopWindow == DaggerfallUI.Instance.ExteriorAutomapWindow;

            if (!automapOpen && (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0))
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            if (automapOpen)
                DaggerfallUI.UIManager.PopWindow();
            else
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenAutomap);
        }

        void OnStatusClicked()
        {
            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiStatusInfo);
        }

        void OnTransportModeClicked(TransportModes mode)
        {
            if (!TransportActions.CanChangeTransportMode())
                return;

            TransportManager transportManager = GameManager.Instance.TransportManager;
            switch (mode)
            {
                case TransportModes.Horse:
                    if (!transportManager.HasHorse())
                        return;
                    break;
                case TransportModes.Cart:
                    if (!transportManager.HasCart())
                        return;
                    break;
                case TransportModes.Ship:
                    if (!transportManager.ShipAvailiable())
                        return;
                    break;
            }

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            // Setting Ship here isn't a persistent mode switch - TransportManager.UpdateMode() teleports
            // to/from the player's ship and resets mode back to Foot synchronously, the same one-shot
            // action the real DaggerfallTransportWindow's own Ship button triggers.
            transportManager.TransportMode = mode;
        }

        void RefreshTransportModeHighlight()
        {
            if (footModeImage == null)
                return; // BuildUI() hasn't run yet (first frame)

            TransportManager transportManager = GameManager.Instance.TransportManager;
            bool canChange = TransportActions.CanChangeTransportMode();
            TransportModes currentMode = transportManager.TransportMode;

            SetModeKeyState(footModeButton, footModeImage, canChange, currentMode == TransportModes.Foot);
            SetModeKeyState(horseModeButton, horseModeImage, canChange && transportManager.HasHorse(), currentMode == TransportModes.Horse);
            SetModeKeyState(cartModeButton, cartModeImage, canChange && transportManager.HasCart(), currentMode == TransportModes.Cart);
            // Ship is never "current" - selecting it is a momentary board/unboard action, not a mode the
            // player stays in (see OnTransportModeClicked).
            SetModeKeyState(shipModeButton, shipModeImage, canChange && transportManager.ShipAvailiable(), false);
        }

        void SetModeKeyState(Button button, Image image, bool enabled, bool active)
        {
            button.interactable = enabled;
            image.color = !enabled ? DisabledColor : (active ? ActiveColor : IdleColor);
        }

        void OnNotesClicked()
        {
            // dfuiOpenNotebookWindow's handler (DaggerfallUI.cs) sets DisplayMode = Notebook before
            // pushing the shared DaggerfallQuestJournalWindow instance - this is the first real
            // end-to-end use of the Display 2 keyboard (see KeyboardPanel), since adding/editing a note
            // opens a DaggerfallInputMessageBox with an editable TextBox.
            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenNotebookWindow);
        }

        void OnKeyboardClicked()
        {
            // No "nothing else open" guard here on purpose - the whole point is reaching into whatever
            // text-input window (Enter Note, character name entry, ...) is already open on Display 1,
            // without needing a precise tap to land on its (often small) text field.
            if (TouchscreenKeyboardManager.Instance.TryOpenKeyboardForVisibleTextbox())
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }
    }
}
