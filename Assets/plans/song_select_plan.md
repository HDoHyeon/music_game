# 곡 선택 화면 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 목적: 등록된 곡을 선택하고 해당 곡의 플레이를 시작한다.
- 상태: 기존 곡 선택 구현 완료 / 사용자 확인 완료 (2026-11-15, PLAN.md 확인). 곡 미리듣기 보완은 구현 완료(2026-11-16) / 사용자 청취 확인 대기. 곡 "희망" 추가는 구현 완료(2026-11-16) / 사용자 플레이·청취 확인 대기
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항
- 등록된 곡을 선택하고 해당 곡의 플레이를 시작한다.
- 기존 기능을 보존하고 요청 범위에 필요한 변경만 수행한다.
- 확정 사항 (2026-11-15 사용자 승인):

| 항목 | 확정값 |
|---|---|
| 곡 데이터 | 곡마다 `SongData` 에셋(제목, 아티스트, 채보, 음악[선택]), 곡 목록 에셋 `SongList`에 등록. 인스펙터에서 곡 추가·수정 |
| 테스트 곡 | 3곡. Test Song 1(기존 `test_chart`), Test Song 2·3(새 짧은 채보, 롱노트 포함). 아티스트 "Test", 음악 없음 |
| 추가 곡 (2026-11-15, 사용자 요청) | `kick` / 아티스트 `claude_ai`. 140 BPM 일렉트로닉, 90.6초, 파이썬으로 직접 합성한 WAV. 채보·SongData를 만들어 `SongList` 4번째에 등록 |
| 화면 흐름 | `SongSelectScene`(새 씬, 게임 시작 씬) → `IngameScene`(플레이) → `ResultScene`(RETRY / SONG SELECT) |
| 화면 배치 | 왼쪽 곡 목록, 오른쪽 선택한 곡 정보(제목, 아티스트, 노트 수, 길이), START 버튼 |
| 조작 | 마우스: 목록 클릭으로 선택, START로 시작. 키보드: ↑↓로 선택, Enter로 시작 |
| 시작 불가 | 곡이 없거나, 채보가 비었거나, 노트가 0개면 START 비활성 + "CANNOT PLAY" 표시 |
| 선택 기억 | 결과 화면에서 SONG SELECT로 돌아오면 마지막으로 고른 곡이 선택된 상태 |
| 키 설정 버튼 (2026-11-15, `key_setting_plan.md`) | ~~KEY SETTING 버튼으로 설정 패널을 열고, 패널이 열린 동안 ↑↓/Enter 무시~~ → 2026-11-16부터 곡 선택 화면에는 **SETTINGS 버튼**(오른쪽 위)만 두고, 키 설정·오프셋은 설정 화면에서 (settings_save_plan.md) |
| 단독 실행 | `IngameScene`을 바로 Play하면 인스펙터에 지정한 채보로 플레이 (AUTO 검증용). 2026-11-15부터 에디터 Play는 항상 SongSelectScene에서 시작하므로 에디터에서는 이 경로를 쓰지 않음(코드는 유지) |

### 추가 요구사항 — 곡 미리듣기 (2026-11-16, 사용자 확정)
| 항목 | 확정 내용 |
|---|---|
| 재생 시작 | 선택한 곡의 처음(0초) |
| 재생 방식 | 전체 곡 한 번만 재생, 반복 없음 |
| 다른 곡 선택 | 이전 음악 즉시 정지, 새 음악 0초부터 재생 |
| 같은 곡 재선택 | 현재 재생 상태 유지. 끝난 뒤에도 자동으로 다시 틀지 않음 |
| 음악 없는 곡 | 이전 음악 정지, 무음 |
| 화면 최초 진입 | 처음 선택되는 곡(기존 규칙: 이전에 고른 곡, 없으면 첫 곡)에 음악이 있으면 자동 재생 |
| START / Enter | 미리듣기 정지 후 인게임 진입, 인게임 음악은 기존대로 처음부터 |
| ~~AUTO SYNC~~ SETTINGS | ~~미리듣기 정지 후 보정 화면 진입~~ → 2026-11-16부터 SETTINGS: 미리듣기 정지 후 설정 화면 진입 (AUTO SYNC는 설정 화면으로 이동) |
| 곡 선택 복귀 | 결과 화면·~~AUTO SYNC~~ 설정 화면(BACK)·일시정지 메뉴에서 돌아오면 기존 선택 복원 규칙대로 선택된 곡을 처음부터 재생. 설정 화면에서 돌아오면 들어가기 직전에 고른 곡 |
| 키 설정 패널 | ~~열어도 미리듣기는 계속 재생 (기존 입력 차단은 유지)~~ → 2026-11-16부터 키 설정 패널은 설정 화면에 있어 곡 선택 화면에는 해당 없음 |

- 미리듣기는 `SongData.Music`을 그대로 쓴다. 별도 음원·하이라이트 구간·반복·페이드·재생 버튼·볼륨 UI는 추가하지 않는다.


