using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어가 말할 때 전달할 데이터 꾸러미
public class PlayerSpeakData
{
    public string question;         // 유저가 친 질문
    public string evidenceContext;  // 현재 보유한 단서 리스트
}

public enum GameEventType
{
    SceneLoadStart,
    SceneLoadProgress,
    SceneLoadComplete,

    ShowMainMenuUI,
    HideMainMenuUI,
    ShowScenarioSelectUI,
    HideScenarioSelectUI,

    InitMainPlayUI,
    ShowMainPlayUI,
    TargetChanged,
    EvidenceFound,
    HideMainPlayUI,

    ShowAnswerSubmitUI,
    HideAnswerSubmitUI,

    ShowResultUI,
    HideResultUI,

    ShowBriefingUI,
    HideBriefingUI,

    ClueObtained,       // 데이터: 획득한 단서 객체(ClueData)
    AnswerSubmitted,    // 데이터: 제출된 정답 데이터

    PlayerSpeaks,       // 플레이어가 채팅을 입력했을 때 (UI -> AI)
    AIThinkingStart,   // AI가 추론을 시작할 때 (AI -> UI, 버튼 잠금용)
    AIResponded,        // AI가 대답을 완료했을 때 (AI -> UI)
    AIError,             // 추론 중 에러 발생 시 (AI -> UI)

    UpdateUIScore,      // 데이터: 현재 점수(int)

    ShowLoadingScreen,
    LoadingTextChanged,
    LoadingProgress,
    HideLoadingScreen
}

public static class GlobalEventManager
{
    // 이벤트를 저장할 딕셔너리. 데이터 전달을 위해 Action<object>를 사용합니다.
    private static readonly Dictionary<GameEventType, Action<object>> eventDictionary = new Dictionary<GameEventType, Action<object>>();

    /// <summary>
    /// 이벤트 구독 (UI 스크립트의 OnEnable에서 주로 호출)
    /// </summary>
    public static void Subscribe(GameEventType eventType, Action<object> listener)
    {
        if (eventDictionary.ContainsKey(eventType))
        {
            eventDictionary[eventType] += listener;
        }
        else
        {
            eventDictionary.Add(eventType, listener);
        }
    }

    /// <summary>
    /// 이벤트 구독 해제 (UI 스크립트의 OnDisable에서 반드시 호출하여 메모리 누수 방지)
    /// </summary>
    public static void Unsubscribe(GameEventType eventType, Action<object> listener)
    {
        if (eventDictionary.ContainsKey(eventType))
        {
            eventDictionary[eventType] -= listener;
        }
    }

    /// <summary>
    /// 이벤트 발행 (로직 처리 후 호출)
    /// </summary>
    public static void Publish(GameEventType eventType, object eventData = null)
    {
        if (eventDictionary.ContainsKey(eventType))
        {
            eventDictionary[eventType]?.Invoke(eventData);
        }
    }
}