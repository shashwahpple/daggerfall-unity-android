using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DaggerfallWorkshop.Game
{
    /// <summary>
    /// Per-display touch queries shared between the Display 2 input-suppression fix
    /// (OpenPointerCapture/CapturedInput.cs) and the cursor-mode/multitouch-gesture fixes in
    /// InputManager.cs, DaggerfallTravelMapWindow.cs and DaggerfallAutomapWindow.cs.
    ///
    /// Legacy UnityEngine.Input (Input.touches, Input.mousePosition, and Android's touch-to-mouse
    /// emulation feeding them) carries no display attribution at all - confirmed both by
    /// SecondScreenTouchDispatcher's own class notes and DisplayTestLogger's "RAW TOUCH" logging -
    /// so every display-aware check here goes through the Input System's per-display Touchscreen
    /// devices instead (confirmed on-device: the AYN Thor exposes each screen as its own
    /// Touchscreen device with correct per-touch displayIndex).
    /// </summary>
    public static class DualDisplayTouch
    {
        const int Display1Index = 0;
        const int Display2Index = 1;

        /// <summary>True if any touch is currently pressed on Display 2.</summary>
        public static bool IsDisplay2TouchActive()
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!(device is Touchscreen touchscreen))
                    continue;

                foreach (TouchControl touch in touchscreen.touches)
                {
                    if (touch.displayIndex.ReadValue() == Display2Index && touch.press.isPressed)
                        return true;
                }
            }
            return false;
        }

        /// <summary>True if a new touch began on Display 1 this frame.</summary>
        public static bool Display1TouchBeganThisFrame()
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!(device is Touchscreen touchscreen))
                    continue;

                foreach (TouchControl touch in touchscreen.touches)
                {
                    if (touch.displayIndex.ReadValue() == Display1Index && touch.press.wasPressedThisFrame)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// True if fingerId (from a legacy UnityEngine.Touch) matches a touch currently pressed on
        /// Display 2. Correlates the legacy Touch.fingerId against the Input System's per-touch
        /// touchId - both derive from the same native Android pointer id. Verify this numeric
        /// correlation on-device via logging before trusting it elsewhere.
        /// </summary>
        public static bool IsFingerIdOnDisplay2(int fingerId)
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!(device is Touchscreen touchscreen))
                    continue;

                foreach (TouchControl touch in touchscreen.touches)
                {
                    if (touch.displayIndex.ReadValue() == Display2Index &&
                        touch.press.isPressed &&
                        touch.touchId.ReadValue() == fingerId)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Input.touchCount filtered to exclude any touch attributed to Display 2 - use together
        /// with GetDisplay1Touch(i) in place of Input.touchCount/Input.GetTouch(i) for multitouch
        /// gesture code that reads the legacy touch list directly (DaggerfallTravelMapWindow,
        /// DaggerfallAutomapWindow), so a Display 2 touch can't be miscounted as an extra Display 1
        /// finger.
        /// </summary>
        public static int Display1TouchCount()
        {
            int count = 0;
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (!IsFingerIdOnDisplay2(Input.GetTouch(i).fingerId))
                    count++;
            }
            return count;
        }

        /// <summary>Like Input.GetTouch(index), but indexes only into the Display1TouchCount() subset.</summary>
        public static Touch GetDisplay1Touch(int index)
        {
            int seen = 0;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (IsFingerIdOnDisplay2(touch.fingerId))
                    continue;

                if (seen == index)
                    return touch;
                seen++;
            }

            throw new System.IndexOutOfRangeException("index");
        }
    }
}
