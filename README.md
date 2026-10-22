# rdp-key-swap-for-mac

MacBook에서 RDP로 Windows에 접속했을 때, 전달되는 modifier 키를 다시 해석하는 Windows 트레이 앱입니다.

macOS 단축키를 Windows 단축키로 바꾸는 프로그램이 아닙니다. **등록한 Mac 클라이언트에서 접속한 경우에만 키 자체를 재해석**하고, 조합의 의미는 Windows와 앱이 결정합니다.

| MacBook에서 누른 키 | RDP가 전달하는 키 | 앱이 만드는 키 |
|---|---|---|
| Control | Ctrl | Ctrl (그대로) |
| Command | Windows 키 | Alt |
| Option | Alt | Windows 키 |

좌우 구분은 유지합니다. 우측 Command는 우측 Alt가 되므로, 한국어 101키 종류 1에서는 한/영 전환이 그대로 동작합니다. 우측 Control은 Ctrl 그대로이므로 한자키 동작도 유지됩니다.

## 적용 조건

아래 두 조건을 모두 만족할 때만 변환합니다. 그 외에는 모든 입력을 그대로 통과시킵니다.

1. 현재 사용자 세션이 RDP 세션이다.
2. WTS API로 읽은 RDP 클라이언트 이름이 설정에 등록한 이름 중 하나와 일치한다.

이름은 앞뒤 공백을 제거하고 대소문자를 구분하지 않으며 완전 일치로 비교합니다. 빈 항목은 무시하고 중복은 하나로 취급합니다.

## 빌드와 실행

.NET 8 SDK가 필요합니다.

```text
dotnet build
dotnet run --project src/RdpKeySwap
```

빌드 결과는 `src/RdpKeySwap/bin/Debug/net8.0-windows/RdpKeySwap.exe`이며, 이 파일을 직접 실행해도 됩니다.

## 릴리즈 빌드 (단일 실행 파일)

```text
dotnet publish src/RdpKeySwap/RdpKeySwap.csproj -p:PublishProfile=win-x64
```

결과는 `dist/RdpKeySwap.exe` 하나입니다. win-x64 자체 포함(self-contained) 단일 파일이라 .NET 런타임을 설치하지 않은 PC에서도 실행되며, 배포할 때는 이 파일만 복사하면 됩니다. 설정 프로필은 `src/RdpKeySwap/Properties/PublishProfiles/win-x64.pubxml`에 있습니다.

## 사용

