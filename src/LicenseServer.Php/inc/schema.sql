-- ─────────────────────────────────────────────────────────────
-- ساختار جداول سرور لایسنس Sahand ClinicPro — MySQL 5.7+ / MariaDB 10.3+
-- utf8mb4 برای پشتیبانی کامل فارسی
-- ─────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS ls_admins (
    id            INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    username      VARCHAR(50)  NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name     VARCHAR(100) NOT NULL,
    is_active     TINYINT(1)   NOT NULL DEFAULT 1,
    failed_attempts INT        NOT NULL DEFAULT 0,
    locked_until  DATETIME     NULL,
    last_login_at DATETIME     NULL,
    created_at    DATETIME     NOT NULL,
    INDEX idx_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ls_licenses (
    id                  BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    license_key         VARCHAR(24) NOT NULL UNIQUE,
    license_type        ENUM('subscription','perpetual') NOT NULL DEFAULT 'subscription',
    clinic_name         VARCHAR(200) NULL,
    clinic_phone        VARCHAR(30)  NULL,
    clinic_manager      VARCHAR(120) NULL,
    clinic_email        VARCHAR(160) NULL,
    clinic_city         VARCHAR(100) NULL,
    clinic_address      VARCHAR(300) NULL,
    -- چرخهٔ بیست‌وهفتم — تکمیل فیلدهای سرور لایسنس: کد اقتصادی + شناسه ملی
    economic_code       VARCHAR(50)  NULL,
    national_id         VARCHAR(20)  NULL,
    max_users           INT NOT NULL DEFAULT 5,
    max_doctors         INT NOT NULL DEFAULT 10,
    max_patients        INT NOT NULL DEFAULT 5000,
    max_branches        INT NOT NULL DEFAULT 1,
    enabled_modules     TEXT NOT NULL,
    issued_date         DATE NOT NULL,
    expiry_date         DATE NULL,
    hardware_fingerprint VARCHAR(150) NULL,
    activation_code     VARCHAR(40)  NULL,
    is_revoked          TINYINT(1) NOT NULL DEFAULT 0,
    revoked_at          DATETIME NULL,
    revoked_reason      VARCHAR(255) NULL,
    last_heartbeat_at   DATETIME NULL,
    last_validated_at   DATETIME NULL,
    activation_count    INT NOT NULL DEFAULT 0,
    -- چرخهٔ ۸۰ — امضای RSA لایسنس (license_key|expiry_date|max_users).
    -- چرخهٔ ۶۶ «امضای دوبارهٔ همهٔ لایسنس‌ها پس از چرخش کلید» را روی این ستون
    -- نوشته بود ولی ستون در اسکیما هرگز ساخته نشده بود ⇒ هر بار چرخش کلید با
    -- SQLSTATE[42S22] Unknown column 'signature' می‌شکست و لایسنس‌ها
    -- بازامضا نمی‌شدند. TEXT انتخاب شد تا به سقف اندازهٔ ردیف InnoDB نخوریم.
    signature           TEXT NULL,
    notes               TEXT NULL,
    created_at          DATETIME NOT NULL,
    updated_at          DATETIME NOT NULL,
    INDEX idx_expiry (expiry_date),
    INDEX idx_fingerprint (hardware_fingerprint(60)),
    INDEX idx_status (is_revoked)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── پیام‌های مستقیم به داشبورد درمانگاه (چرخه دوازدهم) ───
-- با هر تپ‌قلب کلاینت، پیام‌های تحویل‌نشده ارسال و «تحویل‌شده» علامت می‌خورند
CREATE TABLE IF NOT EXISTS ls_messages (
    id              BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    license_id      BIGINT UNSIGNED NOT NULL,
    title           VARCHAR(200) NOT NULL,
    body            TEXT NOT NULL,
    level           ENUM('info','success','warning','danger') NOT NULL DEFAULT 'info',
    -- ═══ چرخهٔ ۶۷ — تکمیل زنجیرهٔ رنگ پیام (چرخهٔ ۳۵ سمت کلاینت آماده بود؛ ستون در پنل نبود) ═══
    color           VARCHAR(20) NULL,
    created_by      INT UNSIGNED NULL,
    delivered_at    DATETIME NULL,
    read_at         DATETIME NULL,
    created_at      DATETIME NOT NULL,
    FOREIGN KEY (license_id) REFERENCES ls_licenses(id) ON DELETE CASCADE,
    INDEX idx_pending (license_id, delivered_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ls_activations (
    id                  BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    license_id          BIGINT UNSIGNED NOT NULL,
    hardware_fingerprint VARCHAR(150) NOT NULL,
    clinic_name         VARCHAR(200) NULL,
    app_version         VARCHAR(20)  NULL,
    ip_address          VARCHAR(45)  NULL,
    activated_at        DATETIME NOT NULL,
    last_seen_at        DATETIME NULL,
    is_active           TINYINT(1) NOT NULL DEFAULT 1,
    UNIQUE KEY uq_license_fp (license_id, hardware_fingerprint(60)),
    FOREIGN KEY (license_id) REFERENCES ls_licenses(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ls_check_logs (
    id          BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    license_id  BIGINT UNSIGNED NULL,
    license_key VARCHAR(24)  NULL,
    result      VARCHAR(32)  NOT NULL,
    source      VARCHAR(10)  NOT NULL DEFAULT 'online',
    message     VARCHAR(500) NULL,
    ip_address  VARCHAR(45)  NULL,
    created_at  DATETIME NOT NULL,
    FOREIGN KEY (license_id) REFERENCES ls_licenses(id) ON DELETE SET NULL,
    INDEX idx_created (created_at),
    INDEX idx_result (result)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ls_rate_limits (
    id           BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    bucket       CHAR(64) NOT NULL UNIQUE,
    hits         INT NOT NULL DEFAULT 0,
    window_start INT  NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ls_settings (
    skey   VARCHAR(60) NOT NULL PRIMARY KEY,
    svalue TEXT NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ls_admin_audit (
    id         BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    admin_id   INT UNSIGNED NULL,
    action     VARCHAR(60)  NOT NULL,
    entity     VARCHAR(40)  NULL,
    entity_id  BIGINT       NULL,
    details    VARCHAR(500) NULL,
    ip_address VARCHAR(45)  NULL,
    created_at DATETIME NOT NULL,
    INDEX idx_created (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
