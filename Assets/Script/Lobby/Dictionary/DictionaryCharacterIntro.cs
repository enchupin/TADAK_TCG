using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 도감 캐릭터 소개: 이름 · 소개 문구 · 고유 효과 · 아이덴티티 스킬(비용)
/// 버프 이름은 툴팁과 같은 이로운 · 해로운 효과 색으로 표시
/// </summary>
public class DictionaryCharacterIntro : MonoBehaviour
{
    [Tooltip("소개를 표시할 텍스트 (DescriptionRect 안, 리치 텍스트 사용)")]
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("이름 · 고유 효과 · 아이덴티티 스킬 제목을 굵게 표시")]
    [SerializeField] private bool boldHeaders = true;

    private int shownCharacterId;

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Refresh;
    }

    public void Show(int characterId)
    {
        shownCharacterId = characterId;
        Refresh();
    }

    private void Refresh()
    {
        if (descriptionText == null)
        {
            return;
        }

        descriptionText.richText = true;
        descriptionText.text = shownCharacterId > 0 ? Build(shownCharacterId) : string.Empty;
    }

    private string Build(int characterId)
    {
        StringBuilder builder = new StringBuilder();
        // 첫 줄은 캐릭터 이름 (제목과 같은 굵기), 소개는 다음 줄
        string name = GameText.CharacterName(characterId);
        if (!string.IsNullOrWhiteSpace(name))
        {
            builder.Append(boldHeaders ? $"<b>{name}</b>" : name);
        }

        string intro = GameText.CharacterIntro(characterId);
        if (!string.IsNullOrWhiteSpace(intro))
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }
            builder.Append(intro);
        }

        string traits = GameText.CharacterTraits(characterId);
        if (!string.IsNullOrWhiteSpace(traits))
        {
            AppendSection(builder, GameText.Ui("ui.dictionary.traits"), traits);
        }

        string identity = GameText.IdentityDescription(characterId);
        if (!string.IsNullOrWhiteSpace(identity))
        {
            CharacterData character = CharacterManager.GetCharacter(characterId);
            int cost = character != null && character.identity != null ? character.identity.cost : 0;
            string header = GameText.Ui("ui.dictionary.identity", new Dictionary<string, object> { { "cost", cost } });
            AppendSection(builder, header, identity);
        }

        return builder.ToString();
    }

    // 빈 줄 + 제목 줄 + 본문 (본문의 버프 참조는 극성 색)
    private void AppendSection(StringBuilder builder, string header, string body)
    {
        if (builder.Length > 0)
        {
            builder.Append("\n\n");
        }

        builder.Append(boldHeaders ? $"<b>{header}</b>" : header);
        builder.Append('\n');
        builder.Append(BuffTooltipTextBuilder.FormatReferences(body));
    }
}