- 실행하면 설정 창이 열립니다. 이름을 추가·삭제하면 즉시 저장되고 적용 조건이 다시 평가됩니다.
- 창을 닫아도 트레이에서 계속 실행됩니다. 설정을 다시 열려면 트레이 아이콘을 두 번 클릭하거나 메뉴에서 `설정 열기`를 선택합니다.
- `Windows 로그인 시 자동 실행`을 켜면 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`에 이 앱의 값만 등록하고, 끄면 그 값만 삭제합니다.
- 설정은 `%APPDATA%\RdpKeySwap\settings.json`에 저장됩니다.

```json
{
  "ClientNames": [
    "example-mac"
  ]
}
```

- 같은 세션에서 중복 실행하면 기존 인스턴스가 유지되고 새 프로세스는 종료됩니다. 세션마다 독립적으로 실행되므로 다른 RDP 세션의 판정과 섞이지 않습니다.

## 입력 진단

실제 RDP 경로에서 어떤 코드가 도착하는지 확인할 때 사용합니다. 변환하지 않고 관측만 합니다.

```text
RdpKeySwap.exe --diagnose
```

가상 키, 스캔 코드, extended/injected 플래그, 누름/뗌과 함께 앱이 적용할 변환 결과를 출력합니다. `Esc` 또는 120초 후 종료합니다.

### 관리자 권한 앱에서도 변환하기

앱이 일반 사용자 권한이면 Windows UIPI 때문에 관리자 권한 앱으로 입력을 보낼 수 없습니다. 그 앱들에서는 변환이 적용되지 않고 원래 키(Command = Windows 키, Option = Alt)가 그대로 전달됩니다.

설정 창 상태 영역의 `권한` 행에 현재 실행 권한이 표시됩니다. 일반 사용자 권한이면 그 아래에 안내와 `관리자 권한으로 등록하고 다시 시작` 버튼이 나타납니다. 누르면 UAC 확인 후 다음을 수행합니다.

- `schtasks`로 현재 사용자의 로그온 작업(`RdpKeySwap`, `RunLevel=Highest`, 대화형)을 등록
- `HKCU\...\Run` 값을 지워 중복 실행 방지
- 관리자 권한으로 다시 시작

이후에는 로그인할 때마다 UAC 없이 관리자 권한으로 실행됩니다. 해제하려면 `Windows 로그인 시 자동 실행`을 끄면 되고, 이때 등록된 작업도 함께 삭제됩니다.

## 구조

| 경로 | 역할 |
|---|---|
| `src/RdpKeySwap/Program.cs` | 진입점, 단일 인스턴스, 시작 옵션 |
| `src/RdpKeySwap/AppController.cs` | 훅·세션 감시·적용 조건·눌림 상태 관리 |
| `src/RdpKeySwap/AppStatus.cs` | 적용 조건 판정 (순수 로직) |
| `src/RdpKeySwap/Keyboard/KeyMap.cs` | 유일한 변환 규칙 |
| `src/RdpKeySwap/Keyboard/RemapState.cs` | 누름/뗌 대응과 해제 시 정리 |
| `src/RdpKeySwap/Keyboard/KeyboardHook.cs` | `WH_KEYBOARD_LL` 설치와 이벤트 전달 |
| `src/RdpKeySwap/Keyboard/KeyInjector.cs` | `SendInput`과 합성 이벤트 마커 |
| `src/RdpKeySwap/Session/WtsSession.cs` | 세션 종류와 클라이언트 이름 조회 |
| `src/RdpKeySwap/Session/SessionWatcher.cs` | 세션 연결·해제·잠금 알림 |
| `src/RdpKeySwap/Settings/` | 이름 목록 저장, 이름 비교, 자동 실행 |
| `src/RdpKeySwap/Ui/` | 아이콘 로드, 트레이 아이콘과 설정 창 |
| `src/RdpKeySwap/Assets/app.ico` | 실행 파일·창·트레이가 함께 쓰는 아이콘 (16~256px 9개 크기) |

설계상 지켜야 하는 동작:

- 조건이 사라지면 프로그램이 만든 키를 즉시 떼어 modifier가 눌린 채 남지 않게 하고, 그 뒤 도착하는 원본 key-up은 버립니다.
- 변환된 키는 가상 키 코드가 아니라 **스캔 코드 이벤트**로 보냅니다. 그래야 현재 키보드 레이아웃이 물리 키와 똑같이 해석합니다.
  - 한국어 101 종류 1은 `0x38 + extended → VK_HANGUL`, `0x1D + extended → VK_HANJA`로 매핑합니다.
  - 가상 키로 `VK_RMENU`를 직접 보내면 이 변환을 건너뛰어 Alt가 전달되고, IME가 스캔 코드를 보고 한/영으로 처리해 두 동작이 겹칩니다.
- 앱이 만든 입력에는 `dwExtraInfo` 마커를 넣어 다시 변환하지 않습니다.
- 훅 콜백에서는 조회·파일 접근·UI 갱신을 하지 않고, 미리 계산한 상태만 참조합니다.

## 테스트

```text
dotnet test
```

변환 표, 누름/뗌 상태와 해제, 이름 비교 규칙, 설정 저장, 적용 조건 판정을 검증합니다.

## 지원 범위

- 일반 사용자 권한으로 동작합니다. 관리자 권한으로 실행된 앱에는 `SendInput`이 UIPI로 차단되므로 변환이 적용되지 않고, 원래 키(Command = Windows 키, Option = Alt)가 그대로 전달됩니다.
- 로그인 화면, UAC 보안 데스크톱, Ctrl+Alt+Delete 같은 보안 입력은 다루지 않습니다.
- Mac 또는 RDP 클라이언트가 먼저 가로채서 호스트에 전달하지 않는 키는 복구할 수 없습니다.
- Windows 키보드 레이아웃이나 IME 설정을 변경하지 않습니다.
