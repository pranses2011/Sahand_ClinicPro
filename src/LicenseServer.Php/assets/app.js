/* ═══════════════════════════════════════════════════════════
   پنل مدیریت سرور لایسنس Sahand ClinicPro — اسکریپت تعاملی
   چرخه دوازدهم: شمارنده عددی + حلقه پیشرفت + ترسیم خط SVG +
   موج لمس دکمه‌ها + خروجی CSV — همه با JS خالص، بدون وابستگی
   ═══════════════════════════════════════════════════════════ */
(function () {
    'use strict';

    /* ─────────  شمارنده عددی فارسی (count-up)  ───────── */
    function animateCounters() {
        var els = document.querySelectorAll('.count-up[data-target]');
        els.forEach(function (el) {
            var target = parseInt(el.getAttribute('data-target'), 10) || 0;
            if (target === 0) { el.textContent = '۰'; return; }
            var dur = 950;                 // میلی‌ثانیه
            var start = null;
            // قالب‌بندی فارسی
            var faDigits = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];
            function fa(n) {
                return String(n).replace(/\d/g, function (d) { return faDigits[+d]; });
            }
            function step(ts) {
                if (start === null) start = ts;
                var p = Math.min(1, (ts - start) / dur);
                var eased = 1 - Math.pow(1 - p, 3);   // easeOutCubic
                var val = Math.round(target * eased);
                el.textContent = fa(val.toLocaleString('en-US').replace(/,/g, '٬'));
                if (p < 1) requestAnimationFrame(step);
            }
            requestAnimationFrame(step);
        });
    }

    /* ─────────  حلقه پیشرفت SVG  ───────── */
    function animateRings() {
        var rings = document.querySelectorAll('.progress-ring[data-percent]');
        // محیط دایره r=52 → 2πr ≈ 327
        var circumference = 2 * Math.PI * 52;
        rings.forEach(function (ring) {
            var pct = parseFloat(ring.getAttribute('data-percent')) || 0;
            var fg = ring.querySelector('.pr-fg');
            if (!fg) return;
            var offset = circumference * (1 - Math.max(0, Math.min(100, pct)) / 100);
            // شروع از پر و انیمیت به مقدار واقعی
            requestAnimationFrame(function () {
                setTimeout(function () { fg.style.strokeDashoffset = String(offset); }, 120);
            });
        });
    }

    /* ─────────  ترسیم خط نمودار سطحی (stroke-dashoffset)  ───────── */
    function animateAreaLines() {
        var lines = document.querySelectorAll('.chart-area .area-line');
        lines.forEach(function (line) {
            try {
                var len = line.getTotalLength();
                if (!len || !isFinite(len)) return;
                line.style.setProperty('--dash', String(len + 4));
                line.setAttribute('stroke-dasharray', String(len + 4));
                // مقدار اولیه از CSS keyframes استفاده می‌کند؛ فقط متغیر تنظیم شد
            } catch (e) { /* قدیمی‌ها بدون انیمیشن، نمودار کامل دیده می‌شود */ }
        });
    }

    /* ─────────  موج لمس دکمه‌ها (ripple)  ───────── */
    function attachRipples() {
        document.querySelectorAll('.btn').forEach(function (btn) {
            btn.addEventListener('click', function (ev) {
                var rect = btn.getBoundingClientRect();
                var ink = document.createElement('span');
                ink.className = 'ripple-ink';
                var size = Math.max(rect.width, rect.height);
                ink.style.width = ink.style.height = size + 'px';
                ink.style.left = (ev.clientX - rect.left - size / 2) + 'px';
                ink.style.top = (ev.clientY - rect.top - size / 2) + 'px';
                btn.appendChild(ink);
                setTimeout(function () { ink.remove(); }, 650);
            });
        });
    }

    /* ─────────  خروجی CSV جدول (نمای کلی درمانگاه‌ها)  ───────── */
    // راه‌اندازی سراسری — از onclick فراخوانی می‌شود
    window.lsExportTableToCsv = function (clickedBtn) {
        // نخستین جدول «reveal» بالاتر از دکمه — جدول نمای کلی
        var table = null;
        var scope = clickedBtn.closest('.actions');
        if (scope) {
            var prev = scope.previousElementSibling;
            while (prev && !prev.tagName) prev = prev.previousElementSibling;
            if (prev && prev.classList && prev.classList.contains('reveal') && prev.tagName === 'TABLE') {
                table = prev;
            }
        }
        if (!table) table = document.querySelector('table.reveal');
        if (!table) { alert('جدولی برای خروجی یافت نشد.'); return; }

        var rows = [];
        table.querySelectorAll('tr').forEach(function (tr) {
            var cells = [];
            tr.querySelectorAll('th,td').forEach(function (td) {
                var txt = td.innerText.replace(/\s+/g, ' ').trim();
                cells.push('"' + txt.replace(/"/g, '""') + '"');
            });
            if (cells.length > 0) rows.push(cells.join(','));
        });
        var csv = '\uFEFF' + rows.join('\r\n');   // BOM برای اکسل فارسی
        var blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
        var a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = 'licenses-overview-' + new Date().toISOString().slice(0, 10) + '.csv';
        document.body.appendChild(a);
        a.click();
        setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 300);
    };

    /* ─────────  راه‌اندازی پس از بارگذاری DOM  ───────── */
    function init() {
        animateCounters();
        animateRings();
        animateAreaLines();
        attachRipples();
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
