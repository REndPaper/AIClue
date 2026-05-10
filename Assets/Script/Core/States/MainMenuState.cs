using UnityEngine;

public class MainMenuState : ISceneChangeState
{
    public TransitionType Transition => TransitionType.SceneChange;
    // 이 상태로 넘어오면 코어 매니저가 SceneLoadingTask를 이용해 아래 씬을 로드합니다!
    public string TargetSceneName => "Title";

    public void Enter()
    {
        // 다른 상태(예: 시나리오 선택)에서 '뒤로가기'로 돌아올 때도 UI를 켜줍니다.
        GlobalEventManager.Publish(GameEventType.ShowMainMenuUI);
        Debug.Log("[MainMenu] 메인 메뉴 UI 활성화");
    }

    public void Execute() { }

    public void Exit()
    {
        // 다음 화면으로 넘어갈 때 메인 메뉴 UI를 숨깁니다.
        GlobalEventManager.Publish(GameEventType.HideMainMenuUI);
    }
}