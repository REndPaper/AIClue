using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ResultController : MonoBehaviour
{
    [Header("CanvasGroup")]
    public CanvasGroup myGroup;

    [Header("결과 헤더 UI")]
    public TextMeshProUGUI resultTitleText;   // "CASE CLOSED" or "COLD CASE"
    public TextMeshProUGUI gradeText;         // "A+", "F" 등
    public TextMeshProUGUI scoreText;         // "85 / 100"

    [Header("결과 상세 UI")]
    public TextMeshProUGUI culpritResultText; // 추리한 범인
    public TextMeshProUGUI weaponResultText;  // 추리한 흉기
    public TextMeshProUGUI searchCountText;   // 단서 탐색 횟수
    public TextMeshProUGUI chatCountText;     // 심문 대화 횟수

    [Header("교수 AI 피드백 UI")]
    public TextMeshProUGUI professorFeedbackText; // "수사 기록을 검토 중..."

    [Header("버튼")]
    public Button restartButton;

    // --- 애니메이션 제어를 위한 내부 카드 오브젝트 레퍼런스 (Auto-binding) ---
    private GameObject culpritCard;
    private GameObject weaponCard;
    private GameObject chatCountCard;
    private GameObject searchCountCard;
    private GameObject resultTitleCard;
    private GameObject scoreCard;
    private GameObject feedbackCard;
    private GameObject gradeStamp;
    private GameObject confirmButton;

    // --- 동적 생성할 검은색 로딩 화면 오브젝트 ---
    private GameObject loadingPanel;
    private TextMeshProUGUI loadingText;

    private Coroutine loadingCoroutine;
    private Coroutine sequenceCoroutine;

    private void Awake()
    {
        GlobalEventManager.Subscribe(GameEventType.ShowResultUI, OnShowUI);
        GlobalEventManager.Subscribe(GameEventType.HideResultUI, OnHideUI);
        restartButton.onClick.AddListener(RestartGame);
    }

    private void OnDestroy()
    {
        GlobalEventManager.Unsubscribe(GameEventType.ShowResultUI, OnShowUI);
        GlobalEventManager.Unsubscribe(GameEventType.HideResultUI, OnHideUI);
    }

    private void OnShowUI(object data) => SetCanvasGroup(true);
    private void OnHideUI(object data) => SetCanvasGroup(false);

    // 텍스트 컴포넌트들을 기반으로 부모 카드 오브젝트들을 자동으로 바인딩합니다.
    private void InitializeCards()
    {
        if (culpritResultText != null) culpritCard = culpritResultText.transform.parent.gameObject;
        if (weaponResultText != null) weaponCard = weaponResultText.transform.parent.gameObject;
        if (chatCountText != null) chatCountCard = chatCountText.transform.parent.gameObject;
        if (searchCountText != null) searchCountCard = searchCountText.transform.parent.gameObject;
        if (resultTitleText != null) resultTitleCard = resultTitleText.transform.parent.gameObject;
        if (scoreText != null) scoreCard = scoreText.transform.parent.gameObject;
        if (professorFeedbackText != null) feedbackCard = professorFeedbackText.transform.parent.gameObject;
        if (gradeText != null) gradeStamp = gradeText.gameObject;
        if (restartButton != null) confirmButton = restartButton.gameObject;
    }

    // 전체화면을 덮는 검은색 로딩 패널을 동적으로 생성합니다.
    private void CreateLoadingPanel()
    {
        if (loadingPanel != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null && myGroup != null)
        {
            canvas = myGroup.GetComponentInParent<Canvas>();
        }
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null) return;

        // 1. 패널 오브젝트 및 레이아웃 설정
        loadingPanel = new GameObject("AIFeedbackLoadingPanel");
        loadingPanel.transform.SetParent(canvas.transform, false);

        // 결과 리포트 종이(Panel)보다 위(앞)에 배치하기 위해 sibling index 조정
        RectTransform reportRt = null;
        if (resultTitleCard != null) reportRt = resultTitleCard.transform.parent as RectTransform;
        if (reportRt != null)
        {
            loadingPanel.transform.SetSiblingIndex(reportRt.transform.GetSiblingIndex() + 1);
        }

        RectTransform loadingRt = loadingPanel.AddComponent<RectTransform>();
        loadingRt.anchorMin = Vector2.zero;
        loadingRt.anchorMax = Vector2.one;
        loadingRt.sizeDelta = Vector2.zero;
        loadingRt.anchoredPosition = Vector2.zero;

        // 95% 불투명한 검은색 배경 설정
        Image bgImage = loadingPanel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.95f);

        // 2. 중앙 로딩 텍스트 설정
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(loadingPanel.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.sizeDelta = new Vector2(1000, 200);
        textRt.anchoredPosition = Vector2.zero;

        loadingText = textObj.AddComponent<TextMeshProUGUI>();
        loadingText.fontSize = 45f;
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingText.color = Color.white;

        // 한글이 깨지지 않도록 기존 텍스트의 폰트를 참조 복사
        if (resultTitleText != null)
        {
            loadingText.font = resultTitleText.font;
        }
    }

    // 결과 UI가 켜질 때 텍스트를 로드하고, 검은 화면으로 전체를 덮어 로딩 대기를 시작합니다.
    public void SetupResult(JudgmentSystem.ReportCard report, CharacterData suspect, WeaponData weapon, int searchCount, int chatCount)
    {
        InitializeCards();
        CreateLoadingPanel();

        // 1. 값 미리 대입
        if (gradeText != null) gradeText.text = report.finalGrade;
        if (scoreText != null) scoreText.text = $"{report.totalScore} / 100";

        bool isSolved = report.isCulpritCorrect && report.isWeaponCorrect;
        if (resultTitleText != null)
            resultTitleText.text = isSolved ? "<color=#00FF00>CASE CLOSED</color>" : "<color=#FF0000>COLD CASE</color>";

        if (culpritResultText != null)
            culpritResultText.text = report.isCulpritCorrect ? $"<color=#00FF00>정답 ({suspect.name})</color>" : $"<color=#FF0000>오답 ({suspect.name})</color>";

        if (weaponResultText != null)
            weaponResultText.text = report.isWeaponCorrect ? $"<color=#00FF00>정답 ({weapon.name})</color>" : $"<color=#FF0000>오답 ({weapon.name})</color>";

        if (searchCountText != null)
            searchCountText.text = $"{searchCount} 회";

        if (chatCountText != null)
            chatCountText.text = $"{chatCount} 회";

        // 2. 초기 리포트 카드 비활성화 (검은 로딩창 걷힌 후 순차 등장을 위함)
        if (culpritCard != null) culpritCard.SetActive(false);
        if (weaponCard != null) weaponCard.SetActive(false);
        if (chatCountCard != null) chatCountCard.SetActive(false);
        if (searchCountCard != null) searchCountCard.SetActive(false);
        if (resultTitleCard != null) resultTitleCard.SetActive(false);
        if (scoreCard != null) scoreCard.SetActive(false);
        if (feedbackCard != null) feedbackCard.SetActive(false);
        if (gradeStamp != null) gradeStamp.SetActive(false);
        if (confirmButton != null) confirmButton.SetActive(false);

        // 3. 검은 로딩 패널을 켜고 알파 및 컬러 원복
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            Image bgImage = loadingPanel.GetComponent<Image>();
            if (bgImage != null) bgImage.color = new Color(0f, 0f, 0f, 0.95f);
            if (loadingText != null) loadingText.color = Color.white;
        }

        // 4. 로딩 텍스트 애니메이션 시작
        if (loadingCoroutine != null) StopCoroutine(loadingCoroutine);
        loadingCoroutine = StartCoroutine(AnimateFeedbackLoading());
    }

    // AI 교수 피드백 수신이 완료되면 호출됩니다.
    public void UpdateProfessorFeedback(string feedback)
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }

        if (professorFeedbackText != null)
            professorFeedbackText.text = feedback;

        // 검은 화면을 걷어낸 뒤 순차 카드 활성화 시퀀스 동작
        if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
        sequenceCoroutine = StartCoroutine(RevealResultSequence());
    }

    // "교수 AI가 수사 기록을 채점 중입니다..." 애니메이션
    private IEnumerator AnimateFeedbackLoading()
    {
        string baseText = "교수 AI가 수사 리포트를 채점 중입니다";
        int dotCount = 0;
        while (true)
        {
            string dots = new string('.', dotCount);
            if (loadingText != null)
            {
                loadingText.text = $"{baseText}{dots}";
            }
            dotCount = (dotCount + 1) % 4;
            yield return new WaitForSeconds(0.5f);
        }
    }

    // 검은 화면 로딩 패널을 부드럽게 페이드 아웃 시킵니다.
    private IEnumerator FadeOutLoadingPanel(float duration)
    {
        if (loadingPanel == null) yield break;
        Image bgImage = loadingPanel.GetComponent<Image>();
        if (bgImage == null) yield break;

        float elapsed = 0f;
        Color startColor = bgImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        Color textStartColor = loadingText != null ? loadingText.color : Color.white;
        Color textTargetColor = new Color(textStartColor.r, textStartColor.g, textStartColor.b, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            bgImage.color = Color.Lerp(startColor, targetColor, t);
            if (loadingText != null)
            {
                loadingText.color = Color.Lerp(textStartColor, textTargetColor, t);
            }
            yield return null;
        }

        loadingPanel.SetActive(false);
    }

    // 로딩 페이드아웃 후 결과 요소들을 순차적으로 보여주는 시퀀스
    private IEnumerator RevealResultSequence()
    {
        // 1. 검은 로딩 패널 페이드아웃 (0.5초)
        yield return StartCoroutine(FadeOutLoadingPanel(0.5f));

        // 2. 기존의 순차 뿅뿅뿅 등장 연출 실행
        yield return StartCoroutine(AnimateResultSequence());
    }

    // 각 결과 요소들이 차례대로 통통 튀며 활성화되는 애니메이션 코루틴
    private IEnumerator AnimateResultSequence()
    {
        float interval = 0.25f;

        // 순차적으로 활성화
        if (resultTitleCard != null) StartCoroutine(PopIn(resultTitleCard, interval * 0));
        if (culpritCard != null) StartCoroutine(PopIn(culpritCard, interval * 1));
        if (weaponCard != null) StartCoroutine(PopIn(weaponCard, interval * 2));
        if (chatCountCard != null) StartCoroutine(PopIn(chatCountCard, interval * 3));
        if (searchCountCard != null) StartCoroutine(PopIn(searchCountCard, interval * 4));
        if (feedbackCard != null) StartCoroutine(PopIn(feedbackCard, interval * 5));
        if (scoreCard != null) StartCoroutine(PopIn(scoreCard, interval * 6));

        // 카드들이 모두 등장할 때까지 대기
        yield return new WaitForSeconds(interval * 6 + 0.35f);

        // 등급 스탬프 쾅! 연출
        if (gradeStamp != null)
        {
            gradeStamp.SetActive(true);
            RectTransform stampRt = gradeStamp.GetComponent<RectTransform>();
            if (stampRt != null)
            {
                stampRt.localScale = Vector3.one * 5.0f;
                float stampDuration = 0.15f;
                float elapsed = 0f;
                while (elapsed < stampDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / stampDuration;
                    stampRt.localScale = Vector3.Lerp(Vector3.one * 5.0f, Vector3.one, t * t);
                    yield return null;
                }
                stampRt.localScale = Vector3.one;
            }

            PlayThudSound();

            RectTransform panelRt = null;
            if (resultTitleCard != null)
            {
                panelRt = resultTitleCard.transform.parent as RectTransform;
            }
            if (panelRt != null)
            {
                StartCoroutine(ShakeUI(panelRt, 0.25f, 15f));
            }
        }

        // 마지막 확인 버튼 등장
        if (confirmButton != null)
        {
            StartCoroutine(PopIn(confirmButton, 0.2f));
        }
    }

    // 뿅 튀어나오는 탄성 애니메이션 (BackOut Easing)
    private IEnumerator PopIn(GameObject obj, float delay)
    {
        if (obj == null) yield break;

        yield return new WaitForSeconds(delay);

        obj.SetActive(true);
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 targetScale = Vector3.one;
        rt.localScale = Vector3.zero;

        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scaleValue;
            if (t < 1f)
            {
                float ts = t - 1f;
                float s = 1.70158f;
                scaleValue = ts * ts * ((s + 1f) * ts + s) + 1f;
            }
            else
            {
                scaleValue = 1f;
            }

            rt.localScale = Vector3.one * scaleValue;
            yield return null;
        }
        rt.localScale = targetScale;
    }

    // 화면 흔들림 효과
    private IEnumerator ShakeUI(RectTransform rectTransform, float duration, float strength)
    {
        Vector2 originalPos = rectTransform.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            float currentStrength = Mathf.Lerp(strength, 0f, percent);
            rectTransform.anchoredPosition = originalPos + new Vector2(
                Random.Range(-1f, 1f) * currentStrength,
                Random.Range(-1f, 1f) * currentStrength
            );
            yield return null;
        }
        rectTransform.anchoredPosition = originalPos;
    }

    // 쿵 소리 오디오 실시간 합성
    private void PlayThudSound()
    {
        int sampleRate = 44100;
        float duration = 0.4f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(80f, 30f, t / duration);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            
            float envelope = Mathf.Exp(-7f * t);
            float noise = (Random.value * 2f - 1f) * 0.12f * Mathf.Exp(-16f * t);

            samples[i] = (sine * 0.85f + noise) * envelope;
        }

        AudioClip thudClip = AudioClip.Create("ThudSynthesized", sampleCount, 1, sampleRate, false);
        thudClip.SetData(samples, 0);

        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = thudClip;
        audioSource.volume = 1.0f;
        audioSource.spatialBlend = 0.0f;
        audioSource.Play();
        Destroy(audioSource, duration + 0.1f);
    }

    private void RestartGame()
    {
        CoreSystemManager.Instance.ChangeState(GameState.MainMenu);
    }

    private void SetCanvasGroup(bool isOpen)
    {
        if (myGroup == null) return;
        myGroup.alpha = isOpen ? 1f : 0f;
        myGroup.interactable = isOpen;
        myGroup.blocksRaycasts = isOpen;
    }
}
