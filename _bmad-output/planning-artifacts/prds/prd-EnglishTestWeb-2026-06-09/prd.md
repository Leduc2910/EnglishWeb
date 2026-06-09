---
title: "PRD: EnglishTestWeb MVP Test Workflows"
status: "draft-ready-for-architecture"
created: "2026-06-09"
updated: "2026-06-09"
project: "EnglishTestWeb"
source_mode: "fast-path bridge from WDS/BMad artifacts"
inputDocuments:
  - "_bmad-output/A-Product-Brief/project-brief.md"
  - "_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml"
  - "_bmad-output/E-Development/test-scenarios/TS-001-mvp-test-workflows.yaml"
  - "_bmad-output/E-Development/change-proposals/sprint-change-proposal-2026-06-08-homework-live-exam.md"
  - "docs/stitch_h_th_ng_kh_o_th_englishtestweb/STITCH_MAPPING.md"
---

# PRD: EnglishTestWeb MVP Test Workflows

## 0. Document Purpose

PRD này gom Product Brief, DD-001, TS-001, change proposal Homework/Live Exam, và Stitch UI Mapping thành một tài liệu yêu cầu sản phẩm đủ rõ để đi tiếp sang BMad Architecture, Epics/Stories, và Sprint Planning. PRD không thay thế các page specs WDS chi tiết; nó khóa vocabulary, scope MVP, functional requirements, non-functional requirements, open questions, và nguyên tắc ưu tiên. Khi có khác biệt, **DD-001 và WDS page specs là nguồn chuẩn cho behavior/domain**, còn **Stitch là nguồn tham chiếu visual/layout**.

## 1. Vision

EnglishTestWeb là một website giúp giáo viên tiếng Anh tạo đề kiểm tra từ tài liệu có sẵn, giao bài cho học sinh, thu bài, tự chấm Reading/Listening bằng answer key, và chấm Speaking thủ công trong một workspace tập trung.

MVP đặt cược vào một workflow thực tế: giáo viên thường đã có PDF/audio/cue card, nên hệ thống không cần parse PDF thành câu hỏi ở phiên bản đầu. Thay vào đó, giáo viên tạo **Đề gốc** trong **Thư viện đề**, upload tài liệu, nhập answer key/scoring riêng, rồi dùng Đề gốc đó làm **Homework** hoặc **Thi trực tiếp**. Cách tách này tránh nhầm giữa nội dung đề và lần sử dụng đề trong lớp.

Sản phẩm thành công khi giáo viên có thể tạo một Reading template từ PDF và answer key trong dưới 10 phút, học sinh tự vào đúng lớp bằng mã lớp và nộp bài không cần hướng dẫn trực tiếp, còn giáo viên xem/chấm kết quả trong một nơi thay vì ghép file, điểm và feedback thủ công.

## 2. Target User

### 2.1 Jobs To Be Done

- Giáo viên muốn biến PDF/audio/cue card có sẵn thành đề online nhanh mà không phải nhập lại toàn bộ nội dung.
- Giáo viên muốn dùng lại cùng một Đề gốc cho nhiều lớp hoặc nhiều buổi học mà không nhân bản dữ liệu đề.
- Giáo viên muốn giao Homework có hạn nộp và chạy Thi trực tiếp có trạng thái mở/đóng rõ ràng.
- Học sinh muốn vào đúng lớp, thấy đúng bài được giao, làm bài trên web, và biết chắc bài đã được nộp.
- Giáo viên muốn tự động có điểm Reading/Listening và chỉ tập trung chấm Speaking nơi cần đánh giá con người.

### 2.2 Non-Users (v1)

- Trung tâm cần LMS đầy đủ với lịch học, học phí, CRM, hoặc báo cáo vận hành nâng cao.
- Người học tự do không thuộc lớp/mã lớp do giáo viên quản lý.
- Người cần mobile app native hoặc trải nghiệm thi offline.
- Người cần AI parse PDF, AI sinh câu hỏi, hoặc AI chấm Speaking ngay trong MVP.

### 2.3 Key User Journeys

