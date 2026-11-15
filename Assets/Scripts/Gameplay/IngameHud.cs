using UnityEngine;
using UnityEngine.UI;

// 인게임 화면 표시 전용(HUD). 판정·점수·콤보 계산은 하지 않고 GameplayManager / ScoreComboState 값을 읽기만 한다.
// - 콤보: 콤보가 오를 때마다 숫자를 확대했다가 원래 크기로 줄이고, 뒤에 터지는 이펙트를 띄운다.
// - 판정: OnJudged마다 PERFECT/GREAT/GOOD/MISS 이미지를 잠깐 보여 준다. (롱노트 틱은 표시하지 않음)
// - 진행 시간: 화면 위 진행 바와 "mm:ss / mm:ss" (실제 음악 위치 기준, 오프셋·배속과 무관)
// - 레인 입력 빛: 누르고 있는 레인의 빛 기둥을 켠다. AUTO의 짧은 입력은 판정 순간 잠깐 빛나게 한다.
// 일시정지·재개 카운트다운(gameplay.IsPaused) 중에는 모든 표시가 멈추고, 레인 입력 빛만 꺼진다. Restart는 씬을 다시 불러오므로 따로 초기화하지 않는다.
public class IngameHud : MonoBehaviour
{
    private const float PulseScale = 1.25f;       // 콤보 증가 순간 확대 배율
    private const float PulseDuration = 0.18f;    // 확대 → 원래 크기 시간(초)
    private const float BurstDuration = 0.25f;    // 콤보 이펙트가 퍼지며 사라지는 시간(초)
    private const float BurstAlpha = 0.3f;        // 콤보 이펙트 시작 투명도 (노트를 가리지 않게 낮춤)
    private const float JudgementDuration = 0.4f; // 판정 이미지 표시 시간(초)
    private const float JudgementPop = 0.08f;     // 판정 이미지 등장 확대 시간(초)
    private const float JudgementPopScale = 1.15f;
    private const float JudgementAlpha = 0.7f;    // 판정 이미지 최대 투명도 (뒤의 노트가 보이게)
    private const float JudgementFade = 0.1f;     // 판정 이미지가 사라지기 전 옅어지는 시간(초)
    private const float GlowAlpha = 0.45f;        // 레인 입력 빛 투명도
    private const float GlowFade = 0.1f;          // 손을 뗀 뒤 빛이 꺼지는 시간(초)

    [SerializeField] private GameplayManager gameplay;
    [SerializeField] private ScoreComboState scoreCombo;

    [Header("콤보")]
    [SerializeField] private RectTransform comboRect;  // ScoreComboState가 숫자를 쓰는 ComboText
    [SerializeField] private Image comboBurst;         // 콤보 뒤 터지는 이펙트 (색은 인스펙터 값, 투명도만 바꿈)

    [Header("판정")]
    [SerializeField] private Image judgementImage;
    [SerializeField] private Sprite[] judgementSprites = new Sprite[4]; // Judgement 순서: Perfect, Great, Good, Miss
    // 판정 이미지 폭(월드 유닛). 가운데 두 레인(F·J, x −1 ~ 1, 폭 2) 안에 들어가도록, 등장 확대(1.15배)까지 포함해 2보다 작게
    [SerializeField] private float judgementWorldWidth = 1.7f;

    [Header("진행 시간")]
    [SerializeField] private Image progressFill;       // Image Type Filled (Horizontal)
    [SerializeField] private Text timeText;

    [Header("레인 입력 빛")]
    [SerializeField] private SpriteRenderer[] laneGlows = new SpriteRenderer[4];

    private Vector3 comboBaseScale = Vector3.one;
    private int previousCombo;
    private float comboElapsed = BurstDuration;   // 콤보 애니메이션 경과 시간 (끝난 상태로 시작)
    private float judgementElapsed = JudgementDuration;
    private float[] glowAlphas;
    private bool musicStarted;                    // 음악이 한 번이라도 재생됐는지 (시작 전 0과 끝난 뒤 0으로 돌아간 것을 구분)

    private void OnEnable()
    {
        if (gameplay != null) gameplay.OnJudged += HandleJudged;
    }

    private void OnDisable()
    {
        if (gameplay != null) gameplay.OnJudged -= HandleJudged;
    }

    private void Start()
    {
        if (comboRect != null) comboBaseScale = comboRect.localScale;
        if (judgementImage != null) judgementImage.gameObject.SetActive(false);
        glowAlphas = new float[laneGlows.Length];
        ApplyBurst(1f);
        ApplyGlows();
        UpdateProgress();
    }

    // 판정 1회마다 해당 등급 이미지로 바꾸고 표시 시간을 처음부터 센다. 같은 프레임에 여러 번이면 마지막 판정이 남는다.
    private void HandleJudged(Judgement judgement, int lane)
    {
        int index = (int)judgement;
        if (judgementImage != null && index < judgementSprites.Length && judgementSprites[index] != null)
        {
            judgementImage.sprite = judgementSprites[index];
            FitJudgement();
            judgementImage.gameObject.SetActive(true);
            judgementElapsed = 0f;
        }
        // AUTO의 일반 노트처럼 누르는 시간이 거의 없는 입력도 보이도록, 맞힌 레인은 판정 순간 빛나게 한다
        if (judgement != Judgement.Miss && glowAlphas != null && lane >= 0 && lane < glowAlphas.Length) glowAlphas[lane] = GlowAlpha;
    }

