# 곡 및 난이도 데이터 관리 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 대상: PLAN.md 2-2. 플레이 안정성 및 데이터 구조 / 3. 곡 및 난이도 데이터 관리
- 목적: kick과 희망을 Easy / Normal / Hard 세 난이도로 나누고, 곡을 고른 뒤 난이도를 골라 플레이할 수 있게 한다.
- 상태: 구현 완료(2026-11-16) / 사용자 확인 대기 (난이도별 실제 플레이 체감, 음악 싱크, 화면)
- 작성 경위: GPT가 만든 작업 프롬프트와 예시 이미지(사용자 제공, 2026-11-16)를 로컬 소스·씬·채보와 대조해 작성.
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항 (사용자 프롬프트 기준)
| 곡 | Easy | Normal | Hard |
|---|---|---|---|
| kick | 신규 | **기존 채보** (`kick_chart.txt`) | 신규 |
| 희망 | 신규 | 신규 | **기존 채보** (`huimang_chart.txt`) |

- 기존 두 채보와 음원은 수정·삭제하지 않는다. 신규 채보 4개, 음원은 난이도끼리 같이 쓴다.
- 제목·아티스트·음원·곡 기본 볼륨은 난이도와 상관없이 공통, 채보만 난이도별.
- 곡 선택 화면: kick·희망을 고르면 [Easy] [Normal] [Hard] 버튼 표시, 선택 난이도 강조, 바꾸면 노트 수·길이 갱신, START/Enter로 그 난이도 플레이. 목록에 같은 곡이 난이도별로 중복되지 않음. 난이도만 바꾸면 미리듣기는 다시 시작하지 않음. 채보가 없거나 유효하지 않은 난이도는 플레이 불가.
- RESTART(일시정지)·RETRY(결과)는 같은 곡·같은 난이도로, 설정 화면 왕복·결과/일시정지에서 곡 선택 복귀 시 곡과 난이도 유지.
- Test Song 1~3은 지금처럼 채보 하나로 플레이(난이도 채보 만들지 않음).
- 기존 배속·키 설정·오프셋·소리 설정·곡 기본 볼륨은 바꾸지 않는다.

### 난이도 기준
| 난이도 | 대상 | 기준 |
|---|---|---|
| Easy | 초보자 | 주요 박자·강한 소리만, 낮은 밀도, 입력 사이 여유. 빠른 연타·복잡한 동시치기·어려운 롱노트 조합 최소화 |
| Normal | 일반 | kick 기존 채보가 기준. Easy보다 밀도↑, 연속·동시치기·롱노트를 적당히, 세부 리듬 일부 반영 |
| Hard | 숙련자 | 희망 기존 채보가 기준. 빠른 연속 입력, 다양한 레인 전환, 동시치기+롱노트 조합. 노트 수만 늘리지 않고 패턴 난이도도 고려. 4키에서 칠 수 있게 |

### 제외 범위
- 난이도별 최고 기록, 별점·레벨 숫자, 새 판정 기준, 새 레인 모드, Test Song 1~3 난이도 채보, 곡 선택 화면 전체 개편(예시 이미지의 곡 정보 카드·아이콘 등 — 3절 결정 1), 결과 화면에 난이도 표시(요청 없음)

