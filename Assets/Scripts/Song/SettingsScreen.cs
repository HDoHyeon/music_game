using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 설정 화면: 키 설정 패널(기존 KeySettingPanel), 오프셋 패널, 오디오 패널을 열고, BACK으로 곡 선택 화면에 돌아간다.
// 오프셋 패널에는 현재 오프셋 값과 AUTO SYNC(자동 싱크 보정 화면 이동) 버튼이 있다. 오프셋 저장은 보정 화면의 APPLY에서만 한다.
// 오디오 패널에는 MASTER / MUSIC / SFX 슬라이더(0~100%)와 RESET이 있다. 슬라이더를 움직이면 바로 저장된다(AudioVolume).
// 한 패널이 열려 있는 동안에는 BACK과 다른 패널 열기를 무시한다.
public class SettingsScreen : MonoBehaviour
{
    [SerializeField] private KeySettingPanel keySettingPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private Button offsetOpenButton;    // OFFSET SETTING
    [SerializeField] private GameObject offsetPanelRoot; // 오프셋 패널 (처음엔 숨김)
    [SerializeField] private Button offsetCloseButton;
    [SerializeField] private Text offsetText;            // 현재 오프셋 (예: OFFSET +15 ms)
    [SerializeField] private Button autoSyncButton;      // 오프셋 패널 안의 AUTO SYNC
    [SerializeField] private Button audioOpenButton;     // AUDIO SETTING
    [SerializeField] private GameObject audioPanelRoot;  // 오디오 패널 (처음엔 숨김)
    [SerializeField] private Button audioCloseButton;
    [SerializeField] private Button audioResetButton;
    [SerializeField] private Slider[] volumeSliders = new Slider[3]; // AudioVolume 종류 순서: MASTER, MUSIC, SFX
    [SerializeField] private Text[] volumeTexts = new Text[3];       // 현재 값 표시 (예: 100%)
    [SerializeField] private string songSelectSceneName = "SongSelectScene";
    [SerializeField] private string calibrationSceneName = "OffsetCalibrationScene";

    private bool loading;

    private bool IsOffsetOpen => offsetPanelRoot != null && offsetPanelRoot.activeSelf;
    private bool IsAudioOpen => audioPanelRoot != null && audioPanelRoot.activeSelf;
    private bool IsKeyOpen => keySettingPanel != null && keySettingPanel.IsOpen;

    // 버튼을 연결하고 오프셋·오디오 패널을 닫힌 상태로 시작한다. 오프셋 값은 보정 화면에서 APPLY 후 돌아오면 새 값이 보인다.
    private void Start()
    {
        if (offsetPanelRoot != null) offsetPanelRoot.SetActive(false);
        if (audioPanelRoot != null) audioPanelRoot.SetActive(false);
        if (offsetText != null) offsetText.text = OffsetSetting.Label;
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (offsetOpenButton != null) offsetOpenButton.onClick.AddListener(OpenOffset);
        if (offsetCloseButton != null) offsetCloseButton.onClick.AddListener(CloseOffset);
        if (autoSyncButton != null) autoSyncButton.onClick.AddListener(OpenAutoSync);
        if (audioOpenButton != null) audioOpenButton.onClick.AddListener(OpenAudio);
        if (audioCloseButton != null) audioCloseButton.onClick.AddListener(CloseAudio);
        if (audioResetButton != null) audioResetButton.onClick.AddListener(ResetVolumes);
        for (int i = 0; i < volumeSliders.Length; i++)
        {
            if (volumeSliders[i] == null) continue;
            int kind = i;
            volumeSliders[i].minValue = AudioVolume.Min;
            volumeSliders[i].maxValue = AudioVolume.Max;
            volumeSliders[i].wholeNumbers = true;
            volumeSliders[i].SetValueWithoutNotify(AudioVolume.Get(kind));
            volumeSliders[i].onValueChanged.AddListener(v => OnVolumeChanged(kind, v));
        }
        RefreshVolumeTexts();
    }

    // 곡 선택 화면으로 돌아간다. 패널이 열려 있으면 무시한다 (먼저 CLOSE).
    public void Back()
    {
        if (loading || IsKeyOpen || IsOffsetOpen || IsAudioOpen) return;
        Load(songSelectSceneName);
    }

    // 오프셋 패널을 연다. 다른 패널이 열려 있으면 무시한다.
    public void OpenOffset()
    {
        if (loading || IsKeyOpen || IsAudioOpen || offsetPanelRoot == null) return;
        if (offsetText != null) offsetText.text = OffsetSetting.Label;
        offsetPanelRoot.SetActive(true);
    }

    // 오프셋 패널을 닫는다.
    public void CloseOffset()
    {
        if (offsetPanelRoot != null) offsetPanelRoot.SetActive(false);
    }

    // 오디오 패널을 연다. 다른 패널이 열려 있으면 무시한다.
    public void OpenAudio()
    {
        if (loading || IsKeyOpen || IsOffsetOpen || audioPanelRoot == null) return;
        audioPanelRoot.SetActive(true);
    }

    // 오디오 패널을 닫는다.
    public void CloseAudio()
    {
        if (audioPanelRoot != null) audioPanelRoot.SetActive(false);
    }

    // 세 볼륨을 100%로 되돌리고 슬라이더·표시를 갱신한다.
    public void ResetVolumes()
    {
        AudioVolume.ResetAll();
        for (int i = 0; i < volumeSliders.Length; i++)
            if (volumeSliders[i] != null) volumeSliders[i].SetValueWithoutNotify(AudioVolume.Get(i));
        RefreshVolumeTexts();
    }

    // 슬라이더를 움직이면 바로 저장하고 % 표시를 갱신한다. (MASTER는 저장과 함께 게임 전체 볼륨에 적용)
    private void OnVolumeChanged(int kind, float value)
    {
        AudioVolume.Set(kind, Mathf.RoundToInt(value));
        RefreshVolumeTexts();
    }

    private void RefreshVolumeTexts()
    {
        for (int i = 0; i < volumeTexts.Length; i++)
            if (volumeTexts[i] != null) volumeTexts[i].text = AudioVolume.Get(i) + "%";
    }

    // 자동 싱크 보정 화면으로 이동한다. 오프셋 패널이 열려 있을 때만 (패널 안의 버튼).
    public void OpenAutoSync()
    {
        if (loading || !IsOffsetOpen) return;
        Load(calibrationSceneName);
    }

    private void Load(string sceneName)
    {
        loading = true;
        SceneManager.LoadScene(sceneName);
    }
}
