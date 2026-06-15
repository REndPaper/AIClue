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

    [Header("Investigation Mode (2D 패널 설정)")]
    public InvestigationMode currentMode = InvestigationMode.Interrogation;
    public CanvasGroup interrogationPanelGroup;
    public CanvasGroup explorationPanelGroup;

    [Header("HUD Panels (CanvasGroup)")]
    public CanvasGroup chatPanelGroup;      // 대화 로그 패널
    public CanvasGroup clueInventoryGroup; // 단서 인벤토리 패널
    public CanvasGroup interrogationInputGroup; // 하단 입력기 (심문 모드 용)

    [Header("심문 UI 요소")]
    public GameObject speechBubble;         // 말풍선
    public TextMeshProUGUI speechBubbleText;
    public TextMeshProUGUI modeToggleBtnText;
    public TMP_InputField questionInput;
    public Button sendButton;
    public int maxQuestionLength = 60;
    public float bubbleDuration = 30f;

    [Header("용의자 선택 UI")]
    public Button[] suspectButtons; // 화면 우측의 버튼 3개 할당

    [Header("2D 용의자 이미지 설정")]
    public UnityEngine.UI.Image npcStandingImage;
    public Sprite[] suspectSprites;
    public ScrollRect historyScrollRect;

    [Header("단서 인벤토리")]
    public List<EvidenceData> acquiredEvidences = new List<EvidenceData>();

    [Header("단서 획득 팝업 UI")]
    public GameObject evidencePopupPanel;
    public UnityEngine.UI.Image evidencePopupImage;
    public TextMeshProUGUI evidencePopupNameText;
    public TextMeshProUGUI evidencePopupDescText;
    public Button evidencePopupCloseButton;

    [Header("2D 리스트화 템플릿")]
    public RectTransform clueScrollContent;
    public GameObject clueItemTemplate; // 비활성화 상태의 템플릿

    public RectTransform dialogueScrollContent;
    public GameObject dialogueItemTemplate; // 비활성화 상태의 템플릿

    private string _lastSentQuestion = ""; // AI 답변 완료 시 Q/A 결합을 위한 임시 캐싱
    private string _accumulatedChatHistory = ""; // 최종 채점에 전달할 전체 원본 텍스트 로그

    private int _currentSuspectIndex = 0;
    private Dictionary<int, List<GameObject>> _suspectDialogues = new Dictionary<int, List<GameObject>>();

    private Coroutine _bubbleTimerCoroutine;
    
    private Vector2 _originalStandingPos;
    private bool _hasOriginalPos = false;
    private Coroutine _standingAnimationCoroutine;
    private Coroutine _thinkingCoroutine;
    private Coroutine _typingCoroutine;
    private AudioSource _typingAudioSource;
    private AudioClip _typingTickClip;

    [Header("수사 데이터 집계")]
    public int totalSearchCount = 0;
    public int totalChatCount = 0;

    private void Awake()
    {
        if (npcStandingImage != null && !_hasOriginalPos)
        {
            _originalStandingPos = npcStandingImage.GetComponent<RectTransform>().anchoredPosition;
            _hasOriginalPos = true;
        }

        _typingAudioSource = gameObject.AddComponent<AudioSource>();
        _typingAudioSource.playOnAwake = false;
    }

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
        _accumulatedChatHistory = "<color=#FFFF00>수사를 시작합니다. 단서를 찾고 용의자를 심문하십시오.</color>\n\n";

        UpdateModeUI();
        SpawnNPC(0);
        SetupSuspectButtons();

        if (evidencePopupCloseButton != null)
        {
            evidencePopupCloseButton.onClick.AddListener(() => {
                if (evidencePopupPanel != null) evidencePopupPanel.SetActive(false);
            });
        }
        if (evidencePopupPanel != null)
        {
            evidencePopupPanel.SetActive(false);
        }

        // 템플릿들은 시작 시 비활성화
        if (clueItemTemplate != null) clueItemTemplate.SetActive(false);
        if (dialogueItemTemplate != null) dialogueItemTemplate.SetActive(false);
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
            submitCtrl.ReceiveInvestigationData(totalSearchCount, totalChatCount, _accumulatedChatHistory);
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
        UpdateModeUI();
    }

    private void UpdateModeUI()
    {
        bool isInterrogation = (currentMode == InvestigationMode.Interrogation);
        modeToggleBtnText.text = isInterrogation ? "현장 수색" : "용의자 심문";
        
        SetCanvasGroup(interrogationPanelGroup, isInterrogation);
        SetCanvasGroup(explorationPanelGroup, !isInterrogation);
        SetCanvasGroup(interrogationInputGroup, isInterrogation);
        
        if (!isInterrogation)
        {
            speechBubble.SetActive(false);
            if (npcStandingImage != null) npcStandingImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
        else
        {
            if (npcStandingImage != null) npcStandingImage.color = Color.white;
        }
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
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

        _lastSentQuestion = questionText;
        _accumulatedChatHistory += $"<color=#55AAFF>수사관:</color> {questionText}\n\n";

        speechBubble.SetActive(true);

        if (_thinkingCoroutine != null) StopCoroutine(_thinkingCoroutine);
        _thinkingCoroutine = StartCoroutine(AnimateThinkingBubble());

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
        if (_thinkingCoroutine != null)
        {
            StopCoroutine(_thinkingCoroutine);
            _thinkingCoroutine = null;
        }

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeTextRoutine(answer));

        _accumulatedChatHistory += $"<color=#FF5555>용의자:</color> {answer}\n\n";

        CreateDialogueFeedCard(_lastSentQuestion, answer);
    }

    private void OnAIError(object data)
    {
        if (_thinkingCoroutine != null)
        {
            StopCoroutine(_thinkingCoroutine);
            _thinkingCoroutine = null;
        }

        string errMsg = "<color=red>답변을 거부하고 있습니다. (통신 오류)</color>";
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeTextRoutine(errMsg));

        _accumulatedChatHistory += $"{errMsg}\n\n";

        CreateDialogueFeedCard(_lastSentQuestion, errMsg);
    }

    private void CreateDialogueFeedCard(string question, string answer)
    {
        if (dialogueScrollContent == null || dialogueItemTemplate == null) return;

        var feedObj = Instantiate(dialogueItemTemplate, dialogueScrollContent);
        feedObj.SetActive(true);

        var qText = feedObj.transform.Find("QuestionText")?.GetComponent<TextMeshProUGUI>();
        var aText = feedObj.transform.Find("AnswerText")?.GetComponent<TextMeshProUGUI>();

        if (qText != null) qText.text = $"<color=#55AAFF><b>Q.</b></color> {question}";
        if (aText != null) aText.text = $"<color=#FFCC55><b>A.</b></color> {answer}";

        // 현재 용의자 대화 목록에 추가
        if (!_suspectDialogues.ContainsKey(_currentSuspectIndex))
        {
            _suspectDialogues[_currentSuspectIndex] = new List<GameObject>();
        }
        _suspectDialogues[_currentSuspectIndex].Add(feedObj);

        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator CloseSpeechBubbleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        speechBubble.SetActive(false);
        _bubbleTimerCoroutine = null;
    }

    public string GetAcquiredEvidencesContext()
    {
        if (acquiredEvidences.Count == 0) return "현재 확보한 증거가 없습니다.";
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

        CreateClueItemSlot(foundEvidence);

        _accumulatedChatHistory += $"<color=#00FF00>[시스템] 증거 '{foundEvidence.name}' 확보.</color>\n\n";

        // 단서 획득 팝업 작동
        if (evidencePopupPanel != null)
        {
            if (evidencePopupImage != null)
            {
                Sprite spr = ScenarioManager.Instance.GetEvidenceSprite(foundEvidence.id);
                evidencePopupImage.sprite = spr;
                evidencePopupImage.gameObject.SetActive(spr != null);
            }
            if (evidencePopupNameText != null)
            {
                evidencePopupNameText.text = foundEvidence.name;
            }
            if (evidencePopupDescText != null)
            {
                evidencePopupDescText.text = foundEvidence.description;
            }
            evidencePopupPanel.SetActive(true);
        }
    }

    private void CreateClueItemSlot(EvidenceData evidence)
    {
        if (clueScrollContent == null || clueItemTemplate == null) return;

        var slotObj = Instantiate(clueItemTemplate, clueScrollContent);
        slotObj.SetActive(true);

        var iconImg = slotObj.transform.Find("ClueIcon")?.GetComponent<Image>();
        var nameTxt = slotObj.transform.Find("ClueNameText")?.GetComponent<TextMeshProUGUI>();
        var descTxt = slotObj.transform.Find("ClueDescText")?.GetComponent<TextMeshProUGUI>();

        if (iconImg != null)
        {
            Sprite spr = ScenarioManager.Instance.GetEvidenceSprite(evidence.id);
            iconImg.sprite = spr;
            iconImg.gameObject.SetActive(spr != null);
        }
        if (nameTxt != null) nameTxt.text = evidence.name;
        if (descTxt != null) descTxt.text = evidence.description;
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (historyScrollRect != null) historyScrollRect.verticalNormalizedPosition = 0f;
    }

    public void SpawnNPC(int suspectIndex)
    {
        if (suspectIndex < 0) return;
        
        _currentSuspectIndex = suspectIndex;
        
        CharacterData targetData = ScenarioManager.Instance.GetCharacterDataByIndex(suspectIndex);
        if (targetData == null) return;

        Sprite activeSprite = ScenarioManager.Instance.GetSuspectSprite(targetData.id);
        if (activeSprite == null && suspectIndex < suspectSprites.Length)
        {
            activeSprite = suspectSprites[suspectIndex];
        }

        if (npcStandingImage != null && activeSprite != null)
        {
            npcStandingImage.sprite = activeSprite;
            npcStandingImage.gameObject.SetActive(true);

            if (_standingAnimationCoroutine != null) StopCoroutine(_standingAnimationCoroutine);
            _standingAnimationCoroutine = StartCoroutine(AnimateStandingImage());
        }

        GlobalEventManager.Publish(GameEventType.TargetChanged, targetData);

        string npcNotice = $"--- [{targetData.name}] 심문 시작 ---";
        _accumulatedChatHistory += $"<color=#00FF00>{npcNotice}</color>\n\n";

        // 기존 생성된 대화 카드들 중 현재 용의자의 것만 켜고 나머지는 모두 숨김 처리
        foreach (var kvp in _suspectDialogues)
        {
            bool isCurrent = (kvp.Key == _currentSuspectIndex);
            foreach (var card in kvp.Value)
            {
                if (card != null) card.SetActive(isCurrent);
            }
        }

        // 해당 용의자 대화 목록에 프로필 카드가 없으면 가장 처음 생성해 줌
        if (!_suspectDialogues.ContainsKey(_currentSuspectIndex) || _suspectDialogues[_currentSuspectIndex].Count == 0)
        {
            CreateProfileCard(targetData);
        }

        StartCoroutine(ScrollToBottom());
    }

    private void CreateProfileCard(CharacterData data)
    {
        if (dialogueScrollContent == null || dialogueItemTemplate == null || data == null) return;

        var feedObj = Instantiate(dialogueItemTemplate, dialogueScrollContent);
        feedObj.SetActive(true);
        feedObj.transform.SetAsFirstSibling(); // 스크롤 뷰 Content 내부 최상단에 배치

        var qText = feedObj.transform.Find("QuestionText")?.GetComponent<TextMeshProUGUI>();
        var aText = feedObj.transform.Find("AnswerText")?.GetComponent<TextMeshProUGUI>();

        if (qText != null) 
            qText.text = $"<color=#FFCC55><b>[수사 대상 정보: {data.name}]</b></color>";
        
        if (aText != null) 
            aText.text = $"<b>성격:</b> {data.personality}\n<b>알리바이:</b> {data.alibi}\n<b>설명:</b> {data.description}";

        if (!_suspectDialogues.ContainsKey(_currentSuspectIndex))
        {
            _suspectDialogues[_currentSuspectIndex] = new List<GameObject>();
        }
        _suspectDialogues[_currentSuspectIndex].Insert(0, feedObj);
    }

    private IEnumerator AnimateStandingImage()
    {
        RectTransform rt = npcStandingImage.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 targetPos = _hasOriginalPos ? _originalStandingPos : rt.anchoredPosition;
        Vector2 startPos = targetPos + new Vector2(150f, 0f);

        rt.anchoredPosition = startPos;
        npcStandingImage.color = new Color(1f, 1f, 1f, 0f);

        float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease-Out

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            npcStandingImage.color = new Color(1f, 1f, 1f, t);
            yield return null;
        }

        rt.anchoredPosition = targetPos;
        npcStandingImage.color = Color.white;
        _standingAnimationCoroutine = null;
    }

    private IEnumerator AnimateThinkingBubble()
    {
        int dotCount = 0;
        while (true)
        {
            string dots = new string('.', dotCount + 1);
            speechBubbleText.text = dots;
            dotCount = (dotCount + 1) % 3;
            yield return new WaitForSeconds(0.4f);
        }
    }

    private IEnumerator TypeTextRoutine(string fullText)
    {
        speechBubbleText.text = "";
        
        for (int i = 0; i <= fullText.Length; i++)
        {
            speechBubbleText.text = fullText.Substring(0, i);
            if (i % 2 == 0)
            {
                PlayTypingTick();
            }
            yield return new WaitForSeconds(0.03f);
        }

        _typingCoroutine = null;

        if (_bubbleTimerCoroutine != null) StopCoroutine(_bubbleTimerCoroutine);
        _bubbleTimerCoroutine = StartCoroutine(CloseSpeechBubbleAfterDelay(bubbleDuration));
    }

    private void PlayTypingTick()
    {
        if (_typingAudioSource == null) return;
        if (_typingTickClip == null)
        {
            _typingTickClip = CreateTickAudioClip();
        }
        if (_typingTickClip != null)
        {
            _typingAudioSource.PlayOneShot(_typingTickClip, 0.12f);
        }
    }

    private AudioClip CreateTickAudioClip()
    {
        int sampleRate = 44100;
        float duration = 0.015f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2f * Mathf.PI * 1900f * t) * Mathf.Exp(-t * 220f);
        }

        AudioClip clip = AudioClip.Create("TypingTick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}