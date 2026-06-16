using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LLama;
using LLama.Common;
using LLama.Sampling;
using LLama.Native;
using System.Linq;

public class LLMManager : MonoBehaviour
{
    public static LLMManager Instance { get; private set; }

    [Header("모델 경로")]
    [Tooltip("StreamingAssets 폴더 기준 상대 경로를 입력하세요.")]
    public string[] modelpaths = { "Models/gemma-3-4b-it-Q4_K_M.gguf", "Models/EXAONE-3.5-7.8B-Instruct-Q4_K_M.gguf", "Models/gemma-3-12b-it-qat-int4-Q4_K_M.gguf" };

    [Header("AI 품질 설정 (유저 옵션)")]
    public int qualityIndex = 0;
    public int contextWindowSize = 4096;

    // LLamaSharp 핵심 객체
    private LLamaWeights _weights;
    private LLamaContext _context;
    private StatelessExecutor _executor;

    private bool _isGenerating = false;

    private void Awake()
    {
        // 싱글톤 패턴 및 씬 전환 시 파괴 방지
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        // 1. 플러그인이 모여있는 폴더 절대 경로 계산
        string pluginDir = System.IO.Path.Combine(Application.dataPath, "Plugins/Linux/x86_64");
        
        // 2. 현재 우분투 시스템의 LD_LIBRARY_PATH 가져오기
        string currentLdPath = System.Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";

        // 3. 우리 경로가 없다면 운영체제 환경 변수에 강제로 쑤셔넣기
        if (!currentLdPath.Contains(pluginDir))
        {
            string newLdPath = string.IsNullOrEmpty(currentLdPath) ? pluginDir : currentLdPath + ":" + pluginDir;
            
            // 환경 변수 런타임 세팅!
            System.Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", newLdPath);
            Debug.Log($"[로컬 AI] 우분투 환경 변수 강제 주입 완료: {newLdPath}");
        }
#endif

        Instance = this;
        DontDestroyOnLoad(this.gameObject); // 씬이 넘어가도 VRAM의 AI 모델을 유지함
    }

    private void Start()
    {
        // PlayerPrefs에서 저장된 AI 품질 설정 로드
        qualityIndex = PlayerPrefs.GetInt("SelectedModelIndex", 0);
        if (qualityIndex < 0 || qualityIndex >= modelpaths.Length)
        {
            qualityIndex = 0;
        }

        // 게임 초기화 시 백그라운드에서 모델 미리 로드 시작
        _ = LoadModelAsync();
    }

    void TraceLibraryPaths()
    {
        Debug.Log($"[로컬 AI] 현재 작업 디렉토리: {System.Environment.CurrentDirectory}");
        Debug.Log($"[로컬 AI] 데이터 경로: {Application.dataPath}");

        // 리눅스 전용: LD_LIBRARY_PATH 변수 확인
        string ldPath = System.Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
        Debug.Log($"[로컬 AI] LD_LIBRARY_PATH: {ldPath ?? "설정 안 됨"}");

        // 라이브러리가 있어야 할 실제 절대 경로 계산
        string expectedPath = System.IO.Path.Combine(Application.dataPath, "Plugins/Linux/x86_64/libllama.so");
        Debug.Log($"[로컬 AI] 내가 예상하는 절대 경로: {expectedPath}");
        Debug.Log($"[로컬 AI] 해당 경로에 파일 존재 여부: {System.IO.File.Exists(expectedPath)}");
    }

    /// <summary>
    /// 설정된 품질(12B/4B)에 따라 모델을 VRAM에 로드합니다.
    /// 디스크에서 읽어오는 무거운 작업이므로 Task.Run으로 분리합니다.
    /// </summary>
    public async Task LoadModelAsync()
    {

        string relativePath = modelpaths[qualityIndex];
        string absolutePath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);

        // [추가] 슬래시를 윈도우식(\)으로 통일하고, 깔끔한 절대 경로로 다림질
        absolutePath = System.IO.Path.GetFullPath(absolutePath.Replace("/", "\\"));

