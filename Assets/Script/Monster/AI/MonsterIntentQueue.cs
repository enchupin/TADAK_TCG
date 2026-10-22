using System;
using System.Collections.Generic;

// R-70 의도 큐: 아직 표시되지 않은 확정된 다음 의도만 보관 (현재 의도는 큐에 없음)
public sealed class MonsterIntentQueue
{
    // 봉인으로 넣는 '행동 없음' 칸
    public const string NoActionKey = "__NONE__";
    private const int MaxFillCount = 32;

    private readonly List<string> queued = new List<string>();
    private readonly Func<string> generator;

    public MonsterIntentQueue(Func<string> generator)
    {
        this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }

    public IReadOnlyList<string> Items => queued;
    public int Count => queued.Count;

    // 큐가 비어 있으면 현재 상태 기준으로 새 의도를 정함
    public string Dequeue(out bool generated)
    {
        if (queued.Count > 0)
        {
            string key = queued[0];
            queued.RemoveAt(0);
            generated = false;
            return key;
        }

        generated = true;
        return generator();
    }

    public void Clear()
    {
        queued.Clear();
    }

    // n칸 미루기: 현재 의도를 큐의 n번째로 바꾸고 앞의 n−1칸을 버림, 현재와 같은 패턴이면 다음 칸으로
    public string Delay(string currentKey, int count)
    {
        int index = Math.Max(1, count) - 1;
        EnsureCount(index + 1);
        while (index < queued.Count && queued[index] == currentKey)
        {
            index++;
            if (index >= MaxFillCount)
            {
                return currentKey;
            }

            EnsureCount(index + 1);
        }

        if (index >= queued.Count)
        {
            return currentKey;
        }

        string result = queued[index];
        queued.RemoveRange(0, index + 1);
        return result;
    }

    // 공격 의도 복사: 큐에서 가장 빠른 공격 의도 (큐에 없으면 채워 가며 찾음)
    public string FindFirstAttack(Func<string, bool> isAttack)
    {
        for (int index = 0; index < MaxFillCount; index++)
        {
            EnsureCount(index + 1);
            if (index >= queued.Count)
            {
                return null;
            }

            if (isAttack(queued[index]))
            {
                return queued[index];
            }
        }

        return null;
    }

    // 봉인: 큐 맨 앞에 '행동 없음' 1칸
    public void InsertNoAction()
    {
        queued.Insert(0, NoActionKey);
    }

    private void EnsureCount(int count)
    {
        while (queued.Count < count && queued.Count < MaxFillCount)
        {
            string key = generator();
            if (key == null)
            {
                return;
            }

            queued.Add(key);
        }
    }
}
