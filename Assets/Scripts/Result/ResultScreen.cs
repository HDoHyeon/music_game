using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 결과 씬: 마지막 플레이 결과(PlayResult.Last)를 표시하고, 다시 하기(같은 곡)·곡 선택 버튼으로 다음 씬을 불러온다.
public class ResultScreen : MonoBehaviour
{
    [SerializeField] private Text rankText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text maxComboText;
    [SerializeField] private Text accuracyText;
    [SerializeField] private Text judgementText; // Perfect/Great/Good/Miss 개수 4줄 (아래 개수 칸이 연결되지 않았을 때만 사용)
    [SerializeField] private Text perfectCountText; // 판정별 개수 (숫자만, 라벨·색 막대는 씬에 고정)
    [SerializeField] private Text greatCountText;
    [SerializeField] private Text goodCountText;
    [SerializeField] private Text missCountText;
    [SerializeField] private Text newBestText;   // 최고 점수를 갱신했으면 NEW BEST (아니면 숨김)
    [SerializeField] private Text bestText;      // 저장된 최고 점수 (이번 결과와 구분)
    [SerializeField] private Text songTitleText; // 플레이한 곡명 (오른쪽 위)
    [SerializeField] private Text songSubText;   // 아티스트 · 난이도 (난이도는 난이도 색)
    [SerializeField] private Color[] difficultyColors = new Color[3]; // Easy/Normal/Hard 글자 색 (곡 선택 화면과 같은 값)
    [SerializeField] private Button retryButton;
    [SerializeField] private Button songSelectButton;
    [SerializeField] private string playSceneName = "IngameScene";
    [SerializeField] private string songSelectSceneName = "SongSelectScene";

    // 숫자 형식(쉼표·소수점)이 문화권에 따라 바뀌지 않도록 고정
    private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

    // 결과를 화면에 채우고 다시 하기 버튼을 연결한다.
    private void Start()
    {
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (songSelectButton != null) songSelectButton.onClick.AddListener(GoToSongSelect);

        PlayResult result = PlayResult.Last;
        if (result == null)
        {
            // 결과 씬을 바로 실행한 경우 등 넘겨받은 결과가 없을 때
            SetText(rankText, "-");
            if (rankText != null) rankText.color = RankColor("-");
            SetText(scoreText, "NO RESULT"); // 내장 폰트(LegacyRuntime)에 한글 글리프가 없을 수 있어 영문으로 표시
            SetText(maxComboText, "");
            SetText(accuracyText, "");
            SetText(judgementText, "");
            SetText(perfectCountText, "");
            SetText(greatCountText, "");
            SetText(goodCountText, "");
            SetText(missCountText, "");
            SetText(bestText, "");
            if (newBestText != null) newBestText.gameObject.SetActive(false);
            ShowSongInfo(null); // 결과가 없으면 곡 정보도 숨긴다
            return;
        }

        // 값만 표시한다 (SCORE·MAX COMBO 등 라벨은 씬에 고정). 계산은 PlayResult 그대로.
        SetText(rankText, result.Rank);
        if (rankText != null) rankText.color = RankColor(result.Rank);
        SetText(scoreText, result.Score.ToString("N0", Inv));                // 1,174,000
        SetText(maxComboText, result.MaxCombo.ToString("N0", Inv));          // 1,510
        SetText(accuracyText, result.Accuracy.ToString("F1", Inv) + "%");    // 100.0% (소수 첫째 자리)
        if (perfectCountText != null)
        {
            SetText(perfectCountText, result.Perfect.ToString("N0", Inv));
            SetText(greatCountText, result.Great.ToString("N0", Inv));
            SetText(goodCountText, result.Good.ToString("N0", Inv));
            SetText(missCountText, result.Miss.ToString("N0", Inv));
        }
        else
        {
            // 개수 칸이 연결되지 않은 경우 예전처럼 4줄 문자열
            SetText(judgementText,
                "PERFECT  " + result.Perfect + "\n" +
                "GREAT  " + result.Great + "\n" +
                "GOOD  " + result.Good + "\n" +
                "MISS  " + result.Miss);
        }

        // 기록: AUTO TEST·곡 선택 없이 실행한 플레이는 저장되지 않으므로 NEW BEST가 나오지 않는다
        if (newBestText != null) newBestText.gameObject.SetActive(result.IsNewBest);
        SetText(bestText, "BEST  " + (result.HasBest ? result.BestScore.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : "-"));

        // 곡 정보: 결과 씬은 플레이 종료(ResultTransition)로만 열리고 그 사이 선택 값이 바뀌지 않으므로,
        // 지금의 선택 곡·난이도가 방금 플레이한 곡이다. 곡 선택 없이 플레이했으면 null → 숨김.
        ShowSongInfo(SongSelection.Selected);
    }

    // 곡명과 "아티스트 · 난이도"를 표시한다. song이 null이면 둘 다 숨긴다.
    // 난이도는 난이도 구분이 있는 곡만 표시 (Test Song처럼 채보가 하나뿐인 곡은 숨김, 곡 선택 화면과 같은 규칙).
    private void ShowSongInfo(SongData song)
    {
        bool show = song != null;
        if (songTitleText != null) songTitleText.gameObject.SetActive(show);
        if (songSubText != null) songSubText.gameObject.SetActive(show);
        if (!show) return;

        SetText(songTitleText, string.IsNullOrEmpty(song.Title) ? song.name : song.Title); // 곡명이 비면 에셋 이름

        string sub = song.Artist ?? "";
        if (song.HasDifficulties)
        {
            Difficulty difficulty = SongSelection.SelectedDifficulty;
            int i = (int)difficulty;
            Color color = difficultyColors != null && i < difficultyColors.Length ? difficultyColors[i] : Color.white;
            string label = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + "><b>" + difficulty.ToString().ToUpperInvariant() + "</b></color>";
            sub = sub.Length > 0 ? sub + "  ·  " + label : label; // 아티스트가 비면 난이도만
        }
        SetText(songSubText, sub);
    }

    // 랭크별 글자 색 (곡 선택 화면 SongSelectScreen.RankColor와 같은 값). 결과 없음("-")은 D와 같은 회색.
    private static Color RankColor(string rank)
    {
        switch (rank)
        {
            case "SS": return new Color(1f, 0.84f, 0.29f, 1f);    // 금색
            case "S": return new Color(0.97f, 0.58f, 0.12f, 1f);  // 주황
            case "A": return new Color(0.36f, 0.75f, 1f, 1f);     // 하늘
            case "B": return new Color(0.5f, 0.88f, 0.5f, 1f);    // 초록
            case "C": return new Color(0.75f, 0.77f, 0.82f, 1f);  // 밝은 회색
            default: return new Color(0.54f, 0.56f, 0.63f, 1f);   // D, 결과 없음: 회색
        }
    }

    // 다시 하기: 플레이 씬을 처음부터 다시 불러온다.
    public void Retry()
    {
        SceneManager.LoadScene(playSceneName);
    }

    // 곡 선택: 곡 선택 화면으로 돌아간다 (마지막으로 고른 곡이 선택된 상태)
    public void GoToSongSelect()
    {
        SceneManager.LoadScene(songSelectSceneName);
    }

    private static void SetText(Text text, string value)
    {
        if (text != null) text.text = value;
    }
}
