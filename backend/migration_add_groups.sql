IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AcademicYears] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(20) NOT NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NOT NULL,
    [IsCurrent] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AcademicYears] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Conversations] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NULL,
    [Type] int NOT NULL,
    [LastMessageAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [GradeLevels] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [LevelOrder] int NOT NULL,
    [Stage] nvarchar(50) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_GradeLevels] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SchoolProfiles] (
    [Id] int NOT NULL IDENTITY,
    [SchoolName] nvarchar(200) NOT NULL,
    [Governorate] nvarchar(100) NOT NULL,
    [Directorate] nvarchar(150) NOT NULL,
    [EducationalAdministration] nvarchar(150) NOT NULL,
    [Address] nvarchar(300) NULL,
    [Phone] nvarchar(30) NULL,
    [Email] nvarchar(150) NULL,
    [ManagerName] nvarchar(150) NULL,
    [LogoPath] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SchoolProfiles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Subjects] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(10) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [Role] int NOT NULL,
    [IsActive] bit NOT NULL,
    [ProfilePictureUrl] nvarchar(500) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [EvaluationPeriods] (
    [Id] int NOT NULL IDENTITY,
    [AcademicYearId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [PeriodType] int NOT NULL,
    [OrderNum] int NOT NULL,
    [StartDate] date NULL,
    [EndDate] date NULL,
    [MonthName] nvarchar(20) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EvaluationPeriods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EvaluationPeriods_AcademicYears_AcademicYearId] FOREIGN KEY ([AcademicYearId]) REFERENCES [AcademicYears] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Classes] (
    [Id] int NOT NULL IDENTITY,
    [GradeLevelId] int NOT NULL,
    [AcademicYearId] int NOT NULL,
    [Name] nvarchar(20) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Classes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Classes_AcademicYears_AcademicYearId] FOREIGN KEY ([AcademicYearId]) REFERENCES [AcademicYears] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Classes_GradeLevels_GradeLevelId] FOREIGN KEY ([GradeLevelId]) REFERENCES [GradeLevels] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [EvaluationTemplates] (
    [Id] int NOT NULL IDENTITY,
    [GradeLevelId] int NOT NULL,
    [SubjectId] int NOT NULL,
    [AcademicYearId] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [CalculationType] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EvaluationTemplates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EvaluationTemplates_AcademicYears_AcademicYearId] FOREIGN KEY ([AcademicYearId]) REFERENCES [AcademicYears] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EvaluationTemplates_GradeLevels_GradeLevelId] FOREIGN KEY ([GradeLevelId]) REFERENCES [GradeLevels] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EvaluationTemplates_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AIGenerationLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [OperationType] nvarchar(50) NOT NULL,
    [InputSummary] nvarchar(1000) NULL,
    [IsSuccess] bit NOT NULL,
    [TokensUsed] int NULL,
    [LatencyMs] int NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AIGenerationLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AIGenerationLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ConversationParticipants] (
    [Id] int NOT NULL IDENTITY,
    [ConversationId] int NOT NULL,
    [UserId] int NOT NULL,
    [JoinedAt] datetime2 NOT NULL,
    [LastReadAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ConversationParticipants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ConversationParticipants_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ConversationParticipants_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LibraryItems] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(300) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [ItemType] int NOT NULL,
    [FileUrl] nvarchar(500) NULL,
    [SubjectId] int NULL,
    [GradeLevelId] int NULL,
    [AcademicYearId] int NULL,
    [UploadedById] int NOT NULL,
    [IsActive] bit NOT NULL,
    [FileSizeBytes] bigint NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_LibraryItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LibraryItems_AcademicYears_AcademicYearId] FOREIGN KEY ([AcademicYearId]) REFERENCES [AcademicYears] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LibraryItems_GradeLevels_GradeLevelId] FOREIGN KEY ([GradeLevelId]) REFERENCES [GradeLevels] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LibraryItems_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LibraryItems_Users_UploadedById] FOREIGN KEY ([UploadedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Messages] (
    [Id] int NOT NULL IDENTITY,
    [ConversationId] int NOT NULL,
    [SenderId] int NOT NULL,
    [Content] nvarchar(4000) NOT NULL,
    [AttachmentUrl] nvarchar(500) NULL,
    [AttachmentType] nvarchar(20) NULL,
    [SentAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Messages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Body] nvarchar(2000) NOT NULL,
    [Type] int NOT NULL,
    [IsRead] bit NOT NULL,
    [DataJson] nvarchar(1000) NULL,
    [ReadAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Token] nvarchar(500) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [RevokedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ResultVisibilitySettings] (
    [Id] int NOT NULL IDENTITY,
    [AcademicYearId] int NOT NULL,
    [Term] int NOT NULL,
    [IsVisible] bit NOT NULL,
    [VisibleFrom] datetime2 NULL,
    [VisibleUntil] datetime2 NULL,
    [ControlledById] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ResultVisibilitySettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ResultVisibilitySettings_AcademicYears_AcademicYearId] FOREIGN KEY ([AcademicYearId]) REFERENCES [AcademicYears] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ResultVisibilitySettings_Users_ControlledById] FOREIGN KEY ([ControlledById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Students] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [NationalId] nvarchar(14) NULL,
    [Gender] int NULL,
    [BirthDate] date NULL,
    [UserId] int NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Students_NationalId_14Digits] CHECK ([NationalId] IS NULL OR (LEN([NationalId]) = 14 AND [NationalId] NOT LIKE '%[^0-9]%')),
    CONSTRAINT [FK_Students_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Announcements] (
    [Id] int NOT NULL IDENTITY,
    [AuthorId] int NOT NULL,
    [Title] nvarchar(300) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [TargetRole] int NULL,
    [TargetClassId] int NULL,
    [ExpiresAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Announcements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Announcements_Classes_TargetClassId] FOREIGN KEY ([TargetClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Announcements_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ClassSubjectTeachers] (
    [Id] int NOT NULL IDENTITY,
    [ClassId] int NOT NULL,
    [SubjectId] int NOT NULL,
    [TeacherId] int NOT NULL,
    [AcademicYearId] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClassSubjectTeachers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClassSubjectTeachers_AcademicYears_AcademicYearId] FOREIGN KEY ([AcademicYearId]) REFERENCES [AcademicYears] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassSubjectTeachers_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassSubjectTeachers_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassSubjectTeachers_Users_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Timetables] (
    [Id] int NOT NULL IDENTITY,
    [ClassId] int NOT NULL,
    [AcademicYearId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Timetables] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Timetables_AcademicYears_AcademicYearId] FOREIGN KEY ([AcademicYearId]) REFERENCES [AcademicYears] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Timetables_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [EvaluationItems] (
    [Id] int NOT NULL IDENTITY,
    [TemplateId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [MaxScore] decimal(5,2) NOT NULL,
    [Weight] decimal(4,2) NOT NULL,
    [ItemType] int NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsVisible] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EvaluationItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EvaluationItems_EvaluationTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [EvaluationTemplates] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ParentStudents] (
    [Id] int NOT NULL IDENTITY,
    [ParentId] int NOT NULL,
    [StudentId] int NOT NULL,
    [Relationship] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ParentStudents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ParentStudents_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ParentStudents_Users_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [StudentEnrollments] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [ClassId] int NOT NULL,
    [AcademicYearId] int NOT NULL,
    [EnrolledAt] date NOT NULL,
    [LeftAt] date NULL,
    [TransferReason] nvarchar(500) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentEnrollments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentEnrollments_AcademicYears_AcademicYearId] FOREIGN KEY ([AcademicYearId]) REFERENCES [AcademicYears] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentEnrollments_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentEnrollments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Assignments] (
    [Id] int NOT NULL IDENTITY,
    [ClassSubjectTeacherId] int NOT NULL,
    [Title] nvarchar(300) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [DueDate] datetime2 NULL,
    [MaxScore] decimal(5,2) NOT NULL,
    [IsAutoGraded] bit NOT NULL,
    [IsAIGenerated] bit NOT NULL,
    [Category] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Assignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Assignments_ClassSubjectTeachers_ClassSubjectTeacherId] FOREIGN KEY ([ClassSubjectTeacherId]) REFERENCES [ClassSubjectTeachers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Exams] (
    [Id] int NOT NULL IDENTITY,
    [ClassSubjectTeacherId] int NOT NULL,
    [Title] nvarchar(300) NOT NULL,
    [StartTime] datetime2 NULL,
    [EndTime] datetime2 NULL,
    [DurationMinutes] int NULL,
    [TotalScore] decimal(5,2) NOT NULL,
    [IsAIGenerated] bit NOT NULL,
    [IsPublished] bit NOT NULL,
    [Category] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Exams] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Exams_ClassSubjectTeachers_ClassSubjectTeacherId] FOREIGN KEY ([ClassSubjectTeacherId]) REFERENCES [ClassSubjectTeachers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [TimetableSlots] (
    [Id] int NOT NULL IDENTITY,
    [TimetableId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [PeriodNumber] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [ClassSubjectTeacherId] int NULL,
    [IsBreak] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TimetableSlots] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TimetableSlots_ClassSubjectTeachers_ClassSubjectTeacherId] FOREIGN KEY ([ClassSubjectTeacherId]) REFERENCES [ClassSubjectTeachers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TimetableSlots_Timetables_TimetableId] FOREIGN KEY ([TimetableId]) REFERENCES [Timetables] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [DailyAbsences] (
    [Id] int NOT NULL IDENTITY,
    [EnrollmentId] int NOT NULL,
    [ClassSubjectTeacherId] int NULL,
    [AbsenceDate] date NOT NULL,
    [PeriodId] int NULL,
    [IsAbsent] bit NOT NULL,
    [Reason] nvarchar(300) NULL,
    [RecordedById] int NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DailyAbsences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DailyAbsences_ClassSubjectTeachers_ClassSubjectTeacherId] FOREIGN KEY ([ClassSubjectTeacherId]) REFERENCES [ClassSubjectTeachers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DailyAbsences_EvaluationPeriods_PeriodId] FOREIGN KEY ([PeriodId]) REFERENCES [EvaluationPeriods] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DailyAbsences_StudentEnrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [StudentEnrollments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DailyAbsences_Users_RecordedById] FOREIGN KEY ([RecordedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [FinalGrades] (
    [Id] int NOT NULL IDENTITY,
    [EnrollmentId] int NOT NULL,
    [PeriodAvgScore] decimal(5,2) NOT NULL,
    [Assessment1Score] decimal(5,2) NOT NULL,
    [Assessment2Score] decimal(5,2) NOT NULL,
    [WrittenTotal] decimal(5,2) NOT NULL,
    [FinalExamScore] decimal(5,2) NOT NULL,
    [Total] decimal(5,2) NOT NULL,
    [IsPublished] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FinalGrades] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FinalGrades_StudentEnrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [StudentEnrollments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LessonFeedbacks] (
    [Id] int NOT NULL IDENTITY,
    [EnrollmentId] int NOT NULL,
    [ClassSubjectTeacherId] int NOT NULL,
    [LessonDate] date NOT NULL,
    [Rating] int NOT NULL,
    [Understanding] int NOT NULL,
    [Comment] nvarchar(1000) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_LessonFeedbacks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LessonFeedbacks_ClassSubjectTeachers_ClassSubjectTeacherId] FOREIGN KEY ([ClassSubjectTeacherId]) REFERENCES [ClassSubjectTeachers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LessonFeedbacks_StudentEnrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [StudentEnrollments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [PeriodAverages] (
    [Id] int NOT NULL IDENTITY,
    [EnrollmentId] int NOT NULL,
    [PeriodId] int NOT NULL,
    [AvgScore] decimal(5,2) NOT NULL,
    [MaxScore] decimal(5,2) NOT NULL,
    [CalculatedAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PeriodAverages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PeriodAverages_EvaluationPeriods_PeriodId] FOREIGN KEY ([PeriodId]) REFERENCES [EvaluationPeriods] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PeriodAverages_StudentEnrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [StudentEnrollments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [PeriodicAssessments] (
    [Id] int NOT NULL IDENTITY,
    [EnrollmentId] int NOT NULL,
    [AssessmentType] int NOT NULL,
    [Score] decimal(5,2) NOT NULL,
    [MaxScore] decimal(5,2) NOT NULL,
    [AssessmentDate] date NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PeriodicAssessments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PeriodicAssessments_StudentEnrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [StudentEnrollments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [StudentEvaluations] (
    [Id] int NOT NULL IDENTITY,
    [EnrollmentId] int NOT NULL,
    [EvaluationItemId] int NOT NULL,
    [PeriodId] int NOT NULL,
    [Score] decimal(5,2) NULL,
    [EnteredById] int NOT NULL,
    [EnteredAt] datetime2 NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentEvaluations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentEvaluations_EvaluationItems_EvaluationItemId] FOREIGN KEY ([EvaluationItemId]) REFERENCES [EvaluationItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentEvaluations_EvaluationPeriods_PeriodId] FOREIGN KEY ([PeriodId]) REFERENCES [EvaluationPeriods] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentEvaluations_StudentEnrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [StudentEnrollments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentEvaluations_Users_EnteredById] FOREIGN KEY ([EnteredById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [StudyPlans] (
    [Id] int NOT NULL IDENTITY,
    [EnrollmentId] int NOT NULL,
    [GeneratedByAI] bit NOT NULL,
    [AIPromptSummary] nvarchar(500) NULL,
    [StartDate] date NOT NULL,
    [EndDate] date NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudyPlans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudyPlans_StudentEnrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [StudentEnrollments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AssignmentQuestions] (
    [Id] int NOT NULL IDENTITY,
    [AssignmentId] int NOT NULL,
    [QuestionText] nvarchar(2000) NOT NULL,
    [QuestionType] int NOT NULL,
    [ImageUrl] nvarchar(500) NULL,
    [CorrectAnswer] nvarchar(1000) NULL,
    [DisplayOrder] int NOT NULL,
    [Points] decimal(4,2) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AssignmentQuestions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssignmentQuestions_Assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [Assignments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [StudentAssignmentSubmissions] (
    [Id] int NOT NULL IDENTITY,
    [EnrollmentId] int NOT NULL,
    [AssignmentId] int NOT NULL,
    [SubmittedAt] datetime2 NOT NULL,
    [Score] decimal(5,2) NULL,
    [MaxScore] decimal(5,2) NOT NULL,
    [IsGraded] bit NOT NULL,
    [AIFeedback] nvarchar(3000) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentAssignmentSubmissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentAssignmentSubmissions_Assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [Assignments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentAssignmentSubmissions_StudentEnrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [StudentEnrollments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ExamQuestions] (
    [Id] int NOT NULL IDENTITY,
    [ExamId] int NOT NULL,
    [QuestionText] nvarchar(2000) NOT NULL,
    [QuestionType] int NOT NULL,
    [CorrectAnswer] nvarchar(1000) NULL,
    [ImageUrl] nvarchar(500) NULL,
    [DisplayOrder] int NOT NULL,
    [Points] decimal(4,2) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ExamQuestions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExamQuestions_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [StudentExamAttempts] (
    [Id] int NOT NULL IDENTITY,
    [EnrollmentId] int NOT NULL,
    [ExamId] int NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [SubmittedAt] datetime2 NULL,
    [Score] decimal(5,2) NULL,
    [TotalScore] decimal(5,2) NOT NULL,
    [IsGraded] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentExamAttempts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentExamAttempts_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentExamAttempts_StudentEnrollments_EnrollmentId] FOREIGN KEY ([EnrollmentId]) REFERENCES [StudentEnrollments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [StudyPlanItems] (
    [Id] int NOT NULL IDENTITY,
    [StudyPlanId] int NOT NULL,
    [SubjectId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [Topic] nvarchar(300) NULL,
    [Notes] nvarchar(500) NULL,
    [IsCompleted] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudyPlanItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudyPlanItems_StudyPlans_StudyPlanId] FOREIGN KEY ([StudyPlanId]) REFERENCES [StudyPlans] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudyPlanItems_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AssignmentQuestionOptions] (
    [Id] int NOT NULL IDENTITY,
    [QuestionId] int NOT NULL,
    [OptionText] nvarchar(500) NOT NULL,
    [IsCorrect] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AssignmentQuestionOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssignmentQuestionOptions_AssignmentQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [AssignmentQuestions] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ExamQuestionOptions] (
    [Id] int NOT NULL IDENTITY,
    [QuestionId] int NOT NULL,
    [OptionText] nvarchar(500) NOT NULL,
    [IsCorrect] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ExamQuestionOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExamQuestionOptions_ExamQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [ExamQuestions] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [StudentAssignmentAnswers] (
    [Id] int NOT NULL IDENTITY,
    [SubmissionId] int NOT NULL,
    [QuestionId] int NOT NULL,
    [AnswerText] nvarchar(max) NULL,
    [SelectedOptionId] int NULL,
    [BooleanAnswer] bit NULL,
    [IsCorrect] bit NULL,
    [PointsEarned] decimal(4,2) NOT NULL,
    [AIFeedback] nvarchar(2000) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentAssignmentAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentAssignmentAnswers_AssignmentQuestionOptions_SelectedOptionId] FOREIGN KEY ([SelectedOptionId]) REFERENCES [AssignmentQuestionOptions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentAssignmentAnswers_AssignmentQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [AssignmentQuestions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentAssignmentAnswers_StudentAssignmentSubmissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [StudentAssignmentSubmissions] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [StudentExamAnswers] (
    [Id] int NOT NULL IDENTITY,
    [AttemptId] int NOT NULL,
    [QuestionId] int NOT NULL,
    [AnswerText] nvarchar(max) NULL,
    [SelectedOptionId] int NULL,
    [BooleanAnswer] bit NULL,
    [IsCorrect] bit NULL,
    [PointsEarned] decimal(4,2) NOT NULL,
    [AIFeedback] nvarchar(2000) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentExamAnswers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentExamAnswers_ExamQuestionOptions_SelectedOptionId] FOREIGN KEY ([SelectedOptionId]) REFERENCES [ExamQuestionOptions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentExamAnswers_ExamQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [ExamQuestions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentExamAnswers_StudentExamAttempts_AttemptId] FOREIGN KEY ([AttemptId]) REFERENCES [StudentExamAttempts] ([Id]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [IX_AcademicYears_Name] ON [AcademicYears] ([Name]);
GO

CREATE INDEX [IX_AIGenerationLogs_UserId] ON [AIGenerationLogs] ([UserId]);
GO

CREATE INDEX [IX_Announcements_AuthorId] ON [Announcements] ([AuthorId]);
GO

CREATE INDEX [IX_Announcements_TargetClassId] ON [Announcements] ([TargetClassId]);
GO

CREATE INDEX [IX_AssignmentQuestionOptions_QuestionId] ON [AssignmentQuestionOptions] ([QuestionId]);
GO

CREATE INDEX [IX_AssignmentQuestions_AssignmentId] ON [AssignmentQuestions] ([AssignmentId]);
GO

CREATE INDEX [IX_Assignments_ClassSubjectTeacherId] ON [Assignments] ([ClassSubjectTeacherId]);
GO

CREATE INDEX [IX_Classes_AcademicYearId] ON [Classes] ([AcademicYearId]);
GO

CREATE UNIQUE INDEX [IX_Classes_GradeLevelId_AcademicYearId_Name] ON [Classes] ([GradeLevelId], [AcademicYearId], [Name]);
GO

CREATE INDEX [IX_ClassSubjectTeachers_AcademicYearId] ON [ClassSubjectTeachers] ([AcademicYearId]);
GO

CREATE UNIQUE INDEX [IX_ClassSubjectTeachers_ClassId_SubjectId_AcademicYearId] ON [ClassSubjectTeachers] ([ClassId], [SubjectId], [AcademicYearId]);
GO

CREATE INDEX [IX_ClassSubjectTeachers_SubjectId] ON [ClassSubjectTeachers] ([SubjectId]);
GO

CREATE INDEX [IX_ClassSubjectTeachers_TeacherId] ON [ClassSubjectTeachers] ([TeacherId]);
GO

CREATE UNIQUE INDEX [IX_ConversationParticipants_ConversationId_UserId] ON [ConversationParticipants] ([ConversationId], [UserId]);
GO

CREATE INDEX [IX_ConversationParticipants_UserId] ON [ConversationParticipants] ([UserId]);
GO

CREATE INDEX [IX_DailyAbsences_ClassSubjectTeacherId] ON [DailyAbsences] ([ClassSubjectTeacherId]);
GO

CREATE UNIQUE INDEX [IX_DailyAbsences_EnrollmentId_ClassSubjectTeacherId_AbsenceDate] ON [DailyAbsences] ([EnrollmentId], [ClassSubjectTeacherId], [AbsenceDate]) WHERE [ClassSubjectTeacherId] IS NOT NULL;
GO

CREATE INDEX [IX_DailyAbsences_PeriodId] ON [DailyAbsences] ([PeriodId]);
GO

CREATE INDEX [IX_DailyAbsences_RecordedById] ON [DailyAbsences] ([RecordedById]);
GO

CREATE INDEX [IX_EvaluationItems_TemplateId] ON [EvaluationItems] ([TemplateId]);
GO

CREATE INDEX [IX_EvaluationPeriods_AcademicYearId] ON [EvaluationPeriods] ([AcademicYearId]);
GO

CREATE INDEX [IX_EvaluationTemplates_AcademicYearId] ON [EvaluationTemplates] ([AcademicYearId]);
GO

CREATE UNIQUE INDEX [IX_EvaluationTemplates_GradeLevelId_SubjectId_AcademicYearId] ON [EvaluationTemplates] ([GradeLevelId], [SubjectId], [AcademicYearId]);
GO

CREATE INDEX [IX_EvaluationTemplates_SubjectId] ON [EvaluationTemplates] ([SubjectId]);
GO

CREATE INDEX [IX_ExamQuestionOptions_QuestionId] ON [ExamQuestionOptions] ([QuestionId]);
GO

CREATE INDEX [IX_ExamQuestions_ExamId] ON [ExamQuestions] ([ExamId]);
GO

CREATE INDEX [IX_Exams_ClassSubjectTeacherId] ON [Exams] ([ClassSubjectTeacherId]);
GO

CREATE UNIQUE INDEX [IX_FinalGrades_EnrollmentId] ON [FinalGrades] ([EnrollmentId]);
GO

CREATE INDEX [IX_LessonFeedbacks_ClassSubjectTeacherId] ON [LessonFeedbacks] ([ClassSubjectTeacherId]);
GO

CREATE INDEX [IX_LessonFeedbacks_EnrollmentId] ON [LessonFeedbacks] ([EnrollmentId]);
GO

CREATE INDEX [IX_LibraryItems_AcademicYearId] ON [LibraryItems] ([AcademicYearId]);
GO

CREATE INDEX [IX_LibraryItems_GradeLevelId] ON [LibraryItems] ([GradeLevelId]);
GO

CREATE INDEX [IX_LibraryItems_SubjectId] ON [LibraryItems] ([SubjectId]);
GO

CREATE INDEX [IX_LibraryItems_UploadedById] ON [LibraryItems] ([UploadedById]);
GO

CREATE INDEX [IX_Messages_ConversationId_SentAt] ON [Messages] ([ConversationId], [SentAt]);
GO

CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
GO

CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
GO

CREATE UNIQUE INDEX [IX_ParentStudents_ParentId_StudentId] ON [ParentStudents] ([ParentId], [StudentId]);
GO

CREATE INDEX [IX_ParentStudents_StudentId] ON [ParentStudents] ([StudentId]);
GO

CREATE UNIQUE INDEX [IX_PeriodAverages_EnrollmentId_PeriodId] ON [PeriodAverages] ([EnrollmentId], [PeriodId]);
GO

CREATE INDEX [IX_PeriodAverages_PeriodId] ON [PeriodAverages] ([PeriodId]);
GO

CREATE UNIQUE INDEX [IX_PeriodicAssessments_EnrollmentId_AssessmentType] ON [PeriodicAssessments] ([EnrollmentId], [AssessmentType]);
GO

CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
GO

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_ResultVisibilitySettings_AcademicYearId_Term] ON [ResultVisibilitySettings] ([AcademicYearId], [Term]);
GO

CREATE INDEX [IX_ResultVisibilitySettings_ControlledById] ON [ResultVisibilitySettings] ([ControlledById]);
GO

CREATE INDEX [IX_SchoolProfiles_IsActive] ON [SchoolProfiles] ([IsActive]);
GO

CREATE INDEX [IX_StudentAssignmentAnswers_QuestionId] ON [StudentAssignmentAnswers] ([QuestionId]);
GO

CREATE INDEX [IX_StudentAssignmentAnswers_SelectedOptionId] ON [StudentAssignmentAnswers] ([SelectedOptionId]);
GO

CREATE UNIQUE INDEX [IX_StudentAssignmentAnswers_SubmissionId_QuestionId] ON [StudentAssignmentAnswers] ([SubmissionId], [QuestionId]);
GO

CREATE INDEX [IX_StudentAssignmentSubmissions_AssignmentId] ON [StudentAssignmentSubmissions] ([AssignmentId]);
GO

CREATE UNIQUE INDEX [IX_StudentAssignmentSubmissions_EnrollmentId_AssignmentId] ON [StudentAssignmentSubmissions] ([EnrollmentId], [AssignmentId]);
GO

CREATE INDEX [IX_StudentEnrollments_AcademicYearId] ON [StudentEnrollments] ([AcademicYearId]);
GO

CREATE INDEX [IX_StudentEnrollments_ClassId] ON [StudentEnrollments] ([ClassId]);
GO

CREATE INDEX [IX_StudentEnrollments_StudentId] ON [StudentEnrollments] ([StudentId]);
GO

CREATE UNIQUE INDEX [IX_StudentEvaluations_EnrollmentId_EvaluationItemId_PeriodId] ON [StudentEvaluations] ([EnrollmentId], [EvaluationItemId], [PeriodId]);
GO

CREATE INDEX [IX_StudentEvaluations_EnteredById] ON [StudentEvaluations] ([EnteredById]);
GO

CREATE INDEX [IX_StudentEvaluations_EvaluationItemId] ON [StudentEvaluations] ([EvaluationItemId]);
GO

CREATE INDEX [IX_StudentEvaluations_PeriodId] ON [StudentEvaluations] ([PeriodId]);
GO

CREATE UNIQUE INDEX [IX_StudentExamAnswers_AttemptId_QuestionId] ON [StudentExamAnswers] ([AttemptId], [QuestionId]);
GO

CREATE INDEX [IX_StudentExamAnswers_QuestionId] ON [StudentExamAnswers] ([QuestionId]);
GO

CREATE INDEX [IX_StudentExamAnswers_SelectedOptionId] ON [StudentExamAnswers] ([SelectedOptionId]);
GO

CREATE INDEX [IX_StudentExamAttempts_EnrollmentId] ON [StudentExamAttempts] ([EnrollmentId]);
GO

CREATE INDEX [IX_StudentExamAttempts_ExamId] ON [StudentExamAttempts] ([ExamId]);
GO

CREATE UNIQUE INDEX [IX_Students_NationalId] ON [Students] ([NationalId]) WHERE [NationalId] IS NOT NULL;
GO

CREATE INDEX [IX_Students_UserId] ON [Students] ([UserId]);
GO

CREATE INDEX [IX_StudyPlanItems_StudyPlanId] ON [StudyPlanItems] ([StudyPlanId]);
GO

CREATE INDEX [IX_StudyPlanItems_SubjectId] ON [StudyPlanItems] ([SubjectId]);
GO

CREATE INDEX [IX_StudyPlans_EnrollmentId] ON [StudyPlans] ([EnrollmentId]);
GO

CREATE INDEX [IX_Timetables_AcademicYearId] ON [Timetables] ([AcademicYearId]);
GO

CREATE INDEX [IX_Timetables_ClassId] ON [Timetables] ([ClassId]);
GO

CREATE INDEX [IX_TimetableSlots_ClassSubjectTeacherId] ON [TimetableSlots] ([ClassSubjectTeacherId]);
GO

CREATE UNIQUE INDEX [IX_TimetableSlots_TimetableId_DayOfWeek_PeriodNumber] ON [TimetableSlots] ([TimetableId], [DayOfWeek], [PeriodNumber]);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260602001042_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_TimetableSlots_TimetableId_DayOfWeek_PeriodNumber] ON [TimetableSlots];
GO

DROP INDEX [IX_ClassSubjectTeachers_ClassId_SubjectId_AcademicYearId] ON [ClassSubjectTeachers];
GO

DROP INDEX [IX_Classes_GradeLevelId_AcademicYearId_Name] ON [Classes];
GO

DROP INDEX [IX_AcademicYears_Name] ON [AcademicYears];
GO

ALTER TABLE [TimetableSlots] ADD [RoomId] int NULL;
GO

ALTER TABLE [EvaluationItems] ADD [AutoCalcType] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Assignments] ADD [IsPublished] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE TABLE [Rooms] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Type] int NOT NULL,
    [Capacity] int NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_TimetableSlots_RoomId_DayOfWeek_PeriodNumber] ON [TimetableSlots] ([RoomId], [DayOfWeek], [PeriodNumber]) WHERE [IsDeleted] = 0 AND [RoomId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_TimetableSlots_TimetableId_DayOfWeek_PeriodNumber] ON [TimetableSlots] ([TimetableId], [DayOfWeek], [PeriodNumber]) WHERE [IsDeleted] = 0;
GO

CREATE UNIQUE INDEX [IX_ClassSubjectTeachers_ClassId_SubjectId_AcademicYearId] ON [ClassSubjectTeachers] ([ClassId], [SubjectId], [AcademicYearId]) WHERE [IsDeleted] = 0;
GO

CREATE UNIQUE INDEX [IX_Classes_GradeLevelId_AcademicYearId_Name] ON [Classes] ([GradeLevelId], [AcademicYearId], [Name]) WHERE [IsDeleted] = 0;
GO

CREATE UNIQUE INDEX [IX_AcademicYears_Name] ON [AcademicYears] ([Name]) WHERE [IsDeleted] = 0;
GO

CREATE UNIQUE INDEX [IX_Rooms_Name_Type] ON [Rooms] ([Name], [Type]) WHERE [IsDeleted] = 0;
GO

ALTER TABLE [TimetableSlots] ADD CONSTRAINT [FK_TimetableSlots_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260603135652_FixUniqueIndexFilteredSoftDelete', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_ExamQuestions_ExamId] ON [ExamQuestions];
GO

DROP INDEX [IX_EvaluationTemplates_GradeLevelId_SubjectId_AcademicYearId] ON [EvaluationTemplates];
GO

ALTER TABLE [ExamQuestions] ADD [ContentText] nvarchar(max) NULL;
GO

ALTER TABLE [ExamQuestions] ADD [DisplayType] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [ExamQuestions] ADD [GroupId] int NULL;
GO

ALTER TABLE [EvaluationItems] ADD [AbsenceMaxScore] decimal(5,2) NULL;
GO

CREATE TABLE [ClassTemplateLinks] (
    [Id] int NOT NULL IDENTITY,
    [ClassId] int NOT NULL,
    [TemplateId] int NOT NULL,
    [AcademicYearId] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClassTemplateLinks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClassTemplateLinks_AcademicYears_AcademicYearId] FOREIGN KEY ([AcademicYearId]) REFERENCES [AcademicYears] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassTemplateLinks_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassTemplateLinks_EvaluationTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [EvaluationTemplates] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ExamQuestionGroups] (
    [Id] int NOT NULL IDENTITY,
    [ExamId] int NOT NULL,
    [DisplayType] int NOT NULL,
    [ContentTitle] nvarchar(300) NULL,
    [ContentText] nvarchar(max) NULL,
    [ImagePrompt] nvarchar(1000) NULL,
    [ImageUrl] nvarchar(500) NULL,
    [DisplayOrder] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ExamQuestionGroups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExamQuestionGroups_Exams_ExamId] FOREIGN KEY ([ExamId]) REFERENCES [Exams] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_ExamQuestions_ExamId_GroupId] ON [ExamQuestions] ([ExamId], [GroupId]);
GO

CREATE INDEX [IX_ExamQuestions_GroupId] ON [ExamQuestions] ([GroupId]);
GO

CREATE UNIQUE INDEX [IX_EvaluationTemplates_GradeLevelId_SubjectId_AcademicYearId] ON [EvaluationTemplates] ([GradeLevelId], [SubjectId], [AcademicYearId]) WHERE [IsDeleted] = 0;
GO

CREATE INDEX [IX_ClassTemplateLinks_AcademicYearId] ON [ClassTemplateLinks] ([AcademicYearId]);
GO

CREATE UNIQUE INDEX [IX_ClassTemplateLinks_ClassId_TemplateId_AcademicYearId] ON [ClassTemplateLinks] ([ClassId], [TemplateId], [AcademicYearId]) WHERE [IsDeleted] = 0;
GO

CREATE INDEX [IX_ClassTemplateLinks_TemplateId] ON [ClassTemplateLinks] ([TemplateId]);
GO

CREATE INDEX [IX_ExamQuestionGroups_ExamId] ON [ExamQuestionGroups] ([ExamId]);
GO

ALTER TABLE [ExamQuestions] ADD CONSTRAINT [FK_ExamQuestions_ExamQuestionGroups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [ExamQuestionGroups] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260605213944_AddExamQuestionGroupsAndDisplayType', N'8.0.0');
GO

COMMIT;
GO

