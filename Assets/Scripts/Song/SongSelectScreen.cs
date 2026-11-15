using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 곡 선택 화면: 곡 목록 버튼을 만들고, 선택한 곡 정보를 보여 주며, START(또는 Enter)로 플레이 씬을 시작한다.
// 조작: 마우스 클릭으로 선택, ↑↓로 선택 이동, Enter로 시작, ←→ 또는 -/+ 버튼으로 노트 배속 변경.
public class SongSelectScreen : MonoBehaviour
{
    [SerializeField] private SongList songList;
    [SerializeField] private RectTransform listContainer; // 곡 버튼을 놓을 부모 (왼쪽 목록 영역)
    [SerializeField] private Button songButtonTemplate;   // 곡마다 복제할 버튼 템플릿 (비활성 상태로 둠)
    [SerializeField] private float itemSpacing = 110f;    // 목록 버튼 간격
    [SerializeField] private Text titleText;
    [SerializeField] private Text artistText;
    [SerializeField] private Text notesText;
    [SerializeField] private Text lengthText;
    [SerializeField] private Text messageText;            // 시작할 수 없을 때 "CANNOT PLAY"
    [SerializeField] private Button startButton;
    [SerializeField] private KeySettingPanel keySettingPanel; // 열려 있는 동안 ↑↓/Enter/←→ 무시
    [SerializeField] private Text speedText;              // 노트 배속 표시 (예: SPEED x3.0)
    [SerializeField] private Button speedDownButton;      // 배속 -0.5
    [SerializeField] private Button speedUpButton;        // 배속 +0.5
    [SerializeField] private string playSceneName = "IngameScene";
    [SerializeField] private int laneCount = 4;           // 노트 수 계산 시 채보 검증에 쓰는 레인 수 (GameplayManager와 같아야 함)
    [SerializeField] private Color normalColor = new Color(0.25f, 0.25f, 0.3f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.6f, 0.1f, 1f);

    private readonly List<Button> songButtons = new List<Button>();
    private int selectedIndex = -1;
    private bool canStart;
    private bool loading;

    private SongData[] Songs => songList != null && songList.Songs != null ? songList.Songs : new SongData[0];

    // 곡 목록 버튼을 만들고, 이전에 고른 곡(없으면 첫 곡)을 선택한다.
    private void Start()
    {
        SongData[] songs = Songs;
        for (int i = 0; i < songs.Length; i++)
        {
            Button button = Instantiate(songButtonTemplate, listContainer);
            button.gameObject.SetActive(true);
            button.name = "Song" + i;
            RectTransform rt = (RectTransform)button.transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -i * itemSpacing);
            // ↑↓는 이 스크립트에서만 처리한다 (UI 기본 내비게이션과 겹치면 두 번 움직일 수 있음)
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Text label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = songs[i] != null ? songs[i].Title : "(EMPTY)";
            int index = i;
            button.onClick.AddListener(() => Select(index));
            songButtons.Add(button);
        }

        if (startButton != null) startButton.onClick.AddListener(StartSong);
        if (speedDownButton != null) speedDownButton.onClick.AddListener(SpeedDown);
        if (speedUpButton != null) speedUpButton.onClick.AddListener(SpeedUp);
        UpdateSpeedText();

        int initial = System.Array.IndexOf(songs, SongSelection.Selected);
        Select(initial >= 0 ? initial : 0);
    }

    // 키보드: ↑↓로 선택 이동, Enter로 시작, ←→로 배속 변경
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || loading) return;
        if (keySettingPanel != null && keySettingPanel.IsOpen) return; // 키 설정 중에는 곡 선택 조작을 막는다
        if (keyboard.upArrowKey.wasPressedThisFrame) Select(selectedIndex - 1);
        if (keyboard.downArrowKey.wasPressedThisFrame) Select(selectedIndex + 1);
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) StartSong();
        if (keyboard.leftArrowKey.wasPressedThisFrame) SpeedDown();
        if (keyboard.rightArrowKey.wasPressedThisFrame) SpeedUp();
    }

    // 노트 배속을 한 단계(0.5) 내린다. (x1.0 미만으로는 내려가지 않음, 저장됨)
    public void SpeedDown()
    {
        NoteSpeed.Decrease();
        UpdateSpeedText();
    }

    // 노트 배속을 한 단계(0.5) 올린다. (x5.0 초과로는 올라가지 않음, 저장됨)
    public void SpeedUp()
    {
        NoteSpeed.Increase();
        UpdateSpeedText();
    }

    // 현재 배속을 화면에 표시한다.
    private void UpdateSpeedText()
    {
        SetText(speedText, NoteSpeed.Label);
    }

    // 곡을 선택하고 강조 표시·곡 정보·시작 가능 여부를 갱신한다. 범위를 벗어나면 끝에 맞춘다.
    public void Select(int index)
    {
        SongData[] songs = Songs;
        if (songs.Length == 0)
        {
            selectedIndex = -1;
            ShowInfo(null, null);
            return;
        }

        selectedIndex = Mathf.Clamp(index, 0, songs.Length - 1);
        for (int i = 0; i < songButtons.Count; i++)
            songButtons[i].image.color = i == selectedIndex ? selectedColor : normalColor;

        SongData song = songs[selectedIndex];
        List<NoteData> notes = song != null && song.Chart != null ? ChartLoader.Load(song.Chart, laneCount) : null;
        ShowInfo(song, notes);
    }

    // 선택한 곡으로 플레이 씬을 시작한다. 시작할 수 없는 곡이면 무시한다.
    public void StartSong()
    {
        if (!canStart || loading) return;
        loading = true;
        SongSelection.Selected = Songs[selectedIndex];
        SceneManager.LoadScene(playSceneName);
    }

    // 곡 정보 표시. 곡·채보가 없거나 노트가 0개면 START를 막고 CANNOT PLAY를 표시한다.
    private void ShowInfo(SongData song, List<NoteData> notes)
    {
        canStart = song != null && notes != null && notes.Count > 0;

        SetText(titleText, song != null ? song.Title : "-");
        SetText(artistText, song != null ? song.Artist : "");
        SetText(notesText, "NOTES  " + (notes != null ? notes.Count : 0));

        double end = 0;
        if (notes != null)
            foreach (NoteData note in notes)
                end = System.Math.Max(end, note.IsLong ? note.EndTime : note.Time);
        int seconds = Mathf.CeilToInt((float)end);
        SetText(lengthText, "LENGTH  " + seconds / 60 + ":" + (seconds % 60).ToString("00"));

        SetText(messageText, canStart ? "" : "CANNOT PLAY");
        if (startButton != null) startButton.interactable = canStart;
    }

    private static void SetText(Text text, string value)
    {
        if (text != null) text.text = value;
    }
}
