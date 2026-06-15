using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public static class MainPlay2DSetup
{
    [MenuItem("Tools/Convert Main Play to 2D")]
    public static void ConvertTo2D()
    {
        // 1. 현재 열려있는 씬이 Main 씬인지 확인하고, 아니면 로드
        string scenePath = "Assets/Scenes/Main.unity";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != scenePath)
        {
            activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        // 2. 3D 오브젝트 제거
        string[] objsToDelete = { "InterrogationRoom", "Desk", "NPCPoint", "InterrogationTransform", "ExplorationTransform" };
        foreach (string name in objsToDelete)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
                Debug.Log($"[MainPlay2DSetup] 3D 오브젝트 제거 완료: {name}");
            }
        }

        // 3. 카메라 설정 2D 최적화 (단색 어두운 배경)
        var cameraObj = GameObject.Find("Main Camera");
        if (cameraObj != null)
        {
            var camera = cameraObj.GetComponent<Camera>();
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f); // 짙은 네이비
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                Debug.Log("[MainPlay2DSetup] 카메라 설정 2D(Orthographic & 단색) 완료.");
            }
        }

        // 4. Canvas 및 자식 구조 탐색
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            Debug.LogError("[MainPlay2DSetup] 씬 내에서 Canvas 오브젝트를 찾을 수 없습니다!");
            return;
        }

        // 5. ExplorationPanel (3x3 격자 수색 패널) 신규 생성
        string expPanelName = "ExplorationPanel";
        var existingExpPanel = canvasObj.transform.Find(expPanelName);
        if (existingExpPanel != null)
        {
            Object.DestroyImmediate(existingExpPanel.gameObject);
        }

        var expPanelObj = new GameObject(expPanelName);
        expPanelObj.layer = 5; // UI layer
        expPanelObj.transform.SetParent(canvasObj.transform, false);
        var expGroup = expPanelObj.AddComponent<CanvasGroup>();
        var expImage = expPanelObj.AddComponent<Image>();
        expImage.color = new Color(0.08f, 0.08f, 0.12f, 0.95f); // 짙은 그리드 패널 배경

        var expRt = expPanelObj.GetComponent<RectTransform>();
        expRt.anchorMin = new Vector2(0.5f, 0.5f);
        expRt.anchorMax = new Vector2(0.5f, 0.5f);
        expRt.anchoredPosition = new Vector2(0, 50); // 화면 중앙에서 약간 위
        expRt.sizeDelta = new Vector2(650, 650);

        // 3x3 Grid Layout Group 컴포넌트 추가
        var grid = expPanelObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200, 200);
        grid.spacing = new Vector2(15, 15);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;

        // 6. 9개의 방 버튼 생성 및 RoomObject 바인딩
        var expController = Object.FindObjectOfType<ExplorationController>();
        if (expController == null)
        {
            Debug.LogError("[MainPlay2DSetup] 씬 내에서 ExplorationController를 찾을 수 없습니다!");
            return;
        }

        for (int i = 0; i < 9; i++)
        {
            int index = i;
            var roomBtnObj = new GameObject($"Room_{index}");
            roomBtnObj.layer = 5;
            roomBtnObj.transform.SetParent(expPanelObj.transform, false);

            var btnImage = roomBtnObj.AddComponent<Image>();
            btnImage.color = new Color(0.18f, 0.18f, 0.25f, 1f); // 기본 방 색상

            var btn = roomBtnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;

            var roomObj = roomBtnObj.AddComponent<RoomObject>();
            roomObj.roomIndex = index;

            // 텍스트 추가 (TMP)
            var txtObj = new GameObject("Text");
            txtObj.layer = 5;
            txtObj.transform.SetParent(roomBtnObj.transform, false);
            var textComp = txtObj.AddComponent<TextMeshProUGUI>();
            textComp.fontSize = 20;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.text = "?";
            textComp.color = Color.white;

            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            // 버튼 onClick 리스너 바인딩 (ExplorationController.OnClickRoom)
            UnityEditor.Events.UnityEventTools.AddObjectPersistentListener<RoomObject>(btn.onClick, expController.OnClickRoom, roomObj);
        }

        Debug.Log("[MainPlay2DSetup] 3x3 격자 지색용 2D 버튼 생성 및 이벤트 바인딩 완료.");

        // 7. MainUIController 인스펙터 레퍼런스 업데이트
        var mainUI = Object.FindObjectOfType<MainUIController>();
        if (mainUI != null)
        {
            mainUI.mainStateGroup = canvasObj.GetComponent<CanvasGroup>();
            mainUI.explorationPanelGroup = expGroup;

            // interrogationPanelGroup ➔ Canvas/Panel 매핑
            var panelTrans = canvasObj.transform.Find("Panel");
            if (panelTrans != null)
            {
                mainUI.interrogationPanelGroup = panelTrans.GetComponent<CanvasGroup>();
                mainUI.interrogationInputGroup = panelTrans.GetComponent<CanvasGroup>();
            }

            EditorUtility.SetDirty(mainUI);
            Debug.Log("[MainPlay2DSetup] MainUIController 인스펙터 바인딩 업데이트 완료.");
        }

        // 8. NanumMyeongjo SDF 폰트 강제 주입
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NanumMyeongjo SDF.asset");
        if (fontAsset != null)
        {
            // 씬 내의 모든 TMP 컴포넌트들 검색
            var tmpComponents = Object.FindObjectsOfType<TextMeshProUGUI>(true);
            int count = 0;
            foreach (var tmp in tmpComponents)
            {
                tmp.font = fontAsset;
                EditorUtility.SetDirty(tmp);
                count++;
            }
            Debug.Log($"[MainPlay2DSetup] 총 {count}개의 TextMeshProUGUI 컴포넌트에 나눔명조 SDF 폰트를 일괄 바인딩했습니다.");
        }
        else
        {
            Debug.LogWarning("[MainPlay2DSetup] 나눔명조 SDF 폰트 자산을 찾지 못해 폰트 바인딩을 생략합니다.");
        }

        // 9. 씬 세이브
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[MainPlay2DSetup] Main 씬 변환 및 세이브가 완벽하게 완료되었습니다!");
    }
}
