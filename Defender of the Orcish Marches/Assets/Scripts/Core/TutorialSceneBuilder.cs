using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class TutorialSceneBuilder : MonoBehaviour
{
    [MenuItem("Game/Build Tutorial Scene")]
    public static void BuildTutorialScene()
    {
        EditorSceneManager.SaveOpenScenes();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camObj = new GameObject("Main Camera");
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.08f, 0.05f);
        cam.orthographic = true;
        camObj.AddComponent<AudioListener>();
        camObj.tag = "MainCamera";

        // Canvas
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Dark background panel
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.1f, 0.08f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Title text (gold, large, top area)
        var titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "TUTORIAL";
        titleTmp.fontSize = 56;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.9f, 0.75f, 0.3f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.82f);
        titleRect.anchorMax = new Vector2(0.9f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Body text (white, medium, left half)
        var bodyObj = new GameObject("BodyText");
        bodyObj.transform.SetParent(canvasObj.transform, false);
        var bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyTmp.text = "";
        bodyTmp.fontSize = 26;
        bodyTmp.color = Color.white;
        bodyTmp.alignment = TextAlignmentOptions.TopLeft;
        bodyTmp.richText = true;
        bodyTmp.textWrappingMode = TextWrappingModes.Normal;
        bodyTmp.lineSpacing = 5f;
        var bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.06f, 0.18f);
        bodyRect.anchorMax = new Vector2(0.46f, 0.78f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        // Illustration border (thin gold frame)
        var frameBorderObj = new GameObject("IllustrationBorder");
        frameBorderObj.transform.SetParent(canvasObj.transform, false);
        var borderImage = frameBorderObj.AddComponent<Image>();
        borderImage.color = new Color(0.6f, 0.5f, 0.2f, 0.8f);
        var borderRect = frameBorderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.49f, 0.17f);
        borderRect.anchorMax = new Vector2(0.95f, 0.79f);
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        // Illustration image (right half, inset inside border)
        var illustObj = new GameObject("IllustrationImage");
        illustObj.transform.SetParent(frameBorderObj.transform, false);
        var illustImage = illustObj.AddComponent<Image>();
        illustImage.color = Color.white;
        illustImage.preserveAspect = true;
        var illustRect = illustObj.GetComponent<RectTransform>();
        illustRect.anchorMin = Vector2.zero;
        illustRect.anchorMax = Vector2.one;
        illustRect.offsetMin = new Vector2(3f, 3f);
        illustRect.offsetMax = new Vector2(-3f, -3f);

        // Page indicator (bottom center)
        var pageObj = new GameObject("PageIndicator");
        pageObj.transform.SetParent(canvasObj.transform, false);
        var pageTmp = pageObj.AddComponent<TextMeshProUGUI>();
        pageTmp.text = "1 / 15";
        pageTmp.fontSize = 24;
        pageTmp.color = new Color(0.6f, 0.6f, 0.6f);
        pageTmp.alignment = TextAlignmentOptions.Center;
        var pageRect = pageObj.GetComponent<RectTransform>();
        pageRect.anchorMin = new Vector2(0.35f, 0.04f);
        pageRect.anchorMax = new Vector2(0.65f, 0.1f);
        pageRect.offsetMin = Vector2.zero;
        pageRect.offsetMax = Vector2.zero;

        // Back button (bottom-left)
        var backBtn = CreateTutorialButton("BackButton", "BACK", canvasObj.transform);
        var backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.05f, 0.04f);
        backRect.anchorMax = new Vector2(0.05f, 0.04f);
        backRect.sizeDelta = new Vector2(200, 55);
        backRect.pivot = new Vector2(0f, 0f);
        backRect.anchoredPosition = Vector2.zero;

        // Next button (bottom-right)
        var nextBtn = CreateTutorialButton("NextButton", "NEXT", canvasObj.transform);
        var nextRect = nextBtn.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.95f, 0.04f);
        nextRect.anchorMax = new Vector2(0.95f, 0.04f);
        nextRect.sizeDelta = new Vector2(200, 55);
        nextRect.pivot = new Vector2(1f, 0f);
        nextRect.anchoredPosition = Vector2.zero;

        // Play button (bottom-right, same position as Next, initially hidden)
        var playBtn = CreateTutorialButton("PlayButton", "PLAY NOW", canvasObj.transform);
        var playRect = playBtn.GetComponent<RectTransform>();
        playRect.anchorMin = new Vector2(0.95f, 0.04f);
        playRect.anchorMax = new Vector2(0.95f, 0.04f);
        playRect.sizeDelta = new Vector2(200, 55);
        playRect.pivot = new Vector2(1f, 0f);
        playRect.anchoredPosition = Vector2.zero;
        playBtn.gameObject.SetActive(false);

        // Exit button (top-right corner)
        var exitBtn = CreateTutorialButton("ExitButton", "EXIT", canvasObj.transform);
        var exitRect = exitBtn.GetComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(0.95f, 0.9f);
        exitRect.anchorMax = new Vector2(0.95f, 0.9f);
        exitRect.sizeDelta = new Vector2(150, 45);
        exitRect.pivot = new Vector2(1f, 0f);
        exitRect.anchoredPosition = Vector2.zero;

        // Manager object
        var mgrObj = new GameObject("TutorialManager");
        var tutMgr = mgrObj.AddComponent<TutorialManager>();
        mgrObj.AddComponent<SceneLoader>();

        // Wire references
        var tutSO = new SerializedObject(tutMgr);
        tutSO.FindProperty("nextButton").objectReferenceValue = nextBtn;
        tutSO.FindProperty("backButton").objectReferenceValue = backBtn;
        tutSO.FindProperty("playButton").objectReferenceValue = playBtn;
        tutSO.FindProperty("exitButton").objectReferenceValue = exitBtn;
        tutSO.FindProperty("titleText").objectReferenceValue = titleTmp;
        tutSO.FindProperty("bodyText").objectReferenceValue = bodyTmp;
        tutSO.FindProperty("pageIndicator").objectReferenceValue = pageTmp;
        tutSO.FindProperty("illustrationImage").objectReferenceValue = illustImage;
        tutSO.ApplyModifiedProperties();

        // EventSystem
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TutorialScene.unity");
        Debug.Log("[TutorialSceneBuilder] Tutorial scene created successfully.");
    }

    static Button CreateTutorialButton(string name, string label, Transform parent)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        var btn = btnObj.AddComponent<Button>();

        var colors = btn.colors;
        colors.normalColor = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        colors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        colors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
        colors.selectedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        btn.colors = colors;

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.9f, 0.8f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return btn;
    }
}
#endif