- **UJ-1. Cô Mai tạo Đề gốc Reading từ PDF rồi chọn cách dùng.**
  - **Persona + context:** Cô Mai là giáo viên bận rộn, đã có file PDF Reading và answer key.
  - **Entry state:** Đăng nhập tài khoản giáo viên, đang ở Teacher Dashboard.
  - **Path:** Cô Mai mở Thư viện đề, chọn tạo Đề gốc mới, nhập setup tối thiểu, upload PDF, nhập answer key/scoring, review và mark ready.
  - **Climax:** Đề gốc chuyển sang trạng thái sẵn sàng và hiển thị hành động tiếp theo: Giao homework hoặc Tạo thi trực tiếp.
  - **Resolution:** Cô Mai dùng cùng Đề gốc để tạo Homework có deadline hoặc LiveExamSession cho lớp.
  - **Edge case:** Nếu answer key còn thiếu đáp án, hệ thống chặn mark ready và chỉ rõ câu cần sửa.

- **UJ-2. Bạn An vào đúng lớp và hoàn thành Homework hoặc Thi trực tiếp.**
  - **Persona + context:** An là học sinh nhận mã lớp từ giáo viên và cần làm bài đúng ngữ cảnh.
  - **Entry state:** Chưa đăng nhập hoặc chưa chọn lớp trong phiên hiện tại.
  - **Path:** An nhập mã lớp, xác nhận lớp, đăng nhập tài khoản học sinh, xem danh sách Homework/Thi trực tiếp, mở bài đang được phép làm, xem PDF/nghe audio nếu có, nhập đáp án và nộp bài.
  - **Climax:** Hệ thống hiện xác nhận nộp bài thành công, khóa bài đã nộp, và lưu mode Homework hoặc Thi trực tiếp của attempt.
  - **Resolution:** Reading/Listening được tự chấm bằng AnswerKey; Speaking chuyển sang trạng thái chờ giáo viên chấm.
  - **Edge case:** Nếu LiveExamSession chưa mở hoặc Homework đã quá hạn, An không thể bắt đầu attempt mới và nhận thông báo rõ.

- **UJ-3. Cô Linh lọc kết quả và chấm Speaking trong một workspace.**
  - **Persona + context:** Cô Linh cần xử lý nhiều bài Speaking sau buổi học.
  - **Entry state:** Đăng nhập giáo viên, mở module Kết quả.
  - **Path:** Cô Linh lọc theo lớp, Đề gốc, mode, kỹ năng và trạng thái; mở một submission cần chấm; nghe audio; nhập điểm và feedback.
  - **Climax:** Save grading thành công và dòng kết quả chuyển sang Đã chấm.
  - **Resolution:** Điểm/feedback được lưu tập trung theo đúng học sinh, lớp, Đề gốc và HomeworkAssignment hoặc LiveExamSession.
  - **Edge case:** Nếu file Speaking tạm thời không mở được, hệ thống báo lỗi có thể phục hồi và không làm mất draft điểm/feedback.

## 3. Glossary

- **User** - Tài khoản trong hệ thống, có role Teacher hoặc Student.
- **Teacher** - User giáo viên, sở hữu lớp, Đề gốc, HomeworkAssignment, LiveExamSession và quyền xem/chấm kết quả trong phạm vi của mình.
- **Student** - User học sinh, chỉ truy cập lớp/bài được gán qua ClassMembership và trạng thái cho phép.
- **Class** - Lớp học do Teacher quản lý, có class_code để Student nhập trước khi đăng nhập hoặc vào bài.
- **ClassMembership** - Quan hệ Student thuộc Class; dùng để kiểm soát truy cập.
- **Thư viện đề** - Kho quản lý Đề gốc. Đây không phải là danh sách Homework và không phải danh sách Thi trực tiếp.
- **Đề gốc** - TestTemplate reusable chứa cấu hình đề, skill, tài liệu, AnswerKey và trạng thái draft/ready.
- **TestMaterial** - File hoặc nội dung gắn với Đề gốc, gồm PDF, Listening audio, cue card hoặc prompt Speaking.
- **AnswerKey** - Cấu hình số câu, đáp án đúng, scoring mode và điểm cho Reading/Listening.
- **HomeworkAssignment** - Một lần giao Đề gốc cho Class/Student làm ở nhà, có deadline, time limit nếu có, và trạng thái riêng.
- **LiveExamSession** - Một phiên Thi trực tiếp tạo từ Đề gốc cho Class, có trạng thái scheduled/open/closed và do Teacher kiểm soát.
- **Submission** - Bài làm của Student, luôn tham chiếu hoặc HomeworkAssignment hoặc LiveExamSession, không được mơ hồ.
- **SubmissionAnswer** - Câu trả lời theo số thứ tự trong form answer riêng của Reading/Listening.
- **SpeakingSubmission** - File Speaking và dữ liệu chấm thủ công gắn với Submission.
- **Mode** - Ngữ cảnh làm bài: Homework hoặc Thi trực tiếp.
- **Ready** - Trạng thái Đề gốc đã đủ tài liệu/answer key hợp lệ để được dùng tạo HomeworkAssignment hoặc LiveExamSession.

