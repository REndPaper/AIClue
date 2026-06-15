using UnityEngine;



using TMPro;



using UnityEngine.UI;



using System.Text;



using System.Collections;







public class BriefingController : MonoBehaviour



{



    [Header("Canvas Group")]



    public CanvasGroup myGroup;



    [Header("롤아웃 연출 배경")]

    public RectTransform briefingBackground;

    private Coroutine _briefingCoroutine;

    private bool _isTransitionStarted = false;



    [Header("UI Texts")]

    public TextMeshProUGUI briefingTitleText;

    public TextMeshProUGUI scenarioInfoText;

    public TextMeshProUGUI suspectInfoText;



    [Header("Buttons")]

    public Button startBtn;



    private void Awake()



    {



        if (startBtn != null)



        {



            startBtn.onClick.AddListener(StartInvestigation);



        }



    }



    private void Start()
    {
        OnShowUI(null);
    }







    private void OnEnable()

    {

        GlobalEventManager.Subscribe(GameEventType.ShowBriefingUI, OnShowUI);

        GlobalEventManager.Subscribe(GameEventType.HideBriefingUI, OnHideUI);

    }



    private void OnDisable()

    {

        GlobalEventManager.Unsubscribe(GameEventType.ShowBriefingUI, OnShowUI);

        GlobalEventManager.Unsubscribe(GameEventType.HideBriefingUI, OnHideUI);

    }



    private void OnShowUI(object data)

    {

        _isTransitionStarted = false;

        if (_briefingCoroutine != null) StopCoroutine(_briefingCoroutine);

        _briefingCoroutine = StartCoroutine(AnimateBriefingSequence());

    }



    private void OnHideUI(object data)

    {

        if (_briefingCoroutine != null) StopCoroutine(_briefingCoroutine);

        SetCanvasGroup(false);

    }



    private void SetupBriefingData()

    {

        if (ScenarioManager.Instance == null || ScenarioManager.Instance.currentScenario == null)

        {

            Debug.LogWarning("[BriefingController] 시나리오 데이터를 가져올 수 없습니다.");

            return;

        }



        var scenario = ScenarioManager.Instance.currentScenario;



        // 1. 브리핑 타이틀 및 주인공 신분/평가 안내

        if (briefingTitleText != null)

        {

            briefingTitleText.text = "<color=#FFFF00><b>경찰대학교 실기 평가 브리핑</b></color>\n" +

                                     "수습 프로파일러 자네의 모의 사건 수사 평가가 지금 시작되네.\n" +

                                     "단서를 찾고 용의자들을 신문하여 진실을 밝혀내게나.";

        }



        // 2. 시나리오 정보

        if (scenarioInfoText != null)

        {

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"<b>사건 번호:</b> {scenario.caseNo.Replace("_", " ")}");

            sb.AppendLine($"<b>사건명:</b> {scenario.caseName}");

            sb.AppendLine($"<b>사건 현장:</b> {scenario.place}");

            sb.AppendLine($"<b>피해자:</b> {scenario.victim}");

            sb.AppendLine($"<b>사건 개요:</b> {scenario.overview}");

            scenarioInfoText.text = sb.ToString();

        }



        // 3. 용의자 3명 정보

        if (suspectInfoText != null)

        {

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<b>배정된 용의자 3명 정보:</b>");

            int num = 1;

            if (ScenarioManager.Instance.activeSuspects != null)

            {

                foreach (var suspect in ScenarioManager.Instance.activeSuspects)

                {

                    if (suspect != null)

                    {

                        sb.AppendLine($"  {num++}. <color=#FFCC00><b>{suspect.name}</b></color> (성격: {suspect.personality})");

                        sb.AppendLine($"     - {suspect.description}");

                    }

                }

            }

            suspectInfoText.text = sb.ToString();

        }

    }



    private void StartInvestigation()

    {

        if (_isTransitionStarted) return;

        _isTransitionStarted = true;

        CoreSystemManager.Instance.ChangeState(GameState.MainPlay);

    }



    private void SetCanvasGroup(bool isOpen)

    {

        if (myGroup == null) return;

        myGroup.alpha = isOpen ? 1f : 0f;

        myGroup.interactable = isOpen;

        myGroup.blocksRaycasts = isOpen;

    }



    private IEnumerator AnimateBriefingSequence()

    {

        // 1. 초기 상태: 알파 0, 가로 스케일 0.02, 텍스트 투명

        SetCanvasGroup(true);



        RectTransform rt = briefingBackground != null ? briefingBackground : myGroup.GetComponent<RectTransform>();

        if (rt != null)

        {

            rt.localScale = new Vector3(0.02f, 1f, 1f);

        }



        if (myGroup != null) myGroup.alpha = 0f;



        if (briefingTitleText != null) briefingTitleText.color = SetAlpha(briefingTitleText.color, 0f);

        if (scenarioInfoText != null) scenarioInfoText.color = SetAlpha(scenarioInfoText.color, 0f);

        if (suspectInfoText != null) suspectInfoText.color = SetAlpha(suspectInfoText.color, 0f);



        // 데이터 채워넣기

        SetupBriefingData();



        // 2. 배경 가로로 펄럭이며 열리는 연출 (Scale X: 0.02 -> 1.0)

        float duration = 0.42f;

        float elapsed = 0f;

        while (elapsed < duration)

        {

            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            t = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease-Out



            if (rt != null) rt.localScale = new Vector3(Mathf.Lerp(0.02f, 1f, t), 1f, 1f);

            if (myGroup != null) myGroup.alpha = t;

            yield return null;

        }



        if (rt != null) rt.localScale = Vector3.one;

        if (myGroup != null) myGroup.alpha = 1f;



        yield return new WaitForSeconds(0.1f);



        // 3. 텍스트 순차 페이드인 (0.12초 딜레이 오프셋)

        if (briefingTitleText != null)

        {

            yield return StartCoroutine(FadeTextIn(briefingTitleText, 0.32f));

            yield return new WaitForSeconds(0.1f);

        }



        if (scenarioInfoText != null)

        {

            yield return StartCoroutine(FadeTextIn(scenarioInfoText, 0.38f));

            yield return new WaitForSeconds(0.1f);

        }



        if (suspectInfoText != null)

        {

            yield return StartCoroutine(FadeTextIn(suspectInfoText, 0.38f));

        }



        yield return new WaitForSeconds(2.5f);

        Debug.Log("[BriefingController] 브리핑 연출이 끝났습니다. 자동으로 수사를 개시합니다.");

        StartInvestigation();



        _briefingCoroutine = null;
    }


    private Color SetAlpha(Color color, float alpha)



    {



        color.a = alpha;



        return color;



    }







    private IEnumerator FadeTextIn(TextMeshProUGUI text, float fadeDuration)



    {



        Color startColor = text.color;



        startColor.a = 0f;



        text.color = startColor;







        float elapsed = 0f;



        while (elapsed < fadeDuration)



        {



            elapsed += Time.deltaTime;



            float t = elapsed / fadeDuration;



            startColor.a = t;



            text.color = startColor;



            yield return null;



        }



        startColor.a = 1f;



        text.color = startColor;



    }



}



