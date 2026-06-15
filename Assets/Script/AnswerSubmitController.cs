using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class AnswerSubmitController : MonoBehaviour
{
    [Header("내 CanvasGroup")]
    public CanvasGroup myGroup;

    [Header("UI 요소")]
    public TMP_Dropdown suspectDropdown;
    public TMP_Dropdown weaponDropdown;
    public Button backButton;
    public Button submitButton;

    [Header("결과 연동 시스템")]
    public JudgmentSystem judgmentSystem;         // 채점 시스템
    public ResultController resultController;     // 결과 화면 컨트롤러

    public AIFeedbackHandler aiFeedbackHandler; // ★ 인스펙터 연결 필수!

    // 수사 데이터 저장용 변수
    private int _finalSearchCount;
    private int _finalChatCount;
    private string _finalChatLog;

    private void Awake()
    {
        // 1. 매니저가 쏘는 이벤트 주파수 맞추기
        GlobalEventManager.Subscribe(GameEventType.ShowAnswerSubmitUI, OnShowUI);
        GlobalEventManager.Subscribe(GameEventType.HideAnswerSubmitUI, OnHideUI);

        // 2. 버튼 클릭 이벤트 연결
        backButton.onClick.AddListener(BackToInvestigation);
        submitButton.onClick.AddListener(GoToResult);
    }

    private void Start()
    {
        // Auto-assign if missing in inspector
        if (judgmentSystem == null)
        {
            judgmentSystem = FindFirstObjectByType<JudgmentSystem>();
            if (judgmentSystem == null) Debug.LogWarning("[AnswerSubmitController] JudgmentSystem is missing in the scene!");
        }
        if (resultController == null)
        {
            resultController = FindFirstObjectByType<ResultController>();
            if (resultController == null) Debug.LogWarning("[AnswerSubmitController] ResultController is missing in the scene!");
        }
        if (aiFeedbackHandler == null)
        {
            aiFeedbackHandler = FindFirstObjectByType<AIFeedbackHandler>();
            if (aiFeedbackHandler == null) Debug.LogWarning("[AnswerSubmitController] AIFeedbackHandler is missing in the scene!");
        }
    }

    private void OnDestroy()
    {
        GlobalEventManager.Unsubscribe(GameEventType.ShowAnswerSubmitUI, OnShowUI);
        GlobalEventManager.Unsubscribe(GameEventType.HideAnswerSubmitUI, OnHideUI);
    }

    private void OnShowUI(object data)
    {
        SetCanvasGroup(true);
        SetupDropdowns();
    }

    private void OnHideUI(object data)
    {
        SetCanvasGroup(false);
    }

    private void SetupDropdowns()
    {
        if (ScenarioManager.Instance == null || ScenarioManager.Instance.currentScenario == null) return;

        // 1. 범인 드롭다운: 현재 활성화된 용의자 3명 (activeSuspects)
        suspectDropdown.ClearOptions();
        List<string> suspectNames = new List<string>();
        foreach (var s in ScenarioManager.Instance.activeSuspects)
        {
            suspectNames.Add(s.name);
        }
        suspectDropdown.AddOptions(suspectNames);

        // 2. 흉기 드롭다운: JSON에 정의된 모든 무기 리스트 (currentScenario.weapons)
        weaponDropdown.ClearOptions();
        List<string> weaponNames = new List<string>();
        // 단서를 찾았는지 여부와 상관없이 JSON에 있는 모든 무기를 다 띄웁니다.
        foreach (var w in ScenarioManager.Instance.currentScenario.weapons)
        {
            weaponNames.Add(w.name);
        }
        weaponDropdown.AddOptions(weaponNames);

        Debug.Log($"[AnswerSubmit] 드롭다운 세팅 완료: 용의자 {suspectNames.Count}명 / 전체 무기 {weaponNames.Count}종");
    }

    // ★ MainUIController에서 넘어올 때 호출되어 데이터를 받습니다.
    public void ReceiveInvestigationData(int searchCount, int chatCount, string chatLog)
    {
        _finalSearchCount = searchCount;
        _finalChatCount = chatCount;
        _finalChatLog = chatLog; // 받은 로그 저장
        Debug.Log($"[AnswerSubmit] 수사 데이터 수신 완료: 탐색 {_finalSearchCount}회, 대화 {_finalChatCount}회, 로그 길이: {_finalChatLog.Length}자");
    }

    private void BackToInvestigation()
    {
        CoreSystemManager.Instance.ChangeState(GameState.MainPlay);
    }

    private async void GoToResult()
    {
        CharacterData selectedSuspect = ScenarioManager.Instance.activeSuspects[suspectDropdown.value];
        WeaponData selectedWeapon = ScenarioManager.Instance.currentScenario.weapons[weaponDropdown.value];

        JudgmentSystem.ReportCard report = judgmentSystem.EvaluateInvestigation(
            selectedSuspect.id, selectedWeapon.id, _finalSearchCount, _finalChatCount
        );

        if (resultController != null)
        {
            resultController.SetupResult(report, selectedSuspect, selectedWeapon, _finalSearchCount, _finalChatCount);
        }
        CoreSystemManager.Instance.ChangeState(GameState.Result);

        // =========================================================
        // ★ 교수 AI에게 찐 평가 요청! (더미 데이터 삭제)
        // =========================================================
        string professorComment = await aiFeedbackHandler.GenerateProfessorFeedback(
            report.finalGrade,
            report.isCulpritCorrect,
            report.isWeaponCorrect,
            _finalSearchCount,
            _finalChatCount,
            _finalChatLog // ★ 드디어 진짜 플레이어의 대화 로그가 들어갑니다!
        );

        if (resultController != null)
        {
            resultController.UpdateProfessorFeedback(professorComment);
        }
    }

    private void SetCanvasGroup(bool isOpen)
    {
        if (myGroup == null) return;
        myGroup.alpha = isOpen ? 1f : 0f;
        myGroup.interactable = isOpen;
        myGroup.blocksRaycasts = isOpen;
    }
}