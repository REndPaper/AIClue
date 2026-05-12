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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 테스트용 (나중에 TitleUIController나 GameManager에서 호출)
        // SetupGame("Scenario/CASE_001.json"); 
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

        // ★ 4. 단서 8개 추출 (무기 단서는 .Take(3)으로 JSON에 5개가 있어도 3개만 안전하게 뽑음)
        List<EvidenceData> selectedEvidences = new List<EvidenceData>();
        selectedEvidences.AddRange(trueWeapon.evidences.OrderBy(x => rng.Next()).Take(3));
        selectedEvidences.AddRange(falseWeapon.evidences.OrderBy(x => rng.Next()).Take(3));
        selectedEvidences.Add(trueCulprit.breakerEvidence);
        selectedEvidences.Add(falseCulprit.breakerEvidence);
        // ※ innocentSuspect의 증거는 뽑지 않음!

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

    // ScenarioManager.cs 내부에 추가
    public EvidenceData GetEvidenceByRoomIndex(int index)
    {
        // 4번 방(중앙)이거나 범위를 벗어나면 아무것도 주지 않음
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

    /// <summary>
    /// UI에서 용의자 교체 버튼을 누를 때, 랜덤하게 섞인 배열에서 캐릭터를 꺼내줍니다.
    /// </summary>
    public CharacterData GetCharacterDataByIndex(int index)
    {
        if (index >= 0 && index < activeSuspects.Length)
        {
            return activeSuspects[index];
        }
        return activeSuspects[0];
    }
}