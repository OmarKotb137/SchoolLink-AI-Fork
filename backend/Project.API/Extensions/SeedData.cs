using Microsoft.EntityFrameworkCore;
using Project.DAL.Context;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.API.Extensions;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Check if migrations are set up (__EFMigrationsHistory table exists)
        var hasMigrations = false;
        try { hasMigrations = (await ctx.Database.GetAppliedMigrationsAsync()).Any(); }
        catch { /* history table doesn't exist */ }

        if (hasMigrations)
        {
            var pending = await ctx.Database.GetPendingMigrationsAsync();
            if (pending.Any()) await ctx.Database.MigrateAsync();
        }
        else
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        if (await ctx.AcademicYears.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        // ── 1. AcademicYear ──
        var year = new AcademicYear
        {
            Name = "2025/2026", StartDate = new DateOnly(2025, 9, 21),
            EndDate = new DateOnly(2026, 6, 11), IsCurrent = true,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.AcademicYears.Add(year);

        // ── 2. GradeLevel ──
        var grade1 = new GradeLevel
        {
            Name = "الصف الأول الإعدادي", LevelOrder = 1, Stage = "إعدادي",
            CreatedAt = now, UpdatedAt = now
        };
        ctx.GradeLevels.Add(grade1);

        // ── 3. Subjects ──
        var subjects = new List<Subject>
        {
            new() { Name = "اللغة العربية", Code = "ARB", CreatedAt = now, UpdatedAt = now },
            new() { Name = "اللغة الإنجليزية", Code = "ENG", CreatedAt = now, UpdatedAt = now },
            new() { Name = "الرياضيات", Code = "MTH", CreatedAt = now, UpdatedAt = now },
            new() { Name = "العلوم", Code = "SCI", CreatedAt = now, UpdatedAt = now },
            new() { Name = "الدراسات الاجتماعية", Code = "SOC", CreatedAt = now, UpdatedAt = now },
            new() { Name = "التربية الإسلامية", Code = "ISL", CreatedAt = now, UpdatedAt = now },
            new() { Name = "الحاسب الآلي", Code = "CMP", CreatedAt = now, UpdatedAt = now },
        };
        ctx.Subjects.AddRange(subjects);
        await ctx.SaveChangesAsync();
        var subj = subjects.Where(s => s.Code != null).ToDictionary(s => s.Code!, s => s.Id);

        // ── 4. SchoolProfile ──
        ctx.SchoolProfiles.Add(new SchoolProfile
        {
            SchoolName = "مدرسة النيل الإعدادية بنين",
            Governorate = "محافظة الجيزة",
            Directorate = "مديرية التربية والتعليم بالجيزة",
            EducationalAdministration = "إدارة الجيزة التعليمية",
            Address = "شارع النيل - وسط الجيزة",
            Phone = "02-37654321",
            Email = "nile.prep@sch.ed.eg",
            ManagerName = "أ/ محمد عبد الرحمن",
            IsActive = true,
            CreatedAt = now, UpdatedAt = now
        });

        // ── 5. Users - Admin + Teachers ──
        var admin = new User
        {
            FullName = "Super Admin",
            Email = "admin@school.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = UserRole.Admin, IsActive = true,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.Users.Add(admin);
        await ctx.SaveChangesAsync();

        var teacherData = new (string name, string email, string code)[]
        {
            ("أحمد علي حسن",     "ahmed.ali@school.com", "ARB"),
            ("سارة محمود عبد الله","sara.mahmoud@school.com","ENG"),
            ("محمد كمال الدين",  "mohamed.kamal@school.com","MTH"),
            ("إيمان عبد الفتاح", "eman.abdelfattah@school.com","SCI"),
            ("خالد رشاد عثمان",  "khaled.reshad@school.com","SOC"),
            ("نادية جابر سليمان","nadia.gaber@school.com","ISL"),
            ("أيمن شعبان مصطفى", "ayman.shaban@school.com","CMP"),
        };
        var teachers = new List<User>();
        foreach (var (n, e, _) in teacherData)
        {
            var t = new User
            {
                FullName = n, Email = e,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Teacher@123"),
                Role = UserRole.Teacher, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            };
            ctx.Users.Add(t);
            teachers.Add(t);
        }
        await ctx.SaveChangesAsync();
        var teacherMap = new Dictionary<string, int>();
        for (int i = 0; i < teacherData.Length; i++)
            teacherMap[teacherData[i].code] = teachers[i].Id;

        // ── 6. Classes ──
        var class1 = new SchoolClass
        {
            GradeLevelId = grade1.Id, AcademicYearId = year.Id,
            Name = "1/1", CreatedAt = now, UpdatedAt = now
        };
        var class2 = new SchoolClass
        {
            GradeLevelId = grade1.Id, AcademicYearId = year.Id,
            Name = "1/2", CreatedAt = now, UpdatedAt = now
        };
        ctx.Classes.AddRange(class1, class2);
        await ctx.SaveChangesAsync();

        // ── 7. Students (60) ──
        var firstNames = new[] { "أحمد", "محمد", "علي", "عمر", "خالد", "يوسف", "محمود", "كريم",
            "حسن", "حسين", "إبراهيم", "عبد الله", "أمير", "بسام", "جمال", "حمزة", "سامي",
            "صالح", "عادل", "طارق", "زياد", "شادي", "نادر", "هاني", "وائل", "باهر", "رامي",
            "فادي", "مازن", "ياسر" };
        var middleNames = new[] { "عبد الرحمن", "سامح", "جابر", "إسماعيل", "فتحي", "أنور", "حمدي",
            "رفعت", "سمير", "جلال", "نبيل", "كامل", "رشاد", "بهجت", "نعيم", "وجيه", "قاسم",
            "مبشر", "رؤوف", "وديع", "بهاء", "سعيد", "لطفي", "نزيه", "هشام", "أكرم", "بطرس",
            "ثروت", "جورج", "حبيب", "داوود", "رزق", "زكريا", "شكري", "صبري", "ضياء", "طاهر",
            "ظافر", "عبد المجيد", "غريب", "فكري", "قدرى", "كرم", "ماجد", "ناصر", "هادي",
            "وحيد", "يسري", "زغلول", "عبد الحميد" };
        var lastNames  = new[] { "عبد العزيز", "السيد", "خليل", "مرسي", "فوزي", "نصر", "الشيخ",
            "شحاتة", "النجار", "هاشم", "الديب", "رمضان", "سلامة", "بسيوني", "عتريس", "أبو زيد",
            "الزهار", "عرفة", "جابر", "خضر", "زيتون", "طنطاوي", "عبده", "غانم", "قنديل",
            "كمال", "لقمة", "موسى", "نوح", "هندي" };
        var students = new List<Student>();
        var enrollments = new List<StudentEnrollment>();
        int seq = 0;
        foreach (var cls in new[] { class1, class2 })
        {
            for (int i = 0; i < 30; i++)
            {
                seq++;
                var st = new Student
                {
                    FullName = $"{firstNames[i % 30]} {middleNames[i % 50]} {lastNames[i]}",
                    Gender = Gender.Male,
                    BirthDate = new DateOnly(2012, 1 + (seq % 12), 1 + (seq % 28)),
                    IsActive = true,
                    CreatedAt = now, UpdatedAt = now
                };
                ctx.Students.Add(st);
                students.Add(st);
            }
        }
        await ctx.SaveChangesAsync();
        seq = 0;
        foreach (var cls in new[] { class1, class2 })
        {
            for (int i = 0; i < 30; i++)
            {
                var enr = new StudentEnrollment
                {
                    StudentId = students[seq].Id, ClassId = cls.Id, AcademicYearId = year.Id,
                    EnrolledAt = new DateOnly(2025, 9, 21),
                    CreatedAt = now, UpdatedAt = now
                };
                ctx.StudentEnrollments.Add(enr);
                enrollments.Add(enr);
                seq++;
            }
        }
        await ctx.SaveChangesAsync();

        // ── 8. ClassSubjectTeacher ──
        var cstList = new List<ClassSubjectTeacher>();
        foreach (var cls in new[] { class1, class2 })
        {
            foreach (var kv in teacherMap)
            {
                var cst = new ClassSubjectTeacher
                {
                    ClassId = cls.Id, SubjectId = subj[kv.Key],
                    TeacherId = kv.Value, AcademicYearId = year.Id,
                    CreatedAt = now, UpdatedAt = now
                };
                ctx.ClassSubjectTeachers.Add(cst);
                cstList.Add(cst);
            }
        }
        await ctx.SaveChangesAsync();

        // ── 9. Rooms ──
        var rooms = new List<Room>();
        for (int r = 1; r <= 6; r++)
        {
            var rm = new Room
            {
                Name = $"فصل {r}", Type = RoomType.Classroom, Capacity = 35,
                CreatedAt = now, UpdatedAt = now
            };
            ctx.Rooms.Add(rm);
            rooms.Add(rm);
        }
        await ctx.SaveChangesAsync();

        // ── 10. Timetable + Slots ──
        var days = new[] { SchoolDay.Sunday, SchoolDay.Monday, SchoolDay.Tuesday, SchoolDay.Wednesday, SchoolDay.Thursday };
        var periods = new (TimeOnly start, TimeOnly end)[]
        {
            (new TimeOnly(8,0), new TimeOnly(8,45)),
            (new TimeOnly(8,45), new TimeOnly(9,30)),
            (new TimeOnly(9,45), new TimeOnly(10,30)),
            (new TimeOnly(10,30), new TimeOnly(11,15)),
            (new TimeOnly(11,30), new TimeOnly(12,15)),
            (new TimeOnly(12,15), new TimeOnly(13,0)),
            (new TimeOnly(13,15), new TimeOnly(14,0)),
        };
        var subjectCodes = new[] { "ARB", "ENG", "MTH", "SCI", "SOC", "ISL", "CMP" };
        foreach (var cls in new[] { class1, class2 })
        {
            var cstMap = cstList.Where(c => c.ClassId == cls.Id).ToDictionary(c => c.SubjectId, c => c.Id);
            var tt = new Timetable
            {
                ClassId = cls.Id, AcademicYearId = year.Id, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            };
            ctx.Timetables.Add(tt);
            await ctx.SaveChangesAsync();

            int roomOffset = cls.Id == class1.Id ? 0 : 3;
            for (int d = 0; d < days.Length; d++)
            {
                for (int p = 0; p < periods.Length; p++)
                {
                    var slot = new TimetableSlot
                    {
                        TimetableId = tt.Id, DayOfWeek = days[d],
                        PeriodNumber = p + 1, StartTime = periods[p].start,
                        EndTime = periods[p].end,
                        ClassSubjectTeacherId = cstMap[subj[subjectCodes[(d + p) % 7]]],
                        RoomId = rooms[(d + roomOffset) % 6].Id,
                        CreatedAt = now, UpdatedAt = now
                    };
                    ctx.TimetableSlots.Add(slot);
                }
            }
        }
        await ctx.SaveChangesAsync();

        // ── 11. EvaluationPeriods — 12 weeks + months ──
        var weekNames = new[] {
            "الأسبوع الأول", "الأسبوع الثاني", "الأسبوع الثالث", "الأسبوع الرابع",
            "الأسبوع الخامس", "الأسبوع السادس", "الأسبوع السابع", "الأسبوع الثامن",
            "الأسبوع التاسع", "الأسبوع العاشر", "الأسبوع الحادي عشر", "الأسبوع الثاني عشر"
        };
        var monthNames = new[] { "فبراير", "مارس", "أبريل" };
        var periodsList = new List<EvaluationPeriod>();
        for (int w = 0; w < 12; w++)
        {
            var start = new DateOnly(2026, 2, 1).AddDays(w * 7);
            var p = new EvaluationPeriod
            {
                AcademicYearId = year.Id, Name = weekNames[w],
                PeriodType = PeriodType.Weekly, OrderNum = w + 1,
                StartDate = start, EndDate = start.AddDays(4),
                MonthName = monthNames[w < 4 ? 0 : w < 8 ? 1 : 2],
                CreatedAt = now, UpdatedAt = now
            };
            ctx.EvaluationPeriods.Add(p);
            periodsList.Add(p);
        }
        // Monthly periods too
        var feb = new EvaluationPeriod
        {
            AcademicYearId = year.Id, Name = "شهر فبراير",
            PeriodType = PeriodType.Monthly, OrderNum = 1,
            StartDate = new DateOnly(2026, 2, 1), EndDate = new DateOnly(2026, 2, 28),
            MonthName = "فبراير", CreatedAt = now, UpdatedAt = now
        };
        var mar = new EvaluationPeriod
        {
            AcademicYearId = year.Id, Name = "شهر مارس",
            PeriodType = PeriodType.Monthly, OrderNum = 2,
            StartDate = new DateOnly(2026, 3, 1), EndDate = new DateOnly(2026, 3, 31),
            MonthName = "مارس", CreatedAt = now, UpdatedAt = now
        };
        var apr = new EvaluationPeriod
        {
            AcademicYearId = year.Id, Name = "شهر أبريل",
            PeriodType = PeriodType.Monthly, OrderNum = 3,
            StartDate = new DateOnly(2026, 4, 1), EndDate = new DateOnly(2026, 4, 30),
            MonthName = "أبريل", CreatedAt = now, UpdatedAt = now
        };
        ctx.EvaluationPeriods.AddRange(feb, mar, apr);
        await ctx.SaveChangesAsync();

        // ── 12. EvaluationTemplates (Math + Arabic) ──
        var templateMath = new EvaluationTemplate
        {
            GradeLevelId = grade1.Id, SubjectId = subj["MTH"], AcademicYearId = year.Id,
            Name = "تقييم الرياضيات الأسبوعي", CalculationType = EvaluationCalculationType.MiddleSchool,
            IsActive = true, CreatedAt = now, UpdatedAt = now
        };
        var templateArb = new EvaluationTemplate
        {
            GradeLevelId = grade1.Id, SubjectId = subj["ARB"], AcademicYearId = year.Id,
            Name = "تقييم اللغة العربية الأسبوعي", CalculationType = EvaluationCalculationType.MiddleSchool,
            IsActive = true, CreatedAt = now, UpdatedAt = now
        };
        ctx.EvaluationTemplates.AddRange(templateMath, templateArb);
        await ctx.SaveChangesAsync();

        // ── 13. EvaluationItems ──
        var mathItems = new (string name, decimal max, ItemType type, AutoCalcType auto)[]
        {
            ("السلوك والحضور",   5m,  ItemType.Number, AutoCalcType.Attendance),
            ("الواجب المنزلي",   5m,  ItemType.Number, AutoCalcType.None),
            ("النشاط الصفي",     5m,  ItemType.Number, AutoCalcType.None),
            ("التقييم الأسبوعي", 10m, ItemType.Number, AutoCalcType.None),
            ("الاختبار القصير",  5m,  ItemType.Number, AutoCalcType.None),
        };
        var arbItems = new (string name, decimal max, ItemType type, AutoCalcType auto)[]
        {
            ("السلوك والحضور",   3m,  ItemType.Number, AutoCalcType.Attendance),
            ("الواجب المنزلي",   5m,  ItemType.Number, AutoCalcType.None),
            ("الإملاء",          2m,  ItemType.Number, AutoCalcType.None),
            ("النشاط الصفي",     5m,  ItemType.Number, AutoCalcType.None),
            ("التقييم الأسبوعي", 10m, ItemType.Number, AutoCalcType.None),
            ("الاختبار القصير",  5m,  ItemType.Number, AutoCalcType.None),
        };
        var allItems = new List<EvaluationItem>();
        int order = 1;
        foreach (var (n, m, t, a) in mathItems)
        {
            var item = new EvaluationItem
            {
                TemplateId = templateMath.Id, Name = n, MaxScore = m,
                Weight = 1, ItemType = t, AutoCalcType = a, DisplayOrder = order++,
                IsVisible = true, CreatedAt = now, UpdatedAt = now
            };
            ctx.EvaluationItems.Add(item);
            allItems.Add(item);
        }
        order = 1;
        foreach (var (n, m, t, a) in arbItems)
        {
            var item = new EvaluationItem
            {
                TemplateId = templateArb.Id, Name = n, MaxScore = m,
                Weight = 1, ItemType = t, AutoCalcType = a, DisplayOrder = order++,
                IsVisible = true, CreatedAt = now, UpdatedAt = now
            };
            ctx.EvaluationItems.Add(item);
            allItems.Add(item);
        }
        await ctx.SaveChangesAsync();

        var mathItemIds = allItems.Where(i => i.TemplateId == templateMath.Id).ToList();
        var arbItemIds = allItems.Where(i => i.TemplateId == templateArb.Id).ToList();
        var teacherUserId = teachers[0].Id; // use first teacher as entered-by

        // ── 14. StudentEvaluations — scores per item per period ──
        var evalBatch = new List<StudentEvaluation>();
        var rng = new Random(42);
        foreach (var enr in enrollments)
        {
            foreach (var period in periodsList)
            {
                foreach (var item in mathItemIds)
                {
                    var max = (double)item.MaxScore;
                    var score = item.AutoCalcType == AutoCalcType.Attendance
                        ? max
                        : Math.Round(rng.NextDouble() * max * 0.6 + max * 0.3, 2);
                    evalBatch.Add(new StudentEvaluation
                    {
                        EnrollmentId = enr.Id, EvaluationItemId = item.Id,
                        PeriodId = period.Id, Score = (decimal)score,
                        EnteredById = teacherUserId, EnteredAt = now,
                        CreatedAt = now, UpdatedAt = now
                    });
                }
                foreach (var item in arbItemIds)
                {
                    var max = (double)item.MaxScore;
                    var score = item.AutoCalcType == AutoCalcType.Attendance
                        ? max
                        : Math.Round(rng.NextDouble() * max * 0.6 + max * 0.3, 2);
                    evalBatch.Add(new StudentEvaluation
                    {
                        EnrollmentId = enr.Id, EvaluationItemId = item.Id,
                        PeriodId = period.Id, Score = (decimal)score,
                        EnteredById = teacherUserId, EnteredAt = now,
                        CreatedAt = now, UpdatedAt = now
                    });
                }
                if (evalBatch.Count >= 500)
                {
                    ctx.StudentEvaluations.AddRange(evalBatch);
                    await ctx.SaveChangesAsync();
                    evalBatch.Clear();
                }
            }
        }
        if (evalBatch.Count > 0)
        {
            ctx.StudentEvaluations.AddRange(evalBatch);
            await ctx.SaveChangesAsync();
            evalBatch.Clear();
        }

        // ── 15. PeriodAverages ──
        var avgBatch = new List<PeriodAverage>();
        foreach (var enr in enrollments)
        {
            foreach (var period in periodsList)
            {
                var scores = await ctx.StudentEvaluations
                    .Where(se => se.EnrollmentId == enr.Id && se.PeriodId == period.Id && se.Score != null)
                    .ToListAsync();
                var avg = scores.Any() ? scores.Average(s => (double)s.Score!) : 0;
                avgBatch.Add(new PeriodAverage
                {
                    EnrollmentId = enr.Id, PeriodId = period.Id,
                    AvgScore = (decimal)Math.Round(avg, 2),
                    MaxScore = 40, CalculatedAt = now,
                    CreatedAt = now, UpdatedAt = now
                });
            }
        }
        ctx.PeriodAverages.AddRange(avgBatch);
        await ctx.SaveChangesAsync();

        // ── 16. PeriodicAssessments (exam1 & exam2) ──
        var asmBatch = new List<PeriodicAssessment>();
        foreach (var enr in enrollments)
        {
            asmBatch.Add(new PeriodicAssessment
            {
                EnrollmentId = enr.Id, AssessmentType = PeriodicAssessmentType.MonthlyExam1,
                Score = (decimal)Math.Round(rng.NextDouble() * 6 + 9, 2),
                MaxScore = 15, AssessmentDate = new DateOnly(2026, 3, 15),
                CreatedAt = now, UpdatedAt = now
            });
            asmBatch.Add(new PeriodicAssessment
            {
                EnrollmentId = enr.Id, AssessmentType = PeriodicAssessmentType.MonthlyExam2,
                Score = (decimal)Math.Round(rng.NextDouble() * 6 + 9, 2),
                MaxScore = 15, AssessmentDate = new DateOnly(2026, 4, 15),
                CreatedAt = now, UpdatedAt = now
            });
        }
        ctx.PeriodicAssessments.AddRange(asmBatch);
        await ctx.SaveChangesAsync();

        // ── 17. FinalGrades ──
        var fgBatch = new List<FinalGrade>();
        foreach (var enr in enrollments)
        {
            var exam1 = await ctx.PeriodicAssessments
                .FirstOrDefaultAsync(pa => pa.EnrollmentId == enr.Id && pa.AssessmentType == PeriodicAssessmentType.MonthlyExam1);
            var exam2 = await ctx.PeriodicAssessments
                .FirstOrDefaultAsync(pa => pa.EnrollmentId == enr.Id && pa.AssessmentType == PeriodicAssessmentType.MonthlyExam2);
            var e1 = exam1?.Score ?? 0;
            var e2 = exam2?.Score ?? 0;
            var written = e1 + e2;
            var finalExam = (decimal)Math.Round(rng.NextDouble() * 10 + 20, 2);
            var total = written + finalExam;
            fgBatch.Add(new FinalGrade
            {
                EnrollmentId = enr.Id,
                PeriodAvgScore = (decimal)Math.Round(rng.NextDouble() * 15 + 25, 2),
                Assessment1Score = e1,
                Assessment2Score = e2,
                WrittenTotal = written,
                FinalExamScore = finalExam,
                Total = total > 100 ? 100 : total,
                IsPublished = true,
                CreatedAt = now, UpdatedAt = now
            });
        }
        ctx.FinalGrades.AddRange(fgBatch);
        await ctx.SaveChangesAsync();

        // ── 18. DailyAbsences (random) ──
        var absBatch = new List<DailyAbsence>();
        var mathCst = cstList.First(c => c.ClassId == class1.Id && c.SubjectId == subj["MTH"]);
        var febStart = new DateOnly(2026, 2, 1);
        foreach (var enr in enrollments)
        {
            if (rng.NextDouble() < 0.25)
            {
                for (int d = 0; d < 3; d++)
                {
                    int skip = rng.Next(0, 25);
                    absBatch.Add(new DailyAbsence
                    {
                        EnrollmentId = enr.Id,
                        ClassSubjectTeacherId = mathCst.Id,
                        AbsenceDate = febStart.AddDays(skip),
                        IsAbsent = true, Reason = "مرض",
                        PeriodId = periodsList[rng.Next(12)].Id,
                        RecordedById = teacherUserId,
                        CreatedAt = now, UpdatedAt = now
                    });
                }
            }
        }
        ctx.DailyAbsences.AddRange(absBatch);
        await ctx.SaveChangesAsync();

        // ── 19. Parent accounts (simple) ──
        int parentCount = 0;
        foreach (var st in students)
        {
            if (parentCount >= 20) break;
            var parent = new User
            {
                FullName = $"وليّ أمر {st.FullName}",
                Email = $"parent{st.Id}@school.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Parent@123"),
                Role = UserRole.Parent, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            };
            ctx.Users.Add(parent);
            await ctx.SaveChangesAsync();
            ctx.ParentStudents.Add(new ParentStudent
            {
                ParentId = parent.Id, StudentId = st.Id,
                Relationship = RelationshipType.Father,
                CreatedAt = now, UpdatedAt = now
            });
            parentCount++;
        }
        await ctx.SaveChangesAsync();
    }
}
