using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 탭 보정(자동 싱크) 화면.
// 120 BPM 메트로놈에 맞춰 인게임과 같은 4레인에 노트가 레인 1→2→3→4 순서로 내려오고, 준비 4박 뒤 16박 동안 노트가 있는 레인의 키(기본 D/F/J/K)를 친다.
// 누를 때마다 그 박자와의 차이를 기록하고(±250ms 밖·다른 레인 키는 제외), 차이들의 중앙값을 5ms 단위로 맞춰 오프셋 결과로 보여 준다.
// 늦게 치면 + (OffsetSetting의 부호와 같음). APPLY로 저장, RETRY로 다시 측정, CANCEL/Esc로 저장 없이 설정 화면으로 돌아간다. (APPLY 후에도 설정 화면)
// 측정 시간은 메트로놈의 재생 위치(audioSource.time) 기준이며, 저장된 오프셋은 적용하지 않는다.
public class OffsetCalibration : MonoBehaviour
{
    public const double BeatInterval = 0.5;  // 120 BPM
    public const int LeadInBeats = 4;        // 준비 박자 (세지 않음)
    public const int CountedBeats = 16;      // 세는 박자
    public const double FirstBeatTime = 2.0; // 클립 시작부터 첫 박자까지 (첫 노트가 생성 높이에서 내려올 시간, x1.0에서도 대부분 보이도록)
    public const double MaxError = 0.25;     // 박자에서 ±250ms보다 멀면 실수로 보고 제외
    public const int MinValidTaps = 8;       // 결과를 내기 위한 최소 유효 입력 수
    private const int SampleRate = 44100;
    private const double ClipTail = 1.0;     // 마지막 박자 뒤 여유

    [Header("노트 (GameplayManager 인스펙터 값과 같게 유지)")]
    [SerializeField] private Note notePrefab;
    [SerializeField] private float fallSpeed = 5f;   // 기준 배속(x3.0)일 때 낙하 속도(유닛/초)
    [SerializeField] private float spawnY = 6f;      // 노트가 나타나는 높이
    [SerializeField] private float judgeLineY = -3f; // 판정선 Y
    [SerializeField] private float[] laneXs = { -1.5f, -0.5f, 0.5f, 1.5f };

