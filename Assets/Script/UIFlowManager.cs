using System.Collections;
using UnityEngine;

public class UIFlowManager : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject splashPanel;
    public GameObject titlePanel;
    public GameObject briefingPanel;
    public GameObject resultPanel;
    public GameObject mainPlayPanel;

    [Header("Main Play HUD Panels")]
    public GameObject chatPanel;
    public GameObject clueInventoryPanel;

    [Header("Common Popups")]
    public GameObject settingPopup;
    public GameObject loadingPopup;
    public GameObject errorPopup;
    public GameObject exitConfirmPopup;
    public GameObject saveConfirmPopup;
    public GameObject saveFailPopup;

    [Header("Splash Settings")]
    public float splashTime = 2.5f;

    private void Start()
    {
        CloseAllPopups();
        CloseHudPanels();
        ShowSplash();
        StartCoroutine(SplashToTitle());
    }

    private IEnumerator SplashToTitle()
    {
        yield return new WaitForSeconds(splashTime);
        ShowTitle();
    }

    private void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void HideAllMainPanels()
    {
        SetActiveSafe(splashPanel, false);
        SetActiveSafe(titlePanel, false);
        SetActiveSafe(briefingPanel, false);
        SetActiveSafe(resultPanel, false);
        SetActiveSafe(mainPlayPanel, false);
    }

    public void CloseAllPopups()
    {
        SetActiveSafe(settingPopup, false);
        SetActiveSafe(loadingPopup, false);
        SetActiveSafe(errorPopup, false);
        SetActiveSafe(exitConfirmPopup, false);
        SetActiveSafe(saveConfirmPopup, false);
        SetActiveSafe(saveFailPopup, false);
    }

    public void CloseHudPanels()
    {
        SetActiveSafe(chatPanel, false);
        SetActiveSafe(clueInventoryPanel, false);
    }

    public void ShowSplash()
    {
        HideAllMainPanels();
        CloseAllPopups();
        CloseHudPanels();
        SetActiveSafe(splashPanel, true);
    }

    public void ShowTitle()
    {
        HideAllMainPanels();
        CloseAllPopups();
        CloseHudPanels();
        SetActiveSafe(titlePanel, true);
    }

    public void ShowBriefing()
    {
        HideAllMainPanels();
        CloseAllPopups();
        CloseHudPanels();
        SetActiveSafe(briefingPanel, true);
    }

    public void ShowResult()
    {
        HideAllMainPanels();
        CloseAllPopups();
        CloseHudPanels();
        SetActiveSafe(resultPanel, true);
    }

    public void GoToMainPlay()
    {
        HideAllMainPanels();
        CloseAllPopups();
        CloseHudPanels();

        SetActiveSafe(mainPlayPanel, true);
        Debug.Log("메인 플레이 화면으로 이동");
    }

    public void OpenChatPanel()
    {
        SetActiveSafe(clueInventoryPanel, false);
        SetActiveSafe(chatPanel, true);
    }

    public void CloseChatPanel()
    {
        SetActiveSafe(chatPanel, false);
    }

    public void OpenClueInventoryPanel()
    {
        SetActiveSafe(chatPanel, false);
        SetActiveSafe(clueInventoryPanel, true);
    }

    public void CloseClueInventoryPanel()
    {
        SetActiveSafe(clueInventoryPanel, false);
    }

    public void OpenSettingPopup()
    {
        CloseAllPopups();
        SetActiveSafe(settingPopup, true);
    }

    public void OpenLoadingPopup()
    {
        CloseAllPopups();
        SetActiveSafe(loadingPopup, true);
    }

    public void OpenErrorPopup()
    {
        CloseAllPopups();
        SetActiveSafe(errorPopup, true);
    }

    public void OpenExitConfirmPopup()
    {
        CloseAllPopups();
        SetActiveSafe(exitConfirmPopup, true);
    }

    public void OpenSaveConfirmPopup()
    {
        CloseAllPopups();
        SetActiveSafe(saveConfirmPopup, true);
    }

    public void OpenSaveFailPopup()
    {
        CloseAllPopups();
        SetActiveSafe(saveFailPopup, true);
    }

    public void ClosePopup()
    {
        CloseAllPopups();
    }

    public void SaveSettings()
    {
        Debug.Log("설정 저장");
        ClosePopup();
    }

    public void ConfirmSave()
    {
        Debug.Log("저장 확인");
        ClosePopup();
    }

    public void RetrySave()
    {
        Debug.Log("저장 다시 시도");
        ClosePopup();
    }

    public void OpenStatistics()
    {
        Debug.Log("통계 화면 열기");
        ShowResult();
    }

    public void SelectRoom(string roomName)
    {
        Debug.Log(roomName + " 선택됨");
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}