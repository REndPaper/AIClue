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
        string professorComment = "채점 시스템에 문제가 발생했네. 다음 기회에 피드백을 주도록 하지.";
        
        if (aiFeedbackHandler != null)
        {
            try
            {
                professorComment = await aiFeedbackHandler.GenerateProfessorFeedback(
                    report.finalGrade,
                    report.isCulpritCorrect,
                    report.isWeaponCorrect,
                    _finalSearchCount,
                    _finalChatCount,
                    _finalChatLog
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnswerSubmitController] 교수 AI 피드백 생성 중 예외 발생: {ex}");
                professorComment = $"채점 시스템 오류 발생: {ex.Message}\n수사는 잘 마쳤으니 결과 리포트를 확인하게.";
            }
        }
        else
        {
            Debug.LogError("[AnswerSubmitController] aiFeedbackHandler가 null입니다. 씬에 AIFeedbackHandler 오브젝트가 있는지 확인해 주세요.");
            professorComment = "채점 교수님이 부재중이시군. 결과 리포트를 확인해보게.";
        }

        // 결과 데이터를 ScenarioManager 캐시에 저장
        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.latestResultData = new ResultData
            {
                report = report,
                selectedSuspect = selectedSuspect,
                selectedWeapon = selectedWeapon,
                finalSearchCount = _finalSearchCount,
                finalChatCount = _finalChatCount,
                finalChatLog = _finalChatLog,
                professorComment = professorComment
            };

            // 플레이 로그 파일 저장
            SavePlayLogToFile(ScenarioManager.Instance.latestResultData);
        }

        if (resultController != null)
        {
            resultController.UpdateProfessorFeedback(professorComment);
        }
    }

    private void SavePlayLogToFile(ResultData data)
    {
        if (data == null) return;

        try
        {
            // 1. persistentDataPath 폴더 내 저장
            string directoryPath = System.IO.Path.Combine(Application.persistentDataPath, "PlayLogs");
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"PlayLog_{timestamp}.txt";
            string filePath = System.IO.Path.Combine(directoryPath, fileName);

            // 로그 내용 서식화
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine("                 GAME PLAY LOG                    ");
            sb.AppendLine("==================================================");
            sb.AppendLine($"[Timestamp]      {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            if (ScenarioManager.Instance != null && ScenarioManager.Instance.currentScenario != null)
            {
                sb.AppendLine($"[Scenario ID]    {ScenarioManager.Instance.currentScenario.caseNo}");
                sb.AppendLine($"[Scenario Title] {ScenarioManager.Instance.currentScenario.caseName}");
            }
            sb.AppendLine($"[Selected Culprit] {data.selectedSuspect?.name} (ID: {data.selectedSuspect?.id}) - Correct: {data.report.isCulpritCorrect}");
            sb.AppendLine($"[Selected Weapon]  {data.selectedWeapon?.name} (ID: {data.selectedWeapon?.id}) - Correct: {data.report.isWeaponCorrect}");
            sb.AppendLine($"[Total Search]   {data.finalSearchCount}");
            sb.AppendLine($"[Total Chat]     {data.finalChatCount}");
            sb.AppendLine($"[Final Grade]    {data.report.finalGrade}");
            sb.AppendLine($"[Total Score]    {data.report.totalScore}");
            sb.AppendLine();
            sb.AppendLine("==================================================");
            sb.AppendLine("               PROFESSOR FEEDBACK                 ");
            sb.AppendLine("==================================================");
            sb.AppendLine(data.professorComment);
            sb.AppendLine();
            sb.AppendLine("==================================================");
            sb.AppendLine("            INTERROGATION DIALOGUE LOG            ");
            sb.AppendLine("==================================================");
            if (!string.IsNullOrEmpty(data.finalChatLog))
            {
                // 리치 텍스트 태그를 제거하여 텍스트 파일 가독성을 높임
                string cleanedLog = System.Text.RegularExpressions.Regex.Replace(data.finalChatLog, "<.*?>", string.Empty);
                sb.AppendLine(cleanedLog);
            }
            else
            {
                sb.AppendLine("(No dialogue log recorded)");
            }
            sb.AppendLine("==================================================");

            // 파일 쓰기
            System.IO.File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);
            Debug.Log($"[AnswerSubmitController] 플레이 로그가 저장되었습니다: {filePath}");

            // 2. 개발자 편의를 위해 유니티 프로젝트 폴더 내 복사본 저장
            string projectLogDir = System.IO.Path.Combine(Application.dataPath, "..", "PlayLogs");
            projectLogDir = System.IO.Path.GetFullPath(projectLogDir);
            if (!System.IO.Directory.Exists(projectLogDir))
            {
                System.IO.Directory.CreateDirectory(projectLogDir);
            }
            string projectFilePath = System.IO.Path.Combine(projectLogDir, fileName);
            System.IO.File.WriteAllText(projectFilePath, sb.ToString(), System.Text.Encoding.UTF8);
            Debug.Log($"[AnswerSubmitController] 프로젝트 폴더 내 플레이 로그 복사본이 저장되었습니다: {projectFilePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AnswerSubmitController] 플레이 로그 저장 실패: {ex.Message}");
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