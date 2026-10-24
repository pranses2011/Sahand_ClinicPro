; ═══════════════════════════════════════════════════════════════
; اسکریپت Inno Setup — نصب‌کنندهٔ خودبسندهٔ کلاینت درمانگاه سهند
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
Source: "..\publish\desktop\*"; DestDir: "{app}"; Excludes: "appsettings.json"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: WantFullInstall
; فقط فایل‌های به‌روزشده: خروجی سبک بدون Runtime؛ runtimeconfig نصب قبلی دست نمی‌خورد
Source: "..\publish\desktop-update\*"; DestDir: "{app}"; Excludes: "appsettings.json,Sahand.ClinicPro.Desktop.runtimeconfig.json,Sahand.ClinicPro.Desktop.deps.json"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: WantUpdateOnly
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

var
  InstallChoicePage: TInputOptionWizardPage;
  ExistingVersion: String;

// تشخیص نسخهٔ نصب‌شده و ساخت صفحهٔ انتخاب (فقط وقتی نصبی از قبل هست)
procedure InitializeWizard;
var
  ExistingDir: String;
begin
  ExistingVersion := GetExistingVersion();
  ExistingDir := GetExistingInstallDir();
  if (ExistingVersion <> '') and (ExistingDir <> '') and FileExists(ExistingDir + '\Sahand.ClinicPro.Desktop.exe') then
  begin
    InstallChoicePage := CreateInputOptionPage(wpWelcome,
      'نسخهٔ جدید سامانه درمانگاه سهند',
      'نسخهٔ ' + ExistingVersion + ' از سامانه روی این رایانه نصب است (در ' + ExistingDir + ')',
      'می‌خواهید نصب کامل انجام شود یا فقط فایل‌هایی که از نسخهٔ نصب‌شده به بعد تغییر کرده‌اند به‌روز شوند؟',
      True, False);
    InstallChoicePage.Add('نصب کامل — همهٔ فایل‌ها و Runtime از نو نصب می‌شوند (مطمئن‌تر، کمی طولانی‌تر)');
    InstallChoicePage.Add('فقط فایل‌های به‌روزرسانی‌شده از نسخهٔ ' + ExistingVersion + ' به بعد — سریع‌تر (Runtime نصب قبلی می‌ماند)');
    if FileExists(ExistingDir + '\hostfxr.dll') and FileExists(ExistingDir + '\coreclr.dll') and FileExists(ExistingDir + '\hostpolicy.dll') then
      InstallChoicePage.Values[1] := True
    else
      InstallChoicePage.Values[0] := True;
  end;
end;

function WantUpdateOnly(): Boolean;
begin
  Result := (InstallChoicePage <> nil) and InstallChoicePage.Values[1];
end;

function WantFullInstall(): Boolean;
begin
  Result := not WantUpdateOnly();
end;

// اگر «فقط به‌روزرسانی» انتخاب شد ولی Runtime نصب قبلی نیست، متوقف شو
function NextButtonClick(CurPageID: Integer): Boolean;
var
  ExistingDir: String;
begin
  Result := True;
  if (InstallChoicePage <> nil) and (CurPageID = InstallChoicePage.ID) and InstallChoicePage.Values[1] then
  begin
    ExistingDir := GetExistingInstallDir();
    if (ExistingDir = '') or not FileExists(ExistingDir + '\hostfxr.dll') or not FileExists(ExistingDir + '\coreclr.dll') or not FileExists(ExistingDir + '\hostpolicy.dll') then
    begin
      MsgBox('برای گزینهٔ «فقط فایل‌های به‌روزرسانی‌شده»، Runtime کامل نصب قبلی (hostfxr.dll/coreclr.dll/hostpolicy.dll) لازم است و پیدا نشد.' + #13#10 +
             'لطفاً گزینهٔ «نصب کامل» را انتخاب کنید.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;
