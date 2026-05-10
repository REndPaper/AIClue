using System.Collections;
using UnityEngine;

public class UIFlowManager : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject splashPanel;
    public GameObject titlePanel;
    public GameObject briefingPanel;
    public GameObject resultPanel;

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

    public void ShowSplash()
    {
        HideAllMainPanels();
        CloseAllPopups();
        SetActiveSafe(splashPanel, true);
    }

    public void ShowTitle()
    {
        HideAllMainPanels();
        CloseAllPopups();
        SetActiveSafe(titlePanel, true);
    }

    public void ShowBriefing()
    {
        HideAllMainPanels();
        CloseAllPopups();
        SetActiveSafe(briefingPanel, true);
    }

    public void ShowResult()
    {
        HideAllMainPanels();
        CloseAllPopups();
        SetActiveSafe(resultPanel, true);
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

    public void GoToMainPlay()
    {
        Debug.Log("메인 플레이 화면으로 이동 예정");

        // 나중에 실제 메인 플레이 씬이 생기면 여기서 씬 전환 코드 넣기
     
    }

    public void OpenStatistics()
    {
        Debug.Log("통계 화면 열기");

        // 통계 화면이 아직 없으면 임시로 결과 화면
        ShowResult();
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