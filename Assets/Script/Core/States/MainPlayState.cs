using UnityEngine;
using UnityEngine.SceneManagement;

public class MainPlayState : IGameState, ISceneChangeState
{
    // 1. 이 상태는 씬 전환이 필요한 상태입니다.
    public TransitionType Transition => TransitionType.SceneChange;

    // 2. 이동할 타겟 씬 이름 (유니티 빌드 세팅의 씬 이름과 일치해야 함)
    public string TargetSceneName => "Main";

    public void Enter()
    {
        Debug.Log("<color=#00FF00>[State] MainPlay 진입: 게임 루프를 시작합니다.</color>");

        // 씬 로드 후 UI 초기화 요청
        // (탐색 뷰와 심문 뷰의 초기 상태를 설정하는 이벤트를 발행합니다)
        GlobalEventManager.Publish(GameEventType.InitMainPlayUI);

        // BGM 변경이나 카메라 초기화 등 '게임 시작' 연출을 여기서 수행
    }

    public void Execute()
    {
        // 실시간 게임 승리/패배 조건 체크나 
        // 하이브리드 카메라 Lerp 로직 보조 등이 필요할 때 사용합니다.
    }

    public void Exit()
    {
        Debug.Log("<color=#FF0000>[State] MainPlay 종료: 메모리를 정리합니다.</color>");

        // 게임 세션 종료 시 필요한 데이터 정리 및 
        // 가비지 컬렉션(GC) 유도 로직
    }
}