## 4. Product Decisions

- MVP là web app desktop/laptop-first, không phải mobile app native.
- MVP giữ hướng **upload PDF/audio + answer form riêng**; không parse PDF thành câu hỏi tự động.
- Thư viện đề chỉ chứa Đề gốc; HomeworkAssignment và LiveExamSession là các lần dùng Đề gốc.
- Reading/Listening được tự chấm bằng AnswerKey do Teacher khai báo.
- Speaking không tự chấm bằng AI trong MVP; Teacher nghe file và nhập điểm/feedback thủ công.
- Stitch UI Mapping được dùng làm visual/layout reference. DD-001, TS-001 và WDS page specs vẫn là nguồn chuẩn cho behavior, validation, access control và domain model.

## 5. Features

### 5.1 Accounts, Roles, Classes, And Access

**Description:** Hệ thống hỗ trợ Teacher và Student trong một website role-based. Student bắt đầu bằng class code để đảm bảo bối cảnh lớp đúng trước khi xem bài. Realizes UJ-2.

**Functional Requirements:**

#### FR-1: Teacher Authentication And Role Access

Teacher can log in and access Teacher Dashboard, Thư viện đề, lớp, and kết quả surfaces.

**Consequences:**
- Teacher-only routes reject unauthenticated users.
- Student users cannot access teacher management or grading screens.
- Teacher sees only classes, templates, assignments, sessions and submissions in their scope.

#### FR-2: Student Class Code Entry

Student can enter a class code before student login and confirm the selected Class context.

**Consequences:**
- Invalid or expired class code shows a clear retryable error.
- Class confirmation appears before Student proceeds to login.
- Class context is preserved after login.

#### FR-3: Student Membership Enforcement

Student can access work only when a ClassMembership exists for the selected Class.

**Consequences:**
- Student account not in selected Class is blocked with a clear next step.
- Direct route access to another Class, HomeworkAssignment, LiveExamSession or Submission is rejected.
- All Student lists and attempts are scoped to the active Class.

### 5.2 Template Library And Test Creation

**Description:** Teacher creates reusable Đề gốc in Thư viện đề from PDF/audio/cue card and answer key. Setup must not ask for Class, deadline or session timing; those belong to HomeworkAssignment or LiveExamSession. Realizes UJ-1.

#### FR-4: Create And Manage Đề gốc

Teacher can create, save draft, edit, list, search/filter, and inspect Đề gốc in Thư viện đề.

**Consequences:**
- Đề gốc stores title, skill, description and status.
- Draft Đề gốc can be edited without creating Student-visible work.
- Ready Đề gốc exposes usage actions: Giao homework and Tạo thi trực tiếp.

#### FR-5: Upload TestMaterial

Teacher can attach required PDF and optional audio/cue materials to Đề gốc.

**Consequences:**
- Reading requires a PDF before mark ready.
- Listening requires a PDF and can include audio.
- Speaking can use text/cue card/PDF prompt plus later Student upload.
- Upload failure preserves draft state and allows retry/replace.
- Large uploads show progress.

#### FR-6: Configure AnswerKey For Reading/Listening

Teacher can configure question count, correct answer, scoring mode, and score per question or total score for Reading/Listening.

**Consequences:**
- Missing answer rows are identified by question number.
- Invalid scoring blocks mark ready.
- AnswerKey is versioned or otherwise protected so submitted work remains gradeable against the intended key. [ASSUMPTION: Architecture will decide whether AnswerKey edits after submissions create a new version or are blocked.]

#### FR-7: Mark Đề gốc Ready

Teacher can mark Đề gốc as Ready only when required TestMaterial and validation rules pass.

