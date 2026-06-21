using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Domain.Entities;

namespace Project.DAL.Configurations
{
    public class TimetableConfiguration : IEntityTypeConfiguration<Timetable>
    {
        public void Configure(EntityTypeBuilder<Timetable> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Class)
                .WithMany()
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AcademicYear)
                .WithMany()
                .HasForeignKey(x => x.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Data-integrity guarantees (defense in depth) ──────────────────
            // تطبّق هذه القيود على مستوى قاعدة البيانات لضمان عدم كسرها تحت أي
            // ظرف تزامن (race conditions)، حتى لو فشل منطق الـ application في التحقق.

            // 1) مسودة واحدة فقط لكل (فصل، سنة) في نفس الوقت.
            //    يحمي من TOCTOU في CreateTimetableAsync / CloneDraftTimetableAsync.
            builder.HasIndex(x => new { x.ClassId, x.AcademicYearId })
                .IsUnique()
                .HasDatabaseName("UX_Timetable_Draft")
                .HasFilter("[IsActive] = 0 AND [IsDeleted] = 0");

            // 2) جدول منشور واحد فقط لكل (فصل، سنة) في نفس الوقت.
            //    يحمي من وجود جدولين نشطين لنفس الفصل بعد تفعيل متزامن.
            builder.HasIndex(x => new { x.ClassId, x.AcademicYearId })
                .IsUnique()
                .HasDatabaseName("UX_Timetable_Active")
                .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
