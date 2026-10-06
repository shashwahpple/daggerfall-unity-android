using UnityEngine;
using System.Collections.Generic;
using DaggerfallWorkshop.Game;

namespace OpenPointerCapture
{
    // Static class acting as a facade for mouse input,
    // using captured events when pointer capture is active.
    // Can be used in-place as a replacement for many Input. methods
    public static class CapturedInput
    {
        // Internal state for button presses (frame-based)
        private static Dictionary<int, bool> currentButtonState = new Dictionary<int, bool>();
        private static Dictionary<int, bool> lastButtonState = new Dictionary<int, bool>();
        private static List<int> buttonsToProcess = new List<int>(); // To iterate over relevant buttons

        // Internal state for scroll delta (frame-based)
        private static Vector2 capturedScrollDeltaThisFrame = Vector2.zero;

        // Internal state for pointer delta
        private static Vector2 capturedPointerDeltaThisFrame = Vector2.zero;

        // Match the Mouse X/Y axis sensitivity configured in ProjectSettings/InputManager.asset.
        private const float mouseAxisSensitivity = 0.1f;
        private const bool debugMouseLook = false;
        private static float nextDebugLogTime;

        // This flag indicates if the CapturedInput system is initialized and listening
        private static bool isInitialized = false;

        private static Vector2 simulatedMousePosition = Vector2.zero;

        // Call this from a MonoBehaviour's Start or Awake
        // to ensure events are subscribed only once.
        internal static void Initialize()
        {
            if (isInitialized) return;

            Debug.Log("CapturedInput: Initializing and subscribing to events.");

            PointerCaptureManager.OnCapturedPointerMoved += HandleCapturedPointerMoved;
            PointerCaptureManager.OnCapturedMouseButton += HandleCapturedMouseButton;
            PointerCaptureManager.OnCapturedScroll += HandleCapturedScroll;

            // Initialize button state dictionaries for the buttons we care about (0-6)
            for (int i = 0; i <= 6; ++i)
            {
                currentButtonState[i] = false;
                lastButtonState[i] = false;
                buttonsToProcess.Add(i);
            }

            isInitialized = true;
        }

        // Called from CapturedInputUpdater's LateUpdate to manage frame state
        internal static void ManualLateUpdate()
        {
            if (!isInitialized) return;

            // Update last frame's button state based on the current frame's state *before* events
            // are potentially processed for the next frame.
            foreach (int buttonIndex in buttonsToProcess)
            {
                lastButtonState[buttonIndex] = currentButtonState[buttonIndex];
            }


            // Reset deltas for the start of the next frame
            capturedScrollDeltaThisFrame = Vector2.zero;
            capturedPointerDeltaThisFrame = Vector2.zero; // If you were using this
        }

        private static void HandleCapturedPointerMoved(Vector2 delta)
        {
            // Store the latest movement delta. If you needed a facade like Input.GetAxis, you'd accumulate here.
            // For the example script's needs, we don't need to store this in CapturedInput.
            capturedPointerDeltaThisFrame += delta;
            simulatedMousePosition += delta;
        }

        private static void HandleCapturedMouseButton(int buttonIndex, bool isDown)
        {
            if (currentButtonState.ContainsKey(buttonIndex))
            {
                // Update the current state when an event is received
                currentButtonState[buttonIndex] = isDown;
                // Debug.Log($"CapturedInput: Button {buttonIndex} state updated to {isDown}");
            }
        }

        private static void HandleCapturedScroll(Vector2 delta)
        {
            // Accumulate scroll deltas in case multiple scroll events occur in a single frame
            capturedScrollDeltaThisFrame += delta;
            // Debug.Log($"CapturedInput: Scroll delta added: {delta}, total for frame: {capturedScrollDeltaThisFrame}");
        }

        internal static void SetSimulatedMousePosition(Vector2 position)
        {
            simulatedMousePosition = position;
        }

        // --- Facade Methods ---

        public static bool IsPointerCaptured
        {
            get { return isInitialized && PointerCaptureNativeInterface.isPointerCaptured(); }
        }

        public static bool GetMouseButtonDown(int button)
        {
            if (!isInitialized)
            {
                Debug.LogError("CapturedInput not initialized!");
                return Input.GetMouseButtonDown(button); // Fallback
            }

            if (PointerCaptureNativeInterface.isPointerCaptured())
            {
                // If captured, check our internal state transition from last frame to current frame
                bool wasDownLastFrame = lastButtonState.ContainsKey(button) ? lastButtonState[button] : false;
                bool isDownThisFrame = currentButtonState.ContainsKey(button) ? currentButtonState[button] : false;
                return isDownThisFrame && !wasDownLastFrame;
            }
            else
            {
                // If not captured, use Unity's default Input system. Android's touch-to-mouse
                // emulation carries no display attribution at all (see DualDisplayTouch), so a
                // Display 2 tap would otherwise read here as a Display 1 mouse click - suppress
                // just the raw Input.* reading while a Display 2 touch is active, not the
                // internal captured-button state above (that's driven by PointerCaptureManager,
                // an unrelated native pointer-capture path with nothing to do with touchscreen
                // display routing).
                bool wasDownLastFrame = lastButtonState.ContainsKey(button) ? lastButtonState[button] : false;
                bool isDownThisFrame = currentButtonState.ContainsKey(button) ? currentButtonState[button] : false;
                bool rawDown = !DualDisplayTouch.IsDisplay2TouchActive() && Input.GetMouseButtonDown(button);
                return rawDown || (isDownThisFrame && !wasDownLastFrame);
            }
        }

