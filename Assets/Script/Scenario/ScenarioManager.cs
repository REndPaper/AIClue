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
    public CharacterData trueCulprit;     // 진범 (증거 스폰 O)
    public CharacterData falseCulprit;    // 짭 범인 (미끼, 증거 스폰 O)
    public CharacterData innocentSuspect; // 억울한 시민 (증거 스폰 X, 완전 결백)
    
    // 흉기 2종
    public WeaponData trueWeapon;         // 진짜 흉기 (증거 3개 스폰)
    public WeaponData falseWeapon;        // 짭 흉기 (미끼, 증거 3개 스폰)

    public EvidenceData[] gridRooms = new EvidenceData[9];

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
        SetupGame("Scenario/CASE_001.json"); 
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

        // ★ 캐릭터 셔플 후 3명 뽑기 (역할 분담)
        var shuffledChars = currentScenario.characters.OrderBy(x => rng.Next()).ToList();
        trueCulprit = shuffledChars[0];     // 진범
        falseCulprit = shuffledChars[1];    // 미끼 용의자
        innocentSuspect = shuffledChars[2]; // 완전 결백한 용의자

        // 흉기 셔플 후 2개 뽑기
        var shuffledWeapons = currentScenario.weapons.OrderBy(x => rng.Next()).ToList();
        trueWeapon = shuffledWeapons[0];
        falseWeapon = shuffledWeapons[1];

        // 단서 8개 추출
        List<EvidenceData> selectedEvidences = new List<EvidenceData>();
        selectedEvidences.AddRange(trueWeapon.evidences.OrderBy(x => rng.Next()).Take(3));
        selectedEvidences.AddRange(falseWeapon.evidences.OrderBy(x => rng.Next()).Take(3));
        selectedEvidences.Add(trueCulprit.breakerEvidence);
        selectedEvidences.Add(falseCulprit.breakerEvidence);
        // ※ innocentSuspect의 증거는 뽑지 않음!

        // 단서 셔플 후 맵(배열)에 뿌리기
        selectedEvidences = selectedEvidences.OrderBy(x => rng.Next()).ToList();
        int evidenceIndex = 0;
        for (int i = 0; i < 9; i++)
        {
            if (i == 4) 
            {
                gridRooms[i] = null; // 중앙 시체방
                continue;
            }
            gridRooms[i] = selectedEvidences[evidenceIndex++];
        }

        PrintResultLog();
    }

    private void PrintResultLog()
    {
        Debug.Log("<color=#FFFF00><b>[이번 판 정답지 & 등장인물 스포일러]</b></color>");
        Debug.Log($"💀 [진범]: {trueCulprit.name} (단서 스폰됨) / 🔪 진흉기: {trueWeapon.name}");
        Debug.Log($"🤡 [짭범인]: {falseCulprit.name} (단서 스폰됨) / 🪓 짭흉기: {falseWeapon.name}");
        Debug.Log($"😇 [억울한 시민]: {innocentSuspect.name} (단서 스폰 안됨, 완전 무죄!)");
        Debug.Log("----------------------------------------");

        for (int i = 0; i < 9; i++)
        {
            if (i == 4) 
                Debug.Log($"방[{i}] (중앙): 🩸 사건 현장 (피해자의 시신)");
            else 
                Debug.Log($"방[{i}]: 🔍 {gridRooms[i].name} <color=#AAAAAA>({gridRooms[i].description})</color>");
        }
    }
}