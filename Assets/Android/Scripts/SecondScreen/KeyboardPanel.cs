using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Display 2 on-screen keyboard. Unlike the other Display 2 panels this isn't a ContentTabStripPanel
    /// tab - text input can be requested from almost any Display 1 window (Notes, Find Location, character
    /// name entry, ...) regardless of which Display 2 tab happens to be showing - so this is its own
    /// always-present overlay, shown/hidden purely by subscribing to TouchscreenKeyboardManager's
    /// OnKeyboardRequested/OnKeyboardDismissed events (that class already defers to this panel instead of
    /// popping Android's native keyboard whenever a second display is present - see ToggleKeyboardOn()).
    ///
    /// Types directly into the target TextBox's own Text property, the same mechanism the native-keyboard
    /// path already uses (TouchscreenKeyboardManager.OnDummyInputFieldChanged) - so this has the same
    /// limitation that path has: TextBox's numeric/upperOnly restrictions aren't enforced here, since
    /// TextBox exposes no public getter for them. Cursor is always implicitly at the end of the string
    /// (TextBox has no public API to move it mid-string), so this only supports append/backspace editing.
    /// </summary>
    public class KeyboardPanel : MonoBehaviour
    {
        static readonly Color KeyColor = new Color(0.25f, 0.25f, 0.25f, 0.95f);
        static readonly Color ControlKeyColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        static readonly Color ShiftActiveColor = new Color(0.85f, 0.7f, 0.15f, 1f);

        static readonly string[] NumberRow = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        static readonly string[] TopRow = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
        static readonly string[] MidRow = { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
        static readonly string[] BotRow = { "Z", "X", "C", "V", "B", "N", "M" };

        GameObject rootGO;
        TextBox targetTextBox;
        bool shiftActive;
        Image shiftButtonImage;

        void Start()
        {
            // High sorting order so this draws above every other Display 2 panel's canvas regardless of
            // creation order in SecondScreenManager - it must overlay whatever tab is currently showing.
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 100;

            BuildUI();
            rootGO.SetActive(false);

            if (TouchscreenKeyboardManager.Instance != null)
            {
                TouchscreenKeyboardManager.Instance.OnKeyboardRequested += OnKeyboardRequested;
                TouchscreenKeyboardManager.Instance.OnKeyboardDismissed += OnKeyboardDismissed;
            }
        }

        void OnDestroy()
        {
            if (TouchscreenKeyboardManager.Instance != null)
            {
                TouchscreenKeyboardManager.Instance.OnKeyboardRequested -= OnKeyboardRequested;
                TouchscreenKeyboardManager.Instance.OnKeyboardDismissed -= OnKeyboardDismissed;
            }
        }

        void OnKeyboardRequested(TextBox textBox)
        {
            targetTextBox = textBox;
            shiftActive = false;
            shiftButtonImage.color = ControlKeyColor;
            rootGO.SetActive(true);
        }

        void OnKeyboardDismissed()
        {
            targetTextBox = null;
            rootGO.SetActive(false);
        }

        void BuildUI()
        {
            // Same content band every other tab page occupies (space above is reserved for
            // ContentTabStripPanel/InteractModeStripPanel) - this overlays that band, not the strips.
            rootGO = new GameObject("Root");
            rootGO.transform.SetParent(transform, false);
            RectTransform panelRect = rootGO.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = new Vector2(1f, 0.80f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            rootGO.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.98f);

            VerticalLayoutGroup vlayout = rootGO.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 12f;
            vlayout.padding = new RectOffset(20, 20, 20, 20);
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = true;

            BuildCharacterRow(rootGO.transform, NumberRow);
            BuildCharacterRow(rootGO.transform, TopRow);
            BuildCharacterRow(rootGO.transform, MidRow);
            BuildBottomLetterRow();
            BuildControlRow();
        }

        void BuildCharacterRow(Transform parent, string[] keys)
        {
            GameObject rowGO = NewRow(parent);
            foreach (string key in keys)
                AddKey(rowGO.transform, key, KeyColor, 1f, () => OnCharacterKey(key));
        }

        void BuildBottomLetterRow()
        {
            GameObject rowGO = NewRow(rootGO.transform);

            UnityEngine.UI.Button shiftButton = AddKey(rowGO.transform, "Shift", ControlKeyColor, 1.5f, OnShiftKey);
            shiftButtonImage = shiftButton.GetComponent<Image>();

            foreach (string key in BotRow)
                AddKey(rowGO.transform, key, KeyColor, 1f, () => OnCharacterKey(key));

            AddKey(rowGO.transform, "Back", ControlKeyColor, 1.5f, OnBackspaceKey);
        }

        void BuildControlRow()
        {
            GameObject rowGO = NewRow(rootGO.transform);
            AddKey(rowGO.transform, "Esc", ControlKeyColor, 1.5f, OnEscapeKey);
            AddKey(rowGO.transform, "Space", ControlKeyColor, 6f, OnSpaceKey);
            AddKey(rowGO.transform, "Enter", ControlKeyColor, 1.5f, OnEnterKey);
        }

        GameObject NewRow(Transform parent)
        {
            GameObject rowGO = new GameObject("Row");
            rowGO.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = rowGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return rowGO;
        }

        UnityEngine.UI.Button AddKey(Transform parent, string label, Color color, float flexibleWidth, UnityEngine.Events.UnityAction onClick)
        {
            GameObject keyGO = new GameObject("Key_" + label);
            keyGO.transform.SetParent(parent, false);

            LayoutElement layoutElement = keyGO.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = flexibleWidth;

            Image keyImage = keyGO.AddComponent<Image>();
            keyImage.color = color;

            UnityEngine.UI.Button button = keyGO.AddComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(onClick);

            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(keyGO.transform, false);
            TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 30;
            labelText.color = Color.white;
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        void OnCharacterKey(string key)
        {
            AppendText(shiftActive ? key.ToUpperInvariant() : key.ToLowerInvariant());
        }

        void OnSpaceKey()
        {
            AppendText(" ");
        }

        void OnShiftKey()
        {
            shiftActive = !shiftActive;
            shiftButtonImage.color = shiftActive ? ShiftActiveColor : ControlKeyColor;
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }

        void OnBackspaceKey()
        {
            if (targetTextBox == null || targetTextBox.Text.Length == 0)
                return;

            targetTextBox.Text = targetTextBox.Text.Substring(0, targetTextBox.Text.Length - 1);
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }

        void OnEnterKey()
        {
            // Reuses the exact same "submit" signal the native Android keyboard's own Enter already
            // drives (see TouchscreenKeyboardManager.SignalSubmit) - this also calls ToggleKeyboardOff()
            // internally, which fires OnKeyboardDismissed and hides this panel through the normal path.
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            TouchscreenKeyboardManager.Instance.SignalSubmit();
        }

        void OnEscapeKey()
        {
            // Must only dismiss the keyboard, never reach the game's own Escape handling - ToggleKeyboardOff()
            // only clears TouchscreenKeyboardManager's state and fires OnKeyboardDismissed, it never touches
            // DaggerfallUI.OnKeyPress/lastKeyCode.
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            TouchscreenKeyboardManager.Instance.ToggleKeyboardOff();
        }

        void AppendText(string s)
        {
            if (targetTextBox == null)
                return;
            if (targetTextBox.Text.Length >= targetTextBox.MaxCharacters)
                return;

            targetTextBox.Text += s;
        }
    }
}