### 추가 요구사항 — 곡 "희망" 추가 (2026-11-16, 사용자 요청)
| 항목 | 확정 내용 |
|---|---|
| 음원 | 사용자가 준 `희망.mp3` (MPEG-1 Layer 3, 128kbps, 44.1kHz, 약 4분 18초, 파일 안에 제목·아티스트 정보 없음) |
| 제목 | 한글 그대로 `희망` |
| 아티스트 | `GrandChace` (사용자 입력 그대로) |
| 채보 | 음악 분석으로 자동 생성 (BPM·박자 위치를 찾아 강한 소리에 노트 배치) |
| 난이도 | 어렵게: 초당 약 3~4개, 동시치기·롱노트 많이 |
| 목록 위치 | SongList 5번째 (kick 다음) |

- 한글 표시: 기본 글꼴(LegacyRuntime, 동적 글꼴)이 Windows 시스템 글꼴로 대체해 `희`·`망` 글자를 그릴 수 있음을 확인했다(에디터, `HasCharacter` true). 대상 플랫폼이 PC라 같은 방식으로 표시된다. 화면에서 깨지면 그때 한글 글꼴 추가를 다시 묻는다.
- 음원은 사용자가 제공한 파일을 그대로 쓴다(변환·편집 없음).

## 2. 기존 코드 분석
- `GameplayManager`는 인스펙터의 `chart`(TextAsset)와 `audioSource`(clip이 있으면 곡 시간 기준)로 플레이한다. 곡을 고르는 구조는 없다.
- `ResultScreen`에는 RETRY 버튼만 있다.
- Build Settings: `IngameScene`(0), `ResultScene`(1).
- 입력은 Input System(`Keyboard.current`), UI는 uGUI + `InputSystemUIInputModule`.

### 미리듣기 보완 분석 (2026-11-16, 로컬 소스·씬 확인)
- `SongSelectScreen.Start()`: 목록 버튼을 만들고 `SongSelection.Selected`의 인덱스(없으면 0)로 `Select()` 호출. `selectedIndex` 초기값은 -1.
- 마우스 클릭과 ↑↓는 모두 `Select(int index)`를 부른다. `Select`는 범위를 맞추고(Clamp) 강조 표시와 `ShowInfo`를 갱신한다. 같은 곡이 다시 와도 갱신만 한다.
- `StartSong()`: `canStart`·`loading` 확인 후 `SongSelection.Selected` 저장, IngameScene 로드. `OpenAutoSync()`: `loading`·키 설정 패널 확인 후 OffsetCalibrationScene 로드.
- `Update()`: `loading` 중이거나 키 설정 패널이 열려 있으면 ↑↓/Enter/←→ 무시.
- `GameplayManager.Start()`는 선택 곡의 `Chart`·`Music`을 자기 AudioSource로 0초부터 재생한다. 미리듣기와 별개다.
- **SongSelectScene에는 AudioListener가 없다** (씬 파일에서 0개 확인). 이대로는 미리듣기 소리가 들리지 않는다. 같은 원인으로 AUTO SYNC 화면이 무음이었던 적이 있다(offset_setting_plan.md).
- 음원이 있는 곡은 kick 하나. Test Song 1~3은 음원 없음.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Song/SongData.cs` | 신규 | 곡 에셋 (제목, 아티스트, 채보, 음악) |
| `Assets/Scripts/Song/SongList.cs` | 신규 | 곡 목록 에셋 |
| `Assets/Scripts/Song/SongSelection.cs` | 신규 | 고른 곡을 씬 사이에 넘겨 주는 `SongSelection.Selected` |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 신규 | 목록 버튼 생성, 선택 강조, ↑↓/Enter, 곡 정보 표시, START, 시작 가능 여부 확인 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | 시작 시 `SongSelection.Selected`가 있으면 그 곡의 채보·음악 사용, 없으면 인스펙터 값 사용 |
| `Assets/Scripts/Result/ResultScreen.cs` | 수정 | SONG SELECT 버튼 연결 (`SongSelectScene` 불러오기) |
| `Assets/Scenes/SongSelectScene.unity` | 신규 | 카메라, Canvas(목록·정보·START), EventSystem, `SongSelectScreen` |
| `Assets/Scenes/ResultScene.unity` | 수정 | SONG SELECT 버튼 추가 (RETRY 옆) |
| `Assets/Charts/test_chart_2.txt`, `test_chart_3.txt` | 신규 | 테스트 채보 2개 |
| `Assets/Songs/TestSong1~3.asset`, `Assets/Songs/SongList.asset` | 신규 | 곡 에셋 3개, 목록 에셋 |
| `ProjectSettings/EditorBuildSettings.asset` | 수정 | SongSelectScene(0, 시작) → IngameScene(1) → ResultScene(2) (사용자 승인) |
| `Assets/Editor/PlayModeStartScene.cs` | 신규 (2026-11-15) | 에디터에서 Play 시 열려 있는 씬과 상관없이 SongSelectScene부터 시작 (`EditorSceneManager.playModeStartScene`) |
| `Assets/Scenes/SampleScene.unity` → `IngameScene.unity` | 이름 변경 (2026-11-15) | 씬 이름 변경, `SongSelectScreen`·`ResultScreen`의 `playSceneName`과 Build Settings 경로 갱신 |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 (2026-11-15, 키 설정) | 키 설정 패널이 열려 있으면 ↑↓/Enter 무시 |
| `Assets/Scenes/SongSelectScene.unity` | 수정 (2026-11-15, 키 설정) | KEY SETTING 버튼, 설정 패널 추가 |
| `Assets/Music/kick.wav` | 신규 (2026-11-15, kick) | 곡 음원 (44.1kHz 모노 16bit, 90.6초) |
| `Assets/Charts/kick_chart.txt` | 신규 (2026-11-15, kick) | 노트 200개(롱노트 20), 첫 노트 6.86초. 곡과 같은 악보에서 생성 |
| `Assets/Songs/Kick.asset`, `Assets/Songs/SongList.asset` | 신규·수정 (2026-11-15, kick) | 곡 에셋 생성, 목록 4번째에 추가 |
| `Assets/Scenes/IngameScene.unity` | 수정 (2026-11-15, kick) | `Music` 오브젝트(AudioSource, Play On Awake 끔, 반복 없음) 추가 → `GameplayManager.audioSource` 연결. 그전에는 연결이 비어 있어 음악이 재생되지 않았음 |

- 목록 버튼의 키보드 이동은 UI 기본 내비게이션을 끄고 `SongSelectScreen`에서만 처리한다(↑↓ 한 번에 두 번 움직이는 것 방지).
- 제외 범위: ~~곡 미리듣기~~(2026-11-16 사용자 요청으로 범위에 추가), 앨범 이미지, 난이도 구분, 정렬·검색, 최고 기록 표시.

### 미리듣기 보완 수정 대상 (2026-11-16)
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 | 미리듣기 AudioSource 참조, `Select`에서 곡이 바뀔 때 재생, START·AUTO SYNC 때 정지 |
| `Assets/Scenes/SongSelectScene.unity` | 수정 | `SongPreviewAudio` 오브젝트(AudioSource) 추가·연결, **Main Camera에 AudioListener 추가** |

- 수정하지 않음: SongData, SongList, SongSelection, GameplayManager, IngameScene, ResultScene, OffsetCalibrationScene.
- **중요 — 사용자 승인 필요**: Main Camera에 AudioListener 추가 (없으면 미리듣기가 들리지 않음. 씬에 1개만 두며 중복 없음).


### 곡 "희망" 수정 대상 (2026-11-16)
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Music/huimang.mp3` | 신규 | 사용자 음원 복사 (파일 이름만 영문으로, 내용 그대로) |
| `Assets/Charts/huimang_chart.txt` | 신규 | 자동 생성 채보 (기존 형식 `시각,레인[,종료시각]`) |
| `Assets/Songs/Huimang.asset` | 신규 | SongData (제목 `희망`, 아티스트, 채보, 음악) |
| `Assets/Songs/SongList.asset` | 수정 | 5번째에 추가 |

