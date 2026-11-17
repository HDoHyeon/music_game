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
    [SerializeField] private Text judgementText; // Perfect/Great/Good/Miss 개수
    [SerializeField] private Text newBestText;   // 최고 점수를 갱신했으면 NEW BEST (아니면 숨김)
    [SerializeField] private Text bestText;      // 저장된 최고 점수 (이번 결과와 구분)
    [SerializeField] private Button retryButton;
    [SerializeField] private Button songSelectButton;
    [SerializeField] private string playSceneName = "IngameScene";
    [SerializeField] private string songSelectSceneName = "SongSelectScene";

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
            SetText(scoreText, "NO RESULT"); // 내장 폰트(LegacyRuntime)에 한글 글리프가 없을 수 있어 영문으로 표시
            SetText(maxComboText, "");
            SetText(accuracyText, "");
            SetText(judgementText, "");
            SetText(bestText, "");
            if (newBestText != null) newBestText.gameObject.SetActive(false);
            return;
        }

        SetText(rankText, result.Rank);
        SetText(scoreText, "SCORE  " + result.Score);
        SetText(maxComboText, "MAX COMBO  " + result.MaxCombo);
        SetText(accuracyText, "ACCURACY  " + result.Accuracy.ToString("F1") + "%");
        SetText(judgementText,
            "PERFECT  " + result.Perfect + "\n" +
            "GREAT  " + result.Great + "\n" +
            "GOOD  " + result.Good + "\n" +
            "MISS  " + result.Miss);

        // 기록: AUTO TEST·곡 선택 없이 실행한 플레이는 저장되지 않으므로 NEW BEST가 나오지 않는다
        if (newBestText != null) newBestText.gameObject.SetActive(result.IsNewBest);
        SetText(bestText, "BEST  " + (result.HasBest ? result.BestScore.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) : "-"));
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
