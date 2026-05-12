using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MainUIController : MonoBehaviour
{
    public enum InvestigationMode { Interrogation, Exploration }

    [Header("State 전환 (CanvasGroup)")]
    public CanvasGroup mainStateGroup; // 이 스크립트가 달린 전체 UI 그룹

    [Header("Investigation Mode (카메라 및 모드)")]
    public InvestigationMode currentMode = InvestigationMode.Interrogation;
    public Transform camPosInterrogation;
    public Transform camPosExploration;
    public float cameraMoveSpeed = 4f;

    [Header("HUD Panels (CanvasGroup)")]
    public CanvasGroup chatPanelGroup;      // 좌측 대화 로그 패널
    public CanvasGroup clueInventoryGroup; // 단서 인벤토리 패널
    public CanvasGroup interrogationInputGroup; // 하단 입력창 (심문 모드 전용)

    [Header("심문 UI 요소")]
    public GameObject speechBubble;         // 용의자 말풍선
    public TextMeshProUGUI speechBubbleText;
    public TextMeshProUGUI chatHistoryText;
    public TextMeshProUGUI modeToggleBtnText;
    public TMP_InputField questionInput;
    public Button sendButton;
    public int maxQuestionLength = 60;
    public float bubbleDuration = 30f;

    [Header("용의자 교체 UI")]
    public Button[] suspectButtons; // 유니티 화면에서 만든 버튼 3개 할당

    [Header("NPC 및 스크롤")]
    public Transform npcPoint;
    public GameObject[] npcPrefabs;
    public ScrollRect historyScrollRect;

    [Header("단서 인벤토리")]
    public List<EvidenceData> acquiredEvidences = new List<EvidenceData>();
    public TextMeshProUGUI clueInventoryText;

    private GameObject currentSpawnedNPC;
    private Coroutine _bubbleTimerCoroutine;
    private Coroutine _cameraMoveCoroutine;
    private Camera _mainCam;

    [Header("수사 지표 데이터 (집계용)")]
    public int totalSearchCount = 0;
    public int totalChatCount = 0;

    private void OnEnable()
    {
        // 1. 기존 이벤트 구독
        GlobalEventManager.Subscribe(GameEventType.AIResponded, OnAIResponded);
        GlobalEventManager.Subscribe(GameEventType.AIError, OnAIError);
        GlobalEventManager.Subscribe(GameEventType.EvidenceFound, OnEvidenceFound);

        // ★ 2. State Machine 전용 이벤트 구독
        GlobalEventManager.Subscribe(GameEventType.ShowMainPlayUI, OnShowUI);
        GlobalEventManager.Subscribe(GameEventType.HideMainPlayUI, OnHideUI);
    }

    private void OnDisable()
    {
        GlobalEventManager.Unsubscribe(GameEventType.AIResponded, OnAIResponded);
        GlobalEventManager.Unsubscribe(GameEventType.AIError, OnAIError);
        GlobalEventManager.Unsubscribe(GameEventType.EvidenceFound, OnEvidenceFound);

        GlobalEventManager.Unsubscribe(GameEventType.ShowMainPlayUI, OnShowUI);
        GlobalEventManager.Unsubscribe(GameEventType.HideMainPlayUI, OnHideUI);
    }

    private void Start()
    {
        _mainCam = Camera.main;

        if (questionInput != null)
        {
            questionInput.characterLimit = maxQuestionLength;
            questionInput.onSubmit.AddListener(delegate { OnClickSendQuestion(); });
        }

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnClickSendQuestion);
        }

        speechBubble.SetActive(false);
        chatHistoryText.text = "<color=#FFFF00>수사를 시작합니다. 단서를 바탕으로 심문하십시오.</color>\n\n";

        UpdateModeUI();
        SpawnNPC(0);
        SetupSuspectButtons();

        if (clueInventoryText != null)
            clueInventoryText.text = "현재 확보된 단서가 없습니다.\n\n";
    }

    // ★ State 방송을 들었을 때 내 UI를 켜고 끄는 로직
    private void OnShowUI(object data) => SetCanvasGroup(mainStateGroup, true);
    private void OnHideUI(object data) => SetCanvasGroup(mainStateGroup, false);

    // ★ [사건 종결] 버튼에 연결할 함수
    public void TransitionToAnswerSubmit()
    {
        // 1. 지금까지 모은 수사 데이터와 대화 로그를 몽땅 AnswerSubmitController로 배달!
        var submitCtrl = FindObjectOfType<AnswerSubmitController>(true);
        if (submitCtrl != null)
        {
            // ★ 수정: chatHistoryText.text (실제 대화 로그) 추가 전달
            submitCtrl.ReceiveInvestigationData(totalSearchCount, totalChatCount, chatHistoryText.text);
        }

        // 2. 매니저에게 스테이트 변경 요청
        CoreSystemManager.Instance.ChangeState(GameState.AnswerSubmit);
    }

    // ---------------- [UI 토글 함수] ----------------
    public void ToggleChatPanel()
    {
        bool isCurrentlyOff = chatPanelGroup.alpha < 0.5f;
        SetCanvasGroup(chatPanelGroup, isCurrentlyOff);
    }

    public void ToggleClueInventory()
    {
        bool isCurrentlyOff = clueInventoryGroup.alpha < 0.5f;
        SetCanvasGroup(clueInventoryGroup, isCurrentlyOff);
    }

    private void SetupSuspectButtons()
    {
        if (suspectButtons == null || suspectButtons.Length == 0) return;
        for (int i = 0; i < suspectButtons.Length; i++)
        {
            int index = i;
            CharacterData data = ScenarioManager.Instance.GetCharacterDataByIndex(index);
            if (data == null) continue;
            TextMeshProUGUI btnText = suspectButtons[index].GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = data.name;

            suspectButtons[index].onClick.RemoveAllListeners();
            suspectButtons[index].onClick.AddListener(() =>
            {
                if (currentMode == InvestigationMode.Interrogation) SpawnNPC(index);
            });
        }
    }

    public void ToggleInvestigationMode()
    {
        currentMode = (currentMode == InvestigationMode.Interrogation) ? InvestigationMode.Exploration : InvestigationMode.Interrogation;
        if (_cameraMoveCoroutine != null) StopCoroutine(_cameraMoveCoroutine);
        Transform target = (currentMode == InvestigationMode.Interrogation) ? camPosInterrogation : camPosExploration;
        _cameraMoveCoroutine = StartCoroutine(MoveCameraSmoothly(target));
        UpdateModeUI();
    }

    private void UpdateModeUI()
    {
        bool isInterrogation = (currentMode == InvestigationMode.Interrogation);
        modeToggleBtnText.text = (currentMode == InvestigationMode.Interrogation) ? "단서 수색" : "용의자 심문";
        SetCanvasGroup(interrogationInputGroup, isInterrogation);
        if (!isInterrogation) speechBubble.SetActive(false);
    }

    private IEnumerator MoveCameraSmoothly(Transform target)
    {
        if (target == null) yield break;
        while (Vector3.Distance(_mainCam.transform.position, target.position) > 0.01f ||
               Quaternion.Angle(_mainCam.transform.rotation, target.rotation) > 0.1f)
        {
            _mainCam.transform.position = Vector3.Lerp(_mainCam.transform.position, target.position, Time.deltaTime * cameraMoveSpeed);
            _mainCam.transform.rotation = Quaternion.Slerp(_mainCam.transform.rotation, target.rotation, Time.deltaTime * cameraMoveSpeed);
            yield return null;
        }
        _mainCam.transform.position = target.position;
        _mainCam.transform.rotation = target.rotation;
    }

    private void SetCanvasGroup(CanvasGroup group, bool isOpen)
    {
        if (group == null) return;
        group.alpha = isOpen ? 1f : 0f;
        group.interactable = isOpen;
        group.blocksRaycasts = isOpen;
    }

    public void OnClickSendQuestion()
    {
        string questionText = questionInput.text.Trim();
        if (string.IsNullOrEmpty(questionText) || currentMode != InvestigationMode.Interrogation) return;

        // ★ 수사 지표 집계: 질문 던질 때마다 카운트 업!
        totalChatCount++;

        if (_bubbleTimerCoroutine != null) StopCoroutine(_bubbleTimerCoroutine);

        AppendChatHistory($"<color=#55AAFF>형사:</color> {questionText}");
        speechBubble.SetActive(true);
        speechBubbleText.text = "...";

        PlayerSpeakData pack = new PlayerSpeakData
        {
            question = questionText,
            evidenceContext = GetAcquiredEvidencesContext()
        };
        GlobalEventManager.Publish(GameEventType.PlayerSpeaks, pack);

        questionInput.text = "";
        questionInput.ActivateInputField();
    }

    private void OnAIResponded(object data)
    {
        string answer = data as string;
        speechBubbleText.text = answer;
        AppendChatHistory($"<color=#FF5555>용의자:</color> {answer}");
        if (_bubbleTimerCoroutine != null) StopCoroutine(_bubbleTimerCoroutine);
        _bubbleTimerCoroutine = StartCoroutine(CloseSpeechBubbleAfterDelay(bubbleDuration));
    }

    private void OnAIError(object data)
    {
        speechBubbleText.text = "<color=red>대답을 거부하고 있습니다. (통신 오류)</color>";
        if (_bubbleTimerCoroutine != null) StopCoroutine(_bubbleTimerCoroutine);
        _bubbleTimerCoroutine = StartCoroutine(CloseSpeechBubbleAfterDelay(5f));
    }

    private IEnumerator CloseSpeechBubbleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        speechBubble.SetActive(false);
        _bubbleTimerCoroutine = null;
    }

    public string GetAcquiredEvidencesContext()
    {
        if (acquiredEvidences.Count == 0) return "현재 확보된 증거가 없습니다.";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("[플레이어가 확보한 단서 목록]");
        foreach (var evi in acquiredEvidences)
            sb.AppendLine($"- {evi.name}: {evi.description}");
        return sb.ToString();
    }

    private void OnEvidenceFound(object data)
    {
        EvidenceData foundEvidence = data as EvidenceData;
        if (foundEvidence == null) return;

        // ★ 수사 지표 집계: 단서를 발견할 때마다 수색 카운트 업!
        totalSearchCount++;

        if (acquiredEvidences.Exists(e => e.id == foundEvidence.id)) return;
        acquiredEvidences.Add(foundEvidence);
        if (acquiredEvidences.Count == 1) clueInventoryText.text = "";
        clueInventoryText.text += $"<color=#FFD700>■ {foundEvidence.name}</color>\n<size=80%>{foundEvidence.description}</size>\n\n";
        AppendChatHistory($"<color=#00FF00>[시스템] 단서 '{foundEvidence.name}' 확보.</color>");
    }

    private void AppendChatHistory(string newText)
    {
        chatHistoryText.text += newText + "\n\n";
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (historyScrollRect != null) historyScrollRect.verticalNormalizedPosition = 0f;
    }

    public void SpawnNPC(int suspectIndex)
    {
        if (currentSpawnedNPC != null) Destroy(currentSpawnedNPC);
        if (suspectIndex < 0 || suspectIndex >= npcPrefabs.Length) return;
        if (npcPoint != null && npcPrefabs[suspectIndex] != null)
            currentSpawnedNPC = Instantiate(npcPrefabs[suspectIndex], npcPoint.position, npcPoint.rotation, npcPoint);

        CharacterData targetData = ScenarioManager.Instance.GetCharacterDataByIndex(suspectIndex);
        GlobalEventManager.Publish(GameEventType.TargetChanged, targetData);
        AppendChatHistory($"<color=#00FF00>--- [{targetData.name}] 심문 시작 ---</color>");
    }
}