using System.Collections.Generic;
using UnityEngine;

public class SplashState : IGameState
{
    // 스플래시는 게임 시작 시 이미 띄워져 있는 씬이므로, 씬 전환 없이 UI 캔버스만 띄웁니다.
    public TransitionType Transition => TransitionType.JustFloatUI;

    public async void Enter()
    {
        Debug.Log("[SplashState] 스플래시 상태 진입. 필수 데이터 로딩을 시작합니다.");

        // 1. 실행할 로딩 태스크들을 순서대로 큐에 담습니다.
        var loadingTasks = new List<ILoadingTask>
        {
            // (나중에 필요하다면 ScenarioParsingTask 등을 여기에 추가)
            new LLMLoadingTask() // 가장 무거운 로컬 AI 모델 VRAM 적재
        };

        // 2. 코어 매니저의 모듈형 로딩 실행기 호출 (완료될 때까지 대기)
        await CoreSystemManager.Instance.ProcessLoadingQueueAsync(loadingTasks);

        Debug.Log("[SplashState] 모든 초기 로딩 완료! 메인 메뉴로 상태를 전환합니다.");

        // 3. 로딩이 끝나면 자연스럽게 메인 메뉴 상태로 넘어갑니다.
        CoreSystemManager.Instance.ChangeState(GameState.MainMenu);
    }

    public void Execute()
    {
        // 로딩 중에는 비동기(Task)가 다 알아서 하므로 Update에서 처리할 게 없습니다.
    }

    public void Exit()
    {
        Debug.Log("[SplashState] 스플래시 상태 종료.");
        // (필요하다면 여기서 스플래시 로고 UI를 끄거나 페이드아웃 효과 이벤트를 날림)
    }
}