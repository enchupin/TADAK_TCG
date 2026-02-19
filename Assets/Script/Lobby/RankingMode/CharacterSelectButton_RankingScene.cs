using UnityEngine;

namespace RankingMode
{
    /// <summary>
    /// 랭킹 모드 씬에서 캐릭터 선택 버튼
    /// CharButtonManager에 의해 관리됨
    /// </summary>
    public class CharacterSelectBtn : MonoBehaviour
    {
        [Header("캐릭터 설정")]
        [SerializeField] private Character character; // 이 버튼이 담당하는 캐릭터

        [Header("생성할 오브젝트 프리팹")]
        [SerializeField] private GameObject deckSelectButtonPrefab; // 버튼 우측에 생성할 프리팹 (ex: 덱 선택 팝업)

        [Header("RankingScene 버튼 매니저")]
        [SerializeField] public RankingModeManager manager;

        private GameObject deckListBtn;

        private readonly Vector3 deckListBtnPos = new(160f, 0f, 0f);

        /// <summary>
        /// 버튼 클릭 시 호출
        /// </summary>
        public void OnButtonClick()
        {
            if (manager == null)
            {
                Debug.LogError("[CharSelectButton] Manager를 찾을 수 없습니다.");
                return;
            }

            // 매니저에게 나를 클릭했음을 알림
            manager.OnCharacterButtonClicked(this);
        }

        /// <summary>
        /// 덱 리스트 버튼 열기
        /// </summary>
        public void OpenExpandedMenu()
        {
            if (deckSelectButtonPrefab == null) return;
            if (deckListBtn != null) return; // 이미 생성되어 있으면 종료

            deckListBtn = Instantiate(deckSelectButtonPrefab, transform);
            deckListBtn.transform.localScale = Vector3.one;
            deckListBtn.transform.localPosition = deckListBtnPos;
        }

        /// <summary>
        /// 덱 리스트 버튼 닫기
        /// </summary>
        public void CloseCurrentExpanded()
        {
            if (deckListBtn != null)
            {
                Destroy(deckListBtn);
                deckListBtn = null;
            }
        }
    }
}
