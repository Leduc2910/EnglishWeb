---
baseline_commit: 9a7dc1d
---

# Story 4.2: Reading/Listening Attempt Workspace

Status: done

## Story

LÃ  há»c sinh,
tÃ´i muá»‘n má»™t khÃ´ng gian lÃ m bÃ i á»•n Ä‘á»‹nh vá»›i PDF/audio vÃ  form tráº£ lá»i riÃªng biá»‡t,
Ä‘á»ƒ tÃ´i cÃ³ thá»ƒ hoÃ n thÃ nh bÃ i Ä‘á»c hoáº·c bÃ i nghe mÃ  há»‡ thá»‘ng khÃ´ng cáº§n phÃ¢n tÃ­ch ná»™i dung PDF.

## Acceptance Criteria

1. **Given** há»c sinh má»Ÿ má»™t bÃ i Homework hoáº·c Live Exam cÃ³ sáºµn (Reading hoáº·c Listening)
   **When** má»™t attempt Ä‘Æ°á»£c báº¯t Ä‘áº§u hoáº·c tiáº¿p tá»¥c
   **Then** Submission/Attempt tham chiáº¿u Ä‘Ãºng má»™t HomeworkAssignment hoáº·c má»™t LiveExamSession
   **And** DB/application validation ngÄƒn cáº£ hai null vÃ  cáº£ hai cÃ³ giÃ¡ trá»‹.

2. **Given** workspace táº£i xong
   **When** materials cÃ³ sáºµn
   **Then** trang hiá»ƒn thá»‹: tiÃªu Ä‘á» bÃ i thi, skill, active class, mode badge, vÃ¹ng autosave status (placeholder), PDF viewer, Ä‘iá»u khiá»ƒn trang PDF, audio player tÃ¹y chá»n (chá»‰ Listening), answer progress, cÃ¡c answer rows, nÃºt jump Ä‘áº¿n cÃ¢u chÆ°a tráº£ lá»i, vÃ  nÃºt Ná»™p bÃ i (placeholder cho story 4.4).

3. **Given** bÃ i thi lÃ  Listening vÃ  cÃ³ audio
   **When** há»c sinh phÃ¡t audio
   **Then** phÃ¡t qua endpoint streaming cÃ³ xÃ¡c thá»±c (khÃ´ng dÃ¹ng public URL)
   **And** audio player cÃ³ thá»ƒ Ä‘iá»u khiá»ƒn báº±ng bÃ n phÃ­m.

4. **Given** PDF hoáº·c audio khÃ´ng táº£i Ä‘Æ°á»£c hoáº·c file bá»‹ thiáº¿u
   **When** workspace táº£i hoáº·c phÃ¡t media
   **Then** hiá»ƒn thá»‹ thÃ´ng bÃ¡o lá»—i cÃ³ thá»ƒ phá»¥c há»“i
   **And** khÃ´ng Ä‘á»ƒ lá»™ storage path hay storage key.

5. **Given** há»c sinh chuyá»ƒn trang PDF
   **When** trang thay Ä‘á»•i
   **Then** answer panel giá»¯ nguyÃªn, khÃ´ng máº¥t cÃ¡c cÃ¢u tráº£ lá»i Ä‘Ã£ nháº­p.

6. **Given** workspace hiá»ƒn thá»‹ á»Ÿ Ä‘á»™ rá»™ng desktop vÃ  tablet
   **When** viewport thay Ä‘á»•i
   **Then** cÃ¡c action quan trá»ng vÃ  text khÃ´ng bá»‹ chá»“ng lÃªn nhau hoáº·c khÃ´ng thá»ƒ tiáº¿p cáº­n.

## Tasks / Subtasks

- [x] Task 1: Backend â€” Domain entities (AC1)
  - [x] 1.1 Táº¡o `src/EnglishTestWeb.Api/Domain/Submissions/Submission.cs` â€” entity vá»›i fields: Id (Guid), StudentId (string), HomeworkAssignmentId (Guid?), LiveExamSessionId (Guid?), AnswerKeyVersionId (Guid? â€” snapped táº¡i thá»i Ä‘iá»ƒm táº¡o), Status (string = "draft"), RowVersion (byte[]), CreatedAt (DateTimeOffset), UpdatedAt (DateTimeOffset); navigation props: `HomeworkAssignment?`, `LiveExamSession?`
  - [x] 1.2 Táº¡o `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionStatuses.cs` â€” constants: `Draft = "draft"`, `Submitted = "submitted"` (Submitted dÃ¹ng story 4.4)

- [x] Task 2: Backend â€” Contracts/DTOs (AC1, AC2)
  - [x] 2.1 Táº¡o `src/EnglishTestWeb.Api/Contracts/Submissions/CreateSubmissionRequest.cs`:
    ```csharp
    public sealed record CreateSubmissionRequest(Guid? HomeworkAssignmentId, Guid? LiveExamSessionId);
    ```
  - [x] 2.2 Táº¡o `src/EnglishTestWeb.Api/Contracts/Submissions/SubmissionDto.cs` â€” response cho POST:
    ```csharp
    public sealed record SubmissionDto(Guid Id, string Status, string Mode);
    ```
  - [x] 2.3 Táº¡o `src/EnglishTestWeb.Api/Contracts/Submissions/SubmissionWorkspaceDto.cs` â€” response cho GET workspace:
    - Id, Status, Mode, TemplateTitle, Skill, ClassId, ClassName
    - HomeworkAssignmentId (Guid?), LiveExamSessionId (Guid?)
    - DeadlineAt (DateTimeOffset?), TimeLimitMinutes (int?)
    - SessionOpenedAt (DateTimeOffset?), SessionClosedAt (DateTimeOffset?)
    - PdfMaterialId (Guid), AudioMaterialId (Guid?)
    - QuestionCount (int)
    - AnswerRows (IReadOnlyList\<AnswerRowDto\>) â€” rá»—ng trong story 4.2, sáº½ cÃ³ data tá»« story 4.3
  - [x] 2.4 Táº¡o `src/EnglishTestWeb.Api/Contracts/Submissions/AnswerRowDto.cs`:
    ```csharp
    public sealed record AnswerRowDto(int QuestionNumber, string? Answer);
    ```

