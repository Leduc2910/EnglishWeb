# WDS Design Log: EnglishTestWeb

**Created:** 2026-06-08  
**Current phase:** Phase 4 - UX Design

---

## Current

- Product brief completed at `_bmad-output/A-Product-Brief/project-brief.md`.
- Trigger Mapping completed in Dream mode.
- Output created at `_bmad-output/B-Trigger-Map/`.
- Phase 4 Dream mode started for Scenario 01.
- Scenario 01 page specifications completed for 7/7 pages.
- Scenario 02 page specifications completed for 5/5 pages.
- Scenario 03 page specifications completed for 3/3 pages.
- Phase 4 conceptual specifications completed for 15/15 pages.

---

## Backlog / Follow-Up

- Validate teacher test creation time target with prototype testing.
- Run usability test for student class-code, exam-taking and submission confirmation flow.
- Define UX scenarios for:
  1. Teacher creates Reading test from PDF.
  2. Student takes Reading/Listening test.
  3. Teacher reviews results and grades Speaking.

---

## Decisions

- MVP keeps PDF/audio upload as the main test creation path.
- MVP uses separate answer forms and teacher-defined answer keys.
- PDF parsing and AI Speaking grading are out of scope for MVP.
- Trigger Map prioritizes teacher workload reduction while preserving a clear student test-taking flow.
- Thư viện đề is the source template library only; Homework and Live Exam are separate usage modes created from a ready template.

---

## Progress

### 2026-06-08 — Phase 3: UX Scenarios Complete

**Agent:** Saga (Scenario Outline)
**Scenarios:** 3 scenarios covering 15 pages/views
**Quality:** Excellent

**Artifacts Created:**
- `C-UX-Scenarios/00-ux-scenarios.md` — Scenario index
- `C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio.md` — Giáo viên bận rộn tạo bài test từ PDF/audio
- `C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.1-teacher-login-account-access/1.1-teacher-login-account-access.md` — Scenario 01 step 1
- `C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.2-teacher-dashboard/1.2-teacher-dashboard.md` — Scenario 01 step 2
- `C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.3-test-list-test-library/1.3-test-list-test-library.md` — Scenario 01 step 3
- `C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.4-create-test-setup/1.4-create-test-setup.md` — Scenario 01 step 4
- `C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.5-create-test-upload-materials/1.5-create-test-upload-materials.md` — Scenario 01 step 5
- `C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.6-create-test-answer-key-scoring/1.6-create-test-answer-key-scoring.md` — Scenario 01 step 6
- `C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/1.7-create-test-review-publish/1.7-create-test-review-publish.md` — Scenario 01 step 7
- `C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop.md` — Học sinh làm bài được giao trong đúng lớp
- `C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/2.1-student-class-code-entry/2.1-student-class-code-entry.md` — Scenario 02 step 1
- `C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/2.2-student-login-account-access/2.2-student-login-account-access.md` — Scenario 02 step 2
- `C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/2.3-student-assigned-tests/2.3-student-assigned-tests.md` — Scenario 02 step 3
- `C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/2.4-student-exam-taking-reading-listening/2.4-student-exam-taking-reading-listening.md` — Scenario 02 step 4
- `C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/2.5-student-speaking-submission/2.5-student-speaking-submission.md` — Scenario 02 step 5
- `C-UX-Scenarios/03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking/03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking.md` — Giáo viên bận rộn xem kết quả và chấm Speaking
- `C-UX-Scenarios/03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking/3.1-teacher-login-account-access/3.1-teacher-login-account-access.md` — Scenario 03 step 1
- `C-UX-Scenarios/03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking/3.2-teacher-dashboard/3.2-teacher-dashboard.md` — Scenario 03 step 2
- `C-UX-Scenarios/03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking/3.3-results-grading/3.3-results-grading.md` — Scenario 03 step 3

**Summary:** Phase 3 defined three priority UX scenarios: teacher creates/publishes a Reading test from PDF with answer key, student enters class and completes assigned tests, and teacher reviews results while grading Speaking. During the process, the product direction was clarified as one website with role-based experiences, module navigation through navbar, and no dashboard CTA for creating tests. Page coverage is complete at 15/15 pages/views and all scenarios scored Excellent in quality review.

**Next:** Phase 4 — UX Design

---

## Design Loop Status

| Scenario | Page | Page name | Status | Date |
|----------|------|-----------|--------|------|
| 01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio | 01.1 | Teacher Login / Account Access | specified | 2026-06-08 |
| 01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio | 01.2 | Teacher Dashboard | specified | 2026-06-08 |
| 01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio | 01.3 | Test List / Test Library | specified | 2026-06-08 |
| 01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio | 01.4 | Create Test: Setup | specified | 2026-06-08 |
| 01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio | 01.5 | Create Test: Upload Materials | specified | 2026-06-08 |
| 01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio | 01.6 | Create Test: Answer Key & Scoring | specified | 2026-06-08 |
| 01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio | 01.7 | Create Test: Review & Publish | specified | 2026-06-08 |
| 02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop | 02.1 | Student Class Code Entry | specified | 2026-06-08 |
| 02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop | 02.2 | Student Login / Account Access | specified | 2026-06-08 |
| 02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop | 02.3 | Student Assigned Tests | specified | 2026-06-08 |
| 02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop | 02.4 | Student Exam Taking: Reading/Listening | specified | 2026-06-08 |
| 02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop | 02.5 | Student Speaking Submission | specified | 2026-06-08 |
| 03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking | 03.1 | Teacher Login / Account Access | specified | 2026-06-08 |
| 03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking | 03.2 | Teacher Dashboard | specified | 2026-06-08 |
| 03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking | 03.3 | Results & Grading | specified | 2026-06-08 |

