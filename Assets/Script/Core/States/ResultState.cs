using UnityEngine;

public class ResultState : IGameState
{
    public TransitionType Transition => TransitionType.JustFloatUI;

    public void Enter()
    {
        // 결과 화면 켜기
        GlobalEventManager.Publish(GameEventType.ShowResultUI);
        Debug.Log("[Result] 결과 화면 진입");
    }

    public void Execute() { }

    public void Exit()
    {
        GlobalEventManager.Publish(GameEventType.HideResultUI);
    }
}