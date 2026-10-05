; ═══════════════════════════════════════════════════════════════
; اسکریپت Inno Setup — نصب‌کننده کلاینت درمانگاه سهند v1.7.0
; فارسی‌سازی پیام‌ها + بررسی پیش‌نیاز .NET 8 Desktop Runtime
; ═══════════════════════════════════════════════════════════════

#define MyAppName "سامانه درمانگاه سهند"
#define MyAppNameEn "Sahand ClinicPro"
#define MyAppVersion "1.7.0"
#define MyAppPublisher "Sahand ClinicPro Team"
#define MyAppURL "https://sahand-clinicpro.ir"
#define MyAppExeName "Sahand.ClinicPro.Desktop.exe"

[Setup]
; شناسه یکتا — تغییر در ارتقا نسخه باعث نصب موازی می‌شود
AppId={{7C2E9F3A-5B84-4D61-9A17-3E5C8D4F2B96}
AppName={#MyAppNameEn}
AppVersion={#MyAppVersion}
AppVerName={#MyAppNameEn} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\SahandClinicPro
DefaultGroupName={#MyAppNameEn}
UninstallDisplayName={#MyAppNameEn}
OutputDir=..\dist
OutputBaseFilename=SahandClinicPro-Setup-v{#MyAppVersion}
SetupIconFile=appicon.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
LicenseFile=LICENSE.txt

; پیام‌های رابط — فارسی
[Messages]
SelectDirDesc=محصص نصب را انتخاب کنید
SelectDirLabel3=نصب‌کننده برنامه را در پوشه زیر نصب خواهد کرد.
SelectDirBrowseLabel=برای ادامه روی «بعدی» بزنید، یا روی «جستجو» برای انتخاب پوشه دیگر.
DiskSpaceMBLabel=حداقل [mb] مگابایت فضای آزاد نیاز است.
SelectDirBrowsePath=محل:
StatusClosingApplications=بستن برنامه‌های در حال اجرا...
StatusCreateShortcut=ساخت میانبر...
StatusExtractFiles=استخراج فایل‌ها...
StatusRegisterFiles=ثبت فایل‌ها...
StatusRollback=بازگردانی تغییرات...
StatusRunProgram=راه‌اندازی برنامه...
WelcomeLabel1=خوش آمدید به نصب [name]
WelcomeLabel2=این نصب‌کننده [name/ver] را روی رایانه شما نصب می‌کند.%n%nتوصیه می‌شود پیش از ادامه، سایر برنامه‌ها را ببندید.
SelectTasksLabel2=وظایف اضافی نصب را انتخاب کنید و روی «بعدی» بزنید.

[CustomMessages]
farsi_language=فارسی
CreateDesktopIcon=ایجاد میانبر روی &دسکتاپ
CreateQuickLaunchIcon=ایجاد میانبر در نوار ابزار &Quick Launch
AdditionalIcons=میانبرهای اضافی:
LaunchProgram=اجرا %1
AssocFileExtension=&پیوند فایل‌های %2 با برنامه %1
AssocingFileExtension=در حال پیوند فایل‌های %2 با برنامه %1...
DesktopTooltip=سامانه جامع مدیریت درمانگاه سهند

[Languages]
; چیدمان فارسی از طریق پیام‌های سفارشی بالا پوشش داده شده است
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: uncheckedonce

[Files]
; خروجی publish دسکتاپ — توسط BUILD-INSTALLER.bat ساخته می‌شود
Source: "..\publish\desktop\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; فایل‌های پیکربندی نمونه
Source: "appsettings.client.example.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist; DestName: "appsettings.json"

[Dirs]
Name: "{app}\logs"; Flags: uninsneveruninstall

[Icons]
Name: "{group}\{#MyAppNameEn}"; Filename: "{app}\{#MyAppExeName}"; Comment: "{cm:DesktopTooltip}"
Name: "{group}\راهنمای کار با نرم‌افزار"; Filename: "{app}\docs\User-Guide.pdf"
Name: "{group}\حذف {#MyAppNameEn}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppNameEn}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; Comment: "{cm:DesktopTooltip}"
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppNameEn}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; بررسی پیش‌نیاز .NET 8 Desktop Runtime — در صورت نبود، صفحه دانلود باز می‌شود
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppNameEn}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"

[Code]
// ═════ بررسی .NET 8 Desktop Runtime پیش از نصب ═════
function IsDotNet8DesktopInstalled(): Boolean;
var
  releases: string;
begin
  Result := RegValueExists(HKLM, 'SOFTWARE\Microsoft\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', '8.') ;
  if not Result then
  begin
    // روش دوم: نبود کلید یعنی بررسی از طریق dotnet --list-runtimes غیرقابل اتکاست؛ کلید v8 در شاخه shared host
    Result := RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost\Microsoft.WindowsDesktop.App\8.');
  end;
end;

function InitializeSetup(): Boolean;
var
  answer: Integer;
  errCode: Integer;
begin
  Result := True;
  if not IsDotNet8DesktopInstalled() then
  begin
    answer := MsgBox(
      'برای اجرای این برنامه، .NET 8 Desktop Runtime لازم است که یافت نشد.' + #13#10 +
      'می‌خواهید اکنون صفحه دانلود آن باز شود؟ (پس از نصب Runtime، نصب‌کننده را دوباره اجرا کنید)',
      mbConfirmation, MB_YESNOCANCEL);
    if answer = IDYES then
    begin
      errCode := 0;
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, errCode);
    end;
    // ادامه نصب مجاز است ولی برنامه تا نصب Runtime اجرا نمی‌شود
    Result := answer <> IDCANCEL;
  end;
end;
