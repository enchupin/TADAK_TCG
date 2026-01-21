using System.Collections.Generic;
using UnityEngine;

namespace TADAK.Lobby
{
    /// <summary>
    /// 선택된 캐릭터 데이터를 씬 간에 전달하는 싱글톤
    /// DontDestroyOnLoad로 씬 전환 시에도 유지됨
    /// </summary>
    public class SelectedCharactersData : MonoBehaviour
    {
        private static SelectedCharactersData instance;
        
        public static SelectedCharactersData Instance
        {
            get
            {
                if (instance == null)
                {
                    // 씬에서 찾기
                    instance = FindObjectOfType<SelectedCharactersData>();
                    
                    // 없으면 새로 생성
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SelectedCharactersData");
                        instance = go.AddComponent<SelectedCharactersData>();
                    }
                }
                return instance;
            }
        }
        
        private List<Character> selectedCharacters = new List<Character>();
        
        public IReadOnlyList<Character> SelectedCharacters => selectedCharacters.AsReadOnly();
        
        private void Awake()
        {
            // 싱글톤 설정
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[SelectedCharactersData] 초기화 완료");
        }
        
        /// <summary>
        /// 선택된 캐릭터 설정
        /// </summary>
        public void SetSelectedCharacters(List<Character> characters)
        {
            selectedCharacters.Clear();
            selectedCharacters.AddRange(characters);
            
            Debug.Log($"[SelectedCharactersData] {selectedCharacters.Count}개의 캐릭터가 저장되었습니다:");
            foreach (var character in selectedCharacters)
            {
                Debug.Log($"  - {character}");
            }
        }
        
        /// <summary>
        /// 선택된 캐릭터 가져오기
        /// </summary>
        public List<Character> GetSelectedCharacters()
        {
            return new List<Character>(selectedCharacters);
        }
        
        /// <summary>
        /// 특정 캐릭터가 선택되었는지 확인
        /// </summary>
        public bool IsCharacterSelected(Character character)
        {
            return selectedCharacters.Contains(character);
        }
        
        /// <summary>
        /// 선택된 캐릭터 개수
        /// </summary>
        public int GetSelectionCount()
        {
            return selectedCharacters.Count;
        }
        
        /// <summary>
        /// 선택 데이터 초기화
        /// </summary>
        public void ClearSelection()
        {
            selectedCharacters.Clear();
            Debug.Log("[SelectedCharactersData] 선택 데이터가 초기화되었습니다.");
        }
    }
}
