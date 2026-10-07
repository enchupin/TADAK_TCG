using System;
using System.Collections;
using System.Collections.Generic;

public static class CardEffectSequence
{
    public static IEnumerator Execute(IEnumerable<ICardEffect> effects, TrainingBattleManager manager, int? amount = null)
    {
        if (effects == null)
            yield break;

        foreach (ICardEffect effect in effects)
        {
            if (effect != null)
                yield return effect.ExecuteSequence(manager, amount);
        }
    }

    // 중첩 효과를 같은 실행 흐름에서 처리하고 중단이나 예외 발생 시 컨텍스트를 복구
    public static IEnumerator Run(IEnumerator sequence, TrainingBattleManager manager)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(sequence);
        try
        {
            while (stack.Count > 0 && manager != null && manager.CurrentTurnState != BattleTurnState.CombatEnd)
            {
                IEnumerator current = stack.Peek();
                if (!current.MoveNext())
                {
                    stack.Pop();
                    (current as IDisposable)?.Dispose();
                }
                else if (current.Current is IEnumerator child)
                {
                    stack.Push(child);
                }
                else
                {
                    yield return current.Current;
                }
            }
        }
        finally
        {
            while (stack.Count > 0)
                (stack.Pop() as IDisposable)?.Dispose();
        }
    }
}
