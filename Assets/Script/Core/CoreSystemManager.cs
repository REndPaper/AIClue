using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CoreSystemManager : MonoBehaviour
{
    public static CoreSystemManager Instance { get; private set; }

    private Dictionary<GameState, IGameState> _stateDictionary;
    private IGameState _currentState;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeStates();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임 시작 시 초기 로딩 큐 실행 (Splash 씬에서 시작됨)
        //_ = ProcessLoadingQueueAsync();
        ChangeState(GameState.Splash);
    }

    private void InitializeStates()
    {
        _stateDictionary = new Dictionary<GameState, IGameState>
        {
            { GameState.Splash, new SplashState() },
            { GameState.MainMenu, new MainMenuState() },
            { GameState.ScenarioSelect, new ScenarioSelectState() },
            { GameState.Briefing, new BriefingState() },
            { GameState.MainPlay, new MainPlayState() },
            { GameState.AnswerSubmit, new AnswerSubmitState() },
            { GameState.Result, new ResultState() }
            // 팀장님이 만드실 상태 객체들을 여기에 매핑합니다.
        };
    }

    // 상태 전환기 (이전 상태 Exit -> 새 상태 Enter)
    public async void ChangeState(GameState newState)
    {
        if (!_stateDictionary.ContainsKey(newState)) return;
        if (_currentState != null && _currentState == _stateDictionary[newState]) return;

        IGameState oldState = _currentState;
        _currentState = _stateDictionary[newState];

        Debug.Log($"[Core] 상태 전환: {newState} (타입: {_currentState.Transition})");

        if (_currentState.Transition == TransitionType.JustFloatUI)
        {
            _currentState.Enter();
        }
        else if (_currentState.Transition == TransitionType.SceneChange)
        {
            string sceneName = (_currentState as ISceneChangeState)?.TargetSceneName;

            // ★ [추가된 핵심 방어 로직] 
            // 가려고 하는 씬이 이미 현재 켜져있는 씬과 같다면? -> 씬 로딩 생략하고 UI만 켬!
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == sceneName)
            {
                Debug.Log($"[Core] 이미 '{sceneName}' 씬에 있습니다. 로딩을 생략하고 UI만 전환합니다.");
                _currentState.Enter();
            }
            else
            {
                // 진짜로 다른 씬으로 넘어갈 때만 로딩 큐 실행
                var sceneTask = new SceneLoadingTask(sceneName);
                await ProcessLoadingQueueAsync(new List<ILoadingTask> { sceneTask });
                _currentState.Enter();
            }
        }

        oldState?.Exit();
    }

    private void Update()
    {
        _currentState?.Execute();
    }

    // 모듈형 로딩 실행기
    public async Task ProcessLoadingQueueAsync(List<ILoadingTask> tasks)
    {
        float totalTasks = tasks.Count;
        float currentCompleted = 0;

        // 1. 글로벌 로딩 오버레이 UI 켜기 요청
        GlobalEventManager.Publish(GameEventType.ShowLoadingScreen);

        // 2. 태스크 큐 순차적 실행
        foreach (var task in tasks)
        {
            // 현재 어떤 작업 중인지 UI로 브로드캐스트
            GlobalEventManager.Publish(GameEventType.LoadingTextChanged, task.TaskName);

            // ⭐️ 실제 작업이 끝날 때까지 스레드를 멈추지 않고(Non-blocking) 대기
            await task.ExecuteAsync();

            currentCompleted++;

            // 진행률 UI 갱신
            float totalProgress = currentCompleted / totalTasks;
            GlobalEventManager.Publish(GameEventType.LoadingProgress, totalProgress);
        }

        // 3. 로딩 완료, 로딩 오버레이 끄기
        GlobalEventManager.Publish(GameEventType.HideLoadingScreen);
    }
}