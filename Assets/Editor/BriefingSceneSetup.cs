using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;

public static class BriefingSceneSetup
{
    [MenuItem("Tools/Build Briefing Scene")]
    public static void BuildScene()
    {
        // 1. 디렉토리 확인 및 씬 생성
        string scenePath = "Assets/Scenes/Briefing.unity";
        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
        }

        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 폰트 에셋 로드
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NanumMyeongjo SDF.asset");
        if (fontAsset == null)
        {
            Debug.LogWarning("[BriefingSceneSetup] Assets/Fonts/NanumMyeongjo SDF.asset 폰트를 찾을 수 없습니다!");
        }

        // 2. Main Camera 생성
        var camObj = new GameObject("Main Camera");
        var camera = camObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f); // 어두운 배경 테마
        camObj.AddComponent<AudioListener>();

        // 3. EventSystem 생성
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // 4. Canvas 생성
        var canvasObj = new GameObject("Canvas");
        canvasObj.layer = 5; // UI layer
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();
        var canvasGroup = canvasObj.AddComponent<CanvasGroup>();

        // 5. BriefingController 생성 및 Canvas에 바인딩
        var controllerObj = new GameObject("BriefingController");
        var controller = controllerObj.AddComponent<BriefingController>();
        controller.myGroup = canvasGroup;

        // 6. UI 배치 (배경 패널)
        var bgObj = new GameObject("BriefingBackground");
        bgObj.layer = 5;
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.92f); // 짙은 네이비 반투명
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.15f, 0.1f);
        bgRt.anchorMax = new Vector2(0.85f, 0.9f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        controller.briefingBackground = bgRt;

        // 7. Title 텍스트 생성 (TMP)
        var titleObj = new GameObject("BriefingTitleText");
        titleObj.layer = 5;
        titleObj.transform.SetParent(bgObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 42;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "사건 브리핑";
        titleText.color = Color.white;
        if (fontAsset != null) titleText.font = fontAsset;
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.05f, 0.8f);
        titleRt.anchorMax = new Vector2(0.95f, 0.95f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        controller.briefingTitleText = titleText;

        // 8. ScenarioInfo 텍스트 생성 (TMP)
        var infoObj = new GameObject("ScenarioInfoText");
        infoObj.layer = 5;
        infoObj.transform.SetParent(bgObj.transform, false);
        var infoText = infoObj.AddComponent<TextMeshProUGUI>();
        infoText.fontSize = 24;
        infoText.alignment = TextAlignmentOptions.TopLeft;
        infoText.text = "사건 요약 정보 로딩 중...";
        infoText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        if (fontAsset != null) infoText.font = fontAsset;
        var infoRt = infoObj.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0.05f, 0.15f);
        infoRt.anchorMax = new Vector2(0.48f, 0.75f);
        infoRt.offsetMin = Vector2.zero;
        infoRt.offsetMax = Vector2.zero;
        controller.scenarioInfoText = infoText;

        // 9. SuspectInfo 텍스트 생성 (TMP)
        var suspectObj = new GameObject("SuspectInfoText");
        suspectObj.layer = 5;
        suspectObj.transform.SetParent(bgObj.transform, false);
        var suspectText = suspectObj.AddComponent<TextMeshProUGUI>();
        suspectText.fontSize = 24;
        suspectText.alignment = TextAlignmentOptions.TopLeft;
        suspectText.text = "용의자 정보 로딩 중...";
        suspectText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        if (fontAsset != null) suspectText.font = fontAsset;
        var suspectRt = suspectObj.GetComponent<RectTransform>();
        suspectRt.anchorMin = new Vector2(0.52f, 0.15f);
        suspectRt.anchorMax = new Vector2(0.95f, 0.75f);
        suspectRt.offsetMin = Vector2.zero;
        suspectRt.offsetMax = Vector2.zero;
        controller.suspectInfoText = suspectText;

        // 10. Start (Skip) 버튼 생성
        var btnObj = new GameObject("StartBtn");
        btnObj.layer = 5;
        btnObj.transform.SetParent(bgObj.transform, false);
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.35f, 1f);
        var btn = btnObj.AddComponent<Button>();
        var btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.4f, 0.03f);
        btnRt.anchorMax = new Vector2(0.6f, 0.1f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;
        controller.startBtn = btn;

        // 버튼 하위 텍스트 생성
        var btnTxtObj = new GameObject("Text");
        btnTxtObj.layer = 5;
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        var btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
        btnTxt.fontSize = 20;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.text = "스킵 (바로 시작)";
        btnTxt.color = Color.white;
        if (fontAsset != null) btnTxt.font = fontAsset;
        var btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero;
        btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.offsetMin = Vector2.zero;
        btnTxtRt.offsetMax = Vector2.zero;

        // 버튼 트랜지션 타겟 설정
        btn.targetGraphic = btnImage;

        // 11. 씬 저장
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"[BriefingSceneSetup] 씬 생성 및 저장 완료: {scenePath}");

        // 12. Build Settings에 추가
        var buildScenes = EditorBuildSettings.scenes.ToList();
        if (!buildScenes.Any(s => s.path == scenePath))
        {
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[BriefingSceneSetup] Build Settings에 씬 등록 완료: {scenePath}");
        }
    }
}