        Debug.Log($"[로컬 AI] 모델 로딩 시작...");

        // 기존에 로드된 모델이 있다면 VRAM 누수 방지를 위해 안전하게 메모리 해제
        DisposeAI();

        await Task.Run(() =>
        {
            try
            {
                var parameters = new ModelParams(absolutePath)
                {
                    ContextSize = (uint)contextWindowSize,
                    // Vulkan 가속을 위해 레이어를 GPU로 전면 오프로딩
                    GpuLayerCount = 15
                };

                _weights = LLamaWeights.LoadFromFile(parameters);
                _context = _weights.CreateContext(parameters);
                _executor = new StatelessExecutor(_weights, parameters);

                Debug.Log("[로컬 AI] 모델 로딩 완료! 심문 준비가 끝났습니다.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[로컬 AI] 모델 로드 중 치명적 오류 발생: {e.Message}, {e.InnerException}");
                Debug.LogError($"{e.InnerException.Message} , {e.InnerException.StackTrace}");
            }
        });
    }

    /// <summary>
    /// NPC 심문 시 JSON 형태의 답변을 비동기로 생성합니다.
    /// </summary>
    public async Task<string> GenerateResponseAsync(string prompt)
    {
        if (_executor == null || _isGenerating) return null;

        _isGenerating = true;
        StringBuilder responseBuilder = new StringBuilder();

        var inferenceParams = new InferenceParams()
        {
            MaxTokens = 1024,
            // 안티프롬프트는 그대로 유지 (엔진의 중단을 위함)
            AntiPrompts = new List<string> { "Player:", "[Player]", "System:" },
            SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.8f, RepeatPenalty = 1.1f }
        };

        try
        {
            // InferAsync는 태생적으로 비동기 스트림이므로 Task.Run 안에 넣을 필요가 없습니다.
            // 유니티 메인 스레드를 방해하지 않기 위해 바로 await foreach를 씁니다.
            await foreach (var text in _executor.InferAsync(prompt, inferenceParams))
            {
                responseBuilder.Append(text);

                // [실시간 필터링 팁] 
                // 만약 나중에 텍스트를 한 글자씩 실시간 UI로 보여주실 거라면
                // 여기서 StringBuilder의 현재 내용을 체크해서 "Player:"가 포함되면 
                // 루프를 break 하도록 짤 수도 있습니다.
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[로컬 AI] 추론 중 오류 발생: {e.Message}");
        }

        _isGenerating = false;

        // [최종 후처리] 
        // 안티프롬프트를 만나서 멈췄더라도, 결과물 끝에 "Player:"가 붙어있을 수 있으므로 싹둑 잘라냅니다.
        string finalResponse = responseBuilder.ToString();

        foreach (var anti in inferenceParams.AntiPrompts)
        {
            if (finalResponse.Contains(anti))
            {
                finalResponse = finalResponse.Split(new[] { anti }, StringSplitOptions.None)[0];
            }
        }

        return finalResponse.Trim();
    }

    /// <summary>
    /// 옵션 창에서 모델 품질을 변경했을 때 호출하는 메서드
    /// </summary>
    public void ChangeQualitySetting(int quality)
    {
        if (0 <= quality && quality < modelpaths.Length)
        {
            qualityIndex = quality;
            _ = LoadModelAsync();
        }
    }

    public void UnloadModelFromVRAM()
    {
        Debug.Log("[로컬 AI] VRAM에서 모델을 안전하게 해제합니다.");
        DisposeAI(); // 기존에 만든 해제 함수 호출
    }

    /// <summary>
    /// 씬이 넘어가거나 게임이 종료될 때 VRAM을 반드시 비워주어야 합니다.
    /// </summary>
    private void DisposeAI()
    {
        _context?.Dispose();
        _weights?.Dispose();

        _context = null;
        _weights = null;
        _executor = null;
    }

    private void OnDestroy()
    {
        DisposeAI();
    }
}