- [x] Task 3: Backend â€” Application interface (AC1, AC2)
  - [x] 3.1 Táº¡o `src/EnglishTestWeb.Api/Application/Submissions/ISubmissionService.cs`:
    - `Task<CreateSubmissionResult> CreateOrResumeAsync(string studentId, Guid activeClassId, CreateSubmissionRequest request, CancellationToken ct)`
    - `Task<SubmissionWorkspaceDto?> GetWorkspaceAsync(Guid submissionId, string studentId, CancellationToken ct)`
  - [x] 3.2 Äá»‹nh nghÄ©a `CreateSubmissionResult` trong cÃ¹ng file:
    ```csharp
    public sealed record CreateSubmissionResult(bool Success, Guid? SubmissionId, string? ErrorCode, bool Created);
    ```
  - [x] 3.3 ThÃªm method vÃ o `IProtectedFileService`:
    ```csharp
    Task<ProtectedFileAccessResult> OpenForStudentWithSubmissionAsync(
        Guid fileId, string studentId, Guid submissionId, CancellationToken ct = default);
    ```

- [x] Task 4: Backend â€” Service implementation (AC1, AC2, AC3, AC4)
  - [x] 4.1 Táº¡o `src/EnglishTestWeb.Api/Infrastructure/Submissions/SubmissionService.cs`:
    - **CreateOrResumeAsync**: 
      - Validate: chá»‰ má»™t trong hai Id Ä‘Æ°á»£c set; khÃ´ng cáº£ hai null, khÃ´ng cáº£ hai cÃ³ giÃ¡ trá»‹ â†’ error code `submission.invalidSource`
      - Verify há»c sinh lÃ  member cá»§a class: gá»i `IClassAuthorizationService.RequireStudentClassAccessAsync(activeClassId, studentId)`
      - Verify source cÃ³ sáºµn: Homework â†’ `DeadlineAt >= now`; LiveExam â†’ `Status == "open"`
      - Náº¿u Ä‘Ã£ cÃ³ Draft submission cho (studentId, homeworkId) hoáº·c (studentId, sessionId): tráº£ vá» existing (idempotent)
      - Snap `AnswerKeyVersionId`: query `AnswerKeyVersions WHERE TemplateId = templateId AND Status = 'ready' ORDER BY UpdatedAt DESC FIRST` â†’ lÆ°u vÃ o Submission; null náº¿u khÃ´ng tÃ¬m tháº¥y (tiáº¿p tá»¥c â€” story 4.4 dÃ¹ng)
      - Táº¡o `Submission` vá»›i Status = "draft", save, return `Created = true`
    - **GetWorkspaceAsync**:
      - Query Submission JOIN HomeworkAssignment/LiveExamSession JOIN TestTemplate JOIN AnswerKeyVersion (optional)
      - Verify `Submission.StudentId == studentId`
      - Láº¥y PdfMaterialId: query `TestMaterials WHERE TemplateId = templateId AND IsActive = true AND Role = "pdf"` â†’ `StoredFileId`
      - Láº¥y AudioMaterialId: query `TestMaterials WHERE TemplateId = templateId AND IsActive = true AND Role = "audio"` â†’ `StoredFileId`; null náº¿u khÃ´ng cÃ³
      - Tráº£ vá» `SubmissionWorkspaceDto`; AnswerRows = `[]` (story 4.3 populate)
  - [x] 4.2 Implement `ProtectedFileService.OpenForStudentWithSubmissionAsync`:
    - Load Submission WHERE Id = submissionId AND StudentId = studentId
    - Náº¿u khÃ´ng tÃ¬m tháº¥y â†’ `NotAllowed, files.notFound`
    - Láº¥y templateId tá»« `Submission.HomeworkAssignment!.TestTemplateId` hoáº·c `Submission.LiveExamSession!.TestTemplateId` (Include nav props)
    - Query TestMaterials WHERE IsActive = true AND StoredFileId = fileId AND TemplateId = templateId
    - Náº¿u khÃ´ng tÃ¬m tháº¥y material â†’ `NotAllowed, files.notFound`
    - Stream file (giá»‘ng `OpenForAuthorizedUserAsync`)
  - [x] 4.3 Register services trong DI (cÃ¹ng file vá»›i cÃ¡c service khÃ¡c):
    - `services.AddScoped<ISubmissionService, SubmissionService>()`
  - [x] 4.4 `dotnet test` â€” xÃ¡c nháº­n tests hiá»‡n cÃ³ váº«n pass

- [x] Task 5: Backend â€” EF Core config + migration (AC1)
  - [x] 5.1 Táº¡o `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs`:
    ```csharp
    entity.HasKey(s => s.Id);
    entity.Property(s => s.RowVersion).IsRowVersion();
    entity.HasIndex(s => new { s.StudentId, s.HomeworkAssignmentId }).IsUnique()
        .HasFilter("[HomeworkAssignmentId] IS NOT NULL");
    entity.HasIndex(s => new { s.StudentId, s.LiveExamSessionId }).IsUnique()
        .HasFilter("[LiveExamSessionId] IS NOT NULL");
    entity.HasCheckConstraint("CK_Submissions_ExactlyOneSource",
        "([HomeworkAssignmentId] IS NOT NULL AND [LiveExamSessionId] IS NULL) OR ([HomeworkAssignmentId] IS NULL AND [LiveExamSessionId] IS NOT NULL)");
    entity.HasOne<HomeworkAssignment>().WithMany()
        .HasForeignKey(s => s.HomeworkAssignmentId).OnDelete(DeleteBehavior.Restrict);
    entity.HasOne<LiveExamSession>().WithMany()
        .HasForeignKey(s => s.LiveExamSessionId).OnDelete(DeleteBehavior.Restrict);
    entity.HasOne<AnswerKeyVersion>().WithMany()
        .HasForeignKey(s => s.AnswerKeyVersionId).OnDelete(DeleteBehavior.Restrict);
    ```
    **LÆ°u Ã½**: `OnDelete(DeleteBehavior.Restrict)` trÃªn AnswerKeyVersion FK â€” giáº£i quyáº¿t deferred issue tá»« story 2.4 code review. FK `AnswerKeyVersion â†’ TestTemplate` cÃ³ cascade DELETE trÆ°á»›c Ä‘Ã¢y sáº½ bá»‹ cháº·n bá»Ÿi Restrict nÃ y khi cÃ³ submissions.
  - [x] 5.2 ThÃªm `DbSet<Submission>` vÃ o `EnglishTestWebDbContext`:
    ```csharp
    public DbSet<Submission> Submissions => Set<Submission>();
    ```
    ThÃªm `using EnglishTestWeb.Api.Domain.Submissions;` vÃ o usings.
  - [x] 5.3 `dotnet ef migrations add AddSubmissions --project src/EnglishTestWeb.Api/EnglishTestWeb.Api.csproj`
  - [x] 5.4 Inspect migration file â€” xÃ¡c nháº­n cÃ³ check constraint `CK_Submissions_ExactlyOneSource`, unique indexes, vÃ  FKs Ä‘Ãºng
  - [x] 5.5 `dotnet test` â€” xÃ¡c nháº­n tests hiá»‡n cÃ³ váº«n pass (in-memory DB khÃ´ng enforce check constraints nhÆ°ng migration pháº£i compile)

