; ═══════════════════════════════════════════════════════════════
; اسکریپت Inno Setup — نصب‌کنندهٔ خودبسندهٔ کلاینت درمانگاه سهند
; نسخه از طریق /DMyAppVersion=... هنگام build قابل تعیین است
; ═══════════════════════════════════════════════════════════════

#define MyAppName "سامانه درمانگاه سهند"
#define MyAppNameEn "Sahand ClinicPro"
#ifndef MyAppVersion
  #define MyAppVersion "2.22.0"
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
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: uncheckedonce

[Files]
; خروجی publish دسکتاپ — توسط BUILD-INSTALLER.bat ساخته می‌شود
Source: "..\publish\desktop\*"; DestDir: "{app}"; Excludes: "appsettings.json"; Flags: ignoreversion recursesubdirs createallsubdirs
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