    // 점수·콤보 갱신(판정 이벤트)이 끝난 뒤의 값을 읽도록 LateUpdate에서 처리한다.
    private void LateUpdate()
    {
        if (gameplay == null) return;
        if (gameplay.IsPaused)
        {
            // 정지·카운트다운 중에는 모두 멈춤. 레인 입력 빛만은 끈다 (정지 중 누른 키가 빛나 보이지 않게)
            if (glowAlphas != null) System.Array.Clear(glowAlphas, 0, glowAlphas.Length);
            ApplyGlows();
            return;
        }
        float dt = Time.unscaledDeltaTime;                 // 표시용 타이머 (곡 시간 계산에는 쓰지 않음)
        UpdateCombo(dt);
        UpdateJudgement(dt);
        UpdateProgress();
        UpdateGlows(dt);
    }

    // 콤보가 오른 프레임에 확대와 이펙트를 처음부터 다시 재생한다. 항상 기본 크기 기준이라 연속으로 올라도 커지지 않는다.
    private void UpdateCombo(float dt)
    {
        if (scoreCombo == null || comboRect == null) return;
        int combo = scoreCombo.Combo;
        if (combo > previousCombo) comboElapsed = 0f;            // 증가(틱 포함): 처음부터
        else if (combo == 0) comboElapsed = BurstDuration;       // Miss로 끊김: 이펙트 끝 상태로
        previousCombo = combo;

        float p = Mathf.Clamp01(comboElapsed / PulseDuration);
        comboRect.localScale = comboBaseScale * (1f + (PulseScale - 1f) * (1f - p) * (1f - p)); // ease-out
        ApplyBurst(Mathf.Clamp01(comboElapsed / BurstDuration));
        comboElapsed = Mathf.Min(BurstDuration, comboElapsed + dt);
    }

    // 콤보 이펙트: 진행도 b(0~1)에 따라 커지면서 옅어진다.
    private void ApplyBurst(float b)
    {
        if (comboBurst == null) return;
        Color c = comboBurst.color;
        c.a = BurstAlpha * (1f - b);
        comboBurst.color = c;
        comboBurst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.3f, b);
    }

    // 판정 이미지: 처음에 살짝 크게 나타났다가 원래 크기, 끝나기 직전 옅어지고 숨김.
    private void UpdateJudgement(float dt)
    {
        if (judgementImage == null || !judgementImage.gameObject.activeSelf) return;
        judgementElapsed += dt;
        if (judgementElapsed >= JudgementDuration)
        {
            judgementImage.gameObject.SetActive(false);
            return;
        }
        float pop = Mathf.Clamp01(judgementElapsed / JudgementPop);
        judgementImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(JudgementPopScale, 1f, pop);
        Color c = judgementImage.color;
        c.a = JudgementAlpha * Mathf.Clamp01((JudgementDuration - judgementElapsed) / JudgementFade);
        judgementImage.color = c;
    }

    // 판정 이미지 크기를 화면 해상도와 상관없이 월드 폭 judgementWorldWidth에 맞춘다 (높이는 스프라이트 비율대로).
    // 카메라로 월드 폭을 화면 픽셀로 바꾼 뒤 Canvas 배율로 나눠 UI 크기로 쓴다.
    private void FitJudgement()
    {
        Camera cam = Camera.main;
        Canvas canvas = judgementImage.canvas;
        Sprite sprite = judgementImage.sprite;
        if (cam == null || canvas == null || sprite == null) return;
        float half = judgementWorldWidth * 0.5f;
        float screenWidth = cam.WorldToScreenPoint(new Vector3(half, 0f, 0f)).x - cam.WorldToScreenPoint(new Vector3(-half, 0f, 0f)).x;
        float width = screenWidth / canvas.scaleFactor;
        judgementImage.rectTransform.sizeDelta = new Vector2(width, width * sprite.rect.height / sprite.rect.width);
    }

    // 진행 바와 시간 글자. 음악이 끝나면(한 번 재생된 뒤 멈추고 재생 위치가 0으로 돌아감) 100%로 고정, 시작 전은 0%.
    private void UpdateProgress()
    {
        if (gameplay == null) return;
        double duration = gameplay.MusicDuration;
        double position = gameplay.MusicPosition;
        if (gameplay.IsMusicPlaying) musicStarted = true;
        else if (musicStarted && position <= 0.0) position = duration;
        position = System.Math.Max(0.0, System.Math.Min(position, duration));
        if (progressFill != null) progressFill.fillAmount = duration > 0 ? (float)(position / duration) : 0f;
        if (timeText != null) timeText.text = FormatTime(position) + " / " + FormatTime(duration);
    }

    // 초 → "mm:ss" (초 단위 내림)
    private static string FormatTime(double seconds)
    {
        int total = (int)System.Math.Floor(seconds);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    // 누르고 있으면 빛을 켜고, 떼면 GlowFade초 동안 꺼진다.
    private void UpdateGlows(float dt)
    {
        if (glowAlphas == null) return;
        for (int lane = 0; lane < glowAlphas.Length; lane++)
            glowAlphas[lane] = gameplay.IsLaneHeld(lane) ? GlowAlpha : Mathf.Max(0f, glowAlphas[lane] - dt * GlowAlpha / GlowFade);
        ApplyGlows();
    }

    private void ApplyGlows()
    {
        for (int lane = 0; lane < laneGlows.Length; lane++)
        {
            if (laneGlows[lane] == null) continue;
            Color c = laneGlows[lane].color;
            c.a = glowAlphas != null && lane < glowAlphas.Length ? glowAlphas[lane] : 0f;
            laneGlows[lane].color = c;
        }
    }
}
