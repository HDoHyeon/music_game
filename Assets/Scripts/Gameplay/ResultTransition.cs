using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 플레이 종료 → 결과 화면 전환.
// 모든 노트 판정·화면 표시가 끝나고 음악도 끝나면 1초 대기 후 0.5초 동안 검게 페이드 아웃하고,
// 결과를 PlayResult.Last에 저장한 뒤 결과 씬을 불러온다.
public class ResultTransition : MonoBehaviour
{
    [SerializeField] private GameplayManager gameplay;
    [SerializeField] private ScoreComboState scoreCombo;
    [SerializeField] private Image fadeOverlay;                     // 화면 전체를 덮는 검은 이미지 (처음엔 투명)
    [SerializeField] private string resultSceneName = "ResultScene";
    [SerializeField] private float waitBeforeFade = 1f;             // 종료 조건 만족 후 대기 시간(초)
    [SerializeField] private float fadeDuration = 0.5f;             // 페이드 아웃 시간(초)

    private readonly int[] judgedCounts = new int[4]; // Judgement 순서 (Perfect, Great, Good, Miss)
    private float finishedElapsed = -1f;               // 종료 조건을 만족한 뒤 지난 시간 (-1: 아직 만족 전)
    private bool loading;

    private void OnEnable()
    {
        if (gameplay != null) gameplay.OnJudged += CountJudged;
    }

    private void OnDisable()
    {
        if (gameplay != null) gameplay.OnJudged -= CountJudged;
    }

    // 시작 시 페이드 이미지를 투명하게 둔다.
    private void Start()
    {
        SetFadeAlpha(0f);
    }

    private void Update()
    {
        if (loading || gameplay == null) return;

        // 음악이 먼저 끝나도 모든 노트의 판정과 화면 표시가 끝날 때까지 기다린다.
        if (finishedElapsed < 0f)
        {
            if (!gameplay.IsFinished || !gameplay.IsMusicFinished) return;
            finishedElapsed = 0f;
        }

        finishedElapsed += Time.deltaTime;
        float fadeProgress = (finishedElapsed - waitBeforeFade) / fadeDuration;
        if (fadeProgress <= 0f) return;

        SetFadeAlpha(Mathf.Clamp01(fadeProgress));
        if (fadeProgress >= 1f) LoadResult();
    }

    // 결과를 바뀌지 않는 객체로 복사해 저장하고 결과 씬을 불러온다.
    private void LoadResult()
    {
        loading = true;
        int score = scoreCombo != null ? scoreCombo.Score : 0;
        int maxCombo = scoreCombo != null ? scoreCombo.MaxCombo : 0;
        PlayResult.Last = new PlayResult(score, maxCombo, judgedCounts[0], judgedCounts[1], judgedCounts[2], judgedCounts[3]);
        SceneManager.LoadScene(resultSceneName);
    }

    // 판정 1회마다 등급별 개수를 센다 (틱은 OnTick이라 포함되지 않음).
    private void CountJudged(Judgement judgement, int lane)
    {
        judgedCounts[(int)judgement]++;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null) return;
        Color c = fadeOverlay.color;
        fadeOverlay.color = new Color(c.r, c.g, c.b, alpha);
    }
}
