using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 모든 카드 데이터를 관리하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "CardCollection", menuName = "TCG/Card Collection")]
public class CardCollection : ScriptableObject
{
    [Header("모든 카드")]
    public List<CardData> allCards = new();
}
