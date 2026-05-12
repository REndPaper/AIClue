using System.Collections.Generic;

[System.Serializable]
public class ScenarioData
{
    // ★ 새로 갱신된 JSON 포맷에 맞춘 변수들
    public string caseNo;
    public string caseName;
    public string overview;
    public string victim;
    public string place;
    public string objective;
    public List<List<string>> gridMap; // 3x3 맵 데이터

    public List<CharacterData> characters;
    public List<WeaponData> weapons;
}

[System.Serializable]
public class CharacterData
{
    public string id;
    public string name;
    public string personality;
    public string description;
    public string alibi; // 캐릭터가 앵무새처럼 반복할 알리바이
    public EvidenceData breakerEvidence; // 거짓말을 깨부술 결정적 단서
}

[System.Serializable]
public class WeaponData
{
    public string id;
    public string name;
    public List<EvidenceData> evidences;
}

[System.Serializable]
public class EvidenceData
{
    public string id;
    public string name;
    public string description;
}
