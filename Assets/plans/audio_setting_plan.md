# 사운드 및 볼륨 설정 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 대상: PLAN.md 2-3. 사용자 편의성 및 기록 관리 / 2. 사운드 및 볼륨 설정
- 목적: 설정 화면에 AUDIO SETTING 패널을 추가해 게임 소리 크기를 조절한다.
- 상태: 구현 완료(2026-11-16) / 사용자 확인 대기 (실제 슬라이더 조작, 소리 크기 청취, 두 곡 크기 비교)
- 작성 경위: GPT가 만든 작업 프롬프트(사용자 제공, 2026-11-16)를 로컬 소스·씬과 대조해 작성.
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항 (사용자 프롬프트 기준)
| 항목 | 내용 |
|---|---|
| 진입 | 설정 화면(SettingsScene)에 AUDIO SETTING 버튼 추가 → 키·오프셋 패널과 같은 형태의 팝업 패널. CLOSE로 설정 화면 |
| 볼륨 종류 | MASTER(전체), MUSIC(곡 선택 미리듣기·인게임 음악), SFX(효과음) |
| 조절 | 슬라이더 0~100%, 현재 값 숫자(%) 표시, 바꾸면 바로 반영 |
| 기본값·RESET | 모두 100%, RESET으로 세 값 모두 100% |
| 저장 | PlayerPrefs, 다시 실행해도 유지 |
| 보호 | 음악 재생 위치·노트 싱크·판정·오프셋에 영향 없음. 기존 키 설정·오프셋·배속·곡 선택·미리듣기 유지. 음원·채보 수정 없음 |

### 추가 요구사항 — 곡 기본 볼륨 (2026-11-16, 사용자 요청)
- kick은 소리가 작고 희망은 너무 커서 곡끼리 크기 차이가 난다 → 조절한다.
- 측정 (전체 길이, 소리 나는 부분 기준):

| 곡 | RMS | 저역 무게를 낮춘 체감 크기(0.1×저음 + 중음 + 고음 에너지) |
|---|---|---|
| kick | −14.7 dBFS | −16.1 dB |
| 희망 | −12.1 dBFS | −12.0 dB |

  - 희망은 중·고음이 훨씬 많아 RMS 차이(약 2.6 dB)보다 체감 차이(약 4.1 dB)가 크다. 희망을 약 0.62배로 줄이면 kick과 비슷해진다 → **0.6**으로 정한다.
- 방식: 음원 파일은 바꾸지 않고, `SongData`에 **곡 기본 볼륨**(0~1, 기본 1) 칸을 추가해 재생할 때 곱한다. 실제 크기 = MASTER × MUSIC × 곡 기본 볼륨.
  - 음원을 다시 만들지 않으므로 원본이 보존되고, 인스펙터에서 곡마다 다시 조절할 수 있다.
  - 기존 곡(Test Song 1~3, kick)은 기본값 1이라 지금과 같다.
- **중요 — 사용자 승인(요청)으로 추가**: `SongData.cs`에 직렬화 칸 1개 추가 → 기존 곡 에셋은 값이 없으면 1로 읽혀 영향 없음.

### 제외 범위
- 새 효과음 추가(효과음 시스템이 없으므로 — 2절), AudioMixer 에셋, 음소거 버튼, ~~곡별 볼륨~~(2026-11-16 사용자 요청으로 "곡 기본 볼륨" 추가, 아래), 사용자가 곡마다 조절하는 UI, 인게임·일시정지 메뉴에서의 볼륨 조절, 신규 패키지·에셋

## 2. 기존 코드 분석 (로컬 확인 2026-11-16)
- **소리를 내는 곳은 3개뿐** (Scripts 전체 `AudioSource`·`PlayOneShot` 검색, 씬 파일 확인):

