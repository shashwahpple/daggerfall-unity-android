using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Routes taps and drags on Display 2 to the second-screen panels' own Button/ScrollRect handlers,
    /// without touching the shared EventSystem/StandaloneInputModule (which the Android soft-keyboard
    /// bridge depends on - see SecondScreenManager's class comment for why that can't be swapped for
    /// InputSystemUIInputModule).
    ///
    /// Reads touches directly from the Input System's Touchscreen devices, confirmed on-device to carry
    /// correct per-touch displayIndex on the AYN Thor (each screen is a separate Touchscreen device, each
    /// reporting its own display's index). Tracks each touch from press to release (keyed by touchId) and
    /// manually drives the same PointerDown/Drag/Click event sequence StandaloneInputModule would, against
    /// each panel's own GraphicRaycaster - so a small tap still clicks a Button/row, while a drag past
    /// EventSystem.pixelDragThreshold instead feeds a ScrollRect's OnBeginDrag/OnDrag/OnEndDrag, cancelling
    /// whatever the initial press would have clicked (same as normal EventSystem drag-vs-click handling).
    ///
    /// Also suppresses the shared EventSystem while any Display 2 touch is down. That EventSystem's
    /// StandaloneInputModule reads the legacy Input.touches API, which - confirmed on-device - carries no
    /// per-touch display information at all, so it treats every Display 2 touch as if it landed on Display
    /// 1 at the same raw coordinates. Without suppression this drives Display 1's own UI: dragging on
    /// Display 2 rotates the camera via VirtualJoystick's look-drag zone, and tapping Display 2 can press
    /// Display 1's floating on-screen buttons. Marked DefaultExecutionOrder(-1000) so this Update() runs
    /// before EventSystem's own per-frame Update() in the same frame a touch begins - otherwise the very
    /// first frame of a press could reach EventSystem before suppression takes effect.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SecondScreenTouchDispatcher : MonoBehaviour
    {
        const int SecondScreenDisplayIndex = 1;

        readonly List<GraphicRaycaster> raycasters = new List<GraphicRaycaster>();
        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
        readonly Dictionary<int, ActiveTouch> activeTouches = new Dictionary<int, ActiveTouch>();

        EventSystem eventSystem;
        bool sharedEventSystemSuppressed;

        class ActiveTouch
        {
            public PointerEventData EventData;
            public GameObject PressedObject; // Click target from the initial raycast, cleared if a drag starts.
            public GameObject DragObject;     // Nearest IBeginDragHandler ancestor from the initial raycast (typically a ScrollRect).
            public bool Dragging;
        }

        void Awake()
        {
            // Any EventSystem instance works here - PointerEventData just needs one for its constructor,
            // this never touches its module or calls SetSelectedGameObject/EventSystem.current.
            eventSystem = FindObjectOfType<EventSystem>();
            raycasters.AddRange(GetComponentsInChildren<GraphicRaycaster>(true));
        }

        void Update()
        {
            if (eventSystem == null || raycasters.Count == 0)
                return;

            bool display2TouchActive = false;

            foreach (InputDevice device in InputSystem.devices)
            {
                if (!(device is Touchscreen touchscreen))
                    continue;

                foreach (TouchControl touch in touchscreen.touches)
                {
                    if (touch.displayIndex.ReadValue() != SecondScreenDisplayIndex)
                        continue;

                    if (touch.press.isPressed)
                        display2TouchActive = true;

                    ProcessTouch(touch);
                }
            }

            UpdateSharedEventSystemSuppression(display2TouchActive);
        }

        void ProcessTouch(TouchControl touch)
        {
            int touchId = touch.touchId.ReadValue();

            if (touch.press.wasPressedThisFrame)
            {
                BeginTouch(touchId, touch.position.ReadValue());
                return;
            }

            if (!activeTouches.TryGetValue(touchId, out ActiveTouch active))
                return;

            Vector2 position = touch.position.ReadValue();

            if (touch.press.wasReleasedThisFrame)
            {
                EndTouch(active, position);
                activeTouches.Remove(touchId);
                return;
            }

            if (touch.press.isPressed)
                UpdateTouch(active, position);
        }

        void BeginTouch(int touchId, Vector2 position)
        {
            PointerEventData eventData = new PointerEventData(eventSystem)
            {
                position = position,
                pressPosition = position,
                displayIndex = SecondScreenDisplayIndex,
                button = PointerEventData.InputButton.Left,
            };

            ActiveTouch active = new ActiveTouch { EventData = eventData };

            raycastResults.Clear();
            foreach (GraphicRaycaster raycaster in raycasters)
                raycaster.Raycast(eventData, raycastResults);

            if (raycastResults.Count > 0)
            {
                eventData.pointerCurrentRaycast = raycastResults[0];
                eventData.pointerPressRaycast = raycastResults[0];

                GameObject hit = raycastResults[0].gameObject;
                active.PressedObject = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
                active.DragObject = ExecuteEvents.GetEventHandler<IBeginDragHandler>(hit);

                if (active.PressedObject != null)
                    ExecuteEvents.Execute(active.PressedObject, eventData, ExecuteEvents.pointerDownHandler);
            }

            activeTouches[touchId] = active;
        }

        void UpdateTouch(ActiveTouch active, Vector2 position)
        {
            PointerEventData eventData = active.EventData;
            eventData.delta = position - eventData.position;
            eventData.position = position;

            if (!active.Dragging)
            {
                if (Vector2.Distance(position, eventData.pressPosition) < eventSystem.pixelDragThreshold)
                    return;

                active.Dragging = true;
                if (active.DragObject != null)
                    ExecuteEvents.Execute(active.DragObject, eventData, ExecuteEvents.beginDragHandler);

                // A drag starting on a pressable row/button cancels that press, same as EventSystem does -
                // otherwise scrolling past a row on the way to a drag would also click it.
                if (active.PressedObject != null)
                {
                    ExecuteEvents.Execute(active.PressedObject, eventData, ExecuteEvents.pointerUpHandler);
                    active.PressedObject = null;
                }
            }

            if (active.DragObject != null)
                ExecuteEvents.Execute(active.DragObject, eventData, ExecuteEvents.dragHandler);
        }

        void EndTouch(ActiveTouch active, Vector2 position)
        {
            PointerEventData eventData = active.EventData;
            eventData.delta = position - eventData.position;
            eventData.position = position;

            if (active.Dragging)
            {
                if (active.DragObject != null)
                    ExecuteEvents.Execute(active.DragObject, eventData, ExecuteEvents.endDragHandler);
                return;
            }

            if (active.PressedObject == null)
                return;

            // Walk up from the hit graphic to find the actual click handler, same as StandaloneInputModule
            // does internally - the hit is often a child Label/Image rather than the Button/panel itself.
            ExecuteEvents.Execute(active.PressedObject, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(active.PressedObject, eventData, ExecuteEvents.pointerClickHandler);
        }

        void UpdateSharedEventSystemSuppression(bool suppress)
        {
            if (suppress == sharedEventSystemSuppressed)
                return;

            sharedEventSystemSuppressed = suppress;
            eventSystem.enabled = !suppress;
        }

        void OnDisable()
        {
            // Never leave Display 1 permanently deaf if this component is disabled/destroyed mid-touch.
            if (sharedEventSystemSuppressed && eventSystem != null)
            {
                sharedEventSystemSuppressed = false;
                eventSystem.enabled = true;
            }
        }
    }
}
