---
title: "Addendum: EnglishTestWeb PRD Source Notes"
created: "2026-06-09"
updated: "2026-06-09"
---

# Addendum: EnglishTestWeb PRD Source Notes

## Source Hierarchy

1. `prd.md` is the consolidated product requirements bridge for BMad Architecture.
2. `_bmad-output/E-Development/deliveries/DD-001-mvp-test-workflows.yaml` is the delivery-level source for acceptance criteria, domain model and implementation handoff.
3. `_bmad-output/C-UX-Scenarios/` contains page-level WDS specs and object IDs.
4. `docs/stitch_h_th_ng_kh_o_th_englishtestweb/STITCH_MAPPING.md` maps imported Stitch screens to the BMad/WDS model.
5. Stitch HTML/screens are visual references only; they should not override domain semantics.

## Stitch Implementation Notes

- Use `th_vi_n` as the strongest visual reference for Thư viện đề, but change usage actions to "Giao homework" and "Tạo thi trực tiếp".
- Use `t_o_m_u_thi` for the wizard/upload split layout, but do not add class/deadline/session fields to template setup.
- Use `b_i_thi_c_a_t_i` for Student assigned work list and mode tabs.
- Use `ph_ng_thi_tr_c_tuy_n` for exam workspace layout, but MVP remains PDF viewer plus answer form unless question rendering is explicitly scoped.
- Use `n_p_b_i_thi_n_i_speaking` for Speaking submission; file upload is the assumed MVP path.
- Use `k_t_qu_ch_m_b_i` for Results & Grading master-detail workspace.
- Use `proctor_pedagogy/DESIGN.md` for visual tokens: calm operational UI, Inter typography, green primary actions, amber pending states, blue live-session states, tables, wizard and split-panel patterns.

## Architecture Handoff Notes

- Architecture should decide storage and secure access for PDFs, Listening audio and Speaking files.
- Architecture should decide AnswerKey edit/version rules after submissions exist.
- Architecture should decide whether scheduled LiveExamSession fields only display schedule or trigger automatic open/close.
- Architecture should preserve Submission mode integrity: a Submission references exactly one HomeworkAssignment or one LiveExamSession.

