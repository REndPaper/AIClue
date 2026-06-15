using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class RoomObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("0~8 번 방의 고유 번호")]
    public int roomIndex;

    public bool isSearched = false;

    [Header("사운드 연출 (선택사항)")]
    public AudioClip searchSuccessSound;
    private AudioSource _audioSource;

    private Image _buttonImage;
    private Outline _outline;
    private Coroutine _blinkCoroutine;

    // 돋보기 커서 캐싱
    private static Texture2D _magnifierCursor;
    private static readonly Vector2 CursorHotspot = new Vector2(10, 22); // 돋보기 렌즈 센터 부근

    private void Awake()
    {
        _buttonImage = GetComponent<Image>();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (roomIndex == 4)
        {
            isSearched = true;
        }
    }

    private void Start()
    {
        // 1. 방 이름 텍스트 설정 (시나리오 매니저 데이터 연동)
        if (ScenarioManager.Instance != null && ScenarioManager.Instance.roomNames != null &&
            roomIndex >= 0 && roomIndex < ScenarioManager.Instance.roomNames.Length)
        {
            string roomName = ScenarioManager.Instance.roomNames[roomIndex];
            
            // 중앙 구역(취조실) 예외 처리
            if (roomIndex == 4)
            {
                SetRoomText(roomName, "🚨");
            }
            else
            {
                SetRoomText(roomName, "?");
            }
        }
    }

    public void MarkAsSearched(bool foundEvidence)
    {
        isSearched = true;

        string roomName = "";
        if (ScenarioManager.Instance != null && ScenarioManager.Instance.roomNames != null &&
            roomIndex >= 0 && roomIndex < ScenarioManager.Instance.roomNames.Length)
        {
            roomName = ScenarioManager.Instance.roomNames[roomIndex];
        }

        Sprite roomSprite = string.IsNullOrEmpty(roomName) ? null : ScenarioManager.Instance.GetRoomSprite(roomName);

        if (_buttonImage != null)
        {
            if (roomSprite != null)
            {
                _buttonImage.sprite = roomSprite;
                _buttonImage.color = foundEvidence ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            }
            else
            {
                // 단서를 찾았으면 초록색, 허탕이면 어두운 회색으로 바꿉니다.
                Color targetColor = foundEvidence ? new Color(0.2f, 0.8f, 0.2f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
                _buttonImage.color = targetColor;
            }
        }

        // 텍스트 이모지 갱신
        if (!string.IsNullOrEmpty(roomName))
        {
            if (roomSprite != null)
            {
                SetRoomText(roomName, "");
            }
            else
            {
                string emoji = GetRoomEmoji(roomName, foundEvidence);
                SetRoomText(roomName, emoji);
            }
        }

        if (foundEvidence)
        {
            // 골드 스파클링 이펙트 트리거
            PlayGoldSparkleEffect();

            // 효과음 재생
            if (searchSuccessSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(searchSuccessSound);
            }
        }
    }

    private void SetRoomText(string roomName, string iconEmoji)
    {
        var txt = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (txt != null)
        {
            if (string.IsNullOrEmpty(iconEmoji))
            {
                txt.text = roomName;
            }
            else
            {
                txt.text = $"<size=140%>{iconEmoji}</size>\n{roomName}";
            }
        }
    }

    private string GetRoomEmoji(string roomName, bool foundEvidence)
    {
        if (!foundEvidence) return "❌";

        if (roomName.Contains("도서관") || roomName.Contains("서재")) return "📚";
        if (roomName.Contains("주방") || roomName.Contains("부엌")) return "🍳";
        if (roomName.Contains("화장실") || roomName.Contains("욕실")) return "🚽";
        if (roomName.Contains("침실") || roomName.Contains("방")) return "🛏️";
        if (roomName.Contains("거실")) return "🛋️";
        if (roomName.Contains("복도") || roomName.Contains("계단")) return "🚶";
        if (roomName.Contains("정원") || roomName.Contains("마당")) return "🌳";
        if (roomName.Contains("창고") || roomName.Contains("차고")) return "📦";
        if (roomName.Contains("식당") || roomName.Contains("다이닝")) return "🍽️";

        return "🔍";
    }

    // 마우스 호버 시작
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSearched && roomIndex != 4) return; // 이미 조사했거나 중앙구역은 호버 없음
        if (roomIndex == 4) return;

        SetMagnifierCursor(true);

        if (_outline == null)
        {
            _outline = gameObject.GetComponent<Outline>();
            if (_outline == null)
            {
                _outline = gameObject.AddComponent<Outline>();
                _outline.effectDistance = new Vector2(4f, 4f);
            }
        }
        _outline.enabled = true;
        _outline.effectColor = new Color(1f, 0.9f, 0.2f, 0f);

        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = StartCoroutine(BlinkOutlineRoutine());
    }

    // 마우스 호버 종료
    public void OnPointerExit(PointerEventData eventData)
    {
        SetMagnifierCursor(false);

        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        if (_outline != null)
        {
            _outline.enabled = false;
        }
    }

    private IEnumerator BlinkOutlineRoutine()
    {
        float speed = 4f;
        while (true)
        {
            float alpha = (Mathf.Sin(Time.time * speed) + 1f) * 0.4f + 0.2f;
            if (_outline != null)
            {
                _outline.effectColor = new Color(1f, 0.9f, 0.2f, alpha);
            }
            yield return null;
        }
    }

    private void SetMagnifierCursor(bool showMagnifier)
    {
        if (showMagnifier)
        {
            if (_magnifierCursor == null)
            {
                _magnifierCursor = CreateMagnifierTexture();
            }
            Cursor.SetCursor(_magnifierCursor, CursorHotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    private Texture2D CreateMagnifierTexture()
    {
        Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        
        // 투명하게 청소
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                tex.SetPixel(x, y, Color.clear);
            }
        }

        // 돋보기 픽셀 아트 드로잉
        Color black = Color.black;
        Color white = new Color(1f, 1f, 1f, 0.5f);

        int cx = 10, cy = 22;
        int r = 6;
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d < r)
                {
                    tex.SetPixel(x, y, white);
                }
                if (Mathf.Abs(d - r) < 0.8f)
                {
                    tex.SetPixel(x, y, black);
                }
            }
        }

        for (int i = 0; i < 15; i++)
        {
            tex.SetPixel(cx + 4 + i, cy - 4 - i, black);
            tex.SetPixel(cx + 5 + i, cy - 4 - i, black);
        }

        tex.Apply();
        return tex;
    }

    private void PlayGoldSparkleEffect()
    {
        for (int i = 0; i < 12; i++)
        {
            GameObject particleObj = new GameObject("SparklePiece");
            particleObj.transform.SetParent(transform.parent, false);
            particleObj.transform.position = transform.position;

            var img = particleObj.AddComponent<Image>();
            img.color = new Color(1f, 0.85f, 0.2f, 1f);

            var rect = particleObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Random.Range(8f, 16f), Random.Range(8f, 16f));

            var sparkle = particleObj.AddComponent<UISparklePiece>();
            sparkle.Init(Random.insideUnitCircle.normalized * Random.Range(150f, 350f));
        }
    }
}

public class UISparklePiece : MonoBehaviour
{
    private Vector2 _velocity;
    private float _gravity = -450f;
    private float _lifeTime = 1f;
    private float _elapsed = 0f;
    private Image _image;
    private RectTransform _rect;

    public void Init(Vector2 initialVelocity)
    {
        _velocity = initialVelocity;
        _image = GetComponent<Image>();
        _rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        float t = _elapsed / _lifeTime;
        _velocity.y += _gravity * Time.deltaTime;
        _rect.anchoredPosition += _velocity * Time.deltaTime;

        _rect.localScale = Vector3.one * (1f - t);
        if (_image != null)
        {
            _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, 1f - t);
        }
    }
}