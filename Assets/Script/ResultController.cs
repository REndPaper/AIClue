using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultController : MonoBehaviour
{
    [Header("내 CanvasGroup")]
    public CanvasGroup myGroup;

    [Header("메인 결과 UI")]
    public TextMeshProUGUI resultTitleText;   // "수사 성공" or "수사 실패"
    public TextMeshProUGUI gradeText;         // "A+", "F" 등
    public TextMeshProUGUI scoreText;         // "85 / 100"

    [Header("상세 스탯 UI (개별 텍스트)")]
    public TextMeshProUGUI culpritResultText; // 범인 지목 결과 (예: 정답 (용의자A))
    public TextMeshProUGUI weaponResultText;  // 흉기 특정 결과 (예: 오답 (권총))
    public TextMeshProUGUI searchCountText;   // 현장 탐색 횟수 (예: 8 회)
    public TextMeshProUGUI chatCountText;     // 심문 대화 횟수 (예: 15 회)

    [Header("교수 AI 피드백 UI")]
    public TextMeshProUGUI professorFeedbackText; // "자네의 보고서는..."

    [Header("버튼")]
    public Button restartButton;

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

    // ★ 개별 UI에 맞게 텍스트를 따로따로 꽂아줍니다!
    public void SetupResult(JudgmentSystem.ReportCard report, CharacterData suspect, WeaponData weapon, int searchCount, int chatCount)
    {
        // 1. 학점 & 점수
        if (gradeText != null) gradeText.text = report.finalGrade;
        if (scoreText != null) scoreText.text = $"{report.totalScore} / 100";

        // 2. 타이틀 (사건 해결 여부)
        bool isSolved = report.isCulpritCorrect && report.isWeaponCorrect;
        if (resultTitleText != null)
            resultTitleText.text = isSolved ? "<color=#00FF00>CASE CLOSED</color>" : "<color=#FF0000>COLD CASE</color>";

        // 3. 상세 스탯 개별 할당
        if (culpritResultText != null)
            culpritResultText.text = report.isCulpritCorrect ? $"<color=#00FF00>정답 ({suspect.name})</color>" : $"<color=#FF0000>오답 ({suspect.name})</color>";

        if (weaponResultText != null)
            weaponResultText.text = report.isWeaponCorrect ? $"<color=#00FF00>정답 ({weapon.name})</color>" : $"<color=#FF0000>오답 ({weapon.name})</color>";

        if (searchCountText != null)
            searchCountText.text = $"{searchCount} 회";

        if (chatCountText != null)
            chatCountText.text = $"{chatCount} 회";

        // 4. 피드백 로딩 대기 텍스트
        if (professorFeedbackText != null)
            professorFeedbackText.text = "<color=#AAAAAA>담당 교수가 수사 기록을 검토 중입니다...</color>";
    }

    // AI 통신이 끝나면 텍스트 업데이트
    public void UpdateProfessorFeedback(string feedback)
    {
        if (professorFeedbackText != null)
            professorFeedbackText.text = feedback;
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