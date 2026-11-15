# music_game (Unity Project)

## 1. 프로젝트 개요 (Project Overview)

- **프로젝트명:** music_game
- **장르:** 리듬게임
- **타겟 플랫폼:** PC
- **참고 게임:** DJMAX, EZ2ON
- **핵심 게임플레이:** 음악과 리듬에 맞춰 노트를 입력하고 버튼을 조작하는 리듬게임.
- **개발 목표:** 음악에 맞춰 등장하는 노트를 정해진 타이밍에 입력하는 PC 리듬게임을 개발한다.

## 2. 개발 환경 (Tech Stack & Environment)

| 항목 | 설정 |
| --- | --- |
| Unity Version | Unity 6.6 (6000.6.9f1, 실제 버전 확인 필요) |
| Render Pipeline | URP (Universal Render Pipeline) |
| Scripting Backend | Mono |
| Input System | Legacy Input Manager |
| Programming Language | C# |
| 주요 에셋 및 추가 패키지 | None |

실제 Unity 버전은 `ProjectSettings/ProjectVersion.txt`에서 확인한다.
설치된 패키지는 `Packages/manifest.json`에서 확인한다.

## 3. 핵심 기능 (Core Features)

아래는 개발 예정 기능이며, 구현 완료를 의미하지 않는다.

- **노트 입력:** 음악에 맞춰 등장하는 노트를 키보드로 입력한다.
- **싱크 보정:** 오프셋 설정을 통해 음악과 노트의 타이밍을 보정한다.
- **점수 및 콤보 시스템:** 노트 입력 결과에 따라 점수와 콤보를 처리한다.
- **롱노트 시스템:** 지정된 시간 동안 키를 유지하는 노트를 처리한다.
- **곡 선택 화면:** 플레이할 곡을 선택한다.
- **게임 결과 화면:** 플레이 결과를 표시한다.
- **키 설정 및 변경:** 사용자가 입력 키를 설정하고 변경한다.
- **노트 배속 설정:** 노트의 표시 속도를 조절한다.

세부 판정 기준, 점수 계산식, UI 구성 등은 해당 기능 작업 시 결정한다.

## 4. 프로젝트 구조 (Project Structure)

```text
music_game/
├── Assets/             # Unity 에셋 및 소스 코드
├── Packages/           # 패키지 설정
├── ProjectSettings/    # Unity 프로젝트 설정
├── README.md           # 프로젝트 개요 및 개발 환경
└── CLAUDE.md           # Claude 작업 규칙
```

위 구조는 Unity 프로젝트의 기본 구조를 나타낸다. 실제 하위 폴더 및 파일은 프로젝트에서 확인한다.

## 5. 실행 방법 (Getting Started)

1. Unity Hub에서 프로젝트 폴더를 연다.
2. `ProjectSettings/ProjectVersion.txt`에 명시된 Unity 버전을 사용한다.
3. Unity Editor에서 실제 게임 실행용 Scene을 확인하고 연다.
4. Play 버튼을 눌러 게임을 실행한다.

실행용 Scene의 경로 및 이름은 실제 프로젝트 확인 후 명시한다.

## 6. 개발 문서 (Development Documents)

- `README.md`: 프로젝트 개요, 개발 환경 및 개발 예정 기능.
- `CLAUDE.md`: Claude가 작업할 때 준수해야 할 규칙.
- `plan.md`: 사용자가 작성을 요청한 경우에만 생성하는 특정 작업의 상세 구현 계획서.

계획서는 프로젝트 전체가 아닌 개별 기능 또는 작업 단위로 작성한다.
계획서 작성 요청만으로 실제 코드 구현을 시작하지 않는다.

## 7. 개발 진행 원칙 (Development Guidelines)

- 사용자가 요청한 기능과 작업 범위만 구현한다.
- 추가 기능이 필요하면 사용자에게 먼저 확인한다.
- 기존 소스 파일을 확인한 후 구현한다.
- 개발 환경과 프로젝트 설정은 사용자 승인 없이 변경하지 않는다.
- 구현 상태 및 테스트 결과는 실제 확인한 내용을 기준으로 기록한다.

세부 작업 규칙은 `CLAUDE.md`를 참조한다.
