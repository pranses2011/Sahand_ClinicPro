# -*- coding: utf-8 -*-
"""
╔══════════════════════════════════════════════════════════════════════════╗
║  هستهٔ مشخصاتِ بازطراحی ۲ — سهند ClinicPro                                ║
║  هر فرم «طراحیِ اختصاصیِ خود» را دارد: کهن‌الگو، اندازه‌ها، دادهٔ نمونه،     ║
║  آیکون حرفه‌ای و ارتباطات. این فایل تنها منبع حقیقت برای generate2.py است. ║
╚══════════════════════════════════════════════════════════════════════════╝

کهن‌الگوها (archetype) — هر فرم یکی از این‌هاست، نه یک قالبِ یکسان برای همه:

  dashboard        پیشخوان: ردیف شاخص + نمودارها + فید فعالیّت + اقدام‌های سریع
  list-detail      فهرست با جدول + پنل جزئیات در کنار
  list-dense       جدول متراکم با نوار ابزار (لاگ، حسابداری)
  list-cards       شبکهٔ کارت‌ها
  kanban           ستون‌های کانبان (نوبت‌ها، شیفت‌ها)
  form-std         فرمِ گروه‌بندی‌شده با اندازه‌های گوناگون فیلد
  form-narrow      فرمِ باریک (ورود، گفتگوهای کوچک)
  form-wizard      فرمِ چندمرحله‌ای
  form-lines       فرم + اقلامِ قابل ویرایش (فاکتور، نسخه، پذیرش)
  detail-tabs      جزئیات با سربرگ خلاصه + تب‌ها
  detail-timeline  جزئیات با خط زمان (پرونده بیمار)
  settings         تنظیمات با ناوبری کناری
  dialog           گفتگوی کوچک
  viewer           نمایشگر تمام‌صفحه با ریل ابزار
  report           گزارش‌ساز: فیلترها + پیش‌نمایش
  pos              پایانه فروش: کاتالوگ + سبد
  shell            قاب اصلی
  splash           صفحه آغازین
"""

FORMS = []

# ────────────────────────────────────────────────────────────────────────
#  ابزارهای تعریف
# ────────────────────────────────────────────────────────────────────────

def Fd(p, t, l, s=6, req=False, ic=None, v=None, hint=None, opts=None):
    """تعریف یک فیلد.

    p=ویژگی  t=نوع  l=برچسب  s=اندازه از ۱۲ ستون  req=الزامی
    ic=آیکون  v=مقدار نمونه  hint=راهنما  opts=گزینه‌های کشو
    """
    return dict(p=p, t=t, l=l, s=s, req=req, ic=ic, v=v, hint=hint, opts=opts)


def Col(b, h, w=None, al="r", k="text", ic=None):
    """تعریف یک ستونِ جدول.

    b=اتصال  h=سرستون  w=عرض  al=تراز(r|l|c)  k=نوع(text|num|money|chip|who|date|icon|bool)
    """
    return dict(b=b, h=h, w=w, al=al, k=k, ic=ic)


def Kpi(ic, l, v, tr=None, dir="up", ac=None):
    """کارت شاخص. ac=رنگ (teal|green|amber|rose|blue|violet)"""
    return dict(ic=ic, l=l, v=v, tr=tr, dir=dir, ac=ac or "teal")


def Act(c, l, st="s", ic=None):
    """دکمهٔ اقدام. st=p|s|g|d|ok"""
    return dict(c=c, l=l, st=st, ic=ic)


def Tb(k, l, w=None, v=None, ic=None, opts=None):
    """ابزارِ نوار فیلتر. k=search|combo|date|check|seg|btn"""
    return dict(k=k, l=l, w=w, v=v, ic=ic, opts=opts)


def Sec(t, ic, fields, note=None, cols=None):
    """بخشِ فرم."""
    return dict(t=t, ic=ic, fields=fields, note=note, cols=cols)


def F(name, title, icon, arch, subtitle="", group="عمومی",
      kpis=None, toolbar=None, cols=None, rows=None, sections=None,
      actions=None, tabs=None, rel=None, notes=None, aside=None,
      total=None, hero=None, steps=None, panels=None, cards=None):
    """ثبت مشخصات کامل یک فرم."""
    FORMS.append(dict(
        name=name, title=title, icon=icon, arch=arch, subtitle=subtitle,
        group=group,
        kpis=kpis or [], toolbar=toolbar or [], cols=cols or [], rows=rows or [],
        sections=sections or [], actions=actions or [], tabs=tabs or [],
        rel=rel or [], notes=notes or "", aside=aside, total=total, hero=hero,
        steps=steps or [], panels=panels or [], cards=cards or [],
    ))


# ────────────────────────────────────────────────────────────────────────
#  داده‌های کمکیِ غنی برای سلول‌ها
# ────────────────────────────────────────────────────────────────────────

def chip(text, tone="nu", dot=True):
    return ("chip", text, tone, dot)

def who(name, sub, av="", tone=""):
    return ("who", name, sub, av, tone)

def num(v, unit=""):
    return ("num", v, unit)

def ico(name, tone="nu"):
    return ("icon", name, tone)

def dtx(v):
    return ("date", v)


# ╔═════════════════════════════════════════════════════════════════════╗
# ║  🏠 گروه ۱ — پیشخوان و ناوبری                                        ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("DashboardView", "پیشخوان", "layout-dashboard", "dashboard",
  "نمای کلیِ وضعیت امروز درمانگاه — یک نگاه برای همه‌چیز", "پیشخوان",
  kpis=[
      Kpi("users", "بیماران امروز", "۴۸", "+۱۲٪", "up", "teal"),
      Kpi("calendar-check", "نوبت‌های امروز", "۳۶", "+۸٪", "up", "blue"),
      Kpi("banknote", "درآمد امروز", "۲۸,۴۵۰,۰۰۰", "+۲۳٪", "up", "green"),
      Kpi("timer", "میانگین انتظار", "۱۴ دقیقه", "−۶٪", "down", "amber"),
      Kpi("triangle-alert", "هشدارهای فعال", "۳", None, "flat", "rose"),
  ],
  actions=[Act("NewReception", "پذیرش جدید", "p", "user-plus"),
           Act("NewAppointment", "نوبت جدید", "s", "calendar-plus"),
           Act("Refresh", "تازه‌سازی", "g", "refresh-cw")],
  rel=[("پیشخوان", "پذیرش و صف", "پذیرش جدید می‌سازد"),
       ("پیشخوان", "نوبت‌دهی", "نوبت امروز را نشان می‌دهد"),
       ("پیشخوان", "صورتحساب", "درآمد از فاکتورها می‌آید")],
  notes="پنج کارت شاخص در بالا، «صف انتظار زنده» در ستون راست، نمودار درآمد ۳۰ روزه و نمودار توزیع ویزیت در وسط، و فید فعالیّت‌های اخیر در پایین.")

F("LoginWindow", "ورود به برنامه", "log-in", "form-narrow",
  "احراز هویت کاربر با نام کاربری و رمز عبور", "پیشخوان",
  sections=[Sec("ورود به حساب", "log-in", [
      Fd("UserName", "text", "نام کاربری", 12, True, "user", "admin"),
      Fd("Password", "password", "رمز عبور", 12, True, "lock", "••••••••"),
      Fd("RememberMe", "check", "مرا در این دستگاه به خاطر بسپار", 12, False, None, True),
  ])],
  actions=[Act("Login", "ورود", "p", "log-in"), Act("Cancel", "انصراف", "g")],
  hero=dict(features=[("shield-check", "داده‌ها با رمزنگاری AES-256 محافظت می‌شوند"),
                     ("database", "پشتیبان‌گیری خودکار هر شب"),
                     ("monitor", "سازگار با ویندوز ۱۰ و ۱۱")]),
  rel=[("ورود", "پنجره اصلی", "پس از احراز هویت"),
       ("ورود", "تغییر رمز عبور", "در صورت MustChangePassword")],
  notes="پنلِ راست شامل برند و سه مزیت کلیدی است؛ فرم در پنل چپ و با اندازه‌ی کاملِ ستون.")

F("SplashWindow", "در حال بارگذاری", "loader", "splash",
  "صفحه آغازین برنامه", "پیشخوان",
  notes="نشان برنامه با حلقهٔ پیشرفت و نوار وضعیتِ مرحله‌ای (در حال اتصال… ← بارگذاری تنظیمات… ← آماده).")

F("MainWindow", "پنجره‌ی اصلی", "layout-panel-left", "shell",
  "قاب اصلی برنامه: ناوبری، جستجوی یکپارچه، اعلان‌ها و انتخاب‌گر تم و زبان", "پیشخوان",
  notes="نوار کناری با گروه‌بندیِ ۱۵گانه و شمارندهٔ آیتم؛ نوار بالا با جستجوی یکپارچه، زنگولهٔ اعلان، انتخاب‌گر تم و زبان و منوی کاربر.")

F("ChangePasswordWindow", "تغییر رمز عبور", "key-round", "form-narrow",
  "تغییر رمز عبورِ کاربرِ جاری", "پیشخوان",
  sections=[Sec("رمز عبور", "key-round", [
      Fd("CurrentPassword", "password", "رمز عبور فعلی", 12, True, "lock", "••••••••"),
      Fd("NewPassword", "password", "رمز عبور جدید", 12, True, "key-round", "••••••••",
         hint="حداقل ۸ نویسه، شامل حرف بزرگ و عدد"),
      Fd("ConfirmPassword", "password", "تکرار رمز عبور جدید", 12, True, "key-round", "••••••••"),
  ])],
  actions=[Act("Save", "تغییر رمز", "p", "check"), Act("Cancel", "انصراف", "g")],
  rel=[("تغییر رمز", "ورود", "پس از تغییر، نشست بسته می‌شود")])

F("SettingsView", "تنظیمات", "settings", "settings",
  "تنظیمات سراسری برنامه", "پیشخوان",
  sections=[Sec("عمومی", "sliders-horizontal", [
      Fd("ClinicName", "text", "نام درمانگاه", 8, True, "building-2", "درمانگاه تخصصی سهند"),
      Fd("ClinicPhone", "phone", "تلفن", 4, False, "phone", "۰۴۱-۳۴۴۳۶۱۴۱"),
      Fd("Address", "textarea", "نشانی", 12, False, "map-pin", "تبریز، خیابان ولیعصر، پلاک ۱۲۰"),
      Fd("AutoBackup", "check", "پشتیبان‌گیری خودکار هر شب", 6, False, None, True),
      Fd("DarkTheme", "check", "تم تاریک", 6, False, None, False),
  ]),
  Sec("مالی", "wallet", [
      Fd("Currency", "combo", "واحد پول", 4, True, "coins", "تومان", opts=["تومان", "ریال"]),
      Fd("TaxPercent", "number", "درصد مالیات", 4, False, "percent", "۹"),
      Fd("DefaultInsurance", "combo", "بیمه پیش‌فرض", 4, False, "shield-check", "آزاد", opts=["آزاد", "تأمین اجتماعی", "سلامت"]),
  ])],
  actions=[Act("Save", "ذخیره تنظیمات", "p", "save"), Act("Reset", "بازگردانی", "g", "undo-2")],
  rel=[("تنظیمات", "پشتیبان‌گیری", "تنظیمات بکاپ در این‌جا"),
       ("تنظیمات", "شعب", "نام شعبهٔ اصلی")])

F("ShortcutPickerWindow", "انتخاب میانبر", "layout-grid", "dialog",
  "انتخاب میانبرهای پیشخوان از میان صفحات", "پیشخوان",
  notes="کاشی‌های میانبر با گروه‌بندیِ دسته‌ای و انتخاب چندگانه؛ هر کاشی آیکون و نام صفحه را دارد.")

F("NotificationsView", "اعلان‌ها", "bell", "list-dense",
  "فهرست اعلان‌های سیستم", "پیشخوان",
  toolbar=[Tb("search", "جستجو", "w-lg", "عنوان یا متن…"),
           Tb("combo", "نوع", "w-md", "همه", opts=["همه", "بحرانی", "هشدار", "اطلاعات"]),
           Tb("check", "فقط خوانده‌نشده", None, True)],
  cols=[Col("Title", "عنوان", None, "r", "text", "message-square"),
        Col("Level", "سطح", "100px", "c", "chip"),
        Col("Date", "زمان", "130px", "r", "date", "clock"),
        Col("IsRead", "وضعیت", "90px", "c", "chip")],
  rows=[
      [chip("بحرانی", "dg"), "موجودی آموکسی‌سیلین به زیر حد سفارش رسید", dtx("امروز ۰۹:۱۲"), chip("خوانده‌نشده", "wn")],
      [chip("هشدار", "wn"), "۳ نوبت تأیید‌نشده برای فردا", dtx("امروز ۰۸:۴۰"), chip("خوانده‌نشده", "wn")],
      [chip("اطلاعات", "in"), "پشتیبان‌گیری شبانه با موفقیت انجام شد", dtx("دیروز ۲۳:۰۰"), chip("خوانده شده", "nu")],
      [chip("بحرانی", "dg"), "گواهی لایسنس ۷ روز دیگر منقضی می‌شود", dtx("دیروز ۱۸:۲۵"), chip("خوانده‌نشده", "wn")],
  ],
  actions=[Act("MarkAllRead", "خواندن همه", "s", "check"), Act("Delete", "حذف", "d", "trash-2")],
  rel=[("اعلان‌ها", "داروخانه", "هشدار کمبود موجودی"),
       ("اعلان‌ها", "نوبت‌دهی", "هشدار نوبت تأیید‌نشده"),
       ("اعلان‌ها", "وضعیت لایسنس", "هشدار انقضا")])

F("GuidedTourWindow", "راهنمای تعاملی", "compass", "dialog",
  "تور آموزشیِ برنامه", "پیشخوان",
  notes="حباب راهنما با ماسکِ سوراخ‌دار روی المان هدف؛ نوار پیشرفتِ مراحل و دکمه‌های «قبلی/بعدی/پایان».")

F("TrainingCenterView", "مرکز آموزش", "graduation-cap", "list-cards",
  "محتوای آموزشی و ویدئوهای راهنما", "پیشخوان",
  toolbar=[Tb("search", "جستجو", "w-lg", "عنوان دوره…"),
           Tb("combo", "دسته", "w-md", "همه", opts=["همه", "مقدماتی", "مالی", "پزشکی"])],
  actions=[Act("New", "دوره جدید", "p", "plus")],
  rel=[("مرکز آموزش", "راهنمای تعاملی", "مکمل تور")])

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  👥 گروه ۲ — بیماران                                                 ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("PatientsView", "بیماران", "users", "list-detail",
  "مدیریت پرونده‌ها و جستجوی بیماران", "بیماران",
  kpis=[Kpi("users", "کل بیماران", "۲,۸۴۱", "+۳٪", "up", "teal"),
        Kpi("user-plus", "جدید این ماه", "۱۲۷", "+۱۸٪", "up", "green"),
        Kpi("circle-check", "پرونده فعال", "۲,۶۰۳", None, "flat", "blue"),
        Kpi("archive", "بایگانی‌شده", "۲۳۸", "−۲٪", "down", "amber")],
  toolbar=[Tb("search", "جستجو", "w-xl", "نام، کد ملی یا موبایل…"),
           Tb("combo", "شعبه", "w-md", "همه شعب", opts=["همه شعب", "مرکزی", "شعبه ۲"]),
           Tb("combo", "وضعیت", "w-sm", "فعال", opts=["فعال", "غیرفعال", "بایگانی"]),
           Tb("combo", "جنسیت", "w-sm", "همه", opts=["همه", "زن", "مرد"]),
           Tb("date", "از تاریخ", "w-md", "۱۴۰۴/۰۱/۰۱")],
  cols=[Col("Code", "کد پرونده", "104px", "r", "text", "hash"),
        Col("FullName", "نام بیمار", None, "r", "who", "user"),
        Col("NationalId", "کد ملی", "118px", "l", "num"),
        Col("Phone", "موبایل", "124px", "l", "num"),
        Col("Age", "سن", "58px", "c", "num"),
        Col("Gender", "جنسیت", "74px", "c", "text"),
        Col("LastVisit", "آخرین ویزیت", "118px", "r", "date"),
        Col("Balance", "مانده", "110px", "l", "money"),
        Col("Status", "وضعیت", "96px", "c", "chip")],
  rows=[
      ["P-۱۰۲۴", who("سارا احمدی", "۴۲ سال · زن", "سا", "b3"), "۰۰۱-۲۳۴۵۶۷-۸", "۰۹۱۴-۴۲۲-۰۸۷۱", 42, "زن", dtx("۱۴۰۴/۰۷/۱۲"), num("۰"), chip("فعال", "ok")],
      ["P-۱۰۲۵", who("محمد رضایی", "۳۵ سال · مرد", "مر", ""), "۰۰۲-۳۴۵۶۷۸-۹", "۰۹۱۲-۳۳۵-۱۲۹۰", 35, "مرد", dtx("۱۴۰۴/۰۷/۱۰"), num("۱۸۰,۰۰۰"), chip("فعال", "ok")],
      ["P-۱۰۲۶", who("فاطمه کریمی", "۲۸ سال · زن", "فک", "b2"), "۰۰۳-۴۵۶۷۸۹-۰", "۰۹۳۸-۱۱۴-۲۳۴۵", 28, "زن", dtx("۱۴۰۴/۰۶/۲۸"), num("۰"), chip("فعال", "ok")],
      ["P-۱۰۲۷", who("علی حسینی", "۶۱ سال · مرد", "اح", ""), "۰۰۴-۵۶۷۸۹۰-۱", "۰۹۱۱-۷۷۸-۴۵۱۲", 61, "مرد", dtx("۱۴۰۴/۰۵/۱۴"), num("۰"), chip("بایگانی", "nu")],
      ["P-۱۰۲۸", who("زهرا موسوی", "۱۹ سال · زن", "زم", "b5"), "۰۰۵-۶۷۸۹۰۱-۲", "۰۹۳۵-۲۲۳-۹۰۱۴", 19, "زن", dtx("۱۴۰۴/۰۷/۱۸"), num("۴۵,۰۰۰"), chip("فعال", "ok")],
  ],
  actions=[Act("New", "بیمار جدید", "p", "user-plus"),
           Act("Export", "خروجی Excel", "s", "file-spreadsheet"),
           Act("Print", "چاپ کارت", "s", "printer")],
  aside=dict(title="خلاصه پرونده", icon="id-card",
             fields=[("کد پرونده", "P-۱۰۲۴"), ("نام", "سارا احمدی"), ("کد ملی", "۰۰۱-۲۳۴۵۶۷-۸"),
                     ("سن/جنسیت", "۴۲ سال · زن"), ("بیمه", "تأمین اجتماعی"), ("موبایل", "۰۹۱۴-۴۲۲-۰۸۷۱"),
                     ("آخرین ویزیت", "۱۴۰۴/۰۷/۱۲"), ("مانده حساب", "۰ تومان")]),
  rel=[("بیماران", "پرونده‌ی بیمار", "هر ردیف به پرونده می‌رود"),
       ("بیماران", "پذیرش و صف", "بیمار پذیرش می‌شود"),
       ("بیماران", "نوبت‌دهی", "نوبت برای بیمار ثبت می‌شود"),
       ("بیماران", "صورتحساب", "مانده حساب از فاکتورها")])

