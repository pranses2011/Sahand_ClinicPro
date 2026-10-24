; ═══════════════════════════════════════════════════════════════
; اسکریپت Inno Setup — نصب‌کنندهٔ «به‌روزرسانی» کم‌حجم کلاینت درمانگاه سهند
; ═══ تفاوت با نصب کامل ═══
; این بسته فقط فایل‌های خود برنامه را (بدون .NET Runtime) می‌آورد؛ خروجی
; publish وابسته به فریم‌ورک است و روی نصب کاملِ موجود سوار می‌شود —
; Runtime خودبسندهٔ نصب کامل در پوشهٔ برنامه می‌ماند و میزبان .NET همان
; را از کنار برنامه پیدا می‌کند؛ به همین دلیل حجمش چند مگابایت است.
; اگر نصب کاملی وجود نداشته باشد، نصب‌کننده با پیام روشن متوقف می‌شود.
; نسخه از طریق /DMyAppVersion=... هنگام build قابل تعیین است
; ═══════════════════════════════════════════════════════════════

#define MyAppName "سامانه درمانگاه سهند"
#define MyAppNameEn "Sahand ClinicPro"
#ifndef MyAppVersion
  #define MyAppVersion "2.23.0"
#endif
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
DefaultDirName={code:GetInstallDirDefault}
DefaultGroupName={#MyAppNameEn}
UninstallDisplayName={#MyAppNameEn}
OutputDir=..\dist
OutputBaseFilename=SahandClinicPro-Update-v{#MyAppVersion}
SetupIconFile=appicon.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
LicenseFile=LICENSE.txt

; پیام‌های رابط — فارسی
[Messages]
SelectDirDesc=مسیر نصب را انتخاب کنید
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
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; خروجی publish دسکتاپ — توسط BUILD-INSTALLER.bat ساخته می‌شود
Source: "..\publish\desktop-update\*"; DestDir: "{app}"; Excludes: "appsettings.json,Sahand.ClinicPro.Desktop.runtimeconfig.json,Sahand.ClinicPro.Desktop.deps.json"; Flags: ignoreversion recursesubdirs createallsubdirs
; پیکربندی کلاینت فقط در نصب اول ایجاد شود تا به‌روزرسانی نشانی سرور را بازنویسی نکند
Source: "appsettings.client.example.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist; DestName: "appsettings.json"

[Dirs]
Name: "{app}\logs"; Flags: uninsneveruninstall

[Icons]
Name: "{group}\{#MyAppNameEn}"; Filename: "{app}\{#MyAppExeName}"; Comment: "{cm:DesktopTooltip}"
Name: "{group}\راهنمای نصب و راه‌اندازی"; Filename: "{app}\docs\install-guide.html"
Name: "{group}\راهنمای راه‌اندازی دیتابیس"; Filename: "{app}\docs\sql-setup-guide.html"
Name: "{group}\حذف {#MyAppNameEn}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppNameEn}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; Comment: "{cm:DesktopTooltip}"
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppNameEn}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; کلاینت self-contained است و .NET Runtime جداگانه لازم ندارد
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppNameEn}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"


[Code]
const
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{7C2E9F3A-5B84-4D61-9A17-3E5C8D4F2B96}_is1';

function ReadUninstallValue(const ValueName: String): String;
var
  S: String;
begin
  Result := '';
  if RegQueryStringValue(HKLM, UninstallKey, ValueName, S) then Result := S
  else if RegQueryStringValue(HKLM32, UninstallKey, ValueName, S) then Result := S
  else if RegQueryStringValue(HKLM64, UninstallKey, ValueName, S) then Result := S
  else if RegQueryStringValue(HKCU, UninstallKey, ValueName, S) then Result := S;
end;

function GetExistingInstallDir(): String;
var
  S: String;
begin
  S := ReadUninstallValue('InstallLocation');
  if (Length(S) > 3) and (Copy(S, Length(S), 1) = '\') then
    Delete(S, Length(S), 1);
  Result := S;
end;

function GetInstallDirDefault(Param: String): String;
begin
  Result := GetExistingInstallDir();
  if Result = '' then
    Result := ExpandConstant('{autopf}\SahandClinicPro');
end;

function GetExistingVersion(): String;
begin
  Result := ReadUninstallValue('DisplayVersion');
end;

// به‌روزرسانی فقط روی نصب کامل سوار می‌شود. مسیر نصب از رجیستری خوانده
// می‌شود (نصب قبلی ممکن است در مسیر دلخواه باشد، نه لزوماً Program Files).
// runtimeconfig نصب کامل نباید با نسخهٔ وابسته به فریم‌ورک بازنویسی شود؛
// Runtime کنار برنامه (hostfxr.dll و فایل‌های همراهش) میزبان برنامه است.
function InitializeSetup(): Boolean;
var
  AppDir, Ver: String;
begin
  Result := True;
  AppDir := GetExistingInstallDir();
  if AppDir = '' then
    AppDir := ExpandConstant('{autopf}\SahandClinicPro');
  if not FileExists(AppDir + '\Sahand.ClinicPro.Desktop.exe') then
  begin
    MsgBox('نصب سامانه درمانگاه سهند روی این رایانه پیدا نشد (در مسیر ' + AppDir + ').' + #13#10 +
           'این فایل فقط «به‌روزرسانی» است؛ ابتدا فایل نصب کامل (Setup) را نصب کنید و سپس این به‌روزرسانی را اجرا کنید.',
           mbError, MB_OK);
    Result := False;
    Exit;
  end;
  if not FileExists(AppDir + '\hostfxr.dll') or not FileExists(AppDir + '\coreclr.dll') or not FileExists(AppDir + '\hostpolicy.dll') then
  begin
    MsgBox('در مسیر نصب برنامه، Runtime کامل نیست (hostfxr.dll/coreclr.dll/hostpolicy.dll).' + #13#10 +
           'احتمالاً نصب قبلی ناقص است؛ فایل نصب کامل (Setup) نسخهٔ جدید را اجرا کنید — خودش نصب قبلی را پیدا می‌کند و می‌توانید «نصب کامل» را انتخاب کنید.',
           mbError, MB_OK);
    Result := False;
    Exit;
  end;
  Ver := GetExistingVersion();
  if Ver <> '' then
    Log('نسخهٔ نصب‌شدهٔ شناسایی‌شده: ' + Ver + ' در ' + AppDir);
end;
