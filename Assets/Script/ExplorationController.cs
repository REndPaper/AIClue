using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ExplorationController : MonoBehaviour
{
    public Camera mainCam;
    public LayerMask roomLayer;

    // ★ MainUIController를 캐싱할 변수
    private MainUIController _mainUI;

    private void Start()
    {
        if (mainCam == null) mainCam = Camera.main;

        // ★ Start에서 딱 한 번만 찾아서 연결해둡니다. (성능 저하 없음, 싱글톤 불필요!)
        _mainUI = FindObjectOfType<MainUIController>();
        if (_mainUI == null)
        {
            Debug.LogError("[Exploration] 씬에 MainUIController가 없습니다!");
        }
    }

    private void Update()
    {
        if (_mainUI == null || _mainUI.currentMode != MainUIController.InvestigationMode.Exploration)
            return;

        // =================================================================
        // ★ 수정됨: Input.GetMouseButtonDown(0) 대신 새로운 마우스 감지 방식 사용
        // =================================================================
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // UI를 클릭한 거면 무시 (EventSystem은 구형/신형 알아서 호환됨)
            if (EventSystem.current.IsPointerOverGameObject()) return;

            ExecuteRaycast();
        }
    }

    private void ExecuteRaycast()
    {
        // ★ 수정됨: Input.mousePosition 대신 Mouse.current.position.ReadValue() 사용
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, roomLayer))
        {
            RoomObject room = hit.collider.GetComponent<RoomObject>();
            if (room != null)
            {
                if (room.roomIndex == 4)
                {
                    Debug.Log("<color=red>[Exploration]</color> 여기는 사건 현장입니다.");
                    return;
                }

                if (room.isSearched)
                {
                    Debug.Log("[Exploration] 이미 샅샅이 뒤져본 방입니다.");
                    return;
                }

                SearchRoom(room);
            }
        }
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
            Debug.Log("[Exploration] 이 방에는 특이점이 없습니다.");
        }
    }
}