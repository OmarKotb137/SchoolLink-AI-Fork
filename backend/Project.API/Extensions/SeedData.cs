using Microsoft.EntityFrameworkCore;
using Project.DAL.Context;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.API.Extensions;

/// <summary>
/// One-time database seeder.
/// Runs on every startup but exits immediately if Users table already has data.
/// Covers every table in the schema with at least one record.
/// </summary>
public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // ── Apply any pending migrations ───────────────────────────────────
        // Auto-heal: if the DB has tables but zero migration history
        // (happens when dotnet-ef drop targeted a different SQL instance
        // than the one the app connects to at runtime), drop and recreate.
        if (await ctx.Database.CanConnectAsync())
        {
            var applied = (await ctx.Database.GetAppliedMigrationsAsync()).ToList();
            if (!applied.Any())
                await ctx.Database.EnsureDeletedAsync(); // wipe orphaned schema
        }
        await ctx.Database.MigrateAsync();

        // ── Guard: seed only once ──────────────────────────────────────────
        if (await ctx.Users.IgnoreQueryFilters().AnyAsync()) return;

        var now = DateTime.UtcNow;
        var rng = new Random(42);

        // =================================================================
        // 1. AcademicYear
        // =================================================================
        var year = new AcademicYear
        {
            Name      = "2025/2026",
            StartDate = new DateOnly(2025, 9, 21),
            EndDate   = new DateOnly(2026, 6, 11),
            IsCurrent = true,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.AcademicYears.Add(year);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 2. SchoolProfile
        // =================================================================
        ctx.SchoolProfiles.Add(new SchoolProfile
        {
            SchoolName                = "مدرسة النيل الإعدادية بنين",
            Governorate               = "محافظة الجيزة",
            Directorate               = "مديرية التربية والتعليم بالجيزة",
            EducationalAdministration = "إدارة الجيزة التعليمية",
            Address    = "شارع النيل - وسط الجيزة",
            Phone      = "02-37654321",
            Email      = "nile.prep@sch.ed.eg",
            ManagerName= "أ/ محمد عبد الرحمن",
            IsActive   = true,
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        // =================================================================
        // 3. GradeLevel
        // =================================================================
        var grade1 = new GradeLevel
        {
            Name = "الصف الأول الإعدادي", LevelOrder = 1, Stage = "إعدادي",
            CreatedAt = now, UpdatedAt = now
        };
        var grade2 = new GradeLevel
        {
            Name = "الصف الثاني الإعدادي", LevelOrder = 2, Stage = "إعدادي",
            CreatedAt = now, UpdatedAt = now
        };
        var grade3 = new GradeLevel
        {
            Name = "الصف الثالث الإعدادي", LevelOrder = 3, Stage = "إعدادي",
            CreatedAt = now, UpdatedAt = now
        };
        ctx.GradeLevels.AddRange(grade1, grade2, grade3);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 4. Subjects  (7 subjects)
        // =================================================================
        var subjectDefs = new (string name, string code)[]
        {
            ("اللغة العربية",       "ARB"),
            ("اللغة الإنجليزية",    "ENG"),
            ("الرياضيات",           "MTH"),
            ("العلوم",              "SCI"),
            ("الدراسات الاجتماعية","SOC"),
            ("التربية الإسلامية",  "ISL"),
            ("الحاسب الآلي",       "CMP"),
        };
        var subjectEntities = subjectDefs
            .Select(d => new Subject { Name = d.name, Code = d.code, CreatedAt = now, UpdatedAt = now })
            .ToList();
        ctx.Subjects.AddRange(subjectEntities);
        await ctx.SaveChangesAsync();

        // Quick lookup: code → entity
        var S = subjectEntities.ToDictionary(s => s.Code!, s => s);

        // =================================================================
        // 5. Users — Admin
        // =================================================================
        var admin = new User
        {
            FullName = "Super Admin", Username = "admin",
            ContactEmail = "admin@school.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = UserRole.Admin, IsActive = true,
            CreatedAt = now, UpdatedAt = now
        };
        var director = new User
        {
            FullName = "مدير المدرسة", Username = "director",
            ContactEmail = "director@school.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Director@123"),
            Role = UserRole.Admin, IsActive = true,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.Users.AddRange(admin, director);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 6. Users — Teachers (one per subject)
        // =================================================================
        var teacherDefs = new (string fullName, string username, string email, string code)[]
        {
            ("أحمد علي حسن",        "ahmed.ali",       "ahmed.ali@school.com",       "ARB"),
            ("سارة محمود عبد الله", "sara.mahmoud",    "sara.mahmoud@school.com",    "ENG"),
            ("محمد كمال الدين",     "mohamed.kamal",   "mohamed.kamal@school.com",   "MTH"),
            ("إيمان عبد الفتاح",    "eman.abdelfattah","eman.abdelfattah@school.com","SCI"),
            ("خالد رشاد عثمان",     "khaled.reshad",   "khaled.reshad@school.com",   "SOC"),
            ("نادية جابر سليمان",   "nadia.gaber",     "nadia.gaber@school.com",     "ISL"),
            ("أيمن شعبان مصطفى",    "ayman.shaban",    "ayman.shaban@school.com",    "CMP"),
        };
        var teacherUsers = teacherDefs.Select(d => new User
        {
            FullName = d.fullName, Username = d.username, ContactEmail = d.email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Teacher@123"),
            Role = UserRole.Teacher, IsActive = true,
            CreatedAt = now, UpdatedAt = now
        }).ToList();
        ctx.Users.AddRange(teacherUsers);
        await ctx.SaveChangesAsync();

        // code → teacher User
        var T = teacherDefs.Zip(teacherUsers, (d, u) => (d.code, user: u))
                           .ToDictionary(x => x.code, x => x.user);

        // =================================================================
        // 7. Rooms
        // =================================================================
        var roomDefs = new (string name, string type, int cap)[]
        {
            ("فصل أ",               "Classroom", 35),
            ("فصل ب",               "Classroom", 35),
            ("فصل ج",               "Classroom", 35),
            ("فصل د",               "Classroom", 35),
            ("فصل هـ",              "Classroom", 35),
            ("فصل و",               "Classroom", 35),
            ("معمل علوم",           "Lab",       30),
            ("معمل حاسب",           "Lab",       30),
            ("قاعة متعددة الأغراض","Hall",      100),
        };
        var rooms = roomDefs.Select(d => new Room
        {
            Name = d.name, Type = d.type, Capacity = d.cap,
            CreatedAt = now, UpdatedAt = now
        }).ToList();
        ctx.Rooms.AddRange(rooms);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 8. Classes  (2 classes, same grade)
        // =================================================================
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

        // =================================================================
        // 8b. Classes — grade2 & grade3
        // =================================================================
        var class3 = new SchoolClass
        {
            GradeLevelId = grade2.Id, AcademicYearId = year.Id,
            Name = "2/1", CreatedAt = now, UpdatedAt = now
        };
        var class4 = new SchoolClass
        {
            GradeLevelId = grade2.Id, AcademicYearId = year.Id,
            Name = "2/2", CreatedAt = now, UpdatedAt = now
        };
        var class5 = new SchoolClass
        {
            GradeLevelId = grade3.Id, AcademicYearId = year.Id,
            Name = "3/1", CreatedAt = now, UpdatedAt = now
        };
        var class6 = new SchoolClass
        {
            GradeLevelId = grade3.Id, AcademicYearId = year.Id,
            Name = "3/2", CreatedAt = now, UpdatedAt = now
        };
        ctx.Classes.AddRange(class3, class4, class5, class6);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 9. TeacherSubjects + ClassSubjectTeachers
        // =================================================================
        var weeklyPeriods = new Dictionary<string, int>
        {
            ["ARB"] = 6, ["ENG"] = 4, ["MTH"] = 5,
            ["SCI"] = 3, ["SOC"] = 3, ["ISL"] = 2, ["CMP"] = 2
        };

        // TeacherSubject — one row per (teacher, subject)
        foreach (var (code, teacher) in T)
        {
            ctx.TeacherSubjects.Add(new TeacherSubject
            {
                TeacherId = teacher.Id, SubjectId = S[code].Id,
                CreatedAt = now, UpdatedAt = now
            });
        }
        await ctx.SaveChangesAsync();

        // ClassSubjectTeacher — each teacher × each class
        var cstList = new List<ClassSubjectTeacher>();
        foreach (var cls in new[] { class1, class2, class3, class4, class5, class6 })
        {
            foreach (var (code, teacher) in T)
            {
                var cst = new ClassSubjectTeacher
                {
                    ClassId = cls.Id, SubjectId = S[code].Id,
                    TeacherId = teacher.Id, AcademicYearId = year.Id,
                    WeeklyPeriods = weeklyPeriods[code],
                    CreatedAt = now, UpdatedAt = now
                };
                ctx.ClassSubjectTeachers.Add(cst);
                cstList.Add(cst);
            }
        }
        await ctx.SaveChangesAsync();

        // =================================================================
        // 10. Timetables + TimetableSlots
        // =================================================================
        var schoolDays = new[] { SchoolDay.Sunday, SchoolDay.Monday, SchoolDay.Tuesday, SchoolDay.Wednesday, SchoolDay.Thursday };
        var slotTimes  = new (TimeOnly start, TimeOnly end)[]
        {
            (new TimeOnly(8,  0), new TimeOnly(8,  45)),
            (new TimeOnly(8, 45), new TimeOnly(9,  30)),
            (new TimeOnly(9, 45), new TimeOnly(10, 30)),
            (new TimeOnly(10,30), new TimeOnly(11, 15)),
            (new TimeOnly(11,30), new TimeOnly(12, 15)),
            (new TimeOnly(12,15), new TimeOnly(13,  0)),
            (new TimeOnly(13,15), new TimeOnly(14,  0)),
        };
        var codes7 = new[] { "ARB", "ENG", "MTH", "SCI", "SOC", "ISL", "CMP" };

        var classArray = new[] { class1, class2, class3, class4, class5, class6 };
        for (int ci = 0; ci < classArray.Length; ci++)
        {
            var cls = classArray[ci];

            // subjectId → cstId for this class
            var cstMap = cstList
                .Where(c => c.ClassId == cls.Id)
                .ToDictionary(c => c.SubjectId, c => c.Id);

            var tt = new Timetable
            {
                ClassId = cls.Id, AcademicYearId = year.Id,
                IsActive = true, CreatedAt = now, UpdatedAt = now
            };
            ctx.Timetables.Add(tt);
            await ctx.SaveChangesAsync();

            var slots = new List<TimetableSlot>();
            for (int d = 0; d < schoolDays.Length; d++)
            {
                for (int p = 0; p < slotTimes.Length; p++)
                {
                    var code = codes7[(d * slotTimes.Length + p) % 7];
                    slots.Add(new TimetableSlot
                    {
                        TimetableId           = tt.Id,
                        DayOfWeek             = schoolDays[d],
                        PeriodNumber          = p + 1,
                        StartTime             = slotTimes[p].start,
                        EndTime               = slotTimes[p].end,
                        ClassSubjectTeacherId = cstMap[S[code].Id],
                        RoomId                = rooms[ci].Id,  // كل فصل في أوضته الخاصة
                        CreatedAt = now, UpdatedAt = now
                    });
                }
            }
            ctx.TimetableSlots.AddRange(slots);
        }
        await ctx.SaveChangesAsync();

        // =================================================================
        // 11. EvaluationPeriods
        // =================================================================
        var periodsList = Project.Domain.Helpers.EvaluationPeriodGenerator
            .GeneratePeriods(year.Id, year.StartDate, year.EndDate)
            .ToList();
        ctx.EvaluationPeriods.AddRange(periodsList);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 12. EvaluationTemplates — Math + Arabic (للصفوف الثلاثة)
        // =================================================================
        var templateMath = new EvaluationTemplate
        {
            GradeLevelId = grade1.Id, SubjectId = S["MTH"].Id, AcademicYearId = year.Id,
            Name = "تقييم الرياضيات - الأول الإعدادي",
            CalculationType = EvaluationCalculationType.MiddleSchool,
            IsActive = true, Weeks = 12, CreatedAt = now, UpdatedAt = now
        };
        var templateArb = new EvaluationTemplate
        {
            GradeLevelId = grade1.Id, SubjectId = S["ARB"].Id, AcademicYearId = year.Id,
            Name = "تقييم اللغة العربية - الأول الإعدادي",
            CalculationType = EvaluationCalculationType.MiddleSchool,
            IsActive = true, Weeks = 12, CreatedAt = now, UpdatedAt = now
        };
        var templateMath2 = new EvaluationTemplate
        {
            GradeLevelId = grade2.Id, SubjectId = S["MTH"].Id, AcademicYearId = year.Id,
            Name = "تقييم الرياضيات - الثاني الإعدادي",
            CalculationType = EvaluationCalculationType.MiddleSchool,
            IsActive = true, Weeks = 12, CreatedAt = now, UpdatedAt = now
        };
        var templateArb2 = new EvaluationTemplate
        {
            GradeLevelId = grade2.Id, SubjectId = S["ARB"].Id, AcademicYearId = year.Id,
            Name = "تقييم اللغة العربية - الثاني الإعدادي",
            CalculationType = EvaluationCalculationType.MiddleSchool,
            IsActive = true, Weeks = 12, CreatedAt = now, UpdatedAt = now
        };
        var templateMath3 = new EvaluationTemplate
        {
            GradeLevelId = grade3.Id, SubjectId = S["MTH"].Id, AcademicYearId = year.Id,
            Name = "تقييم الرياضيات - الثالث الإعدادي",
            CalculationType = EvaluationCalculationType.MiddleSchool,
            IsActive = true, Weeks = 12, CreatedAt = now, UpdatedAt = now
        };
        var templateArb3 = new EvaluationTemplate
        {
            GradeLevelId = grade3.Id, SubjectId = S["ARB"].Id, AcademicYearId = year.Id,
            Name = "تقييم اللغة العربية - الثالث الإعدادي",
            CalculationType = EvaluationCalculationType.MiddleSchool,
            IsActive = true, Weeks = 12, CreatedAt = now, UpdatedAt = now
        };
        ctx.EvaluationTemplates.AddRange(
            templateMath, templateArb,
            templateMath2, templateArb2,
            templateMath3, templateArb3);
        await ctx.SaveChangesAsync();

        // EvaluationItems — grade 1
        var mathItems = BuildItems(templateMath.Id, now, new[]
        {
            ("السلوك والحضور",    5m,  ItemType.Number, AutoCalcType.Attendance),
            ("الواجب المنزلي",    5m,  ItemType.Number, AutoCalcType.None),
            ("النشاط الصفي",      5m,  ItemType.Number, AutoCalcType.None),
            ("التقييم الأسبوعي", 10m, ItemType.Number, AutoCalcType.None),
            ("الاختبار القصير",   5m,  ItemType.Number, AutoCalcType.None),
        });
        var arbItems = BuildItems(templateArb.Id, now, new[]
        {
            ("السلوك والحضور",    3m,  ItemType.Number, AutoCalcType.Attendance),
            ("الواجب المنزلي",    5m,  ItemType.Number, AutoCalcType.None),
            ("الإملاء",           2m,  ItemType.Number, AutoCalcType.None),
            ("النشاط الصفي",      5m,  ItemType.Number, AutoCalcType.None),
            ("التقييم الأسبوعي", 10m, ItemType.Number, AutoCalcType.None),
            ("الاختبار القصير",   5m,  ItemType.Number, AutoCalcType.None),
        });

        // EvaluationItems — grade 2
        var mathItems2 = BuildItems(templateMath2.Id, now, new[]
        {
            ("السلوك والحضور",    5m,  ItemType.Number, AutoCalcType.Attendance),
            ("الواجب المنزلي",    5m,  ItemType.Number, AutoCalcType.None),
            ("النشاط الصفي",      5m,  ItemType.Number, AutoCalcType.None),
            ("التقييم الأسبوعي", 10m, ItemType.Number, AutoCalcType.None),
            ("الاختبار القصير",   5m,  ItemType.Number, AutoCalcType.None),
        });
        var arbItems2 = BuildItems(templateArb2.Id, now, new[]
        {
            ("السلوك والحضور",    3m,  ItemType.Number, AutoCalcType.Attendance),
            ("الواجب المنزلي",    5m,  ItemType.Number, AutoCalcType.None),
            ("الإملاء",           2m,  ItemType.Number, AutoCalcType.None),
            ("النشاط الصفي",      5m,  ItemType.Number, AutoCalcType.None),
            ("التقييم الأسبوعي", 10m, ItemType.Number, AutoCalcType.None),
            ("الاختبار القصير",   5m,  ItemType.Number, AutoCalcType.None),
        });

        // EvaluationItems — grade 3
        var mathItems3 = BuildItems(templateMath3.Id, now, new[]
        {
            ("السلوك والحضور",    5m,  ItemType.Number, AutoCalcType.Attendance),
            ("الواجب المنزلي",    5m,  ItemType.Number, AutoCalcType.None),
            ("النشاط الصفي",      5m,  ItemType.Number, AutoCalcType.None),
            ("التقييم الأسبوعي", 10m, ItemType.Number, AutoCalcType.None),
            ("الاختبار القصير",   5m,  ItemType.Number, AutoCalcType.None),
        });
        var arbItems3 = BuildItems(templateArb3.Id, now, new[]
        {
            ("السلوك والحضور",    3m,  ItemType.Number, AutoCalcType.Attendance),
            ("الواجب المنزلي",    5m,  ItemType.Number, AutoCalcType.None),
            ("الإملاء",           2m,  ItemType.Number, AutoCalcType.None),
            ("النشاط الصفي",      5m,  ItemType.Number, AutoCalcType.None),
            ("التقييم الأسبوعي", 10m, ItemType.Number, AutoCalcType.None),
            ("الاختبار القصير",   5m,  ItemType.Number, AutoCalcType.None),
        });

        var allEvalItems = mathItems.Concat(arbItems)
            .Concat(mathItems2).Concat(arbItems2)
            .Concat(mathItems3).Concat(arbItems3).ToList();
        ctx.EvaluationItems.AddRange(allEvalItems);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 13. ClassTemplateLinks  (each class × its grade templates)
        // =================================================================
        var classTmplPairs = new (SchoolClass cls, EvaluationTemplate math, EvaluationTemplate arb)[]
        {
            (class1, templateMath,  templateArb),
            (class2, templateMath,  templateArb),
            (class3, templateMath2, templateArb2),
            (class4, templateMath2, templateArb2),
            (class5, templateMath3, templateArb3),
            (class6, templateMath3, templateArb3),
        };
        foreach (var (cls, tmplMath, tmplArb) in classTmplPairs)
        {
            ctx.ClassTemplateLinks.Add(new ClassTemplateLink
            {
                ClassId = cls.Id, TemplateId = tmplMath.Id, AcademicYearId = year.Id,
                CreatedAt = now, UpdatedAt = now
            });
            ctx.ClassTemplateLinks.Add(new ClassTemplateLink
            {
                ClassId = cls.Id, TemplateId = tmplArb.Id, AcademicYearId = year.Id,
                CreatedAt = now, UpdatedAt = now
            });
        }
        await ctx.SaveChangesAsync();

        // =================================================================
        // 14. Students (10 per class × 6 classes = 60) + User accounts + Enrollments
        // =================================================================
        var firstNames  = new[] { "أحمد","محمد","علي","عمر","خالد","يوسف","محمود","كريم","حسن","حسين","إبراهيم","عبد الله","أمير","بسام","جمال","حمزة","سامي","صالح","عادل","طارق","زياد","شادي","نادر","هاني","وائل","باهر","رامي","فادي","مازن","ياسر" };
        var middleNames = new[] { "عبد الرحمن","سامح","جابر","إسماعيل","فتحي","أنور","حمدي","رفعت","سمير","جلال","نبيل","كامل","رشاد","بهجت","نعيم","وجيه","قاسم","مبشر","رؤوف","وديع","بهاء","سعيد","لطفي","نزيه","هشام","أكرم","بطرس","ثروت","جورج","حبيب" };
        var lastNames   = new[] { "عبد العزيز","السيد","خليل","مرسي","فوزي","نصر","الشيخ","شحاتة","النجار","هاشم","الديب","رمضان","سلامة","بسيوني","عتريس","أبو زيد","الزهار","عرفة","جابر","خضر","زيتون","طنطاوي","عبده","غانم","قنديل","كمال","لقمة","موسى","نوح","هندي" };

        // ── Create Student rows ─────────────────────────────────────────
        var students = new List<Student>();
        int seq = 0;
        foreach (var cls in new[] { class1, class2, class3, class4, class5, class6 })
        {
            for (int i = 0; i < 10; i++)
            {
                seq++;
                var st = new Student
                {
                    FullName  = $"{firstNames[i]} {middleNames[i]} {lastNames[i]}",
                    Gender    = Gender.Male,
                    BirthDate = new DateOnly(2012, 1 + (seq % 12), 1 + (seq % 28)),
                    IsActive  = true,
                    CreatedAt = now, UpdatedAt = now
                };
                ctx.Students.Add(st);
                students.Add(st);
            }
        }
        await ctx.SaveChangesAsync();

        // ── Create Student User accounts (batch) ────────────────────────
        var studentUsers = students.Select(st => new User
        {
            FullName     = st.FullName,
            Username     = $"student{st.Id}",
            ContactEmail = $"student{st.Id}@school.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123"),
            Role = UserRole.Student, IsActive = true,
            CreatedAt = now, UpdatedAt = now
        }).ToList();
        ctx.Users.AddRange(studentUsers);
        await ctx.SaveChangesAsync();

        // Link Student → User
        for (int i = 0; i < students.Count; i++)
            students[i].UserId = studentUsers[i].Id;
        await ctx.SaveChangesAsync();

        // ── Enrollments ─────────────────────────────────────────────────
        var enrollments = new List<StudentEnrollment>();
        seq = 0;
        foreach (var cls in new[] { class1, class2, class3, class4, class5, class6 })
        {
            for (int i = 0; i < 10; i++)
            {
                var enr = new StudentEnrollment
                {
                    StudentId      = students[seq].Id,
                    ClassId        = cls.Id,
                    AcademicYearId = year.Id,
                    EnrolledAt     = new DateOnly(2025, 9, 21),
                    CreatedAt = now, UpdatedAt = now
                };
                ctx.StudentEnrollments.Add(enr);
                enrollments.Add(enr);
                seq++;
            }
        }
        await ctx.SaveChangesAsync();

        // =================================================================
        // 15. Parents (one per student) + ParentStudents
        // =================================================================
        var parentUsers = students.Select(st => new User
        {
            FullName     = $"وليّ أمر {st.FullName}",
            Username     = $"parent{st.Id}",
            ContactEmail = $"parent{st.Id}@school.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Parent@123"),
            Role = UserRole.Parent, IsActive = true,
            CreatedAt = now, UpdatedAt = now
        }).ToList();
        ctx.Users.AddRange(parentUsers);
        await ctx.SaveChangesAsync();

        var parentStudentLinks = students.Zip(parentUsers, (st, p) => new ParentStudent
        {
            ParentId     = p.Id,
            StudentId    = st.Id,
            Relationship = RelationshipType.Father,
            CreatedAt = now, UpdatedAt = now
        }).ToList();
        ctx.ParentStudents.AddRange(parentStudentLinks);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 16. StudentEvaluations (batch insert)
        //     + accumulate scores in memory for PeriodAverages (no N+1)
        // =================================================================
        var mathTeacherId = T["MTH"].Id;

        // (enrollmentId, periodId) → (earnedTotal, maxTotal)
        var periodTotals = new Dictionary<(int, int), (double earned, double max)>();

        var evalBatch = new List<StudentEvaluation>(500);
        // كل فصل ياخد items بتاعة درجته بس
        var classEvalItems = new Dictionary<int, List<EvaluationItem>>
        {
            [class1.Id] = mathItems.Concat(arbItems).ToList(),
            [class2.Id] = mathItems.Concat(arbItems).ToList(),
            [class3.Id] = mathItems2.Concat(arbItems2).ToList(),
            [class4.Id] = mathItems2.Concat(arbItems2).ToList(),
            [class5.Id] = mathItems3.Concat(arbItems3).ToList(),
            [class6.Id] = mathItems3.Concat(arbItems3).ToList(),
        };
        foreach (var enr in enrollments)
        {
            var gradeItems = classEvalItems[enr.ClassId];
            foreach (var period in periodsList)
            {
                double periodEarned = 0;
                double periodMax    = 0;

                foreach (var item in gradeItems)
                {
                    var maxD  = (double)item.MaxScore;
                    var score = item.AutoCalcType == AutoCalcType.Attendance
                        ? maxD
                        : Math.Round(rng.NextDouble() * maxD * 0.6 + maxD * 0.4, 2);

                    evalBatch.Add(new StudentEvaluation
                    {
                        EnrollmentId     = enr.Id,
                        EvaluationItemId = item.Id,
                        PeriodId         = period.Id,
                        Score            = (decimal)score,
                        EnteredById      = mathTeacherId,
                        EnteredAt        = now,
                        CreatedAt = now, UpdatedAt = now
                    });

                    periodEarned += score;
                    periodMax    += maxD;
                }

                periodTotals[(enr.Id, period.Id)] = (periodEarned, periodMax);

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
        }

        // =================================================================
        // 17. PeriodAverages  (fully in-memory, zero extra queries)
        // =================================================================
        var avgBatch = periodTotals.Select(kv => new PeriodAverage
        {
            EnrollmentId = kv.Key.Item1,
            PeriodId     = kv.Key.Item2,
            AvgScore     = (decimal)Math.Round(kv.Value.earned, 2),
            MaxScore     = (decimal)Math.Round(kv.Value.max,    2),
            CalculatedAt = now,
            CreatedAt = now, UpdatedAt = now
        }).ToList();
        ctx.PeriodAverages.AddRange(avgBatch);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 18. PeriodicAssessments (2 exams per enrollment)
        // =================================================================
        var assessments = new List<PeriodicAssessment>();
        foreach (var enr in enrollments)
        {
            assessments.Add(new PeriodicAssessment
            {
                EnrollmentId   = enr.Id,
                AssessmentType = PeriodicAssessmentType.MonthlyExam1,
                Score          = (decimal)Math.Round(rng.NextDouble() * 6 + 9, 2),
                MaxScore       = 15,
                AssessmentDate = new DateOnly(2026, 3, 15),
                CreatedAt = now, UpdatedAt = now
            });
            assessments.Add(new PeriodicAssessment
            {
                EnrollmentId   = enr.Id,
                AssessmentType = PeriodicAssessmentType.MonthlyExam2,
                Score          = (decimal)Math.Round(rng.NextDouble() * 6 + 9, 2),
                MaxScore       = 15,
                AssessmentDate = new DateOnly(2026, 4, 15),
                CreatedAt = now, UpdatedAt = now
            });
        }
        ctx.PeriodicAssessments.AddRange(assessments);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 19. FinalGrades  (in-memory from assessments already created)
        // =================================================================
        var asmByEnr = assessments.GroupBy(a => a.EnrollmentId)
                                   .ToDictionary(g => g.Key, g => g.ToList());
        var finalGrades = enrollments.Select(enr =>
        {
            var asms = asmByEnr.GetValueOrDefault(enr.Id) ?? new();
            var e1   = asms.FirstOrDefault(a => a.AssessmentType == PeriodicAssessmentType.MonthlyExam1)?.Score ?? 0;
            var e2   = asms.FirstOrDefault(a => a.AssessmentType == PeriodicAssessmentType.MonthlyExam2)?.Score ?? 0;
            var written  = e1 + e2;
            var finalExam= (decimal)Math.Round(rng.NextDouble() * 10 + 20, 2);
            var total    = written + finalExam;
            return new FinalGrade
            {
                EnrollmentId     = enr.Id,
                PeriodAvgScore   = (decimal)Math.Round(rng.NextDouble() * 15 + 25, 2),
                Assessment1Score = e1,
                Assessment2Score = e2,
                WrittenTotal     = written,
                FinalExamScore   = finalExam,
                Total            = total > 100 ? 100 : total,
                IsPublished      = true,
                CreatedAt = now, UpdatedAt = now
            };
        }).ToList();
        ctx.FinalGrades.AddRange(finalGrades);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 20. DailyAbsences  (~25% of students, 3 absences each)
        // =================================================================
        var mathCstClass1 = cstList.First(c => c.ClassId == class1.Id && c.SubjectId == S["MTH"].Id);
        var febStart = new DateOnly(2026, 2, 1);
        var absences = new List<DailyAbsence>();
        foreach (var enr in enrollments)
        {
            if (rng.NextDouble() >= 0.25) continue;
            var mathCstForClass = cstList.First(c => c.ClassId == enr.ClassId && c.SubjectId == S["MTH"].Id);
            for (int d = 0; d < 3; d++)
            {
                absences.Add(new DailyAbsence
                {
                    EnrollmentId          = enr.Id,
                    ClassSubjectTeacherId = mathCstForClass.Id,
                    AbsenceDate           = febStart.AddDays(d * 8 + rng.Next(0, 7)),
                    IsAbsent              = true,
                    Reason                = "مرض",
                    PeriodId              = periodsList[rng.Next(periodsList.Count)].Id,
                    RecordedById          = mathTeacherId,
                    CreatedAt = now, UpdatedAt = now
                });
            }
        }
        ctx.DailyAbsences.AddRange(absences);
        await ctx.SaveChangesAsync();

        // =================================================================
        // 21. Units + Lessons  (all 7 subjects)
        // =================================================================
        await SeedUnitsAndLessons(ctx, S, now);

        // =================================================================
        // 22. Assignment + Question + Option + Submission + Answer
        // =================================================================
        var mathCst1 = cstList.First(c => c.ClassId == class1.Id && c.SubjectId == S["MTH"].Id);
        var assignment = new Assignment
        {
            ClassSubjectTeacherId = mathCst1.Id,
            Title       = "واجب الرياضيات الأول",
            Description = "واجب أسبوعي — حل التمارين من الكتاب.",
            DueDate     = now.AddDays(7),
            MaxScore    = 10,
            IsAutoGraded= true,
            Category    = EvaluationCategory.Academic,
            IsPublished = true,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.Assignments.Add(assignment);
        await ctx.SaveChangesAsync();

        var asgQ = new AssignmentQuestion
        {
            AssignmentId = assignment.Id,
            QuestionText = "ما ناتج 2 + 3؟",
            QuestionType = QuestionType.MultipleChoice,
            CorrectAnswer= "5",
            DisplayOrder = 1,
            Points       = 10,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.AssignmentQuestions.Add(asgQ);
        await ctx.SaveChangesAsync();

        var asgOpt = new AssignmentQuestionOption
        {
            QuestionId   = asgQ.Id,
            OptionText   = "5",
            IsCorrect    = true,
            DisplayOrder = 1,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.AssignmentQuestionOptions.Add(asgOpt);
        await ctx.SaveChangesAsync();

        var firstEnr = enrollments.First();
        var submission = new StudentAssignmentSubmission
        {
            EnrollmentId = firstEnr.Id,
            AssignmentId = assignment.Id,
            SubmittedAt  = now,
            Score        = 10,
            MaxScore     = 10,
            IsGraded     = true,
            AIFeedback   = "إجابة صحيحة.",
            CreatedAt = now, UpdatedAt = now
        };
        ctx.StudentAssignmentSubmissions.Add(submission);
        await ctx.SaveChangesAsync();

        ctx.StudentAssignmentAnswers.Add(new StudentAssignmentAnswer
        {
            SubmissionId     = submission.Id,
            QuestionId       = asgQ.Id,
            AnswerText       = "5",
            SelectedOptionId = asgOpt.Id,
            IsCorrect        = true,
            PointsEarned     = 10,
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        // =================================================================
        // 23. Exam + Group + Question + Option + Attempt + Answer
        // =================================================================
        var exam = new Exam
        {
            ClassSubjectTeacherId = mathCst1.Id,
            Title           = "اختبار الرياضيات القصير",
            StartTime       = now.AddDays(1),
            EndTime         = now.AddDays(1).AddMinutes(30),
            DurationMinutes = 30,
            TotalScore      = 10,
            Category        = EvaluationCategory.Academic,
            IsPublished     = true,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.Exams.Add(exam);
        await ctx.SaveChangesAsync();

        var examGroup = new ExamQuestionGroup
        {
            ExamId       = exam.Id,
            DisplayType  = TemplateContentType.Passage,
            ContentTitle = "أسئلة عامة",
            ContentText  = "اختر الإجابة الصحيحة.",
            DisplayOrder = 1,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.ExamQuestionGroups.Add(examGroup);
        await ctx.SaveChangesAsync();

        var examQ = new ExamQuestion
        {
            ExamId       = exam.Id,
            GroupId      = examGroup.Id,
            QuestionText = "ما ناتج 4 + 1؟",
            QuestionType = QuestionType.MultipleChoice,
            CorrectAnswer= "5",
            DisplayOrder = 1,
            Points       = 10,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.ExamQuestions.Add(examQ);
        await ctx.SaveChangesAsync();

        var examOpt = new ExamQuestionOption
        {
            QuestionId   = examQ.Id,
            OptionText   = "5",
            IsCorrect    = true,
            DisplayOrder = 1,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.ExamQuestionOptions.Add(examOpt);
        await ctx.SaveChangesAsync();

        var attempt = new StudentExamAttempt
        {
            EnrollmentId = firstEnr.Id,
            ExamId       = exam.Id,
            StartedAt    = now,
            SubmittedAt  = now.AddMinutes(12),
            Score        = 10,
            TotalScore   = 10,
            IsGraded     = true,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.StudentExamAttempts.Add(attempt);
        await ctx.SaveChangesAsync();

        ctx.StudentExamAnswers.Add(new StudentExamAnswer
        {
            AttemptId        = attempt.Id,
            QuestionId       = examQ.Id,
            AnswerText       = "5",
            SelectedOptionId = examOpt.Id,
            IsCorrect        = true,
            PointsEarned     = 10,
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        // =================================================================
        // 24. Conversation + Participants + Message + BlockedUser
        // =================================================================
        var conversation = new Conversation
        {
            Title         = "محادثة تجريبية",
            Type          = ConversationType.Direct,
            LastMessageAt = now,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.Conversations.Add(conversation);
        await ctx.SaveChangesAsync();

        var firstTeacher     = teacherUsers.First();
        var firstStudentUser = studentUsers.First();
        ctx.ConversationParticipants.AddRange(
            new ConversationParticipant
            {
                ConversationId = conversation.Id, UserId = firstTeacher.Id,
                JoinedAt = now, LastReadAt = now, CreatedAt = now, UpdatedAt = now
            },
            new ConversationParticipant
            {
                ConversationId = conversation.Id, UserId = firstStudentUser.Id,
                JoinedAt = now, CreatedAt = now, UpdatedAt = now
            }
        );
        ctx.Messages.Add(new Message
        {
            ConversationId = conversation.Id,
            SenderId       = firstTeacher.Id,
            Content        = "مرحباً، هل أتممت الواجب؟",
            SentAt         = now,
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        ctx.BlockedUsers.Add(new BlockedUser
        {
            BlockerId      = firstTeacher.Id,
            BlockedUserId  = firstStudentUser.Id,
            ConversationId = conversation.Id,
            CreatedAt      = now,
            UpdatedAt      = now
        });
        await ctx.SaveChangesAsync();

        // =================================================================
        // 25. Notifications
        // =================================================================
        ctx.Notifications.Add(new Notification
        {
            UserId   = firstStudentUser.Id,
            Title    = "واجب جديد",
            Body     = "تم إضافة واجب رياضيات جديد.",
            Type     = NotificationType.NewAssignment,
            DataJson = "{\"source\":\"seed\"}",
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        // =================================================================
        // 26. Announcements
        // =================================================================
        ctx.Announcements.AddRange(
            new Announcement
            {
                AuthorId = admin.Id,
                Title    = "مرحباً بكم في العام الدراسي 2025/2026",
                Body     = "يسعد إدارة المدرسة أن ترحب بجميع الطلاب وأولياء الأمور. نتمنى للجميع عاماً دراسياً موفقاً ومثمراً.",
                TargetRole = null,
                ExpiresAt  = new DateTime(2025, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt  = new DateTime(2025,  9,21, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAt  = now
            },
            new Announcement
            {
                AuthorId = admin.Id,
                Title    = "موعد امتحانات نصف العام",
                Body     = "تُعلن الإدارة عن بدء امتحانات نصف العام في 15 يناير 2026. يُرجى الاطلاع على الجدول المعتمد.",
                TargetRole = null,
                ExpiresAt  = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt  = new DateTime(2026, 1,  5, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAt  = now
            },
            new Announcement
            {
                AuthorId   = admin.Id,
                Title      = "تنبيه للمعلمين: رفع الدرجات",
                Body       = "يُرجى من جميع المعلمين الانتهاء من رفع درجات الفصل الأول قبل 20 يناير 2026.",
                TargetRole = UserRole.Teacher,
                ExpiresAt  = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt  = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAt  = now
            },
            new Announcement
            {
                AuthorId   = admin.Id,
                Title      = "نتائج الفصل الأول متاحة",
                Body       = "يمكن لأولياء الأمور الاطلاع على نتائج الفصل الأول لأبنائهم من خلال تطبيق SchoolLink.",
                TargetRole = UserRole.Parent,
                ExpiresAt  = new DateTime(2026, 3,  1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt  = new DateTime(2026, 1, 22, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAt  = now
            }
        );
        await ctx.SaveChangesAsync();

        // =================================================================
        // 27. LibraryItems
        // =================================================================
        ctx.LibraryItems.AddRange(
            new LibraryItem
            {
                Title        = "سجل الرصد",
                ItemType     = LibraryItemType.File,
                FileUrl      = "https://dl.dropboxusercontent.com/scl/fi/bma8k570mpck582jg1wuh/.docx?rlkey=1he3059cyyvw1gnf4cy0kqrqe&dl=0",
                UploadedById = admin.Id,
                IsActive     = true,
                CreatedAt    = new DateTime(2026, 6, 7, 21, 0, 20, DateTimeKind.Utc),
                UpdatedAt    = now
            },
            new LibraryItem
            {
                Title        = "تجربه",
                ItemType     = LibraryItemType.Video,
                FileUrl      = "https://www.youtube.com/watch?v=bythRt5CIkI",
                SubjectId    = S["SCI"].Id,
                UploadedById = admin.Id,
                IsActive     = true,
                CreatedAt    = new DateTime(2026, 6, 7, 21, 3, 0, DateTimeKind.Utc),
                UpdatedAt    = now
            }
        );
        await ctx.SaveChangesAsync();

        // =================================================================
        // 28. ResultVisibilitySettings
        // =================================================================
        ctx.ResultVisibilitySettings.AddRange(
            new ResultVisibilitySetting
            {
                AcademicYearId = year.Id, Term = AcademicTerm.FirstSemester,
                IsVisible      = true,
                VisibleFrom    = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
                VisibleUntil   = new DateTime(2026, 3,  1, 0, 0, 0, DateTimeKind.Utc),
                ControlledById = admin.Id, CreatedAt = now, UpdatedAt = now
            },
            new ResultVisibilitySetting
            {
                AcademicYearId = year.Id, Term = AcademicTerm.SecondSemester,
                IsVisible      = false,
                ControlledById = admin.Id, CreatedAt = now, UpdatedAt = now
            },
            new ResultVisibilitySetting
            {
                AcademicYearId = year.Id, Term = AcademicTerm.Final,
                IsVisible      = false,
                ControlledById = admin.Id, CreatedAt = now, UpdatedAt = now
            }
        );
        await ctx.SaveChangesAsync();

        // =================================================================
        // 29. RefreshToken + EmailOtp
        // =================================================================
        ctx.RefreshTokens.Add(new RefreshToken
        {
            UserId    = admin.Id,
            Token     = $"seed-{Guid.NewGuid():N}",
            ExpiresAt = now.AddDays(7),
            CreatedAt = now, UpdatedAt = now
        });
        ctx.EmailOtps.Add(new EmailOtp
        {
            UserId       = admin.Id,
            Email        = admin.ContactEmail!,
            CodeHash     = BCrypt.Net.BCrypt.HashPassword("123456"),
            ExpiresAt    = now.AddMinutes(10),
            UsedAt       = now,
            AttemptCount = 0,
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        // =================================================================
        // 30. StudyPlan + StudyPlanItem
        // =================================================================
        var studyPlan = new StudyPlan
        {
            EnrollmentId  = firstEnr.Id,
            GeneratedByAI = false,
            StartDate     = DateOnly.FromDateTime(now),
            EndDate       = DateOnly.FromDateTime(now.AddDays(14)),
            IsActive      = true,
            RestDay       = 5,
            CreatedAt = now, UpdatedAt = now
        };
        ctx.StudyPlans.Add(studyPlan);
        await ctx.SaveChangesAsync();

        ctx.StudyPlanItems.Add(new StudyPlanItem
        {
            StudyPlanId = studyPlan.Id,
            SubjectId   = S["MTH"].Id,
            DayOfWeek   = 0,
            StartTime   = new TimeOnly(17, 0),
            EndTime     = new TimeOnly(18, 0),
            Topic       = "مراجعة المعادلات التربيعية",
            Notes       = "حل تمارين الكتاب ص 45-50.",
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        // =================================================================
        // 31. LessonFeedback
        // =================================================================
        ctx.LessonFeedbacks.Add(new LessonFeedback
        {
            EnrollmentId          = firstEnr.Id,
            ClassSubjectTeacherId = mathCstClass1.Id,
            LessonDate            = DateOnly.FromDateTime(now),
            Rating                = 5,
            Understanding         = LessonUnderstanding.Yes,
            Comment               = "الشرح كان واضحاً ومفيداً.",
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        // =================================================================
        // 32. AIGenerationLog
        // =================================================================
        ctx.AIGenerationLogs.Add(new AIGenerationLog
        {
            UserId        = firstTeacher.Id,
            OperationType = "SeedPreview",
            InputSummary  = "بيانات تجريبية للتأكد من عمل سجل عمليات الذكاء الاصطناعي.",
            IsSuccess     = true,
            TokensUsed    = 120,
            LatencyMs     = 450,
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();

        // =================================================================
        // 33. AgentConversationMessage
        // =================================================================
        ctx.AgentConversationMessages.Add(new AgentConversationMessage
        {
            ConversationId = $"seed-agent-{Guid.NewGuid():N}",
            Sender         = "Teacher",
            Content        = "رسالة تجريبية لمساعد المعلم الذكي.",
            AgentType      = "TeacherAssistant",
            Timestamp      = now,
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    /// <summary>Builds EvaluationItem list from a compact definition array.</summary>
    private static List<EvaluationItem> BuildItems(
        int templateId,
        DateTime now,
        (string name, decimal max, ItemType type, AutoCalcType auto)[] defs)
        => defs.Select((d, i) => new EvaluationItem
        {
            TemplateId   = templateId,
            Name         = d.name,
            MaxScore     = d.max,
            Weight       = 1,
            ItemType     = d.type,
            AutoCalcType = d.auto,
            DisplayOrder = i + 1,
            IsVisible    = true,
            CreatedAt = now, UpdatedAt = now
        }).ToList();

    /// <summary>Seeds Units and Lessons for all 7 subjects.</summary>
    private static async Task SeedUnitsAndLessons(
        AppDbContext ctx,
        Dictionary<string, Subject> S,
        DateTime now)
    {
        // Helper: add a unit with optional lessons
        async Task<Unit> AddUnit(Subject subject, string name, int order,
                                  (string title, string content, int dispOrder)[]? lessons = null)
        {
            var unit = new Unit
            {
                SubjectId    = subject.Id,
                Name         = name,
                DisplayOrder = order,
                CreatedAt    = now, UpdatedAt = now
            };
            ctx.Units.Add(unit);
            await ctx.SaveChangesAsync();

            if (lessons != null)
            {
                ctx.Lessons.AddRange(lessons.Select(l => new Lesson
                {
                    UnitId       = unit.Id,
                    Title        = l.title,
                    Content      = l.content,
                    DisplayOrder = l.dispOrder,
                    CreatedAt    = now, UpdatedAt = now
                }));
                await ctx.SaveChangesAsync();
            }
            return unit;
        }

        // ── الرياضيات ──────────────────────────────────────────────────────
        await AddUnit(S["MTH"], "الوحدة الأولى: الأعداد والجبر", 1, new[]
        {
            ("المعادلات التربيعية",
             "المعادلة التربيعية: ax²+bx+c=0 حيث a≠0.\nالقانون العام: x = (-b ± √(b²-4ac)) / 2a\nالمميز = b²-4ac.",
             1),
            ("الدوال الخطية",
             "الدالة الخطية: f(x)=mx+b\nم = الميل، ب = المقطع الصادي.\nميل الخط = (y₂-y₁)/(x₂-x₁).",
             2),
        });
        await AddUnit(S["MTH"], "الوحدة الثانية: الهندسة", 2, new[]
        {
            ("نظرية فيثاغورس",    "في المثلث القائم: a²+b²=c² حيث c هو الوتر.", 1),
            ("مساحات الأشكال",    "مساحة المثلث = ½×القاعدة×الارتفاع.\nمساحة الدائرة = π×r².", 2),
        });

        // ── العلوم ─────────────────────────────────────────────────────────
        await AddUnit(S["SCI"], "الوحدة الأولى: الكيمياء", 1, new[]
        {
            ("المادة وخصائصها",    "المادة: كل ما له كتلة ويشغل حيزاً. توجد صلبة وسائلة وغازية.", 1),
            ("التفاعلات الكيميائية","التفاعل الكيميائي: تحوّل مادة إلى مادة جديدة بخصائص مختلفة.", 2),
        });
        await AddUnit(S["SCI"], "الوحدة الثانية: الأحياء", 2, new[]
        {
            ("الميتوز والميوز",
             "الميتوز: ينتج خليتين (2n).\nالميوز: ينتج أربع خلايا (n).",
             1),
        });

        // ── اللغة العربية ──────────────────────────────────────────────────
        await AddUnit(S["ARB"], "الوحدة الأولى: القراءة والنصوص", 1, new[]
        {
            ("النصوص الأدبية", "النص الأدبي عمل فني يهدف إلى التعبير عن المشاعر بلغة جمالية.", 1),
        });
        await AddUnit(S["ARB"], "الوحدة الثانية: النحو والصرف", 2, new[]
        {
            ("أقسام الكلام",   "الكلمة تنقسم إلى: اسم، فعل، حرف.", 1),
            ("المبتدأ والخبر", "المبتدأ: اسم مرفوع في أول الجملة الاسمية.\nالخبر: ما يتمّ به المعنى مع المبتدأ.", 2),
        });

        // ── اللغة الإنجليزية ───────────────────────────────────────────────
        await AddUnit(S["ENG"], "Unit 1: Greetings and Introductions", 1, new[]
        {
            ("Hello and Goodbye", "Greetings: Hello, Hi, Good morning, Goodbye, Bye.", 1),
        });
        await AddUnit(S["ENG"], "Unit 2: Daily Routines",    2);
        await AddUnit(S["ENG"], "Unit 3: Food and Drinks",   3);
        await AddUnit(S["ENG"], "Unit 4: Travel and Tourism",4);

        // ── الدراسات الاجتماعية ─────────────────────────────────────────────
        await AddUnit(S["SOC"], "الوحدة الأولى: الجغرافيا", 1, new[]
        {
            ("الموقع الجغرافي لمصر",
             "تقع مصر في شمال شرق أفريقيا، يحدها شمالاً البحر المتوسط وشرقاً البحر الأحمر.",
             1),
        });
        await AddUnit(S["SOC"], "الوحدة الثانية: التاريخ", 2, new[]
        {
            ("الحضارة المصرية القديمة",
             "تُعدّ الحضارة المصرية القديمة من أقدم الحضارات وتتميز ببناء الأهرامات.",
             1),
        });

        // ── التربية الإسلامية ───────────────────────────────────────────────
        await AddUnit(S["ISL"], "الوحدة الأولى: القرآن الكريم", 1, new[]
        {
            ("سورة الفاتحة", "سورة الفاتحة أعظم سورة في القرآن وتُسمى السبع المثاني.", 1),
        });
        await AddUnit(S["ISL"], "الوحدة الثانية: الحديث النبوي", 2, new[]
        {
            ("أحاديث الرحمة", "الرحماء يرحمهم الرحمن، ارحموا من في الأرض يرحمكم من في السماء.", 1),
        });

        // ── الحاسب الآلي ────────────────────────────────────────────────────
        await AddUnit(S["CMP"], "الوحدة الأولى: أساسيات الحاسب", 1, new[]
        {
            ("مكونات الحاسب", "الحاسب يتكون من: وحدة معالجة، ذاكرة، أجهزة إدخال وإخراج.", 1),
        });
        await AddUnit(S["CMP"], "الوحدة الثانية: البرمجة", 2, new[]
        {
            ("مقدمة في البرمجة", "البرمجة: كتابة تعليمات للحاسب باستخدام لغة برمجة.", 1),
            ("المتغيرات",        "المتغير: مكان في الذاكرة يُخزّن فيه قيمة يمكن تغييرها.", 2),
        });
    }
}
