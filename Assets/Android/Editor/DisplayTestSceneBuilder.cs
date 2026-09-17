using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Builds a minimal scene for testing whether Unity can render to a second physical
    /// display on Android hardware (e.g. the AYN Thor's secondary display), before any
    /// DFU-specific UI is built on top of it.
    /// </summary>
    public static class DisplayTestSceneBuilder
    {
        const string SceneFolder = "Assets/Android/DisplayTest";
        const string ScenePath = SceneFolder + "/DisplayTestScene.unity";

        [MenuItem("Daggerfall Tools/Android/Create Display Test Scene")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // GraphicRaycaster filters hits per-canvas by comparing the canvas's targetDisplay against
            // the pointer event's displayIndex, so a single EventSystem is the correct setup in
            // principle. But the legacy StandaloneInputModule never populates PointerEventData.displayIndex
            // for touch input (confirmed on-device: Display 2 taps were misrouted to Display 1's canvas),
            // so this uses InputSystemUIInputModule instead, which resolves the correct display per touch.
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<InputSystemUIInputModule>();

            BuildDisplay(targetDisplay: 0, label: "Display1", bgColor: Color.blue, tapColor: Color.green, text: "DISPLAY 1");
            BuildDisplay(targetDisplay: 1, label: "Display2", bgColor: Color.red, tapColor: new Color(1f, 0.55f, 0f), text: "DISPLAY 2 TEST");

            new GameObject("DisplayTestLogger").AddComponent<DisplayTestLogger>();

            if (!Directory.Exists(SceneFolder))
                Directory.CreateDirectory(SceneFolder);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.LogFormat("[DisplayTest] Saved scene to {0}", ScenePath);
        }

        static void BuildDisplay(int targetDisplay, string label, Color bgColor, Color tapColor, string text)
        {
            // Cameras/canvases for each display are spatially separated far apart so their
            // Screen Space - Camera UI content (placed in front of each camera automatically)
            // can never overlap into the other camera's view frustum.
            GameObject camGO = new GameObject("Camera_" + label);
            camGO.transform.position = new Vector3(targetDisplay * 100000f, 0f, 0f);

            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bgColor;
            cam.targetDisplay = targetDisplay;
            if (targetDisplay == 0)
                camGO.AddComponent<AudioListener>();

            GameObject canvasGO = new GameObject("Canvas_" + label);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            // Required for any UI element on this canvas to receive pointer/touch events at all.
            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject imgGO = new GameObject("BackgroundImage");
            imgGO.transform.SetParent(canvasGO.transform, false);
            Image img = imgGO.AddComponent<Image>();
            img.color = bgColor;
            StretchFull(img.rectTransform);

            GameObject textGO = new GameObject("Label");
            textGO.transform.SetParent(canvasGO.transform, false);
            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 72;
            tmp.color = Color.white;
            RectTransform tmpRect = tmp.rectTransform;
            tmpRect.anchorMin = new Vector2(0f, 0.78f);
            tmpRect.anchorMax = new Vector2(1f, 1f);
            tmpRect.offsetMin = Vector2.zero;
            tmpRect.offsetMax = Vector2.zero;

            BuildTapButton(canvasGO.transform, targetDisplay, Color.white, tapColor);
        }

        static void BuildTapButton(Transform canvasTransform, int targetDisplay, Color idleColor, Color tapColor)
        {
            GameObject buttonGO = new GameObject("TapButton");
            buttonGO.transform.SetParent(canvasTransform, false);
            Image btnImg = buttonGO.AddComponent<Image>();
            btnImg.color = idleColor;
            RectTransform btnRect = btnImg.rectTransform;
            btnRect.anchorMin = new Vector2(0.3f, 0.4f);
            btnRect.anchorMax = new Vector2(0.7f, 0.62f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            GameObject btnLabelGO = new GameObject("TapButtonLabel");
            btnLabelGO.transform.SetParent(buttonGO.transform, false);
            TextMeshProUGUI btnLabelTmp = btnLabelGO.AddComponent<TextMeshProUGUI>();
            btnLabelTmp.text = "TAP";
            btnLabelTmp.alignment = TextAlignmentOptions.Center;
            btnLabelTmp.fontSize = 48;
            btnLabelTmp.color = Color.black;
            StretchFull(btnLabelTmp.rectTransform);

            GameObject counterGO = new GameObject("Counter");
            counterGO.transform.SetParent(canvasTransform, false);
            TextMeshProUGUI counterTmp = counterGO.AddComponent<TextMeshProUGUI>();
            counterTmp.text = "Taps: 0";
            counterTmp.alignment = TextAlignmentOptions.Center;
            counterTmp.fontSize = 48;
            counterTmp.color = Color.white;
            RectTransform counterRect = counterTmp.rectTransform;
            counterRect.anchorMin = new Vector2(0f, 0.2f);
            counterRect.anchorMax = new Vector2(1f, 0.35f);
            counterRect.offsetMin = Vector2.zero;
            counterRect.offsetMax = Vector2.zero;

            DisplayTestTapButton tapHandler = buttonGO.AddComponent<DisplayTestTapButton>();
            tapHandler.ExpectedDisplayIndex = targetDisplay;
            tapHandler.TargetImage = btnImg;
            tapHandler.CounterText = counterTmp;
            tapHandler.IdleColor = idleColor;
            tapHandler.TapColor = tapColor;
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
