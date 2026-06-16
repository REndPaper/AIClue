using UnityEngine;
using UnityEngine.UI; // 버튼 제어용
using TMPro;

public class TitleUIController : MonoBehaviour
{
    [Header("UI 패널 할당 (CanvasGroup)")]
    public CanvasGroup mainMenuGroup;
    public CanvasGroup scenarioSelectGroup;
    public CanvasGroup buttonPanelGroup;
    public CanvasGroup settingsPanelGroup;

    [Header("설정 UI 요소")]
    public Slider volumeSlider;
    public TMP_Dropdown modelDropdown;

    [Header("시나리오 정보 텍스트 (Folder UI)")]
    public TextMeshProUGUI CaseNoTxt;
    public TextMeshProUGUI CaseTxt;
    public TextMeshProUGUI OverviewTxt;
    public TextMeshProUGUI VictimTxt;
    public TextMeshProUGUI PlaceTxt;

    [Header("제어 버튼")]
    public Button prevBtn; // 이전 사건 버튼
    public Button nextBtn; // 다음 사건 버튼

    public Button startBtn;

    private void OnEnable()
    {
        GlobalEventManager.Subscribe(GameEventType.ShowMainMenuUI, ShowMainMenu);
        GlobalEventManager.Subscribe(GameEventType.HideMainMenuUI, HideMainMenu);
        GlobalEventManager.Subscribe(GameEventType.ShowScenarioSelectUI, ShowScenarioSelect);
        GlobalEventManager.Subscribe(GameEventType.HideScenarioSelectUI, HideScenarioSelect);

        LLMManager.OnModelIndexChanged += OnModelIndexChanged;
    }

    private void OnDisable()
    {
        GlobalEventManager.Unsubscribe(GameEventType.ShowMainMenuUI, ShowMainMenu);
        GlobalEventManager.Unsubscribe(GameEventType.HideMainMenuUI, HideMainMenu);
        GlobalEventManager.Unsubscribe(GameEventType.ShowScenarioSelectUI, ShowScenarioSelect);
        GlobalEventManager.Unsubscribe(GameEventType.HideScenarioSelectUI, HideScenarioSelect);

        LLMManager.OnModelIndexChanged -= OnModelIndexChanged;
    }

    private void OnModelIndexChanged(int index)
    {
        if (modelDropdown != null)
        {
            modelDropdown.value = index;
            Debug.Log($"[TitleUI] AI 모델이 폴백/변경되어 UI를 인덱스 {index}로 갱신합니다.");
        }
    }

    // ---------------- [이벤트 수신 시 작동할 함수들] ----------------
    private void ShowMainMenu(object data = null) => EnablePanel(mainMenuGroup);
    private void HideMainMenu(object data = null) => DisablePanel(mainMenuGroup);

    private void ShowScenarioSelect(object data = null)
    {
        EnablePanel(scenarioSelectGroup);
        UpdateScenarioUI(); // ★ 화면이 켜질 때 데이터를 새로고침합니다.
    }
    private void HideScenarioSelect(object data = null) => DisablePanel(scenarioSelectGroup);

    // ---------------- [데이터 바인딩: JSON -> UI] ----------------
    private void UpdateScenarioUI()
    {
        // 1. 시나리오 매니저에 이미 로드된 데이터가 있는지 확인
        var data = ScenarioManager.Instance.currentScenario;
        if (data == null || string.IsNullOrEmpty(data.caseNo))
        {
            // 아직 로드 전이라면 기본 파일 하나를 로드함
            ScenarioManager.Instance.SetupGame("Scenario/CASE_001/CASE_001.json");
            data = ScenarioManager.Instance.currentScenario;
        }

        // 2. UI 텍스트에 데이터 꽂아넣기
        CaseNoTxt.text = data.caseNo.Replace("_", " ");
        CaseTxt.text = data.caseName;
        OverviewTxt.text = data.overview;
        VictimTxt.text = data.victim;
        PlaceTxt.text = data.place;

        // 3. 이전/다음 버튼 끄기 & 시작 버튼 활성화 확실히 하기
        if (prevBtn != null) prevBtn.gameObject.SetActive(false);
        if (nextBtn != null) nextBtn.gameObject.SetActive(false);
        if (startBtn != null) startBtn.gameObject.SetActive(true);

        Debug.Log($"[TitleUI] {data.caseNo} 데이터 바인딩 완료!");
    }