- [x] Task 6: Backend â€” Controller (AC1, AC2, AC3, AC4)
  - [x] 6.1 Táº¡o `src/EnglishTestWeb.Api/Controllers/SubmissionsController.cs`:
    - `[Authorize(Roles = IdentityRoleNames.Student)]` trÃªn táº¥t cáº£ methods
    - **`POST /api/submissions`** â†’ `CreateOrResume`:
      - Extract studentId tá»« `ICurrentUserContext.UserId`
      - Extract activeClassId tá»« `ICurrentUserContext.ActiveClassId`; náº¿u null â†’ `404 submission.notFound` (há»c sinh khÃ´ng cÃ³ class)
      - Gá»i `IClassAuthorizationService.RequireStudentClassAccessAsync` trÆ°á»›c
      - Gá»i `ISubmissionService.CreateOrResumeAsync`
      - Success + Created: return `201 Created` vá»›i `SubmissionDto`, Location header = `/api/submissions/{id}`
      - Success + Existing: return `200 OK` vá»›i `SubmissionDto`
      - Failure: return `400` (invalidSource) hoáº·c `404` (class scope/source not found/unavailable)
    - **`GET /api/submissions/{id:guid}/workspace`** â†’ `GetWorkspace`:
      - Extract studentId
      - Gá»i `ISubmissionService.GetWorkspaceAsync(id, studentId)`
      - Náº¿u null â†’ `404 submission.notFound`
      - Return `200 OK` vá»›i `SubmissionWorkspaceDto`
    - **`GET /api/submissions/{id:guid}/materials/{fileId:guid}/content`** â†’ `GetMaterialContent`:
      - Extract studentId
      - Gá»i `IProtectedFileService.OpenForStudentWithSubmissionAsync(fileId, studentId, id)`
      - Náº¿u khÃ´ng allowed â†’ `404 files.notFound`
      - `Response.Headers[HeaderNames.AcceptRanges] = "bytes"`
      - Return `File(stream, contentType, filename, enableRangeProcessing: true)`
  - [x] 6.2 `dotnet test` â€” xÃ¡c nháº­n tests pass

- [x] Task 7: Backend â€” API tests (AC1, AC2, AC3, AC4)
  - [x] 7.1 Táº¡o `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsTestHelper.cs`:
    - `SeedHomeworkWithReadyTemplateAsync(factory)` â†’ tráº£ vá» `(homeworkId, classId, templateId, pdfStoredFileId)`
    - `SeedOpenLiveExamWithAudioAsync(factory)` â†’ tráº£ vá» `(sessionId, classId, templateId, pdfStoredFileId, audioStoredFileId)`
    - `CreateSubmissionAsync(client, homeworkId?, sessionId?)` â†’ tráº£ vá» submissionId
  - [x] 7.2 Táº¡o `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsControllerTests.cs`:
    - `CreateSubmission_AsAnonymous_Returns401`
    - `CreateSubmission_AsTeacher_Returns403`
    - `CreateSubmission_WithHomeworkId_AsStudent_Returns201WithDraftStatus`
    - `CreateSubmission_WithLiveExamSessionId_AsStudent_Returns201WithDraftStatus`
    - `CreateSubmission_Idempotent_ReturnsSameSubmissionId` (POST twice â†’ same Id, status 200 second time)
    - `CreateSubmission_WithBothIds_Returns400` (error code `submission.invalidSource`)
    - `CreateSubmission_WithNoIds_Returns400` (error code `submission.invalidSource`)
    - `CreateSubmission_ForExpiredHomework_Returns400` (error code `submission.sourceUnavailable`)
    - `CreateSubmission_ForClosedLiveExam_Returns400` (error code `submission.sourceUnavailable`)
    - `CreateSubmission_StudentFromDifferentClass_Returns404`
    - `GetWorkspace_AsAnonymous_Returns401`
    - `GetWorkspace_AsOtherStudent_Returns404`
    - `GetWorkspace_AsOwnerStudent_ReturnsWorkspace` â€” xÃ¡c nháº­n: templateTitle, skill, pdfMaterialId cÃ³ giÃ¡ trá»‹, questionCount > 0, answerRows = []
    - `GetMaterialContent_AsStudent_WithValidSubmission_ReturnsStream`
    - `GetMaterialContent_AsStudent_WithoutSubmission_Returns404`
    - `GetMaterialContent_AsStudent_WrongSubmission_Returns404` (submission cá»§a student khÃ¡c)
  - [x] 7.3 ThÃªm `POST /api/submissions` vÃ  `GET /api/submissions/{id}/workspace` vÃ o `AuthorizationMatrixTests.cs`
  - [x] 7.4 `dotnet test` â€” xÃ¡c nháº­n táº¥t cáº£ tests pass