    [Header("소리 / UI")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Text counterText;       // N / 16
    [SerializeField] private Text currentOffsetText; // 지금 저장된 오프셋
    [SerializeField] private Text guideText;         // 안내 문구 (실제 레인 키 이름 표시)
    [SerializeField] private GameObject resultPanel; // 결과와 버튼 (측정 중에는 숨김)
    [SerializeField] private Text resultText;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private string settingsSceneName = "SettingsScene"; // APPLY / CANCEL / Esc 후 돌아갈 씬 (설정 화면)

    private readonly List<double> errors = new List<double>();    // 유효 입력의 박자 차이(초)
    private readonly HashSet<int> tappedBeats = new HashSet<int>(); // 이미 센 박자 (한 박자에 1번만)
    private readonly List<Note> notes = new List<Note>();
    private bool showingResult;
    private bool loading;
    private int resultMs;  // 측정 결과(ms, 5ms 단위, ±200 제한)

    public bool IsShowingResult => showingResult;
    public int ValidTapCount => errors.Count;
    public int ResultMs => resultMs;
    public bool HasResult => errors.Count >= MinValidTaps;

    private static int TotalBeats => LeadInBeats + CountedBeats;
    private static double LastBeatTime => FirstBeatTime + (TotalBeats - 1) * BeatInterval;
    private int LaneCount => Mathf.Min(KeyBindings.LaneCount, laneXs.Length);
    // 배속을 반영한 실제 낙하 속도 (GameplayManager.FallSpeed와 같은 식)
    private float FallSpeed => fallSpeed * NoteSpeed.Value / NoteSpeed.Default;

    // 박자 k의 노트 레인: 1→2→3→4 순서로 반복
    public int LaneOfBeat(int beat) => beat % LaneCount;

    // 메트로놈 클립과 박자별 노트를 만들고 버튼을 연결한 뒤 측정을 시작한다.
    private void Start()
    {
        audioSource.clip = CreateMetronomeClip();
        audioSource.volume = AudioVolume.Sfx01; // 메트로놈은 SFX 볼륨 (MASTER는 AudioListener가 곱함)
        for (int i = 0; i < TotalBeats; i++)
        {
            Note note = Instantiate(notePrefab, transform);
            note.name = "Note" + i;
            note.Init(new NoteData(FirstBeatTime + i * BeatInterval, LaneOfBeat(i)), laneXs[LaneOfBeat(i)]);
            notes.Add(note);
        }
        if (applyButton != null) applyButton.onClick.AddListener(Apply);
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        if (currentOffsetText != null) currentOffsetText.text = "CURRENT " + OffsetSetting.Label;
        if (guideText != null)
        {
            var keys = new List<string>();
            for (int lane = 0; lane < LaneCount; lane++) keys.Add(KeyBindings.Get(lane).ToString());
            guideText.text = "PRESS THE LANE KEY ON THE BEAT  (" + string.Join(" ", keys) + ")   ESC: CANCEL";
        }
        BeginMeasure();
    }

    private void Update()
    {
        if (loading) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { Cancel(); return; }
        if (showingResult) return;

        double t = audioSource.time;
        if (keyboard != null)
            for (int lane = 0; lane < LaneCount; lane++)
                if (keyboard[KeyBindings.Get(lane)].wasPressedThisFrame) RegisterTap(lane, t);
        UpdateNotes(t);
        UpdateCounter(t);
        // 마지막 박자 판정 범위가 지나면 결과를 보여 준다. (클립은 그보다 0.75초 더 길어 끝나기 전에 도달한다)
        if (t > LastBeatTime + MaxError) ShowResult();
    }

    // 측정을 처음부터 시작한다.
    private void BeginMeasure()
    {
        errors.Clear();
        tappedBeats.Clear();
        showingResult = false;
        if (resultPanel != null) resultPanel.SetActive(false);
        audioSource.Stop();
        audioSource.time = 0f;
        audioSource.Play();
        UpdateNotes(0);
        UpdateCounter(0);
    }

    // 레인 키를 누른 순간의 메트로놈 재생 위치로 가장 가까운 '세는 박자'와의 차이를 기록한다.
    // 준비 박자·범위 밖·노트와 다른 레인·±250ms 밖·이미 센 박자는 무시한다. (테스트에서 직접 호출할 수 있게 public)
    public void RegisterTap(int lane, double audioTime)
    {
        if (showingResult) return;
        int beat = (int)System.Math.Round((audioTime - FirstBeatTime) / BeatInterval);
        if (beat < LeadInBeats || beat >= TotalBeats) return;
        if (lane != LaneOfBeat(beat)) return;
        double error = audioTime - (FirstBeatTime + beat * BeatInterval);
        if (System.Math.Abs(error) > MaxError) return;
        if (!tappedBeats.Add(beat)) return;
        errors.Add(error);
        if (beat < notes.Count) notes[beat].gameObject.SetActive(false); // 인게임처럼 맞게 친 노트는 바로 사라진다
    }

    // 측정을 끝내고 결과를 보여 준다. 중앙값(ms)을 5ms 단위로 맞추고 ±200ms로 자른다.
    public void ShowResult()
    {
        if (showingResult) return;
        showingResult = true;
        audioSource.Stop();
        foreach (Note note in notes) note.gameObject.SetActive(false);

        string text;
        if (HasResult)
        {
            int rounded = Mathf.RoundToInt((float)(Median(errors) * 1000.0) / OffsetSetting.Step) * OffsetSetting.Step;
            resultMs = Mathf.Clamp(rounded, OffsetSetting.Min, OffsetSetting.Max);
            text = "RESULT " + resultMs.ToString("+0;-0;0") + " ms";
            if (rounded != resultMs) text += "\nOUT OF RANGE (LIMIT " + OffsetSetting.Min + " ~ +" + OffsetSetting.Max + " ms)";
            text += "\nVALID TAPS " + errors.Count + " / " + CountedBeats;
        }
        else
        {
            resultMs = 0;
            text = "NOT ENOUGH TAPS\nVALID TAPS " + errors.Count + " / " + CountedBeats + " (NEED " + MinValidTaps + ")";
        }
        if (resultText != null) resultText.text = text;
        if (applyButton != null) applyButton.interactable = HasResult;
        if (resultPanel != null) resultPanel.SetActive(true);
    }

    // 결과를 오프셋으로 저장하고 설정 화면으로 돌아간다.
    public void Apply()
    {
        if (!showingResult || !HasResult || loading) return;
        OffsetSetting.Set(resultMs);
        LoadSettings();
    }

    // 기록을 지우고 처음부터 다시 측정한다.
    public void Retry()
    {
        if (loading) return;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); // 선택된 버튼이 키 입력으로 다시 눌리지 않게
        BeginMeasure();
    }

