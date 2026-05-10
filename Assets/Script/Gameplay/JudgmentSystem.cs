using UnityEngine;

/// <summary>
/// 플레이어의 최종 추리 결과를 채점하고 학점(Grade)을 산출하는 시스템입니다.
/// </summary>
public class JudgmentSystem : MonoBehaviour
{
    [Header("수사 효율성 기준점 (이 횟수를 넘기면 감점)")]
    public int recommendExploreCount = 5;  // 권장 탐색 횟수 (맵이 8칸이므로 5칸 정도가 적당)
    public int recommendChatCount = 10;    // 권장 대화 횟수

    /// <summary>
    /// 채점 결과를 담는 구조체 (AIFeedbackHandler에 통째로 넘기기 좋음)
    /// </summary>
    public struct ReportCard
    {
        public bool isCulpritCorrect;
        public bool isWeaponCorrect;
        public int totalScore;
        public string finalGrade;
    }

    /// <summary>
    /// UI에서 제출 버튼을 눌렀을 때 호출되어 최종 성적표를 반환합니다.
    /// </summary>
    public ReportCard EvaluateInvestigation(string submittedCulpritId, string submittedWeaponId, int actualExploreCount, int actualChatCount)
    {
        ReportCard report = new ReportCard();

        // 1. 진짜 정답 가져오기 (ScenarioManager에서 훔쳐옴)
        string trueCulpritId = ScenarioManager.Instance.trueCulprit.id;
        string trueWeaponId = ScenarioManager.Instance.trueWeapon.id;

        // 2. 핵심 추리 채점 (각 40점, 총 80점)
        int coreScore = 0;
        report.isCulpritCorrect = (submittedCulpritId == trueCulpritId);
        report.isWeaponCorrect = (submittedWeaponId == trueWeaponId);

        if (report.isCulpritCorrect) coreScore += 40;
        if (report.isWeaponCorrect) coreScore += 40;

        // 3. 수사 효율성 채점 (20점 만점, 초과 횟수당 1점씩 감점)
        int efficiencyScore = 20;

        if (actualExploreCount > recommendExploreCount)
        {
            efficiencyScore -= (actualExploreCount - recommendExploreCount);
        }

        if (actualChatCount > recommendChatCount)
        {
            efficiencyScore -= (actualChatCount - recommendChatCount);
        }

        // 깎인 점수가 0 밑으로 내려가지 않게 방어 (0 ~ 20점 유지)
        efficiencyScore = Mathf.Clamp(efficiencyScore, 0, 20);

        // 4. 최종 점수 합산
        report.totalScore = coreScore + efficiencyScore;

        // 5. 학점 판정 (기획서 임계치 기준)
        report.finalGrade = CalculateGrade(report.totalScore);

        Debug.Log($"[JudgmentSystem] 채점 완료: {report.totalScore}점 ({report.finalGrade}) / 범인({report.isCulpritCorrect}), 흉기({report.isWeaponCorrect})");

        return report;
    }

    /// <summary>
    /// 점수에 따른 학점을 반환합니다.
    /// </summary>
    private string CalculateGrade(int score)
    {
        if (score >= 90) return "A+";
        if (score >= 70) return "B0";
        if (score >= 40) return "C+";
        return "F";
    }
}