        public static bool GetMouseButtonUp(int button)
        {
            if (!isInitialized)
            {
                Debug.LogError("CapturedInput not initialized!");
                return Input.GetMouseButtonUp(button); // Fallback
            }

            if (PointerCaptureNativeInterface.isPointerCaptured())
            {
                // If captured, check our internal state transition from last frame to current frame
                bool wasDownLastFrame = lastButtonState.ContainsKey(button) ? lastButtonState[button] : false;
                bool isDownThisFrame = currentButtonState.ContainsKey(button) ? currentButtonState[button] : false;
                return !isDownThisFrame && wasDownLastFrame;
            }
            else
            {
                // If not captured, use Unity's default Input - same Display 2 suppression as
                // GetMouseButtonDown above.
                bool wasDownLastFrame = lastButtonState.ContainsKey(button) ? lastButtonState[button] : false;
                bool isDownThisFrame = currentButtonState.ContainsKey(button) ? currentButtonState[button] : false;
                bool rawUp = !DualDisplayTouch.IsDisplay2TouchActive() && Input.GetMouseButtonUp(button);
                return rawUp || (!isDownThisFrame && wasDownLastFrame);
            }
        }

        public static bool GetMouseButton(int button)
        {
            if (!isInitialized)
            {
                Debug.LogError("CapturedInput not initialized!");
                return Input.GetMouseButton(button); // Fallback
            }

            if (PointerCaptureNativeInterface.isPointerCaptured())
            {
                // If captured, return the current internal state
                return currentButtonState.ContainsKey(button) ? currentButtonState[button] : false;
            }
            else
            {
                // If not captured, use Unity's default Input - same Display 2 suppression as
                // GetMouseButtonDown above.
                bool isDownThisFrame = currentButtonState.ContainsKey(button) ? currentButtonState[button] : false;
                bool rawHeld = !DualDisplayTouch.IsDisplay2TouchActive() && Input.GetMouseButton(button);
                return rawHeld || isDownThisFrame;
            }
        }

        public static bool GetKey(KeyCode key)
        {
            int mouseButton = GetMouseButtonIndex(key);
            if (mouseButton != -1)
                return GetMouseButton(mouseButton);

            return Input.GetKey(key);
        }

        public static bool GetKeyDown(KeyCode key)
        {
            int mouseButton = GetMouseButtonIndex(key);
            if (mouseButton != -1)
                return GetMouseButtonDown(mouseButton);

            return Input.GetKeyDown(key);
        }

        public static bool GetKeyUp(KeyCode key)
        {
            int mouseButton = GetMouseButtonIndex(key);
            if (mouseButton != -1)
                return GetMouseButtonUp(mouseButton);

            return Input.GetKeyUp(key);
        }

        public static float GetAxis(string axisName)
        {
            if (!isInitialized)
            {
                Debug.LogError("CapturedInput not initialized!");
                return Input.GetAxis(axisName); // Fallback
            }

            // If pointer is captured, use our accumulated deltas for standard mouse axes
            if (PointerCaptureNativeInterface.isPointerCaptured())
            {
                switch (axisName)
                {
                    case "Mouse X":
                    {
                        float value = capturedPointerDeltaThisFrame.x * mouseAxisSensitivity;
                        return value;
                    }
                    case "Mouse Y":
                    {
                        float value = capturedPointerDeltaThisFrame.y * mouseAxisSensitivity;
                        return value;
                    }
                    case "Mouse ScrollWheel":
                        return capturedScrollDeltaThisFrame.y;
                    default:
                        return Input.GetAxis(axisName);
                }
            }
            else
            {
                // If not captured, use Unity's default Input system for all axes. Mouse X/Y is
                // what InputManager.UpdateControllerCursorPosition() reads to detect a drag and
                // drop out of controller-cursor mode - without this, a drag on Display 2 would
                // leak into that same check and affect Display 1's cursor mode.
                if ((axisName == "Mouse X" || axisName == "Mouse Y") && DualDisplayTouch.IsDisplay2TouchActive())
                    return 0f;

                if (axisName == "Mouse ScrollWheel")
                    return Input.GetAxis(axisName) + capturedScrollDeltaThisFrame.y;

                return Input.GetAxis(axisName);
            }
        }
        public static Vector3 mousePosition { get { return PointerCaptureNativeInterface.isPointerCaptured() ? new Vector3(simulatedMousePosition.x, simulatedMousePosition.y, 0) : Input.mousePosition; } }

        public static Vector2 mouseScrollDelta
        {
            get
            {
                if (!isInitialized)
                {
                    Debug.LogError("CapturedInput not initialized!");
                    return Input.mouseScrollDelta; // Fallback
                }

                if (PointerCaptureNativeInterface.isPointerCaptured())
                {
                    // If captured, return the accumulated delta for this frame
                    return capturedScrollDeltaThisFrame;
                }
                else
                {
                    // If not captured, use Unity's default Input
                    return Input.mouseScrollDelta + capturedScrollDeltaThisFrame;
                }
            }
        }

        private static int GetMouseButtonIndex(KeyCode key)
        {
            if (key < KeyCode.Mouse0 || key > KeyCode.Mouse6)
                return -1;

            return (int)key - (int)KeyCode.Mouse0;
        }
    }
}