- 코드·씬·다른 곡은 수정하지 않는다. 채보 생성은 에디터에서 일회성으로 실행하고 생성 코드는 프로젝트에 넣지 않는다(kick 때와 같은 방식). 생성 방법은 아래 4절에 기록한다.
- 음원 import 설정은 kick과 같은 기본값. 분석 중 샘플을 읽기 위해 잠시 바꾼 설정이 있으면 원래대로 되돌린다.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
```csharp
// GameplayManager.Start: 곡 선택 화면에서 고른 곡이 있으면 그 곡으로 플레이
SongData song = SongSelection.Selected;
if (song != null)
{
    chart = song.Chart;
    if (audioSource != null) audioSource.clip = song.Music; // 음악이 없으면 null → 시작 후 경과 시간 사용
}
```
- 노트 수·길이는 곡 선택 화면에서 `ChartLoader.Load`로 채보를 읽어 계산한다. 노트 수는 일반·롱노트 각 1개, 길이는 마지막 노트 종료 시각을 `m:ss`로 표시.

### 미리듣기 보완 (2026-11-16)
#### 씬 구성
- `SongPreviewAudio` 오브젝트 + AudioSource: Play On Awake 끔, Loop 끔, Spatial Blend 0(2D), Volume 1, Clip 없음. `SongSelectScreen.previewSource`에 연결. DontDestroyOnLoad는 쓰지 않는다(씬을 나가면 함께 사라짐).
- Main Camera에 AudioListener 추가 (승인 후).

#### 재생·정지
```csharp
[SerializeField] private AudioSource previewSource; // 곡 미리듣기 전용 (인게임 음악과 별개)

// 미리듣기를 멈추고 클립을 비운다.
private void StopPreview()
{
    if (previewSource == null) return;
    previewSource.Stop();
    previewSource.clip = null;
}

// 곡의 음악을 처음부터 한 번 재생한다. 음악이 없으면 멈춘 상태(무음)로 둔다.
private void PlayPreview(SongData song)
{
    StopPreview();
    if (previewSource == null || song == null || song.Music == null) return;
    previewSource.clip = song.Music;
    previewSource.loop = false;
    previewSource.time = 0f;
    previewSource.Play();
}
```
- 정지 → 클립 교체 → 0초 → 재생 순서. `PlayOneShot`을 쓰지 않아 이전 곡과 겹치지 않는다. 자연 종료 후 다시 재생하지 않는다.