| 씬 | AudioSource | 사용 스크립트 | 내용 |
|---|---|---|---|
| SongSelectScene | `SongPreviewAudio` | `SongSelectScreen.previewSource` | 미리듣기. `PlayPreview`에서 clip 교체 후 0초 재생 |
| IngameScene | `Music` | `GameplayManager.audioSource` | 곡 음악. 곡 시간 = `audioSource.time − 오프셋` |
| OffsetCalibrationScene | `OffsetCalibration` 오브젝트 | `OffsetCalibration.audioSource` | 코드로 만든 메트로놈 클립. 측정 시간 = `audioSource.time` |

  - 세 AudioSource 모두 Volume 1, Play On Awake 끔. ResultScene·SettingsScene에는 AudioSource가 없다(설정 화면에서는 소리가 나지 않음).
  - AudioListener는 소리 나는 씬마다 1개 (SettingsScene에도 1개, ResultScene에는 없음).
- **효과음(SFX) 시스템은 없다.** 판정음·버튼 클릭음·키음 등 효과음 재생 코드와 효과음 에셋이 없다. 소리는 음악 2곳과 메트로놈 1곳뿐이다.
- **AudioMixer 없음** (프로젝트에 `.mixer` 파일 없음). ProjectSettings/AudioManager의 전체 볼륨 1.
- **설정 저장 방식**: `OffsetSetting`·`NoteSpeed`·`KeyBindings`가 같은 구조 — static 클래스, 처음 읽을 때 PlayerPrefs에서 불러오고, 바꾸면 바로 저장, Play 시작 시(SubsystemRegistration) 캐시 초기화.
- **SettingsScreen.cs**: `keySettingPanel`, 오프셋 패널(`offsetPanelRoot` 등), BACK. 한 패널이 열려 있으면 BACK·다른 패널 열기를 무시. 패널 바탕(전체 화면 반투명 Image)이 뒤 버튼 클릭을 막는다.
- **SettingsScene 배치**: KEY SETTING (0, 70), OFFSET SETTING (0, −80), 각 480×110. 패널은 Box 900×820(키) / 900×560(오프셋).
- 볼륨을 바꿔도 `AudioSource.time`(재생 위치)은 변하지 않으므로, 곡 시간·판정·오프셋·보정 측정에는 영향이 없다.

## 3. 작업 범위 및 수정 대상 파일
### 방식 제안: AudioMixer 대신 기존 AudioSource.volume + AudioListener.volume
- 소리 나는 곳이 3곳뿐이고 모두 스크립트가 AudioSource를 직접 들고 있어, 각 스크립트에서 `volume`만 지정하면 된다.
- MASTER는 `AudioListener.volume`(게임 전체 볼륨, 씬이 바뀌어도 유지)에 적용한다. 앞으로 소리가 늘어나도 자동으로 적용된다.
- AudioMixer는 믹서 에셋을 새로 만들고 AudioSource마다 출력 그룹을 연결해야 해서, 지금 규모에서는 이득이 없다. 효과음이 많아지면 그때 다시 검토한다.

| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Song/AudioVolume.cs` | 신규 | MASTER·MUSIC·SFX 값(0~100), PlayerPrefs 저장·불러오기, RESET, MASTER를 `AudioListener.volume`에 적용 |
| `Assets/Scripts/Song/SettingsScreen.cs` | 수정 | AUDIO SETTING 패널 열기·닫기, 슬라이더·% 표시·RESET 연결, 패널끼리 막기에 오디오 패널 추가 |
| `Assets/Scenes/SettingsScene.unity` | 수정 | AUDIO SETTING 버튼(0, −230), 오디오 패널(슬라이더 3줄, RESET, CLOSE) |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 1줄 | `PlayPreview`에서 `previewSource.volume = AudioVolume.Music01 * song.Volume` |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | `Start`에서 음악 재생 전 `audioSource.volume = AudioVolume.Music01 * 곡 기본 볼륨` (곡 선택 없이 단독 실행이면 1) |
| `Assets/Scripts/Song/SongData.cs` | 수정 | 곡 기본 볼륨 `volume`(0~1, 기본 1, `[Range]`) 칸과 `Volume` 속성 추가 |
| `Assets/Songs/Huimang.asset` | 수정 | 곡 기본 볼륨 0.6 |
| `Assets/Scripts/Song/OffsetCalibration.cs` | 수정 1줄 | `Start`에서 메트로놈 `audioSource.volume = AudioVolume.Sfx01` |

- 수정하지 않음: KeySettingPanel, KeyBindings, OffsetSetting, NoteSpeed, PauseController, ResultTransition, 음원·채보, 다른 씬 배치.
- PLAN.md: 2-3 #2 상태 칸만 바꾼다(미작성 → 미확인). 기능 목록 추가 없음.

### 사용자 결정 (2026-11-16)
1. **메트로놈 볼륨 = SFX** (확정). 메트로놈은 효과음 성격이고 지금 유일한 효과음이라 SFX 슬라이더가 실제로 쓰인다. 실제 크기 = MASTER × SFX.
2. **SFX 범위**: 효과음 시스템이 없으므로 이번에는 SFX 값을 저장·표시하고 메트로놈에만 적용한다. 앞으로 효과음(판정음 등)을 추가하면 그 AudioSource에 `AudioVolume.Sfx01`을 쓰면 된다(4.5절).
3. **설정 화면에서 바로 들어 보기**: 설정 화면에는 소리가 없어서 슬라이더를 움직여도 그 자리에서는 들리지 않는다(값은 바로 저장되고, 다른 화면으로 가면 적용). 시험음 재생은 요청 범위 밖이라 넣지 않는다.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
### 4.1 AudioVolume.cs (신규, OffsetSetting과 같은 구조)
```csharp
using UnityEngine;

