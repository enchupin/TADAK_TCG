using UnityEngine;

/// <summary>
/// 키워드 효과 (보존, 휘발, 소멸 등)
/// </summary>
public class KeywordEffect : ICardEffect
{
    public string keyword;  // "보존", "휘발", "소멸", "연쇄" 등
    public int amount;
    public string amountFormula;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int keywordAmount = GetAmount(battleManager.battleContext, battleManager.playerData);
        
        // TODO: 키워드 시스템 구현 필요
        // 현재는 로그만 출력
        Debug.Log($"[KeywordEffect] 키워드 '{keyword}' {keywordAmount} 부여 (구현 예정)");
        
        // 키워드 시스템이 구현되면:
        // - Card 객체에 keywords 리스트 추가
        // - 턴 종료 시 키워드에 따라 동작 (보존: 버리지 않음, 휘발: 소멸 등)
        
        battleManager.UpdateAllUI();
    }
    
    public int GetAmount(BattleContext context, PlayerData player = null)
    {
        if (!string.IsNullOrEmpty(amountFormula))
        {
            return FormulaEvaluator.Evaluate(amountFormula, context, player);
        }
        return amount;
    }
}