#### Select 연결 (기존 코드에 추가되는 부분만 표시)
```csharp
public void Select(int index)
{
    SongData[] songs = Songs;
    if (songs.Length == 0)
    {
        selectedIndex = -1;
        StopPreview();                                      // 추가
        ShowInfo(null, null);
        return;
    }

    int nextIndex = Mathf.Clamp(index, 0, songs.Length - 1);
    bool changed = nextIndex != selectedIndex;              // 추가: 다른 곡으로 바뀌었을 때만 재생
    selectedIndex = nextIndex;
    // ... 기존 강조 표시 ...
    SongData song = songs[selectedIndex];
    if (changed) PlayPreview(song);                         // 추가
    // ... 기존 ChartLoader.Load / ShowInfo ...
}
```
- 같은 곡 클릭, 목록 끝에서 ↑↓(Clamp로 같은 인덱스)는 재생을 다시 시작하지 않는다. 강조·정보 갱신은 기존대로.
- `selectedIndex` 초기값 -1 덕분에 `Start`의 첫 `Select`에서 재생이 시작된다.
- 미리듣기 여부는 `Music` 유무, START 가능 여부는 기존 채보 검증으로 서로 따로 판단한다.
- GPT 초안에 있던 `Select` 맨 앞 `if (loading) return;`은 넣지 않는다. 씬을 불러오는 중에는 `Update`가 이미 키 입력을 막고, 그사이 마우스 클릭으로 재생이 시작돼도 씬 전환과 함께 사라져 영향이 없다. 기존 동작을 바꾸지 않기 위해서다.

#### 씬 전환
```csharp
public void StartSong()
{
    if (!canStart || loading) return;
    loading = true;
    StopPreview();                                          // 추가
    SongSelection.Selected = Songs[selectedIndex];
    SceneManager.LoadScene(playSceneName);
}

public void OpenAutoSync()
{
    if (loading || (keySettingPanel != null && keySettingPanel.IsOpen)) return;
    loading = true;
    StopPreview();                                          // 추가
    SceneManager.LoadScene(calibrationSceneName);
}
```
- START가 막힌 곡에서 Enter를 눌러도 미리듣기는 멈추지 않는다(기존 조건 그대로).
- 씬을 나가면 AudioSource가 씬과 함께 사라지므로 GPT 초안의 `OnDisable` 정리는 넣지 않는다.
- `SongSelection.Selected`는 기존처럼 START 때만 저장한다.


### 채보 자동 생성 방법 (곡 "희망")
1. **샘플 읽기**: Unity로 음원을 불러와 `AudioClip.GetData`로 모노 샘플을 얻는다.
2. **소리 세기 변화(온셋) 계산**: 약 11.6ms(512샘플) 단위로 저음·중음·고음 3개 대역의 에너지를 구하고, 직전보다 커진 양을 더해 "강한 소리 시작" 곡선을 만든다.
3. **BPM·첫 박자 찾기**: 온셋 곡선의 자기상관으로 60~200 BPM 중 가장 잘 맞는 템포를 고르고, 박자 격자를 앞뒤로 밀어 온셋 합이 가장 큰 위치를 첫 박자로 정한다. 찾은 BPM은 결과에 기록한다.
4. **노트 위치**: 박자 격자(1/4, 1/8, 강한 구간은 1/16 일부)에 맞춰, 주변 평균보다 충분히 강한 온셋이 있는 격자점에 노트를 둔다. 기준값을 조절해 노래가 나오는 구간 평균 초당 3~4개가 되게 한다. 조용한 전주·간주·후주는 자연히 적어진다. 첫 노트는 2초 이후.
5. **레인 배치**: 대역별로 기본 레인을 나누고(저음 → 바깥 레인, 고음 → 안쪽 레인 등) 같은 레인이 1/16 간격으로 연속되지 않게 옮긴다. 저음·고음이 함께 강한 박에는 2개 동시치기(최대 2개).
6. **롱노트**: 소리가 길게 이어지는 구간(온셋은 없고 에너지가 유지됨)에서, 그 레인의 다음 노트까지 1박 이상 비어 있으면 0.5~2박 길이 롱노트로 만든다. 같은 레인의 다른 노트와 겹치지 않게 다음 노트 1/8박 전에 끝낸다.
7. **검사**: 같은 레인에서 노트·롱노트가 겹치지 않는지, 레인 0~3·시각 순서·`ChartLoader` 경고 0을 확인한다.
- 실제 적용 (2026-11-16):
  - 대역 에너지는 Unity에서 512샘플마다 계산해 파일로 내보내고(1차 필터 2단: 저역 150Hz, 2500Hz 기준), BPM·채보 계산은 Python으로 했다(일회성, 프로젝트에 넣지 않음).
  - BPM: 자기상관 후보 123~124 → 격자 맞춤으로 124.00. 30초 구간별 위상 차이 ±8ms 이내라 템포가 일정하다고 판단했다.
  - 노트 선택: 격자점 온셋을 주변 ±2초 평균으로 나눈 값이 기준 이상이면 노트(1/8은 1.15배, 1/16은 1.8배 더 엄격, 조용한 구간 1.4배). 1/16 연속은 3개까지. 목표 950개가 되도록 기준값을 이분 탐색.
  - 같은 레인 최소 간격 1/8박(약 0.24초). 롱노트는 중음이 1~2박 유지되는 박 위치 중 점수가 높은 80개(시작 간격 2박 이상), 동시치기면 한쪽만 롱노트.

