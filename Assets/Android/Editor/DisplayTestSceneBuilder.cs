using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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

            BuildDisplay(targetDisplay: 0, label: "Display1", bgColor: Color.blue, text: "DISPLAY 1");
            BuildDisplay(targetDisplay: 1, label: "Display2", bgColor: Color.red, text: "DISPLAY 2 TEST");

            new GameObject("DisplayTestLogger").AddComponent<DisplayTestLogger>();

            if (!Directory.Exists(SceneFolder))
                Directory.CreateDirectory(SceneFolder);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.LogFormat("[DisplayTest] Saved scene to {0}", ScenePath);
        }

        static void BuildDisplay(int targetDisplay, string label, Color bgColor, string text)
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
            StretchFull(tmp.rectTransform);
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
