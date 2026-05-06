using System;
using System.Collections.Generic;
using UnityEngine;

// 팀장님의 취향을 반영하여 이벤트 이름은 직관적인 스네이크 케이스로 유지했습니다.
public enum GameEventType
{
    SceneLoadStart,
    SceneLoadProgress,
    SceneLoadComplete,

    ClueObtained,       // 데이터: 획득한 단서 객체(ClueData)
    AnswerSubmitted,    // 데이터: 제출된 정답 데이터

    OnAIResponded,     // 데이터: AI가 반환한 텍스트(string)
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