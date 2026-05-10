using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AIChat : MonoBehaviour
{
    [Header("UI 연결")]
    public TMP_InputField inputField;
    public Button submitButton;
    public TextMeshProUGUI logText;
    public ScrollRect scrollRect;

    private void Start()
    {
        submitButton.onClick.AddListener(OnSubmit);
        inputField.onSubmit.AddListener(delegate { OnSubmit(); });
    }

    private void OnEnable()
    {
        // 백엔드의 신호들을 구독합니다.
        GlobalEventManager.Subscribe(GameEventType.AIThinkingStart, OnAiThinking);
        GlobalEventManager.Subscribe(GameEventType.AIResponded, OnAiResponded);
        GlobalEventManager.Subscribe(GameEventType.AIError, OnAiError);
    }

    private void OnDisable()
    {
        GlobalEventManager.Unsubscribe(GameEventType.AIThinkingStart, OnAiThinking);
        GlobalEventManager.Unsubscribe(GameEventType.AIResponded, OnAiResponded);
        GlobalEventManager.Unsubscribe(GameEventType.AIError, OnAiError);
    }

    // --- UI -> 백엔드로 전송 ---
    private void OnSubmit()
    {
        string userQuestion = inputField.text.Trim();
        if (string.IsNullOrEmpty(userQuestion)) return;

        // UI에 내가 친 채팅 띄우기
        AppendLog("Player", userQuestion, "#55FF55");
        inputField.text = "";

        // ★ 백엔드(AISystemManager)로 질문을 냅다 던집니다.
        GlobalEventManager.Publish(GameEventType.PlayerSpeaks, userQuestion);
    }

    // --- 백엔드 -> UI 수신부 ---
    private void OnAiThinking(object data)
    {
        // 상태 잠금 (전송 버튼 비활성화)
        inputField.interactable = false;
        submitButton.interactable = false;
        AppendLog("System", "용의자가 생각 중입니다...", "#AAAAAA");
    }

    private void OnAiResponded(object data)
    {
        string response = data as string;
        AppendLog("Suspect", response, "#FF5555");
        UnlockUI();
    }

    private void OnAiError(object data)
    {
        string errorMsg = data as string;
        AppendLog("System", errorMsg, "#FF0000");
        UnlockUI();
    }

    private void UnlockUI()
    {
        inputField.interactable = true;
        submitButton.interactable = true;
        inputField.ActivateInputField();
    }

    private void AppendLog(string speaker, string message, string colorHex)
    {
        logText.text += $"\n<color={colorHex}><b>[{speaker}]</b></color> {message}\n";
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}