#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""ممیزی انفجار دکارتی — چرخهٔ ۶۵ (P-04).
یافتن کوئری‌هایی که «موجودیتِ لیست» را با Includeِ مجموعه‌ای ماتریالیزه
می‌کنند (بدون Select) — تنها الگویی که در معرض انفجار دکارتی است.
کوئری‌های تک‌والد (FirstOrDefault) و کوئری‌های دارای پروجکشن (Select →
تجمیع سمت دیتابیس) از دامنهٔ ممیزی خارج‌اند.
اجرا: python3 scripts/audit_cartesian_queries.py  (خروجی غیر‌صفر یعنی نقض)
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

# الگوهای Include مجموعه‌ای — فهرست وقفی ناوبری‌های کالکشنِ کدبیس.
# دامنهٔ «هر واژهٔ متهی به s» رفتار کاذب می‌دهد (PrimaryDiagnosis ارجاع است نه
# کالکشن)؛ به همین دلیل فهرست صریح به‌کار می‌رود و با افزودن کالکشنِ جدید
# به دامنه، نامش همین‌جا افزوده می‌شود.
COLLECTION_NAVS = (
    'Items', 'Lines', 'Rows', 'Transactions', 'Schedules', 'Roles',
    'Permissions', 'Payments', 'Attachments', 'Documents', 'Batches',
    'WeeklySchedules', 'SalaryComponents', 'LabTests',
)
COLLECTION_INCLUDE = re.compile(
    r'(?:Include|ThenInclude)\(\s*[^)]*=>\s*\w+\!?(?:\.\w+)?\.(?:' + '|'.join(COLLECTION_NAVS) + r')\b')

QUERYABLE = re.compile(r'Queryable(?:Tracking|NoTracking)\s*\n(?P<chain>(?:(?!ToListAsync|FirstOrDefaultAsync).)*?)(?:ToListAsync|FirstOrDefaultAsync)', re.S)


def audit_file(path: Path):
    text = path.read_text(encoding='utf-8-sig')
    findings = []
    for m in re.finditer(
            r'Queryable(?:Tracking|NoTracking)\s*\n(?P<chain>(?:(?!ToListAsync|FirstOrDefaultAsync|AnyAsync|CountAsync).)*?)ToListAsync',
            text, re.S):
        chain = m.group('chain')
        if 'Include(' not in chain:
            continue
        if '.Select(' in chain:
            continue  # پروجکشن → تجمیع سمت دیتابیس — امن
        if COLLECTION_INCLUDE.search(chain):
            line = text[:m.start()].count('\n') + 1
            findings.append((path.relative_to(ROOT), line, chain.strip().splitlines()[0][:80]))
    return findings


def main() -> int:
    violations = []
    for path in (ROOT / 'src' / 'Sahand.ClinicPro.Application' / 'Features').rglob('*.cs'):
        violations.extend(audit_file(path))

    if violations:
        print('✗ کوئری‌های در معرض انفجار دکارتی (ماتریالیزهٔ موجودیت با Include مجموعه‌ای):')
        for path, line, head in violations:
            print(f'  {path}:{line}  →  {head}')
        print('الگوهای اصلاح: پروجکشن Select (تجمیع سمت دیتابیس) یا — پس از ارتقای EF — AsSplitQuery')
        return 1

    print('✓ هیچ کوئریِ ماتریالیزهٔ موجودیت با Include مجموعه‌ای یافت نشد (چرخهٔ ۶۵)')
    return 0


if __name__ == '__main__':
    sys.exit(main())
