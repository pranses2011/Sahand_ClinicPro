# -*- coding: utf-8 -*-
"""
کتابخانه کمکی مولد لایه دامنه — گام ۲
تولید فایل‌های C# برای پروژه Sahand.ClinicPro.Domain
قواعد: کد انگلیسی / کامنت XML فارسی / بدون -- در کامنت‌ها
"""
import os

DOMAIN = os.path.join(os.path.dirname(__file__), "..", "src", "Sahand.ClinicPro.Domain")
DOMAIN = os.path.abspath(DOMAIN)


def write(rel_path: str, content: str):
    """نوشتن فایل با BOM (سازگاری فارسی با ویندوز)"""
    full = os.path.join(DOMAIN, rel_path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel_path}")


def header(namespace: str) -> str:
    return f"namespace Sahand.ClinicPro.Domain.{namespace};\n"


class P:
    """یک پراپرتی عادی"""
    def __init__(self, name, ctype, comment, default=None):
        self.name, self.ctype, self.comment, self.default = name, ctype, comment, default


class N:
    """پراپرتی ناوبری (تکی)"""
    def __init__(self, name, ntype, comment):
        self.name, self.ntype, self.comment = name, ntype, comment


class C:
    """پراپرتی ناوبری مجموعه‌ای"""
    def __init__(self, name, itype, comment):
        self.name, self.itype, self.comment = name, itype, comment


class E:
    """یک Entity کامل"""
    def __init__(self, name, folder, table, summary, props, inherits="AuditableEntity",
                 interfaces=("ISoftDeletable",), remarks=None, extra_usings=()):
        self.name, self.folder, self.table = name, folder, table
        self.summary, self.props, self.inherits = summary, props, inherits
        self.interfaces, self.remarks = list(interfaces), remarks
        self.extra_usings = list(extra_usings)

    def render(self):
        lines = []
        lines.append("/// <summary>")
        lines.append(f"/// {self.summary}")
        lines.append("/// </summary>")
        if self.remarks:
            lines.append("/// <remarks>")
            for r in self.remarks:
                lines.append(f"/// {r}")
            lines.append("/// </remarks>")
        base = self.inherits
        sig = f"public class {self.name} : {base}"
        if self.interfaces:
            sig += ", " + ", ".join(self.interfaces)
        lines.append(sig)
        lines.append("{")

        body = []
        for p in self.props:
            if isinstance(p, N):
                body.append(f"    /// <summary>{p.comment}</summary>")
                body.append(f"    public {p.ntype} {p.name} {{ get; set; }}")
            elif isinstance(p, C):
                body.append(f"    /// <summary>{p.comment}</summary>")
                body.append(f"    public ICollection<{p.itype}> {p.name} {{ get; set; }} = new List<{p.itype}>();")
            else:
                body.append(f"    /// <summary>{p.comment}</summary>")
                t = p.ctype
                if t == "string":
                    body.append(f"    public string {p.name} {{ get; set; }} = null!;")
                elif t == "string?":
                    body.append(f"    public string? {p.name} {{ get; set; }}")
                elif t == "byte[]":
                    body.append(f"    public byte[] {p.name} {{ get; set; }} = null!;")
                elif t == "byte[]?":
                    body.append(f"    public byte[]? {p.name} {{ get; set; }}")
                else:
                    init = ""
                    if p.default is not None:
                        init = f" = {p.default};"
                    body.append(f"    public {t} {p.name} {{ get; set; }}{init}")
            body.append("")

        # حذف خط خالی انتهایی اضافی
        while body and body[-1] == "":
            body.pop()
        lines.extend(body)
        lines.append("}")
        return "\n".join(lines)


def entity_file(e: E, ns_folder: str):
    """ساخت فایل کامل Entity"""
    ns = f"Entities.{ns_folder}"
    usings = set(e.extra_usings)
    # نیاز به System.Collections.Generic برای ICollection
    txt = ""
    if usings:
        txt += "\n".join(sorted(usings)) + "\n\n"
    txt += f"namespace Sahand.ClinicPro.Domain.{ns};\n\n"
    txt += e.render()
    write(f"{ns_folder}/{e.name}.cs", txt)


def enum_file(name, summary, members, ns_folder="Enums", underlying="byte"):
    """ساخت فایل enum — members: لیست (value, نام, کامنت فارسی)"""
    txt = f"namespace Sahand.ClinicPro.Domain.Enums;\n\n"
    txt += "/// <summary>\n"
    txt += f"/// {summary}\n"
    txt += "/// </summary>\n"
    txt += f"public enum {name} : {underlying}\n{{\n"
    for val, mname, comment in members:
        txt += f"    /// <summary>{comment}</summary>\n"
        txt += f"    {mname} = {val},\n"
    txt += "}"
    write(f"Enums/{name}.cs" if ns_folder == "Enums" else f"{ns_folder}/{name}.cs", txt)