- [x] Task 8: Angular â€” core service vÃ  models (AC2, AC3)
  - [x] 8.1 Táº¡o `src/EnglishTestWeb.Client/src/app/core/submissions/submissions.models.ts`:
    ```typescript
    export interface SubmissionDto {
      id: string;
      status: 'draft' | 'submitted';
      mode: 'homework' | 'live-exam';
    }

    export interface AnswerRowDto {
      questionNumber: number;
      answer: string | null;
    }

    export interface SubmissionWorkspace {
      id: string;
      status: 'draft' | 'submitted';
      mode: 'homework' | 'live-exam';
      templateTitle: string;
      skill: 'reading' | 'listening';
      classId: string;
      className: string;
      homeworkAssignmentId: string | null;
      liveExamSessionId: string | null;
      deadlineAt: string | null;
      timeLimitMinutes: number | null;
      sessionOpenedAt: string | null;
      sessionClosedAt: string | null;
      pdfMaterialId: string;
      audioMaterialId: string | null;
      questionCount: number;
      answerRows: AnswerRowDto[];
    }

    export const SUBMISSION_MODE_LABELS: Record<string, string> = {
      homework: 'BÃ i táº­p vá» nhÃ ',
      'live-exam': 'Thi trá»±c tiáº¿p',
    };

    export const SUBMISSION_ERROR_MESSAGES: Record<string, string> = {
      'submission.invalidSource': 'Nguá»“n bÃ i thi khÃ´ng há»£p lá»‡.',
      'submission.sourceUnavailable': 'BÃ i thi nÃ y hiá»‡n khÃ´ng cÃ²n kháº£ dá»¥ng.',
      'submission.notFound': 'KhÃ´ng tÃ¬m tháº¥y bÃ i lÃ m.',
      'files.notFound': 'File khÃ´ng táº£i Ä‘Æ°á»£c. Vui lÃ²ng thá»­ láº¡i.',
    };
    ```
  - [x] 8.2 Táº¡o `src/EnglishTestWeb.Client/src/app/core/submissions/submissions-api.service.ts`:
    - `createOrResume(request: { homeworkAssignmentId?: string; liveExamSessionId?: string }): Promise<SubmissionDto>`
      - gá»i `POST /api/submissions`; tráº£ vá» body dÃ¹ status 200 hay 201
    - `getWorkspace(submissionId: string): Promise<SubmissionWorkspace>`
      - gá»i `GET /api/submissions/{submissionId}/workspace`
    - `getMaterialContentUrl(submissionId: string, fileId: string): string`
      - tráº£ vá» `/api/submissions/${submissionId}/materials/${fileId}/content` (URL string, khÃ´ng pháº£i HTTP call)

- [x] Task 9: Angular â€” workspace component (AC2, AC3, AC4, AC5, AC6)
  - [x] 9.1 Táº¡o `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.ts`:
    - Inject: `SubmissionsApiService`, `ActivatedRoute`, `Router`, `DomSanitizer`, `ClassContextService`, `AuthSessionService`
    - Signals: `viewState: 'loading' | 'loaded' | 'error'`, `workspace: SubmissionWorkspace | null`, `errorCode: string | null`
    - Computed `pdfUrl`: `DomSanitizer.bypassSecurityTrustResourceUrl(submissionsApi.getMaterialContentUrl(id, workspace.pdfMaterialId))`
    - Computed `audioUrl`: tÆ°Æ¡ng tá»± cho audioMaterialId náº¿u cÃ³
    - `answerInputs`: `signal<Record<number, string>>({})` â€” lÆ°u tráº¡ng thÃ¡i answer form in-memory (key = questionNumber)
    - `ngOnInit()`: láº¥y `submissionId` tá»« `route.snapshot.paramMap.get('submissionId')`; gá»i `getWorkspace`; set signals
    - `onAnswerChange(questionNumber: number, value: string)`: cáº­p nháº­t `answerInputs` signal (khÃ´ng autosave â€” story 4.3)
    - `answeredCount()` computed: Ä‘áº¿m sá»‘ entries trong answerInputs cÃ³ giÃ¡ trá»‹ non-empty
    - `jumpToFirstUnanswered()`: scroll Ä‘áº¿n answer row Ä‘áº§u tiÃªn chÆ°a cÃ³ answer
    - `onSubmit()`: placeholder â€” khÃ´ng lÃ m gÃ¬ (story 4.4)
    - `backToTests()`: `router.navigate(['/student/tests'])`
  - [x] 9.2 Táº¡o `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.html`:
    - Loading state: spinner/loading message
    - Error state: error message (dÃ¹ng `SUBMISSION_ERROR_MESSAGES[errorCode]` hoáº·c fallback), retry button + back button
    - Loaded state â€” split layout:
      - **Header bar**: tiÃªu Ä‘á» bÃ i thi, skill badge, mode badge (dÃ¹ng `SUBMISSION_MODE_LABELS`), class name, autosave status placeholder (`data-testid="autosave-status"` hiá»ƒn thá»‹ "â€”")
      - **Left panel (PDF)**: `<iframe [src]="pdfUrl" ...>` hoáº·c `<embed>` vá»›i width=100%, height phÃ¹ há»£p; `data-testid="pdf-viewer"`
      - **Audio player** (chá»‰ khi `workspace.skill === 'listening'` vÃ  `audioUrl`): `<audio controls [src]="audioUrl">` â€” `data-testid="audio-player"`; cáº§n `DomSanitizer.bypassSecurityTrustUrl()` cho audio src
      - **Right panel (answer form)**:
        - Progress bar/counter: "ÄÃ£ tráº£ lá»i: X/N" â€” `data-testid="answer-progress"`
        - `@for (q of answerRange(); track q)` â€” render answer rows; `answerRange()` = `Array.from({length: workspace.questionCount}, (_, i) => i+1)`
        - Má»—i row: `<input type="text" [value]="answerInputs()[q] ?? ''" (input)="onAnswerChange(q, $event.target.value)" data-testid="answer-input-{{q}}" aria-label="CÃ¢u {{q}}">`
        - Jump button: "CÃ¢u chÆ°a Ä‘iá»n" â†’ `jumpToFirstUnanswered()` â€” `data-testid="jump-to-unanswered"`
        - Submit button (placeholder): `<button data-testid="submit-button" disabled>Ná»™p bÃ i (ChÆ°a triá»ƒn khai)</button>` â€” **KHÃ”NG** implement logic submit trong story nÃ y
    - DÃ¹ng `@if` / `@for` (Angular 17+ control flow), KHÃ”NG dÃ¹ng `*ngIf` / `*ngFor`
    - Stable IDs: `data-testid="workspace-header"`, `data-testid="pdf-viewer"`, `data-testid="answer-form"`, `data-testid="autosave-status"`, `data-testid="submit-button"`, `data-testid="answer-progress"`
  - [x] 9.3 Táº¡o CSS cho split layout:
    - Desktop: left panel (PDF) ~60% width, right panel (answer form) ~40%, side by side
    - Tablet: stack vertically náº¿u viewport < ~768px
    - Header bar fixed hoáº·c sticky
  - [x] 9.4 Cáº­p nháº­t `student-assigned-tests.component.ts` â€” implement `onStartItem`:
    - Inject `SubmissionsApiService` (náº¿u chÆ°a inject)
    - ThÃªm signal `startingItemId: signal<string | null>(null)` Ä‘á»ƒ track loading state
    - `onStartItem(item)`: gá»i API POST create/resume â†’ navigate Ä‘áº¿n `/student/workspace/{submissionId}`
    - Xá»­ lÃ½ lá»—i: náº¿u API tráº£ lá»—i â†’ set `blockedItemMessage` vá»›i error message phÃ¹ há»£p
    - `data-testid="start-button-{{item.id}}"` cÃ³ thá»ƒ cáº§n `[disabled]="startingItemId() === item.id"` khi Ä‘ang load

