using UnityEngine;

public class AnswerSubmitState : IGameState
{
    public TransitionType Transition => TransitionType.JustFloatUI;

    public void Enter()
    {
        // 1. 메인 플레이 UI 끄기
        GlobalEventManager.Publish(GameEventType.HideMainPlayUI);
        // 2. 최종 추리 UI 켜기
        GlobalEventManager.Publish(GameEventType.ShowAnswerSubmitUI);
        Debug.Log("[AnswerSubmit] 최종 추리 화면 진입");
    }

    public void Execute() { }

    public void Exit()
    {
        GlobalEventManager.Publish(GameEventType.HideAnswerSubmitUI);
    }
}