using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 플레이 중 일시정지 메뉴: Esc로 정지하고 RESUME / RESTART / SONG SELECT를 고른다.
// 재개는 3-2-1 카운트다운 뒤에 이루어지며, 카운트다운 동안 게임은 계속 정지 상태라 레인 키를 다시 잡을 수 있다.
// Esc: 플레이 중 → 메뉴, 메뉴 → 카운트다운(재개), 카운트다운 중 → 메뉴로 돌아감.
public class PauseController : MonoBehaviour
{
    [SerializeField] private GameplayManager gameplay;
    [SerializeField] private ResultTransition transition;   // 결과 전환이 시작되면 일시정지를 막는다
    [SerializeField] private GameObject menuPanel;          // 반투명 전체 배경 + PAUSED + 버튼 3개 (뒤쪽 클릭 차단)
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button songSelectButton;
    [SerializeField] private Text countdownText;            // 화면 가운데 3 / 2 / 1
    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private string songSelectSceneName = "SongSelectScene";

    private float countdownLeft; // 남은 카운트다운 시간 (0이면 카운트다운 중 아님)
    private bool loading;        // 씬 로드를 요청했으면 다른 요청을 무시한다

    public bool IsMenuOpen => menuPanel != null && menuPanel.activeSelf;
    public bool IsCountingDown => countdownLeft > 0f;

    // 버튼을 연결하고 메뉴·카운트다운을 숨긴 상태로 시작한다.
    private void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (songSelectButton != null) songSelectButton.onClick.AddListener(GoToSongSelect);
        SetMenu(false);
        SetCountdown(false);
    }

    private void Update()
    {
        if (loading || gameplay == null) return;

        // 카운트다운 진행: 끝나면 게임을 재개한다.
        if (IsCountingDown)
        {
            countdownLeft -= Time.deltaTime;
            if (countdownLeft <= 0f)
            {
                countdownLeft = 0f;
                SetCountdown(false);
                gameplay.TryResume();
            }
            else if (countdownText != null)
            {
                countdownText.text = Mathf.CeilToInt(countdownLeft).ToString(); // 3 → 2 → 1
            }
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
        if (!gameplay.IsPaused || IsCountingDown) Pause(); // 플레이 중 정지, 또는 카운트다운 취소
        else Resume();                                     // 메뉴에서 Esc → 재개 카운트다운
    }

    // 게임을 멈추고 메뉴를 연다. 카운트다운 중이면 카운트다운을 취소하고 메뉴로 돌아간다.
    // 결과 전환이 이미 시작되었으면 무시한다.
    public void Pause()
    {
        if (loading || gameplay == null) return;
        if (transition != null && transition.IsTransitioning) return;
        if (IsCountingDown)
        {
            countdownLeft = 0f;
            SetCountdown(false);
            SetMenu(true);
            return;
        }
        if (gameplay.TryPause()) SetMenu(true);
    }

    // 메뉴를 닫고 재개 카운트다운을 시작한다. (게임은 카운트다운이 끝날 때까지 정지 상태)
    public void Resume()
    {
        if (loading || gameplay == null || !gameplay.IsPaused || IsCountingDown) return;
        SetMenu(false);
        // 클릭한 버튼이 선택된 채로 남아 Space/Enter가 UI 제출로 처리되지 않게 선택을 해제한다.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        countdownLeft = countdownSeconds;
        if (countdownText != null) countdownText.text = Mathf.CeilToInt(countdownLeft).ToString();
        SetCountdown(true);
    }

    // 같은 곡을 처음부터 다시 플레이한다 (씬 다시 불러오기). 메뉴가 열려 있을 때만.
    public void Restart()
    {
        if (!CanUseMenu()) return;
        loading = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 곡 선택 화면으로 돌아간다. 고른 곡(SongSelection.Selected)은 그대로라 같은 곡이 선택된 상태가 된다.
    public void GoToSongSelect()
    {
        if (!CanUseMenu()) return;
        loading = true;
        SceneManager.LoadScene(songSelectSceneName);
    }

    // 메뉴 버튼을 쓸 수 있는 상태인지: 정지 중이고 카운트다운 중이 아니며 씬 로드를 요청하지 않음
    private bool CanUseMenu()
    {
        return !loading && gameplay != null && gameplay.IsPaused && !IsCountingDown;
    }

    private void SetMenu(bool open)
    {
        if (menuPanel != null) menuPanel.SetActive(open);
    }

    private void SetCountdown(bool show)
    {
        if (countdownText != null) countdownText.gameObject.SetActive(show);
    }
}