F("PatientFormWindow", "فرم بیمار", "user-plus", "form-std",
  "ایجاد یا ویرایش پرونده بیمار", "بیماران",
  sections=[
      Sec("اطلاعات هویتی", "id-card", [
          Fd("FirstName", "text", "نام", 3, True, "user", "سارا"),
          Fd("LastName", "text", "نام خانوادگی", 3, True, "user", "احمدی"),
          Fd("NationalId", "national", "کد ملی", 3, True, "fingerprint", "۰۰۱۲۳۴۵۶۷۸"),
          Fd("Gender", "combo", "جنسیت", 3, True, "user-round", "زن", opts=["زن", "مرد"]),
          Fd("BirthDate", "date", "تاریخ تولد", 3, False, "cake", "۱۴۰۴/۰۷/۱۹".replace("۱۴۰۴", "۱۳۶۲")),
          Fd("MaritalStatus", "combo", "وضعیت تأهل", 3, False, "heart", "متأهل", opts=["مجرد", "متأهل"]),
          Fd("Occupation", "text", "شغل", 3, False, "briefcase", "کارمند"),
          Fd("Education", "combo", "تحصیلات", 3, False, "graduation-cap", "کارشناسی",
             opts=["زیردیپلم", "دیپلم", "کارشناسی", "ارشد", "دکتری"]),
      ]),
      Sec("اطلاعات تماس", "phone", [
          Fd("Phone", "phone", "موبایل", 3, True, "smartphone", "۰۹۱۴۴۲۲۰۸۷۱"),
          Fd("HomePhone", "phone", "تلفن ثابت", 3, False, "phone", "۰۴۱۳۳۳۳۲۲۱۱"),
          Fd("Email", "text", "رایانامه", 3, False, "mail", "s.ahmadi@example.com"),
          Fd("EmergencyContact", "phone", "شماره اضطراری", 3, False, "phone-call", "۰۹۱۲۱۱۱۲۲۳۳"),
          Fd("Province", "combo", "استان", 3, False, "map", "آذربایجان شرقی", opts=["آذربایجان شرقی", "تهران", "اصفهان"]),
          Fd("City", "combo", "شهر", 3, False, "building-2", "تبریز", opts=["تبریز", "تهران", "اصفهان"]),
          Fd("Address", "textarea", "نشانی کامل", 12, False, "map-pin", "تبریز، خیابان ولیعصر، کوچه گلستان، پلاک ۱۲"),
      ]),
      Sec("اطلاعات بیمه و بالینی", "shield-check", [
          Fd("InsuranceId", "combo", "بیمه پایه", 4, False, "shield", "تأمین اجتماعی",
             opts=["آزاد", "تأمین اجتماعی", "سلامت", "نیروهای مسلح"]),
          Fd("InsuranceNumber", "text", "شماره بیمه", 4, False, "hash", "۱۲۳۴۵۶۷۸۹۰"),
          Fd("InsuranceExpiry", "date", "تاریخ اعتبار", 4, False, "calendar-clock", "۱۴۰۵/۰۳/۳۱"),
          Fd("BloodType", "combo", "گروه خونی", 3, False, "droplet", "O+", opts=["O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-"]),
          Fd("Weight", "number", "وزن (کیلوگرم)", 3, False, "weight", "۶۵"),
          Fd("Height", "number", "قد (سانتی‌متر)", 3, False, "ruler", "۱۶۵"),
          Fd("ChronicDiseases", "combo", "بیماری‌های مزمن", 3, False, "activity", "دیابت", opts=["ندارد", "دیابت", "فشار خون", "قلبی"]),
          Fd("Allergies", "textarea", "حساسیت‌های دارویی", 12, False, "triangle-alert", "پنی‌سیلین — واکنش پوستی شدید",
             hint="بررسی تداخل دارویی در نسخه بر این اساس انجام می‌شود"),
      ]),
  ],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("SaveNew", "ذخیره و جدید", "s", "plus"),
           Act("Cancel", "انصراف", "g")],
  rel=[("فرم بیمار", "بیماران", "پس از ذخیره به فهرست بازمی‌گردد"),
       ("فرم بیمار", "فرم پذیرش", "بیمه از پرونده می‌آید"),
       ("فرم بیمار", "ویزیت (SOAP)", "حساسیت‌ها در ویزیت نمایش داده می‌شود")],
  notes="فیلدها بر اساس نقشِ خود اندازه گرفته‌اند: نام/نام‌خانوادگی/کدملی/جنسیت هر یک ۳ ستون از ۱۲؛ نشانی و حساسیت‌ها عرض کامل.")

F("PatientDetailWindow", "پرونده‌ی بیمار", "id-card", "detail-timeline",
  "پرونده کامل بیمار با خط زمان درمان", "بیماران",
  hero=dict(name="سارا احمدی", sub="کد پرونده P-۱۰۲۴ · ۴۲ سال · زن", av="سا", tone="b3",
            chips=[("تأمین اجتماعی", "cy"), ("فعال", "ok"), ("O+", "pp")],
            stats=[("ویزیت‌ها", "۱۸"), ("نوبت‌ها", "۲۴"), ("پرداختی کل", "۱۲,۴۵۰,۰۰۰")],
            fields=[("کد ملی", "۰۰۱-۲۳۴۵۶۷-۸"), ("موبایل", "۰۹۱۴-۴۲۲-۰۸۷۱"),
                    ("تاریخ تولد", "۱۳۶۲/۰۴/۱۵"), ("نشانی", "تبریز، ولیعصر، پلاک ۱۲")]),
  tabs=[("خط زمان درمان", "history", 8), ("ویزیت‌ها", "stethoscope", 18),
        ("نوبت‌ها", "calendar-days", 24), ("نسخه‌ها", "file-text", 11),
        ("آزمایش‌ها", "flask-conical", 6), ("تصویربرداری", "scan-line", 2),
        ("صورتحساب‌ها", "receipt", 9), ("پیوست‌ها", "paperclip", 3)],
  actions=[Act("NewVisit", "ویزیت جدید", "p", "stethoscope"),
           Act("NewAppointment", "نوبت جدید", "s", "calendar-plus"),
           Act("Print", "چاپ پرونده", "s", "printer")],
  rel=[("پرونده بیمار", "ویزیت (SOAP)", "هر ویزیت در خط زمان"),
       ("پرونده بیمار", "صورتحساب", "مانده از فاکتورها"),
       ("پرونده بیمار", "آزمایشگاه", "نتایج آزمایش")],
  notes="سربرگ با کارتِ هویت، آمار و اطلاعات تماس؛ سپس تب‌ها؛ تب نخست «خط زمان درمان» رویدادها را به ترتیب زمان نشان می‌دهد.")