// 볼륨 설정: MASTER / MUSIC / SFX, 각 0~100(%), 기본 100. PlayerPrefs에 저장해 다시 실행해도 유지된다.
// MASTER는 AudioListener.volume(게임 전체)에 적용하고, MUSIC·SFX는 각 AudioSource가 재생 전에 volume으로 쓴다.
// 볼륨은 재생 위치(AudioSource.time)를 바꾸지 않으므로 곡 시간·판정·오프셋에 영향이 없다.
public static class AudioVolume
{
    public const int Min = 0, Max = 100, Default = 100;
    private static readonly string[] Keys = { "MasterVolume", "MusicVolume", "SfxVolume" };
    private static readonly int[] values = new int[3];
    private static bool loaded;

    public static int Master => Get(0);
    public static int Music => Get(1);
    public static int Sfx => Get(2);
    public static float Music01 => Music / 100f; // AudioSource.volume용 (MASTER는 AudioListener에서 곱해짐)
    public static float Sfx01 => Sfx / 100f;

    public static int Get(int kind) { EnsureLoaded(); return values[kind]; }

    // 값을 저장하고, MASTER면 바로 AudioListener에 적용한다.
    public static void Set(int kind, int v)
    {
        EnsureLoaded();
        values[kind] = Mathf.Clamp(v, Min, Max);
        PlayerPrefs.SetInt(Keys[kind], values[kind]);
        PlayerPrefs.Save();
        if (kind == 0) ApplyMaster();
    }

    public static void ResetAll() { for (int i = 0; i < 3; i++) Set(i, Default); }

    public static void ApplyMaster() { AudioListener.volume = Master / 100f; }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        for (int i = 0; i < 3; i++) values[i] = Mathf.Clamp(PlayerPrefs.GetInt(Keys[i], Default), Min, Max);
        loaded = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() { loaded = false; }

    // 게임 시작 시 저장된 MASTER를 적용한다 (첫 씬이 소리를 내기 전에)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStart() { ApplyMaster(); }
}
```
- MUSIC·SFX는 MASTER를 곱하지 않는다. MASTER는 `AudioListener.volume`이 모든 소리에 한 번 곱해 주므로 실제 크기 = MASTER × MUSIC(또는 SFX).
- `AudioListener.volume`은 씬과 상관없는 전역 값이라, Play(또는 게임) 시작 때 `ApplyOnStart`가 저장값으로 맞춘다. 에디터에서 Play를 끝낸 뒤 이 값이 어떻게 남는지는 구현 때 확인한다.

### 4.2 기존 AudioSource에 적용
```csharp
// SongData: 곡 기본 볼륨 (곡끼리 소리 크기 차이를 맞춤, 음원은 그대로)
[SerializeField, Range(0f, 1f)] private float volume = 1f;
public float Volume => volume;

// SongSelectScreen.PlayPreview (clip 지정 뒤, Play 전)
previewSource.volume = AudioVolume.Music01 * song.Volume;