## 2. 기존 코드 분석 (로컬 확인 2026-11-16)
- **SongData.cs**: `title`, `artist`, `chart`(TextAsset 1개), `music`, `volume`(곡 기본 볼륨). 속성 `Title/Artist/Chart/Music/Volume`.
- **SongList.cs**: `SongData[] songs` (Test Song 1~3, kick, 희망 순서 5곡).
- **SongSelection.cs**: `static SongData Selected` 하나. Play 시작 시(SubsystemRegistration) null로 초기화.
- **SongSelectScreen.cs**: `Select(index)`가 곡이 바뀔 때만 미리듣기 재생, `ChartLoader.Load(song.Chart)`로 노트 수·길이(마지막 노트 종료 시각, m:ss) 표시, 노트 0개면 START 비활성 + CANNOT PLAY. `StartSong`·`OpenSettings`에서 `Selected` 저장. ↑↓ 곡 이동, Enter 시작, ←→ 배속.
- **GameplayManager.Start**: `SongSelection.Selected`가 있으면 `chart = song.Chart`, `audioSource.clip = song.Music`, 볼륨 = MUSIC × 곡 기본 볼륨. 없으면(단독 실행) 인스펙터 채보.
- **PauseController.Restart**: 현재 씬 다시 불러오기 → `GameplayManager.Start`가 다시 `Selected`를 읽음. `GoToSongSelect`: 곡 선택 화면으로(`Selected` 그대로).
- **ResultScreen.Retry**: IngameScene 다시 불러오기(같은 방식), SONG SELECT: 곡 선택 화면으로.
- → **선택한 난이도를 `SongSelection`에 함께 두면** RESTART·RETRY·복귀·설정 왕복이 지금 구조 그대로 같은 난이도를 쓴다(씬은 모두 `SongSelection`만 읽음).
- **ChartLoader**: `시각,레인[,종료시각]`, 잘못된 줄은 경고 후 건너뜀. 같은 레인 겹침은 검사하지 않으므로 신규 채보는 만들 때 직접 검사한다.
- **기존 채보**:
  - kick (140 BPM, 90.6초): 노트 200개(롱노트 20), 동시치기 37곳, 첫 6.86초·마지막 87.43초, 초당 약 2.5개. 곡을 합성할 때 쓴 악보(킥·스네어·하이햇·멜로디)에서 채보를 만들었다(당시 생성 스크립트가 임시 폴더에 남아 있음).
  - 희망 (124 BPM, 258.25초): 노트 950개(롱노트 80), 동시치기 210곳, 초당 약 3.75개. 음원 분석(대역별 온셋)으로 생성.
- **SongSelectScene 배치**: SongTitle (1000, −260), SongArtist −350, SongNotes −460, SongLength −530, Message −640, StartButton −760(360×100), 배속 줄 −900. 버튼 기본색 회색(0.25, 0.25, 0.3), 선택 주황.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Song/Difficulty.cs` | 신규 | `enum Difficulty { Easy, Normal, Hard }` |
| `Assets/Scripts/Song/SongData.cs` | 수정 | 난이도별 채보 칸 3개 추가, `GetChart(Difficulty)`, `HasDifficulties`. 기존 `chart` 칸은 그대로 둠 |
| `Assets/Scripts/Song/SongSelection.cs` | 수정 | `static Difficulty SelectedDifficulty` 추가 (기본 Normal), Play 시작 시 초기화 |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 | 난이도 버튼 3개·강조, `SelectDifficulty`, 노트 수·길이를 선택 난이도 채보로 계산, START 시 난이도 저장 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 1줄 | `chart = song.GetChart(SongSelection.SelectedDifficulty)` |
| `Assets/Scenes/SongSelectScene.unity` | 수정 | DIFFICULTY 글자, Easy/Normal/Hard 버튼, Message·START 위치 조정 |
| `Assets/Charts/kick_easy.txt`, `kick_hard.txt`, `huimang_easy.txt`, `huimang_normal.txt` | 신규 | 신규 채보 4개 |
| `Assets/Songs/Kick.asset`, `Huimang.asset` | 수정 | 난이도 칸 연결 (kick Normal = `kick_chart.txt`, 희망 Hard = `huimang_chart.txt`, 신규 4개) |

- 수정하지 않음: 기존 채보 2개, 음원, Test Song 1~3 에셋, ChartLoader, PauseController, ResultScreen, ResultTransition, 판정·점수·콤보·롱노트 코드, 설정 관련 코드.
- PLAN.md: 2-2 #3 상태 칸만 바꾼다(미작성 → 미확인). 기능 목록 추가 없음.
- **[중요 — 사용자 승인 필요] SongData 구조 변경**: 직렬화 칸이 추가된다. 기존 칸(`chart` 등)은 이름·값을 그대로 두므로 기존 에셋 데이터가 사라지지 않고, 새 칸이 비어 있는 곡(Test Song 1~3)은 지금처럼 `chart` 하나로 플레이된다(4.1).

### 사용자 결정 (2026-11-16, 추천안 확정)
1. **예시 이미지 범위**: 기존 곡 정보 글자는 그대로 두고 DIFFICULTY 글자와 Easy/Normal/Hard 버튼만 이미지처럼 추가한다. 곡 정보 카드·아이콘은 이번 범위 밖(필요하면 PLAN.md 2-4 #2 곡 선택 UI/UX에서).
2. **난이도 기억**: 곡을 바꿔도 마지막으로 고른 난이도를 유지한다(처음은 Normal).
3. **조작**: 난이도는 마우스 클릭으로만 바꾼다(←→는 배속).

## 4. 상세 구현 방법 및 필요한 코드 스니펫
### 4.1 데이터 구조와 호환성
```csharp
// Difficulty.cs
public enum Difficulty { Easy, Normal, Hard }

