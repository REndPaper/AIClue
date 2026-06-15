using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class SceneLoadingTask : ILoadingTask
{
    private readonly string _sceneName;
    public string TaskName => $"{_sceneName} 씬 데이터를 복구하는 중...";
    public float Progress { get; private set; }

    public SceneLoadingTask(string sceneName)
    {
        _sceneName = sceneName;
    }

    public async Task ExecuteAsync()
    {
        var asyncOp = SceneManager.LoadSceneAsync(_sceneName);

        // 씬 로드가 완료될 때까지 Progress 갱신 루프
        while (!asyncOp.isDone)
        {
            // 유니티 씬 로딩은 0.9에서 사실상 완료되므로 보정 처리
            Progress = Mathf.Clamp01(asyncOp.progress / 0.9f);

            // 메인 스레드를 점유하지 않도록 아주 잠깐 대기
            await Task.Yield();
        }

        Progress = 1.0f;
    }
}