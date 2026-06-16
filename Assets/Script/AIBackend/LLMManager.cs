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

    // 모델 인덱스 변경 알림 이벤트 (UI 연동용)
    public static event Action<int> OnModelIndexChanged;

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
    private int _loadedQualityIndex = -1;
    private bool _isLoadingModel = false;

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
    private bool LoadModelInternal(string absolutePath)
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
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[로컬 AI] 모델 로드 실패: {e.Message}");
            if (e.InnerException != null)
            {
                Debug.LogError($"[로컬 AI] 상세 예외: {e.InnerException.Message}");
            }
            return false;
        }
    }

    public async Task LoadModelAsync()
    {
        if (_isLoadingModel)
        {
            Debug.Log("[로컬 AI] 이미 모델이 로딩 중입니다. 이전 로딩을 대기하거나 생략합니다.");
            return;
        }

        if (_weights != null && _loadedQualityIndex == qualityIndex)
        {
            Debug.Log($"[로컬 AI] {modelpaths[qualityIndex]} 모델이 이미 메모리에 로드되어 있습니다. 로딩을 생략합니다.");
            return;
        }

        _isLoadingModel = true;

        string relativePath = modelpaths[qualityIndex];
        string absolutePath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);

        // [추가] 백슬래시 윈도우식(\\)으로 통일하고, 깔끔한 절대 경로로 다듬기
        absolutePath = System.IO.Path.GetFullPath(absolutePath.Replace("/", "\\"));

        Debug.Log($"[로컬 AI] 모델 로딩 시작... (Index: {qualityIndex}, Path: {relativePath})");

        // 기존에 로딩된 모델이 있다면 VRAM 누수 방지를 위해 안전하게 메모리 해제
        DisposeAI();

        bool isFallback = false;
        string fallbackPath = "";

        await Task.Run(() =>
        {
            bool success = LoadModelInternal(absolutePath);
            if (!success && qualityIndex != 0)
            {
                isFallback = true;
            }
        });

        if (isFallback)
        {
            Debug.LogWarning("[로컬 AI] 모델 로딩 실패로 인해 최하옵(Gemma 3 4B-it)으로 고정 후 재시도합니다.");
            qualityIndex = 0;
            PlayerPrefs.SetInt("SelectedModelIndex", 0);
            PlayerPrefs.Save();

            // 최하옵 경로 계산 (메인 스레드에서 안전하게 실행!)
            fallbackPath = System.IO.Path.Combine(Application.streamingAssetsPath, modelpaths[0]);
            fallbackPath = System.IO.Path.GetFullPath(fallbackPath.Replace("/", "\\"));

            // 불완전 리소스 정리
            DisposeAI();

            // 최하옵으로 로드 재시도
            await Task.Run(() =>
            {
                LoadModelInternal(fallbackPath);
            });

            // UI 연동을 위한 이벤트 발생
            OnModelIndexChanged?.Invoke(0);

            _loadedQualityIndex = 0;
        }
        else
        {
            _loadedQualityIndex = qualityIndex;
        }

        _isLoadingModel = false;
    }

    /// <summary>
    /// NPC 심문 시 JSON 형태의 답변을 비동기로 생성합니다.
    /// </summary>
    public async Task<string> GenerateResponseAsync(string prompt, List<string> customAntiPrompts = null, bool stopOnDoubleNewline = true)
    {
        if (_executor == null || _isGenerating) return null;

        _isGenerating = true;
        StringBuilder responseBuilder = new StringBuilder();

        // 기본 안티프롬프트 목록 설정
        var antiPrompts = new List<string> { "Player:", "[Player]", "System:", "Suspect:" };
        if (customAntiPrompts != null)
        {
            foreach (var anti in customAntiPrompts)
            {
                if (!antiPrompts.Contains(anti))
                {
                    antiPrompts.Add(anti);
                }
            }
        }

        var inferenceParams = new InferenceParams()
        {
            MaxTokens = 1024,
            AntiPrompts = antiPrompts,
            SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.8f, RepeatPenalty = 1.1f }
        };

        try
        {
            // InferAsync는 태생적으로 비동기 스트림이므로 Task.Run 안에 넣을 필요가 없습니다.
            // 유니티 메인 스레드를 방해하지 않기 위해 바로 await foreach를 씁니다.
            await foreach (var text in _executor.InferAsync(prompt, inferenceParams))
            {
                responseBuilder.Append(text);

                // 실시간 중단 검사 (줄바꿈 두 번이나 코드 블록 기호 등 감지 시 즉시 중단하여 추론 시간 절약)
                if (ShouldStopGeneration(responseBuilder.ToString(), customAntiPrompts, stopOnDoubleNewline))
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[로컬 AI] 추론 중 오류 발생: {e.Message}");
        }
        finally
        {
            _isGenerating = false;
        }

        string finalResponse = responseBuilder.ToString();

        // 최종 후처리 클리닝 적용
        string npcName = "";
        if (customAntiPrompts != null && customAntiPrompts.Count > 0)
        {
            npcName = customAntiPrompts[0].Replace(":", "").Replace("[", "").Replace("]", "").Trim();
        }

        return CleanLLMResponse(finalResponse, npcName, stopOnDoubleNewline);
    }

    private bool ShouldStopGeneration(string text, List<string> antiPrompts, bool stopOnDoubleNewline)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // 앞부분 공백/줄바꿈을 제거한 실제 생성 콘텐츠가 존재할 때부터 중단 검사를 수행합니다.
        string trimmedStart = text.TrimStart();
        if (trimmedStart.Length == 0) return false;

        // 1. 트리플 백틱 ``` 이 포함되어 있는지 확인
        if (trimmedStart.Contains("```")) return true;

        // 2. 트리플 쌍따옴표 """ 가 포함되어 있는지 확인
        if (trimmedStart.Contains("\"\"\"")) return true;

        // 3. 안티 프롬프트 중 하나라도 포함되어 있는지 확인
        if (antiPrompts != null)
        {
            foreach (var anti in antiPrompts)
            {
                if (text.Contains(anti))
                {
                    return true;
                }
            }
        }

        // 4. 본문 중간에 나타나는 더블 엔터 (\n\n) 검사 (대화형 모드 등 줄바꿈 단절 필요시에만 작동)
        if (stopOnDoubleNewline)
        {
            int doubleNewlineIndex = trimmedStart.IndexOf("\n\n", StringComparison.Ordinal);
            if (doubleNewlineIndex >= 0) return true;

            int doubleNewlineRnIndex = trimmedStart.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (doubleNewlineRnIndex >= 0) return true;
        }

        return false;
    }

    private string CleanLLMResponse(string text, string npcName, bool stopOnDoubleNewline)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        string cleanText = text.Trim();

        // 1. 만약 전체 텍스트가 ``` 로 둘러싸여 있다면 제거 (예: ```대사```)
        if (cleanText.StartsWith("```") && cleanText.EndsWith("```") && cleanText.Length > 6)
        {
            cleanText = cleanText.Substring(3, cleanText.Length - 6).Trim();
        }
        else if (cleanText.StartsWith("```"))
        {
            cleanText = cleanText.Substring(3).Trim();
        }

        // 2. 만약 전체 텍스트가 쌍따옴표(")나 따옴표로 감싸여 있다면 제거
        if (cleanText.StartsWith("\"") && cleanText.EndsWith("\"") && cleanText.Length > 2)
        {
            cleanText = cleanText.Substring(1, cleanText.Length - 2).Trim();
        }
        else if (cleanText.StartsWith("'") && cleanText.EndsWith("'") && cleanText.Length > 2)
        {
            cleanText = cleanText.Substring(1, cleanText.Length - 2).Trim();
        }

        cleanText = cleanText.Trim();

        // 3. 본문 내부의 원치 않는 반복 패턴 잘라내기
        var stopPatterns = new List<string>();

        // 대화 중단형 턴 패턴들은 대화 모드(stopOnDoubleNewline이 true)일 때만 작동하도록 격리하여
        // 평가 피드백 같은 자유형 줄글이 본문 내 키워드 매칭(예: [시스템] 등)으로 통째로 잘려나가는 것을 방지합니다.
        if (stopOnDoubleNewline)
        {
            stopPatterns.Add("Player:");
            stopPatterns.Add("[Player]");
            stopPatterns.Add("System:");
            stopPatterns.Add("[System]");
            stopPatterns.Add("Suspect:");
            stopPatterns.Add("\n\n");
            stopPatterns.Add("\r\n\r\n");
        }

        stopPatterns.Add("```");
        stopPatterns.Add("\"\"\"");

        if (!string.IsNullOrEmpty(npcName))
        {
            stopPatterns.Add($"{npcName}:");
            stopPatterns.Add($"[{npcName}]");
            stopPatterns.Add($"\n{npcName}");
        }

        int earliestIndex = cleanText.Length;
        foreach (var pattern in stopPatterns)
        {
            int idx = cleanText.IndexOf(pattern, StringComparison.Ordinal);
            if (idx >= 0 && idx < earliestIndex)
            {
                earliestIndex = idx;
            }
        }

        if (earliestIndex < cleanText.Length)
        {
            cleanText = cleanText.Substring(0, earliestIndex).Trim();
        }

        // 마지막 정리
        cleanText = cleanText.Trim(' ', '\n', '\r', '\t', '`', '"', '\'');

        return cleanText;
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
        _loadedQualityIndex = -1;
    }

    private void OnDestroy()
    {
        DisposeAI();
    }
}