// SongData.cs 추가 부분
[Header("난이도별 채보 (비우면 위의 chart 하나로 플레이)")]
[SerializeField] private TextAsset easyChart;
[SerializeField] private TextAsset normalChart;
[SerializeField] private TextAsset hardChart;

// 난이도 채보가 하나라도 있으면 난이도 곡 (곡 선택 화면에 난이도 버튼 표시)
public bool HasDifficulties => easyChart != null || normalChart != null || hardChart != null;

// 플레이할 채보. 난이도 곡이면 그 난이도 채보(없으면 null → 플레이 불가), 아니면 기존 chart
public TextAsset GetChart(Difficulty difficulty)
{
    if (!HasDifficulties) return chart;
    switch (difficulty)
    {
        case Difficulty.Easy: return easyChart;
        case Difficulty.Hard: return hardChart;
        default: return normalChart;
    }
}
```
- 기존 `chart` 칸·`Chart` 속성은 지우지 않는다. kick·희망의 `chart`에는 지금 채보가 그대로 남고(참조 유지), 난이도 칸에도 같은 파일을 연결한다(kick Normal, 희망 Hard). 채보 파일은 복제하지 않는다.
- Test Song 1~3은 난이도 칸이 비어 있어 `GetChart`가 `chart`를 돌려준다 → 지금과 같다.
- `SongData.Chart`를 쓰는 곳(곡 선택, GameplayManager)은 `GetChart(...)`로 바꾼다.

### 4.2 선택 난이도 전달
```csharp
// SongSelection.cs
public static Difficulty SelectedDifficulty { get; set; } = Difficulty.Normal;
// ResetOnPlay: SelectedDifficulty = Difficulty.Normal;