- [x] Task 10: Angular â€” route update (AC2)
  - [x] 10.1 Cáº­p nháº­t `src/EnglishTestWeb.Client/src/app/app.routes.ts`:
    ```typescript
    {
      path: 'student/workspace/:submissionId',
      canActivate: [studentGuard],
      loadComponent: () =>
        import('./features/student-attempt-workspace/student-attempt-workspace.component').then(
          (m) => m.StudentAttemptWorkspaceComponent,
        ),
    },
    ```
    ThÃªm sau route `student/tests`.

- [x] Task 11: Angular â€” component spec (AC1, AC2, AC3, AC4, AC5)
  - [x] 11.1 Táº¡o `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.spec.ts`:
    - Mock `SubmissionsApiService` vá»›i `vi.fn()`
    - Mock `ActivatedRoute` vá»›i `paramMap.get('submissionId')` tráº£ vá» `'test-sub-1'`
    - Mock `DomSanitizer` vá»›i `bypassSecurityTrustResourceUrl: (url) => url` (tráº£ vá» string Ä‘Æ¡n giáº£n cho test)
    - Test: `táº£i workspace thÃ nh cÃ´ng â€” hiá»ƒn thá»‹ tiÃªu Ä‘á» vÃ  skill badge`
    - Test: `workspace Reading â€” khÃ´ng hiá»ƒn thá»‹ audio player`
    - Test: `workspace Listening vá»›i audio â€” hiá»ƒn thá»‹ audio player`
    - Test: `answer form render Ä‘Ãºng sá»‘ cÃ¢u há»i tá»« questionCount`
    - Test: `nháº­p cÃ¢u tráº£ lá»i â†’ answeredCount tÄƒng, answer panel khÃ´ng reset`
    - Test: `error khi getWorkspace tháº¥t báº¡i â€” hiá»ƒn thá»‹ error state`
    - Test: `submit button hiá»‡n diá»‡n vÃ  disabled`
    - Test: `autosave-status region hiá»‡n diá»‡n`
  - [x] 11.2 `npm test` â€” xÃ¡c nháº­n táº¥t cáº£ tests pass

## Dev Notes

### Backend: Submission Entity Design

**QUAN TRá»ŒNG**: `Submission` lÃ  entity Ä‘áº§u tiÃªn trong `Domain/Submissions/`. Táº¡o thÆ° má»¥c má»›i.

```csharp
namespace EnglishTestWeb.Api.Domain.Submissions;

public sealed class Submission
{
    public Guid Id { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public Guid? HomeworkAssignmentId { get; set; }

    public Guid? LiveExamSessionId { get; set; }

    public Guid? AnswerKeyVersionId { get; set; }

    public string Status { get; set; } = SubmissionStatuses.Draft;

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties (cáº§n cho ProtectedFileService query)
    public HomeworkAssignment? HomeworkAssignment { get; set; }
    public LiveExamSession? LiveExamSession { get; set; }
}
```

**AnswerKeyVersionId snapping logic** (trong `SubmissionService.CreateOrResumeAsync`):
```csharp
// Láº¥y Ready AnswerKeyVersion cho template
var templateId = homeworkAssignment?.TestTemplateId ?? liveExamSession!.TestTemplateId;
var answerKeyVersionId = await dbContext.AnswerKeyVersions
    .AsNoTracking()
    .Where(a => a.TemplateId == templateId && a.Status == AnswerKeyStatuses.Ready)
    .OrderByDescending(a => a.UpdatedAt)
    .Select(a => (Guid?)a.Id)
    .FirstOrDefaultAsync(ct);
// LÆ°u vÃ o submission.AnswerKeyVersionId (nullable â€” OK náº¿u null)
```

Táº¡i sao snap táº¡i thá»i Ä‘iá»ƒm táº¡o: architecture yÃªu cáº§u "AnswerKey edits after submissions create a new version; historical submissions never rebind silently."

### Backend: SubmissionConfiguration â€” Check Constraint

```csharp
// TÃªn constraint PHáº¢I lÃ  CK_Submissions_ExactlyOneSource (tá»« architecture doc)
entity.HasCheckConstraint("CK_Submissions_ExactlyOneSource",
    "([HomeworkAssignmentId] IS NOT NULL AND [LiveExamSessionId] IS NULL) OR ([HomeworkAssignmentId] IS NULL AND [LiveExamSessionId] IS NOT NULL)");
```

**Unique index Ä‘á»ƒ prevent duplicate attempt per (student, source)**:
- In-memory DB (test) KHÃ”NG enforce check constraints, nhÆ°ng ENFORCE unique index
- Test idempotency sáº½ dá»±a vÃ o unique index violation + retry logic trong service

**DeleteBehavior.Restrict trÃªn AnswerKeyVersion FK**: giáº£i quyáº¿t deferred issue tá»« story 2-4. TrÆ°á»›c khi story 4.2, `AnswerKeyVersion` khÃ´ng cÃ³ child entities nÃªn cascade OK. Sau story 4.2, cascade DELETE sáº½ orphan submission data. Restrict lÃ  Ä‘Ãºng.