**Consequences:**
- Double-clicking mark ready creates only one state transition.
- Ready state is required before creating HomeworkAssignment or LiveExamSession.
- Ready does not assign the template to any Class by itself.

### 5.3 Homework And Live Exam Usage Modes

**Description:** Teacher uses a Ready Đề gốc either as Homework or as a controlled Thi trực tiếp. The mode must remain visible to Teacher and Student, and must be preserved on every Submission. Realizes UJ-1 and UJ-2.

#### FR-8: Create HomeworkAssignment

Teacher can create HomeworkAssignment from a Ready Đề gốc for a Class with due date and optional time limit.

**Consequences:**
- HomeworkAssignment references exactly one Đề gốc and one Class.
- Student sees Homework only when assigned and allowed by membership/status.
- New attempts are blocked after deadline unless extension/reopen rules are later defined.
- Homework due state appears in Student Assigned Tests and Results.

#### FR-9: Create And Control LiveExamSession

Teacher can create LiveExamSession from a Ready Đề gốc for a Class and control whether the session is open or closed.

**Consequences:**
- LiveExamSession references exactly one Đề gốc and one Class.
- Student cannot start Live Exam before the session is open.
- Closed LiveExamSession blocks new attempts.
- [ASSUMPTION: Manual open/close is required in MVP; automatic schedule-based opening is an architecture/product decision still open.]

#### FR-10: Preserve Mode Context

System must show and persist whether work is Homework or Thi trực tiếp in Student lists, exam workspace, submissions, results and grading.

**Consequences:**
- Submission must reference either HomeworkAssignment or LiveExamSession.
- A Submission cannot reference both modes at the same time.
- Results filtering includes Mode.
- UI labels avoid using "Bài thi" generically for Đề gốc when the object is still only a reusable template.

### 5.4 Student Assigned Work And Reading/Listening Attempt

**Description:** Student sees allowed work for the active Class, opens available Homework or Live Exam, works in a stable PDF/audio + answer form workspace, and submits. Realizes UJ-2.

#### FR-11: Student Assigned Tests List

Student can view available Homework and Thi trực tiếp items grouped or filtered by mode and status.

**Consequences:**
- Empty state is tied to active Class, not a generic failure.
- Status labels include not started, in progress, submitted, not open, open now, needs grading and graded where relevant.
- Speaking routes to Speaking submission; Reading/Listening routes to exam workspace.

#### FR-12: Reading/Listening Exam Workspace

Student can view PDF by page, play audio when present, enter answers in a separate answer form, track progress, and submit.

**Consequences:**
- PDF viewer and answer form remain visible in a stable split workspace on desktop/laptop.
- Listening audio is playable in the workspace.
- Student can see active Class, Đề gốc title, Mode, save state and submit action.
- MVP does not require rendering individual parsed questions from PDF. [ASSUMPTION: Stitch question-block layout is visual inspiration only unless manually configured question metadata is later added.]

#### FR-13: Draft Answer Persistence

System saves answer drafts during Reading/Listening attempts where technically feasible.

**Consequences:**
- Autosave acknowledgement appears within 1 second on normal connection.
- Reload should restore saved/local answers where technically feasible.
- Degraded/offline state must not imply final submission succeeded.

#### FR-14: Final Submission And Auto-Grading

Student can final-submit Reading/Listening; system locks the attempt and auto-grades against AnswerKey.

**Consequences:**
- Submission confirmation warns if answers are missing.
- Final submission cannot be duplicated by double-click.
- Submitted answers become read-only for Student.
- Auto_score is stored on Submission and results become visible to Teacher.

### 5.5 Speaking Submission And Manual Grading

**Description:** Student uploads Speaking file; Teacher reviews and grades manually in Results. Realizes UJ-2 and UJ-3.

#### FR-15: Student Speaking File Submission

Student can upload a valid Speaking file, see draft upload status, and confirm final submission.

**Consequences:**
- Missing or invalid file blocks final submit with clear error.
- Uploaded-but-not-submitted file remains draft.
- Final submitted state shows filename and timestamp.
- [ASSUMPTION: MVP supports file upload first; browser recording is deferred unless explicitly pulled into MVP.]

#### FR-16: Teacher Speaking Grading

