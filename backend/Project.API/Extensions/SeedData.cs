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

        // Ensure ClassTemplateLinks table exists (entity was added after initial migration)
        try { await ctx.Database.ExecuteSqlRawAsync("SELECT TOP 1 1 FROM ClassTemplateLinks"); }
        catch
        {
            await ctx.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE ClassTemplateLinks (
                    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    ClassId int NOT NULL,
                    TemplateId int NOT NULL,
                    AcademicYearId int NOT NULL,
                    IsDeleted bit NOT NULL DEFAULT 0,
                    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    UpdatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT FK_ClassTemplateLinks_Class FOREIGN KEY (ClassId) REFERENCES Classes(Id),
                    CONSTRAINT FK_ClassTemplateLinks_Template FOREIGN KEY (TemplateId) REFERENCES EvaluationTemplates(Id),
                    CONSTRAINT FK_ClassTemplateLinks_AcademicYear FOREIGN KEY (AcademicYearId) REFERENCES AcademicYears(Id)
                );
                CREATE UNIQUE INDEX IX_ClassTemplateLinks_Class_Template_Year
                    ON ClassTemplateLinks(ClassId, TemplateId, AcademicYearId)
                    WHERE IsDeleted = 0;");
        }

        // Ensure EvaluationTemplates unique index has IsDeleted filter
        try
        {
            await ctx.Database.ExecuteSqlRawAsync(@"
                DROP INDEX IX_EvaluationTemplates_GradeLevelId_SubjectId_AcademicYearId ON EvaluationTemplates;
                CREATE UNIQUE INDEX IX_EvaluationTemplates_GradeLevelId_SubjectId_AcademicYearId
                    ON EvaluationTemplates(GradeLevelId, SubjectId, AcademicYearId)
                    WHERE IsDeleted = 0;");
        }
        catch { /* index might already be correct */ }

        // Ensure EvaluationItems has AbsenceMaxScore column
        try
        {
            await ctx.Database.ExecuteSqlRawAsync("SELECT TOP 1 [AbsenceMaxScore] FROM EvaluationItems");
        }
        catch
        {
            await ctx.Database.ExecuteSqlRawAsync("ALTER TABLE EvaluationItems ADD AbsenceMaxScore decimal(5,2) NULL");
        }

        if (await ctx.AcademicYears.AnyAsync())
        {
        await SeedUnitsAndLessons(ctx);
        await SeedStudentUsers(ctx);
        await SeedEvaluationPeriods(ctx);
        await SeedParents(ctx);
        return;
        }

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

        // ── 7.1 Student user accounts ──
        var studentUsers = new List<User>();
        foreach (var st in students)
        {
            var studentUser = new User
            {
                FullName = st.FullName,
                Email = $"student{st.Id}@school.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123"),
                Role = UserRole.Student,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            ctx.Users.Add(studentUser);
            studentUsers.Add(studentUser);
        }
        await ctx.SaveChangesAsync();

        for (int i = 0; i < students.Count; i++)
        {
            students[i].UserId = studentUsers[i].Id;
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

        // ── 8a. TeacherSubject (teacher specializations) ──
        foreach (var kv in teacherMap)
        {
            ctx.Set<TeacherSubject>().Add(new TeacherSubject
            {
                TeacherId  = kv.Value,
                SubjectId  = subj[kv.Key],
                CreatedAt  = now,
                UpdatedAt  = now
            });
        }
        await ctx.SaveChangesAsync();

        // ── 8b. ClassSubjectTeacher ──
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

        // ── 11. EvaluationPeriods — dynamic from academic year ──
        var generatedPeriods = Project.Domain.Helpers.EvaluationPeriodGenerator.GeneratePeriods(year.Id, year.StartDate, year.EndDate);
        var periodsList = generatedPeriods.ToList();
        ctx.EvaluationPeriods.AddRange(periodsList);
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

        // ── 15.1 Parent accounts ──
        await SeedParents(ctx);

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

        await SeedUnitsAndLessons(ctx);
    }

    private static async Task SeedUnitsAndLessons(AppDbContext ctx)
    {
        if (await ctx.Units.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var subjects = await ctx.Subjects.ToDictionaryAsync(s => s.Code ?? s.Name, s => s);

        Unit AddUnit(AppDbContext c, Subject s, string name, int order, List<Lesson>? lessons = null)
        {
            var u = new Unit
            {
                SubjectId = s.Id, Name = name, DisplayOrder = order,
                CreatedAt = now, UpdatedAt = now
            };
            c.Units.Add(u);
            if (lessons != null)
            {
                foreach (var l in lessons)
                {
                    l.Unit = u;
                    c.Lessons.Add(l);
                }
            }
            return u;
        }

        // ── الرياضيات ──
        if (subjects.TryGetValue("MTH", out var mth))
        {
            var u1 = AddUnit(ctx, mth, "الوحدة الأولى: الأعداد والجبر", 1, new()
            {
                new() { Title = "المعادلات التربيعية", Content = "المعادلة التربيعية هي معادلة من الشكل ax² + bx + c = 0 حيث a ≠ 0.\nيمكن حلها بثلاث طرق: التحليل إلى عوامل، إكمال المربع، القانون العام.\nالقانون العام: x = (-b ± √(b²-4ac)) / 2a\nالمميز (Discriminant) = b²-4ac.", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
                new() { Title = "الدوال الخطية", Content = "الدالة الخطية هي دالة من الشكل f(x) = mx + b حيث m هو الميل و b هو المقطع الصادي.\nميل الخط المستقيم = (y₂-y₁)/(x₂-x₁)", DisplayOrder = 2, CreatedAt = now, UpdatedAt = now },
            });
            var u2 = AddUnit(ctx, mth, "الوحدة الثانية: الهندسة", 2, new()
            {
                new() { Title = "نظرية فيثاغورس", Content = "في المثلث القائم الزاوية، مربع طول الوتر يساوي مجموع مربعي طولي الضلعين الآخرين.\na² + b² = c²", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            });
        }

        // ── العلوم ──
        if (subjects.TryGetValue("SCI", out var sci))
        {
            AddUnit(ctx, sci, "الوحدة الأولى: الكيمياء", 1, new()
            {
                new() { Title = "المادة وخصائصها", Content = "المادة هي كل ما له كتلة ويشغل حيزاً من الفراغ.\nتوجد المادة في ثلاث حالات: صلبة، سائلة، غازية.", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            });
            AddUnit(ctx, sci, "الوحدة الثانية: الأحياء", 2, new()
            {
                new() { Title = "الميتوز والميوز", Content = "الميتوز هو انقسام خلوي ينتج عنه خليتان بنت تحملان نفس العدد الكروموسومي للخلية الأم (2n).\nالميوز هو انقسام اختزالي ينتج عنه أربع خلايا يحمل كل منها نصف عدد الكروموسومات (n).", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            });
        }

        // ── اللغة العربية ──
        if (subjects.TryGetValue("ARB", out var arb))
        {
            AddUnit(ctx, arb, "الوحدة الأولى: القراءة والنصوص", 1, new()
            {
                new() { Title = "نصوص أدبية", Content = "النص الأدبي هو عمل فني يهدف إلى التعبير عن المشاعر والأفكار باستخدام اللغة الجمالية.", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            });
            AddUnit(ctx, arb, "الوحدة الثانية: النحو", 2, new()
            {
                new() { Title = "أقسام الكلام", Content = "الكلمة في اللغة العربية تنقسم إلى ثلاثة أقسام: اسم، فعل، حرف.", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            });
        }

        // ── اللغة الإنجليزية (بدون دروس) ──
        if (subjects.TryGetValue("ENG", out var eng))
        {
            AddUnit(ctx, eng, "Unit 1: Greetings and Introductions", 1);
            AddUnit(ctx, eng, "Unit 2: Daily Routines", 2);
            AddUnit(ctx, eng, "Unit 3: Food and Drinks", 3);
            AddUnit(ctx, eng, "Unit 4: Travel and Tourism", 4);
        }

        // ── الدراسات الاجتماعية ──
        if (subjects.TryGetValue("SOC", out var soc))
        {
            AddUnit(ctx, soc, "الوحدة الأولى: الجغرافيا", 1, new()
            {
                new() { Title = "الموقع الجغرافي لمصر", Content = "تقع مصر في شمال شرق قارة أفريقيا، يحدها من الشمال البحر المتوسط، ومن الشرق البحر الأحمر.", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            });
            AddUnit(ctx, soc, "الوحدة الثانية: التاريخ", 2, new()
            {
                new() { Title = "الحضارة المصرية القديمة", Content = "تعد الحضارة المصرية القديمة واحدة من أقدم الحضارات في العالم، وتميزت ببناء الأهرامات.", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            });
        }

        // ── التربية الإسلامية ──
        if (subjects.TryGetValue("ISL", out var isl))
        {
            AddUnit(ctx, isl, "الوحدة الأولى: القرآن الكريم", 1, new()
            {
                new() { Title = "سورة الفاتحة", Content = "سورة الفاتحة هي أعظم سورة في القرآن الكريم، وهي السبع المثاني.", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            });
        }

        // ── الحاسب الآلي ──
        if (subjects.TryGetValue("CMP", out var cmp))
        {
            AddUnit(ctx, cmp, "الوحدة الأولى: أساسيات الحاسب", 1, new()
            {
                new() { Title = "مكونات الحاسب", Content = "يتكون الحاسب من مكونات مادية: وحدة المعالجة المركزية، الذاكرة، أجهزة الإدخال والإخراج.", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            });
            AddUnit(ctx, cmp, "الوحدة الثانية: البرمجة", 2, new()
            {
                new() { Title = "مقدمة في البرمجة", Content = "البرمجة هي عملية كتابة تعليمات للحاسب لتنفيذ مهمة محددة.", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            });
        }

        await ctx.SaveChangesAsync();
    }

    private static async Task SeedStudentUsers(AppDbContext ctx)
    {
        var now = DateTime.UtcNow;
        var allStudents = await ctx.Students.ToListAsync();
        var existingEmails = await ctx.Users
            .Where(u => u.Role == UserRole.Student && u.Email != null)
            .Select(u => u.Email)
            .ToListAsync();
        var emailSet = new HashSet<string>(existingEmails!);

        foreach (var st in allStudents)
        {
            if (st.UserId != null) continue;

            var email = $"student{st.Id}@school.com";
            var existingUser = await ctx.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                st.UserId = existingUser.Id;
            }
            else
            {
                var su = new User
                {
                    FullName = st.FullName,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123"),
                    Role = UserRole.Student, IsActive = true,
                    CreatedAt = now, UpdatedAt = now
                };
                ctx.Users.Add(su);
                await ctx.SaveChangesAsync();
                st.UserId = su.Id;
            }
        }
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedEvaluationPeriods(AppDbContext ctx)
    {
        if (await ctx.EvaluationPeriods.AnyAsync())
            return;

        var year = await ctx.AcademicYears.FirstAsync();
        var periods = Project.Domain.Helpers.EvaluationPeriodGenerator.GeneratePeriods(year.Id, year.StartDate, year.EndDate);
        ctx.EvaluationPeriods.AddRange(periods);
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedParents(AppDbContext ctx)
    {
        var now = DateTime.UtcNow;
        var existingParentEmails = await ctx.Users
            .Where(u => u.Role == UserRole.Parent && u.Email != null)
            .Select(u => u.Email!)
            .ToListAsync();
        var emailSet = new HashSet<string>(existingParentEmails);

        var studentsWithoutParent = await ctx.Students
            .Where(s => !s.IsDeleted && !ctx.ParentStudents.Any(ps => ps.StudentId == s.Id))
            .ToListAsync();

        foreach (var st in studentsWithoutParent)
        {
            var email = $"parent{st.Id}@school.com";
            if (emailSet.Contains(email)) continue;

            var parent = new User
            {
                FullName = $"وليّ أمر {st.FullName}",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Parent@123"),
                Role = UserRole.Parent,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            ctx.Users.Add(parent);
            await ctx.SaveChangesAsync();

            ctx.ParentStudents.Add(new ParentStudent
            {
                ParentId = parent.Id,
                StudentId = st.Id,
                Relationship = RelationshipType.Father,
                CreatedAt = now,
                UpdatedAt = now
            });
            emailSet.Add(email);
        }
        await ctx.SaveChangesAsync();
    }
}