## 5. 구현 순서 및 진행 상태
- [x] 곡 데이터 스크립트·테스트 채보·에셋 (`Assets/Songs/TestSong1~3.asset`, `SongList.asset`, `test_chart_2/3.txt`)
- [x] 곡 선택 화면 스크립트, `GameplayManager` 연결, 결과 화면 버튼 → 컴파일 확인 (에러 0건)
- [x] `SongSelectScene` 생성, `ResultScene` 버튼 추가(RETRY 왼쪽 -220, SONG SELECT 오른쪽 220), Build Settings 변경 (SongSelectScene 0 / IngameScene 1 / ResultScene 2)
- [x] 검증
- 계획에 없던 수정 (검증 중 발견, 2026-11-15):
  - 새 씬을 만들 때 `SongSelectScreen.songList` 참조가 비어 저장됨 → 씬을 다시 열어 연결 후 저장.
  - 프로젝트에 Enter Play Mode Options가 켜져 있어 Play를 다시 시작해도 static 값이 남음 → `IngameScene` 단독 실행 시 이전 세션에서 고른 곡(Test Song 2)으로 플레이되는 문제 발견. `SongSelection.Selected`와 `PlayResult.Last`를 Play 시작 시 비우도록 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 추가.

- 완료: 계획, 구현, Claude 자동 검증
- 진행 중: 없음
- 남은 작업: 사용자 확인 (↑↓/Enter 키, 마우스 클릭, 화면 배치)

### 미리듣기 보완 (2026-11-16)
- [x] GPT 초안 검토, 로컬 소스·씬 대조 (AudioListener 없음 확인)
- [x] 사용자 재생 정책 확정 (0초부터 전체 곡 1회)
- [x] 사용자 구현 승인 (AudioListener 추가 포함, 2026-11-16)
- [x] SongPreviewAudio·AudioListener 배치 및 참조 연결 (AudioListener 1개)
- [x] 재생/정지 메서드, Select·START·AUTO SYNC 연결
- [x] 컴파일·Inspector 참조 확인 (오류 0, previewSource 연결)
- [x] Play Mode 검증 (6절 SP 표)
- [x] 기존 곡 선택·인게임·자동 싱크 회귀 (AUTO 3 3곡)
- [ ] 사용자 청취 확인
- [x] 결과 기록, PLAN.md 곡 선택 상태 확인 → 미확인 (2026-11-16)
- PLAN.md: 구현 시작 시 곡 선택 상태를 "확인" → "미확인"으로 바꾸고, 사용자 확인 후 "확인"으로 되돌린다(기능 목록 상태 칸).


### 곡 "희망" 진행
- [x] 음원 확인 (약 4분 18초, 태그 없음), 한글 글꼴 표시 가능 확인
- [x] 사용자 결정: 제목 한글, 자동 생성 채보, 어려운 난이도
- [x] 아티스트 이름 확인(GrandChace), 구현 승인 (2026-11-16)
- [x] 음원 복사·import 확인 (`Assets/Music/huimang.mp3`, 원본과 바이트 동일, 258.25초, 스테레오 44.1kHz, kick과 같은 기본 import 설정)
- [x] 채보 생성: **124.00 BPM**, 첫 박자 0.129초, 노트 950개(일반 870 + 롱노트 80), 동시치기 210곳(최대 2개), 격자 1/4 488·1/8 210·1/16 42곳, 첫 노트 2.06초·마지막 255.73초, 평균 초당 3.75개, 같은 레인 겹침 0
- [x] SongData 생성(`Assets/Songs/Huimang.asset`), SongList 5번째 등록
- [x] 검증 (6절)
- [ ] 사용자 플레이·청취 확인 (박자와 노트가 맞는지, 난이도)

