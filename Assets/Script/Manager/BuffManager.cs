using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    private Dictionary<int, BuffData> buffDatabase = new Dictionary<int, BuffData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadBuffData();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadBuffData()
    {
        buffDatabase.Clear();

        TextAsset jsonFile = Resources.Load<TextAsset>("JsonData/buffs");
        if (jsonFile == null)
        {
            Debug.LogError("[BuffManager] Failed to load JsonData/buffs.json.");
            return;
        }

        BuffList buffList = JsonUtility.FromJson<BuffList>(jsonFile.text);
        if (buffList?.buffs == null)
        {
            Debug.LogError("[BuffManager] Invalid buffs.json format.");
            return;
        }

        foreach (BuffData buff in buffList.buffs)
        {
            if (buff == null)
            {
                continue;
            }

            NormalizeBuffText(buff);
            if (buffDatabase.ContainsKey(buff.buffId))
            {
                Debug.LogWarning($"[BuffManager] Duplicate buffId detected: {buff.buffId}. Later entry ignored.");
                continue;
            }

            buffDatabase.Add(buff.buffId, buff);
        }

        Debug.Log($"[BuffManager] Loaded {buffDatabase.Count} buffs.");
    }

    public BuffData GetBuffData(int buffId)
    {
        if (buffDatabase.TryGetValue(buffId, out BuffData data))
        {
            return data;
        }
        Debug.LogWarning($"[BuffManager] Buff ID {buffId} not found.");
        return null;
    }

    public bool TryGetBuffData(int buffId, out BuffData data)
    {
        return buffDatabase.TryGetValue(buffId, out data);
    }

    public float GetIncomingDamageMultiplier(int buffId, float fallbackValue = 1f)
    {
        if (TryGetBuffData(buffId, out BuffData data) && data != null)
        {
            return data.GetIncomingDamageMultiplier(fallbackValue);
        }

        return fallbackValue;
    }

    public float GetOutgoingDamageMultiplier(int buffId, float fallbackValue = 1f)
    {
        if (TryGetBuffData(buffId, out BuffData data) && data != null)
        {
            return data.GetOutgoingDamageMultiplier(fallbackValue);
        }

        return fallbackValue;
    }

    public void RegisterRuntimeBuffData(BuffData data)
    {
        if (data == null || data.buffId <= 0)
        {
            return;
        }

        NormalizeBuffText(data);
        buffDatabase[data.buffId] = data;
    }

    private static void NormalizeBuffText(BuffData data)
    {
        if (data == null)
        {
            return;
        }

        data.name = RestoreKoreanText(data.name);
        data.description = RestoreKoreanText(data.description);
    }

    private static string RestoreKoreanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || ContainsHangul(text))
        {
            return text;
        }

        try
        {
            byte[] encodedBytes = Encoding.GetEncoding(949).GetBytes(text);
            string restoredText = Encoding.UTF8.GetString(encodedBytes);
            if (string.IsNullOrWhiteSpace(restoredText))
            {
                return text;
            }

            return ContainsHangul(restoredText) ? restoredText : text;
        }
        catch
        {
            return text;
        }
    }

    private static bool ContainsHangul(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (char character in text)
        {
            if ((character >= '\u1100' && character <= '\u11FF')
                || (character >= '\u3130' && character <= '\u318F')
                || (character >= '\uAC00' && character <= '\uD7A3'))
            {
                return true;
            }
        }

        return false;
    }
}
