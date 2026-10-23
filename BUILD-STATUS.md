# v0.2 검증 결과 — 2026-10-23

## 성공

- .NET SDK 9.0.318, net8.0-windows: WPF 앱 Release 빌드 성공, 경고 0/오류 0.
- net8.0: Native Messaging 호스트 Release 빌드 성공, 경고 0/오류 0.
- tests/extension.test.cjs: 실패 후 재시도, 성공 중복 방지, 잘못된 origin 거부, 빈 대상 처리, Edge 결과 전송 통과. 모의 API 테스트입니다.
- 실제 호스트 DLL을 실행하고 4바이트 little-endian 길이 + JSON 입력/출력을 검사했습니다. 별도 데이터 폴더에서 저장된 요청 전달, Chrome heartbeat 상태 파일 생성, stdin EOF 후 종료 코드 0을 확인했습니다.

## 미검증/미생성

- Chrome/Edge 실제 확장 설치, 실제 쿠키/사이트 데이터 삭제, 다중 프로필, 트레이 조작, Windows 로그인/종료/재시작/사용자 로그아웃은 직접 시험하지 않았습니다.
- Inno Setup 6가 설치되어 있지 않아 설치 EXE는 컴파일하지 않았습니다. .iss와 빌드 지원만 포함합니다.
- self-contained win-x64 publish는 실행하지 않았습니다. Release 빌드 성공이 self-contained 배포나 설치 프로그램 성공을 뜻하지 않습니다.
- 코드 서명과 설치/제거 시험은 수행하지 않았습니다.
- ZIP은 소스 배포판입니다. 설치 EXE, bin/obj 및 환경별 빌드 캐시는 포함하지 않습니다.

## 환경 관련 기록

최초 PowerShell 경로에서는 작업 폴더 생성 및 .NET 초기 파일 생성 오류가 발생했습니다. 파일 작업 경로를 보완하고 빌드 하위 프로세스의 ProgramData 등 Windows 환경 변수를 제공한 뒤 로컬 참조 팩을 사용한 restore/build로 성공했습니다. 일반적인 NuGet 초기화도 이 환경에서는 path1 null 오류가 발생하여, 검증 빌드는 packageSources를 비운 별도 작업용 NuGet 설정을 사용했습니다. 프로젝트 소스나 배포 빌드 스크립트에 이 환경 우회 설정은 강제하지 않았습니다.

## 재검증

```powershell
dotnet build src/AutoLogout.App/AutoLogout.App.csproj -c Release
dotnet build src/AutoLogout.Host/AutoLogout.Host.csproj -c Release
node tests/extension.test.cjs
powershell -ExecutionPolicy Bypass -File scripts/build-installer.ps1
```

Native host의 AUTOLOGOUT_DATA_DIR 환경 변수는 프로토콜 시험용 격리 폴더를 지정합니다. 기본 실행은 %LOCALAPPDATA%\AutoLogout을 사용합니다.

실사용 전 시험 프로필에서 확장 재연결, 즉시 정리 결과, 브라우저가 닫힌 동안 요청 저장/다음 시작 재시도, 트레이 닫기/재열기, 자동 실행, 정상 종료/사용자 로그아웃, 설치/제거를 확인하세요.
