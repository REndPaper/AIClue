using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Threading.Tasks;

public class AIDialogueHandler : MonoBehaviour
{
    [Header("현재 심문 상태")]
    private CharacterData _currentNPC;
    private string _dynamicSystemPrompt;

    private List<string> _chatHistory = new List<string>();
    private int _chatTurnCount = 0;
    private const int MAX_CHAT_TURNS = 9;

    private void OnEnable()
    {
        // 주파수 맞추기: UI에서 던지는 이벤트들을 구독합니다.
        GlobalEventManager.Subscribe(GameEventType.PlayerSpeaks, OnPlayerSpeaks);
        GlobalEventManager.Subscribe(GameEventType.TargetChanged, OnTargetChanged);
    }

    private void OnDisable()
    {
        GlobalEventManager.Unsubscribe(GameEventType.PlayerSpeaks, OnPlayerSpeaks);
        GlobalEventManager.Unsubscribe(GameEventType.TargetChanged, OnTargetChanged);
    }

    // ★ UI에서 NPC를 바꿨을 때 호출되는 이벤트 핸들러
    private void OnTargetChanged(object data)
    {
        CharacterData npcData = data as CharacterData;
        if (npcData == null) return;

        _currentNPC = npcData;
        _chatHistory.Clear();
        _chatTurnCount = 0;

        BuildSystemPrompt();

        // UI에 남은 턴 수를 갱신하라고 알림 (필요시)
        // GlobalEventManager.Publish(GameEventType.TurnCountChanged, MAX_CHAT_TURNS);

        Debug.Log($"[AIDialogueHandler] {_currentNPC.name} 심문 세션 준비 완료.");
    }

    private void BuildSystemPrompt()
    {
        if (_currentNPC == null) return;

        // ScenarioManager의 정답지와 비교
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
            sb.AppendLine("당신은 진범입니다! 하지만 절대 자백하지 마십시오.");
            if (_currentNPC.breakerEvidence != null)
                sb.AppendLine($"현장에 당신의 결정적 단서가 남았습니다: [{_currentNPC.breakerEvidence.name}] - {_currentNPC.breakerEvidence.description}");

            sb.AppendLine("단서를 추궁당하면 당황하는 기색을 보이되, 억지 논리로 변명하거나 불쾌함을 표현하며 화제를 돌리십시오.");
        }
        else if (isFalseCulprit)
        {
            sb.AppendLine("당신은 무고하지만 오해를 받고 있는 시민입니다.");
            if (_currentNPC.breakerEvidence != null)
                sb.AppendLine($"억울하게 현장에 남은 흔적: [{_currentNPC.breakerEvidence.name}] - {_currentNPC.breakerEvidence.description}");

            sb.AppendLine("단서를 추궁당하면 자신의 결백을 주장하며 매우 억울해하십시오.");
        }
        else
        {
            sb.AppendLine("당신은 사건과 100% 무관한 결백한 시민입니다.");
            sb.AppendLine("질문을 받으면 당당하게 자신의 알리바이를 반복하여 말하십시오.");
        }

        sb.AppendLine("\n[General Rules]");
        sb.AppendLine("1. 자신이 AI나 언어 모델이라고 절대 말하지 마십시오.");
        sb.AppendLine("2. 3문장 이내로 한국어로 짧고 간결하게 대답하십시오.");
        sb.AppendLine("3. 존댓말과 반말 중 캐릭터 성격에 맞는 말투를 일관되게 사용하십시오.");

        _dynamicSystemPrompt = sb.ToString();
    }

    private async void OnPlayerSpeaks(object data)
    {
        PlayerSpeakData speakData = data as PlayerSpeakData;
        if (speakData == null || string.IsNullOrEmpty(speakData.question) || _currentNPC == null) return;
        string userQuestion = speakData.question;
        string evidenceContext = speakData.evidenceContext;

        // 1. 턴 제한 체크 로직 (기존과 동일)
        if (_chatTurnCount >= MAX_CHAT_TURNS)
        {
            string refuseMsg = "더 이상 당신의 억지 수사에 어울려줄 생각 없습니다. 제 변호사를 부르거나 기소하시죠.";
            GlobalEventManager.Publish(GameEventType.AIResponded, refuseMsg);
            return;
        }

        GlobalEventManager.Publish(GameEventType.AIThinkingStart);

        // 2. 대화 기록 누적 (최근 티키타카 유지)
        _chatHistory.Add($"Player: {userQuestion}");
        if (_chatHistory.Count > 8) _chatHistory.RemoveRange(0, 2);
        string historyText = string.Join("\n", _chatHistory);


        // ★ 시스템 프롬프트 + [확보된 단서] + [대화 내역] 순서로 햄버거 조립
        string finalPrompt = $"{_dynamicSystemPrompt}\n\n" +
                             $"{evidenceContext}\n\n" +
                             $"[Conversation History]\n{historyText}\n" +
                             $"{_currentNPC.name}:";

        try
        {
            // 조립된 프롬프트를 LLM에게 전송
            var customAntiPrompts = new List<string>
            {
                $"{_currentNPC.name}:",
                $"[{_currentNPC.name}]",
                "Player:",
                "[Player]",
                "System:"
            };
            string response = await LLMManager.Instance.GenerateResponseAsync(finalPrompt, customAntiPrompts);

            if (!string.IsNullOrEmpty(response))
            {
                _chatHistory.Add($"{_currentNPC.name}: {response}");
                _chatTurnCount++;
                GlobalEventManager.Publish(GameEventType.AIResponded, response);
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