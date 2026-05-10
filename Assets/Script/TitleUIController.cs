using UnityEngine;

public class TitleUIController : MonoBehaviour
{
    [Header("UI 패널 할당 (CanvasGroup을 달아주세요)")]
    public CanvasGroup mainMenuGroup;       // 타이틀 화면
    public CanvasGroup scenarioSelectGroup; // 시나리오 선택 화면

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

    private void ShowScenarioSelect(object data = null) => EnablePanel(scenarioSelectGroup);
    private void HideScenarioSelect(object data = null) => DisablePanel(scenarioSelectGroup);

    // ---------------- [CanvasGroup 온오프 헬퍼 함수] ----------------
    private void EnablePanel(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 1f;             // 보이게
        group.interactable = true;    // 상호작용 켜기
        group.blocksRaycasts = true;  // 클릭 차단 벽 세우기
    }

    private void DisablePanel(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;             // 안 보이게 투명하게
        group.interactable = false;   // 상호작용 끄기
        group.blocksRaycasts = false; // 클릭 통과시키기
    }

    // ---------------- [유저가 버튼을 눌렀을 때 발동할 함수들] ----------------
    public void OnClickStartGame()
    {
        Debug.Log("[TitleUI] 유저가 시작 버튼을 눌렀습니다. 상태를 전환합니다!");
        CoreSystemManager.Instance.ChangeState(GameState.ScenarioSelect);
    }

    public void OnClickBackToMain()
    {
        CoreSystemManager.Instance.ChangeState(GameState.MainMenu);
    }
}