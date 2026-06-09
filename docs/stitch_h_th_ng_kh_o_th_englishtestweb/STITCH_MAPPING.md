# Stitch UI Mapping: EnglishTestWeb

**Created:** 2026-06-09  
**Source folder:** `docs/stitch_h_th_ng_kh_o_th_englishtestweb`  
**Purpose:** Map Stitch-generated UI screens to the current BMad/WDS product model.

---

## Business Model Reminder

- **Thư viện đề** is the reusable source template library.
- A ready template can be used as:
  - **Homework**: assigned with due date.
  - **Thi trực tiếp / Live Exam**: opened or controlled by teacher during class.
- Student submissions must preserve whether they came from HomeworkAssignment or LiveExamSession.

---

## Screen Mapping

| Stitch folder | Screen meaning | Maps to WDS/BMad spec | Fit | Notes |
|---------------|----------------|------------------------|-----|-------|
| `b_ng_i_u_khi_n_gi_o_vi_n` | Teacher dashboard | `01.2 Teacher Dashboard`, `03.2 Teacher Dashboard` | Good | Add metrics for source templates, active homework, and live exams today. Current Speaking queue is useful. |
| `th_vi_n` | Question/Test Library | `01.3 Question / Test Library` | Strong | Keep this as source of truth for library visual direction. Clarify card action: "Giao homework" and "Tạo thi trực tiếp", not generic "Giao bài" only. |
| `t_o_m_u_thi` | Create template wizard, upload step | `01.4-01.7 Create Template Wizard` | Strong | Split upload/preview layout is good. Ensure setup does not ask class/deadline; those belong to Homework/Live Exam creation. |
| `b_i_thi_c_a_t_i` | Student assigned work list | `02.3 Student Assigned Tests` | Very strong | Already matches Homework / Thi trực tiếp tabs. Keep status model: Chưa mở, Đang mở, Đã nộp, Chờ chấm điểm. |
| `ph_ng_thi_tr_c_tuy_n` | Reading/Listening exam workspace | `02.4 Student Exam Taking` | Strong, with scope decision | UI renders questions directly. Current MVP specs allow PDF/source material plus answer form. Decide whether to implement rendered question blocks now or keep PDF viewer hybrid. |
| `n_p_b_i_thi_n_i_speaking` | Speaking submission | `02.5 Student Speaking Submission` | Strong | Recording + upload tabs are useful. MVP can start with upload-only if browser recording is too much for first build. |
| `k_t_qu_ch_m_b_i` | Results and grading | `03.3 Results & Grading` | Very strong | Use as main reference for master-detail grading workspace. Add filters for class, template, mode, skill, status where needed. |
| `proctor_pedagogy/DESIGN.md` | Design tokens/style guide | Design system reference | Strong | Use colors, typography, badges, sidebar, tables, wizard, and split-panel guidance. |

---

## Recommended Implementation Priority

1. **Teacher shell + navigation**
   - Sidebar, topbar, role switch, shared layout.
2. **Thư viện đề**
   - Template cards/table, create-template CTA, ready/draft states.
3. **Create template wizard**
   - Setup, upload, answer key, review/next action.
4. **Student assigned work**
   - Homework / Thi trực tiếp tabs and cards.
5. **Exam workspace**
   - Reading/Listening split workspace.
6. **Speaking submission**
   - Upload or recording workflow.
7. **Results & grading**
   - Master-detail submissions list and Speaking grading panel.

---

## Required UI Wording Corrections

- Prefer **Đề gốc** or **Mẫu đề** for reusable templates.
- Prefer **Giao homework** and **Tạo thi trực tiếp** for template usage actions.
- Avoid using **Bài thi** as the generic label for all library items when the object is still only a source template.
- Student-facing labels can use **Bài tập về nhà** and **Thi trực tiếp**.
- Results should show the usage mode: Homework or Thi trực tiếp.

---

## Product Decisions Still Open

1. Can teachers extend or reopen Homework after the due date?
2. Do Live Exam sessions open manually, by schedule, or both?
3. Should the first build render individual questions like the Stitch exam workspace, or keep a PDF viewer plus answer form?
4. Is browser-based Speaking recording in MVP, or is file upload enough for the first slice?

---

## Recommendation

Use Stitch as the visual source for layout and styling, but keep BMad/WDS artifacts as the behavior source of truth. The Stitch screens are strong enough to guide implementation after the wording and scope decisions above are resolved.
