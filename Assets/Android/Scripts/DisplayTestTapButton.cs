using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Interactive tap target for the display test scene. Flashes a distinct color and increments
    /// a per-display counter on tap, and logs the display index Unity's EventSystem assigned to the
    /// pointer event (PointerEventData.displayIndex) alongside the display this button actually lives
    /// on, so a misrouted tap (wrong display's canvas receiving the event) is visible in logcat rather
    /// than only as a visual glitch.
    /// </summary>
    public class DisplayTestTapButton : MonoBehaviour, IPointerClickHandler
    {
        public int ExpectedDisplayIndex;
        public Image TargetImage;
        public TMP_Text CounterText;
        public Color IdleColor;
        public Color TapColor;

        int tapCount;
        Coroutine revertRoutine;

        public void OnPointerClick(PointerEventData eventData)
        {
            tapCount++;
            CounterText.text = "Taps: " + tapCount;

            if (revertRoutine != null)
                StopCoroutine(revertRoutine);
            TargetImage.color = TapColor;
            revertRoutine = StartCoroutine(RevertColorAfterDelay());

            bool misrouted = eventData.displayIndex != ExpectedDisplayIndex;
            Debug.LogFormat("[DisplayTest] TAP button=Display{0} eventData.displayIndex={1} pos={2} count={3}{4}",
                ExpectedDisplayIndex, eventData.displayIndex, eventData.position, tapCount,
                misrouted ? " ** MISROUTED: eventData.displayIndex does not match the display this button is on **" : "");
        }

        IEnumerator RevertColorAfterDelay()
        {
            yield return new WaitForSeconds(0.2f);
            TargetImage.color = IdleColor;
            revertRoutine = null;
        }
    }
}
