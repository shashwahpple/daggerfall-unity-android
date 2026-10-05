// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2024 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Vivian V Wing (vwing@multitude.city)
// Contributors:
// 
// Notes:
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using System.Linq;

namespace DaggerfallWorkshop.Game
{
    public class TouchscreenKeyboardManager : MonoBehaviour
    {
        public static TouchscreenKeyboardManager Instance { get; private set; }
        public static bool SubmittedInput { get; private set; }
        public bool IsKeyboardActive { get { return currentTextbox != null; } }

        // Fired whenever a textbox wants keyboard input (ToggleKeyboardOn) or gives it up
        // (ToggleKeyboardOff) - lets the Display 2 keyboard panel (see Assets/Android/Scripts/SecondScreen)
        // show/hide itself without duplicating this class's own tap-detection/visibility logic.
        public event Action<TextBox> OnKeyboardRequested;
        public event Action OnKeyboardDismissed;

        [SerializeField] private TMPro.TMP_InputField dummyInputField;
        private TextBox currentTextbox;
        private HashSet<TextBox> registeredTextboxes = new HashSet<TextBox>();

        private void Awake()
        {
            if (Instance) {
                Debug.LogError("Extra instance of touchscreen keyboard manager singleton is present! Destroying self.");
                Destroy(this);
                return;
            }
    
            Instance = this;
        }
        private void Start()
        {
            GameObject inputFieldGO = new GameObject("Dummy Input Field");
            inputFieldGO.transform.parent = transform;
            dummyInputField.gameObject.SetActive(false);
            dummyInputField.onValueChanged.AddListener(OnDummyInputFieldChanged);
            dummyInputField.onSubmit.AddListener(OnDummyInputFieldSubmit);
            dummyInputField.onEndEdit.AddListener((string str) => ToggleKeyboardOff());
        }
        private bool IsTextboxVisible(TextBox textbox)
        {
            // checks if textbox is visible on the screen
            BaseScreenComponent cur = textbox;
            while(cur != null)
            {
                if(cur == null || !cur.Enabled)
                    return false;
                if (cur.Parent != null && DaggerfallUI.UIManager.TopWindow.ParentPanel == cur.Parent)
                    return true;
                cur = cur.Parent;
            }
            return false;
        }
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // check if mouse clicked within a valid textbox
                Vector2 mousePos = Input.mousePosition;
                mousePos.y = AScreen.height - mousePos.y;
                var activeTextboxes = registeredTextboxes.Where(p => IsTextboxVisible(p) && !p.ReadOnly);
                TextBox textBox = activeTextboxes.Where(p =>
                    {
                        Rect rect = p.Rectangle;
                        rect.width = rect.width == 0 ? 1000 : rect.width;
                        return rect.Contains(mousePos);
                    }).Take(1).SingleOrDefault();

                if (textBox != default(TextBox))
                    ToggleKeyboardOn(textBox); // it did! Open the keyboard.
            }
        }
        public void RegisterTextbox(TextBox textBox) => registeredTextboxes.Add(textBox);
        public void UnregisterTextbox(TextBox textBox) => registeredTextboxes.Remove(textBox);

        // Manual override for the Display 2 Home tab's "Keyboard" button - opens the keyboard for
        // whatever registered textbox is currently visible, same visibility rule Update()'s own tap
        // detection uses above, without needing a precise tap to land on Display 1's (often small) field.
        // Returns false (and opens nothing) if no visible textbox exists right now.
        public bool TryOpenKeyboardForVisibleTextbox()
        {
            TextBox textBox = registeredTextboxes.FirstOrDefault(p => IsTextboxVisible(p) && !p.ReadOnly);
            if (textBox == null)
                return false;

            ToggleKeyboardOn(textBox);
            return true;
        }

        public void ToggleKeyboardOn(TextBox textBox)
        {
            // Debug.Log("Opening android keyboard for textbox: " + textBox.Name + " with text: " + textBox.Text);
            this.currentTextbox = textBox;
            OnKeyboardRequested?.Invoke(textBox);

            // On a dual-screen device the Display 2 keyboard panel handles this instead (it subscribes to
            // OnKeyboardRequested above) - don't also pop the native Android keyboard over Display 1.
            // currentTextbox is still set either way, since that's what OnDummyInputFieldChanged/our own
            // Display 2 keyboard both write typed text into.
            if (Display.displays.Length >= 2)
                return;

            dummyInputField.text = textBox.Text;
            dummyInputField.gameObject.SetActive(true);
            dummyInputField.Select();
        }
        public void ToggleKeyboardOff()
        {
            currentTextbox = null;
            dummyInputField.text = "";
            dummyInputField.gameObject.SetActive(false);
            OnKeyboardDismissed?.Invoke();
        }
        private IEnumerator SubmitCoroutine()
        {
            // ensure everything sees 'submittedinput' for a single frame, regardless of script execution order
            yield return new WaitForEndOfFrame();
            SubmittedInput = true;
            yield return new WaitForEndOfFrame();
            SubmittedInput = false;
        }
        // Lets an external input source (the Display 2 keyboard's own Enter key) signal "submit" the same
        // way the native Android keyboard's own submit does, without needing to drive dummyInputField
        // itself - DaggerfallInputMessageBox and TextBox both already check TouchscreenKeyboardManager.
        // SubmittedInput directly (see Part 2 research), this just reuses that exact proven mechanism.
        public void SignalSubmit()
        {
            StartCoroutine(SubmitCoroutine());
            ToggleKeyboardOff();
        }
        private void OnDummyInputFieldSubmit(string submittedVal) => SignalSubmit();
        private void OnDummyInputFieldChanged(string newVal)
        {
            Debug.Log("dummy input changed: " + newVal);
            if(currentTextbox != null)
                currentTextbox.Text = newVal;
        }
    }
}