using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Diagnostic-only component: logs every InputSystem device present, and per-touch displayIndex for
    /// every Touchscreen device, to determine whether the AYN Thor exposes its two screens as one
    /// touchscreen device with a varying per-touch displayIndex, or as two separate touchscreen devices
    /// each fixed to one display. This does NOT touch the shared EventSystem/StandaloneInputModule the
    /// Android soft-keyboard bridge depends on - it reads Input System devices directly, independent of
    /// uGUI's event pipeline, so it's safe to run alongside the existing input setup.
    /// </summary>
    public class SecondScreenInputDiagnostics : MonoBehaviour
    {
        void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            LogAllDevices("initial");
        }

        void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            Debug.LogFormat("[SecondScreenInput] Device {0}: {1} ({2})", change, device.displayName, device.deviceId);
            LogAllDevices("post-change");
        }

        void LogAllDevices(string label)
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is Touchscreen touchscreen)
                {
                    Debug.LogFormat("[SecondScreenInput] ({0}) Touchscreen id={1} name={2} device.displayIndex={3}",
                        label, touchscreen.deviceId, touchscreen.displayName, touchscreen.displayIndex.ReadValue());
                }
                else
                {
                    Debug.LogFormat("[SecondScreenInput] ({0}) Device id={1} name={2} layout={3}",
                        label, device.deviceId, device.displayName, device.layout);
                }
            }
        }

        void Update()
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (!(device is Touchscreen touchscreen))
                    continue;

                foreach (TouchControl touch in touchscreen.touches)
                {
                    if (touch.press.wasPressedThisFrame)
                    {
                        Debug.LogFormat("[SecondScreenInput] TOUCH BEGAN screen.id={0} touchId={1} pos={2} touch.displayIndex={3}",
                            touchscreen.deviceId, touch.touchId.ReadValue(), touch.position.ReadValue(), touch.displayIndex.ReadValue());
                    }
                }
            }
        }
    }
}
