using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Routes taps on Display 2 to the second-screen panels' own Button click handlers, without touching
    /// the shared EventSystem/StandaloneInputModule (which the Android soft-keyboard bridge depends on -
    /// see SecondScreenManager's class comment for why that can't be swapped for InputSystemUIInputModule).
    ///
    /// Reads touches directly from the Input System's Touchscreen devices, confirmed on-device to carry
    /// correct per-touch displayIndex on the AYN Thor (each screen is a separate Touchscreen device, each
    /// reporting its own display's index), then manually raycasts against each panel's own
    /// GraphicRaycaster and executes the click on whatever was hit - reusing the same Button components a
    /// normal EventSystem would drive, just with a hand-rolled dispatch path instead of one.
    /// </summary>
    public class SecondScreenTouchDispatcher : MonoBehaviour
    {
        const int SecondScreenDisplayIndex = 1;

        readonly List<GraphicRaycaster> raycasters = new List<GraphicRaycaster>();
        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        EventSystem eventSystem;

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

            foreach (InputDevice device in InputSystem.devices)
            {
                if (!(device is Touchscreen touchscreen))
                    continue;

                foreach (TouchControl touch in touchscreen.touches)
                {
                    if (!touch.press.wasPressedThisFrame)
                        continue;
                    if (touch.displayIndex.ReadValue() != SecondScreenDisplayIndex)
                        continue;

                    Dispatch(touch.position.ReadValue());
                }
            }
        }

        void Dispatch(Vector2 position)
        {
            PointerEventData eventData = new PointerEventData(eventSystem)
            {
                position = position,
                displayIndex = SecondScreenDisplayIndex,
            };

            raycastResults.Clear();
            foreach (GraphicRaycaster raycaster in raycasters)
                raycaster.Raycast(eventData, raycastResults);

            if (raycastResults.Count == 0)
                return;

            // Walk up from the hit graphic to find the actual click handler, same as StandaloneInputModule
            // does internally - the hit is often a child Label/Image rather than the Button/panel itself.
            GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(raycastResults[0].gameObject);
            if (target != null)
                ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
        }
    }
}
