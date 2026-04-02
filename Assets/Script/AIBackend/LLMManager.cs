using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LLama;
using LLama.Common;
using LLama.Sampling;

public class LLMManager : MonoBehaviour
{
    public static LLMManager Instance { get; private set; }

    [Header("Gemma 3 모델 경로")]
    [Tooltip("StreamingAssets 폴더 기준 상대 경로를 입력하세요.")]
    public string path4B = "Models/gemma-3-4b-it-Q4_K_M.gguf";
    public string path12B = "Models/gemma-3-12b-it-Q4_K_M.gguf";

    [Header("AI 품질 설정 (유저 옵션)")]
    public bool useHighQuality12B = false; // VRAM이 넉넉한 PC일 경우 true로 변경
    public int contextWindowSize = 2048; // 최대 누적 토큰 (9턴 심문 기준 2048이면 충분)

    // LLamaSharp 핵심 객체
    private LLamaWeights _weights;
    private LLamaContext _context;
    private InteractiveExecutor _executor;

    private bool _isGenerating = false;

    private void Awake()
    {
        // 싱글톤 패턴 및 씬 전환 시 파괴 방지
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(this.gameObject); // 씬이 넘어가도 VRAM의 AI 모델을 유지함
    }

    private void Start()
    {
        // 게임 초기화 시 백그라운드에서 모델 미리 로드 시작
        _ = LoadModelAsync();
    }

    /// <summary>
    /// 설정된 품질(12B/4B)에 따라 모델을 VRAM에 로드합니다.
    /// 디스크에서 읽어오는 무거운 작업이므로 Task.Run으로 분리합니다.
    /// </summary>
    public async Task LoadModelAsync()
    {
        string relativePath = useHighQuality12B ? path12B : path4B;
        string absolutePath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);
        
        Debug.Log($"[로컬 AI] {(useHighQuality12B ? "12B (고품질)" : "4B (성능우선)")} 모델 로딩 시작...");

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
                    GpuLayerCount = 99 
                };

                _weights = LLamaWeights.LoadFromFile(parameters);
                _context = _weights.CreateContext(parameters);
                _executor = new InteractiveExecutor(_context);

                Debug.Log("[로컬 AI] 모델 로딩 완료! 심문 준비가 끝났습니다.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[로컬 AI] 모델 로드 중 치명적 오류 발생: {e.Message}");
            }
        });
    }

    /// <summary>
    /// NPC 심문 시 JSON 형태의 답변을 비동기로 생성합니다.
    /// </summary>
    public async Task<string> GenerateResponseAsync(string prompt)
    {
        if (_executor == null)
        {
            Debug.LogError("모델이 아직 로드되지 않았습니다.");
            return null;
        }

        if (_isGenerating)
        {
            Debug.LogWarning("이미 답변을 생성 중입니다. 연속 클릭 방지.");
            return null;
        }

        _isGenerating = true;
        StringBuilder responseBuilder = new StringBuilder();

        // 추론 파라미터 세팅 (온도 조절을 통해 위증/환각의 빈도를 기획 의도에 맞게 통제 가능)
        var inferenceParams = new InferenceParams()
        {
            MaxTokens = 512,
            AntiPrompts = new List<string> { "Player:" }, // 역할극 붕괴 방지
            
            // 온도(Temperature) 등의 확률 제어는 이제 SamplingPipeline 내부에서 처리합니다.
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = 0.8f 
            }
        };

        await Task.Run(async () =>
        {
            try
            {
                // 토큰을 하나씩 생성하며 StringBuilder에 누적
                await foreach (var text in _executor.InferAsync(prompt, inferenceParams))
                {
                    responseBuilder.Append(text);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[로컬 AI] 추론 중 오류 발생: {e.Message}");
            }
        });

        _isGenerating = false;
        
        // 최종 생성된 텍스트 반환 (이후 JsonUtility 등으로 파싱)
        return responseBuilder.ToString().Trim();
    }

    /// <summary>
    /// 옵션 창에서 모델 품질을 변경했을 때 호출하는 메서드
    /// </summary>
    public void ChangeQualitySetting(bool toHighQuality)
    {
        if (useHighQuality12B == toHighQuality) return;
        
        useHighQuality12B = toHighQuality;
        _ = LoadModelAsync();
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