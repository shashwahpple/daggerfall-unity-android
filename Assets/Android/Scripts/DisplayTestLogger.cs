using System.Collections;
using UnityEngine;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Diagnostic component for the second-display test scene. Activates any secondary
    /// display Unity detects (inactive by default) and logs Display.displays info to logcat.
    /// </summary>
    public class DisplayTestLogger : MonoBehaviour
    {
        void Awake()
        {
            // Display.main (index 0) is always active; secondary displays must be activated manually
            // before Unity will render anything to them.
            for (int i = 1; i < Display.displays.Length; i++)
                Display.displays[i].Activate();
        }

        void Start()
        {
            LogDisplays("initial");
            StartCoroutine(LogDisplaysDelayed());
        }

        IEnumerator LogDisplaysDelayed()
        {
            // renderingWidth/renderingHeight on a just-activated display can read 0 until a frame
            // has actually been rendered to it, so log again once activation has had time to settle.
            yield return new WaitForSeconds(1f);
            LogDisplays("post-activation");
        }

        void LogDisplays(string label)
        {
            Debug.LogFormat("[DisplayTest] ({0}) Display.displays.Length = {1}", label, Display.displays.Length);
            for (int i = 0; i < Display.displays.Length; i++)
            {
                Display d = Display.displays[i];
                Debug.LogFormat("[DisplayTest] ({0}) Display {1}: system={2}x{3} rendering={4}x{5}",
                    label, i, d.systemWidth, d.systemHeight, d.renderingWidth, d.renderingHeight);
            }
        }
    }
}