// GameplayManager.Start
chart = song.GetChart(SongSelection.SelectedDifficulty);
```
- 곡 선택 화면에서 난이도 버튼을 누르면 바로 `SongSelection.SelectedDifficulty`에 저장한다. START·SETTINGS 때 `Selected`를 저장하는 지금 흐름과 합쳐져, RESTART·RETRY·일시정지/결과 복귀·설정 왕복 모두 같은 곡·같은 난이도가 된다.
- 난이도는 Test Song 1~3에서는 쓰이지 않는다(난이도 버튼 숨김, `GetChart`가 무시).

### 4.3 곡 선택 화면
- 배치 (1920×1080, 기존 오른쪽 정보 열 x = 1000 기준, 스크린샷 보며 조정):

| 오브젝트 | 위치·크기 | 내용 |
|---|---|---|
| DifficultyLabel | (1000, −590) 400×40 | "DIFFICULTY" (작은 글자, 회색) |
| EasyButton / NormalButton / HardButton | y −640, x 1000 / 1270 / 1540, 각 250×70 | 기본: 어두운 회색, 선택: 난이도 색(Easy 초록, Normal 파랑, Hard 빨강 — 예시 이미지의 Hard 빨강 기준) |
| Message (CANNOT PLAY) | −640 → −720 | 버튼과 겹치지 않게 이동 |
| StartButton | −760 → −790 | Message 아래 |

  - 배속 줄(−900)과 곡 목록·제목·아티스트·노트 수·길이는 그대로.
  - 난이도 버튼 3개는 곡이 `HasDifficulties`일 때만 보이고(DifficultyLabel 포함), Test Song은 숨긴다.
```csharp
[SerializeField] private Button[] difficultyButtons = new Button[3]; // Easy, Normal, Hard 순서
[SerializeField] private GameObject difficultyGroup;                 // DIFFICULTY 글자 + 버튼 (Test Song이면 숨김)
[SerializeField] private Color[] difficultyColors = { 초록, 파랑, 빨강 };