    // 저장하지 않고 설정 화면으로 돌아간다.
    public void Cancel()
    {
        if (loading) return;
        LoadSettings();
    }

    // 저장 여부와 상관없이 설정 화면으로 돌아간다.
    private void LoadSettings()
    {
        loading = true;
        audioSource.Stop();
        SceneManager.LoadScene(settingsSceneName);
    }

    // 박자마다 노트 1개: 인게임과 같은 식(Note.UpdatePosition)으로 박자 시각에 판정선에 닿게 한다.
    // 생성 높이보다 위이거나, 판정선을 0.1초 넘게 지났거나, 이미 맞게 친 박자의 노트는 숨긴다.
    private void UpdateNotes(double t)
    {
        float speed = FallSpeed;
        double leadTime = (spawnY - judgeLineY) / speed; // 생성 높이에서 판정선까지 걸리는 시간
        for (int i = 0; i < notes.Count; i++)
        {
            double beatTime = FirstBeatTime + i * BeatInterval;
            bool visible = beatTime - t <= leadTime && t <= beatTime + 0.1 && !tappedBeats.Contains(i);
            notes[i].gameObject.SetActive(visible);
            if (visible) notes[i].UpdatePosition(t, judgeLineY, speed);
        }
    }

    // 지나간 '세는 박자' 수를 N / 16으로 표시한다 (준비 박자 동안은 READY).
    private void UpdateCounter(double t)
    {
        if (counterText == null) return;
        int passed = (int)System.Math.Floor((t - FirstBeatTime) / BeatInterval) + 1 - LeadInBeats;
        counterText.text = passed <= 0 ? "READY" : Mathf.Min(passed, CountedBeats) + " / " + CountedBeats;
    }

    private static double Median(List<double> values)
    {
        var sorted = new List<double>(values);
        sorted.Sort();
        int n = sorted.Count;
        return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) * 0.5;
    }

    // 메트로놈 클립: 박자마다 짧은 클릭(감쇠하는 사인파). 준비 박자는 낮은 음, 세는 박자는 높은 음.
    private static AudioClip CreateMetronomeClip()
    {
        int length = (int)((LastBeatTime + ClipTail) * SampleRate);
        float[] data = new float[length];
        int clickLength = (int)(0.04 * SampleRate);
        for (int b = 0; b < TotalBeats; b++)
        {
            int start = (int)((FirstBeatTime + b * BeatInterval) * SampleRate);
            float freq = b < LeadInBeats ? 880f : 1320f;
            for (int i = 0; i < clickLength && start + i < length; i++)
            {
                float x = i / (float)SampleRate;
                data[start + i] = 0.6f * Mathf.Sin(2f * Mathf.PI * freq * x) * Mathf.Exp(-x * 90f);
            }
        }
        AudioClip clip = AudioClip.Create("Metronome", length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