### Backend: MaterialRoles

Kiá»ƒm tra `MaterialRoles.cs` trong Domain/TestTemplates Ä‘á»ƒ biáº¿t Ä‘Ãºng tÃªn role constants:
```csharp
// DÃ¹ng constants thay vÃ¬ hardcode string
MaterialRoles.Pdf  // hoáº·c tÆ°Æ¡ng Ä‘Æ°Æ¡ng
MaterialRoles.Audio
```
Cháº¡y `grep -r "MaterialRoles" src/` Ä‘á»ƒ tÃ¬m tÃªn chÃ­nh xÃ¡c trÆ°á»›c khi code.

### Backend: Idempotency Logic (CreateOrResumeAsync)

```csharp
// Check existing
var existing = await dbContext.Submissions
    .AsNoTracking()
    .Where(s => s.StudentId == studentId
                && (request.HomeworkAssignmentId == null || s.HomeworkAssignmentId == request.HomeworkAssignmentId)
                && (request.LiveExamSessionId == null || s.LiveExamSessionId == request.LiveExamSessionId)
                && s.Status == SubmissionStatuses.Draft)
    .Select(s => s.Id)
    .FirstOrDefaultAsync(ct);

if (existing != default)
    return new CreateSubmissionResult(true, existing, null, Created: false);
```

**Xá»­ lÃ½ source availability**:
```csharp
// Homework
var homework = await dbContext.HomeworkAssignments
    .AsNoTracking()
    .Where(h => h.Id == request.HomeworkAssignmentId && h.ClassId == activeClassId)
    .Select(h => new { h.DeadlineAt, h.TestTemplateId })
    .FirstOrDefaultAsync(ct);

if (homework is null)
    return new CreateSubmissionResult(false, null, "submission.notFound", false);

if (homework.DeadlineAt < timeProvider.GetUtcNow())
    return new CreateSubmissionResult(false, null, "submission.sourceUnavailable", false);
```

### Backend: FilesController â€” Student File Access

**CRITICAL**: `FilesController.GetContent` hiá»‡n táº¡i chá»‰ cho phÃ©p `[Authorize(Roles = Teacher)]`. Cáº§n thÃªm student path **RIÃŠNG BIá»†T** thay vÃ¬ sá»­a endpoint hiá»‡n cÃ³ (Ä‘á»ƒ khÃ´ng break teacher access):

```csharp
// Endpoint Má»šI trong FilesController (hoáº·c SubmissionsController)
[Authorize(Roles = IdentityRoleNames.Student)]
[HttpGet("{submissionId:guid}/materials/{fileId:guid}/content", Name = "GetSubmissionMaterial")]
// Äáº·t trong SubmissionsController, khÃ´ng trong FilesController
```

**Route**: `GET /api/submissions/{id}/materials/{fileId}/content` â€” Ä‘áº·t trong `SubmissionsController` lÃ  cleaner hÆ¡n lÃ  thÃªm phá»©c táº¡p vÃ o `FilesController`. KhÃ´ng sá»­a `FilesController` hiá»‡n táº¡i.

**ProtectedFileService.OpenForStudentWithSubmissionAsync** â€” query cáº§n:
```csharp
var sub = await dbContext.Submissions
    .AsNoTracking()
    .Include(s => s.HomeworkAssignment)
    .Include(s => s.LiveExamSession)
    .Where(s => s.Id == submissionId && s.StudentId == studentId)
    .FirstOrDefaultAsync(ct);

if (sub is null) return new ProtectedFileAccessResult(false, null, "files.notFound");

var templateId = sub.HomeworkAssignment?.TestTemplateId ?? sub.LiveExamSession?.TestTemplateId;
if (templateId is null) return new ProtectedFileAccessResult(false, null, "files.notFound");
```

### Backend: WorkspaceDto â€” PdfMaterialId mapping

`pdfMaterialId` trong DTO lÃ  `StoredFile.Id` (Guid) â€” Ä‘Ã¢y lÃ  giÃ¡ trá»‹ truyá»n vÃ o `/api/submissions/{id}/materials/{fileId}/content`. Khi query `TestMaterials`, select `material.StoredFileId` (khÃ´ng pháº£i `material.Id`).

```csharp
var pdfMaterial = await dbContext.TestMaterials
    .AsNoTracking()
    .Where(m => m.TemplateId == templateId && m.IsActive && m.Role == MaterialRoles.Pdf)
    .Select(m => (Guid?)m.StoredFileId)
    .FirstOrDefaultAsync(ct);
```

### Backend: Error Codes

DÃ¹ng dot-notation theo architecture pattern:
- `submission.invalidSource` â€” cáº£ hai null hoáº·c cáº£ hai set
- `submission.sourceUnavailable` â€” homework expired hoáº·c live exam khÃ´ng open
- `submission.notFound` â€” khÃ´ng tÃ¬m tháº¥y submission
- `files.notFound` â€” file khÃ´ng accessible (giá»¯ nguyÃªn consistent vá»›i existing)

### Angular: PDF Viewer vá»›i DomSanitizer

Angular cháº·n resource URLs khÃ´ng tin cáº­y. **Báº¯t buá»™c** dÃ¹ng `DomSanitizer`:

```typescript
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

// Trong component:
protected readonly pdfUrl = computed<SafeResourceUrl | null>(() => {
  const w = this.workspace();
  if (!w) return null;
  const url = this.submissionsApi.getMaterialContentUrl(w.id, w.pdfMaterialId);
  return this.sanitizer.bypassSecurityTrustResourceUrl(url);
});

// Template:
// <iframe [src]="pdfUrl()" ...></iframe>
```

**TÆ°Æ¡ng tá»± cho audio** â€” dÃ¹ng `bypassSecurityTrustResourceUrl` cho `[src]` cá»§a `<audio>`.

**Táº¡i sao bypass lÃ  an toÃ n**: URL Ä‘Æ°á»£c táº¡o tá»« data Ä‘Ã£ xÃ¡c thá»±c qua API (khÃ´ng pháº£i user input trá»±c tiáº¿p), endpoint server-side enforce authorization.

### Angular: Answer Form â€” Giá»¯ State Khi Chuyá»ƒn Trang PDF

