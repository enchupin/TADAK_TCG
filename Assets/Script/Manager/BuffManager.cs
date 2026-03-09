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
}
