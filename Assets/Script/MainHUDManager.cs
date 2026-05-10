
// 심문 & 추리 둘 다 뜨는 경우

using UnityEngine;

public class MainHUDManager : MonoBehaviour
{
    [Header("HUD Panels")]
    public GameObject chatPanel;
    public GameObject clueInventoryPanel;

    private void Start()
    {
        CloseAllHudPanels();
    }

    private void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    public void OpenChatPanel()
    {
        SetActiveSafe(chatPanel, true);
        Debug.Log("심문/채팅창 열림");
    }

    public void CloseChatPanel()
    {
        SetActiveSafe(chatPanel, false);
        Debug.Log("심문/채팅창 닫힘");
    }

    public void OpenClueInventoryPanel()
    {
        SetActiveSafe(clueInventoryPanel, true);
        Debug.Log("사건 단서창 열림");
    }

    public void CloseClueInventoryPanel()
    {
        SetActiveSafe(clueInventoryPanel, false);
        Debug.Log("사건 단서창 닫힘");
    }

    public void CloseAllHudPanels()
    {
        SetActiveSafe(chatPanel, false);
        SetActiveSafe(clueInventoryPanel, false);
    }
}

/*
// 심문 & 추리 따로 뜨게 하는 경우

using UnityEngine;

public class MainHUDManager : MonoBehaviour
{
    [Header("HUD Panels")]
    public GameObject chatPanel;
    public GameObject clueInventoryPanel;

    private void Start()
    {
        CloseAllHudPanels();
    }

    private void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    public void OpenChatPanel()
    {
        SetActiveSafe(clueInventoryPanel, false);
        SetActiveSafe(chatPanel, true);
        Debug.Log("심문/채팅창 열림");
    }

    public void CloseChatPanel()
    {
        SetActiveSafe(chatPanel, false);
        Debug.Log("심문/채팅창 닫힘");
    }

    public void OpenClueInventoryPanel()
    {
        SetActiveSafe(chatPanel, false);
        SetActiveSafe(clueInventoryPanel, true);
        Debug.Log("사건 단서창 열림");
    }

    public void CloseClueInventoryPanel()
    {
        SetActiveSafe(clueInventoryPanel, false);
        Debug.Log("사건 단서창 닫힘");
    }

    public void CloseAllHudPanels()
    {
        SetActiveSafe(chatPanel, false);
        SetActiveSafe(clueInventoryPanel, false);
    }
}
*/