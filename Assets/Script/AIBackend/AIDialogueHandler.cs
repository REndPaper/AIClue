using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 인게임 3D 씬에서 NPC와의 심문을 전담하는 핸들러.
/// 9턴 묵비권 제한 및 대화 기록 관리를 수행합니다.
/// </summary>
public class AIDialogueHandler : MonoBehaviour
{
    [Header("현재 심문 상태")]
    private CharacterData _currentNPC;
    private string _dynamicSystemPrompt;
    
    // 대화 메모리 및 턴 제한 (최대 9회)
    private List<string> _chatHistory = new List<string>();
    private int _chatTurnCount = 0;
    private const int MAX_CHAT_TURNS = 9; 

    private void OnEnable()
    {
        // UI 채팅창에서 엔터를 치면 발생하는 이벤트 구독
        GlobalEventManager.Subscribe(GameEventType.PlayerSpeaks, OnPlayerSpeaks);
    }

    private void OnDisable()
    {
        GlobalEventManager.Unsubscribe(GameEventType.PlayerSpeaks, OnPlayerSpeaks);
    }

    /// <summary>
    /// 플레이어가 씬에서 NPC를 클릭했을 때 호출 (InteractionSystemManager가 호출)
    /// </summary>
    public void StartInterrogation(CharacterData npcData)
    {
        _currentNPC = npcData;
        _chatHistory.Clear();
        _chatTurnCount = 0; 

        BuildSystemPrompt();
        Debug.Log($"[AIDialogueHandler] {_currentNPC.name} 심문 시작. 남은 질문 기회: {MAX_CHAT_TURNS}회");
    }

    private void BuildSystemPrompt()
    {
        if (_currentNPC == null) return;

        bool isTrueCulprit = (_currentNPC.id == ScenarioManager.Instance.trueCulprit.id);
        bool isFalseCulprit = (_currentNPC.id == ScenarioManager.Instance.falseCulprit.id);

        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("[Role]");
        sb.AppendLine($"당신의 이름은 '{_currentNPC.name}'입니다.");
        sb.AppendLine($"성격: {_currentNPC.personality}");
        sb.AppendLine($"알리바이: '{_currentNPC.alibi}'");
        sb.AppendLine("당신은 살인 사건의 용의자로서 형사(Player)에게 심문을 받고 있습니다.");

        sb.AppendLine("\n[Secret Instruction]");
        if (isTrueCulprit)
        {
            sb.AppendLine("★ 당신은 진범입니다! 절대 자백하지 마십시오.");
            sb.AppendLine($"현장에 당신의 단서가 남았습니다: [{_currentNPC.breakerEvidence.name} - {_currentNPC.breakerEvidence.description}]");
            sb.AppendLine("단서를 추궁당하면 속으로는 당황하되 겉으론 불쾌해하며, 묘하게 앞뒤가 안 맞는 변명을 대거나 화제를 돌리십시오.");
        }
        else if (isFalseCulprit)
        {
            sb.AppendLine("★ 당신은 억울하게 의심받는 무고한 시민입니다.");
            sb.AppendLine($"하지만 현장에 오해를 살 만한 흔적이 있습니다: [{_currentNPC.breakerEvidence.name} - {_currentNPC.breakerEvidence.description}]");
            sb.AppendLine("단서를 추궁당하면 억울해하며 화를 내고 해명하십시오.");
        }
        else
        {
            sb.AppendLine("★ 당신은 사건과 무관한 100% 결백한 시민입니다.");
            sb.AppendLine("질문이 들어오면 당당하게 알리바이를 말하십시오.");
        }

        sb.AppendLine("\n[Rules]");
        sb.AppendLine("1. 자신이 AI라고 말하지 마십시오.");
        sb.AppendLine("2. 한국어로 짧고 간결하게 대답하십시오.");
        _dynamicSystemPrompt = sb.ToString();
    }

    private async void OnPlayerSpeaks(object data)
    {
        string userQuestion = data as string;
        if (string.IsNullOrEmpty(userQuestion) || _currentNPC == null) return;

        // ★ 9턴 초과 시 묵비권 발동 로직 ★
        if (_chatTurnCount >= MAX_CHAT_TURNS)
        {
            string refuseMsg = "더 이상 당신의 억지 수사에 어울려줄 생각 없습니다. 제 변호사를 부르거나 기소하시죠. (진술 거부권 행사)";
            GlobalEventManager.Publish(GameEventType.AIResponded, refuseMsg);
            return; 
        }

        GlobalEventManager.Publish(GameEventType.AIThinkingStart); // UI 로딩 켜기

        _chatHistory.Add($"Player: {userQuestion}");
        if (_chatHistory.Count > 10) _chatHistory.RemoveRange(0, 2); // 최근 5번의 티키타카 유지

        string historyText = string.Join("\n", _chatHistory);
        string finalPrompt = $"{_dynamicSystemPrompt}\n\n[Chat History]\n{historyText}\n{_currentNPC.name}:";

        try
        {
            // 순수 두뇌(LLMManager)에게 하청 전송!
            string response = await LLMManager.Instance.GenerateResponseAsync(finalPrompt);

            if (!string.IsNullOrEmpty(response))
            {
                _chatHistory.Add($"{_currentNPC.name}: {response}");
                _chatTurnCount++; 
                GlobalEventManager.Publish(GameEventType.AIResponded, response); // UI 텍스트 출력
                Debug.Log($"[AIDialogueHandler] 남은 질문: {MAX_CHAT_TURNS - _chatTurnCount}회");
            }
            else
            {
                GlobalEventManager.Publish(GameEventType.AIError, "용의자가 침묵을 지킵니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIDialogueHandler] 추론 에러: {e.Message}");
            GlobalEventManager.Publish(GameEventType.AIError, "머리가 복잡한 것 같습니다.");
        }
    }
}