using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    [Header("현재 게임 세팅 데이터")]
    public ScenarioData currentScenario;

    // 등장인물 3인방
    public CharacterData trueCulprit;      // 진범 (증거 스폰 O)
    public CharacterData falseCulprit;     // 짭 범인 (미끼, 증거 스폰 O)
    public CharacterData innocentSuspect;  // 억울한 시민 (증거 스폰 X, 완전 결백)

    [Header("UI 표시용 용의자 리스트 (순서 랜덤)")]
    public CharacterData[] activeSuspects = new CharacterData[3];

    // 흉기 2종
    public WeaponData trueWeapon;          // 진짜 흉기 (증거 3개 스폰)
    public WeaponData falseWeapon;         // 짭 흉기 (미끼, 증거 3개 스폰)

    [Header("맵 데이터 (인덱스 0~8)")]
    public string[] roomNames = new string[9];       // UI 버튼에 이름 뿌려줄 때 사용할 1차원 배열
    public EvidenceData[] gridRooms = new EvidenceData[9]; // 방에 숨겨진 실제 단서 데이터

    [Header("최근 결과 데이터 캐시 (씬 전환용)")]
    public ResultData latestResultData;

    // 시나리오 관련 동적 런타임 스프라이트 캐시
    private Dictionary<string, Sprite> _cachedSuspectSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> _cachedRoomSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> _cachedWeaponSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, Sprite> _cachedEvidenceSprites = new Dictionary<string, Sprite>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void SetupGame(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[ScenarioManager] 시나리오 파일을 찾을 수 없습니다: {filePath}");
            return;
        }

        string jsonText = File.ReadAllText(filePath);
        currentScenario = JsonConvert.DeserializeObject<ScenarioData>(jsonText);

        // 시나리오가 속한 폴더 경로 추출 및 리소스 일괄 로드
        string scenarioDir = Path.GetDirectoryName(fileName);
        LoadScenarioResources(scenarioDir);

        System.Random rng = new System.Random();

        // ★ 1. 맵 이름 평탄화 (2차원 리스트 -> 1차원 배열)
        int roomIndex = 0;
        foreach (var row in currentScenario.gridMap)
        {
            foreach (var roomName in row)
            {
                roomNames[roomIndex++] = roomName;
            }
        }

        // ★ 2. 캐릭터 셔플 후 3명 뽑기 (역할 분담)
        var shuffledChars = currentScenario.characters.OrderBy(x => rng.Next()).ToList();
        trueCulprit = shuffledChars[0];     // 진범
        falseCulprit = shuffledChars[1];    // 미끼 용의자
        innocentSuspect = shuffledChars[2]; // 완전 결백한 용의자

        // ★ 3. 흉기 셔플 후 2개 뽑기
        var shuffledWeapons = currentScenario.weapons.OrderBy(x => rng.Next()).ToList();
        trueWeapon = shuffledWeapons[0];
        falseWeapon = shuffledWeapons[1];

        // ★ 4. 단서 8개 추출
        List<EvidenceData> selectedEvidences = new List<EvidenceData>();
        selectedEvidences.AddRange(trueWeapon.evidences.OrderBy(x => rng.Next()).Take(3));
        selectedEvidences.AddRange(falseWeapon.evidences.OrderBy(x => rng.Next()).Take(3));
        selectedEvidences.Add(trueCulprit.breakerEvidence);
        selectedEvidences.Add(falseCulprit.breakerEvidence);

        // ★ 5. 단서 셔플 후 맵(배열)에 뿌리기
        selectedEvidences = selectedEvidences.OrderBy(x => rng.Next()).ToList();
        int evidenceIndex = 0;
        for (int i = 0; i < 9; i++)
        {
            if (i == 4)
            {
                gridRooms[i] = null; // 중앙 시체방 (단서 없음)
                continue;
            }
            gridRooms[i] = selectedEvidences[evidenceIndex++];
        }

        // ★ 6. 유저에게 보여줄 용의자 순서 무작위 섞기
        activeSuspects[0] = trueCulprit;
        activeSuspects[1] = falseCulprit;
        activeSuspects[2] = innocentSuspect;
        activeSuspects = activeSuspects.OrderBy(x => rng.Next()).ToArray();

        PrintResultLog();
    }

    public EvidenceData GetEvidenceByRoomIndex(int index)
    {
        if (index == 4 || index < 0 || index >= gridRooms.Length)
        {
            return null;
        }
        return gridRooms[index];
    }

    private void PrintResultLog()
    {
        Debug.Log("<color=#FFFF00><b>[이번 판 정답지 & 등장인물 스포일러]</b></color>");
        Debug.Log($"💀 [진범]: {trueCulprit.name} / 🔪 진흉기: {trueWeapon.name}");
        Debug.Log($"🤡 [짭범인]: {falseCulprit.name} / 🪓 짭흉기: {falseWeapon.name}");
        Debug.Log($"😇 [억울한 시민]: {innocentSuspect.name}");
        Debug.Log("----------------------------------------");

        for (int i = 0; i < 9; i++)
        {
            if (i == 4)
                Debug.Log($"방[{i}] ({roomNames[i]}): 🩸 사건 현장 (단서 없음)");
            else
                Debug.Log($"방[{i}] ({roomNames[i]}): 🔍 {gridRooms[i].name} <color=#AAAAAA>({gridRooms[i].description})</color>");
        }
    }

    public CharacterData GetCharacterDataByIndex(int index)
    {
        if (index >= 0 && index < activeSuspects.Length)
        {
            return activeSuspects[index];
        }
        return activeSuspects[0];
    }

    public Sprite GetSuspectSprite(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return null;
        string key = characterId.ToLower().Trim();
        return _cachedSuspectSprites.TryGetValue(key, out Sprite sprite) ? sprite : null;
    }

    public Sprite GetRoomSprite(string roomName)
    {
        if (string.IsNullOrEmpty(roomName)) return null;
        string key = roomName.Trim();
        return _cachedRoomSprites.TryGetValue(key, out Sprite sprite) ? sprite : null;
    }

    public Sprite GetWeaponSprite(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId)) return null;
        string key = weaponId.ToLower().Trim();
        return _cachedWeaponSprites.TryGetValue(key, out Sprite sprite) ? sprite : null;
    }

    public Sprite GetEvidenceSprite(string evidenceId)
    {
        if (string.IsNullOrEmpty(evidenceId)) return null;
        string key = evidenceId.ToLower().Trim();

        // 1. 중요 단서 캐시 검사
        if (_cachedEvidenceSprites.TryGetValue(key, out Sprite customSprite))
        {
            return customSprite;
        }

        // 2. 범용 단서 매핑 규칙
        string category = "document";
        if (key.StartsWith("ev_02")) category = "liquid";
        else if (key.StartsWith("ev_05") || key.StartsWith("ev_07")) category = "gear";
        else if (key.StartsWith("ev_01") || key.StartsWith("ev_03") || key.StartsWith("ev_08")) category = "debris";
        else if (key.StartsWith("ev_04") || key.StartsWith("ev_06") || key.StartsWith("ev_09")) category = "trace";

        // 3. 에셋 Resources 로드 시도
        string resourcePath = "Evidences/evidence_" + category;
        Sprite resSprite = Resources.Load<Sprite>(resourcePath);
        if (resSprite != null) return resSprite;

        // 4. 최종 폴백: 절차적 텍스처 생성 (Resources 파일이 부재할 때 가드)
        return CreateProceduralEvidenceSprite(category);
    }

    private Sprite CreateProceduralEvidenceSprite(string category)
    {
        string cacheKey = "procedural_" + category;
        if (_cachedEvidenceSprites.TryGetValue(cacheKey, out Sprite cached)) return cached;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        Color paperBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        Color strokeColor = new Color(0.85f, 0.7f, 0.3f, 1f);
        Color accentColor = Color.white;

        if (category == "liquid")
        {
            accentColor = new Color(0.2f, 0.8f, 0.9f, 1f);
        }
        else if (category == "trace")
        {
            accentColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;
                pixels[idx] = paperBg;

                if (category == "document")
                {
                    if (x > 25 && x < 103 && y > 25 && y < 103)
                    {
                        pixels[idx] = new Color(0.25f, 0.25f, 0.25f, 1f);
                        if ((y == 40 || y == 55 || y == 70 || y == 85) && x > 35 && x < 93)
                        {
                            pixels[idx] = strokeColor;
                        }
                    }
                }
                else if (category == "liquid")
                {
                    if (x > 55 && x < 73 && y > 65 && y < 95) pixels[idx] = strokeColor;
                    float bodyWidth = (95 - y) * 0.75f;
                    if (y >= 30 && y <= 65 && Mathf.Abs(x - 64) < bodyWidth)
                    {
                        pixels[idx] = strokeColor;
                        if (y < 45) pixels[idx] = accentColor;
                    }
                }
                else if (category == "trace")
                {
                    float dx = x - 60;
                    float dy = y - 68;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > 18 && dist < 22) pixels[idx] = strokeColor;
                    else if (dist <= 18) pixels[idx] = new Color(0.25f, 0.25f, 0.25f, 1f);
                    if (x == y - 8 && x > 25 && x < 45) pixels[idx] = strokeColor;
                }
                else if (category == "gear")
                {
                    float dx = x - 64;
                    float dy = y - 64;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > 12 && dist < 22) pixels[idx] = strokeColor;
                    if (dist <= 5) pixels[idx] = strokeColor;
                    if (dist >= 22 && dist <= 28 && (Mathf.Abs(dx) < 4 || Mathf.Abs(dy) < 4)) pixels[idx] = strokeColor;
                }
                else
                {
                    if (Mathf.Abs(x - 64) + Mathf.Abs(y - 64) < 28)
                    {
                        pixels[idx] = new Color(0.35f, 0.35f, 0.35f, 1f);
                        if (x == 64 || y == 64 || x + y == 128) pixels[idx] = strokeColor;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        _cachedEvidenceSprites[cacheKey] = sprite;
        return sprite;
    }

    private void LoadScenarioResources(string scenarioDir)
    {
        _cachedSuspectSprites.Clear();
        _cachedRoomSprites.Clear();
        _cachedWeaponSprites.Clear();
        _cachedEvidenceSprites.Clear();

        string baseDir = Path.Combine(Application.streamingAssetsPath, scenarioDir);

        // 1. 용의자 초상화 로드
        string suspectPath = Path.Combine(baseDir, "img_suspects");
        if (Directory.Exists(suspectPath))
        {
            foreach (var file in Directory.GetFiles(suspectPath, "*.png"))
            {
                Sprite sp = LoadSprite(file);
                if (sp != null)
                {
                    string key = Path.GetFileNameWithoutExtension(file).ToLower().Trim();
                    _cachedSuspectSprites[key] = sp;
                }
            }
        }

        // 2. 방 아이콘 로드
        string roomPath = Path.Combine(baseDir, "img_rooms");
        if (Directory.Exists(roomPath))
        {
            foreach (var file in Directory.GetFiles(roomPath, "*.png"))
            {
                Sprite sp = LoadSprite(file);
                if (sp != null)
                {
                    string key = Path.GetFileNameWithoutExtension(file).Trim();
                    _cachedRoomSprites[key] = sp;
                }
            }
        }

        // 3. 무기/단서 로드
        string weaponPath = Path.Combine(baseDir, "img_weapons");
        if (Directory.Exists(weaponPath))
        {
            foreach (var file in Directory.GetFiles(weaponPath, "*.png"))
            {
                Sprite sp = LoadSprite(file);
                if (sp != null)
                {
                    string key = Path.GetFileNameWithoutExtension(file).ToLower().Trim();
                    _cachedWeaponSprites[key] = sp;
                }
            }
        }

        // 4. 중요 단서 로드
        string evidencePath = Path.Combine(baseDir, "img_evidences");
        if (Directory.Exists(evidencePath))
        {
            foreach (var file in Directory.GetFiles(evidencePath, "*.png"))
            {
                Sprite sp = LoadSprite(file);
                if (sp != null)
                {
                    string key = Path.GetFileNameWithoutExtension(file).ToLower().Trim();
                    _cachedEvidenceSprites[key] = sp;
                }
            }
        }

        Debug.Log($"[ScenarioManager] 리소스 동적 로드 완료: 용의자 {_cachedSuspectSprites.Count}개, 방 {_cachedRoomSprites.Count}개, 무기 {_cachedWeaponSprites.Count}개, 중요단서 {_cachedEvidenceSprites.Count}개");
    }

    private Sprite LoadSprite(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return sprite;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScenarioManager] 스프라이트 로드 실패 ({filePath}): {e.Message}");
            return null;
        }
    }
}

public class ResultData
{
    public JudgmentSystem.ReportCard report;
    public CharacterData selectedSuspect;
    public WeaponData selectedWeapon;
    public int finalSearchCount;
    public int finalChatCount;
    public string finalChatLog;
    public string professorComment;
}