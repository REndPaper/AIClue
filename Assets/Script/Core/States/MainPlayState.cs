using UnityEngine;

public class MainPlayState : ISceneChangeState
{
    public TransitionType Transition => TransitionType.SceneChange;
    public string TargetSceneName => "Main";

    public void Enter()
    {
        Debug.Log("[MainPlayState] 진입. 수색 및 심문 UI를 활성화합니다.");
        GlobalEventManager.Publish(GameEventType.InitMainPlayUI);
        GlobalEventManager.Publish(GameEventType.ShowMainPlayUI);
    }

    public void Execute()
    {
    }

    public void Exit()
    {
        Debug.Log("[MainPlayState] 퇴장. 수색 및 심문 UI를 비활성화합니다.");
        GlobalEventManager.Publish(GameEventType.HideMainPlayUI);
    }
}