// GameplayManager.Start (audioSource.Play() 전)
if (audioSource != null) audioSource.volume = AudioVolume.Music01 * (song != null ? song.Volume : 1f);

// OffsetCalibration.Start (BeginMeasure 전) — 메트로놈은 SFX 볼륨 (사용자 결정)
audioSource.volume = AudioVolume.Sfx01;
```
- 재생 중에 볼륨을 바꿀 수 있는 화면이 없으므로(볼륨은 설정 화면에서만 조절, 그 화면에는 소리가 없음) 재생 시작 시 한 번 지정하면 된다.

### 4.3 SettingsScene 오디오 패널 (좌표는 스크린샷 보며 조정)
| 오브젝트 | 내용 |
|---|---|
| AudioSettingButton | (0, −230), 480×110, "AUDIO SETTING" — OFFSET SETTING 버튼 복제 |
| AudioPanelRoot (처음엔 꺼짐) | 오프셋 패널과 같은 바탕·Box(900×700) |
| Title | "AUDIO" |
| 3줄 (MASTER / MUSIC / SFX) | 왼쪽 이름 Text, 가운데 uGUI Slider(0~100, Whole Numbers), 오른쪽 "100%" Text |
| ResetButton / CloseButton | 키 설정 패널과 같은 위치·모양 |

- Slider는 uGUI 기본 Slider(`DefaultControls.CreateSlider`, Unity 내장 UI 스프라이트)로 만든다. 새 에셋 없음.
- Slider 색은 기존 버튼 색(주황 #F7931E 계열 / 회색)에 맞춘다.

### 4.4 SettingsScreen.cs 추가 부분
```csharp
[SerializeField] private Button audioOpenButton;     // AUDIO SETTING
[SerializeField] private GameObject audioPanelRoot;  // 오디오 패널 (처음엔 숨김)
[SerializeField] private Button audioCloseButton;
[SerializeField] private Button audioResetButton;
[SerializeField] private Slider[] volumeSliders = new Slider[3]; // MASTER, MUSIC, SFX 순서
[SerializeField] private Text[] volumeTexts = new Text[3];       // "100%"

private bool IsAudioOpen => audioPanelRoot != null && audioPanelRoot.activeSelf;

// Start: 패널 숨김, 버튼 연결, 슬라이더 값 = 저장값, onValueChanged → OnVolumeChanged(i)
// 슬라이더를 움직이면 바로 저장하고 % 표시 갱신 (MASTER는 바로 AudioListener에 적용)
private void OnVolumeChanged(int kind, float v)
{
    AudioVolume.Set(kind, Mathf.RoundToInt(v));
    volumeTexts[kind].text = AudioVolume.Get(kind) + "%";
}

