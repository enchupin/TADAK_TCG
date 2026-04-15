using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BuffMetadataJsonData
{
    public int buffId;
    public string name;
    public string description;
    public string enName;
    public string enDescription;
    public string jaName;
    public string jaDescription;
    public string zhHantName;
    public string zhHantDescription;
    public string zhHansName;
    public string zhHansDescription;
}

public static class BuffMetadataJsonParser
{
    private const string BuffResourcePath = "JsonData/buffs";

    public static bool TryLoad(out List<BuffMetadataJsonData> buffs)
    {
        buffs = new List<BuffMetadataJsonData>();

        TextAsset jsonFile = Resources.Load<TextAsset>(BuffResourcePath);
        if (jsonFile == null)
        {
            Debug.LogError("[BuffMetadataJsonParser] buffs.json을 불러오지 못했습니다");
            return false;
        }

        BuffMetadataJsonRoot buffList;
        try
        {
            buffList = JsonUtility.FromJson<BuffMetadataJsonRoot>(jsonFile.text);
        }
        catch
        {
            Debug.LogError("[BuffMetadataJsonParser] buffs.json 파싱에 실패했습니다");
            return false;
        }

        if (buffList?.buffs == null)
        {
            Debug.LogError("[BuffMetadataJsonParser] buffs.json 형식이 올바르지 않습니다");
            return false;
        }

        buffs = buffList.buffs;
        return true;
    }

    [Serializable]
    private sealed class BuffMetadataJsonRoot
    {
        public List<BuffMetadataJsonData> buffs = new List<BuffMetadataJsonData>();
    }
}