Teacher can open SpeakingSubmission, play the file, enter score and feedback, and save grading.

**Consequences:**
- Score validation enforces configured max/min score.
- Save updates row status to Đã chấm.
- Missing file error is recoverable and does not erase score/feedback draft.
- Grading context shows Student, Class, Đề gốc and Mode.

### 5.6 Results, Grading, And Dashboard

**Description:** Teacher can review submissions across classes, templates, modes, skills and grading states. Dashboard is a scan surface; detailed work happens in modules. Realizes UJ-3.

#### FR-17: Results Filtering

Teacher can filter results by Class, Đề gốc, Mode, Student, skill and status.

**Consequences:**
- No-match filter state provides a clear empty state and option to clear filters.
- Result rows preserve HomeworkAssignment or LiveExamSession context.
- Teacher cannot view results outside their scope.

#### FR-18: Master-Detail Grading Workspace

Teacher can select a result row and see detail without losing list context.

**Consequences:**
- Results table and detail/grading panel can be used side by side on desktop.
- Speaking audio player, score input, feedback and save action are together.
- Keyboard navigation and focus states work across list and detail panel.

#### FR-19: Teacher Dashboard Summary

Teacher Dashboard shows scan-level metrics and recent work, then routes to modules rather than hiding core workflows inside dashboard cards.

**Consequences:**
- Dashboard can show source templates, active Homework, live exams today, new submissions and Speaking queue.
- Primary navigation includes Dashboard, Thư viện đề, Lớp and Kết quả.
- Creating Đề gốc starts from Thư viện đề, not from an ambiguous dashboard shortcut.

### 5.7 Visual And Interaction Reference

**Description:** Implementation should use Stitch screens and Proctor & Pedagogy tokens as visual references while preserving BMad/WDS behavior.

#### FR-20: Apply Visual Mapping Without Changing Domain Semantics

Implementation can borrow layout, spacing, badges, sidebar, tables, wizard and split-panel patterns from Stitch, but must keep DD-001 domain semantics.

**Consequences:**
- Library actions say Giao homework and Tạo thi trực tiếp, not generic Giao bài only.
- Student-facing labels can use Bài tập về nhà and Thi trực tiếp.
- Results always show usage mode.
- Styling should remain calm, operational and utility-first.

## 6. Cross-Cutting Non-Functional Requirements

- **NFR-1 Performance:** Dashboard, library, assigned work and results list load initial content in under 2 seconds on normal broadband.
- **NFR-2 Autosave Feedback:** Autosave acknowledgement appears within 1 second when online.
- **NFR-3 Accessibility:** Core flows are keyboard accessible; form labels are visible/programmatic; focus order follows visual order; color contrast meets WCAG AA.
- **NFR-4 Security And Scope:** Role-based access prevents Teacher/Student viewing data outside their scope; direct route access is guarded server-side.
- **NFR-5 Data Integrity:** Submission, mark-ready, homework creation, live-session creation and grading save are protected against duplicate actions.
- **NFR-6 File Safety:** PDF/audio/Speaking storage requires secure access controls, upload progress, retry/replace behavior and recoverable errors.
- **NFR-7 Auditability:** Key state transitions should be traceable enough for Teacher support: template ready, assignment/session created, session opened/closed, submission finalized and grading saved.
- **NFR-8 Responsive Baseline:** MVP is desktop/laptop-first, but pages should degrade safely on tablet/mobile web without content overlap or blocked critical actions.

## 7. Non-Goals (Explicit)

- MVP does not parse PDF into passages/questions/answers automatically.
- MVP does not auto-grade Speaking with AI.
- MVP does not include native mobile apps.
- MVP does not include full LMS/CRM/payment/class scheduling.
- MVP does not include detailed weakness recommendation or adaptive practice generation.
- MVP does not require export to Excel/PDF.
- MVP does not require browser-based Speaking recording unless explicitly re-scoped.
- MVP does not require automatic schedule-based live-exam opening unless confirmed.

## 8. MVP Scope

### 8.1 In Scope

