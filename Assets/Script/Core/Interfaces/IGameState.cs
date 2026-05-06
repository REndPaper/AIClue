// 1. 게임의 전체 상태 정의
public enum GameState
{
    Splash,
    MainMenu,
    ScenarioSelect,
    Briefing,
    MainPlay,       // 3D 공간 탐색, 오브젝트 조사, AI 대화
    AnswerSubmit,   // 정답 입력 UI
    Result          // 추리 실패/완료 화면
}

// 2. 상태 전환 방식 정의
public enum TransitionType
{
    SceneChange,    // 물리적인 씬 로드가 필요한 경우 (예: 로비 -> 인게임)
    JustFloatUI     // 씬 전환 없이 UI 캔버스만 교체하는 경우
}

public interface IGameState
{
    // 해당 상태가 어떤 전환 방식을 쓰는지 명시
    TransitionType Transition { get; }

    void Enter();   // 상태에 진입할 때 1회 실행 (UI 켜기, 변수 초기화 등)
    void Execute(); // 상태가 유지되는 동안 매 프레임 실행 (Update 역할)
    void Exit();    // 상태를 빠져나갈 때 1회 실행 (UI 끄기, 메모리 정리 등)
}

public interface ISceneChangeState : IGameState
{
    new TransitionType Transition => TransitionType.SceneChange;
    string TargetSceneName { get; }
}