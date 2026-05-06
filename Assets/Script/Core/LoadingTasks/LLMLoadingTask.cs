using System.Threading.Tasks;

public class LLMLoadingTask : ILoadingTask
{
    public string TaskName => "로컬 AI 모델 VRAM 적재 중...";
    public float Progress { get; private set; }

    public async Task ExecuteAsync()
    {
        Progress = 0.1f; // 적재 시작

        // 팀장님이 작성하신 LLMManager의 비동기 로드 함수 호출
        await LLMManager.Instance.LoadModelAsync();

        Progress = 1.0f; // 적재 완료
    }
}