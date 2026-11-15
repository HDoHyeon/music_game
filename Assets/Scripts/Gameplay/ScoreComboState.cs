using UnityEngine;
using UnityEngine.UI;

// 판정 결과를 받아 점수와 콤보를 계산하고 화면 Text에 표시한다.
public class ScoreComboState : MonoBehaviour
{
    [SerializeField] private GameplayManager gameplay; // 판정 이벤트(OnJudged)를 보내는 쪽
    [SerializeField] private Text scoreText;           // 오른쪽 위 점수 표시
    [SerializeField] private Text comboText;           // 화면 중앙 위쪽 콤보 표시 (콤보 0이면 숨김)

    // 현재 점수와 콤보 (결과 화면 등에서 읽기 전용으로 사용)
    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; } // 플레이 중 가장 높았던 콤보 (틱 포함)

    private void OnEnable()
    {
        if (gameplay != null)
        {
            gameplay.OnJudged += HandleJudged;
            gameplay.OnTick += HandleTick;
        }
    }

    private void OnDisable()
    {
        if (gameplay != null)
        {
            gameplay.OnJudged -= HandleJudged;
            gameplay.OnTick -= HandleTick;
        }
    }

    // 플레이 시작 시 점수와 콤보를 초기화한다.
    private void Start()
    {
        ResetState();
    }

    private void ResetState()
    {
        Score = 0;
        Combo = 0;
        MaxCombo = 0;
        RefreshTexts();
    }

    // 판정 1회마다 점수를 더하고, Miss면 콤보를 끊고 그 외에는 콤보를 1 올린다.
    private void HandleJudged(Judgement judgement, int lane)
    {
        Score += GetPoints(judgement);
        Combo = judgement == Judgement.Miss ? 0 : Combo + 1;
        if (Combo > MaxCombo) MaxCombo = Combo;
        RefreshTexts();
    }

    // 롱노트 틱 1회마다 시작 판정 등급 점수의 30%를 더하고 콤보를 1 올린다.
    // (Perfect 300 / Great 210 / Good 90)
    private void HandleTick(Judgement headJudgement, int lane)
    {
        Score += GetPoints(headJudgement) * 30 / 100;
        Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;
        RefreshTexts();
    }

    // 판정 등급별 점수
    private static int GetPoints(Judgement judgement)
    {
        switch (judgement)
        {
            case Judgement.Perfect: return 1000;
            case Judgement.Great: return 700;
            case Judgement.Good: return 300;
            default: return 0;
        }
    }

    // 현재 점수·콤보를 Text에 반영한다. 콤보가 0이면 콤보 Text를 숨긴다.
    private void RefreshTexts()
    {
        if (scoreText != null) scoreText.text = Score.ToString();
        if (comboText != null)
        {
            comboText.gameObject.SetActive(Combo > 0);
            comboText.text = Combo + " COMBO";
        }
    }
}
