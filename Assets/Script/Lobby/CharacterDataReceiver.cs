using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TADAK.Lobby
{
    /// <summary>
    /// 다른 씬에서 선택된 캐릭터를 사용하는 예제 스크립트
    /// </summary>
    public class CharacterDataReceiver : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text selectedCharactersText;
        [SerializeField] private Transform characterDisplayContainer;
        
        private void Start()
        {
            LoadSelectedCharacters();
        }
        
        /// <summary>
        /// 선택된 캐릭터 데이터 로드
        /// </summary>
        private void LoadSelectedCharacters()
        {
            // SelectedCharactersData에서 데이터 가져오기
            List<Character> selectedCharacters = SelectedCharactersData.Instance.GetSelectedCharacters();
            
            if (selectedCharacters.Count == 0)
            {
                Debug.LogWarning("[CharacterDataReceiver] 선택된 캐릭터가 없습니다.");
                if (selectedCharactersText != null)
                {
                    selectedCharactersText.text = "선택된 캐릭터가 없습니다.";
                }
                return;
            }
            
            Debug.Log($"[CharacterDataReceiver] {selectedCharacters.Count}개의 캐릭터를 로드했습니다:");
            
            string characterList = "선택된 캐릭터:\n";
            foreach (var character in selectedCharacters)
            {
                Debug.Log($"  - {character}");
                characterList += $"- {GetCharacterName(character)}\n";
                
                // TODO: 캐릭터 프리팹 생성, 카드 덱 구성 등
                SpawnCharacter(character);
            }
            
            if (selectedCharactersText != null)
            {
                selectedCharactersText.text = characterList;
            }
        }
        
        /// <summary>
        /// 캐릭터 생성 (예제)
        /// </summary>
        private void SpawnCharacter(Character character)
        {
            // TODO: 실제 캐릭터 프리팹 생성 로직
            Debug.Log($"[CharacterDataReceiver] {character} 캐릭터를 생성합니다.");
            
            // 예시: 컨테이너에 UI 요소 생성
            if (characterDisplayContainer != null)
            {
                GameObject characterObj = new GameObject($"Character_{character}");
                characterObj.transform.SetParent(characterDisplayContainer);
                
                Text text = characterObj.AddComponent<Text>();
                text.text = GetCharacterName(character);
                text.fontSize = 24;
                text.alignment = TextAnchor.MiddleCenter;
            }
        }
        
        /// <summary>
        /// 캐릭터 이름 가져오기
        /// </summary>
        private string GetCharacterName(Character character)
        {
            return character switch
            {
                Character.Warrior => "전사",
                Character.Mage => "마법사",
                Character.Archer => "궁수",
                Character.Assassin => "암살자",
                Character.Priest => "성직자",
                Character.Knight => "기사",
                _ => "알 수 없음"
            };
        }
        
        /// <summary>
        /// 특정 캐릭터가 선택되었는지 확인하는 예제
        /// </summary>
        public bool HasCharacter(Character character)
        {
            return SelectedCharactersData.Instance.IsCharacterSelected(character);
        }
    }
}
