using UnityEngine;

public class ScenarioSelectState : IGameState
{
    // 씬 전환 없이 UI만 갈아끼웁니다.
    public TransitionType Transition => TransitionType.JustFloatUI;

    public void Enter()
    {
        // 메인 메뉴 UI를 끄고 시나리오 선택 UI를 켜는 이벤트를 날립니다.
        GlobalEventManager.Publish(GameEventType.HideMainMenuUI);
        GlobalEventManager.Publish(GameEventType.ShowScenarioSelectUI);
        Debug.Log("[ScenarioSelect] 시나리오 선택 화면 진입");
    }

    public void Execute() { }

    public void Exit()
    {
        // 시나리오 선택 UI를 끕니다.
        GlobalEventManager.Publish(GameEventType.HideScenarioSelectUI);
    }
}