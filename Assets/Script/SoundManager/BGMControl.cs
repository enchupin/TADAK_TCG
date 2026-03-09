using UnityEngine;

public class BGMControl : MonoBehaviour
{
    public static BGMControl Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource bgmAudioSource;

    // PlayerPrefs 키
    private const string BGM_VOLUME_KEY = "bgmVolume";
    private const float DEFAULT_BGM_VOLUME = 1.0f;

    // 현재 BGM 볼륨
    private float currentBGMVolume;


    private void Awake() {
        // 싱글톤 패턴 구현
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        } else {
            Destroy(gameObject);
            return;
        }

        // AudioSource가 할당되지 않았다면 자동으로 추가
        if (bgmAudioSource == null) {
            bgmAudioSource = GetComponent<AudioSource>();
            if (bgmAudioSource == null) {
                Debug.LogWarning("bgm AudioSource가 없어서 자동으로 추가합니다.");
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // PlayerPrefs에서 bgm 볼륨 로드
        LoadbgmVolume();

        // 테스트 용 (소리가 너무 커서 줄여놓음)
        currentBGMVolume = 0.15f;
    }

    /// <summary>
    /// PlayerPrefs에서 bgm 볼륨 설정을 로드
    /// </summary>
    private void LoadbgmVolume() {
        currentBGMVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, DEFAULT_BGM_VOLUME);
    }

    /// <summary>
    /// bgm 볼륨 설정
    /// </summary>
    public void SetbgmVolume(float volume) {
        currentBGMVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, currentBGMVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 현재 bgm 볼륨 가져오기
    /// </summary>
    public float GetbgmVolume() {
        return currentBGMVolume;
    }


    /// <summary>
    /// 특정 BGM을 재생 (설정된 볼륨 적용)
    /// </summary>
    public void Playbgm(AudioClip clip) {
        if (clip != null && bgmAudioSource != null) {
            bgmAudioSource.PlayOneShot(clip, currentBGMVolume);
        }
    }

}
