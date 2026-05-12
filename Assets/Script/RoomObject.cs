using UnityEngine;

public class RoomObject : MonoBehaviour
{
    [Tooltip("0~8 사이의 방 번호")]
    public int roomIndex;

    public bool isSearched = false;

    // 셰이더의 프로퍼티 이름 (코드에 있던 그대로!)
    private readonly int gridColorId = Shader.PropertyToID("_Color");

    private Renderer _meshRenderer;
    private Material _instancedMaterial;

    private void Awake()
    {
        _meshRenderer = GetComponent<Renderer>();
        if(roomIndex == 4){
            isSearched = true;
        }

        // ★ 중요: Material을 복제해서 사용 (안 그러면 모든 방 색깔이 같이 변함)
        if (_meshRenderer != null)
        {
            _instancedMaterial = _meshRenderer.material;
        }
    }

    /// <summary>
    /// 수색 결과에 따라 방의 와이어프레임 색상을 바꿉니다.
    /// </summary>
    public void MarkAsSearched(bool foundEvidence)
    {
        isSearched = true;

        if (_instancedMaterial != null)
        {
            // 단서를 찾았으면 형광 초록색, 꽝이면 칙칙한 회색/빨간색 등 원하는 색으로!
            Color targetColor = foundEvidence ? Color.green : Color.gray;

            // 셰이더의 _Color 속성에 새로운 색상을 덮어씌움
            _instancedMaterial.SetColor(gridColorId, targetColor);
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지 (복제된 메테리얼 삭제)
        if (_instancedMaterial != null) Destroy(_instancedMaterial);
    }
}