---

### 2026-06-08 — Phase 4: Scenario 01 UX Design Complete

**Agent:** Freya (Dream Up Design)  
**Scenario:** Giáo viên bận rộn tạo bài test từ PDF/audio  
**Pages specified:** 7/7  
**Status:** specified

**Summary:** Created development-ready page specifications for the teacher test-creation flow: login, teacher dashboard, test library, setup, PDF upload, answer key/scoring, and review/publish. The design keeps Dashboard as a summary surface, uses the "Bài test" navbar module as the entry into test creation, and treats PDF upload plus separate answer key entry as the MVP path.

---

### 2026-06-08 — Phase 4: Scenario 02 UX Design Complete

**Agent:** Freya (Dream Up Design)  
**Scenario:** Học sinh làm bài được giao trong đúng lớp  
**Pages specified:** 5/5  
**Status:** specified

**Summary:** Created development-ready page specifications for the student flow: class code entry, student login with preserved class context, assigned-test list, Reading/Listening exam workspace with autosave, and Speaking file submission. The design prioritizes clear class/test identity, answer persistence, and explicit submission confirmation.

---

### 2026-06-08 — Phase 4: Scenario 03 UX Design Complete

**Agent:** Freya (Dream Up Design)  
**Scenario:** Giáo viên bận rộn xem kết quả và chấm Speaking  
**Pages specified:** 3/3  
**Status:** specified

**Summary:** Created development-ready page specifications for the teacher results flow: login, dashboard entry to Results, and a master-detail Results & Grading workspace. The design keeps result filtering, Speaking audio playback, score entry, and feedback in one focused surface.

---

### 2026-06-08 — Design Delivery: DD-001 MVP Test Workflows

**Agent:** Freya (Design Delivery)  
**Delivery:** `_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml`  
**Test Scenario:** `_bmad-output/E-Development/test-scenarios/TS-001-mvp-test-workflows.yaml`  
**Handoff Log:** `_bmad-output/E-Development/deliveries/DD-001-handoff-log.md`  
**Status:** ready_for_handoff

**Summary:** Packaged the complete MVP design set into a development handoff covering teacher test creation, student assigned-test completion, and teacher results/Speaking grading. The delivery references all 15 specified pages and defines acceptance criteria, edge cases, accessibility checks, and QA guidance.

---

### 2026-06-08 — Visual Design: DD-001 HTML Prototype

**Agent:** Freya (Visual Design)  
**Approach:** HTML Prototype  
**Artifact:** `_bmad-output/D-Design-System/01-Visual-Design/design-concepts/dd-001-mvp-test-workflows-prototype.html`  
**Delivery:** DD-001 MVP Test Workflows

**Summary:** Created an interactive one-file HTML prototype covering all 15 DD-001 screens: teacher test creation, student class/test/submission flow, and teacher results/Speaking grading. The prototype is intended for visual review and early user/stakeholder feedback; detailed page specs remain the implementation source of truth.

---

### 2026-06-08 — Correct Course: Homework And Live Exam Model

**Agent:** BMad Correct Course  
**Change Proposal:** `_bmad-output/E-Development/change-proposals/sprint-change-proposal-2026-06-08-homework-live-exam.md`  
**Delivery Updated:** `_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml`  
**Test Scenario Updated:** `_bmad-output/E-Development/test-scenarios/TS-001-mvp-test-workflows.yaml`  
**Status:** accepted for handoff correction

**Summary:** Corrected the DD-001 business model so "Thư viện đề" represents reusable source templates only. Teachers now use a ready template to create either Homework with due date or Live Exam sessions for in-class work. UX specs, delivery data model, acceptance criteria, test scenario, handoff log, and prototype labels were updated to preserve this distinction.

---

## Key Decisions

| Date | Decision | Context | Owner |
|------|----------|---------|-------|
| 2026-06-08 | Use one website with role-based experiences instead of separate teacher and student websites. | Phase 3: Scenarios | Saga + Đức |
| 2026-06-08 | Teacher Dashboard is a summary surface; teachers navigate to modules such as "Bài test" and "Kết quả" from the navbar. | Phase 3: Scenarios | Saga + Đức |
| 2026-06-08 | Scenario 01 keeps class/student management out of the fast test-creation sunshine path because existing classes/students are prerequisites for the under-10-minute creation goal. | Phase 3: Scenarios | Saga + Đức |
| 2026-06-08 | Phase 4 design intent is Dream Up (`D`) for all three scenarios. | Phase 3: Handover | Saga + Đức |
| 2026-06-08 | Thư viện đề represents reusable test templates; Homework and Live Exam are separate delivery modes created from a ready template. | Correct Course: DD-001 | BMad + Đức |