## 6. 테스트 및 검증 방법
| 테스트 항목 | 기대 결과 | 상태 |
|---|---|---|
| 곡 목록 | 3곡 표시 | 통과 |
| 곡 선택 | 강조 표시와 곡 정보(제목·아티스트·노트 수·길이) 갱신 | 통과 (Test Song 1: NOTES 20 / 0:13, 2: 7 / 0:05, 3: 8 / 0:06) |
| 다른 곡 선택 | 선택 상태 갱신 | 통과 (범위 밖 `Select(99)`은 마지막 곡으로 맞춰짐) |
| 선택 없이 시작 / 빈 채보 | START 비활성 + CANNOT PLAY | 통과 (빈 채보: 메모리에서만 잠시 비우고 확인 후 복원, 에셋 변경 없음) |
| 곡 시작 | 고른 곡의 채보로 플레이 (AUTO 3단계 결과 점수 = 채보 계산값) | 통과 (Test Song 2: SS / 9800 / MAX COMBO 14 = 판정 8×1000 + 틱 6×300) |
| RETRY | 같은 곡으로 다시 시작 | 통과 (채보 test_chart_2, 점수 0부터) |
| SONG SELECT | 곡 선택 화면으로 돌아오고 마지막 곡이 선택된 상태 | 통과 (Test Song 2 강조) |
| 단독 실행 | `IngameScene` 바로 Play 시 AUTO 3단계 35800 유지 | 통과 (static 초기화 수정 후. 수정 전에는 9800으로 이전 곡이 남아 있었음) |
| 키보드·마우스 | ↑↓/Enter, 클릭 조작 | 미검증 (사용자 확인) |
| kick 곡 (2026-11-15) | 목록 4번째 표시, 음악 시간 기준 플레이, AUTO 3단계 Miss 0 | 통과 (NOTES 200 / 1:29, SS / 263200 / MAX COMBO 364) |
| 음악 없는 곡 (AudioSource 연결 후) | 기존처럼 시작 후 경과 시간으로 진행 | 통과 (Test Song 1 AUTO 3단계 35800) |

- [x] Unity 컴파일 오류 확인
- [x] 실제 플레이에서 기능 검증 (Claude가 SongSelectScene부터 Play 모드로 전체 흐름 실행, 버튼은 `onClick` 호출)
- [x] 관련 기존 기능 회귀 테스트 (IngameScene 단독 AUTO 3단계 35800, ResultScene 단독 실행 시 NO RESULT)

### 미리듣기 보완 테스트 (2026-11-16)
- C: Claude (컴파일, Play Mode에서 함수 직접 호출, AudioSource 상태·재생 위치 확인) / U: 사용자 (실제 청취, 키·마우스)
- Unity 창에 포커스가 없으면 화면 프레임은 멈추지만 오디오는 계속 재생된다. 그래서 `isPlaying`·`time` 변화와 곡 끝 정지는 C가 확인할 수 있고, 소리가 실제로 들리는지는 U만 확인할 수 있다.

| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| SP-01 | 최초 진입, 음악 없는 첫 곡 | 무음, 곡 정보 정상 | C/U | C 통과 (Test Song 1 선택, clip 없음, 재생 안 함) / U 미검증 |
| SP-02 | kick 클릭 | 0초부터 재생, 시간이 흐름 | C/U | C 통과 (클릭 직후 0.00초, 2초 뒤 2.09초, 4초 뒤 4.37초) / U 미검증 |
| SP-03 | ↑↓로 kick 선택 | 클릭과 같게 재생 | C/U | C 통과 (↑↓와 같은 경로인 Select(i±1) 직접 호출로 확인) / U 미검증 (실제 키) |
| SP-04 | kick → 음악 없는 곡 | 즉시 정지, clip 없음 | C/U | C 통과 (clip 없음, 정지) / U 미검증 |
| SP-05 | 음악 있는 A → 음악 있는 B | A 정지, B 0초부터, 겹침 없음 | C | 통과 (메모리 임시 곡: kick → TempPreview 0초, AudioSource 1개뿐이라 겹침 없음, 에셋 변경 없음) |
| SP-06 | 같은 곡 클릭·목록 끝 입력 | 재생 위치 유지, 다시 시작 안 함 | C/U | C 통과 (같은 곡 클릭·Select(99) 후 4.59 → 5.70초로 이어짐) / U 미검증 |
| SP-07 | kick 끝까지 재생 | 반복 없이 정지, 자동 재시작 없음 | C/U | C 통과 (끝 1초 전 → 90.64초에서 정지, 1초 더 기다려도 재생 안 함, 같은 곡 클릭해도 재시작 없음) / U 미검증 |
| SP-08 | 다른 곡 선택 후 kick 다시 선택 | 0초부터 재생 | C/U | C 통과 (다른 곡 → kick 0.00초, 1초 뒤 1.11초) / U 미검증 |
| SP-09 | START / Enter | 미리듣기 정지, 인게임 음악 처음부터 | C/U | C 통과 (StartSong 직후 clip 없음·정지, 인게임 Music kick 0.00초에서 시작) / U 미검증 |
| SP-10 | AUTO SYNC 진입 | 미리듣기 정지, 보정 메트로놈만 | C/U | C 통과 (OpenAutoSync 직후 정지, 보정 화면 AudioSource는 Metronome 하나) / U 미검증 |
| SP-11 | 결과 → SONG SELECT | 마지막 플레이 곡 선택, 그 곡 음악 처음부터 | C/U | C 통과 (인게임 일시정지 메뉴 Song Select → kick 선택, 1초 뒤 1.11초로 처음부터). 결과 화면 경로는 같은 Start 복원 규칙이라 따로 실행 안 함 / U 미검증 |
| SP-12 | 빈 목록·null 곡·음악 없는 곡 | 오류 없이 무음, START 조건 그대로 | C | 통과 (songList를 메모리에서만 비움 → 선택 -1, 무음, START 불가, 오류 없음 → 복원) |
| SP-13 | 빠르게 연속 선택 | 마지막 곡만 재생 | C | 통과 (0→3→1→3은 kick 0초, …→1로 끝나면 정지) |
| SP-14 | 키 설정 패널 열기 | 기존 입력 차단 유지, 미리듣기 계속 | C/U | C 통과 (패널 열린 채 1초 뒤 1.02초로 계속 재생, AUTO SYNC는 무시) / U 미검증 |
| SP-15 | 씬 전환·Play 종료 | 미리듣기 남지 않음 | C | 통과 (씬 전환 후 미리듣기 AudioSource 없음, Play 종료 후 재생 중 AudioSource 0) |
| SP-16 | 컴파일·참조·AudioListener | 오류·참조 누락 없음, AudioListener 1개 | C | 통과 (컴파일 오류 0, previewSource 연결, AudioListener 1개) |
| SP-17 | AUTO 3 회귀 | Test Song 1 35800/61, Test Song 2 9800/14, kick 263200/364 | C | 통과 (Test Song 1 35,800/61, Test Song 2 9,800/14, kick 263,200/364 Perfect 220 Tick 144 — kick은 음악 시간을 직접 진행) |