- Teacher and Student login with role-based access.
- Class and ClassMembership enforcement using class code entry.
- Thư viện đề for reusable Đề gốc.
- PDF upload for Reading/Listening and audio upload for Listening.
- AnswerKey setup and auto-grading for Reading/Listening.
- HomeworkAssignment with due date from Ready Đề gốc.
- LiveExamSession with manual open/close control from Ready Đề gốc.
- Student assigned work list with Homework/Thi trực tiếp distinction.
- Reading/Listening exam workspace with PDF/audio/answer form/autosave/final submit.
- Speaking file upload and manual Teacher grading.
- Teacher Results filtering and master-detail grading.
- Dashboard summary surface.
- Visual reference alignment with Stitch mapping and Proctor & Pedagogy design style.

### 8.2 Out Of Scope For MVP

- PDF parsing or question extraction.
- AI Speaking grading.
- Browser recording for Speaking unless re-scoped.
- Excel/PDF export.
- Automated scheduling engine for opening/closing LiveExamSession unless confirmed.
- Advanced rubric breakdown by Fluency/Vocabulary/Grammar/Pronunciation.
- Detailed analytics by question type beyond teacher-defined answer rows.
- Multi-tenant billing, center administration or parent portal.

## 9. Success Metrics

**Primary**

- **SM-1:** Teacher creates a reusable Reading Đề gốc from prepared PDF and answer key in under 10 minutes. Validates FR-4, FR-5, FR-6, FR-7.
- **SM-2:** Student can enter class code, log in, find allowed work and submit without teacher guidance in usability testing. Validates FR-2, FR-3, FR-11, FR-12, FR-14, FR-15.
- **SM-3:** Teacher can filter Results and grade one Speaking submission end-to-end in one workspace. Validates FR-16, FR-17, FR-18.

**Secondary**

- **SM-4:** 100% of TS-001 happy paths and blocking error tests pass before MVP sign-off.
- **SM-5:** No critical role-based access defects in QA direct-route testing.
- **SM-6:** Autosave/reload tests preserve entered Reading/Listening answers in normal online use.

**Counter-metrics**

- **SM-C1:** Do not optimize for number of template fields if it slows the under-10-minute creation goal.
- **SM-C2:** Do not optimize visual fidelity to Stitch if it breaks DD-001 behavior, access control or domain semantics.
- **SM-C3:** Do not expand PDF/question intelligence in MVP if it delays the core PDF + answer form workflow.

## 10. Input And Handoff References

- Product context: `_bmad-output/A-Product-Brief/project-brief.md`
- Development delivery: `_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml`
- Test scenario: `_bmad-output/E-Development/test-scenarios/TS-001-mvp-test-workflows.yaml`
- Accepted change proposal: `_bmad-output/E-Development/change-proposals/sprint-change-proposal-2026-06-08-homework-live-exam.md`
- Stitch mapping: `docs/stitch_h_th_ng_kh_o_th_englishtestweb/STITCH_MAPPING.md`
- Stitch source folder: `docs/stitch_h_th_ng_kh_o_th_englishtestweb`
- Page specs source: `_bmad-output/C-UX-Scenarios/`
- Visual prototype: `_bmad-output/D-Design-System/01-Visual-Design/design-concepts/dd-001-mvp-test-workflows-prototype.html`

## 11. Open Questions

1. Can Teacher extend or reopen Homework after the due date? If yes, what audit trail and Student visibility are required?
2. Should LiveExamSession open manually only, by schedule only, or both? If both, does scheduled_start automatically open the session or only display planned time?
3. What max score and validation range should Speaking use in MVP?
4. What file formats and max file sizes are allowed for PDF, Listening audio and Speaking uploads?
5. Should Student see auto-score immediately after Reading/Listening submission, or should scores remain Teacher-only until released?
6. Should AnswerKey edits be blocked after submissions exist, or create a new version for future submissions?
7. Is browser-based Speaking recording intentionally deferred, or should it be included in the first build?
8. Are Class and Student accounts created manually by Teacher/admin in MVP, or imported from a spreadsheet?

## 12. Assumptions Index

- FR-6: [ASSUMPTION] Architecture will decide whether AnswerKey edits after submissions create a new version or are blocked.
- FR-9: [ASSUMPTION] Manual open/close is required in MVP; automatic schedule-based opening is still open.
- FR-12: [ASSUMPTION] Stitch question-block layout is visual inspiration only unless manually configured question metadata is later added.
- FR-15: [ASSUMPTION] MVP supports file upload first; browser recording is deferred unless explicitly pulled into MVP.

