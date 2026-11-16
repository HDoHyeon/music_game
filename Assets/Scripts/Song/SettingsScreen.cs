using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 설정 화면: 키 설정 패널(기존 KeySettingPanel)과 오프셋 패널을 열고, BACK으로 곡 선택 화면에 돌아간다.
// 오프셋 패널에는 현재 오프셋 값과 AUTO SYNC(자동 싱크 보정 화면 이동) 버튼이 있다. 오프셋 저장은 보정 화면의 APPLY에서만 한다.
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
    [SerializeField] private string songSelectSceneName = "SongSelectScene";
    [SerializeField] private string calibrationSceneName = "OffsetCalibrationScene";

    private bool loading;

    private bool IsOffsetOpen => offsetPanelRoot != null && offsetPanelRoot.activeSelf;
    private bool IsKeyOpen => keySettingPanel != null && keySettingPanel.IsOpen;

    // 버튼을 연결하고 오프셋 패널을 닫힌 상태로 시작한다. 오프셋 값은 보정 화면에서 APPLY 후 돌아오면 새 값이 보인다.
    private void Start()
    {
        if (offsetPanelRoot != null) offsetPanelRoot.SetActive(false);
        if (offsetText != null) offsetText.text = OffsetSetting.Label;
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (offsetOpenButton != null) offsetOpenButton.onClick.AddListener(OpenOffset);
        if (offsetCloseButton != null) offsetCloseButton.onClick.AddListener(CloseOffset);
        if (autoSyncButton != null) autoSyncButton.onClick.AddListener(OpenAutoSync);
    }

    // 곡 선택 화면으로 돌아간다. 패널이 열려 있으면 무시한다 (먼저 CLOSE).
    public void Back()
    {
        if (loading || IsKeyOpen || IsOffsetOpen) return;
        Load(songSelectSceneName);
    }

    // 오프셋 패널을 연다. 키 설정 패널이 열려 있으면 무시한다.
    public void OpenOffset()
    {
        if (loading || IsKeyOpen || offsetPanelRoot == null) return;
        if (offsetText != null) offsetText.text = OffsetSetting.Label;
        offsetPanelRoot.SetActive(true);
    }

    // 오프셋 패널을 닫는다.
    public void CloseOffset()
    {
        if (offsetPanelRoot != null) offsetPanelRoot.SetActive(false);
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
