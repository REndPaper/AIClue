using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public static class MainUIStyleOptimizer
{
    [MenuItem("Tools/Optimize Main UI Style and Layers")]
    public static void OptimizeUI()
    {
        // 1. 현재 열려있는 씬이 Main 씬인지 확인하고, 아니면 로드
        string scenePath = "Assets/Scenes/Main.unity";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != scenePath)
        {
            activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        // 2. Main Camera 배경색 리뉴얼
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
            }
        }

        // 3. Canvas 탐색 및 2D UI 레이어 재배치
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            Debug.LogError("[MainUIStyleOptimizer] Canvas 오브젝트를 찾을 수 없습니다!");
            return;
        }
        var canvasTrans = canvasObj.transform;

        // 3-1. 메인 플레이 2D 취조실 배경 이미지 생성 및 깔기
        var mainBgTrans = canvasTrans.Find("MainBackground");
        GameObject mainBgObj;
        if (mainBgTrans == null)
        {
            mainBgObj = new GameObject("MainBackground");
            mainBgObj.layer = 5;
            mainBgObj.transform.SetParent(canvasTrans, false);
        }
        else
        {
            mainBgObj = mainBgTrans.gameObject;
        }
        var bgRt = mainBgObj.GetComponent<RectTransform>();
        if (bgRt == null) bgRt = mainBgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var bgImg = mainBgObj.GetComponent<Image>();
        if (bgImg == null) bgImg = mainBgObj.AddComponent<Image>();
        bgImg.sprite = null; 
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/interrogation_room_bg.png");
        if (bgSprite != null)
        {
            bgImg.sprite = bgSprite;
            bgImg.color = new Color(0.2f, 0.2f, 0.22f, 1f); // 어둡고 중후한 취조실 톤
        }

        // 3-2. 용의자 이미지(npcStandingImage) 2D 오브젝트 자동 생성 및 크기 세팅
        var npcImageTrans = canvasTrans.Find("npcStandingImage");
        GameObject npcImageObj;
        if (npcImageTrans == null)
        {
            npcImageObj = new GameObject("npcStandingImage");
            npcImageObj.layer = 5;
            npcImageObj.transform.SetParent(canvasTrans, false);
            npcImageTrans = npcImageObj.transform;
        }
        else
        {
            npcImageObj = npcImageTrans.gameObject;
        }
        var npcRt = npcImageObj.GetComponent<RectTransform>();
        if (npcRt == null) npcRt = npcImageObj.AddComponent<RectTransform>();
        npcRt.anchorMin = new Vector2(0.5f, 0f);
        npcRt.anchorMax = new Vector2(0.5f, 0f);
        npcRt.pivot = new Vector2(0.5f, 0f);
        npcRt.anchoredPosition = new Vector2(0f, 100f); // 조금 더 아래쪽으로 내림
        npcRt.sizeDelta = new Vector2(850f, 1100f);     // 캐릭터 비율 유지 및 크기 대폭 확장!

        var npcImgComp = npcImageObj.GetComponent<Image>();
        if (npcImgComp == null) npcImgComp = npcImageObj.AddComponent<Image>();
        npcImgComp.sprite = null; 
        npcImgComp.preserveAspect = true; // 찌그러짐 방지!
        npcImgComp.raycastTarget = false; // 클릭 방해 금지 (관통 활성화)!

        // 4. 탐색 패널(ExplorationPanel)을 딤 오버레이 겸용으로 재구축
        var expPanelTrans = canvasTrans.Find("ExplorationPanel");
        if (expPanelTrans != null)
        {
            var expRt = expPanelTrans.GetComponent<RectTransform>();
            expRt.anchorMin = Vector2.zero;
            expRt.anchorMax = Vector2.one;
            expRt.offsetMin = Vector2.zero;
            expRt.offsetMax = Vector2.zero;

            var expImage = expPanelTrans.GetComponent<Image>();
            if (expImage == null) expImage = expPanelTrans.gameObject.AddComponent<Image>();
            expImage.sprite = null;
            expImage.color = new Color(0f, 0f, 0f, 0.72f); // 딤 

            var gridMapBlockTrans = expPanelTrans.Find("GridMapBlock");
            GameObject gridMapBlockObj;
            if (gridMapBlockTrans == null)
            {
                gridMapBlockObj = new GameObject("GridMapBlock");
                gridMapBlockObj.layer = 5;
                gridMapBlockObj.transform.SetParent(expPanelTrans, false);
            }
            else
            {
                gridMapBlockObj = gridMapBlockTrans.gameObject;
            }

            var gridRt = gridMapBlockObj.GetComponent<RectTransform>();
            if (gridRt == null) gridRt = gridMapBlockObj.AddComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.5f, 0.5f);
            gridRt.anchorMax = new Vector2(0.5f, 0.5f);
            gridRt.anchoredPosition = new Vector2(0f, 40f);
            gridRt.sizeDelta = new Vector2(650f, 650f);

            var oldGrid = expPanelTrans.GetComponent<GridLayoutGroup>();
            var newGrid = gridMapBlockObj.GetComponent<GridLayoutGroup>();
            if (newGrid == null) newGrid = gridMapBlockObj.AddComponent<GridLayoutGroup>();
            
            if (oldGrid != null)
            {
                newGrid.cellSize = oldGrid.cellSize;
                newGrid.spacing = oldGrid.spacing;
                newGrid.startCorner = oldGrid.startCorner;
                newGrid.startAxis = oldGrid.startAxis;
                newGrid.childAlignment = oldGrid.childAlignment;
                Object.DestroyImmediate(oldGrid);
            }
            else
            {
                newGrid.cellSize = new Vector2(200f, 200f);
                newGrid.spacing = new Vector2(15f, 15f);
                newGrid.childAlignment = TextAnchor.MiddleCenter;
            }

            for (int i = 0; i < 9; i++)
            {
                var roomTrans = expPanelTrans.Find($"Room_{i}");
                if (roomTrans != null)
                {
                    roomTrans.SetParent(gridMapBlockObj.transform, false);
                }
            }
        }

        // 5. 단서 획득 팝업창(EvidencePopupPanel) 레이어 소팅 및 딤 적용
        var popupPanelTrans = canvasTrans.Find("EvidencePopupPanel");
        if (popupPanelTrans != null)
        {
            popupPanelTrans.SetAsLastSibling();

            var popRt = popupPanelTrans.GetComponent<RectTransform>();
            popRt.anchorMin = Vector2.zero;
            popRt.anchorMax = Vector2.one;
            popRt.offsetMin = Vector2.zero;
            popRt.offsetMax = Vector2.zero;

            var popImage = popupPanelTrans.GetComponent<Image>();
            if (popImage == null) popImage = popupPanelTrans.gameObject.AddComponent<Image>();
            popImage.sprite = null;
            popImage.color = new Color(0f, 0f, 0.02f, 0.88f); 

            var popupBoxTrans = popupPanelTrans.Find("PopupBox");
            GameObject popupBoxObj;
            if (popupBoxTrans == null)
            {
                popupBoxObj = new GameObject("PopupBox");
                popupBoxObj.layer = 5;
                popupBoxObj.transform.SetParent(popupPanelTrans, false);
            }
            else
            {
                popupBoxObj = popupBoxTrans.gameObject;
            }

            var boxRt = popupBoxObj.GetComponent<RectTransform>();
            if (boxRt == null) boxRt = popupBoxObj.AddComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(580f, 680f);

            var boxImage = popupBoxObj.GetComponent<Image>();
            if (boxImage == null) boxImage = popupBoxObj.AddComponent<Image>();
            boxImage.sprite = null;
            boxImage.color = new Color(0.08f, 0.1f, 0.16f, 0.96f); 

            var boxOutline = popupBoxObj.GetComponent<Outline>();
            if (boxOutline == null) boxOutline = popupBoxObj.AddComponent<Outline>();
            boxOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.25f); 
            boxOutline.effectDistance = new Vector2(1.5f, 1.5f);

            string[] popupElements = { "EvidenceImage", "EvidenceNameText", "EvidenceDescText", "EvidencePopupCloseButton" };
            foreach (var elem in popupElements)
            {
                var elTrans = popupPanelTrans.Find(elem);
                if (elTrans != null)
                {
                    elTrans.SetParent(popupBoxObj.transform, false);
                }
            }

            var imgTrans = popupBoxObj.transform.Find("EvidenceImage");
            if (imgTrans != null)
            {
                var rt = imgTrans.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.64f);
                rt.anchorMax = new Vector2(0.5f, 0.64f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(256f, 256f);
            }

            var nameTrans = popupBoxObj.transform.Find("EvidenceNameText");
            if (nameTrans != null)
            {
                var rt = nameTrans.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.86f);
                rt.anchorMax = new Vector2(0.5f, 0.86f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(500f, 60f);
                var txt = nameTrans.GetComponent<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.fontSize = 32;
                    txt.color = new Color(1f, 0.88f, 0.3f, 1f); 
                }
            }

            var descTrans = popupBoxObj.transform.Find("EvidenceDescText");
            if (descTrans != null)
            {
                var rt = descTrans.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.32f);
                rt.anchorMax = new Vector2(0.5f, 0.32f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(480f, 150f);
                var txt = descTrans.GetComponent<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.fontSize = 20;
                    txt.color = new Color(0.9f, 0.9f, 0.95f, 1f);
                }
            }

            var closeBtnTrans = popupBoxObj.transform.Find("EvidencePopupCloseButton");
            if (closeBtnTrans != null)
            {
                var rt = closeBtnTrans.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.08f);
                rt.anchorMax = new Vector2(0.5f, 0.08f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(200f, 50f);

                var btnImg = closeBtnTrans.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.sprite = null;
                    btnImg.color = new Color(0.2f, 0.2f, 0.32f, 1f);
                }
                
                var btnTxt = closeBtnTrans.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null)
                {
                    btnTxt.text = "확인";
                    btnTxt.fontSize = 18;
                }
            }
        }

        // 6. 메인 패널들 (ChatPanel, ClueInventoryPanel, Panel) 스타일 글래스모피즘 리뉴얼
        string[] panels = { "ChatPanel", "ClueInventoryPanel", "Panel" };
        foreach (var pName in panels)
        {
            var pTrans = canvasTrans.Find(pName);
            if (pTrans != null)
            {
                var img = pTrans.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = null; // 기존의 거친 붓터치 텍스트 배경 이미지 제거
                    img.color = new Color(0.07f, 0.08f, 0.12f, 0.91f); 
                }

                var outline = pTrans.GetComponent<Outline>();
                if (outline == null) outline = pTrans.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 1f, 1f, 0.09f); 
                outline.effectDistance = new Vector2(1f, 1f);

                // 패널 하위의 Scroll View들에 남아있는 이전 붓터치 에셋 강제 해제
                var scrollViewTrans = pTrans.Find("Scroll View");
                if (scrollViewTrans != null)
                {
                    var scrollImg = scrollViewTrans.GetComponent<Image>();
                    if (scrollImg != null)
                    {
                        scrollImg.sprite = null; 
                        scrollImg.color = new Color(0f, 0f, 0f, 0.2f); 
                    }
                }
            }
        }

        // 6-2. 텍스트 입력창 (Panel/questionInput) 에셋 정리
        var questionInputTrans = canvasTrans.Find("Panel/questionInput");
        if (questionInputTrans != null)
        {
            var inputImg = questionInputTrans.GetComponent<Image>();
            if (inputImg != null)
            {
                inputImg.sprite = null; // 입력칸 붓터치 이미지 제거!
                inputImg.color = new Color(0.04f, 0.05f, 0.07f, 0.9f); // 어두운 입력 배경
            }
            var outline = questionInputTrans.GetComponent<Outline>();
            if (outline == null) outline = questionInputTrans.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.1f);
            outline.effectDistance = new Vector2(1f, 1f);
        }

        // 7. 폰트 에셋 로드
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/NanumMyeongjo SDF.asset");

        // 8. 2D 리스트 레이아웃 빌드 및 템플릿 오브젝트 자동 생성
        RectTransform clueContentRt = null;
        GameObject clueTemplateObj = null;
        RectTransform chatContentRt = null;
        GameObject chatTemplateObj = null;

        // (A) 단서 인벤토리 템플릿 빌드
        var clueContentTrans = canvasTrans.Find("ClueInventoryPanel/Scroll View/Viewport/Content");
        if (clueContentTrans != null)
        {
            clueContentRt = clueContentTrans.GetComponent<RectTransform>();
            
            var oldClueText = clueContentTrans.Find("ClueText");
            if (oldClueText != null) Object.DestroyImmediate(oldClueText.gameObject);

            var layout = clueContentTrans.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = clueContentTrans.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = clueContentTrans.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = clueContentTrans.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var oldTemplate = clueContentTrans.Find("ClueItemTemplate");
            if (oldTemplate != null) Object.DestroyImmediate(oldTemplate.gameObject);

            clueTemplateObj = new GameObject("ClueItemTemplate");
            clueTemplateObj.layer = 5;
            clueTemplateObj.transform.SetParent(clueContentTrans, false);
            clueTemplateObj.SetActive(false);

            var itemRt = clueTemplateObj.AddComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(0f, 110f); 

            var itemLayoutElement = clueTemplateObj.AddComponent<LayoutElement>();
            itemLayoutElement.minHeight = 110f;
            itemLayoutElement.preferredHeight = 110f;

            var itemImg = clueTemplateObj.AddComponent<Image>();
            itemImg.sprite = null;
            itemImg.color = new Color(0.12f, 0.14f, 0.2f, 0.85f);

            var itemOutline = clueTemplateObj.AddComponent<Outline>();
            itemOutline.effectColor = new Color(1f, 1f, 1f, 0.08f);
            itemOutline.effectDistance = new Vector2(1f, 1f);

            // ClueIcon
            var iconObj = new GameObject("ClueIcon");
            iconObj.layer = 5;
            iconObj.transform.SetParent(clueTemplateObj.transform, false);
            var iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(15f, 0f);
            iconRt.sizeDelta = new Vector2(80f, 80f);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = null;

            // ClueNameText
            var nameObj = new GameObject("ClueNameText");
            nameObj.layer = 5;
            nameObj.transform.SetParent(clueTemplateObj.transform, false);
            var nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0f, 1f);
            nameRt.anchoredPosition = new Vector2(110f, -15f);
            nameRt.sizeDelta = new Vector2(-125f, 25f);
            
            var nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = "단서 이름";
            nameTxt.fontSize = 18;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.color = new Color(1f, 0.85f, 0.3f, 1f); 
            if (fontAsset != null) nameTxt.font = fontAsset;

            // ClueDescText
            var descObj = new GameObject("ClueDescText");
            descObj.layer = 5;
            descObj.transform.SetParent(clueTemplateObj.transform, false);
            var descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = Vector2.zero;
            descRt.anchorMax = Vector2.one;
            descRt.pivot = new Vector2(0f, 1f);
            descRt.offsetMin = new Vector2(110f, 15f);
            descRt.offsetMax = new Vector2(-15f, -45f);

            var descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = "단서 설명입니다.";
            descTxt.fontSize = 14;
            descTxt.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            descTxt.enableWordWrapping = true;
            if (fontAsset != null) descTxt.font = fontAsset;
        }

        // (B) 대화 기록 피드 템플릿 빌드
        var chatContentTrans = canvasTrans.Find("ChatPanel/Scroll View/Viewport/Content");
        if (chatContentTrans != null)
        {
            chatContentRt = chatContentTrans.GetComponent<RectTransform>();

            var oldText = chatContentTrans.Find("Text (TMP)");
            if (oldText != null) Object.DestroyImmediate(oldText.gameObject);

            var layout = chatContentTrans.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = chatContentTrans.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = chatContentTrans.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = chatContentTrans.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var oldTemplate = chatContentTrans.Find("DialogueItemTemplate");
            if (oldTemplate != null) Object.DestroyImmediate(oldTemplate.gameObject);

            chatTemplateObj = new GameObject("DialogueItemTemplate");
            chatTemplateObj.layer = 5;
            chatTemplateObj.transform.SetParent(chatContentTrans, false);
            chatTemplateObj.SetActive(false);

            var itemRt = chatTemplateObj.AddComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(0f, 120f); 

            var itemImg = chatTemplateObj.AddComponent<Image>();
            itemImg.sprite = null;
            itemImg.color = new Color(0.1f, 0.11f, 0.16f, 0.75f);

            var itemOutline = chatTemplateObj.AddComponent<Outline>();
            itemOutline.effectColor = new Color(1f, 1f, 1f, 0.05f);
            itemOutline.effectDistance = new Vector2(1f, 1f);

            var itemLayout = chatTemplateObj.AddComponent<VerticalLayoutGroup>();
            itemLayout.padding = new RectOffset(15, 15, 15, 15);
            itemLayout.spacing = 10;
            itemLayout.childControlWidth = true;
            itemLayout.childControlHeight = true;
            itemLayout.childForceExpandWidth = true;
            itemLayout.childForceExpandHeight = false;

            var itemFitter = chatTemplateObj.AddComponent<ContentSizeFitter>();
            itemFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            itemFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // QuestionText
            var qObj = new GameObject("QuestionText");
            qObj.layer = 5;
            qObj.transform.SetParent(chatTemplateObj.transform, false);
            var qTxt = qObj.AddComponent<TextMeshProUGUI>();
            qTxt.text = "Q. 질문 내용";
            qTxt.fontSize = 16;
            qTxt.color = new Color(0.4f, 0.7f, 1f, 1f); 
            qTxt.enableWordWrapping = true;
            if (fontAsset != null) qTxt.font = fontAsset;

            var qLayout = qObj.AddComponent<LayoutElement>();
            qLayout.flexibleHeight = 1f;

            // AnswerText
            var aObj = new GameObject("AnswerText");
            aObj.layer = 5;
            aObj.transform.SetParent(chatTemplateObj.transform, false);
            var aTxt = aObj.AddComponent<TextMeshProUGUI>();
            aTxt.text = "A. 답변 내용";
            aTxt.fontSize = 16;
            aTxt.color = new Color(1f, 0.82f, 0.4f, 1f); 
            aTxt.enableWordWrapping = true;
            if (fontAsset != null) aTxt.font = fontAsset;

            var aLayout = aObj.AddComponent<LayoutElement>();
            aLayout.flexibleHeight = 1f;
        }

        // 8-C. 말풍선(speechBubble)을 별도 BubbleCanvas에서 메인 Canvas 하위로 완전하게 이사 & 탑재!
        GameObject speechBubbleObj = null;
        var bubbleCanvasObj = GameObject.Find("BubbleCanvas");
        if (bubbleCanvasObj != null)
        {
            var oldPanelTrans = bubbleCanvasObj.transform.Find("Panel");
            if (oldPanelTrans != null)
            {
                oldPanelTrans.SetParent(canvasTrans, false);
                oldPanelTrans.name = "speechBubble";
                speechBubbleObj = oldPanelTrans.gameObject;
            }
            Object.DestroyImmediate(bubbleCanvasObj);
        }
        else
        {
            var existingBubble = canvasTrans.Find("speechBubble");
            if (existingBubble != null) speechBubbleObj = existingBubble.gameObject;
        }

        // 이사한 speechBubble의 UI 속성 및 레이아웃 설정
        if (speechBubbleObj != null)
        {
            var bpRt = speechBubbleObj.GetComponent<RectTransform>();
            bpRt.anchorMin = new Vector2(0.5f, 1f); // 화면 상단 중앙 앵커
            bpRt.anchorMax = new Vector2(0.5f, 1f);
            bpRt.pivot = new Vector2(0.5f, 1f);
            bpRt.anchoredPosition = new Vector2(0f, -320f); // 용의자의 얼굴(머리)을 가리지 않도록 Y 위치를 -320f로 대폭 하향!
            bpRt.sizeDelta = new Vector2(750f, 220f);       

            var bpImg = speechBubbleObj.GetComponent<Image>();
            if (bpImg == null) bpImg = speechBubbleObj.AddComponent<Image>();
            bpImg.sprite = null; 
            bpImg.color = new Color(0.06f, 0.08f, 0.12f, 0.58f); // 뒷배경 캐릭터가 은은하게 비치도록 불투명도를 0.58f로 조정 (반투명화)!
            bpImg.raycastTarget = false; 

            var bpOutline = speechBubbleObj.GetComponent<Outline>();
            if (bpOutline == null) bpOutline = speechBubbleObj.AddComponent<Outline>();
            bpOutline.effectColor = new Color(0.82f, 0.67f, 0.23f, 0.45f); // 골드 아웃라인
            bpOutline.effectDistance = new Vector2(1.5f, 1.5f);

            var bubbleTxtTrans = speechBubbleObj.transform.Find("Text (TMP)");
            if (bubbleTxtTrans != null)
            {
                var btRt = bubbleTxtTrans.GetComponent<RectTransform>();
                btRt.anchorMin = Vector2.zero;
                btRt.anchorMax = Vector2.one;
                btRt.pivot = new Vector2(0.5f, 0.5f);
                btRt.offsetMin = new Vector2(25f, 20f);
                btRt.offsetMax = new Vector2(-25f, -20f);

                var txt = bubbleTxtTrans.GetComponent<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.fontSize = 22;
                    txt.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                    if (fontAsset != null) txt.font = fontAsset;
                }
            }
        }

        // 9. MainUIController 오브젝트 탐색 후 에디터 참조 강제 할당
        var mainCtrl = Object.FindObjectOfType<MainUIController>();
        if (mainCtrl != null)
        {
            mainCtrl.clueScrollContent = clueContentRt;
            mainCtrl.clueItemTemplate = clueTemplateObj;
            mainCtrl.dialogueScrollContent = chatContentRt;
            mainCtrl.dialogueItemTemplate = chatTemplateObj;
            
            // 2D 초상화(npcStandingImage) 할당
            if (npcImageTrans != null)
            {
                mainCtrl.npcStandingImage = npcImageTrans.GetComponent<Image>();
            }

            // 이사 완료된 speechBubble 및 speechBubbleText 필드 참조 갱신!
            if (speechBubbleObj != null)
            {
                mainCtrl.speechBubble = speechBubbleObj;
                var txtComp = speechBubbleObj.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
                if (txtComp != null) mainCtrl.speechBubbleText = txtComp;
            }

            EditorUtility.SetDirty(mainCtrl);
            Debug.Log("[MainUIStyleOptimizer] MainUIController 필드 레퍼런스 주입을 마쳤습니다.");
        }
        else
        {
            Debug.LogWarning("[MainUIStyleOptimizer] 씬 내에서 MainUIController 컴포넌트를 찾지 못했습니다.");
        }

        // 10. 버튼 프리미엄 스타일 설정
        Color btnNormal = new Color(0.08f, 0.1f, 0.16f, 0.95f); // 짙은 네이비
        Color btnHover = new Color(0.82f, 0.67f, 0.23f, 1f); // 다크 골드 (#D1AA3A)
        Color btnPressed = new Color(0.6f, 0.48f, 0.15f, 1f);

        string[] allButtons = { 
            "OpenChatButton", 
            "OpenClueButton", 
            "ModeToggleButton", 
            "GoToAwnserButton"
        };
        foreach (var bName in allButtons)
        {
            var bTrans = canvasTrans.Find(bName);
            if (bTrans != null)
            {
                var btn = bTrans.GetComponent<Button>();
                if (btn != null) ConfigureButtonPremiumStyle(btn, btnNormal, btnHover, btnPressed);
            }
        }

        var sendBtnTrans = canvasTrans.Find("Panel/sendButton");
        if (sendBtnTrans != null)
        {
            var btn = sendBtnTrans.GetComponent<Button>();
            if (btn != null) ConfigureButtonPremiumStyle(btn, btnNormal, btnHover, btnPressed);
        }

        var closeBtnTrans2 = canvasTrans.Find("EvidencePopupPanel/PopupBox/EvidencePopupCloseButton");
        if (closeBtnTrans2 != null)
        {
            var btn = closeBtnTrans2.GetComponent<Button>();
            if (btn != null) ConfigureButtonPremiumStyle(btn, btnNormal, btnHover, btnPressed);
        }

        for (int i = 1; i <= 3; i++)
        {
            var suspTrans = canvasTrans.Find($"Panel/suspectButton{i}");
            if (suspTrans != null)
            {
                var btn = suspTrans.GetComponent<Button>();
                if (btn != null) ConfigureButtonPremiumStyle(btn, btnNormal, btnHover, btnPressed);
            }
        }

        // 11. 최종 추리 패널 (AnswerSubmitCanvas) 리뉴얼 및 붓터치 에셋 완전 탈거
        var submitCanvasObj = GameObject.Find("AnswerSubmitCanvas");
        if (submitCanvasObj != null)
        {
            var submitPanelTrans = submitCanvasObj.transform.Find("Panel");
            if (submitPanelTrans != null)
            {
                var panelImg = submitPanelTrans.GetComponent<Image>();
                if (panelImg != null)
                {
                    var deskSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/detective_desk_clues.png");
                    if (deskSprite != null)
                    {
                        panelImg.sprite = deskSprite;
                        panelImg.color = new Color(0.25f, 0.25f, 0.28f, 1f); 
                    }
                    else
                    {
                        panelImg.sprite = null;
                        panelImg.color = new Color(0.05f, 0.06f, 0.08f, 1f);
                    }
                }

                // 범인/흉기 컨테이너 카드들 (Image, Image (1)) 스타일 리뉴얼
                string[] containerNames = { "Image", "Image (1)" };
                float cardY = 120f;
                foreach (var cName in containerNames)
                {
                    var cardTrans = submitPanelTrans.Find(cName);
                    if (cardTrans != null)
                    {
                        var rt = cardTrans.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = new Vector2(0f, cardY);
                        rt.sizeDelta = new Vector2(600f, 100f);
                        cardY -= 160f; 

                        var cardImg = cardTrans.GetComponent<Image>();
                        if (cardImg != null)
                        {
                            cardImg.sprite = null; // 붓터치 이미지 제거!
                            cardImg.color = new Color(0.06f, 0.08f, 0.12f, 0.85f); 
                        }

                        var cardOutline = cardTrans.GetComponent<Outline>();
                        if (cardOutline == null) cardOutline = cardTrans.gameObject.AddComponent<Outline>();
                        cardOutline.effectColor = new Color(1f, 1f, 1f, 0.08f);
                        cardOutline.effectDistance = new Vector2(1f, 1f);

                        // 자식 라벨 텍스트 설정
                        var labelTxt = cardTrans.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
                        if (labelTxt != null)
                        {
                            labelTxt.fontSize = 26;
                            labelTxt.fontStyle = FontStyles.Bold;
                            labelTxt.color = new Color(0.82f, 0.67f, 0.23f, 1f); 
                            if (fontAsset != null) labelTxt.font = fontAsset;

                            var lRt = labelTxt.GetComponent<RectTransform>();
                            lRt.anchorMin = new Vector2(0f, 0.5f);
                            lRt.anchorMax = new Vector2(0f, 0.5f);
                            lRt.pivot = new Vector2(0f, 0.5f);
                            lRt.anchoredPosition = new Vector2(40f, 0f); 
                            lRt.sizeDelta = new Vector2(120f, 50f);
                        }

                        // 자식 드롭다운 설정
                        var dropdownTrans = cardTrans.Find("Dropdown");
                        if (dropdownTrans != null)
                        {
                            var dRt = dropdownTrans.GetComponent<RectTransform>();
                            dRt.anchorMin = new Vector2(1f, 0.5f);
                            dRt.anchorMax = new Vector2(1f, 0.5f);
                            dRt.pivot = new Vector2(1f, 0.5f);
                            dRt.anchoredPosition = new Vector2(-40f, 0f); 
                            dRt.sizeDelta = new Vector2(320f, 48f);

                            var dImg = dropdownTrans.GetComponent<Image>();
                            if (dImg != null)
                            {
                                dImg.sprite = null; // 드롭다운 붓터치 제거!
                                dImg.color = new Color(0.12f, 0.14f, 0.2f, 0.9f);
                            }
                            var dOutline = dropdownTrans.GetComponent<Outline>();
                            if (dOutline == null) dOutline = dropdownTrans.gameObject.AddComponent<Outline>();
                            dOutline.effectColor = new Color(0.82f, 0.67f, 0.23f, 0.25f);
                            dOutline.effectDistance = new Vector2(1f, 1f);

                            var templateTrans = dropdownTrans.Find("Template");
                            if (templateTrans != null)
                            {
                                var tempImg = templateTrans.GetComponent<Image>();
                                if (tempImg != null)
                                {
                                    tempImg.sprite = null;
                                    tempImg.color = new Color(0.08f, 0.09f, 0.14f, 0.95f);
                                }
                            }
                        }
                    }
                }

                // 타이틀 텍스트 설정 (최종 추리)
                var titleTxt = submitPanelTrans.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
                if (titleTxt != null)
                {
                    titleTxt.text = "최 종 추 리";
                    titleTxt.fontSize = 42;
                    titleTxt.fontStyle = FontStyles.Bold;
                    titleTxt.color = new Color(1f, 0.88f, 0.3f, 1f); 
                    if (fontAsset != null) titleTxt.font = fontAsset;

                    var tRt = titleTxt.GetComponent<RectTransform>();
                    tRt.anchorMin = new Vector2(0.5f, 1f);
                    tRt.anchorMax = new Vector2(0.5f, 1f);
                    tRt.pivot = new Vector2(0.5f, 1f);
                    tRt.anchoredPosition = new Vector2(0f, -60f); 
                    tRt.sizeDelta = new Vector2(500f, 60f);
                }

                // 이전 화면 버튼 (beforeButton) 설정
                var beforeBtnTrans = submitPanelTrans.Find("beforeButton");
                if (beforeBtnTrans != null)
                {
                    var btn = beforeBtnTrans.GetComponent<Button>();
                    if (btn != null) ConfigureButtonPremiumStyle(btn, btnNormal, btnHover, btnPressed);

                    var bRt = beforeBtnTrans.GetComponent<RectTransform>();
                    bRt.anchorMin = new Vector2(0.5f, 0.5f);
                    bRt.anchorMax = new Vector2(0.5f, 0.5f);
                    bRt.pivot = new Vector2(0.5f, 0.5f);
                    bRt.anchoredPosition = new Vector2(-160f, -220f); 
                    bRt.sizeDelta = new Vector2(200f, 55f);

                    var btnTxt = beforeBtnTrans.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnTxt != null)
                    {
                        btnTxt.text = "이전 화면";
                        btnTxt.fontSize = 18;
                    }
                }

                // 사건 종결 버튼 (EndingButton) 설정
                var endingBtnTrans = submitPanelTrans.Find("EndingButton");
                if (endingBtnTrans != null)
                {
                    var btn = endingBtnTrans.GetComponent<Button>();
                    if (btn != null) ConfigureButtonPremiumStyle(btn, btnNormal, btnHover, btnPressed);

                    var eRt = endingBtnTrans.GetComponent<RectTransform>();
                    eRt.anchorMin = new Vector2(0.5f, 0.5f);
                    eRt.anchorMax = new Vector2(0.5f, 0.5f);
                    eRt.pivot = new Vector2(0.5f, 0.5f);
                    eRt.anchoredPosition = new Vector2(160f, -220f); 
                    eRt.sizeDelta = new Vector2(200f, 55f);

                    var btnTxt = endingBtnTrans.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnTxt != null)
                    {
                        btnTxt.text = "사건 종결";
                        btnTxt.fontSize = 18;
                    }
                }
            }
        }

        // 12. NanumMyeongjo SDF 폰트 일괄 매핑 재차 진행
        if (fontAsset != null)
        {
            var tmpComponents = Object.FindObjectsOfType<TextMeshProUGUI>(true);
            int count = 0;
            foreach (var tmp in tmpComponents)
            {
                tmp.font = fontAsset;
                EditorUtility.SetDirty(tmp);
                count++;
            }
            Debug.Log($"[MainUIStyleOptimizer] 총 {count}개의 TextMeshProUGUI 컴포넌트 폰트를 나눔명조 SDF로 강제 세팅했습니다.");
        }

        // Hierarchy 레이어 순서 정리 (중요!)
        if (mainBgObj != null) mainBgObj.transform.SetSiblingIndex(0); 
        if (npcImageTrans != null) npcImageTrans.SetSiblingIndex(1); 

        var panelObjTrans = canvasTrans.Find("Panel");
        if (panelObjTrans != null) panelObjTrans.SetSiblingIndex(2); 

        if (speechBubbleObj != null) speechBubbleObj.transform.SetSiblingIndex(3); 

        if (expPanelTrans != null) expPanelTrans.SetSiblingIndex(4); 

        var chatPanelTrans = canvasTrans.Find("ChatPanel");
        if (chatPanelTrans != null) chatPanelTrans.SetSiblingIndex(5); 

        var cluePanelTrans = canvasTrans.Find("ClueInventoryPanel");
        if (cluePanelTrans != null) cluePanelTrans.SetSiblingIndex(6); 

        // 모드 전환 버튼 등 컨트롤 버튼 배치
        string[] buttons = { "OpenChatButton", "OpenClueButton", "ModeToggleButton", "GoToAwnserButton" };
        int buttonIndex = 7;
        foreach (var bName in buttons)
        {
            var bTrans = canvasTrans.Find(bName);
            if (bTrans != null) bTrans.SetSiblingIndex(buttonIndex++);
        }

        if (popupPanelTrans != null) popupPanelTrans.SetAsLastSibling(); 

        // 13. 씬 세이브
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[MainUIStyleOptimizer] 메인 플레이 UI 스타일 및 계층 소팅 보정 완료!");
    }

    private static void ConfigureButtonPremiumStyle(Button btn, Color normalColor, Color hoverColor, Color pressedColor)
    {
        if (btn == null) return;

        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = null; 
        }

        btn.transition = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = hoverColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;

        // 미세한 테두리선(Outline) 추가
        var outline = btn.GetComponent<Outline>();
        if (outline == null) outline = btn.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.67f, 0.23f, 0.35f); 
        outline.effectDistance = new Vector2(1f, 1f);

        // 텍스트 색상 및 폰트 세팅
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.color = new Color(0.95f, 0.95f, 1f, 1f);
            txt.fontStyle = FontStyles.Bold;
        }
    }
}
