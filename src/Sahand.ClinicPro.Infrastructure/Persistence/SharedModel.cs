using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// مدل مشترک دو کانتکست (اصلی و گزارش) — چرخهٔ ۶۴ (A-01).
/// </summary>
/// <remarks>
/// پیش از این چرخه کل مدل داخل OnModelCreatingِ ClinicProDbContext بود؛
/// با تقسیم کانتکست (P-04/A-01)، این بدنه به متد مشترک منتقل شد تا شکل
/// مدلِ «خواندنِ گزارش‌ها» و «نوشتنِ عملیاتی» هرگز از هم نپرد — تمام
/// روابط چندناوبری، سکانس‌ها، فیلتر سراسری حذف نرم/شعبه، ایندکس‌های
/// فیلترشده و رفتار حذف NoAction یک‌جا تعریف و از هر دو کانتکست صدا
/// زده می‌شوند. هر تغییری از این پس خودکار روی هر دو اعمال می‌شود.
/// پارامترها به‌جای فیلدهای نمونهٔ کانتکست اصلی می‌آیند تا کانتکست
/// گزارش هم همان رفتار را با وضعیت خودش بگیرد (IsInMemory و CurrentBranchId).
/// </remarks>
public static class SharedModel
{
    /// <summary>
    /// ساخت کامل مدل — فراخوانی از OnModelCreating هر دو کانتکست.
    /// </summary>
    /// <param name="modelBuilder">سازندهٔ مدل EF</param>
    /// <param name="isInMemory">آیا موتور درون‌حافظه‌ای است (تست)؟ سکانس‌ها و پیش‌فرض‌های SQL خاموش می‌شوند</param>
    /// <param name="context">نمونهٔ کانتکست جاری — برای فیلتر شعبه (CurrentBranchId باید خاصیت عمومی باشد)</param>
    public static void Build(ModelBuilder modelBuilder, bool isInMemory, DbContext context)
    {
        // در حالت درون‌حافظه‌ای — سکانس SQL کار نمی‌کند؛ مولد سمت کاربر برای ستون الزامی
        if (isInMemory)
        {
            var mrn = modelBuilder.Entity<Patient>()
                .Property(p => p.MedicalRecordNo);
            mrn.HasDefaultValueSql(null);
            mrn.HasValueGenerator<MrnInMemoryValueGenerator>();
            mrn.Metadata.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd;
        }

        // روابط صریح موارد چند‌ناوبری — بیمار دو بیمه و پزشک دو تخصص دارد
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.InsuranceCompany)
            .WithMany(i => i.Patients)
            .HasForeignKey(p => p.InsuranceCompanyId);
        modelBuilder.Entity<Patient>()
            .HasOne(p => p.SupplementaryInsurance)
            .WithMany()
            .HasForeignKey(p => p.SupplementaryInsuranceId);
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.Specialty)
            .WithMany(s => s.Doctors)
            .HasForeignKey(d => d.SpecialtyId);
        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.SubSpecialty)
            .WithMany()
            .HasForeignKey(d => d.SubSpecialtyId);

        // سکانس‌های شماره یکتا — ۱۵ سکانس کسب‌وکاری با شروع از ۱۰۰۰۰۱
        modelBuilder.HasSequence<long>("SeqPatientMrn").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqAppointmentCode").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqReceptionNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqVisitCode").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPrescriptionCode").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqInvoiceNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPaymentNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqJournalEntryNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqLabOrderNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqImagingOrderNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPharmacyTransactionNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqClaimNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqExpenseNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqContractNumber").StartsAt(100001).IncrementsBy(1);
        modelBuilder.HasSequence<long>("SeqPurchaseOrderNumber").StartsAt(100001).IncrementsBy(1);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // فیلتر سراسری حذف نرم + شعبه — گام U17:
            // رکوردهای حذف‌شده پنهان می‌شوند و رکوردهای شعبه‌دار فقط وقتی شعبه جاری
            // تنظیم شده باشد نمایش داده می‌شوند (رکوردهای بدون شعبه همیشه عمومی‌اند)
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                Expression body = Expression.Not(
                    Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));

                var branchProp = clrType.GetProperty(nameof(AuditableEntity.BranchId));
                if (branchProp is not null)
                {
                    // e => !e.IsDeleted && (e.BranchId == null || e.BranchId == ctx.CurrentBranchId)
                    var ctx = Expression.Constant(context);
                    var currentBranch = Expression.Property(ctx, "CurrentBranchId");
                    body = Expression.AndAlso(body, Expression.OrElse(
                        Expression.Equal(Expression.Property(parameter, branchProp), Expression.Constant(null, branchProp.PropertyType)),
                        Expression.Equal(Expression.Property(parameter, branchProp), currentBranch)));
                }

                var filter = Expression.Lambda(body, parameter);
                modelBuilder.Entity(clrType).HasQueryFilter(filter);
            }

            // رفتار حذف همه کلیدهای خارجی NoAction — الزام حذف نرم
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
            }

            // RowVersion به‌عنوان نشانگر همزمانی خوش‌بینانه
            var rowVersion = entityType.FindProperty(nameof(AuditableEntity.RowVersion));
            if (rowVersion is not null)
            {
                modelBuilder.Entity(clrType).Property(nameof(AuditableEntity.RowVersion)).IsRowVersion();
            }
        }

        // ═══ چرخه سی‌ویکم (گزارش مالک) — رفع سه هشدار EF Core «required end of a
        // relationship with query filter» ═══
        // موجودیت‌های وابستهٔ امنیتی (UserRole / RolePermission / RefreshToken) خودشان
        // ISoftDeletable نیستند ولی کلید خارجیِ «الزامی» به موجودیت‌های فیلتردار
        // (User / Role / Permission) دارند؛ EF هشدار می‌داد که ممکن است ردیفِ وابسته به
        // اصلیِ حذف‌شدهٔ نرم، در پرس‌وجوها «یتیم» دیده شود. راه‌حل استانداردِ مستند
        // (go.microsoft.com/fwlink/?linkid=2131316): تعریف فیلتر منطبق برای وابسته —
        // سطرهایی که اصلیِ آن‌ها حذف نرم شده از پرس‌وجوهای مستقیم پنهان می‌شوند.
        // نکتهٔ مهم: فیلترهای پرس‌وجو روی «بارگذاری از مسیر ناوبری» (مثل user.Roles
        // در SyncRolesAsync) اعمال نمی‌شوند؛ بنابراین جریان‌های تغییر نقش دست‌نخورده‌اند.
        modelBuilder.Entity<UserRole>()
            .HasQueryFilter(ur => !ur.User!.IsDeleted && !ur.Role!.IsDeleted);
        modelBuilder.Entity<RolePermission>()
            .HasQueryFilter(rp => !rp.Role!.IsDeleted && !rp.Permission!.IsDeleted);
        modelBuilder.Entity<RefreshToken>()
            .HasQueryFilter(rt => !rt.User!.IsDeleted);

        // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — رفع خطای ۲۶۰۱ اس‌کیوال‌سرور ═══
        // در اس‌کیوال‌سرور ایندکس یکتا روی ستون تهی‌پذیر فقط «یک» رکورد NULL را
        // مجاز می‌شمارد؛ ثبت دومین کاربر/پرسنلِ بدون کد پرسنلی با خطای
        // «Cannot insert duplicate key row … duplicate key value is (<NULL>)»
        // (شمارهٔ ۲۶۰۱) شکست می‌خورد و کل تراکنش (شامل نقش‌ها و دسترسی‌ها)
        // لغو می‌شود. راه‌حل استاندارد: ایندکس فیلترشده که سطرهای NULL را از
        // دامنهٔ یکتایی خارج می‌کند. این حلقهٔ سراسری روی «همهٔ» موجودیت‌ها
        // اعمال می‌شود تا هر ایندکس یکتای تک‌ستونیِ تهی‌پذیر (PersonnelCode،
        // NationalCode، NezamPezeshkiCode، IrcCode، ReferralCode و…) به‌صورت
        // خودکار شرط «IS NOT NULL» بگیرد؛ فیلترهای موجود (حذف نرم) نیز حفظ
        // می‌شوند. از این پس افزودن موجودیت تازه با ستون یکتای اختیاری، بدون
        // نیاز به پیکربندی دستی، از این بیمهٔ یکتایی برخوردار خواهد بود.
        if (!isInMemory)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (tableName is null)
                    continue; // موجودیت بدون جدول (keyless/owned) — بی‌ربط

                var tableId = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());

                foreach (var index in entityType.GetIndexes())
                {
                    if (index is not IMutableIndex mutableIndex)
                        continue;

                    // فقط ایندکس‌های «یکتا» و «تک‌ستونی» با ستون تهی‌پذیر هدف‌اند؛
                    // ایندکس‌های ترکیبی و ستون‌های الزامی یکتایی طبیعی سالم دارند
                    if (!index.IsUnique || index.Properties.Count != 1)
                        continue;

                    var prop = index.Properties[0];
                    if (!prop.IsNullable)
                        continue;

                    var column = prop.GetColumnName(tableId);
                    if (string.IsNullOrEmpty(column))
                        continue;

                    // شرط خروج سطرهای NULL + حفظ فیلتر حذف نرم موجود؛
                    // اگر فیلتر فعلی از قبل همین شرط را دارد (مثل ایندکس‌های
                    // کلید خارجی اختیاری) از تکرار بی‌مورد پرهیز می‌شود
                    var notNullCondition = $"[{column}] IS NOT NULL";
                    var existingFilter = index.GetFilter();
                    if (existingFilter?.Contains(notNullCondition, StringComparison.OrdinalIgnoreCase) == true)
                        continue;
                    mutableIndex.SetFilter(string.IsNullOrEmpty(existingFilter)
                        ? notNullCondition
                        : $"{notNullCondition} AND {existingFilter}");
                }
            }
        }
    }
}