- SP-05: 음원 있는 곡이 kick 하나뿐이라, 검증 중 메모리에서만 임시 AudioClip·SongData를 만들어 확인한다(에셋 추가·변경 없음).
- SP-07: 끝까지 기다리거나 재생 위치를 끝 근처로 옮겨 확인한다.


### 곡 "희망" 테스트
| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| HM-01 | import·컴파일 | 음원이 AudioClip으로 불러와짐, 길이 약 258초, 오류 없음 | C | 통과 (AudioClip 258.25초, 컴파일·import 오류 0) |
| HM-02 | 채보 검사 | ChartLoader 경고 0, 같은 레인 겹침 없음, 노래 구간 초당 3~4개 | C | 통과 (ChartLoader 950개·롱노트 80, 경고 0, 같은 레인 겹침 0, 평균 초당 3.75개, 30초 구간별 51~139개) |
| HM-03 | 곡 선택 표시 | 목록 5번째 `희망`, 한글이 깨지지 않음, NOTES·LENGTH 표시, START 가능 (스크린샷) | C/U | C 통과 (5번째 `희망` / GrandChace / NOTES 950 / LENGTH 4:16, START 가능, 스크린샷에서 한글 정상) / U 미검증 |
| HM-04 | 미리듣기 | 선택하면 0초부터 재생 | C/U | C 통과 (선택 직후 0초, 1.5초 뒤 3.09초로 재생 진행) / U 미검증 |
| HM-05 | AUTO 3 | Miss 0, 최대 콤보 = 판정 수 + 틱 수, 점수 = 계산값 | C | 통과 (음악 시간을 1/30초씩 직접 진행: 1,174,000점 / 최대 콤보 1,510 / Perfect 1,030 / Tick 480 / Miss 0 = 계산값과 같음) |
| HM-06 | 박자 맞음 | 노트가 음악의 강한 소리에 맞게 내려옴 | U | 미검증 |
| HM-07 | 기존 곡 회귀 | Test Song 1 35800/61, Test Song 2 9800/14, kick 263200/364 | C | 통과 (Test Song 1 35,800/61, Test Song 2 9,800/14, kick 263,200/364) |

- HM-05의 kick처럼, 에디터 일시정지 중에는 음악이 멈추므로 음악 시간을 직접 진행해 확인한다. 실제 재생 중 싱크는 HM-06(사용자)으로만 판단한다.

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15, Play 모드를 `EditorApplication.Step()`으로 진행, AUTO TEST 3단계):
  - 컴파일 에러 0건, Play 모드 중 에러·경고 0건.
  - SongSelectScene 시작: 3곡 목록, Test Song 1 선택·강조, START 활성.
  - Test Song 2 클릭 → START → SampleScene이 `test_chart_2`로 플레이 → ResultScene `SS / 9800 / MAX COMBO 14 / 100.0% / PERFECT 8`.
  - RETRY → 같은 채보로 재시작, 같은 결과. SONG SELECT → 곡 선택 화면에서 Test Song 2가 선택된 상태.
  - 빈 채보 → `CANNOT PLAY`, START 비활성, `StartSong()` 호출해도 씬 전환 없음.
  - static 초기화 수정 후 SampleScene 단독 AUTO 3단계 35800, ResultScene 단독 실행 시 `NO RESULT`.