    // ---------------- [CanvasGroup 온오프 헬퍼 함수] ----------------
    private void EnablePanel(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void DisablePanel(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void Start()
    {
        // 1. 컴포넌트 자동 탐색 및 바인딩 (인스펙터 할당 누락 방지)
        if (buttonPanelGroup == null)
        {
            GameObject btnPanelObj = GameObject.Find("MainMenuUI/BtnPanel");
            if (btnPanelObj != null)
            {
                buttonPanelGroup = btnPanelObj.GetComponent<CanvasGroup>() ?? btnPanelObj.AddComponent<CanvasGroup>();
            }
        }

        if (settingsPanelGroup == null)
        {
            GameObject settingsPanelObj = GameObject.Find("MainMenuUI/SettingsPanel");
            if (settingsPanelObj != null)
            {
                settingsPanelGroup = settingsPanelObj.GetComponent<CanvasGroup>() ?? settingsPanelObj.AddComponent<CanvasGroup>();
            }
        }

        if (volumeSlider == null)
        {
            GameObject sliderObj = GameObject.Find("MainMenuUI/SettingsPanel/VolumeSlider");
            if (sliderObj != null)
            {
                volumeSlider = sliderObj.GetComponent<Slider>();
            }
        }

        if (modelDropdown == null)
        {
            GameObject ddObj = GameObject.Find("MainMenuUI/SettingsPanel/ModelDropdown");
            if (ddObj != null)
            {
                modelDropdown = ddObj.GetComponent<TMP_Dropdown>();
            }
        }

        // 2. 버튼 클릭 리스너 동적 바인딩
        GameObject menuSettingBtnObj = GameObject.Find("MainMenuUI/BtnPanel/SettingBtn");
        if (menuSettingBtnObj != null)
        {
            Button menuSettingBtn = menuSettingBtnObj.GetComponent<Button>();
            if (menuSettingBtn != null)
            {
                menuSettingBtn.onClick.RemoveAllListeners();
                menuSettingBtn.onClick.AddListener(OnClickOpenSettings);
            }
        }

        GameObject closeBtnObj = GameObject.Find("MainMenuUI/SettingsPanel/CloseBtn");
        if (closeBtnObj != null)
        {
            Button closeBtn = closeBtnObj.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(OnClickCloseSettings);
            }
        }

        GameObject gameStartBtnObj = GameObject.Find("MainMenuUI/BtnPanel/GameStartBtn");
        if (gameStartBtnObj != null)
        {
            Button gameStartBtn = gameStartBtnObj.GetComponent<Button>();
            if (gameStartBtn != null)
            {
                gameStartBtn.onClick.RemoveAllListeners();
                gameStartBtn.onClick.AddListener(OnClickStartGame);
            }
        }

        GameObject exitBtnObj = GameObject.Find("MainMenuUI/BtnPanel/ExitBtn");
        if (exitBtnObj != null)
        {
            Button exitBtn = exitBtnObj.GetComponent<Button>();
            if (exitBtn != null)
            {
                exitBtn.onClick.RemoveAllListeners();
                exitBtn.onClick.AddListener(OnClickExitGame);
            }
        }

        // 3. 볼륨 설정 불러오기 및 리스너 등록
        float vol = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        AudioListener.volume = vol;
        if (volumeSlider != null)
        {
            volumeSlider.value = vol;
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // 4. AI 모델 설정 불러오기 및 리스너 등록
        int modelIdx = PlayerPrefs.GetInt("SelectedModelIndex", 0);
        if (modelDropdown != null)
        {
            modelDropdown.value = modelIdx;
            modelDropdown.onValueChanged.RemoveAllListeners();
            modelDropdown.onValueChanged.AddListener(OnModelChanged);
        }

        // 5. 초기 상태 가드
        if (settingsPanelGroup != null) DisablePanel(settingsPanelGroup);
        if (buttonPanelGroup != null) EnablePanel(buttonPanelGroup);
    }

    public void OnClickExitGame()
    {
        Debug.Log("[TitleUI] 게임을 종료합니다.");
        Application.Quit();
    }

    // ---------------- [설정 창 제어 함수] ----------------
    public void OnClickOpenSettings()
    {
        if (buttonPanelGroup != null) DisablePanel(buttonPanelGroup);
        if (settingsPanelGroup != null) EnablePanel(settingsPanelGroup);
    }

    public void OnClickCloseSettings()
    {
        if (settingsPanelGroup != null) DisablePanel(settingsPanelGroup);
        if (buttonPanelGroup != null) EnablePanel(buttonPanelGroup);
        SaveSettings();
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    private GameObject loadingPopupInstance;

    private void ShowLoadingPopup(string message)
    {
        if (loadingPopupInstance != null)
        {
            Destroy(loadingPopupInstance);
        }

        GameObject mainMenuUI = GameObject.Find("MainMenuUI");
        if (mainMenuUI == null) return;

        loadingPopupInstance = new GameObject("TempLoadingPopup");
        loadingPopupInstance.transform.SetParent(mainMenuUI.transform, false);

        RectTransform rectTrans = loadingPopupInstance.AddComponent<RectTransform>();
        rectTrans.anchorMin = Vector2.zero;
        rectTrans.anchorMax = Vector2.one;
        rectTrans.sizeDelta = Vector2.zero;
        rectTrans.anchoredPosition = Vector2.zero;

        Image bgImg = loadingPopupInstance.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.75f);

        CanvasGroup group = loadingPopupInstance.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(loadingPopupInstance.transform, false);
        
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = message;
        tmpText.fontSize = 24;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;

        if (modelDropdown != null && modelDropdown.captionText != null)
        {
            tmpText.font = modelDropdown.captionText.font;
        }

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
    }

    private void HideLoadingPopup()
    {
        if (loadingPopupInstance != null)
        {
            Destroy(loadingPopupInstance);
            loadingPopupInstance = null;
        }
    }

    private async void OnModelChanged(int index)
    {
        if (LLMManager.Instance != null)
        {
            if (LLMManager.Instance.qualityIndex == index)
            {
                var weightsField = typeof(LLMManager).GetField("_weights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var weights = weightsField?.GetValue(LLMManager.Instance);
                if (weights != null)
                {
                    return;
                }
            }

            ShowLoadingPopup("AI 모델을 로드하고 확인하는 중입니다...");
            
            await System.Threading.Tasks.Task.Delay(100);
            
            LLMManager.Instance.qualityIndex = index;
            await LLMManager.Instance.LoadModelAsync();
            
            HideLoadingPopup();
        }
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", volumeSlider != null ? volumeSlider.value : 1.0f);
        PlayerPrefs.SetInt("SelectedModelIndex", modelDropdown != null ? modelDropdown.value : 0);
        PlayerPrefs.Save();
        Debug.Log("[TitleUI] 설정 데이터 저장 완료!");
    }

    // ---------------- [버튼 클릭 이벤트] ----------------
    public void OnClickStartGame() // 메인메뉴의 '게임 시작' 버튼
    {
        CoreSystemManager.Instance.ChangeState(GameState.ScenarioSelect);
    }

    public void OnClickStartInvestigation() // 시나리오 선택창의 '추리 시작' 버튼
    {
        Debug.Log("[TitleUI] 사건 수사를 시작합니다! 메인 플레이 상태로 전환.");
        // 여기서 실제로 게임 루프(MainPlay)로 넘어갑니다.
        CoreSystemManager.Instance.ChangeState(GameState.Briefing);
    }

    public void OnClickBackToMain()
    {
        CoreSystemManager.Instance.ChangeState(GameState.MainMenu);
    }
}