PDF viewer lÃ  `<iframe>` â€” chuyá»ƒn trang PDF lÃ  browser-native (scroll hoáº·c built-in controls), KHÃ”NG reload iframe. Answer panel lÃ  Angular component riÃªng biá»‡t â†’ khÃ´ng bá»‹ áº£nh hÆ°á»Ÿng bá»Ÿi iframe navigation. AC5 Ä‘Æ°á»£c thá»a mÃ£n tá»± nhiÃªn.

Náº¿u dÃ¹ng custom page navigation (buttons), chá»‰ thay Ä‘á»•i query param hoáº·c fragment trong iframe src, khÃ´ng re-create component â†’ answer inputs giá»¯ nguyÃªn signal state.

```typescript
// Náº¿u cáº§n custom page nav:
protected readonly currentPage = signal(1);

protected readonly pdfUrlWithPage = computed<SafeResourceUrl | null>(() => {
  const url = this.getMaterialContentUrl() + `#page=${this.currentPage()}`;
  return this.sanitizer.bypassSecurityTrustResourceUrl(url);
});
```

### Angular: SubmissionsApiService â€” Error Handling

```typescript
async createOrResume(request: { homeworkAssignmentId?: string; liveExamSessionId?: string }): Promise<SubmissionDto> {
  const response = await fetch('/api/submissions', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
    credentials: 'include',
  });
  // Fetch-based hoáº·c HttpClient-based â€” theo pattern cá»§a project
}
```

**Kiá»ƒm tra pattern hiá»‡n táº¡i**: project dÃ¹ng Angular `HttpClient` trong API services (xem `AssignedTestsApiService`). DÃ¹ng HttpClient, KHÃ”NG dÃ¹ng raw `fetch`. Inject `HttpClient` vÃ  dÃ¹ng `firstValueFrom(this.http.post(...))`.

### Angular: Cáº­p nháº­t `onStartItem` trong StudentAssignedTestsComponent

```typescript
// ThÃªm vÃ o StudentAssignedTestsComponent
private readonly submissionsApi = inject(SubmissionsApiService);
protected readonly startingItemId = signal<string | null>(null);

protected async onStartItem(item: AssignedTestItem): Promise<void> {
  // ... existing status checks (khÃ´ng thay Ä‘á»•i) ...
  
  this.startingItemId.set(item.id);
  this.blockedItemMessage.set(null);
  
  try {
    const request = item.mode === 'homework'
      ? { homeworkAssignmentId: item.id }
      : { liveExamSessionId: item.id };
    const submission = await this.submissionsApi.createOrResume(request);
    await this.router.navigate(['/student/workspace', submission.id]);
  } catch (err: unknown) {
    const code = extractErrorCode(err); // dÃ¹ng helper hiá»‡n cÃ³ trong project
    this.blockedItemMessage.set(SUBMISSION_ERROR_MESSAGES[code] ?? 'KhÃ´ng thá»ƒ báº¯t Ä‘áº§u bÃ i thi.');
  } finally {
    this.startingItemId.set(null);
  }
}
```

**Kiá»ƒm tra `extractErrorCode` pattern**: tÃ¬m trong project xem cÃ³ ProblemDetails error extraction utility khÃ´ng. Xem `problem-details.interceptor.ts` hoáº·c tÆ°Æ¡ng Ä‘Æ°Æ¡ng.

### Angular: Import `HttpClientModule` / provideHttpClient

Workspace component lÃ  standalone â€” Ä‘áº£m báº£o `HttpClient` available. `HttpClientModule` Ä‘Æ°á»£c configure á»Ÿ app level (khÃ´ng cáº§n import láº¡i trong feature component). Kiá»ƒm tra `app.config.ts`.

### Context tá»« Previous Stories

1. **`flushPromises()` pattern** â€” dÃ¹ng trong táº¥t cáº£ Angular tests async, khÃ´ng dÃ¹ng `fixture.whenStable()`
2. **Signal set trong test**: `(component as any).viewState.set('loaded')` náº¿u cáº§n force state
3. **`@if` / `@for`** â€” Angular 17+ control flow syntax, KHÃ”NG `*ngIf` / `*ngFor`
4. **Stable IDs cho testing**: dÃ¹ng `data-testid` attributes (theo pattern cá»§a story 4.1)
5. **`IClassAuthorizationService.RequireStudentClassAccessAsync`** â€” check membership active, dÃ¹ng trÆ°á»›c khi query data
6. **`TimeProvider`** â€” inject `TimeProvider` (khÃ´ng `DateTime.UtcNow`) trong SubmissionService
7. **`IHiddenResourceResponseFactory`** â€” dÃ¹ng Ä‘á»ƒ return `404 hiddenResource` theo project pattern
8. **`ICurrentUserContext.ActiveClassId`** â€” láº¥y classId tá»« claim, khÃ´ng tá»« request body
9. **Navigation properties** â€” `HomeworkAssignment.Template` vÃ  `LiveExamSession.Template` Ä‘Ã£ cÃ³ (added in story 4.1)
10. **Test helper pattern** â€” `SubmissionsTestHelper` nÃªn follow `AssignedTestsTestHelper.cs`; dÃ¹ng static methods `SeedXxxAsync`
11. **DI registration location** â€” tÃ¬m nÆ¡i `IAssignedTestService` Ä‘Æ°á»£c register, thÃªm `ISubmissionService` cÃ¹ng chá»—

### Files Being Created/Modified

**API (new):**
- `src/EnglishTestWeb.Api/Domain/Submissions/Submission.cs`
- `src/EnglishTestWeb.Api/Domain/Submissions/SubmissionStatuses.cs`
- `src/EnglishTestWeb.Api/Contracts/Submissions/CreateSubmissionRequest.cs`
- `src/EnglishTestWeb.Api/Contracts/Submissions/SubmissionDto.cs`
- `src/EnglishTestWeb.Api/Contracts/Submissions/SubmissionWorkspaceDto.cs`
- `src/EnglishTestWeb.Api/Contracts/Submissions/AnswerRowDto.cs`
- `src/EnglishTestWeb.Api/Application/Submissions/ISubmissionService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Submissions/SubmissionService.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs`
- `src/EnglishTestWeb.Api/Controllers/SubmissionsController.cs`
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/Migrations/` â€” migration má»›i AddSubmissions