- 미해결: ↑↓/Enter 키 조작과 실제 마우스 클릭은 Claude가 입력을 넣을 수 없어 미검증 (사용자 확인 필요). 화면 배치 육안 확인 필요
- 변경 이력: 초안 작성 → 2026-11-15 미확정 사항 확정 및 상세 설계 반영 → 구현 및 검증 (참조 누락·static 초기화 문제 수정 포함) → 2026-11-15 씬 이름 변경(SampleScene → IngameScene), 에디터 Play 시작 씬을 SongSelectScene으로 고정
- 씬 이름 변경·Play 시작 씬 검증 (2026-11-15): IngameScene을 연 채 Play → SongSelectScene부터 시작. Test Song 1 START → IngameScene AUTO 3단계 35800(SS) → RETRY → AUTO 1단계 5000(D) → SONG SELECT → Test Song 3 START → AUTO 2단계 요약 로그와 결과 일치. 에러·경고 0건, Play 종료 후 씬 변경 없음.
- kick 곡 추가·AudioSource 연결 검증 (2026-11-15):
  - 곡 선택 화면: `kick / claude_ai / NOTES 200 / LENGTH 1:29`(길이는 마지막 노트 종료 기준), START 활성.
  - 음악 없이(연결 전) AUTO 3단계: SS / 263200 / MAX COMBO 364 / PERFECT 220(롱노트 시작·끝 2회 판정 포함) / MISS 0.
  - AudioSource 연결 후 실시간 진행(에디터 일시정지 중에는 오디오가 멈춰 Step 방식 불가 → 일시정지 해제 후 진행): 곡 시간 = `audioSource.time`, 음악 종료 후 ResultScene 전환, 결과 동일(263200).
  - Test Song 1(음악 없음) AUTO 3단계 35800 유지. 에러 0건(경고 1건은 검증 스크립트가 음악 없는 곡의 `time`을 읽어서 생긴 것), 씬 변경 남지 않음.
  - 관찰: 시작 직후 약 30초 동안 오디오가 2.2초 위치에서 멈췄다가 재개됨(노트도 함께 멈춰 판정 영향 없음). 에디터 포커스가 없는 상태 때문으로 추정, 미확인.
  - 미해결: 소리 품질과 출력 지연까지 포함한 실제 싱크, 시작 부분 끊김 여부는 사용자 확인 필요(출력 지연 보정은 오프셋 기능 범위).

### 미리듣기 보완 검증 상태 (2026-11-16)
- 구현: `SongSelectScreen.cs`(previewSource, StopPreview/PlayPreview, Select·StartSong·OpenAutoSync 연결), `SongSelectScene`(SongPreviewAudio 오브젝트, Main Camera AudioListener). 계획 4절 그대로이며 다르게 만든 부분 없음.
- 검증 방식: 함수 직접 호출 + AudioSource 상태 확인. 에디터가 일시정지되어 있으면 `isPlaying`이 항상 false이고 재생 위치도 멈춰서, 재생 위치는 일시정지를 잠깐 풀고 읽었다.
- 결과: 6절 SP 표대로 C 항목 모두 통과. 실제 소리가 들리는지, ↑↓·마우스 실제 입력은 사용자 확인 필요.
- 참고: 검증 중 경고 "Attempting to get `time` on an audio source..."가 나왔는데, 검증 스크립트가 clip이 없는 AudioSource의 time을 읽어서 생긴 것이다(게임 코드 아님). 스크립트를 고친 뒤 경고 0.
- 위 기존 검증 기록(2026-11-15)은 과거 결과이며 이번 보완의 통과 결과가 아니다.
- 변경 이력: 2026-11-16 GPT 미리듣기 초안을 로컬 소스·씬과 대조해 반영(AudioListener 추가 명시, `Select`의 `loading` 검사와 `OnDisable` 정리는 제외). 기존 내용은 그대로 보존.
- 변경 이력: 2026-11-16 설정 화면 통합(settings_save_plan.md): 곡 선택 화면의 KEY SETTING 버튼·키 설정 패널·OFFSET 표시·AUTO SYNC 버튼을 설정 화면으로 옮기고 SETTINGS 버튼 추가. `SongSelectScreen`에서 `keySettingPanel`·`offsetText`·`autoSyncButton`·`calibrationSceneName`·`OpenAutoSync`·`UpdateOffsetText`와 `Update`의 패널 검사를 지우고 `settingsButton`·`OpenSettings`(미리듣기 정지, 고른 곡을 `SongSelection.Selected`에 저장 후 이동) 추가. 1절 요구사항 표만 고쳤고, 2~4절 분석·코드와 SP-10·SP-14 검증 기록은 당시 기준으로 그대로 둠. 이동 후 미리듣기 정지·복귀 재생은 settings_save_plan.md ST-02·03·18에서 다시 확인(C 통과).
- 변경 이력: 2026-11-16 곡 "희망" 추가 계획(1·3·4·5·6절의 "곡 희망" 부분) 작성. 사용자 결정: 제목 한글, 채보 음악 분석 자동 생성, 어려운 난이도. 아티스트 이름 미정.
- 곡 "희망" 검증 상태 (2026-11-16):
  - HM-01·02·05·07 통과, HM-03·04 C 통과. HM-06(노트가 음악에 맞는지)과 실제 청취·난이도 체감은 사용자 확인 필요.
  - 참고: 처음 AUTO 3 확인은 음악 시간을 1/15초씩 진행해 Great 244개가 나왔다. 프레임 간격(66ms)이 Perfect 범위(±50ms)보다 커서 생긴 검증 방식 문제이며, 1/30초로 다시 확인해 모두 Perfect.
  - 변경 이력: 2026-11-16 곡 "희망" 구현(음원 복사, 채보 자동 생성, SongData·SongList 등록) 및 검증.