F("AdvancedEmrView", "پرونده بالینی پیشرفته", "notebook-pen", "detail-tabs",
  "ثبت شرح‌حال، معاینه، تشخیص و پیگیری", "بیماران",
  tabs=[("شرح‌حال", "clipboard-list", None), ("معاینه فیزیکی", "stethoscope", None),
        ("تشخیص‌ها", "circle-check", None), ("داروهای جاری", "pill", None),
        ("یادداشت‌ها", "notebook-pen", None), ("پیگیری‌ها", "calendar-clock", None)],
  sections=[Sec("شرح‌حالِ هدف‌دار", "clipboard-list", [
      Fd("ChiefComplaint", "text", "شکایت اصلی", 8, True, "message-square", "درد شکمی از ۳ روز پیش"),
      Fd("Duration", "combo", "مدت زمان", 4, False, "timer", "۳ روز", opts=["امروز", "۲ روز", "۳ روز", "یک هفته", "بیش از یک ماه"]),
      Fd("HistoryPresent", "textarea", "شرح بیماری فعلی", 8, False, "file-text", "درد مبهم در ناحیه اپی‌گاستر که پس از غذا تشدید می‌شود…"),
      Fd("PastMedical", "textarea", "سابقه بیماری‌ها", 4, False, "archive", "دیابت نوع ۲ از ۵ سال پیش"),
      Fd("DrugHistory", "textarea", "سابقه دارویی", 4, False, "pill", "متفورمین ۵۰۰"),
      Fd("FamilyHistory", "textarea", "سابقه خانوادگی", 4, False, "users", "پدر: فشار خون"),
      Fd("SocialHistory", "textarea", "سابقه اجتماعی", 4, False, "activity", "غیرسیگاری"),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("SaveDraft", "پیش‌نویس", "s", "file")],
  rel=[("پرونده بالینی", "ویزیت (SOAP)", "بخشی از ویزیت"),
       ("پرونده بالینی", "پرونده‌ی بیمار", "زیرمجموعه پرونده")])

F("PhotoViewerWindow", "نمایش تصویر", "image", "viewer",
  "نمایشگر تمام‌صفحهٔ تصویر", "بیماران",
  notes="ریل ابزارِ عمودی (بزرگ‌نمایی، چرخش، آینه، تنظیم نور، برش، ذخیره) و پس‌زمینه تیره برای دید بهتر.")

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  📅 گروه ۳ — نوبت‌دهی                                                ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("AppointmentsView", "نوبت‌دهی", "calendar-days", "kanban",
  "تقویم و صف نوبت‌ها در ستون‌های وضعیت", "نوبت‌دهی",
  kpis=[Kpi("calendar-check", "نوبت‌های امروز", "۳۶", "+۸٪", "up", "teal"),
        Kpi("circle-check", "تأیید شده", "۲۴", None, "flat", "green"),
        Kpi("clock", "در انتظار", "۸", None, "flat", "amber"),
        Kpi("circle-x", "لغو شده", "۴", "+۱", "down", "rose")],
  toolbar=[Tb("date", "تاریخ", "w-md", "امروز ۱۴۰۴/۰۷/۱۹"),
           Tb("combo", "پزشک", "w-lg", "همه پزشکان", opts=["همه پزشکان", "دکتر موسوی", "دکتر رضایی"]),
           Tb("search", "جستجو", "w-lg", "نام بیمار…"),
           Tb("seg", "نما", None, "کانبان", opts=["کانبان", "تقویم", "جدول"])],
  actions=[Act("New", "نوبت جدید", "p", "calendar-plus"),
           Act("Confirm", "تأیید", "ok", "check"),
           Act("Refresh", "تازه‌سازی", "g", "refresh-cw")],
  rel=[("نوبت‌دهی", "بیماران", "نوبت برای بیمار"),
       ("نوبت‌دهی", "پذیرش و صف", "تأیید نوبت → پذیرش"),
       ("نوبت‌دهی", "پزشکان", "ظرفیت برنامه هفتگی")],
  notes="چهار ستون کانبان: در انتظار · تأیید شده · حاضر شده · لغو شده. هر کارت شامل نام بیمار، ساعت، پزشک، نوع ویزیت و نشان وضعیت.")

F("AppointmentFormWindow", "فرم نوبت", "calendar-plus", "form-std",
  "ثبت نوبت جدید", "نوبت‌دهی",
  sections=[Sec("زمان و پزشک", "calendar-clock", [
      Fd("PatientId", "lookup", "بیمار", 6, True, "user", "سارا احمدی — P-۱۰۲۴"),
      Fd("DoctorId", "combo", "پزشک", 3, True, "stethoscope", "دکتر موسوی",
         opts=["دکتر موسوی", "دکتر رضایی", "دکتر کریمی"]),
      Fd("Date", "date", "تاریخ نوبت", 3, True, "calendar-days", "۱۴۰۴/۰۷/۲۰"),
      Fd("Time", "time", "ساعت", 3, True, "clock", "۱۰:۳۰"),
      Fd("Duration", "combo", "مدت", 3, False, "timer", "۲۰ دقیقه", opts=["۱۵ دقیقه", "۲۰ دقیقه", "۳۰ دقیقه"]),
      Fd("VisitType", "combo", "نوع ویزیت", 3, False, "stethoscope", "ویزیت عمومی",
         opts=["ویزیت عمومی", "تخصصی", "پیگیری", "معاینه دوره‌ای"]),
      Fd("RoomId", "combo", "اتاق", 3, False, "door-open", "اتاق ۲", opts=["اتاق ۱", "اتاق ۲", "اتاق ۳"]),
  ]),
  Sec("توضیحات", "message-square", [
      Fd("Reason", "text", "علت مراجعه", 12, False, "clipboard-list", "درد معده"),
      Fd("Notes", "textarea", "یادداشت", 12, False, "notebook-pen", ""),
      Fd("SendSms", "check", "ارسال پیامک یادآوری", 6, False, None, True),
      Fd("IsUrgent", "check", "نوبت فوری", 6, False, None, False),
  ])],
  actions=[Act("Save", "ثبت نوبت", "p", "check"), Act("Cancel", "انصراف", "g")],
  rel=[("فرم نوبت", "اتاق‌ها", "اتاق از فهرست اتاق‌ها"),
       ("فرم نوبت", "پیامک", "یادآوری پیامکی")])

F("DoctorVisitView", "ویزیت پزشک", "stethoscope", "split",
  "میزکار پزشک: صف بیماران + فرم ویزیت", "نوبت‌دهی",
  kpis=[Kpi("users", "در صف", "۷", None, "flat", "teal"),
        Kpi("circle-check", "ویزیت‌شده امروز", "۱۹", "+۱۲٪", "up", "green"),
        Kpi("timer", "میانگین ویزیت", "۱۶ دقیقه", "−۴٪", "down", "blue")],
  actions=[Act("StartVisit", "شروع ویزیت", "p", "play"),
           Act("CallNext", "فراخوانی بعدی", "s", "chevron-left"),
           Act("Complete", "پایان ویزیت", "ok", "check")],
  rel=[("ویزیت پزشک", "پذیرش و صف", "بیمار از صف می‌آید"),
       ("ویزیت پزشک", "ویزیت (SOAP)", "فرم ویزیت باز می‌شود"),
       ("ویزیت پزشک", "فرم نسخه", "پس از ویزیت")])


# ╔═════════════════════════════════════════════════════════════════════╗
# ║  🏥 گروه ۴ — پذیرش                                                   ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("ReceptionsView", "پذیرش و صف", "clipboard-list", "list-detail",
  "صف زندهٔ بیمارانِ پذیرش‌شده", "پذیرش",
  kpis=[Kpi("users", "در صف", "۱۲", None, "flat", "teal"),
        Kpi("clock", "میانگین انتظار", "۱۴ دقیقه", "−۶٪", "down", "blue"),
        Kpi("circle-check", "پذیرش امروز", "۴۸", "+۱۲٪", "up", "green"),
        Kpi("hand-coins", "دریافتی امروز", "۱۴,۲۰۰,۰۰۰", "+۱۹٪", "up", "amber")],
  toolbar=[Tb("search", "جستجو", "w-xl", "نام یا کد پرونده…"),
           Tb("combo", "پزشک", "w-md", "همه پزشکان"),
           Tb("combo", "وضعیت", "w-md", "همه", opts=["همه", "در انتظار", "در حال ویزیت", "اتمام", "لغو"]),
           Tb("date", "تاریخ", "w-md", "امروز")],
  cols=[Col("QueueNo", "نوبت", "64px", "c", "num"),
        Col("Patient", "بیمار", None, "r", "who", "user"),
        Col("Doctor", "پزشک", "150px", "r", "text", "stethoscope"),
        Col("Service", "خدمت", "150px", "r", "text"),
        Col("Insurance", "بیمه", "120px", "r", "text", "shield"),
        Col("TimeIn", "ورود", "86px", "c", "date"),
        Col("Wait", "انتظار", "86px", "c", "num"),
        Col("Amount", "مبلغ", "112px", "l", "money"),
        Col("Status", "وضعیت", "104px", "c", "chip")],
  rows=[
      ["۱۲", who("سارا احمدی", "P-۱۰۲۴", "سا", "b3"), "دکتر موسوی", "ویزیت عمومی", "تأمین اجتماعی", dtx("۰۹:۱۲"), num("۱۸ دقیقه"), num("۲۵۰,۰۰۰"), chip("در انتظار", "wn")],
      ["۱۳", who("محمد رضایی", "P-۱۰۲۵", "مر", ""), "دکتر رضایی", "ویزیت تخصصی", "سلامت", dtx("۰۹:۲۵"), num("۵ دقیقه"), num("۴۰۰,۰۰۰"), chip("در حال ویزیت", "in")],
      ["۱۴", who("فاطمه کریمی", "P-۱۰۲۶", "فک", "b2"), "دکتر موسوی", "پیگیری", "آزاد", dtx("۰۹:۳۱"), num("۲ دقیقه"), num("۱۵۰,۰۰۰"), chip("در انتظار", "wn")],
      ["۱۱", who("علی حسینی", "P-۱۰۲۷", "اح", ""), "دکتر کریمی", "ویزیت عمومی", "تأمین اجتماعی", dtx("۰۸:۵۰"), num("۰"), num("۲۵۰,۰۰۰"), chip("اتمام", "ok")],
      ["۱۰", who("زهرا موسوی", "P-۱۰۲۸", "زم", "b5"), "دکتر رضایی", "معاینه دوره‌ای", "آزاد", dtx("۰۸:۳۵"), num("۰"), num("۰"), chip("لغو", "dg")],
  ],
  actions=[Act("New", "پذیرش جدید", "p", "user-plus"),
           Act("Pay", "دریافت وجه", "ok", "hand-coins"),
           Act("Call", "فراخوانی", "s", "megaphone"),
           Act("Cancel", "لغو", "d", "x")],
  aside=dict(title="خلاصه پذیرش", icon="clipboard-check",
             fields=[("شماره در صف", "۱۲"), ("بیمار", "سارا احمدی"),
                     ("پزشک", "دکتر موسوی"), ("خدمت", "ویزیت عمومی"),
                     ("بیمه", "تأمین اجتماعی"), ("سهم بیمه", "۱۷۵,۰۰۰"),
                     ("سهم بیمار", "۷۵,۰۰۰"), ("زمان انتظار", "۱۸ دقیقه")]),
  rel=[("پذیرش و صف", "بیماران", "بیمار انتخاب می‌شود"),
       ("پذیرش و صف", "نوبت‌دهی", "از نوبت تأییدشده"),
       ("پذیرش و صف", "ویزیت (SOAP)", "پس از فراخوانی"),
       ("پذیرش و صف", "صورتحساب", "فاکتور صادر می‌شود"),
       ("پذیرش و صف", "شیفت‌ها", "تحویل در پایان شیفت")])

F("ReceptionFormWindow", "فرم پذیرش", "clipboard-plus", "form-std",
  "ثبت پذیرش جدید", "پذیرش",
  sections=[Sec("بیمار و خدمت", "user-check", [
      Fd("PatientId", "lookup", "بیمار", 6, True, "user", "سارا احمدی — P-۱۰۲۴"),
      Fd("DoctorId", "combo", "پزشک", 3, True, "stethoscope", "دکتر موسوی"),
      Fd("ServiceId", "combo", "خدمت", 3, True, "concierge-bell", "ویزیت عمومی"),
      Fd("InsuranceId", "combo", "بیمه", 4, False, "shield", "تأمین اجتماعی"),
      Fd("VisitType", "combo", "نوع مراجعه", 4, False, "stethoscope", "نوبت‌دار"),
      Fd("RoomId", "combo", "اتاق", 4, False, "door-open", "اتاق ۲"),
  ]),
  Sec("مالی", "wallet", [
      Fd("BasePrice", "money", "تعرفه پایه", 3, True, "banknote", "۲۵۰,۰۰۰"),
      Fd("InsuranceShare", "money", "سهم بیمه", 3, False, "shield-check", "۱۷۵,۰۰۰"),
      Fd("PatientShare", "money", "سهم بیمار", 3, True, "hand-coins", "۷۵,۰۰۰"),
      Fd("Discount", "money", "تخفیف", 3, False, "tag", "۰"),
      Fd("Notes", "textarea", "توضیحات", 12, False, "notebook-pen", ""),
  ])],
  actions=[Act("Save", "ثبت پذیرش", "p", "check"), Act("SavePay", "ثبت و دریافت وجه", "ok", "hand-coins"),
           Act("Cancel", "انصراف", "g")],
  rel=[("فرم پذیرش", "تعرفه خدمات", "قیمت از تعرفه"),
       ("فرم پذیرش", "بیمه و مطالبات", "درصد از قرارداد بیمه")])

F("ReceptionPayWindow", "دریافت وجه", "hand-coins", "form-narrow",
  "تسویه حساب پذیرش", "پذیرش",
  sections=[Sec("مبالغ", "wallet", [
      Fd("Total", "money", "جمع کل", 6, False, "banknote", "۲۵۰,۰۰۰"),
      Fd("Insurance", "money", "سهم بیمه", 6, False, "shield-check", "۱۷۵,۰۰۰"),
      Fd("PatientPay", "money", "قابل پرداخت", 6, True, "hand-coins", "۷۵,۰۰۰"),
      Fd("Discount", "money", "تخفیف", 6, False, "tag", "۰"),
      Fd("PayMethod", "combo", "شیوه پرداخت", 12, True, "credit-card", "نقد",
         opts=["نقد", "کارت‌خوان", "کارت به کارت", "اعتباری"]),
      Fd("Ref", "text", "شماره پیگیری", 12, False, "hash", ""),
  ])],
  actions=[Act("Pay", "ثبت پرداخت", "p", "check"), Act("Print", "چاپ رسید", "s", "printer"),
           Act("Cancel", "انصراف", "g")],
  rel=[("دریافت وجه", "صورتحساب", "فاکتور صادر می‌شود"),
       ("دریافت وجه", "بستن شیفت", "در پایان شیفت جمع‌بندی می‌شود")])

F("PaymentWindow", "پرداخت", "credit-card", "form-narrow",
  "ثبت پرداخت برای بیمار", "پذیرش",
  sections=[Sec("پرداخت", "credit-card", [
      Fd("PatientId", "lookup", "بیمار", 12, True, "user", "سارا احمدی — P-۱۰۲۴"),
      Fd("Amount", "money", "مبلغ", 6, True, "banknote", "۱۵۰,۰۰۰"),
      Fd("PayMethod", "combo", "شیوه پرداخت", 6, True, "credit-card", "کارت‌خوان",
         opts=["نقد", "کارت‌خوان", "کارت به کارت"]),
      Fd("Ref", "text", "شماره پیگیری", 12, False, "hash", "۱۲۳۴۵۶"),
      Fd("Notes", "textarea", "توضیحات", 12, False, "notebook-pen", ""),
  ])],
  actions=[Act("Save", "ثبت", "p", "check"), Act("Cancel", "انصراف", "g")])

F("ShiftsView", "شیفت‌ها", "clock", "list-dense",
  "تعریف و مدیریت شیفت‌های کاری", "پذیرش",
  toolbar=[Tb("combo", "وضعیت", "w-md", "همه", opts=["همه", "فعال", "بسته", "لغو"]),
           Tb("date", "از تاریخ", "w-md", "۱۴۰۴/۰۷/۰۱"),
           Tb("combo", "کاربر", "w-md", "همه")],
  cols=[Col("Code", "کد", "84px", "r", "text", "hash"),
        Col("User", "مسئول", "170px", "r", "who", "user"),
        Col("Start", "شروع", "148px", "r", "date", "clock"),
        Col("End", "پایان", "148px", "r", "date", "clock"),
        Col("Receptions", "پذیرش‌ها", "94px", "c", "num"),
        Col("Total", "مبلغ کل", "124px", "l", "money"),
        Col("Status", "وضعیت", "96px", "c", "chip")],
  rows=[
      ["SH-۲۱۴", who("مریم نوری", "پذیرش", "من", "b2"), dtx("۱۴۰۴/۰۷/۱۹ ۰۸:۰۰"), dtx("۱۴۰۴/۰۷/۱۹ ۱۴:۰۰"), num("۲۴"), num("۶,۲۰۰,۰۰۰"), chip("فعال", "ok")],
      ["SH-۲۱۳", who("احمد صادقی", "پذیرش", "اص", ""), dtx("۱۴۰۴/۰۷/۱۸ ۱۴:۰۰"), dtx("۱۴۰۴/۰۷/۱۸ ۲۰:۰۰"), num("۱۹"), num("۴,۸۵۰,۰۰۰"), chip("بسته", "nu")],
      ["SH-۲۱۲", who("مریم نوری", "پذیرش", "من", "b2"), dtx("۱۴۰۴/۰۷/۱۸ ۰۸:۰۰"), dtx("۱۴۰۴/۰۷/۱۸ ۱۴:۰۰"), num("۲۱"), num("۵,۴۰۰,۰۰۰"), chip("بسته", "nu")],
  ],
  actions=[Act("New", "شیفت جدید", "p", "plus"), Act("Close", "بستن شیفت", "ok", "lock"),
           Act("Detail", "جزئیات", "s", "eye")],
  rel=[("شیفت‌ها", "بستن شیفت", "جمع‌بندی مالی"),
       ("شیفت‌ها", "پذیرش و صف", "پذیرش‌های دوره شیفت")])

F("ShiftCloseWindow", "بستن شیفت", "lock", "form-std",
  "جمع‌بندی مالی شیفت و بستن آن", "پذیرش",
  kpis=[Kpi("hand-coins", "دریافتی نقد", "۳,۲۰۰,۰۰۰", None, "flat", "green"),
        Kpi("credit-card", "کارت‌خوان", "۲,۴۰۰,۰۰۰", None, "flat", "blue"),
        Kpi("shield-check", "سهم بیمه", "۶۰۰,۰۰۰", None, "flat", "teal"),
        Kpi("banknote", "جمع کل", "۶,۲۰۰,۰۰۰", None, "flat", "amber")],
  sections=[Sec("مغایرت‌ها", "triangle-alert", [
      Fd("CashCounted", "money", "موجودی شمارش‌شده", 4, True, "banknote", "۳,۱۵۰,۰۰۰"),
      Fd("CashExpected", "money", "موجودی مورد انتظار", 4, False, "calculator", "۳,۲۰۰,۰۰۰"),
      Fd("Difference", "money", "مغایرت", 4, False, "triangle-alert", "−۵۰,۰۰۰"),
      Fd("Reason", "textarea", "علت مغایرت", 12, False, "notebook-pen", "پرداخت خردِ ثبت‌نشده در پایان شیفت"),
  ])],
  actions=[Act("Close", "بستن شیفت", "p", "lock"), Act("Print", "چاپ رسید", "s", "printer"),
           Act("Cancel", "انصراف", "g")],
  rel=[("بستن شیفت", "پیش‌نمایش رسید شیفت", "رسید چاپ می‌شود"),
       ("بستن شیفت", "حسابداری", "سند حسابداری صادر می‌شود")])

F("ShiftReceiptPreviewWindow", "پیش‌نمایش رسید شیفت", "receipt", "dialog",
  "پیش‌نمایش رسید جمع‌بندی شیفت", "پذیرش",
  notes="کاغذِ رسید با هدر درمانگاه، جدول روش‌های پرداخت، مغایرت و امضای تحویل‌گیرنده.")

F("RoomsView", "اتاق‌ها", "door-open", "list-cards",
  "اتاق‌های معاینه و وضعیت اشغال", "پذیرش",
  toolbar=[Tb("combo", "شعبه", "w-md", "همه"), Tb("combo", "وضعیت", "w-sm", "همه", opts=["همه", "آزاد", "اشغال"])],
  actions=[Act("Manage", "مدیریت اتاق‌ها", "p", "settings"), Act("New", "اتاق جدید", "s", "plus")],
  rel=[("اتاق‌ها", "مدیریت اتاق‌ها", "ویرایش گروهی"),
       ("اتاق‌ها", "نوبت‌دهی", "تخصیص اتاق به نوبت")])

F("RoomsManagerWindow", "مدیریت اتاق‌ها", "door-closed", "dialog",
  "افزودن، ویرایش و حذف اتاق‌ها", "پذیرش",
  notes="فهرست کشوییِ اتاق‌ها با افزودن/حذف و ویرایشِ نام و شمارهٔ درجا.")

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  🩺 گروه ۵ — ویزیت                                                   ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("VisitWindow", "ویزیت (SOAP)", "stethoscope", "form-std",
  "ثبت ویزیت با ساختار SOAP و تشخیص ICD-10", "ویزیت",
  sections=[Sec("علائم حیاتی", "activity", [
      Fd("BP", "text", "فشار خون", 2, False, "heart-pulse", "۱۲۰/۸۰"),
      Fd("Pulse", "number", "نبض", 2, False, "activity", "۷۸"),
      Fd("Temp", "number", "دما", 2, False, "thermometer", "۳۶٫۷"),
      Fd("RR", "number", "تنفس", 2, False, "wind", "۱۶"),
      Fd("O2Sat", "number", "اشباع O₂", 2, False, "droplet", "۹۸"),
      Fd("Weight", "number", "وزن", 2, False, "weight", "۶۵"),
  ]),
  Sec("S — شرح‌حال (Subjective)", "message-square", [
      Fd("Subjective", "textarea", "شرح‌حال از زبان بیمار", 12, True, "message-square",
         "درد مبهم شکم از سه روز پیش که پس از صرف غذا تشدید می‌شود؛ تهوع بدون استفراغ…"),
  ]),
  Sec("O — یافته‌ها (Objective)", "search", [
      Fd("Objective", "textarea", "یافته‌های معاینه", 12, False, "search",
         "حساسیت خفیف در اپی‌گاستر، بدون دفاع عضلانی؛ صداهای روده‌ای طبیعی…"),
  ]),
  Sec("A — ارزیابی (Assessment)", "circle-check", [
      Fd("IcdCode", "lookup", "کد ICD-10", 6, True, "book-marked", "K21.9 — رفلاکس معده"),
      Fd("Assessment", "textarea", "تشخیص", 6, False, "circle-check", "رفلاکس معدی‌مروی بدون ازوفاژیت"),
  ]),
  Sec("P — برنامه (Plan)", "clipboard-check", [
      Fd("Plan", "textarea", "برنامه درمان", 8, False, "clipboard-check", "امپرازول ۲۰ میلی‌گرم هر شب…"),
      Fd("FollowUp", "combo", "پیگیری", 4, False, "calendar-clock", "۲ هفته دیگر"),
      Fd("Referral", "combo", "ارجاع", 6, False, "external-link", "ندارد"),
      Fd("RestDays", "number", "روز استراحت", 6, False, "calendar-days", "۰"),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("SaveVisit", "تکمیل ویزیت", "ok", "check"),
           Act("Prescribe", "نسخه", "s", "file-text"), Act("Cancel", "انصراف", "g")],
  rel=[("ویزیت", "فرم نسخه", "نسخه بر اساس تشخیص"),
       ("ویزیت", "درخواست آزمایش", "درخواست پاراکلینیک"),
       ("ویزیت", "پذیرش و صف", "اتمام ویزیت وضعیت پذیرش را عوض می‌کند")],
  notes="فیلدهای علائم حیاتی در یک ردیفِ شش‌تاییِ کم‌عرض — چون مقدارهای کوتاه عددی هستند؛ بخش‌های متنی عرض کامل.")

F("VisitsView", "فهرست ویزیت‌ها", "clipboard-check", "list-dense",
  "گزارش ویزیت‌های انجام‌شده", "ویزیت",
  toolbar=[Tb("search", "جستجو", "w-xl", "نام بیمار یا کد…"),
           Tb("combo", "پزشک", "w-md", "همه پزشکان"),
           Tb("date", "از تاریخ", "w-md", "۱۴۰۴/۰۷/۰۱"), Tb("date", "تا تاریخ", "w-md", "۱۴۰۴/۰۷/۱۹")],
  cols=[Col("Code", "کد", "96px", "r", "text", "hash"),
        Col("Patient", "بیمار", None, "r", "who", "user"),
        Col("Doctor", "پزشک", "148px", "r", "text", "stethoscope"),
        Col("Date", "تاریخ", "112px", "r", "date", "calendar-days"),
        Col("Icd", "ICD-10", "180px", "r", "text", "book-marked"),
        Col("Amount", "مبلغ", "108px", "l", "money"),
        Col("Status", "وضعیت", "96px", "c", "chip")],
  rows=[
      ["V-۴۱۲۰", who("سارا احمدی", "P-۱۰۲۴", "سا", "b3"), "دکتر موسوی", dtx("۱۴۰۴/۰۷/۱۲"), "K21.9", num("۲۵۰,۰۰۰"), chip("تکمیل", "ok")],
      ["V-۴۱۲۱", who("محمد رضایی", "P-۱۰۲۵", "مر", ""), "دکتر رضایی", dtx("۱۴۰۴/۰۷/۱۲"), "J06.9", num("۴۰۰,۰۰۰"), chip("تکمیل", "ok")],
      ["V-۴۱۲۲", who("فاطمه کریمی", "P-۱۰۲۶", "فک", "b2"), "دکتر موسوی", dtx("۱۴۰۴/۰۷/۱۱"), "Z00.0", num("۱۵۰,۰۰۰"), chip("تکمیل", "ok")],
      ["V-۴۱۲۳", who("علی حسینی", "P-۱۰۲۷", "اح", ""), "دکتر کریمی", dtx("۱۴۰۴/۰۷/۱۰"), "I10", num("۲۵۰,۰۰۰"), chip("پیش‌نویس", "wn")],
  ],
  actions=[Act("Export", "خروجی Excel", "s", "file-spreadsheet"), Act("Print", "چاپ", "s", "printer")],
  rel=[("فهرست ویزیت‌ها", "ویزیت (SOAP)", "هر ردیف ویزیت کامل"),
       ("فهرست ویزیت‌ها", "گزارش کارمزد", "مبنای کارمزد پزشک")])

F("DentistryView", "دندانپزشکی", "smile", "detail-tabs",
  "چارت دندانی و ثبت خدمات دندانپزشکی", "ویزیت",
  tabs=[("چارت دندان", "grid-3x3", None), ("خدمات", "briefcase", None),
        ("تصاویر", "image", None), ("طرح درمان", "clipboard-list", None)],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Print", "چاپ چارت", "s", "printer")],
  rel=[("دندانپزشکی", "ویزیت (SOAP)", "بخشی از ویزیت تخصصی"),
       ("دندانپزشکی", "صورتحساب", "خدمات فاکتور می‌شود")],
  notes="چارت دندانی با ۳۲ دندانِ تعاملی؛ هر دندان رنگِ وضعیت خود را دارد (سالم/پوسیده/پرکرده/کشیده‌شده).")

F("OphthalmologyView", "چشم‌پزشکی", "eye", "detail-tabs",
  "معاینه بینایی و ثبت نمره چشم", "ویزیت",
  tabs=[("حدت بینایی", "eye", None), ("فشار چشم", "gauge", None),
        ("فوندوس", "scan-eye", None), ("نسخه عینک", "glasses", None)],
  sections=[Sec("حدت بینایی", "eye", [
      Fd("RightSph", "number", "کروی راست", 2, False, "eye", "−۱٫۵۰"),
      Fd("RightCyl", "number", "استیگمات راست", 2, False, "eye", "−۰٫۵۰"),
      Fd("RightAxis", "number", "محور راست", 2, False, "eye", "۱۸۰"),
      Fd("LeftSph", "number", "کروی چپ", 2, False, "eye", "−۱٫۲۵"),
      Fd("LeftCyl", "number", "استیگمات چپ", 2, False, "eye", "−۰٫۷۵"),
      Fd("LeftAxis", "number", "محور چپ", 2, False, "eye", "۱۷۰"),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Print", "چاپ نسخه عینک", "s", "printer")],
  rel=[("چشم‌پزشکی", "ویزیت (SOAP)", "ویزیت تخصصی")])

F("PhysiotherapyView", "فیزیوتراپی", "accessibility", "detail-tabs",
  "برنامه تمرینی و پیگیری جلسات فیزیوتراپی", "ویزیت",
  tabs=[("جلسات", "calendar-days", None), ("ارزیابی درد", "activity", None),
        ("برنامه تمرینی", "list-checks", None)],
  sections=[Sec("ارزیابی درد", "activity", [
      Fd("PainLevel", "number", "نمره درد (۰ تا ۱۰)", 4, False, "activity", "۴"),
      Fd("PainLocation", "combo", "محل درد", 4, False, "map-pin", "کمر", opts=["گردن", "کمر", "زانو", "شانه"]),
      Fd("Sessions", "number", "تعداد جلسات", 4, False, "repeat", "۱۲"),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("NewSession", "جلسه جدید", "s", "plus")],
  rel=[("فیزیوتراپی", "ویزیت (SOAP)", "ویزیت تخصصی")])

F("ComplementaryView", "خدمات تکمیلی", "sparkles", "list-dense",
  "خدمات پاراکلینیکِ تکمیلی و ثبت نتایج", "ویزیت",
  toolbar=[Tb("search", "جستجو", "w-lg", "نام خدمت…"),
           Tb("combo", "دسته", "w-md", "همه"), Tb("date", "تاریخ", "w-md", "۱۴۰۴/۰۷/۱۹")],
  cols=[Col("Code", "کد", "92px", "r", "text", "hash"),
        Col("Patient", "بیمار", None, "r", "who", "user"),
        Col("Service", "خدمت", "180px", "r", "text", "sparkles"),
        Col("Date", "تاریخ", "112px", "r", "date"),
        Col("Amount", "مبلغ", "108px", "l", "money"),
        Col("Status", "وضعیت", "104px", "c", "chip")],
  rows=[
      ["C-۳۰۱", who("سارا احمدی", "P-۱۰۲۴", "سا", "b3"), "تزریقات", dtx("۱۴۰۴/۰۷/۱۹"), num("۸۰,۰۰۰"), chip("انجام شد", "ok")],
      ["C-۳۰۲", who("محمد رضایی", "P-۱۰۲۵", "مر", ""), "پانسمان", dtx("۱۴۰۴/۰۷/۱۹"), num("۱۲۰,۰۰۰"), chip("در انتظار", "wn")],
  ],
  actions=[Act("New", "خدمت جدید", "p", "plus"), Act("Done", "انجام شد", "ok", "check")],
  rel=[("خدمات تکمیلی", "صورتحساب", "خدمت فاکتور می‌شود")])

F("ConsumablesView", "لوازم مصرفی", "package", "list-dense",
  "مصرف لوازم در ویزیت و خدمات", "ویزیت",
  toolbar=[Tb("search", "جستجو", "w-lg", "نام قلم…"), Tb("combo", "گروه", "w-md", "همه")],
  cols=[Col("Code", "کد", "92px", "r", "text", "hash"),
        Col("Name", "نام قلم", None, "r", "text", "package"),
        Col("Unit", "واحد", "80px", "c", "text"),
        Col("Stock", "موجودی", "96px", "c", "num"),
        Col("MinStock", "حد سفارش", "96px", "c", "num"),
        Col("Price", "قیمت", "104px", "l", "money"),
        Col("Status", "وضعیت", "104px", "c", "chip")],
  rows=[
      ["K-۱۰۱", "دستکش لاتکس (بسته ۱۰۰)", "بسته", num("۴۲"), num("۲۰"), num("۷۵,۰۰۰"), chip("کافی", "ok")],
      ["K-۱۰۲", "سرنگ ۵ سی‌سی", "عدد", num("۱۱"), num("۵۰"), num("۳,۵۰۰"), chip("کمبود", "dg")],
      ["K-۱۰۳", "پنبه استریل ۵۰۰ گرمی", "بسته", num("۲۸"), num("۱۵"), num("۴۵,۰۰۰"), chip("کافی", "ok")],
      ["K-۱۰۴", "الکل ۷۰٪ یک لیتری", "بطری", num("۱۶"), num("۱۲"), num("۶۲,۰۰۰"), chip("نزدیک حد", "wn")],
  ],
  actions=[Act("New", "قلم جدید", "p", "plus"), Act("StockIn", "ورود کالا", "s", "package-plus"),
           Act("Export", "خروجی", "s", "file-spreadsheet")],
  rel=[("لوازم مصرفی", "ورود کالا", "تأمین موجودی"),
       ("لوازم مصرفی", "انبارگردانی", "شمارش دوره‌ای")])

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  💊 گروه ۶ — نسخه                                                    ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("PrescriptionsView", "نسخه‌ها", "file-text", "list-dense",
  "فهرست نسخه‌های صادرشده", "نسخه",
  toolbar=[Tb("search", "جستجو", "w-xl", "نام بیمار یا کد نسخه…"),
           Tb("combo", "پزشک", "w-md", "همه"), Tb("combo", "وضعیت", "w-md", "همه",
           opts=["همه", "صادر شده", "تحویل شده", "لغو شده"])],
  cols=[Col("Code", "کد نسخه", "108px", "r", "text", "hash"),
        Col("Patient", "بیمار", None, "r", "who", "user"),
        Col("Doctor", "پزشک", "148px", "r", "text", "stethoscope"),
        Col("Date", "تاریخ", "112px", "r", "date"),
        Col("Items", "اقلام", "76px", "c", "num"),
        Col("Amount", "مبلغ", "112px", "l", "money"),
        Col("Status", "وضعیت", "112px", "c", "chip")],
  rows=[
      ["RX-۸۵۲۰", who("سارا احمدی", "P-۱۰۲۴", "سا", "b3"), "دکتر موسوی", dtx("۱۴۰۴/۰۷/۱۲"), num("۳"), num("۲۴۵,۰۰۰"), chip("تحویل شده", "ok")],
      ["RX-۸۵۲۱", who("محمد رضایی", "P-۱۰۲۵", "مر", ""), "دکتر رضایی", dtx("۱۴۰۴/۰۷/۱۲"), num("۲"), num("۱۸۰,۰۰۰"), chip("صادر شده", "in")],
      ["RX-۸۵۲۲", who("فاطمه کریمی", "P-۱۰۲۶", "فک", "b2"), "دکتر موسوی", dtx("۱۴۰۴/۰۷/۱۱"), num("۴"), num("۳۱۰,۰۰۰"), chip("تحویل شده", "ok")],
  ],
  actions=[Act("New", "نسخه جدید", "p", "plus"), Act("Dispense", "تحویل دارو", "ok", "package-check"),
           Act("Print", "چاپ", "s", "printer")],
  rel=[("نسخه‌ها", "داروخانه", "تحویل از داروخانه"),
       ("نسخه‌ها", "نسخه الکترونیک", "ارسال به سامانه")])

F("PrescriptionFormWindow", "فرم نسخه", "file-plus", "form-lines",
  "ثبت نسخه با بررسی تداخل دارویی", "نسخه",
  sections=[Sec("اطلاعات نسخه", "file-text", [
      Fd("PatientId", "lookup", "بیمار", 6, True, "user", "سارا احمدی — P-۱۰۲۴"),
      Fd("DoctorId", "combo", "پزشک", 3, True, "stethoscope", "دکتر موسوی"),
      Fd("Date", "date", "تاریخ", 3, True, "calendar-days", "۱۴۰۴/۰۷/۱۹"),
      Fd("Diagnosis", "lookup", "تشخیص", 12, False, "circle-check", "K21.9 — رفلاکس معده"),
      Fd("Notes", "textarea", "توصیه‌ها", 12, False, "notebook-pen", "یک ساعت پس از غذا مصرف شود"),
  ])],
  total=dict(items="۳ قلم", subtotal="۲۴۵,۰۰۰", discount="۰", total="۲۴۵,۰۰۰"),
  actions=[Act("Save", "ذخیره نسخه", "p", "save"), Act("Check", "بررسی تداخل", "s", "shield-alert"),
           Act("Send", "ارسال الکترونیک", "s", "send"), Act("Cancel", "انصراف", "g")],
  rel=[("فرم نسخه", "داروخانه", "موجودی دارو بررسی می‌شود"),
       ("فرم نسخه", "نسخه الکترونیک", "ارسال به سامانه کشوری"),
       ("فرم نسخه", "پرونده‌ی بیمار", "حساسیت‌ها در تداخل لحاظ می‌شود")],
  notes="جدول اقلام با ستون‌های دارو، دوز، تعداد دفعات، مدت و تعداد؛ سبدِ خلاصه در ستون چپ با جمع مبالغ.")

F("PrescriptionDetailWindow", "جزئیات نسخه", "file-search", "dialog",
  "نمایش کامل نسخه و اقلام آن", "نسخه",
  rel=[("جزئیات نسخه", "داروخانه", "تحویل دارو")])

F("EPrescriptionView", "نسخه الکترونیک", "send", "list-dense",
  "ارسال نسخه‌ها به سامانه کشوری", "نسخه",
  toolbar=[Tb("search", "جستجو", "w-lg", "کد نسخه…"),
           Tb("combo", "وضعیت ارسال", "w-md", "همه", opts=["همه", "ارسال شده", "خطا", "در صف"])],
  cols=[Col("Code", "کد", "108px", "r", "text", "hash"),
        Col("Patient", "بیمار", None, "r", "who", "user"),
        Col("Date", "تاریخ", "112px", "r", "date"),
        Col("TrackCode", "کد رهگیری", "140px", "l", "num"),
        Col("Status", "وضعیت", "120px", "c", "chip")],
  rows=[
      ["RX-۸۵۲۰", who("سارا احمدی", "P-۱۰۲۴", "سا", "b3"), dtx("۱۴۰۴/۰۷/۱۲"), num("۱۲۳۴۵۶۷۸۹۰"), chip("ارسال شده", "ok")],
      ["RX-۸۵۲۱", who("محمد رضایی", "P-۱۰۲۵", "مر", ""), dtx("۱۴۰۴/۰۷/۱۲"), num("—"), chip("خطا", "dg")],
  ],
  actions=[Act("Send", "ارسال مجدد", "p", "send"), Act("Refresh", "تازه‌سازی", "g", "refresh-cw")],
  rel=[("نسخه الکترونیک", "نسخه‌ها", "منبع نسخه‌ها")])

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  🏪 گروه ۷ — داروخانه                                                ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("PharmacyView", "داروخانه", "pill", "pos",
  "تحویل نسخه و فروشِ دارو با الگوی FEFO", "داروخانه",
  kpis=[Kpi("package", "اقلام موجود", "۴۲۸", None, "flat", "teal"),
        Kpi("triangle-alert", "زیر حد سفارش", "۱۷", "+۳", "down", "amber"),
        Kpi("calendar-x", "نزدیک انقضا", "۲۹", "+۵", "down", "rose"),
        Kpi("banknote", "فروش امروز", "۵,۸۴۰,۰۰۰", "+۱۴٪", "up", "green")],
  toolbar=[Tb("search", "جستجو", "w-xl", "نام دارو یا بارکد…"),
           Tb("combo", "گروه دارویی", "w-md", "همه"),
           Tb("check", "فقط موجود", None, True)],
  cols=[Col("Code", "کد", "92px", "r", "text", "hash"),
        Col("Name", "نام دارو", None, "r", "text", "pill"),
        Col("Generic", "نام ژنریک", "150px", "r", "text"),
        Col("Batch", "بچ", "92px", "c", "num"),
        Col("Stock", "موجودی", "92px", "c", "num"),
        Col("Price", "قیمت", "104px", "l", "money"),
        Col("Expiry", "انقضا", "108px", "r", "date"),
        Col("Status", "وضعیت", "104px", "c", "chip")],
  rows=[
      ["D-۱۰۰۱", "آموکسی‌سیلین ۵۰۰ کپسول", "Amoxicillin", num("A-۲۲"), num("۳۲۰"), num("۴۵,۰۰۰"), dtx("۱۴۰۵/۰۲/۱۵"), chip("کافی", "ok")],
      ["D-۱۰۰۲", "امپرازول ۲۰ کپسول", "Omeprazole", num("B-۱۴"), num("۸"), num("۳۲,۰۰۰"), dtx("۱۴۰۴/۱۱/۰۸"), chip("کمبود", "dg")],
      ["D-۱۰۰۳", "متفورمین ۵۰۰ قرص", "Metformin", num("C-۳۱"), num("۵۴۰"), num("۲۸,۰۰۰"), dtx("۱۴۰۵/۰۶/۲۰"), chip("کافی", "ok")],
      ["D-۱۰۰۴", "آسپرین ۸۰ قرص", "Aspirin", num("D-۰۷"), num("۶۲"), num("۱۸,۰۰۰"), dtx("۱۴۰۴/۰۸/۰۱"), chip("نزدیک انقضا", "wn")],
  ],
  actions=[Act("Dispense", "تحویل نسخه", "p", "package-check"),
           Act("StockIn", "ورود کالا", "s", "package-plus"),
           Act("Stocktake", "انبارگردانی", "s", "clipboard-check")],
  rel=[("داروخانه", "فرم قلم دارویی", "تعریف قلم"),
       ("داروخانه", "ورود کالا", "تأمین موجودی"),
       ("داروخانه", "انبارگردانی", "شمارش"),
       ("داروخانه", "نسخه‌ها", "تحویل بر اساس نسخه")],
  notes="ستون چپ: کاتالوگِ داروها با جستجوی بارکد. ستون راست: سبدِ تحویل با جمع مبالغ؛ الگوی FEFO بچ نزدیک‌تر به انقضا را اول مصرف می‌کند.")

F("PharmacyItemFormWindow", "فرم قلم دارویی", "package-plus", "form-std",
  "تعریف یا ویرایش قلم دارویی", "داروخانه",
  sections=[Sec("شناسه قلم", "package", [
      Fd("Code", "text", "کد قلم", 3, True, "hash", "D-۱۰۰۵"),
      Fd("Name", "text", "نام تجاری", 5, True, "pill", "سفکسیم ۴۰۰ کپسول"),
      Fd("GenericName", "text", "نام ژنریک", 4, True, "pill", "Cefixime"),
      Fd("Kind", "combo", "نوع قلم", 3, True, "boxes", "دارو", opts=["دارو", "لوازم مصرفی"]),
      Fd("Category", "combo", "گروه دارویی", 3, False, "tag", "آنتی‌بیوتیک"),
      Fd("Form", "combo", "شکل دارویی", 3, False, "pill", "کپسول",
         opts=["قرص", "کپسول", "شربت", "آمپول", "قطره", "پماد"]),
      Fd("Unit", "combo", "واحد", 3, False, "scale", "عدد", opts=["عدد", "بسته", "بطری", "جعبه"]),
  ]),
  Sec("موجودی و قیمت", "coins", [
      Fd("MinStock", "number", "حد سفارش", 3, False, "triangle-alert", "۵۰"),
      Fd("SalePrice", "money", "قیمت فروش", 3, False, "banknote", "۶۵,۰۰۰"),
      Fd("PurchasePrice", "money", "قیمت خرید", 3, False, "shopping-cart", "۵۲,۰۰۰"),
      Fd("TaxRate", "number", "درصد مالیات", 3, False, "percent", "۹"),
      Fd("Barcode", "barcode", "بارکد", 6, False, "barcode", "۶۲۶۰۱۰۷۰۰۱۲۳۴"),
      Fd("IrcCode", "text", "کد IRC", 6, False, "hash", "۱۲۳۴۵۶۷۸۹۰"),
      Fd("RequiresPrescription", "check", "نیازمند نسخه", 6, False, None, True),
      Fd("IsNarcotic", "check", "داروی مخدر", 6, False, None, False),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Cancel", "انصراف", "g")],
  rel=[("فرم قلم دارویی", "ورود کالا", "بچ برای این قلم")])

F("StockInFormWindow", "ورود کالا", "package-plus", "form-lines",
  "ثبت ورود دسته‌جمعی کالا به انبار", "داروخانه",
  sections=[Sec("اطلاعات فاکتور خرید", "truck", [
      Fd("Supplier", "combo", "تأمین‌کننده", 5, True, "truck", "شرکت پخش البرز"),
      Fd("InvoiceNo", "text", "شماره فاکتور", 3, True, "file-text", "F-۹۸۷۶۵"),
      Fd("Date", "date", "تاریخ ورود", 2, True, "calendar-days", "۱۴۰۴/۰۷/۱۹"),
      Fd("Total", "money", "جمع فاکتور", 2, False, "banknote", "۱۲,۴۰۰,۰۰۰"),
  ])],
  total=dict(items="۴ ردیف", subtotal="۱۲,۴۰۰,۰۰۰", discount="۰", total="۱۲,۴۰۰,۰۰۰"),
  actions=[Act("Save", "ثبت ورود", "p", "check"), Act("AddRow", "افزودن ردیف", "s", "plus"),
           Act("Cancel", "انصراف", "g")],
  rel=[("ورود کالا", "داروخانه", "موجودی به‌روز می‌شود"),
       ("ورود کالا", "حسابداری", "سند خرید صادر می‌شود")])

F("StocktakingView", "انبارگردانی", "clipboard-check", "list-dense",
  "شمارش دوره‌ای موجودی انبار", "داروخانه",
  toolbar=[Tb("combo", "انبار", "w-md", "اصلی"),
           Tb("date", "تاریخ شمارش", "w-md", "۱۴۰۴/۰۷/۱۹"),
           Tb("check", "فقط مغایرت‌دار", None, False)],
  cols=[Col("Code", "کد", "92px", "r", "text", "hash"),
        Col("Name", "نام قلم", None, "r", "text", "package"),
        Col("System", "موجودی سیستم", "112px", "c", "num"),
        Col("Counted", "شمارش‌شده", "108px", "c", "num"),
        Col("Diff", "مغایرت", "108px", "c", "num"),
        Col("Status", "وضعیت", "112px", "c", "chip")],
  rows=[
      ["D-۱۰۰۱", "آموکسی‌سیلین ۵۰۰ کپسول", num("۳۲۰"), num("۳۲۰"), num("۰"), chip("تطابق", "ok")],
      ["D-۱۰۰۲", "امپرازول ۲۰ کپسول", num("۱۲"), num("۸"), num("−۴"), chip("مغایرت", "dg")],
      ["D-۱۰۰۳", "متفورمین ۵۰۰ قرص", num("۵۴۰"), num("۵۴۰"), num("۰"), chip("تطابق", "ok")],
  ],
  actions=[Act("Save", "ثبت شمارش", "p", "save"), Act("Export", "خروجی", "s", "file-spreadsheet")],
  rel=[("انبارگردانی", "داروخانه", "موجودی اصلاح می‌شود")])

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  🧪 گروه ۸ — آزمایشگاه                                               ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("LabView", "آزمایشگاه", "flask-conical", "list-dense",
  "پیگیری درخواست‌ها و نتایج آزمایش", "آزمایشگاه",
  kpis=[Kpi("test-tube", "درخواست امروز", "۲۴", "+۶", "up", "teal"),
        Kpi("clock", "در انتظار نمونه", "۹", None, "flat", "amber"),
        Kpi("circle-check", "نتیجه آماده", "۱۱", None, "flat", "green"),
        Kpi("send", "ارسال‌شده با پیامک", "۸", None, "flat", "blue")],
  toolbar=[Tb("search", "جستجو", "w-xl", "نام بیمار یا کد…"),
           Tb("combo", "وضعیت", "w-md", "همه", opts=["همه", "درخواست", "نمونه‌گیری", "در حال انجام", "آماده", "ارسال شده"]),
           Tb("date", "تاریخ", "w-md", "۱۴۰۴/۰۷/۱۹")],
  cols=[Col("Code", "کد", "100px", "r", "text", "hash"),
        Col("Patient", "بیمار", None, "r", "who", "user"),
        Col("Tests", "آزمایش‌ها", "220px", "r", "text", "test-tube"),
        Col("Date", "تاریخ", "112px", "r", "date"),
        Col("Amount", "مبلغ", "108px", "l", "money"),
        Col("Status", "وضعیت", "124px", "c", "chip")],
  rows=[
      ["L-۷۴۰", who("سارا احمدی", "P-۱۰۲۴", "سا", "b3"), "CBC، قند ناشتا، چربی", dtx("۱۴۰۴/۰۷/۱۸"), num("۴۲۰,۰۰۰"), chip("آماده", "ok")],
      ["L-۷۴۱", who("محمد رضایی", "P-۱۰۲۵", "مر", ""), "TSH، T4", dtx("۱۴۰۴/۰۷/۱۸"), num("۳۸۰,۰۰۰"), chip("در حال انجام", "in")],
      ["L-۷۴۲", who("فاطمه کریمی", "P-۱۰۲۶", "فک", "b2"), "آزمایش ادرار", dtx("۱۴۰۴/۰۷/۱۹"), num("۹۵,۰۰۰"), chip("نمونه‌گیری", "wn")],
  ],
  actions=[Act("New", "درخواست جدید", "p", "plus"), Act("Result", "ثبت نتیجه", "ok", "file-check"),
           Act("Print", "چاپ جواب", "s", "printer"), Act("SendSms", "ارسال پیامک", "s", "send")],
  rel=[("آزمایشگاه", "درخواست آزمایش", "ثبت درخواست"),
       ("آزمایشگاه", "ثبت نتیجه آزمایش", "ورود نتایج"),
       ("آزمایشگاه", "پیامک", "اطلاع‌رسانی نتیجه"),
       ("آزمایشگاه", "پرونده‌ی بیمار", "نتایج در پرونده")])

F("LabOrderFormWindow", "درخواست آزمایش", "test-tube", "form-std",
  "ثبت درخواست آزمایش برای بیمار", "آزمایشگاه",
  sections=[Sec("درخواست", "clipboard-list", [
      Fd("PatientId", "lookup", "بیمار", 6, True, "user", "سارا احمدی — P-۱۰۲۴"),
      Fd("DoctorId", "combo", "پزشک تجویزکننده", 3, True, "stethoscope", "دکتر موسوی"),
      Fd("Date", "date", "تاریخ", 3, True, "calendar-days", "۱۴۰۴/۰۷/۱۹"),
      Fd("Priority", "combo", "اولویت", 3, False, "flag", "عادی", opts=["عادی", "فوری", "اورژانس"]),
      Fd("Lab", "combo", "آزمایشگاه", 3, False, "building-2", "آزمایشگاه مرکزی"),
      Fd("Fasting", "check", "ناشتا", 3, False, None, True),
      Fd("Tests", "combo", "آزمایش‌ها", 12, True, "test-tube", "CBC، قند ناشتا",
         opts=["CBC", "قند ناشتا", "چربی خون", "کبد", "کلیه", "TSH", "ادرار"]),
      Fd("Notes", "textarea", "توضیحات", 12, False, "notebook-pen", ""),
  ])],
  actions=[Act("Save", "ثبت درخواست", "p", "save"), Act("Print", "چاپ برچسب", "s", "printer"),
           Act("Cancel", "انصراف", "g")],
  rel=[("درخواست آزمایش", "آزمایشگاه", "در فهرست درخواست‌ها")])

F("LabResultWindow", "ثبت نتیجه آزمایش", "file-check", "form-lines",
  "ورود نتایج آزمایش و مقادیر مرجع", "آزمایشگاه",
  sections=[Sec("اطلاعات نمونه", "test-tube", [
      Fd("OrderCode", "text", "کد درخواست", 4, False, "hash", "L-۷۴۰"),
      Fd("Patient", "text", "بیمار", 4, False, "user", "سارا احمدی"),
      Fd("SampleDate", "date", "تاریخ نمونه", 2, False, "calendar-days", "۱۴۰۴/۰۷/۱۸"),
      Fd("ReportDate", "date", "تاریخ پاسخ", 2, False, "calendar-check", "۱۴۰۴/۰۷/۱۹"),
  ])],
  total=dict(items="۱۸ پارامتر", subtotal="—", discount="—", total="آماده ارسال"),
  actions=[Act("Save", "ذخیره نتایج", "p", "save"), Act("Approve", "تأیید نهایی", "ok", "check"),
           Act("SendSms", "ارسال پیامک", "s", "send")],
  rel=[("ثبت نتیجه آزمایش", "آزمایشگاه", "وضعیت به «آماده» تغییر می‌کند"),
       ("ثبت نتیجه آزمایش", "پرونده‌ی بیمار", "نتیجه در پرونده ثبت می‌شود")])

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  🖼 گروه ۹ — تصویربرداری                                             ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("ImagingView", "تصویربرداری", "scan-line", "list-dense",
  "مدیریت درخواست‌ها و تصاویر پزشکی", "تصویربرداری",
  toolbar=[Tb("search", "جستجو", "w-xl", "نام بیمار یا کد…"),
           Tb("combo", "نوع تصویر", "w-md", "همه", opts=["همه", "رادیوگرافی", "سونوگرافی", "CT", "MRI"]),
           Tb("date", "تاریخ", "w-md", "۱۴۰۴/۰۷/۱۹")],
  cols=[Col("Code", "کد", "100px", "r", "text", "hash"),
        Col("Patient", "بیمار", None, "r", "who", "user"),
        Col("Modality", "نوع", "130px", "r", "text", "scan-line"),
        Col("BodyPart", "ناحیه", "130px", "r", "text"),
        Col("Date", "تاریخ", "112px", "r", "date"),
        Col("Images", "تصاویر", "86px", "c", "num"),
        Col("Status", "وضعیت", "112px", "c", "chip")],
  rows=[
      ["I-۳۱۰", who("سارا احمدی", "P-۱۰۲۴", "سا", "b3"), "رادیوگرافی", "قفسه سینه", dtx("۱۴۰۴/۰۷/۱۸"), num("۲"), chip("گزارش شده", "ok")],
      ["I-۳۱۱", who("محمد رضایی", "P-۱۰۲۵", "مر", ""), "سونوگرافی", "شکم", dtx("۱۴۰۴/۰۷/۱۹"), num("۶"), chip("در انتظار گزارش", "wn")],
  ],
  actions=[Act("New", "درخواست جدید", "p", "plus"), Act("Open", "نمایش تصاویر", "s", "monitor"),
           Act("Report", "ثبت گزارش", "ok", "file-check")],
  rel=[("تصویربرداری", "نمایشگر تصاویر پزشکی", "مشاهده DICOM"),
       ("تصویربرداری", "پرونده‌ی بیمار", "تصاویر در پرونده")])

F("DicomViewerWindow", "نمایشگر تصاویر پزشکی", "monitor", "viewer",
  "مشاهده و ابزارهای تصاویر DICOM", "تصویربرداری",
  notes="ریل ابزار عمودی: پیمایش قاب، بزرگ‌نمایی، جابه‌جایی، تنظیم پنجره (WW/WL)، اندازه‌گیری، حاشیه‌نویسی و صدور؛ نمایشگر با پس‌زمینه تیره و اطلاعات بیمار در گوشه‌ها.")


# ╔═════════════════════════════════════════════════════════════════════╗
# ║  💰 گروه ۱۰ — مالی                                                   ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("BillingView", "صورتحساب", "receipt", "list-dense",
  "فهرست صورتحساب‌ها و وضعیت پرداخت", "مالی",
  kpis=[Kpi("receipt", "فاکتور امروز", "۴۲", "+۱۰٪", "up", "teal"),
        Kpi("banknote", "مبلغ کل", "۲۸,۴۵۰,۰۰۰", "+۲۳٪", "up", "green"),
        Kpi("clock", "پرداخت‌نشده", "۵,۲۰۰,۰۰۰", "+۴٪", "down", "amber"),
        Kpi("shield-check", "سهم بیمه", "۱۱,۳۰۰,۰۰۰", None, "flat", "blue")],
  toolbar=[Tb("search", "جستجو", "w-xl", "شماره فاکتور یا بیمار…"),
           Tb("combo", "وضعیت", "w-md", "همه", opts=["همه", "پرداخت شده", "نیمه‌پرداخت", "پرداخت‌نشده", "لغو"]),
           Tb("date", "از تاریخ", "w-md", "۱۴۰۴/۰۷/۰۱"), Tb("date", "تا تاریخ", "w-md", "۱۴۰۴/۰۷/۱۹")],
  cols=[Col("No", "شماره", "110px", "r", "text", "hash"),
        Col("Patient", "بیمار", None, "r", "who", "user"),
        Col("Date", "تاریخ", "112px", "r", "date"),
        Col("Total", "مبلغ کل", "116px", "l", "money"),
        Col("InsShare", "سهم بیمه", "112px", "l", "money"),
        Col("PatShare", "سهم بیمار", "112px", "l", "money"),
        Col("Paid", "پرداختی", "112px", "l", "money"),
        Col("Status", "وضعیت", "116px", "c", "chip")],
  rows=[
      ["INV-۹۵۱۲", who("سارا احمدی", "P-۱۰۲۴", "سا", "b3"), dtx("۱۴۰۴/۰۷/۱۹"), num("۲۵۰,۰۰۰"), num("۱۷۵,۰۰۰"), num("۷۵,۰۰۰"), num("۷۵,۰۰۰"), chip("پرداخت شده", "ok")],
      ["INV-۹۵۱۳", who("محمد رضایی", "P-۱۰۲۵", "مر", ""), dtx("۱۴۰۴/۰۷/۱۹"), num("۴۰۰,۰۰۰"), num("۲۸۰,۰۰۰"), num("۱۲۰,۰۰۰"), num("۰"), chip("پرداخت‌نشده", "dg")],
      ["INV-۹۵۱۴", who("فاطمه کریمی", "P-۱۰۲۶", "فک", "b2"), dtx("۱۴۰۴/۰۷/۱۸"), num("۱۵۰,۰۰۰"), num("۰"), num("۱۵۰,۰۰۰"), num("۸۰,۰۰۰"), chip("نیمه‌پرداخت", "wn")],
  ],
  actions=[Act("New", "فاکتور جدید", "p", "plus"), Act("Pay", "ثبت پرداخت", "ok", "hand-coins"),
           Act("Print", "چاپ", "s", "printer"), Act("Export", "خروجی", "s", "file-spreadsheet")],
  rel=[("صورتحساب", "فرم صورتحساب", "ایجاد/ویرایش"),
       ("صورتحساب", "حسابداری", "سند دوبلِ خودکار"),
       ("صورتحساب", "بیمه و مطالبات", "مطالبات دوره‌ای"),
       ("صورتحساب", "پذیرش و صف", "از پذیرش می‌آید")])

F("InvoiceFormWindow", "فرم صورتحساب", "file-plus", "form-lines",
  "ایجاد یا ویرایش صورتحساب", "مالی",
  sections=[Sec("اطلاعات فاکتور", "receipt", [
      Fd("PatientId", "lookup", "بیمار", 6, True, "user", "سارا احمدی — P-۱۰۲۴"),
      Fd("Date", "date", "تاریخ", 3, True, "calendar-days", "۱۴۰۴/۰۷/۱۹"),
      Fd("InsuranceId", "combo", "بیمه", 3, False, "shield", "تأمین اجتماعی"),
      Fd("ReceptionId", "lookup", "پذیرش مرتبط", 6, False, "clipboard-list", "R-۱۲۸۴"),
      Fd("DoctorId", "combo", "پزشک", 6, False, "stethoscope", "دکتر موسوی"),
  ])],
  total=dict(items="۳ ردیف", subtotal="۲۷۵,۰۰۰", discount="۲۵,۰۰۰", total="۲۵۰,۰۰۰"),
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Pay", "ثبت پرداخت", "ok", "hand-coins"),
           Act("Print", "چاپ فاکتور", "s", "printer"), Act("Cancel", "انصراف", "g")],
  rel=[("فرم صورتحساب", "تعرفه خدمات", "قیمت از تعرفه"),
       ("فرم صورتحساب", "حسابداری", "سند خودکارِ متوازن")])

F("AccountingView", "حسابداری", "calculator", "split",
  "درخت سرفصل‌ها و اسناد حسابداری دوبل", "مالی",
  kpis=[Kpi("trending-up", "درآمد ماه", "۴۲۸,۰۰۰,۰۰۰", "+۱۸٪", "up", "green"),
        Kpi("trending-down", "هزینه ماه", "۲۹۱,۰۰۰,۰۰۰", "+۱۱٪", "up", "amber"),
        Kpi("equal", "سود خالص", "۱۳۷,۰۰۰,۰۰۰", "+۲۴٪", "up", "teal"),
        Kpi("scale", "تراز", "متوازن", None, "flat", "blue")],
  toolbar=[Tb("search", "جستجو", "w-xl", "شماره سند یا شرح…"),
           Tb("date", "از تاریخ", "w-md", "۱۴۰۴/۰۷/۰۱"), Tb("date", "تا تاریخ", "w-md", "۱۴۰۴/۰۷/۱۹")],
  cols=[Col("No", "شماره سند", "104px", "r", "text", "hash"),
        Col("Date", "تاریخ", "112px", "r", "date"),
        Col("Desc", "شرح", None, "r", "text", "file-text"),
        Col("Debit", "بدهکار", "124px", "l", "money"),
        Col("Credit", "بستانکار", "124px", "l", "money"),
        Col("Status", "وضعیت", "112px", "c", "chip")],
  rows=[
      ["JV-۱۲۴۰", dtx("۱۴۰۴/۰۷/۱۹"), "فاکتور INV-۹۵۱۲ — درآمد ویزیت", num("۲۵۰,۰۰۰"), num("۲۵۰,۰۰۰"), chip("تراز", "ok")],
      ["JV-۱۲۴۱", dtx("۱۴۰۴/۰۷/۱۹"), "خرید دارو — فاکتور F-۹۸۷۶۵", num("۱۲,۴۰۰,۰۰۰"), num("۱۲,۴۰۰,۰۰۰"), chip("تراز", "ok")],
      ["JV-۱۲۴۲", dtx("۱۴۰۴/۰۷/۱۸"), "حقوق پرسنل — تیر ماه", num("۱۸۵,۰۰۰,۰۰۰"), num("۱۸۵,۰۰۰,۰۰۰"), chip("تراز", "ok")],
      ["JV-۱۲۴۳", dtx("۱۴۰۴/۰۷/۱۸"), "پیش‌پرداخت بیمه تکمیلی", num("۸,۰۰۰,۰۰۰"), num("۰"), chip("نامتوازن", "dg")],
  ],
  actions=[Act("New", "سند جدید", "p", "plus"), Act("Detail", "جزئیات سند", "s", "eye"),
           Act("Trial", "تراز آزمایشی", "s", "scale"), Act("Export", "خروجی", "s", "file-spreadsheet")],
  aside=dict(title="درخت سرفصل‌ها", icon="git-fork",
             fields=[("۱ — دارایی‌ها", "۱,۲۴۰,۰۰۰,۰۰۰"), ("۲ — بدهی‌ها", "۴۱۸,۰۰۰,۰۰۰"),
                     ("۳ — سرمایه", "۶۸۰,۰۰۰,۰۰۰"), ("۴ — درآمدها", "۴۲۸,۰۰۰,۰۰۰"),
                     ("۵ — هزینه‌ها", "۲۹۱,۰۰۰,۰۰۰")]),
  rel=[("حسابداری", "جزئیات سند حسابداری", "سطرهای سند"),
       ("حسابداری", "صورتحساب", "سند خودکارِ فاکتور"),
       ("حسابداری", "حقوق و دستمزد", "سند حقوق")],
  notes="قانون طلایی حسابداری دوبل: جمع بدهکار باید برابر با جمع بستانکار باشد؛ سند نامتوازن هرگز ذخیره نمی‌شود.")

F("JournalEntryDetailWindow", "جزئیات سند حسابداری", "file-spreadsheet", "dialog",
  "نمایش و ویرایش سطرهای یک سند", "مالی",
  total=dict(items="۴ سطر", subtotal="۲۵۰,۰۰۰", discount="—", total="بدهکار = بستانکار"),
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Cancel", "انصراف", "g")],
  rel=[("جزئیات سند حسابداری", "حسابداری", "سند اصلی")])

F("TariffFormWindow", "تعرفه خدمات", "tags", "form-std",
  "تعریف تعرفه خدمات برای بیمه‌های مختلف", "مالی",
  sections=[Sec("تعرفه", "tags", [
      Fd("ServiceId", "combo", "خدمت", 6, True, "concierge-bell", "ویزیت عمومی"),
      Fd("InsuranceId", "combo", "بیمه", 3, True, "shield", "تأمین اجتماعی"),
      Fd("Year", "combo", "سال", 3, True, "calendar-days", "۱۴۰۴"),
      Fd("BasePrice", "money", "تعرفه پایه", 3, True, "banknote", "۲۵۰,۰۰۰"),
      Fd("InsuranceShare", "money", "سهم بیمه", 3, False, "shield-check", "۱۷۵,۰۰۰"),
      Fd("PatientShare", "money", "سهم بیمار", 3, True, "hand-coins", "۷۵,۰۰۰"),
      Fd("DoctorShare", "money", "سهم پزشک", 3, False, "stethoscope", "۱۲۵,۰۰۰"),
      Fd("Active", "check", "فعال", 6, False, None, True),
      Fd("Notes", "textarea", "توضیحات", 12, False, "notebook-pen", ""),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Copy", "رونوشت از سال قبل", "s", "copy"),
           Act("Cancel", "انصراف", "g")],
  rel=[("تعرفه خدمات", "صورتحساب", "قیمت‌گذاری فاکتور"),
       ("تعرفه خدمات", "فرم شرکت بیمه", "بیمهٔ مرتبط")])

F("CompanyFormWindow", "فرم شرکت بیمه", "building-2", "form-narrow",
  "تعریف شرکت بیمه و قرارداد", "مالی",
  sections=[Sec("شرکت بیمه", "building-2", [
      Fd("Name", "text", "نام شرکت", 12, True, "building-2", "بیمه تأمین اجتماعی"),
      Fd("Code", "text", "کد شرکت", 6, True, "hash", "INS-۰۰۱"),
      Fd("ContractNo", "text", "شماره قرارداد", 6, False, "file-text", "۱۴۰۴/۲۳۴"),
      Fd("CoveragePercent", "number", "درصد پوشش", 6, False, "percent", "۷۰"),
      Fd("Ceiling", "money", "سقف تعهد ماهانه", 6, False, "banknote", "۵۰,۰۰۰,۰۰۰"),
      Fd("Active", "check", "فعال", 12, False, None, True),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Cancel", "انصراف", "g")],
  rel=[("فرم شرکت بیمه", "بیمه و مطالبات", "در فهرست بیمه‌ها"),
       ("فرم شرکت بیمه", "تعرفه خدمات", "تعرفه برای این بیمه")])

F("InsuranceView", "بیمه و مطالبات", "shield-check", "detail-tabs",
  "پیگیری مطالبات از شرکت‌های بیمه", "مالی",
  kpis=[Kpi("shield-check", "مطالبات جاری", "۸۴,۵۰۰,۰۰۰", "+۷٪", "up", "blue"),
        Kpi("clock", "در انتظار تأیید", "۲۳,۱۰۰,۰۰۰", None, "flat", "amber"),
        Kpi("circle-check", "وصول شده", "۶۱,۴۰۰,۰۰۰", "+۱۲٪", "up", "green"),
        Kpi("circle-x", "برگشتی", "۳,۲۰۰,۰۰۰", "−۲٪", "down", "rose")],
  tabs=[("دوره‌ها", "calendar-range", 6), ("صورت‌حساب‌ها", "receipt", 128),
        ("برگشتی‌ها", "rotate-ccw", 9), ("قراردادها", "signature", 4)],
  toolbar=[Tb("combo", "شرکت بیمه", "w-lg", "همه"),
           Tb("combo", "دوره", "w-md", "مهر ۱۴۰۴"),
           Tb("combo", "وضعیت", "w-md", "همه", opts=["همه", "ارسال شده", "تأیید شده", "وصول شده", "برگشتی"])],
  cols=[Col("Period", "دوره", "112px", "r", "text", "calendar-range"),
        Col("Company", "شرکت بیمه", None, "r", "text", "building-2"),
        Col("Claims", "تعداد", "84px", "c", "num"),
        Col("Amount", "مبلغ", "124px", "l", "money"),
        Col("Approved", "تأیید شده", "124px", "l", "money"),
        Col("Status", "وضعیت", "120px", "c", "chip")],
  rows=[
      ["مهر ۱۴۰۴", "تأمین اجتماعی", num("۴۲"), num("۳۸,۵۰۰,۰۰۰"), num("۳۱,۲۰۰,۰۰۰"), chip("در انتظار", "wn")],
      ["شهریور ۱۴۰۴", "تأمین اجتماعی", num("۳۹"), num("۳۴,۱۰۰,۰۰۰"), num("۳۴,۱۰۰,۰۰۰"), chip("وصول شده", "ok")],
      ["مهر ۱۴۰۴", "سلامت", num("۱۸"), num("۱۱,۹۰۰,۰۰۰"), num("۰"), chip("ارسال شده", "in")],
  ],
  actions=[Act("NewClaim", "مطالبه جدید", "p", "plus"), Act("Send", "ارسال به بیمه", "s", "send"),
           Act("Export", "خروجی", "s", "file-spreadsheet")],
  rel=[("بیمه و مطالبات", "صورتحساب", "فاکتورهای دوره"),
       ("بیمه و مطالبات", "فرم شرکت بیمه", "شرکت‌های طرف قرارداد")])

F("PayrollView", "حقوق و دستمزد", "banknote", "list-dense",
  "محاسبه و پرداخت حقوق پرسنل", "مالی",
  kpis=[Kpi("users", "پرسنل", "۳۴", None, "flat", "teal"),
        Kpi("banknote", "جمع حقوق ماه", "۱۸۵,۰۰۰,۰۰۰", "+۵٪", "up", "green"),
        Kpi("hand-coins", "اضافه‌کار", "۱۲,۴۰۰,۰۰۰", None, "flat", "blue"),
        Kpi("trending-down", "کسورات", "۲۱,۳۰۰,۰۰۰", None, "flat", "amber")],
  toolbar=[Tb("combo", "دوره", "w-md", "مهر ۱۴۰۴"),
           Tb("combo", "دپارتمان", "w-md", "همه"),
           Tb("search", "جستجو", "w-lg", "نام پرسنل…")],
  cols=[Col("Code", "کد", "92px", "r", "text", "hash"),
        Col("Employee", "پرسنل", None, "r", "who", "user"),
        Col("Department", "دپارتمان", "130px", "r", "text"),
        Col("Base", "حقوق پایه", "116px", "l", "money"),
        Col("Overtime", "اضافه‌کار", "108px", "l", "money"),
        Col("Deductions", "کسورات", "108px", "l", "money"),
        Col("Net", "خالص پرداختی", "124px", "l", "money"),
        Col("Status", "وضعیت", "108px", "c", "chip")],
  rows=[
      ["E-۱۰۰۱", who("مریم نوری", "پذیرش", "من", "b2"), "پذیرش", num("۱۲,۰۰۰,۰۰۰"), num("۸۰۰,۰۰۰"), num("۱,۲۰۰,۰۰۰"), num("۱۱,۶۰۰,۰۰۰"), chip("پرداخت شده", "ok")],
      ["E-۱۰۰۲", who("احمد صادقی", "پذیرش", "اص", ""), "پذیرش", num("۱۲,۰۰۰,۰۰۰"), num("۴۰۰,۰۰۰"), num("۱,۱۰۰,۰۰۰"), num("۱۱,۳۰۰,۰۰۰"), chip("پرداخت شده", "ok")],
      ["E-۱۰۰۳", who("دکتر موسوی", "درمان", "دم", "b5"), "درمان", num("۴۵,۰۰۰,۰۰۰"), num("۲,۰۰۰,۰۰۰"), num("۴,۵۰۰,۰۰۰"), num("۴۲,۵۰۰,۰۰۰"), chip("در انتظار", "wn")],
  ],
  actions=[Act("Calculate", "محاسبه", "p", "calculator"), Act("Payslip", "فیش حقوقی", "s", "file-text"),
           Act("Pay", "ثبت پرداخت", "ok", "hand-coins"), Act("Export", "خروجی", "s", "file-spreadsheet")],
  rel=[("حقوق و دستمزد", "فرم کارمند", "حقوق پایه از پرونده"),
       ("حقوق و دستمزد", "ویرایش حقوق", "تغییر حقوق"),
       ("حقوق و دستمزد", "فیش حقوقی", "صدور فیش"),
       ("حقوق و دستمزد", "حسابداری", "سند حقوق")])

F("PayslipWindow", "فیش حقوقی", "file-text", "dialog",
  "پیش‌نمایش فیش حقوقی", "مالی",
  notes="فیش با هدر درمانگاه، مشخصات پرسنل، جدول مزایا و کسورات و خالص پرداختی؛ قابل چاپ و خروجی PDF.")

F("CommissionReportWindow", "گزارش کارمزد", "handshake", "dialog",
  "گزارش کارمزد پزشکان در بازه زمانی", "مالی",
  toolbar=[Tb("combo", "پزشک", "w-lg", "همه"), Tb("date", "از تاریخ", "w-md", "۱۴۰۴/۰۷/۰۱"),
           Tb("date", "تا تاریخ", "w-md", "۱۴۰۴/۰۷/۱۹")],
  cols=[Col("Doctor", "پزشک", None, "r", "who", "stethoscope"),
        Col("Visits", "ویزیت‌ها", "88px", "c", "num"),
        Col("Gross", "ناخالص", "120px", "l", "money"),
        Col("Percent", "درصد", "76px", "c", "num"),
        Col("Commission", "کارمزد", "124px", "l", "money")],
  rows=[
      [who("دکتر موسوی", "متخصص داخلی", "دم", "b5"), num("۱۲۴"), num("۳۱,۰۰۰,۰۰۰"), num("۵۰"), num("۱۵,۵۰۰,۰۰۰")],
      [who("دکتر رضایی", "متخصص قلب", "در", ""), num("۸۷"), num("۳۴,۸۰۰,۰۰۰"), num("۵۵"), num("۱۹,۱۴۰,۰۰۰")],
  ],
  actions=[Act("Print", "چاپ", "p", "printer"), Act("Export", "خروجی", "s", "file-spreadsheet")],
  rel=[("گزارش کارمزد", "فهرست ویزیت‌ها", "مبنای محاسبه"),
       ("گزارش کارمزد", "پزشکان", "درصد قرارداد")])

F("DoctorPerformanceWindow", "عملکرد پزشکان", "trending-up", "dialog",
  "شاخص‌های عملکرد هر پزشک", "مالی",
  toolbar=[Tb("date", "دوره", "w-md", "مهر ۱۴۰۴")],
  cols=[Col("Doctor", "پزشک", None, "r", "who", "stethoscope"),
        Col("Visits", "ویزیت", "76px", "c", "num"),
        Col("Revenue", "درآمد", "124px", "l", "money"),
        Col("AvgTime", "میانگین ویزیت", "112px", "c", "num"),
        Col("Satisfaction", "رضایت", "96px", "c", "chip")],
  rows=[
      [who("دکتر موسوی", "متخصص داخلی", "دم", "b5"), num("۱۲۴"), num("۳۱,۰۰۰,۰۰۰"), num("۱۴ دقیقه"), chip("۴٫۶", "ok")],
      [who("دکتر رضایی", "متخصص قلب", "در", ""), num("۸۷"), num("۳۴,۸۰۰,۰۰۰"), num("۲۱ دقیقه"), chip("۴٫۳", "cy")],
  ],
  actions=[Act("Print", "چاپ", "p", "printer"), Act("Export", "خروجی", "s", "file-spreadsheet")],
  rel=[("عملکرد پزشکان", "گزارش کارمزد", "محاسبه مالی"),
       ("عملکرد پزشکان", "نظرسنجی", "نمره رضایت")])

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  👔 گروه ۱۱ — منابع انسانی                                           ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("PersonnelView", "پرسنل", "users", "list-detail",
  "مدیریت پرونده‌های پرسنلی", "منابع انسانی",
  kpis=[Kpi("users", "کل پرسنل", "۳۴", "+۲", "up", "teal"),
        Kpi("briefcase", "دپارتمان‌ها", "۱۷", None, "flat", "blue"),
        Kpi("circle-check", "فعال", "۳۲", None, "flat", "green"),
        Kpi("circle-x", "غیرفعال", "۲", None, "flat", "rose")],
  toolbar=[Tb("search", "جستجو", "w-xl", "نام، کد پرسنلی یا ملی…"),
           Tb("combo", "دپارتمان", "w-md", "همه", opts=["همه", "پذیرش", "درمان", "مالی", "پشتیبانی"]),
           Tb("combo", "وضعیت", "w-sm", "فعال", opts=["فعال", "غیرفعال"])],
  cols=[Col("Code", "کد", "88px", "r", "text", "hash"),
        Col("Name", "نام", None, "r", "who", "user"),
        Col("Position", "سمت", "150px", "r", "text", "briefcase"),
        Col("Department", "دپارتمان", "118px", "r", "text"),
        Col("Phone", "موبایل", "124px", "l", "num"),
        Col("HireDate", "تاریخ استخدام", "124px", "r", "date"),
        Col("Status", "وضعیت", "96px", "c", "chip")],
  rows=[
      ["E-۱۰۰۱", who("مریم نوری", "کارشناس پذیرش", "من", "b2"), "کارشناس پذیرش", "پذیرش", "۰۹۱۴-۱۲۳-۴۵۶۷", dtx("۱۴۰۱/۰۲/۱۵"), chip("فعال", "ok")],
      ["E-۱۰۰۲", who("احمد صادقی", "مسئول پذیرش", "اص", ""), "مسئول پذیرش", "پذیرش", "۰۹۱۲-۹۸۷-۶۵۴۳", dtx("۱۴۰۰/۰۸/۰۱"), chip("فعال", "ok")],
      ["E-۱۰۰۳", who("دکتر موسوی", "متخصص داخلی", "دم", "b5"), "متخصص داخلی", "درمان", "۰۹۱۱-۴۵۶-۷۸۹۰", dtx("۱۳۹۸/۰۴/۱۰"), chip("فعال", "ok")],
      ["E-۱۰۰۴", who("لیلا حسنی", "حسابدار", "لح", "b4"), "حسابدار", "مالی", "۰۹۳۵-۱۱۱-۲۲۳۳", dtx("۱۴۰۲/۰۱/۲۰"), chip("فعال", "ok")],
  ],
  actions=[Act("New", "پرسنل جدید", "p", "user-plus"), Act("Edit", "ویرایش", "s", "pencil"),
           Act("Salary", "ویرایش حقوق", "s", "banknote"), Act("Export", "خروجی", "s", "file-spreadsheet")],
  aside=dict(title="خلاصه پرونده", icon="id-card",
             fields=[("کد پرسنلی", "E-۱۰۰۱"), ("نام", "مریم نوری"),
                     ("کد ملی", "۰۰۹-۸۷۶۵۴۳-۲"), ("سمت", "کارشناس پذیرش"),
                     ("دپارتمان", "پذیرش"), ("موبایل", "۰۹۱۴-۱۲۳-۴۵۶۷"),
                     ("تاریخ استخدام", "۱۴۰۱/۰۲/۱۵"), ("حقوق پایه", "۱۲,۰۰۰,۰۰۰")]),
  rel=[("پرسنل", "فرم کارمند", "ایجاد/ویرایش"),
       ("پرسنل", "حضور و غیاب", "ساعات کاری"),
       ("پرسنل", "حقوق و دستمزد", "پرداخت"),
       ("پرسنل", "پزشکان", "پزشک از میان پرسنل")])

F("EmployeeFormWindow", "فرم کارمند", "user-plus", "form-std",
  "ایجاد یا ویرایش پرونده پرسنلی", "منابع انسانی",
  sections=[Sec("اطلاعات فردی", "id-card", [
      Fd("FirstName", "text", "نام", 3, True, "user", "مریم"),
      Fd("LastName", "text", "نام خانوادگی", 3, True, "user", "نوری"),
      Fd("NationalId", "national", "کد ملی", 3, False, "fingerprint", "۰۰۹۸۷۶۵۴۳۲"),
      Fd("Gender", "combo", "جنسیت", 3, False, "user-round", "زن", opts=["زن", "مرد"]),
      Fd("BirthDate", "date", "تاریخ تولد", 3, False, "cake", "۱۳۶۵/۰۳/۱۲"),
      Fd("MaritalStatus", "combo", "وضعیت تأهل", 3, False, "heart", "مجرد", opts=["مجرد", "متأهل"]),
      Fd("Education", "combo", "تحصیلات", 3, False, "graduation-cap", "کارشناسی"),
      Fd("Photo", "file", "عکس پرسنلی", 3, False, "image", "profile.jpg"),
  ]),
  Sec("اطلاعات شغلی", "briefcase", [
      Fd("EmployeeCode", "text", "کد پرسنلی", 3, True, "hash", "E-۱۰۰۱"),
      Fd("PositionId", "combo", "سمت", 3, True, "briefcase", "کارشناس پذیرش"),
      Fd("DepartmentId", "combo", "دپارتمان", 3, True, "building-2", "پذیرش"),
      Fd("HireDate", "date", "تاریخ استخدام", 3, True, "calendar-days", "۱۴۰۱/۰۲/۱۵"),
      Fd("EmploymentType", "combo", "نوع همکاری", 4, False, "signature", "تمام‌وقت",
         opts=["تمام‌وقت", "پاره‌وقت", "قراردادی", "کارآموز"]),
      Fd("BranchId", "combo", "شعبه", 4, False, "map-pin", "مرکزی"),
      Fd("IsActive", "check", "فعال", 4, False, None, True),
  ]),
  Sec("اطلاعات مالی", "wallet", [
      Fd("BaseSalary", "money", "حقوق پایه", 3, False, "banknote", "۱۲,۰۰۰,۰۰۰"),
      Fd("HourlyRate", "money", "نرخ ساعتی", 3, False, "clock", "۷۵,۰۰۰"),
      Fd("InsuranceNo", "text", "شماره بیمه", 3, False, "shield", "۱۲۳۴۵۶۷۸۹۰"),
      Fd("BankAccount", "text", "شماره حساب", 3, False, "credit-card", "IR۱۲۰۱۷۰۰۰۰۰۰۰۱۲۳۴۵۶۷۸۹۰"),
  ]),
  Sec("اطلاعات تماس", "phone", [
      Fd("Phone", "phone", "موبایل", 4, False, "smartphone", "۰۹۱۴۱۲۳۴۵۶۷"),
      Fd("Email", "text", "رایانامه", 4, False, "mail", "m.nouri@example.com"),
      Fd("Address", "textarea", "نشانی", 4, False, "map-pin", "تبریز، آبرسان، خیابان …"),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("SaveNew", "ذخیره و جدید", "s", "plus"),
           Act("Cancel", "انصراف", "g")],
  rel=[("فرم کارمند", "حقوق و دستمزد", "حقوق پایه مبنای محاسبه"),
       ("فرم کارمند", "شیفت‌ها", "تخصیص به شیفت")])

F("EmployeeSalaryEditWindow", "ویرایش حقوق", "banknote", "form-narrow",
  "تغییر حقوق پایه و مزایا", "منابع انسانی",
  sections=[Sec("حقوق", "wallet", [
      Fd("Employee", "text", "پرسنل", 12, False, "user", "مریم نوری — E-۱۰۰۱"),
      Fd("BaseSalary", "money", "حقوق پایه", 6, True, "banknote", "۱۴,۰۰۰,۰۰۰"),
      Fd("HourlyRate", "money", "نرخ ساعتی", 6, False, "clock", "۸۷,۵۰۰"),
      Fd("OvertimeRate", "money", "نرخ اضافه‌کار", 6, False, "timer", "۱۲۰,۰۰۰"),
      Fd("Percent", "number", "درصد کارمزد", 6, False, "percent", "۰"),
      Fd("EffectiveDate", "date", "تاریخ اجرا", 12, True, "calendar-days", "۱۴۰۴/۰۸/۰۱"),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Cancel", "انصراف", "g")],
  rel=[("ویرایش حقوق", "حقوق و دستمزد", "در دوره بعد اعمال می‌شود")])

F("AttendanceView", "حضور و غیاب", "fingerprint", "list-dense",
  "ثبت و گزارش ورود و خروج پرسنل", "منابع انسانی",
  kpis=[Kpi("circle-check", "حاضر", "۲۹", None, "flat", "green"),
        Kpi("clock", "تأخیر", "۳", None, "flat", "amber"),
        Kpi("circle-x", "غایب", "۲", None, "flat", "rose"),
        Kpi("calendar-x", "مرخصی", "۰", None, "flat", "blue")],
  toolbar=[Tb("date", "تاریخ", "w-md", "۱۴۰۴/۰۷/۱۹"),
           Tb("combo", "دپارتمان", "w-md", "همه"),
           Tb("search", "جستجو", "w-lg", "نام پرسنل…")],
  cols=[Col("Employee", "پرسنل", None, "r", "who", "user"),
        Col("Date", "تاریخ", "112px", "r", "date"),
        Col("In", "ورود", "96px", "c", "date"),
        Col("Out", "خروج", "96px", "c", "date"),
        Col("Hours", "ساعت کار", "96px", "c", "num"),
        Col("Delay", "تأخیر", "96px", "c", "num"),
        Col("Status", "وضعیت", "104px", "c", "chip")],
  rows=[
      [who("مریم نوری", "پذیرش", "من", "b2"), dtx("۱۴۰۴/۰۷/۱۹"), dtx("۰۸:۰۲"), dtx("۱۴:۰۵"), num("۶:۰۳"), num("۰"), chip("حاضر", "ok")],
      [who("احمد صادقی", "پذیرش", "اص", ""), dtx("۱۴۰۴/۰۷/۱۹"), dtx("۰۸:۲۷"), dtx("۱۴:۰۰"), num("۵:۳۳"), num("۲۷"), chip("تأخیر", "wn")],
      [who("لیلا حسنی", "مالی", "لح", "b4"), dtx("۱۴۰۴/۰۷/۱۹"), dtx("—"), dtx("—"), num("۰"), num("—"), chip("غایب", "dg")],
  ],
  actions=[Act("Export", "خروجی", "s", "file-spreadsheet"), Act("Manual", "ثبت دستی", "s", "pencil"),
           Act("Print", "چاپ", "s", "printer")],
  rel=[("حضور و غیاب", "پرسنل", "تردد پرسنل"),
       ("حضور و غیاب", "حقوق و دستمزد", "مبنای اضافه‌کار")])

F("DoctorsView", "پزشکان", "stethoscope", "list-cards",
  "فهرست پزشکان و برنامه کاری", "منابع انسانی",
  toolbar=[Tb("search", "جستجو", "w-xl", "نام پزشک یا تخصص…"),
           Tb("combo", "تخصص", "w-md", "همه"), Tb("combo", "وضعیت", "w-sm", "فعال")],
  actions=[Act("New", "پزشک جدید", "p", "plus"), Act("Schedule", "برنامه هفتگی", "s", "calendar-days")],
  rel=[("پزشکان", "فرم پزشک", "ایجاد/ویرایش"),
       ("پزشکان", "نوبت‌دهی", "ظرفیت برنامه"),
       ("پزشکان", "گزارش کارمزد", "کارمزد")])

F("DoctorFormWindow", "فرم پزشک", "stethoscope", "form-std",
  "تعریف پزشک، تخصص و برنامه کاری", "منابع انسانی",
  sections=[Sec("اطلاعات حرفه‌ای", "stethoscope", [
      Fd("PersonnelId", "combo", "پرسنل مرتبط", 6, True, "user", "دکتر موسوی (E-۱۰۰۳)"),
      Fd("SpecialtyId", "combo", "تخصص", 3, True, "award", "متخصص داخلی"),
      Fd("MedicalCouncilNo", "text", "شماره نظام پزشکی", 3, True, "hash", "۱۲۳۴۵"),
      Fd("ContractPercent", "number", "درصد قرارداد", 3, False, "handshake", "۵۰"),
      Fd("VisitDuration", "combo", "مدت هر ویزیت", 3, False, "timer", "۲۰ دقیقه",
         opts=["۱۰ دقیقه", "۱۵ دقیقه", "۲۰ دقیقه", "۳۰ دقیقه"]),
      Fd("MaxPerDay", "number", "ظرفیت روزانه", 3, False, "users", "۴۰"),
      Fd("RoomId", "combo", "اتاق", 3, False, "door-open", "اتاق ۲"),
  ]),
  Sec("برنامه کاری هفتگی", "calendar-days", [
      Fd("SatStart", "time", "شنبه — شروع", 2, False, "clock", "۰۸:۰۰"),
      Fd("SatEnd", "time", "شنبه — پایان", 2, False, "clock", "۱۴:۰۰"),
      Fd("SunStart", "time", "یکشنبه — شروع", 2, False, "clock", "۰۸:۰۰"),
      Fd("SunEnd", "time", "یکشنبه — پایان", 2, False, "clock", "۱۴:۰۰"),
      Fd("MonStart", "time", "دوشنبه — شروع", 2, False, "clock", "۱۴:۰۰"),
      Fd("MonEnd", "time", "دوشنبه — پایان", 2, False, "clock", "۲۰:۰۰"),
      Fd("IsActive", "check", "فعال", 6, False, None, True),
      Fd("AcceptsWalkIn", "check", "پذیرش بدون نوبت", 6, False, None, False),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Cancel", "انصراف", "g")],
  rel=[("فرم پزشک", "نوبت‌دهی", "ظرفیت برنامه هفتگی"),
       ("فرم پزشک", "پرسنل", "پزشک از میان پرسنل")])

F("UsersView", "کاربران", "user-cog", "list-dense",
  "مدیریت کاربران و دسترسی‌ها", "منابع انسانی",
  toolbar=[Tb("search", "جستجو", "w-xl", "نام کاربری یا نام…"),
           Tb("combo", "نقش", "w-md", "همه"), Tb("combo", "وضعیت", "w-sm", "فعال")],
  cols=[Col("User", "کاربر", None, "r", "who", "user"),
        Col("Role", "نقش", "140px", "r", "text", "shield"),
        Col("LastLogin", "آخرین ورود", "150px", "r", "date", "clock"),
        Col("Branch", "شعبه", "110px", "r", "text", "map-pin"),
        Col("Status", "وضعیت", "104px", "c", "chip")],
  rows=[
      [who("admin", "سعید نجفی", "عا", ""), "مدیر سیستم", dtx("۱۴۰۴/۰۷/۱۹ ۰۸:۱۲"), "مرکزی", chip("فعال", "ok")],
      [who("mnouri", "مریم نوری", "من", "b2"), "پذیرش", dtx("۱۴۰۴/۰۷/۱۹ ۰۸:۰۵"), "مرکزی", chip("فعال", "ok")],
      [who("lhasani", "لیلا حسنی", "لح", "b4"), "حسابدار", dtx("۱۴۰۴/۰۷/۱۸ ۱۶:۴۰"), "مرکزی", chip("فعال", "ok")],
  ],
  actions=[Act("New", "کاربر جدید", "p", "user-plus"), Act("Permissions", "مجوزها", "s", "key-round"),
           Act("ResetPass", "بازنشانی رمز", "s", "rotate-ccw")],
  rel=[("کاربران", "فرم کاربر", "ایجاد/ویرایش"),
       ("کاربران", "نقش‌ها و مجوزها", "تخصیص نقش"),
       ("کاربران", "گزارش حسابرسی", "ردپای عملیات")])

F("UserFormWindow", "فرم کاربر", "user-cog", "form-narrow",
  "ایجاد یا ویرایش کاربر", "منابع انسانی",
  sections=[Sec("حساب کاربری", "user-cog", [
      Fd("PersonnelId", "combo", "پرسنل", 12, True, "user", "مریم نوری — E-۱۰۰۱"),
      Fd("UserName", "text", "نام کاربری", 6, True, "at-sign", "mnouri"),
      Fd("Password", "password", "رمز عبور", 6, True, "lock", "••••••••"),
      Fd("RoleId", "combo", "نقش", 6, True, "shield", "پذیرش"),
      Fd("BranchId", "combo", "شعبه", 6, False, "map-pin", "مرکزی"),
      Fd("MustChangePassword", "check", "تغییر رمز در نخستین ورود", 12, False, None, True),
      Fd("IsActive", "check", "فعال", 12, False, None, True),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Cancel", "انصراف", "g")],
  rel=[("فرم کاربر", "نقش‌ها و مجوزها", "نقش تعیین‌کننده دسترسی")])

F("RolePermissionView", "نقش‌ها و مجوزها", "shield-check", "split",
  "تعریف نقش و تخصیص مجوزها", "منابع انسانی",
  toolbar=[Tb("search", "جستجو", "w-xl", "نام نقش یا مجوز…"),
           Tb("combo", "ماژول", "w-md", "همه")],
  actions=[Act("New", "نقش جدید", "p", "plus"), Act("Save", "ذخیره مجوزها", "ok", "save")],
  aside=dict(title="نقش‌ها", icon="shield",
             fields=[("مدیر سیستم", "۳۹ مجوز"), ("پزشک", "۱۸ مجوز"),
                     ("پذیرش", "۱۲ مجوز"), ("حسابدار", "۱۴ مجوز"),
                     ("پرستار", "۸ مجوز")]),
  rel=[("نقش‌ها و مجوزها", "کاربران", "تخصیص نقش به کاربر"),
       ("نقش‌ها و مجوزها", "مدیریت سیستم", "بخشی از مدیریت")],
  notes="ستون راست: فهرست نقش‌ها با شمار مجوز. ستون چپ: درخت مجوزها با گروه‌بندی ماژول و چک‌باکس.")

F("AdminView", "مدیریت سیستم", "settings-2", "settings",
  "تنظیمات پیشرفته و ابزارهای مدیریتی", "منابع انسانی",
  sections=[Sec("عملیات", "wrench", [
      Fd("ClearCache", "check", "پاک‌سازی حافظه پنهان", 12, False, None, False),
      Fd("Reindex", "check", "بازسازی نمایه‌ها", 12, False, None, False),
      Fd("MaintenanceMode", "check", "حالت نگهداری", 12, False, None, False),
  ])],
  actions=[Act("Run", "اجرا", "p", "play"), Act("Save", "ذخیره", "s", "save")],
  rel=[("مدیریت سیستم", "نقش‌ها و مجوزها", "مدیریت دسترسی"),
       ("مدیریت سیستم", "گزارش حسابرسی", "ردپای عملیات")])

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  🏢 گروه ۱۲ — سازمانی                                                ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("BranchesView", "شعب", "building-2", "list-cards",
  "مدیریت شعبِ درمانگاه", "سازمانی",
  toolbar=[Tb("search", "جستجو", "w-lg", "نام شعبه…"), Tb("combo", "وضعیت", "w-sm", "فعال")],
  actions=[Act("New", "شعبه جدید", "p", "plus"), Act("Edit", "ویرایش", "s", "pencil")],
  rel=[("شعب", "پرسنل", "انتساب پرسنل به شعبه"),
       ("شعب", "بیماران", "فیلتر بر اساس شعبه")])

F("SepasView", "سامانه سپاس", "link-2", "detail-tabs",
  "یکپارچه‌سازی با سامانه سپاس", "سازمانی",
  tabs=[("تنظیمات اتصال", "plug", None), ("ارسال‌ها", "send", None),
        ("خطاها", "triangle-alert", None)],
  sections=[Sec("اتصال", "link-2", [
      Fd("Endpoint", "text", "نشانی سرویس", 8, True, "link-2", "https://sepas.example.ir/api"),
      Fd("Enabled", "check", "فعال", 4, False, None, True),
      Fd("Username", "text", "نام کاربری", 6, False, "user", "clinic_sahand"),
      Fd("Password", "password", "رمز عبور", 6, False, "lock", "••••••••"),
      Fd("Interval", "combo", "دوره ارسال", 6, False, "clock", "روزانه", opts=["ساعتی", "روزانه", "هفتگی"]),
      Fd("LastSync", "text", "آخرین همگام‌سازی", 6, False, "history", "۱۴۰۴/۰۷/۱۹ ۰۷:۰۰"),
  ])],
  actions=[Act("Save", "ذخیره", "p", "save"), Act("Test", "تست اتصال", "s", "wifi"),
           Act("SyncNow", "همگام‌سازی الآن", "s", "refresh-cw")],
  rel=[("سامانه سپاس", "نسخه الکترونیک", "ارسال داده"),
       ("سامانه سپاس", "پرونده‌ی بیمار", "منبع داده")])

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  📱 گروه ۱۳ — ارتباطات                                               ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("SmsView", "پیامک", "message-square", "detail-tabs",
  "ارسال پیامک و مدیریت الگوها", "ارتباطات",
  kpis=[Kpi("send", "ارسالی امروز", "۱۴۲", "+۲۱", "up", "teal"),
        Kpi("circle-check", "تحویل شده", "۱۳۶", None, "flat", "green"),
        Kpi("circle-x", "ناموفق", "۶", "+۲", "down", "rose"),
        Kpi("wallet", "هزینه امروز", "۴۲,۶۰۰", None, "flat", "amber")],
  tabs=[("ارسال جدید", "send", None), ("الگوها", "file-text", None),
        ("گزارش ارسال", "chart-bar", None), ("تنظیمات پنل", "settings", None)],
  sections=[Sec("ارسال پیامک", "send", [
      Fd("Recipients", "combo", "گیرندگان", 6, True, "users", "بیماران امروز",
         opts=["بیماران امروز", "بیماران هفته", "پرسنل", "همه بیماران", "دستی"]),
      Fd("TemplateId", "combo", "الگو", 6, False, "file-text", "یادآوری نوبت"),
      Fd("ManualNumbers", "textarea", "شماره‌های دستی", 6, False, "smartphone", ""),
      Fd("Message", "textarea", "متن پیام", 12, True, "message-square",
         " بیمار گرامی، نوبت شما فردا ساعت ۱۰:۳۰ نزد دکتر موسوی است."),
      Fd("SendAt", "date", "زمان ارسال", 6, False, "clock", "۱۴۰۴/۰۷/۲۰ ۰۹:۰۰"),
      Fd("Provider", "combo", "پنل ارسال", 6, False, "server", "کاوه‌نگار",
         opts=["کاوه‌نگار", "ملی‌پیامک", "فراز‌اس‌ام‌اس"]),
  ])],
  actions=[Act("Send", "ارسال", "p", "send"), Act("Preview", "پیش‌نمایش", "s", "eye"),
           Act("SaveTemplate", "ذخیره الگو", "s", "save")],
  rel=[("پیامک", "نوبت‌دهی", "یادآوری نوبت"),
       ("پیامک", "آزمایشگاه", "اطلاع‌رسانی نتیجه"),
       ("پیامک", "بیماران", "دریافت‌کنندگان")],
  notes="شمارندهٔ نویسه‌ها و هزینهٔ تخمینی زیر جعبهٔ متن؛ متغیرهای الگو با دکمه‌های درج خودکار.")

F("SurveyView", "نظرسنجی", "star", "detail-tabs",
  "طراحی پرسشنامه و تحلیل پاسخ‌ها", "ارتباطات",
  kpis=[Kpi("star", "میانگین امتیاز", "۴٫۵", "+۰٫۲", "up", "amber"),
        Kpi("users", "پاسخ‌دهندگان", "۳۱۸", "+۴۴", "up", "teal"),
        Kpi("message-square", "نظرات متنی", "۹۲", None, "flat", "blue")],
  tabs=[("پرسش‌ها", "list-checks", None), ("پاسخ‌ها", "message-square", None),
        ("تحلیل", "chart-bar", None), ("تنظیمات", "settings", None)],
  actions=[Act("New", "پرسش جدید", "p", "plus"), Act("Export", "خروجی", "s", "file-spreadsheet")],
  rel=[("نظرسنجی", "پیامک", "ارسال لینک نظرسنجی"),
       ("نظرسنجی", "عملکرد پزشکان", "نمره رضایت")])

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  📊 گروه ۱۴ — گزارشات                                                ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("ReportsView", "گزارشات", "chart-column", "settings",
  "کتابخانه گزارش‌های آماده", "گزارشات",
  toolbar=[Tb("search", "جستجو", "w-xl", "عنوان گزارش…"),
           Tb("combo", "دسته", "w-md", "همه", opts=["همه", "مالی", "درمانی", "عملیاتی", "پرسنلی"])],
  actions=[Act("Run", "اجرای گزارش", "p", "play"), Act("Schedule", "زمان‌بندی", "s", "clock")],
  rel=[("گزارشات", "مرورگر گزارشات", "مشاهده خروجی"),
       ("گزارشات", "گزارش‌ساز پویا", "ساخت گزارش جدید")],
  notes="کتابخانه با دسته‌بندیِ سمت راست؛ هر گزارش کارت خود را با آیکون، عنوان، توضیح و زمان آخرین اجرا دارد.")

F("ReportBrowserView", "مرورگر گزارشات", "file-search", "report",
  "مشاهده و خروجی گرفتن از گزارش‌ها", "گزارشات",
  toolbar=[Tb("search", "جستجو", "w-xl", "عنوان یا توضیح…"),
           Tb("combo", "دسته", "w-md", "همه"),
           Tb("date", "از تاریخ", "w-md", "۱۴۰۴/۰۷/۰۱")],
  actions=[Act("ExportPdf", "خروجی PDF", "p", "file-text"),
           Act("ExportExcel", "خروجی Excel", "s", "file-spreadsheet"),
           Act("Print", "چاپ", "s", "printer")],
  rel=[("مرورگر گزارشات", "گزارشات", "انتخاب از کتابخانه"),
       ("مرورگر گزارشات", "گزارش‌ساز پویا", "ساخت گزارش سفارشی")],
  notes="پنلِ راست: فیلترها و پارامترهای گزارش. پنلِ چپ: پیش‌نمایشِ کاغذیِ گزارش با هدر و صفحه‌بندی.")

F("AdhocReportWindow", "گزارش‌ساز پویا", "wand", "report",
  "ساخت گزارش با انتخاب موجودیت و ستون‌ها", "گزارشات",
  steps=["انتخاب موجودیت", "ستون‌ها", "فیلترها", "مرتب‌سازی", "پیش‌نمایش"],
  sections=[Sec("تنظیمات گزارش", "wand", [
      Fd("Entity", "combo", "موجودیت", 4, True, "database", "ویزیت‌ها",
         opts=["بیماران", "ویزیت‌ها", "پذیرش‌ها", "فاکتورها", "پرسنل", "اقلام دارویی"]),
      Fd("Columns", "combo", "ستون‌ها", 4, True, "columns-3", "تاریخ، پزشک، مبلغ"),
      Fd("GroupBy", "combo", "گروه‌بندی", 4, False, "group", "پزشک"),
      Fd("SortBy", "combo", "مرتب‌سازی", 4, False, "arrow-down-up", "تاریخ (نزولی)"),
      Fd("FromDate", "date", "از تاریخ", 4, False, "calendar-days", "۱۴۰۴/۰۷/۰۱"),
      Fd("ToDate", "date", "تا تاریخ", 4, False, "calendar-days", "۱۴۰۴/۰۷/۱۹"),
  ])],
  actions=[Act("Preview", "پیش‌نمایش", "p", "eye"), Act("Save", "ذخیره گزارش", "s", "save"),
           Act("Export", "خروجی", "s", "file-spreadsheet")],
  rel=[("گزارش‌ساز پویا", "مرورگر گزارشات", "ذخیره در کتابخانه")],
  notes="پنج مرحله با نوار پیشرفت در بالا؛ پیش‌نمایشِ زنده در پایین با به‌روزرسانیِ خودکار هنگام تغییر تنظیمات.")

F("AuditLogView", "گزارش حسابرسی", "history", "list-dense",
  "ردپای کامل عملیات کاربران", "گزارشات",
  kpis=[Kpi("history", "رویداد امروز", "۱,۲۴۸", None, "flat", "teal"),
        Kpi("pencil", "ویرایش‌ها", "۳۴۲", None, "flat", "blue"),
        Kpi("trash-2", "حذف‌ها", "۷", None, "flat", "rose"),
        Kpi("log-in", "ورودها", "۴۶", None, "flat", "green")],
  toolbar=[Tb("search", "جستجو", "w-xl", "کاربر، موجودیت یا توضیح…"),
           Tb("combo", "عملیات", "w-md", "همه", opts=["همه", "ایجاد", "ویرایش", "حذف", "ورود", "خروج"]),
           Tb("date", "از تاریخ", "w-md", "۱۴۰۴/۰۷/۱۹")],
  cols=[Col("Time", "زمان", "150px", "r", "date", "clock"),
        Col("User", "کاربر", "160px", "r", "who", "user"),
        Col("Action", "عملیات", "96px", "c", "chip"),
        Col("Entity", "موجودیت", "160px", "r", "text", "database"),
        Col("Desc", "توضیح", None, "r", "text"),
        Col("Ip", "IP", "120px", "l", "num")],
  rows=[
      [dtx("۱۴۰۴/۰۷/۱۹ ۰۹:۴۲:۱۱"), who("mnouri", "مریم نوری", "من", "b2"), chip("ایجاد", "ok"), "Reception", "پذیرش جدید — سارا احمدی", num("۱۹۲٫۱۶۸٫۱٫۲۴")],
      [dtx("۱۴۰۴/۰۷/۱۹ ۰۹:۳۸:۰۵"), who("admin", "سعید نجفی", "عا", ""), chip("ویرایش", "in"), "Tariff", "تغییر تعرفه ویزیت عمومی", num("۱۹۲٫۱۶۸٫۱٫۱۰")],
      [dtx("۱۴۰۴/۰۷/۱۹ ۰۹:۳۰:۵۲"), who("lhasani", "لیلا حسنی", "لح", "b4"), chip("حذف", "dg"), "Invoice", "حذف فاکتور INV-۹۵۰۱", num("۱۹۲٫۱۶۸٫۱٫۱۸")],
      [dtx("۱۴۰۴/۰۷/۱۹ ۰۸:۱۲:۰۰"), who("admin", "سعید نجفی", "عا", ""), chip("ورود", "cy"), "Auth", "ورود موفق", num("۱۹۲٫۱۶۸٫۱٫۱۰")],
  ],
  actions=[Act("Export", "خروجی", "s", "file-spreadsheet"), Act("Filter", "فیلتر پیشرفته", "s", "list-filter")],
  rel=[("گزارش حسابرسی", "کاربران", "عامل عملیات"),
       ("گزارش حسابرسی", "مدیریت سیستم", "بخشی از مدیریت")])

# ╔═════════════════════════════════════════════════════════════════════╗
# ║  ⚙️ گروه ۱۵ — سیستم                                                  ║
# ╚═════════════════════════════════════════════════════════════════════╝

F("BackupView", "پشتیبان‌گیری", "database-backup", "detail-tabs",
  "تهیه و بازیابی نسخه پشتیبان", "سیستم",
  kpis=[Kpi("database-backup", "آخرین پشتیبان", "امروز ۰۲:۰۰", None, "flat", "green"),
        Kpi("hard-drive", "حجم کل", "۴٫۲ گیگابایت", None, "flat", "blue"),
        Kpi("history", "تعداد نسخه‌ها", "۳۰", None, "flat", "teal")],
  tabs=[("نسخه‌ها", "hard-drive", None), ("زمان‌بندی", "clock", None),
        ("بازیابی", "rotate-ccw", None), ("تنظیمات", "settings", None)],
  sections=[Sec("زمان‌بندی خودکار", "clock", [
      Fd("AutoEnabled", "check", "پشتیبان‌گیری خودکار", 12, False, None, True),
      Fd("Schedule", "combo", "دوره", 4, False, "clock", "روزانه", opts=["ساعتی", "روزانه", "هفتگی", "ماهانه"]),
      Fd("Time", "time", "ساعت اجرا", 4, False, "clock", "۰۲:۰۰"),
      Fd("Retention", "number", "نگهداری (روز)", 4, False, "archive", "۳۰"),
      Fd("Path", "text", "مسیر ذخیره", 8, False, "folder", "D:\\Backups\\ClinicPro"),
      Fd("Compress", "check", "فشرده‌سازی", 4, False, None, True),
  ])],
  actions=[Act("BackupNow", "پشتیبان‌گیری الآن", "p", "database-backup"),
           Act("Restore", "بازیابی", "d", "rotate-ccw")],
  rel=[("پشتیبان‌گیری", "تنظیمات", "تنظیمات بکاپ در تنظیمات"),
       ("پشتیبان‌گیری", "مدیریت سیستم", "ابزار مدیریتی")])

F("LicenseStatusView", "وضعیت لایسنس", "shield-check", "detail-tabs",
  "وضعیت مجوز و ماژول‌های فعال", "سیستم",
  kpis=[Kpi("shield-check", "وضعیت", "فعال", None, "flat", "green"),
        Kpi("calendar-clock", "روز باقیمانده", "۳۴۲", None, "flat", "teal"),
        Kpi("boxes", "ماژول‌های فعال", "۲۷ از ۳۹", None, "flat", "blue"),
        Kpi("users", "کاربران مجاز", "۱۲", None, "flat", "amber")],
  tabs=[("نمای کلی", "layout-dashboard", None), ("ماژول‌ها", "boxes", None),
        ("تاریخچه", "history", None), ("پشتیبانی", "life-buoy", None)],
  actions=[Act("Refresh", "به‌روزرسانی وضعیت", "p", "refresh-cw"),
           Act("Renew", "تمدید", "ok", "rotate-ccw"),
           Act("Activate", "فعال‌سازی", "s", "key-round")],
  rel=[("وضعیت لایسنس", "فعال‌سازی لایسنس", "ورود کلید مجوز"),
       ("وضعیت لایسنس", "تنظیمات", "تنظیمات پنل لایسنس")],
  notes="کارت وضعیت با نوار پیشرفتِ روزهای باقیمانده؛ جدول ماژول‌ها با چیپ‌های سبز/خاکستری.")

F("LicenseActivationWindow", "فعال‌سازی لایسنس", "key-round", "form-narrow",
  "ورود کلید مجوز یا بارگذاری فایل درخواست", "سیستم",
  sections=[Sec("فعال‌سازی", "key-round", [
      Fd("LicenseKey", "text", "کلید مجوز", 12, True, "key-round", "SAHD-XXXX-XXXX-XXXX"),
      Fd("ClinicName", "text", "نام درمانگاه", 12, False, "building-2", "درمانگاه سهند"),
      Fd("PortalUrl", "text", "نشانی پنل", 12, False, "link-2", "https://clinic-license.ea-fixer.ir/api.php"),
      Fd("OfflineFile", "file", "فایل درخواست آفلاین", 12, False, "file", ""),
  ])],
  actions=[Act("Activate", "فعال‌سازی", "p", "check"), Act("RequestFile", "دریافت فایل .req", "s", "download"),
           Act("Cancel", "انصراف", "g")],
  rel=[("فعال‌سازی لایسنس", "وضعیت لایسنس", "پس از فعال‌سازی")])

F("ColorPickerWindow", "انتخاب رنگ", "palette", "dialog",
  "انتخاب‌گر رنگ با پیش‌نمایش", "سیستم",
  notes="شبکهٔ رنگ‌های آماده + انتخاب‌گر سفارشی با نوار فام، مربع رنگ و لغزندهٔ روشنایی؛ کد هگز زیر آن.")

F("ThemedDialog", "دیالوگ یکپارچه", "message-square", "dialog",
  "قالبِ یکپارچهٔ پیام‌های برنامه", "سیستم",
  notes="چهار گونه: تأیید · هشدار · خطا · پرسش. هر کدام آیکون، رنگِ متناسب و دکمه‌های استانداردِ فارسی — بدون کادرِ سیستم‌عامل.")
