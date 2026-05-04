using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용
using System.Collections.Generic;

public class ChatView : MonoBehaviour
{
    [Header("UI 연결")]
    public TMP_InputField inputField;
    public Button submitButton;
    public TextMeshProUGUI logText;
    public ScrollRect scrollRect; // 대화 추가 시 맨 아래로 스크롤을 내리기 위해 필요
    private string _systemPrompt =
    "[Role]" +
    "\n너는 현재 살인 사건의 유력한 용의자다. 형사(Player)에게 취조받고 있다." +
    "\n너는 범인이 맞지만, 절대 들키지 않으려고 기를 쓰고 있다." +
    "\n[Rules - 반드시 지킬 것]" +
    "\n1. 지문 금지: (웃으며), (당황하며) 같은 행동 묘사를 절대 하지 마라.오직 대사만 출력해라." +
    "\n2. 단답형: 한 번에 반드시 '한 문장' 혹은 '두 문장'으로만 짧게 대답해라." +
    "\n3. 훈계 금지: 형사에게 따지거나 가르치려 들지 마라.무조건 잡아떼거나 짧게 반박해라." +
    "\n4. 말투: 거친 반말을 사용해라.예의 차리지 마라." +
    "\n5. 분석 금지: 네 대답의 의도나 상황을 설명하는 메타 발언을 절대 하지 마라." +
    "\n[Situation]" +
    "\n어젯밤 피해자의 집 근처 CCTV에 네 모습이 찍혔다. 너는 '근처 편의점에 담배 사러 갔을 뿐'이라고 주장하고 있다.";
    private List<string> _chatHistory = new List<string>();
    private bool _isProcessing = false; // 전송 중인지 체크하는 플래그


    private void Start()
    {
        // 초기화
        logText.text = "<b>[시스템]</b> AI 모델 로딩 대기 중...\n";

        // 버튼 이벤트 연결
        submitButton.onClick.AddListener(OnSubmit);

        // 엔터 키 이벤트 연결
        inputField.onSubmit.AddListener(delegate { OnSubmit(); });
    }

    private async void OnSubmit()
    {
        // 1. 안전장치: 이미 처리 중이거나 입력값이 없으면 즉시 차단
        if (_isProcessing) return;

        string userQuestion = inputField.text.Trim();
        if (string.IsNullOrEmpty(userQuestion)) return;

        // 2. 상태 잠금 (UI 비활성화 및 플래그 세팅)
        _isProcessing = true;
        inputField.interactable = false;
        submitButton.interactable = false;

        // 3. 유저 질문 로그 추가 및 기억 리스트 업데이트
        AppendLog("Player", userQuestion, "#55FF55");
        inputField.text = "";
        _chatHistory.Add($"Player: {userQuestion}");

        // 4. 슬라이딩 윈도우 (최근 9턴 유지)
        if (_chatHistory.Count > 18)
        {
            _chatHistory.RemoveRange(0, 2);
        }

        // 5. 프롬프트 조립
        string historyText = string.Join("\n", _chatHistory);
        string finalPrompt = $"{_systemPrompt}\n{historyText}\nSuspect:";

        // 대기 중임을 알리는 임시 로그 (선택 사항)
        AppendLog("System", "용의자가 생각 중입니다...", "#AAAAAA");

        try
        {
            // 6. AI 응답 생성 대기
            string response = await LLMManager.Instance.GenerateResponseAsync(finalPrompt);

            if (!string.IsNullOrEmpty(response))
            {
                _chatHistory.Add($"Suspect: {response}");
                AppendLog("Suspect", response, "#FF5555");
            }
            else
            {
                AppendLog("System", "용의자가 침묵을 지킵니다. (응답 실패)", "#FF0000");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"심문 중 에러 발생: {e.Message}");
            AppendLog("System", "수사 장비에 오류가 발생했습니다.", "#FF0000");
        }
        finally
        {
            // 7. 상태 해제 (성공하든 실패하든 다시 입력 가능하게)
            _isProcessing = false;
            inputField.interactable = true;
            submitButton.interactable = true;

            // 다시 입력창으로 포커스 이동
            inputField.ActivateInputField();
        }
    }

    /// <summary>
    /// 로그 창에 텍스트를 추가하고 스크롤을 맨 아래로 내립니다.
    /// </summary>
    private void AppendLog(string speaker, string message, string colorHex)
    {
        logText.text += $"\n<color={colorHex}><b>[{speaker}]</b></color> {message}\n";
        
        // 레이아웃이 갱신된 후 스크롤을 맨 아래(0)로 내리기 위해 Canvas.ForceUpdateCanvases 호출
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}