// RESET: 세 값 100으로 저장 후 슬라이더·표시 갱신
public void ResetVolumes()
{
    AudioVolume.ResetAll();
    for (int i = 0; i < 3; i++) volumeSliders[i].SetValueWithoutNotify(AudioVolume.Get(i));
    RefreshVolumeTexts();
}
```
- 막기 규칙에 오디오 패널 추가: `Back`은 세 패널 중 하나라도 열려 있으면 무시, `OpenOffset`·`OpenAudio`는 다른 패널이 열려 있으면 무시. KEY SETTING 버튼은 패널 바탕이 클릭을 막는다(키 설정 때와 같은 방식).
- 슬라이더가 선택된 상태에서 방향키로도 값이 바뀐다(uGUI 기본 동작). 키 설정 패널과 동시에 열리지 않으므로 키 입력과 충돌하지 않는다.

### 4.5 앞으로 효과음을 추가할 때 (이번 작업 아님)
- 효과음용 AudioSource에서 `source.volume = AudioVolume.Sfx01` (또는 `PlayOneShot(clip, AudioVolume.Sfx01)`)을 쓰면 SFX 슬라이더가 적용된다. MASTER는 자동 적용.

## 5. 구현 순서 및 진행 상태
- [x] 프롬프트 검토, 로컬 소스·씬 분석 (소리 3곳, 효과음 시스템 없음, AudioMixer 없음)
- [x] 사용자 결정: 메트로놈 = SFX 볼륨 (2026-11-16)
- [x] 구현 승인 (2026-11-16), 곡 기본 볼륨 추가 요청 반영
- [x] 1단계: AudioVolume.cs, SettingsScreen.cs, 3곳 volume 적용, SongData 곡 기본 볼륨 → 컴파일 오류 0
- [x] 2단계: SettingsScene에 AUDIO SETTING 버튼(0, −230)·오디오 패널(오프셋 패널 복제, Box 900×700, 슬라이더 3줄, RESET, CLOSE) 구성, 참조 연결. Huimang.asset 곡 기본 볼륨 0.6
- [x] 3단계: 테스트 (6절)
- [ ] 사용자 실제 조작·청취 확인
- [x] 검증 결과 기록 (PLAN.md 2-3 #2는 계획서 저장 시 미작성 → 미확인)

## 6. 테스트 및 검증 방법
- C: Claude (컴파일, Play Mode 함수 직접 호출·슬라이더 값 변경, AudioSource·AudioListener 값 확인, AUTO TEST, 스크린샷) / U: 사용자 (실제 슬라이더 조작, 소리 크기 청취)
- 검증 전 PlayerPrefs 볼륨 값을 기록하고 검증 후 되돌린다.

| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| AU-01 | 컴파일·씬 참조 | 오류 0, 참조 누락 없음 | C | 통과 (컴파일 오류 0, SettingsScreen 참조 17개 전부 연결, Play 중 Error/Warning 0) |
| AU-02 | AUDIO SETTING 열기·닫기 | 패널에 세 슬라이더와 현재 % 표시, CLOSE로 설정 화면 | C/U | C 통과 (패널 열림, 100%/100%/100%, CLOSE 후 닫힘, 스크린샷) / U 미검증 |
| AU-03 | 패널끼리 막기 | 오디오 패널 열림 중 BACK·KEY·OFFSET 무시, 다른 패널 열림 중 AUDIO SETTING 무시 | C | 통과 (오디오 열림 중 Back·OpenOffset 무시, KEY SETTING 위치 레이캐스트 최상단 = 패널 Box, 오프셋·키 패널 열림 중 OpenAudio 무시) |
| AU-04 | 슬라이더 변경 | 값·% 표시 즉시 갱신, PlayerPrefs 저장 | C/U | C 통과 (MASTER 50 → 표시 50%·저장 50, MUSIC 40·SFX 30 저장) / U 미검증 (실제 드래그) |
| AU-05 | MASTER | `AudioListener.volume` = 값/100, 씬 이동 후에도 유지 | C/U | C 통과 (MASTER 50 → AudioListener 0.50, 80 → 0.80, 곡 선택·인게임·보정 화면에서도 0.80) / U 미검증 |
| AU-06 | MUSIC | 미리듣기·인게임 음악 AudioSource volume = 값/100 × 곡 기본 볼륨 | C/U | C 통과 (MUSIC 50: kick 미리듣기·인게임 0.50, 희망 0.30 = 0.5 × 0.6) / U 미검증 |
| AU-14 | 곡 기본 볼륨 | 희망 0.6, 기존 곡 1 (MUSIC 100%에서 희망 미리듣기·인게임 volume 0.6, kick 1). 두 곡 체감 크기 비슷 | C/U | C 통과 (MUSIC 100%: 희망 미리듣기 0.60, kick 1.00 / MUSIC 50%: 희망 0.30, kick 0.50) / U 미검증 (두 곡 체감 크기) |
| AU-07 | 메트로놈 | 보정 화면 메트로놈 AudioSource volume = SFX/100 | C/U | C 통과 (SFX 30 → 메트로놈 volume 0.30) / U 미검증 |
| AU-08 | RESET | 세 값 100, 슬라이더·표시·저장값 갱신 | C | 통과 (RESET → 100/100/100, 슬라이더·표시·저장값·AudioListener 1.00) |
| AU-09 | 다시 실행 | Play 다시 시작 후 저장값 유지, MASTER 적용 | C | 통과 (Play 종료 후 저장 80/50/30, 다시 시작 직후 AudioListener 0.80·AudioVolume 80,50,30. Play 종료 후 에디터 AudioListener는 1.00으로 돌아옴) |
| AU-10 | 0% | 소리 없음, 그래도 음악 재생 위치·곡 시간 진행 | C/U | C 통과 (MASTER 50·MUSIC 0: kick 미리듣기 volume 0, 1.5초 뒤 재생 위치 3.07초로 진행) / U 미검증 |
| AU-11 | 싱크·판정 회귀 | 볼륨 50%·0%에서 AUTO 3 결과 동일 (Test Song 1 35800/61, Test Song 2 9800/14, kick 263200/364, 희망 1,174,000/1,510) | C | 통과 (MASTER 50·MUSIC 0·SFX 0에서 Test Song 1 35,800/61, Test Song 2 9,800/14, kick 263,200/364, 희망 1,174,000/1,510 Perfect 1,030 Tick 480 — 음악 곡은 음악 시간 직접 진행) |
| AU-12 | 기존 설정 회귀 | 키 설정·오프셋 패널·AUTO SYNC·BACK 정상 | C | C 통과 (설정 화면 → 오프셋 패널 → AUTO SYNC 진입, BACK, 곡 선택 → 인게임 → 일시정지 Song Select 흐름 정상) |
| AU-13 | 화면 배치 | 설정 화면·오디오 패널 잘림·겹침 없음 (스크린샷) | C/U | C 통과 (오디오 패널 스크린샷 잘림·겹침 없음) / U 미검증 |

## 7. 검증 결과 및 미해결 사항
- 구현: 4절 그대로 + 곡 기본 볼륨(1절 추가 요구사항). 계획과 다르게 만든 부분 없음.
- 검증 방식: Play Mode에서 슬라이더 값 직접 지정(onValueChanged 발생)·함수 호출, AudioSource·AudioListener 값 확인, AUTO TEST, 스크린샷. 검증 전 볼륨 저장값이 없었으므로(none) 검증 후 저장 키 3개를 지워 원래대로 되돌렸다.
- 결과: 6절 표대로 C 항목 모두 통과. 실제 소리 크기와 두 곡 체감 비교는 사용자 확인 필요.
- 참고: AU-02~ 첫 실행은 검증 스크립트의 문자열 연결 실수로 값 조회가 실패했다(게임 코드와 무관). 고친 뒤 다시 실행.
- 미해결: 희망 곡 기본 볼륨(0.6 → 0.42 → 0.3 → 2026-11-16 사용자 요청으로 0.1)이 실제로 kick과 비슷하게 들리는지는 사용자 청취로 확인. 다르면 Huimang.asset의 곡 기본 볼륨만 바꾸면 된다(인스펙터).

### 변경 이력
| 날짜 | 내용 |
|---|---|
| 2026-11-16 | GPT 작업 프롬프트(사용자 제공)를 바탕으로 로컬 소스·씬 분석 후 최초 작성 |
| 2026-11-16 | 사용자 결정 반영: 메트로놈은 SFX 볼륨 |
| 2026-11-16 | 사용자 요청: 곡끼리 소리 크기 차이 조절 → 곡 기본 볼륨(SongData) 추가, 희망 0.6 (측정값 1절) |
| 2026-11-16 | 구현 및 검증 (6·7절) |
| 2026-11-16 | 사용자 청취 피드백: 0.6에서도 희망이 kick보다 크게 들림 → 희망 곡 기본 볼륨을 30% 더 낮춤(0.6 × 0.7 = 0.42). 곡 선택 미리듣기 AudioSource volume 0.42 확인(사용자가 저장해 둔 MASTER 38%·MUSIC 100%는 건드리지 않음, 이후 사용자가 MASTER 100%로 되돌림). 두 곡 크기 비교는 사용자 확인 필요 |
| 2026-11-16 | 사용자 요청: 희망 곡 기본 볼륨 0.42 → 0.3. 곡 선택 미리듣기 AudioSource volume 0.30 확인. 두 곡 크기 비교는 사용자 확인 필요 |
| 2026-11-16 | 사용자 요청: 희망 곡 기본 볼륨 0.3 → 0.1 (사용자가 MASTER를 100%로 되돌린 상태). 곡 선택 미리듣기 AudioSource volume 0.10 확인. 두 곡 크기 비교는 사용자 확인 필요 |
