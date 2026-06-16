using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 정답 제출 후 결과 씬(Result Scene)에서 교수 AI의 채점 코멘트를 생성하는 전담 핸들러.
/// </summary>
public class AIFeedbackHandler : MonoBehaviour
{
    /// <summary>
    /// JudgmentSystem에서 최종 계산이 끝나면 이 함수를 호출하여 피드백 텍스트를 요청합니다.
    /// </summary>
    public async Task<string> GenerateProfessorFeedback(string grade, bool isCulpritCorrect, bool isWeaponCorrect, int exploreCount, int chatCount, string compressedChatLog)
    {
        Debug.Log("[AIFeedbackHandler] 교수 AI 피드백 생성 시작...");
        
        StringBuilder sb = new StringBuilder();

        // 1. 교수 페르소나
        sb.AppendLine("[Role]");
        sb.AppendLine("당신은 경찰대학의 범죄심리학 특임 교수입니다. 학생(Player)의 모의 수사 결과를 채점하고 뼈 때리는 피드백을 주어야 합니다.");
        sb.AppendLine("말투는 근엄하고 지적이며, '~~하게', '~~군', '~~일세' 같은 노교수 어투를 사용하십시오.");

        // 2. 학생의 채점표
        sb.AppendLine("\n[Student's Result Data]");
        sb.AppendLine($"- 최종 학점: {grade}");
        sb.AppendLine($"- 범인 지목: {(isCulpritCorrect ? "성공" : "실패")}");
        sb.AppendLine($"- 흉기 특정: {(isWeaponCorrect ? "성공" : "실패")}");
        sb.AppendLine($"- 현장 탐색: {exploreCount}회");
        sb.AppendLine($"- 심문 횟수: {chatCount}회");
        
        sb.AppendLine("\n[Student's Interrogation Log]");
        // 리치 텍스트 컬러 태그 제거하여 AI 가독성 확보
        string cleanedLog = string.Empty;
        if (!string.IsNullOrEmpty(compressedChatLog))
        {
            cleanedLog = System.Text.RegularExpressions.Regex.Replace(compressedChatLog, "<.*?>", string.Empty);
        }
        sb.AppendLine(cleanedLog);

        // 3. 학점별 차등 지시사항
        sb.AppendLine("\n[Instruction]");
        sb.AppendLine("위 데이터를 바탕으로 학생에게 3~4문장의 강렬한 평가 코멘트를 남겨주십시오.");
        
        switch (grade)
        {
            case "A+":
                sb.AppendLine("지시: 뛰어난 통찰력을 극찬하십시오. 대화 로그 중 예리했던 부분을 칭찬하고, 훌륭한 형사가 될 것이라 덕담을 건네십시오.");
                break;
            case "B0":
                sb.AppendLine("지시: 정답은 맞췄으나 '효율성(탐색/대화 횟수 너무 많음)'이 떨어진 부분을 지적하십시오. '머리는 좋은데 행동이 굼뜨군' 같은 뉘앙스를 주십시오.");
                break;
            case "C+":
                sb.AppendLine("지시: 학생의 추리에 구멍이 있음을 지적하십시오. 헛다리를 짚은 것을 한심해하며 재교육이 필요하다고 꾸짖으십시오.");
                break;
            case "F":
                sb.AppendLine("지시: 크게 화내십시오. 무고한 시민을 감옥에 보낼 뻔한 멍청한 질문(로그 참고)을 조롱하고, 당장 내 수업에서 나가라고 호통치십시오.");
                break;
        }

        // 두뇌(LLMManager)에게 최종 하청!
        try
        {
            string feedback = await LLMManager.Instance.GenerateResponseAsync(sb.ToString(), null, false);
            return string.IsNullOrEmpty(feedback) ? "자네의 보고서는 읽을 가치도 없군. (시스템 에러)" : feedback;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AIFeedbackHandler] 교수 피드백 생성 실패: {e.Message}");
            return "채점 시스템에 문제가 생겼네. 다시 제출하게.";
        }
    }
}