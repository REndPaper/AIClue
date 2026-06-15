using UnityEngine;
using UnityEngine.UI; // 버튼 제어용
using TMPro;

public class TitleUIController : MonoBehaviour
{
    [Header("UI 패널 할당 (CanvasGroup)")]
    public CanvasGroup mainMenuGroup;
    public CanvasGroup scenarioSelectGroup;

    [Header("시나리오 정보 텍스트 (Folder UI)")]
    public TextMeshProUGUI CaseNoTxt;
    public TextMeshProUGUI CaseTxt;
    public TextMeshProUGUI OverviewTxt;
    public TextMeshProUGUI VictimTxt;
    public TextMeshProUGUI PlaceTxt;

    [Header("제어 버튼")]
    public Button prevBtn; // 이전 사건 버튼
    public Button nextBtn; // 다음 사건 버튼

    public Button startBtn;

    private void OnEnable()
    {
        GlobalEventManager.Subscribe(GameEventType.ShowMainMenuUI, ShowMainMenu);
        GlobalEventManager.Subscribe(GameEventType.HideMainMenuUI, HideMainMenu);
        GlobalEventManager.Subscribe(GameEventType.ShowScenarioSelectUI, ShowScenarioSelect);
        GlobalEventManager.Subscribe(GameEventType.HideScenarioSelectUI, HideScenarioSelect);
    }

    private void OnDisable()
    {
        GlobalEventManager.Unsubscribe(GameEventType.ShowMainMenuUI, ShowMainMenu);
        GlobalEventManager.Unsubscribe(GameEventType.HideMainMenuUI, HideMainMenu);
        GlobalEventManager.Unsubscribe(GameEventType.ShowScenarioSelectUI, ShowScenarioSelect);
        GlobalEventManager.Unsubscribe(GameEventType.HideScenarioSelectUI, HideScenarioSelect);
    }

    // ---------------- [이벤트 수신 시 작동할 함수들] ----------------
    private void ShowMainMenu(object data = null) => EnablePanel(mainMenuGroup);
    private void HideMainMenu(object data = null) => DisablePanel(mainMenuGroup);

    private void ShowScenarioSelect(object data = null)
    {
        EnablePanel(scenarioSelectGroup);
        UpdateScenarioUI(); // ★ 화면이 켜질 때 데이터를 새로고침합니다.
    }
    private void HideScenarioSelect(object data = null) => DisablePanel(scenarioSelectGroup);

    // ---------------- [데이터 바인딩: JSON -> UI] ----------------
    private void UpdateScenarioUI()
    {
        // 1. 시나리오 매니저에 이미 로드된 데이터가 있는지 확인
        var data = ScenarioManager.Instance.currentScenario;
        if (data == null || string.IsNullOrEmpty(data.caseNo))
        {
            // 아직 로드 전이라면 기본 파일 하나를 로드함
            ScenarioManager.Instance.SetupGame("Scenario/CASE_001/CASE_001.json");
            data = ScenarioManager.Instance.currentScenario;
        }

        // 2. UI 텍스트에 데이터 꽂아넣기
        CaseNoTxt.text = data.caseNo.Replace("_", " ");
        CaseTxt.text = data.caseName;
        OverviewTxt.text = data.overview;
        VictimTxt.text = data.victim;
        PlaceTxt.text = data.place;

        // 3. 이전/다음 버튼 끄기 & 시작 버튼 활성화 확실히 하기
        if (prevBtn != null) prevBtn.gameObject.SetActive(false);
        if (nextBtn != null) nextBtn.gameObject.SetActive(false);
        if (startBtn != null) startBtn.gameObject.SetActive(true);

        Debug.Log($"[TitleUI] {data.caseNo} 데이터 바인딩 완료!");
    }

    // ---------------- [CanvasGroup 온오프 헬퍼 함수] ----------------
    private void EnablePanel(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void DisablePanel(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    // ---------------- [버튼 클릭 이벤트] ----------------
    public void OnClickStartGame() // 메인메뉴의 '게임 시작' 버튼
    {
        CoreSystemManager.Instance.ChangeState(GameState.ScenarioSelect);
    }

    public void OnClickStartInvestigation() // 시나리오 선택창의 '추리 시작' 버튼
    {
        Debug.Log("[TitleUI] 사건 수사를 시작합니다! 메인 플레이 상태로 전환.");
        // 여기서 실제로 게임 루프(MainPlay)로 넘어갑니다.
        CoreSystemManager.Instance.ChangeState(GameState.Briefing);
    }

    public void OnClickBackToMain()
    {
        CoreSystemManager.Instance.ChangeState(GameState.MainMenu);
    }
}