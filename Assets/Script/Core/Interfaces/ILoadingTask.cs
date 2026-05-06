using System.Threading.Tasks;

public interface ILoadingTask
{
    string TaskName { get; }      // UI에 띄울 텍스트
    float Progress { get; }       // 0.0f ~ 1.0f 사이의 진행률
    Task ExecuteAsync();          // 실제 비동기 실행 로직
}