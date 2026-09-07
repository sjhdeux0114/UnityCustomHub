# UnityHubCustom (Unity Hub+)

Unity Hub의 주요 기능(프로젝트 관리, Unity Editor 버전 감지 및 실행)을 완벽히 대체하며, 레이아웃 깨짐이나 창 오류 발생 시 유용한 **[리셋후 열기 (Layout Reset & Open)]** 기능을 지원하는 Windows 독립 실행형 애플리케이션입니다.

---

## 🌟 주요 기능

### 1. 프로젝트 관리 (Projects)
- **기존 Unity Hub 프로젝트 자동 연동**: 기존 Unity Hub에 등록되어 있던 프로젝트 목록(`%APPDATA%\UnityHub\projects-v1.json`)을 자동으로 불러옵니다.
- **새 프로젝트 추가**: `+ 프로젝트 추가` 버튼으로 디렉토리를 선택하여 손쉽게 프로젝트를 등록할 수 있습니다.
- **Unity 버전 자동 파싱**: `ProjectSettings/ProjectVersion.txt`를 읽어 각 프로젝트의 Unity 버전을 자동 감지하고 설치된 에디터와 매칭합니다.
- **실시간 검색 및 필터**: 프로젝트 이름 또는 디렉토리 경로로 실시간 필터링할 수 있습니다.

### 2. 프로젝트 열기 모드 (2가지 옵션)
- **▶ 열기 (Open)**:
  - 매칭된 Unity Editor(`Unity.exe`)로 프로젝트를 바로 실행합니다.
- **⚡ 리셋후 열기 (Reset & Open)**:
  - 창 배치가 깨지거나 검은 화면, 서브 모니터 이동 후 윈도우가 사라지는 등 레이아웃 문제가 발생했을 때 사용합니다.
  - **프로젝트 Layout 정보 초기화**:
    - `<ProjectRoot>\UserSettings\Layouts\*` (예: `default-2023.dwlt`) 파일 삭제
    - `<ProjectRoot>\Library\CurrentLayout*.dwlt` 및 `CurrentMaximizeLayout.dwlt` 삭제
  - **Unity Editor 전역 Layout 정보 초기화**:
    - `%APPDATA%\Unity\Editor-5.x\Preferences\Layouts\current\*` 캐시 파일 삭제
    - Windows 레지스트리 `HKCU\Software\Unity Technologies\Unity Editor 5.x` 내 윈도우 레이아웃 및 좌표 키(`UnityEditor.SaveWindowLayout*`, `RestoredMainWindowSize*`, `IsMainWindowMaximized*` 등) 삭제
  - 초기화 직전 이전 레이아웃 파일은 `_Backup` 폴더에 안전하게 자동 백업됩니다.
  - 초기화 완료 후 깨끗한 순정(Default Factory) 레이아웃 상태로 Unity Editor를 자동 실행합니다.

### 3. 설치된 에디터 관리 (Installs)
- `C:\Program Files\Unity\Hub\Editor` 및 기본 경로에 설치된 모든 Unity Editor 버전을 자동 감지합니다.
- 사용자 지정 경로의 `Unity.exe`도 `+ 에디터 수동 찾기`로 손쉽게 등록할 수 있습니다.

### 4. 세부 설정 및 활동 로그 (Settings & Logs)
- 프로젝트 레이아웃, 에디터 전역 캐시, 레지스트리 초기화 옵션 개별 On/Off
- 백업 생성 옵션 및 Unity Hub 동기화 On/Off
- 실행 및 초기화 작업 내역을 실시간 로그 창에서 확인 가능

---

## 🚀 실행 및 빌드 방법

### 실행 방법
- `run.bat` 파일을 더블 클릭하거나,
- `bin\Release\UnityHubCustom.exe`를 바로 실행합니다.
  - .NET Framework 4.7.1 기반으로 별도의 런타임 설치 없이 모든 Windows 10/11 환경에서 즉시 실행됩니다.

### 재빌드 방법
- `build.bat`을 실행하면 시스템의 Visual Studio MSBuild를 사용하여 `bin\Release\UnityHubCustom.exe`로 자동 컴파일됩니다.
