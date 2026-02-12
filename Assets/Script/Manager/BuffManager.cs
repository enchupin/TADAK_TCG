using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        TextAsset jsonFile = Resources.Load<TextAsset>("JsonData/buffs");
        if (jsonFile == null)
        {
            Debug.LogError("[BuffManager] Failed to load buffs.json");
            return;
        }

        BuffList buffList = JsonUtility.FromJson<BuffList>(jsonFile.text);
        if (buffList != null && buffList.buffs != null)
        {
            foreach (var buff in buffList.buffs)
            {
                if (!buffDatabase.ContainsKey(buff.buffId))
                {
                    buffDatabase.Add(buff.buffId, buff);
                }
            }
            Debug.Log($"[BuffManager] Loaded {buffDatabase.Count} buffs.");
        }
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
}
