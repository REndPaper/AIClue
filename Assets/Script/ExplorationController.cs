using UnityEngine;

public class ExplorationController : MonoBehaviour
{
    private MainUIController _mainUI;

    private void Start()
    {
        _mainUI = FindObjectOfType<MainUIController>();
        if (_mainUI == null)
        {
            Debug.LogError("[Exploration] MainUIController를 찾을 수 없습니다!");
        }
    }

    public void OnClickRoom(RoomObject room)
    {
        if (room == null) return;
        if (_mainUI == null || _mainUI.currentMode != MainUIController.InvestigationMode.Exploration)
            return;

        if (room.roomIndex == 4)
        {
            Debug.Log("[Exploration] 중앙의 취조실(심문 모드) 구역입니다.");
            return;
        }

        if (room.isSearched)
        {
            Debug.Log("[Exploration] 이미 수색 완료된 방입니다.");
            return;
        }

        SearchRoom(room);
    }

    private void SearchRoom(RoomObject room)
    {
        EvidenceData foundEvidence = ScenarioManager.Instance.GetEvidenceByRoomIndex(room.roomIndex);

        if (foundEvidence != null)
        {
            room.MarkAsSearched(true);
            GlobalEventManager.Publish(GameEventType.EvidenceFound, foundEvidence);
        }
        else
        {
            room.MarkAsSearched(false);
            Debug.Log("[Exploration] 이 방에서는 특별한 증거를 찾을 수 없습니다.");
        }
    }
}