// 난이도 버튼: 저장 → 강조 → 노트 수·길이·START 가능 여부 갱신 (미리듣기는 건드리지 않음)
public void SelectDifficulty(int difficulty)
{
    if (loading) return;
    SongSelection.SelectedDifficulty = (Difficulty)difficulty;
    RefreshInfo();
}
```
- `Select(index)`는 곡 강조·미리듣기(곡이 바뀔 때만)를 지금처럼 하고, 정보 표시는 `RefreshInfo()`로 옮겨 `song.GetChart(SongSelection.SelectedDifficulty)`의 노트로 계산한다. 난이도만 바꾸면 `Select`를 부르지 않으므로 미리듣기가 다시 시작되지 않는다.
- 선택 난이도의 채보가 없거나 노트 0개면 기존과 같이 START 비활성 + CANNOT PLAY(Enter도 무시).

### 4.4 신규 채보 제작
- 공통: 박자 격자에 맞추고, 같은 레인 최소 간격·롱노트 겹침·레인 0~3·음원 길이 안(마지막 노트 종료 < 음원 길이 − 1초)·롱노트 종료 > 시작을 생성 후 검사한다. 생성 스크립트는 일회성(프로젝트에 넣지 않음), 방법과 결과 수치는 이 계획서에 기록.
- **kick (악보 기반)**: 곡을 합성할 때 쓴 악보(140 BPM, 마디·박 단위 킥/스네어/하이햇/멜로디)를 그대로 다시 읽어 만든다.
  - Easy: 킥 위치(4분음표) 위주 + 마디 첫 박 강조, 같은 레인 최소 간격 1박, 동시치기 없음 또는 아주 적게(후렴 첫 박 정도), 롱노트는 긴 멜로디 음 몇 개만(단독, 다른 입력과 겹치지 않게).
  - Hard: Normal(기존) 위치를 모두 포함하는 방향으로 8분 하이햇·스네어 롤 일부·멜로디 16분을 추가, 동시치기(킥+멜로디)와 롱노트+탭 조합 사용, 같은 레인 최소 간격 1/8박.
- **희망 (음원 분석 기반)**: Hard를 만들 때 쓴 분석 결과(124 BPM, 첫 박자 0.129초, 대역별 온셋)를 그대로 쓰고 기준만 바꾼다.
  - Easy: 4분음표 격자만, 강한 온셋 위주, 동시치기 없음(또는 마디 첫 박 소수), 롱노트 소수(단독), 같은 레인 최소 간격 1박.
  - Normal: 4분 + 8분 격자(16분 없음), 동시치기 적당히, 롱노트 Hard의 절반 정도, 같은 레인 최소 간격 1/4박(약 0.48초)~1/8박.
- 제안 노트 수 (곡 길이·리듬 기준, 생성 후 실제 값 기록):

| 곡 | Easy | Normal | Hard |
|---|---|---|---|
| kick (90.6초) | 약 90~110 (초당 약 1.1) | **200** (기존, 초당 약 2.5) | 약 330~380 (초당 약 4) |
| 희망 (258초) | 약 250~300 (초당 약 1.1) | 약 520~600 (초당 약 2.2) | **950** (기존, 초당 약 3.75) |

### 4.5 AUTO TEST 기대 결과 계산 (채보 6개 각각)
- 판정 수 = 일반 노트 수 + 롱노트 수 × 2 (시작·끝)
- 틱 수 = 롱노트마다 시작 + 0.15초부터 0.15초 간격으로 종료 시각 직전까지의 개수 (GameplayManager `HandleHolds`와 같은 규칙)
- AUTO 3 기대값: 점수 = 판정 수 × 1000 + 틱 수 × 300, 최대 콤보 = 판정 수 + 틱 수, Perfect = 판정 수, Miss 0
- 확인된 기준: kick Normal 263,200 / 364 (Perfect 220, Tick 144), 희망 Hard 1,174,000 / 1,510 (Perfect 1,030, Tick 480)
- 음악 곡은 에디터 일시정지 상태에서 음악 시간을 1/30초씩 직접 진행해 확인한다(1/15초는 Perfect 범위보다 커서 Great가 생김 — song_select_plan 기록).

### 4.6 실제 생성 결과 (2026-11-16)
| 채보 | 파일 | 노트 | 롱노트 | 동시치기 | 초당 | 같은 레인 최소 간격 | AUTO 3 기대값 (Perfect / Tick → 점수 / 최대 콤보) |
|---|---|---|---|---|---|---|---|
| kick Easy | kick_easy.txt (신규) | 83 | 11 | 0 | 1.03 | 0.857초 | 94 / 92 → 121,600 / 186 |
| kick Normal | kick_chart.txt (기존) | 200 | 20 | 37 | 2.48 | 0.214초 | 220 / 144 → 263,200 / 364 |
| kick Hard | kick_hard.txt (신규) | 377 | 20 | 61 | 4.49 | 0.214초 | 397 / 144 → 440,200 / 541 |
| 희망 Easy | huimang_easy.txt (신규) | 259 | 16 | 0 | 1.03 | 0.968초 | 275 / 96 → 303,800 / 371 |
| 희망 Normal | huimang_normal.txt (신규) | 560 | 40 | 69 | 2.22 | 0.242초 | 600 / 240 → 672,000 / 840 |
| 희망 Hard | huimang_chart.txt (기존) | 950 | 80 | 210 | 3.75 | 0.242초 | 1,030 / 480 → 1,174,000 / 1,510 |

- kick Easy: 인트로·아웃트로 킥 1·3박, 멜로디는 반 마디(1·3박) 위치 음만, 롱노트는 한 마디 길이 멜로디·브레이크·마지막 화음만(다른 노트와 겹치지 않음), 동시치기 없음.
- kick Hard: 기존 Normal 200개를 모두 포함(확인)하고 인트로 3·4마디 킥 4분, A/B 하이햇 8분 뒷박, 두 번째 프레이즈 2·4박 스네어 동시치기, 브레이크 아르페지오 4분, 31마디 16분 스네어 롤(레인 1·2 번갈아)을 더함. 첫 노트 3.43초(0초 근처는 노트가 내려올 시간이 없어 제외), 동시치기 최대 2개.
- 희망 Easy/Normal: Hard와 같은 분석(124 BPM, 첫 박자 0.129초)·같은 생성 방식에서 기준만 바꿈. Easy = 4분 격자만, 동시치기 없음, 롱노트 16개(시작 간격 8박, 롱노트가 끝날 때까지 다른 노트 없음), 같은 레인 최소 1박. Normal = 4분·8분 격자(16분 없음), 동시치기 69곳, 롱노트 40개, 같은 레인 최소 1/2박. 같은 설정 방식으로 Hard 조건을 넣으면 기존 Hard와 같은 950개·80·210이 나와 생성 방식이 같음을 확인.

## 5. 구현 순서 및 진행 상태
- [x] 프롬프트·예시 이미지 검토, 로컬 소스·씬·채보 분석
- [x] 사용자 결정: 난이도 UI만 추가, 난이도 전역 기억(처음 Normal), 마우스만 (2026-11-16)
- [x] 구현 승인 (SongData 구조 변경 포함, 2026-11-16)
- [x] 1단계: Difficulty.cs, SongData(난이도 채보 3칸·HasDifficulties·GetChart)·SongSelection(SelectedDifficulty)·GameplayManager 수정 → 컴파일 오류 0
- [x] 2단계: 곡 선택 화면 난이도 버튼·코드(SelectDifficulty, RefreshInfo) → 컴파일, 스크린샷
- [x] 3단계: 신규 채보 4개 생성·검사, Kick·Huimang 에셋 연결
- [x] 4단계: 테스트 (6절)
- [ ] 사용자 플레이·청취 확인 (난이도 체감, 싱크)
- [x] 검증 결과 기록 (PLAN.md 2-2 #3은 계획서 저장 시 미작성 → 미확인)

## 6. 테스트 및 검증 방법
- C: Claude (컴파일, Play Mode 함수 직접 호출, AUTO TEST, 스크린샷) / U: 사용자 (실제 플레이 체감, 음악 싱크)

| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| DF-01 | 컴파일·참조 | 오류 0, 씬·에셋 참조 누락 없음, 기존 에셋 값 유지 | C | 통과 (컴파일 오류 0, SongSelectScreen difficultyGroup·버튼 3개 연결, Kick·Huimang 기존 chart·music·volume 값 유지 + 난이도 칸만 추가, Test Song 1~3 HasDifficulties false) |
| DF-02 | 신규 채보 검사 | ChartLoader 경고 0, 같은 레인 겹침 0, 레인 0~3, 음원 길이 안, 롱노트 종료 > 시작 | C | 통과 (4개 모두 레인 0~3, 같은 레인 간격 0.1초 미만 0건, 롱노트 종료 > 시작, 마지막 노트가 음원 길이 −1초 안. 4.6 표) |
| DF-03 | 난이도 차이 | 곡마다 Easy < Normal < Hard (노트 수, 초당 밀도, 동시치기·롱노트·1/8·1/16 비율) | C/U | C 통과 (kick 83 → 200 → 377개·초당 1.03 → 2.48 → 4.49·동시치기 0 → 37 → 61, 희망 259 → 560 → 950개·초당 1.03 → 2.22 → 3.75·동시치기 0 → 69 → 210) / U 미검증 (체감) |
| DF-04 | 버튼 표시 | kick·희망은 버튼 3개, Test Song 1~3은 숨김, 목록 5곡 그대로(중복 없음) | C | 통과 (kick·희망 선택 시 DifficultyGroup 표시, Test Song 1·2 숨김, 목록 5곡 그대로) |
| DF-05 | 난이도 선택 | 강조 표시, 노트 수·길이 갱신, 미리듣기 재생 위치 그대로 | C/U | C 통과 (Easy 초록·Normal 파랑·Hard 빨강 강조, NOTES kick 83/200/377, 희망 259/560/950 갱신, 난이도 바꾸는 동안 kick 미리듣기 2.56초 그대로) / U 미검증 |
| DF-06 | START / Enter | 선택 난이도 채보로 플레이 | C | 통과 (kick Hard START → 인게임 채보 kick_hard, 희망 Easy START → huimang_easy) |
| DF-07 | 플레이 불가 | 채보 없는 난이도(메모리에서만 비워 확인) → START 비활성·CANNOT PLAY | C | 통과 (kick Easy 채보를 메모리에서만 비움 → NOTES 0, CANNOT PLAY, START 비활성, StartSong 호출해도 이동 없음 → 복원) |
| DF-08 | RESTART / RETRY | 같은 곡·같은 난이도 채보로 다시 시작 | C | 통과 (일시정지 RESTART → kick_hard 다시 0초부터, 결과 화면 RETRY → huimang_easy) |
| DF-09 | 복귀·설정 왕복 | 결과·일시정지에서 곡 선택 복귀, 설정 화면 왕복 후 곡과 난이도 유지 | C | 통과 (일시정지 Song Select 복귀·설정 화면 왕복 후 kick·Hard 선택 유지, NOTES 377) |
| DF-10 | AUTO 3 (6채보) | kick Easy/Hard·희망 Easy/Normal은 4.5 계산값과 같음, kick Normal 263,200/364, 희망 Hard 1,174,000/1,510 | C | 통과 (6개 모두 4.6 계산값과 같음, Miss 0 — 음악 시간을 1/30초씩 직접 진행) |
| DF-11 | Test Song 회귀 | Test Song 1 35,800/61, Test Song 2 9,800/14, 곡 선택 표시 그대로 | C | 통과 (Test Song 1 35,800/61, Test Song 2 9,800/14, 곡 선택 표시 NOTES 20/7 그대로) |
| DF-12 | 기존 기능 회귀 | 배속·키 설정·오프셋·소리 설정·곡 기본 볼륨·미리듣기 정상 | C | C 통과 (배속 표시·미리듣기·곡 기본 볼륨(희망 0.1) 유지, 설정 화면 왕복 정상. 키·오프셋·소리 설정 코드 변경 없음) |
| DF-13 | 화면 배치 | 난이도 버튼·Message·START 겹침·잘림 없음 (스크린샷) | C/U | C 통과 (스크린샷: DIFFICULTY·버튼·START·CANNOT PLAY(START 오른쪽)·배속 줄 겹침 없음) / U 미검증 |
| DF-14 | 실제 플레이 | 난이도별 체감이 기준에 맞고 노트가 음악에 맞음 | U | 미검증 |

## 7. 검증 결과 및 미해결 사항
- 구현: 계획 4절 그대로. 곡 선택 화면 배치만 스크린샷을 보고 조정 — DIFFICULTY 글자가 LENGTH와 붙어 보여 (1000, −615)로, 버튼 y −660, START는 원래 위치(−760) 유지, CANNOT PLAY는 START 오른쪽 (1390, −775)으로 옮김(Message가 START와 배속 줄 사이에 들어갈 공간이 없어서).
- 검증 방식: Play Mode에서 버튼 onClick·함수 직접 호출, 인게임 채보 이름 확인, AUTO TEST(음악 시간 1/30초씩 직접 진행), 스크린샷.
- 결과: 6절 표대로 C 항목 모두 통과. 난이도별 체감과 음악 싱크(DF-14), 실제 클릭은 사용자 확인 필요.
- 검증 중 있었던 일:
  - 검증 스크립트가 음원 없는 곡의 미리듣기 time을 읽어 경고 2건(게임 코드 아님).
  - CANNOT PLAY 위치를 보려고 Play 중 Huimang의 hardChart를 메모리에서 비웠는데, ScriptableObject 에셋은 Play를 끝내도 값이 돌아오지 않아 비어 있는 상태가 남았다. 바로 huimang_chart.txt로 다시 연결해 저장했고, 파일에서 kick·희망 난이도 칸 6개가 모두 올바른 채보를 가리키는지 확인했다.
- 미해결: 난이도별 실제 플레이 체감·싱크(DF-14)는 사용자 확인 필요.

### 변경 이력
| 날짜 | 내용 |
|---|---|
| 2026-11-16 | GPT 작업 프롬프트·예시 이미지(사용자 제공)를 바탕으로 로컬 소스·씬·채보 분석 후 최초 작성 |
| 2026-11-16 | 사용자 결정 반영(추천안 확정) |
| 2026-11-16 | 구현 및 검증 (4.6·6·7절) |
