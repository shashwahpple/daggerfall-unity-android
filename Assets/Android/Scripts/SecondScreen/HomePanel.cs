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

            GameObject gridGO = new GameObject("ButtonGrid");
            gridGO.transform.SetParent(rootGO.transform, false);
            RectTransform gridRect = gridGO.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.1f, 0.1f);
            gridRect.anchorMax = new Vector2(0.9f, 0.9f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            // Fixed 2-column grid, fixed cell size - comfortably fits all 6 buttons here.
            GridLayoutGroup grid = gridGO.AddComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(30f, 30f);
            grid.cellSize = new Vector2(440f, 140f);

            AddButton(gridGO.transform, "Fast Travel", OnFastTravelClicked);
            AddButton(gridGO.transform, "Local Map", OnLocalMapClicked);
            AddButton(gridGO.transform, "Status", OnStatusClicked);
            AddButton(gridGO.transform, "Travel Options", OnTravelOptionsClicked);
            AddButton(gridGO.transform, "Notes", OnNotesClicked);
            AddButton(gridGO.transform, "Keyboard", OnKeyboardClicked);
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

        void OnTravelOptionsClicked()
        {
            // No new save to test this with yet (need one that owns a horse or cart) - kept minimal:
            // TransportManager.ToggleMount() is the same cycling logic (Foot -> Horse/Cart if owned ->
            // back to Foot, skipping whatever isn't owned) the real DaggerfallTransportWindow's buttons
            // drive, so this reuses it directly rather than reimplementing the HasHorse()/HasCart()
            // checks ourselves. Doesn't cycle to Ship - ToggleMount() itself doesn't either.
            if (GameManager.IsGamePaused || DaggerfallUI.UIManager.WindowCount > 0)
                return;
            if (GameManager.Instance.IsPlayerInside || !GameManager.Instance.PlayerController.isGrounded)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            GameManager.Instance.TransportManager.ToggleMount();
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