**API (update):**
- `src/EnglishTestWeb.Api/Application/Files/IProtectedFileService.cs` â€” thÃªm `OpenForStudentWithSubmissionAsync`
- `src/EnglishTestWeb.Api/Infrastructure/Files/ProtectedFileService.cs` â€” implement method má»›i
- `src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs` â€” thÃªm `DbSet<Submission>`
- `Program.cs` hoáº·c DI extension â€” register `ISubmissionService`

**Tests (new):**
- `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsTestHelper.cs`
- `tests/EnglishTestWeb.Api.Tests/Submissions/SubmissionsControllerTests.cs`

**Tests (update):**
- `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` â€” thÃªm Submissions endpoints

**Angular (new):**
- `src/EnglishTestWeb.Client/src/app/core/submissions/submissions.models.ts`
- `src/EnglishTestWeb.Client/src/app/core/submissions/submissions-api.service.ts`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.ts`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.html`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.css`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.spec.ts`

**Angular (update):**
- `src/EnglishTestWeb.Client/src/app/app.routes.ts` â€” thÃªm `student/workspace/:submissionId`
- `src/EnglishTestWeb.Client/src/app/features/student-assigned-tests/student-assigned-tests.component.ts` â€” implement `onStartItem` navigation

### Architecture Compliance

- **Controller khÃ´ng access DbContext** â€” delegate hoÃ n toÃ n sang `ISubmissionService` vÃ  `IProtectedFileService`
- **Student file access** â€” thÃ´ng qua scoped route `/api/submissions/{id}/materials/{fileId}/content`, khÃ´ng sá»­a Teacher-only `FilesController` endpoint
- **ClassId tá»« claim** â€” khÃ´ng tá»« query param/body; `ICurrentUserContext.ActiveClassId`
- **Student membership re-check** â€” gá»i `IClassAuthorizationService.RequireStudentClassAccessAsync` trÆ°á»›c khi create submission
- **Exactly-one-source** â€” DB check constraint `CK_Submissions_ExactlyOneSource` + application-level validation
- **DeleteBehavior.Restrict** â€” trÃªn táº¥t cáº£ FKs tá»« Submission; khÃ´ng cascade DELETE
- **AnswerKeyVersion snap** â€” capture táº¡i thá»i Ä‘iá»ƒm táº¡o submission Ä‘á»ƒ story 4.4 dÃ¹ng
- **Response wrapper** â€” `SubmissionDto` tráº£ vá» trá»±c tiáº¿p (khÃ´ng cáº§n wrapper vÃ¬ lÃ  single resource)
- **`ProblemDetails` + stable error codes** â€” `submission.*` namespace

### Anti-Patterns

- **KHÃ”NG** nháº­n `classId` tá»« query param â€” láº¥y tá»« `ICurrentUserContext.ActiveClassId`
- **KHÃ”NG** sá»­a `FilesController` teacher endpoint â€” thÃªm route riÃªng trong `SubmissionsController`
- **KHÃ”NG** cho phÃ©p cáº£ hai HomeworkAssignmentId vÃ  LiveExamSessionId trong cÃ¹ng má»™t request â€” tráº£ 400
- **KHÃ”NG** táº¡o nhiá»u Draft submissions cho cÃ¹ng (student, source) â€” idempotent create
- **KHÃ”NG** implement autosave trong story nÃ y â€” answer inputs chá»‰ lÆ°u in-memory signal
- **KHÃ”NG** implement submit button action trong story nÃ y â€” button tá»“n táº¡i nhÆ°ng disabled/placeholder
- **KHÃ”NG** render answer rows tá»« AnswerKey answers (correct answers) â€” chá»‰ render empty inputs theo questionCount; student-facing DTOs khÃ´ng bao giá» include `correctAnswer` hay `answerKey`
- **KHÃ”NG** dÃ¹ng `[src]="rawUrl"` trá»±c tiáº¿p trong template mÃ  khÃ´ng qua `DomSanitizer.bypassSecurityTrustResourceUrl()` â€” Angular sáº½ sanitize vÃ  break URL

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` â€” Epic 4, Story 4.2]
- [Source: `_bmad-output/planning-artifacts/architecture.md` â€” Submission integrity, File access patterns, Authorization patterns]
- [Source: `src/EnglishTestWeb.Api/Controllers/FilesController.cs` â€” File streaming pattern (teacher)]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Files/ProtectedFileService.cs` â€” File access implementation]
- [Source: `src/EnglishTestWeb.Api/Domain/TestTemplates/AnswerKeyVersion.cs` â€” AnswerKeyVersion entity]
- [Source: `src/EnglishTestWeb.Api/Domain/TestTemplates/AnswerKeyRow.cs` â€” Row structure]
- [Source: `src/EnglishTestWeb.Api/Domain/TestTemplates/MaterialRoles.cs` â€” Material role constants]
- [Source: `src/EnglishTestWeb.Api/Domain/Assignments/HomeworkAssignment.cs` â€” Assignment entity]
- [Source: `src/EnglishTestWeb.Api/Domain/LiveExams/LiveExamSession.cs` â€” Session entity]
- [Source: `src/EnglishTestWeb.Api/Infrastructure/Persistence/EnglishTestWebDbContext.cs` â€” Current DbSets]
- [Source: `src/EnglishTestWeb.Client/src/app/app.routes.ts` â€” Current Angular routes]
- [Source: `src/EnglishTestWeb.Client/src/app/features/student-assigned-tests/student-assigned-tests.component.ts` â€” onStartItem placeholder]
- [Source: `tests/EnglishTestWeb.Api.Tests/AssignedTests/AssignedTestsControllerTests.cs` â€” Test patterns]
- [Source: `tests/EnglishTestWeb.Api.Tests/Security/AuthorizationMatrixTests.cs` â€” Auth matrix pattern]
- [Source: `_bmad-output/implementation-artifacts/deferred-work.md` â€” Story 2.4 AnswerKeyVersion cascade DELETE defer (giáº£i quyáº¿t trong story nÃ y)]
- [Source: `_bmad-output/implementation-artifacts/4-1-student-assigned-tests-list.md` â€” Dev patterns, flushPromises, signal patterns]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
