# AutoLogout v0.2

실제 Downloads/AutoLogout-v0.1-source.zip을 읽고 수정한 소스 배포판입니다. 이전 대화의 본문은 참조 표시만 반환되어, 세부 설계는 원본 코드와 이번 요청을 기준으로 했습니다.

## 추가/수정 기능

- 창 닫기와 Windows 사용자 로그인 자동 실행 시 트레이 상주. 더블 클릭/메뉴로 설정 열기, 즉시 정리, 명시적 종료.
- 같은 Windows 세션에서 앱 중복 실행 방지. 이미 실행 중이면 트레이에서 여세요.
- Chrome/Edge 네이티브 호스트 연결 상태를 설정 창에 표시. 프로필별 호스트가 20초 이내 회신했을 때 연결됨으로 표시합니다. 설치 여부 탐지나 로그아웃 성공 판정은 아닙니다.
- 확장 팝업에서 연결 상태와 최근 정리 결과 확인. 호스트는 브라우저별/호스트별 상태 파일을 저장합니다.
- 실패한 정리 요청은 성공 처리하지 않고 5초 간격 재전송 시 다시 시도. 호스트 재연결은 5초 타이머와 1분 alarms로 복구 시도.
- v0.1의 사용자 로그인, 정상 종료/재시작/Windows 사용자 로그아웃 요청 저장 유지. 설정과 요청 파일은 임시 파일을 원자적으로 교체합니다.
- 사용자별 Inno Setup 설치 프로그램 소스와 빌드 스크립트 추가. 관리자 권한 없이 설치하며 확장 설치는 수동입니다.

## 빌드와 설치

Windows 10/11 x64, .NET 8 SDK 이상, Inno Setup 6가 필요합니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1
# Inno Setup 위치가 다르면 -Iscc "C:\...\ISCC.exe" 지정
```

산출물: out/app, out/host, out/installer/AutoLogout-0.2.0-Setup.exe. 스크립트는 컴파일러 종료 코드를 검사합니다. 설치 후 Chrome chrome://extensions 및 Edge edge://extensions에서 개발자 모드로 설치 경로의 extension 폴더를 각각 로드하세요. 프로필마다 설치해야 합니다.

각 확장 ID를 확인한 뒤 아래 명령을 실제 설치 경로에 맞춰 실행하세요. 확장 ID는 자동으로 알 수 없으므로 설치 프로그램이 임의로 등록하지 않습니다.

```powershell
powershell -ExecutionPolicy Bypass -File "$env:LOCALAPPDATA\Programs\AutoLogout\scripts\install-native-host.ps1" -HostExe "$env:LOCALAPPDATA\Programs\AutoLogout\host\AutoLogout.Host.exe" -ChromeId 실제Chrome확장ID -EdgeId 실제Edge확장ID
```

한 브라우저만 사용하면 다른 ID 인수는 생략합니다. 브라우저에서 확장을 다시 로드하고 앱에서 사이트와 자동 실행 옵션을 저장하세요. HTTPS origin만 한 줄에 하나 입력합니다. 먼저 별도 시험 프로필에서 즉시 정리와 결과를 확인하세요.

## 제한과 동작 범위

- Windows 시작은 사용자 로그인 후 앱 실행을 뜻하며 OS 부팅 전 정리가 아닙니다.
- 브라우저가 실행되어야 정리됩니다. 종료 시 완료는 보장할 수 없으며 브라우저가 먼저 종료되면 다음 실행 때 저장된 최신 요청을 재시도합니다.
- signal.json에는 최신 요청 하나만 남습니다. 여러 요청은 합쳐지며 감사 목적의 요청 큐가 아닙니다. 실패 중 사이트 설정을 변경하면 재시도는 최신 설정을 사용합니다.
- 강제 전원 차단이나 프로세스 강제 종료는 종료 요청을 저장하지 못할 수 있습니다. 취소된 Windows 종료도 이미 정리를 요청했을 수 있습니다.
- 브라우저 데이터 삭제이며 서버 세션 해지가 아닙니다. SSO로 다시 로그인될 수 있고 쿠키 정리는 같은 등록 도메인의 다른 사이트에도 영향을 줄 수 있습니다. LocalStorage/IndexedDB 등 오프라인 데이터도 삭제됩니다.
- 연결 상태 파일은 종료 후 남아도 20초 뒤 연결됨에서 제외됩니다. 오래된 상태 파일 자동 삭제는 구현하지 않았습니다. 저장 위치: %LOCALAPPDATA%\AutoLogout.
- 상태 파일의 profile 값은 확장 설치별 임의 식별자이며 실제 프로필 이름은 아닙니다.
- 설치 프로그램은 서명되지 않습니다. 업데이트/제거 전 트레이 메뉴에서 앱을 종료하고 브라우저도 닫으세요. 제거 시 앱의 Run 항목/네이티브 호스트 등록 키를 삭제하며 사용자 설정은 보존합니다. 확장은 브라우저에서 따로 제거하세요.

## 검증

tests/extension.test.cjs는 Node.js로 실행합니다. 실패 후 재시도, 성공 요청 중복 방지, 잘못된 origin 거부, 빈 대상, Edge 결과 회신을 모의 브라우저 API로 검사합니다. 실제 브라우저/Windows 이벤트 테스트를 대신하지 않습니다. 실제 빌드 결과는 BUILD-STATUS.md를 참고하세요.

원본 [v0.1 문서](docs/README-v0.1.md)는 v0.1 참고 문서이며 v0.2 동작/빌드 상태는 이 문서가 우선합니다.
