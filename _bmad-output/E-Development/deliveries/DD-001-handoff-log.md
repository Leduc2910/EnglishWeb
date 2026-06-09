# DD-001 Handoff Log: MVP Test Workflows

**Delivery:** DD-001 MVP Test Workflows  
**Test Scenario:** TS-001 MVP Test Workflows Validation  
**Created:** 2026-06-08T21:12:30+07:00  
**Status:** Ready for BMad architecture/development handoff, corrected for Homework/Live Exam model

---

## Handoff Summary

Freya packaged the completed Phase 4 UX specifications for EnglishTestWeb into the first development delivery.

This delivery covers the complete MVP loop:

1. Teacher creates a reusable source test/template in Thư viện đề from PDF/audio and answer key.
2. Teacher uses the source template as either Homework with due date or Live Exam for in-class work.
3. Student enters class by class code, logs in, completes available homework/live exam work, and submits.
4. Teacher reviews results and grades Speaking.

All 15 page specs are marked `specified` in the design log.

---

## Key Design Intent

- Keep PDF/audio upload as the primary MVP path.
- Do not parse PDF into questions in MVP.
- Use separate teacher-defined answer keys for Reading/Listening auto-grading.
- Treat Thư viện đề as the source template library only; it does not represent assigned homework or live exam sessions.
- Model Homework and Live Exam as separate usage modes created from a ready template.
- Preserve role-based experience inside one website.
- Keep Teacher Dashboard as a summary surface; teachers navigate to modules from navbar.
- Make student safety cues explicit: class confirmation, active class context, autosave state, and submission confirmation.
- Keep Speaking grading in one focused master-detail workspace with audio player, score, and feedback together.

---

## Artifacts

- Design Delivery: `_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml`
- Test Scenario: `_bmad-output/E-Development/test-scenarios/TS-001-mvp-test-workflows.yaml`
- Scenario specs:
  - `_bmad-output/C-UX-Scenarios/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/`
  - `_bmad-output/C-UX-Scenarios/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/`
  - `_bmad-output/C-UX-Scenarios/03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking/`
- Design log: `_bmad-output/_progress/00-design-log.md`

---

## Recommended Epic Breakdown

1. **Auth, roles, classes**
   - Teacher/student login
   - Class code lookup
   - Student-class membership enforcement

2. **Teacher template creation and use**
   - Question/Test Library
   - Template setup wizard
   - PDF upload
   - Answer key/scoring
   - Review template and next action
   - Create HomeworkAssignment with due date
   - Create/open/close LiveExamSession for in-class exam

3. **Student test taking**
   - Assigned Tests
   - Homework vs Live Exam states
   - Reading/Listening exam workspace
   - Autosave and submission locking
   - Speaking upload and final submission

4. **Results and grading**
   - Results filtering
   - Auto-score display
   - Speaking audio playback
   - Score/feedback save

---

## Open Implementation Questions

| Question | Why it matters | Suggested owner |
|----------|----------------|-----------------|
| What frontend/backend stack will be used? | Delivery names requirements but project architecture is not yet documented. | BMad Architect |
| What file storage backend will host PDFs/audio/Speaking files? | Secure file access is core to tests and submissions. | BMad Architect |
| What is the max file size and allowed Speaking formats? | Needed for validation and user-facing errors. | BMad Architect + Đức |
| What is the exact scoring max for Speaking? | Needed for Results & Grading validation. | Đức |
| Should submitted students see scores immediately? | Affects student post-submit/result views, not fully specified yet. | Đức |
| Can homework be reopened or extended after deadline? | Affects HomeworkAssignment state rules and audit trail. | Đức + BMad Architect |
| How should Live Exam sessions be opened: scheduled automatically or manually by teacher? | Affects in-class timing, locking, and student access states. | Đức + BMad Architect |

---

## Handoff Status

This log is a prepared handoff briefing. Official BMad architecture/development handoff should happen next in a fresh BMad architecture or implementation planning context.
