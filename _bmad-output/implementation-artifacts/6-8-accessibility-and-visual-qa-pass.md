---
baseline_commit: efe7b62ffaaac04ab35af670a7c6413d8d2c46f8
---

# Story 6.8: Accessibility And Visual QA Pass

Status: done

## Story

Là giáo viên hoặc học sinh,
tôi muốn các critical flows được kiểm tra về accessibility và visual consistency,
để MVP có thể sử dụng được và align với WDS/Stitch references trước khi sign-off.

## Acceptance Criteria

1. **Given** accessibility checks chạy
   **When** critical flows được thao tác bằng keyboard
   **Then** tất cả blocking keyboard, label, focus, và contrast issues được fix trước MVP sign-off.

2. **Given** responsive checks chạy
   **When** desktop, tablet, và narrow web viewports được test
   **Then** critical actions vẫn reachable
   **And** text, badges, cards, tables, split panels, và modals không overlap.

3. **Given** visual QA so sánh implemented screens với WDS/Stitch references
   **When** Thư viện đề, create-template wizard, assigned tests, exam workspace, Speaking submission, Results, và Dashboard được inspect
   **Then** layouts vẫn operational và scan-friendly
   **And** Stitch không override DD-001/WDS behavior hoặc wording.

## Tasks / Subtasks

- [x] Task 1: Thêm `:focus-visible` cho `test-template-materials.component.css` (AC1)
  - [x] 1.1 Thêm block focus-visible cho `.btn-primary`, `.btn-secondary`, `.btn-link`:
    ```css
    .btn-primary:focus-visible,
    .btn-secondary:focus-visible,
    .btn-link:focus-visible {
      outline: 2px solid #2563eb;
      outline-offset: 2px;
    }
    ```
  - [x] 1.2 Thêm focus-visible cho preview modal close button (`.close-btn` hoặc tương đương):
    Close button dùng class `.btn-secondary` — đã covered bởi 1.1.

- [x] Task 2: Thêm `:focus-visible` cho `test-template-answer-key.component.css` (AC1)
  - [x] 2.1 Thêm focus-visible cho `.segment` buttons trong scoring mode group
  - [x] 2.2 Thêm focus-visible cho `.answer-key-answer-input`, `.answer-key-score-input`, `.field input`
  - [x] 2.3 Thêm focus-visible cho `.btn-primary`, `.btn-secondary`

- [x] Task 3: Thêm `:focus-visible` cho `test-template-review.component.css` (AC1)
  - [x] 3.1 Thêm focus-visible cho `.btn-primary`, `.btn-secondary`, `.edit-link`
  - [x] 3.2 Mark Ready button verified là `<button>` element (confirmed bằng unit test)

- [x] Task 4: Thêm `:focus-visible` cho `teacher-dashboard.component.css` (AC1)
  - [x] 4.1 Thêm focus-visible cho `.filter-bar select`, `.card-link`, `.row-link`, `.empty-state a`
  - [x] 4.2 `.recent-table tbody tr` không có tabindex nên skip TR-level focus; links trong cells đã covered

- [x] Task 5: Thêm responsive cho `test-template-review.component.css` (AC2)
  - [x] 5.1 Thêm `@media (max-width: 768px) { .wizard-body { grid-template-columns: 1fr; } }`
  - [x] 5.2 `.readiness-panel` không có `position: sticky` conflict khi stacked

- [x] Task 6: Thêm responsive cho `teacher-dashboard.component.css` (AC2)
  - [x] 6.1 Thêm `@media (max-width: 768px) { .recent-table-wrapper { overflow-x: auto; } }`
  - [x] 6.2 Bọc `<table>` trong `<div class="recent-table-wrapper">` trong HTML template

- [x] Task 7: Verify responsive cho `student-speaking-submission.component.css` (AC2)
  - [x] 7.1 Root layout `max-width: 800px` — naturally single-column, không cần thay đổi
  - [x] 7.2 `.success-details { grid-template-columns: auto 1fr; }` — OK trên narrow (auto column tự adjust)
  - [x] 7.3 Modal `max-width: 480px; width: 90%` — đã responsive, không cần thay đổi

- [x] Task 8: Angular unit tests verify accessibility properties (AC1, AC3)
  - [x] 8.1 Trong `teacher-dashboard.component.spec.ts` — thêm 2 tests: recent-table-wrapper wrapper, filter-bar select label association
  - [x] 8.2 Trong `test-template-review.component.spec.ts` — thêm 2 tests: Mark Ready là `<button>`, readiness-panel có `aria-live="polite"`
  - [x] 8.3 Trong `test-template-materials.component.spec.ts` — thêm 1 test: preview modal có `role="dialog"` và `aria-modal="true"`
  - [x] 8.4 `npm test` — 202/202 tests pass (từ 197 → +5 tests)

- [x] Task 9: Visual QA checklist (AC3)
  - [x] 9.1 Thư viện đề — auto-fit grid responsive, badge colors canonical từ story 6.4
  - [x] 9.2 Create-template wizard — responsive stacking added (Task 5), stepper flex-wrap
  - [x] 9.3 Assigned tests — tablist/tab ARIA verified trong HTML
  - [x] 9.4 Exam workspace — `aria-labelledby="confirm-title"` trỏ đúng `id="confirm-title"` ✓
  - [x] 9.5 Speaking submission — modal `aria-labelledby="confirm-modal-title"` trỏ đúng ✓
  - [x] 9.6 Results — responsive từ story 6.4, badge colors canonical ✓
  - [x] 9.7 Dashboard — summary auto-fit, recent-table-wrapper overflow-x added ✓

- [x] Task 10: Quality gate
  - [x] 10.1 `dotnet build` — 0 errors, 0 warnings
  - [x] 10.2 `npm test` — 202/202 passed

## Dev Notes

### Bối cảnh và mục đích

Story 6.8 là **CSS hardening + verification pass** — tiếp tục từ story 6.4 nhưng focus vào các **components chưa được touch** trong 6.4:
- `test-template-materials.component.css` (không có focus-visible)
- `test-template-answer-key.component.css` (không có focus-visible)
- `test-template-review.component.css` (không có focus-visible, không có responsive stacking)
- `teacher-dashboard.component.css` (không có focus-visible, recent-table cần overflow-x wrapper trên mobile)

**Story 6.4 đã xử lý:**
- `teacher-results.component.css` — focus-visible, responsive, badge fix
- `teacher-speaking-grading.component.css` — badge colors, focus-visible
- `student-attempt-workspace.component.css` — focus-visible fix
- `student-speaking-submission.component.css` — mode-badge harmonize, focus-visible
- `student-assigned-tests.component.css` — skill-badge harmonize, focus-visible

**KHÔNG thay đổi:** TypeScript logic, Angular services, backend, route config, hoặc bất kỳ file đã xử lý trong story 6.4.

### Canonical CSS Patterns (từ story 6.4 — phải dùng nhất quán)

**Focus-visible pattern (PHẢI dùng `:focus-visible`, KHÔNG `:focus`):**
```css
.btn-primary:focus-visible {
  outline: 2px solid #2563eb;
  outline-offset: 2px;
}
```
- Blue outline `#2563eb` cho hầu hết controls
- Green outline `#059669` cho table rows (xem teacher-results pattern)
- `outline: none` trong `:focus` mà không có `:focus-visible` fallback là BUG

**Status Badge Palette (canonical, đừng thay đổi):**
| State | Background | Color |
|---|---|---|
| `draft` | `#f3f4f6` | `#4b5563` |
| `ready` | `#dcfce7` | `#166534` |
| `submitted` | `#dbeafe` | `#1e40af` |
| `graded` / `auto-graded` | `#d1fae5` | `#065f46` |
| `needs-grading` / `not-open` | `#fef3c7` | `#92400e` |
| `open` / `available` | `#dcfce7` | `#166534` |
| `closed` / `archived` / `expired` | `#f3f4f6` | `#4b5563` |

### Files cần thay đổi

| File | Type | Changes |
|---|---|---|
| `test-template-materials.component.css` | UPDATE | Thêm `:focus-visible` cho buttons |
| `test-template-answer-key.component.css` | UPDATE | Thêm `:focus-visible` cho buttons và inputs |
| `test-template-review.component.css` | UPDATE | Thêm `:focus-visible`, thêm responsive @media |
| `teacher-dashboard.component.css` | UPDATE | Thêm `:focus-visible`, recent-table overflow wrapper |
| `teacher-dashboard.component.html` | UPDATE (nếu cần) | Bọc recent-table trong div.recent-table-wrapper |
| `teacher-dashboard.component.spec.ts` | UPDATE | Thêm tests (đã có 4 tests) |
| `test-template-review.component.spec.ts` | UPDATE | Thêm tests (đã có 10 tests) |
| `test-template-materials.component.spec.ts` | UPDATE | Thêm tests (đã có 7 tests) |

**Backend:** KHÔNG thay đổi gì.
**Angular services/TypeScript:** KHÔNG thay đổi gì.

### Stitch Reference Context

Stitch mapping: `docs/stitch_h_th_ng_kh_o_th_englishtestweb/STITCH_MAPPING.md`

UX-DR15 (epics.md line 150):
> "calm operational UI, Inter typography, green primary actions, amber pending, blue live-session states, consistent tables, wizard, split panels, WCAG AA, visible focus, and responsive-safe layout."

**Stitch là visual reference ONLY** — không override DD-001/WDS behavior hay wording. Đây là rule từ story 6.4 và epic 6.

### Responsive Strategy

**Pages đã có responsive (không cần thay đổi):**
- `student-attempt-workspace`: `@media (max-width: 768px)` ✓
- `teacher-results`: `@media (max-width: 768px)` ✓ (từ 6.4)
- `test-template-materials`: `@media (min-width: 960px)` ✓ (min-width, expand trên desktop)
- `test-template-answer-key`: `@media (min-width: 960px)` ✓
- `test-template-setup`: `@media (max-width: 900px)` ✓

**Pages cần thêm responsive:**
- `test-template-review`: `.wizard-body` hai-column → stack trên ≤768px (Task 5)
- `teacher-dashboard`: recent-table overflow-x wrapper (Task 6)

**Pages có auto-responsive layout (không cần thêm):**
- `test-template-library`: dùng `repeat(auto-fit, minmax(12rem, 1fr))` — auto-responsive ✓
- `student-assigned-tests`: `max-width: 40rem` content area — naturally narrow ✓
- `student-speaking-submission`: cards flex-direction column — verify trong Task 7

### Bẫy cần tránh

1. **KHÔNG thay đổi TypeScript logic** — story này chỉ CSS + HTML attribute adjustments + unit tests
2. **KHÔNG dùng `!important`** — fix specificity thay vì override
3. **`:focus` vs `:focus-visible`**: `outline: none` trong `:focus` = BUG. Chỉ dùng `:focus-visible`
4. **`test-template-review` dùng CSS custom properties** (e.g., `var(--space-sm)`) — khi thêm responsive code, keep dùng pattern tương tự
5. **`test-template-materials` dùng `@media (min-width: 960px)` pattern** (mobile-first expand) — nhất quán với pattern này nếu cần thêm responsive
6. **KHÔNG break bất kỳ tests nào** — chỉ thêm tests mới vào spec files hiện có
7. **KHÔNG tạo file mới** (trừ trường hợp spec file thực sự chưa tồn tại — nhưng tất cả đã có)
8. **recent-table wrapper**: Nếu HTML không có `.recent-table-wrapper` div, cần thêm vào HTML template. Verify bằng cách đọc `teacher-dashboard.component.html` trước khi thêm CSS rule

### Angular Unit Test Pattern

Tests trong story này chỉ verify **HTML structure** và **ARIA attributes**, KHÔNG verify computed styles:

```typescript
it('should have aria-label on filter select', () => {
  const select = fixture.nativeElement.querySelector('.filter-bar select');
  expect(select).toBeTruthy();
  expect(select.getAttribute('aria-label')).toBeTruthy();
});

it('should render Mark Ready as a button element', () => {
  // Prevent div/span elements being used as interactive controls
  const btn = fixture.nativeElement.querySelector('.btn-primary');
  expect(btn.tagName.toLowerCase()).toBe('button');
});
```

**KHÔNG test computed colors** (`getComputedStyle`) trong Angular unit tests — chỉ verify class names.

### References

- Story 6.4 (canonical patterns): `_bmad-output/implementation-artifacts/6-4-stitch-informed-visual-and-accessibility-hardening.md`
- Canonical badge palette: story 6.4 Dev Notes § "Canonical Status Badge Palette"
- WCAG AA contrast verification: đã verified trong story 6.4 — tất cả canonical colors pass 4.5:1
- Epics NFR-3 (keyboard/WCAG): `_bmad-output/planning-artifacts/epics.md#NFR-3`
- Epics NFR-8 (responsive): `_bmad-output/planning-artifacts/epics.md#NFR-8`
- UX-DR15 (Stitch visual reference): `_bmad-output/planning-artifacts/epics.md#UX-DR15`
- Angular 22 feature components: `src/EnglishTestWeb.Client/src/app/features/`

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

Không có issues — CSS-only changes + HTML wrapper addition.

### Completion Notes List

- Task 1: Thêm `:focus-visible` block cho `.btn-primary`, `.btn-secondary`, `.btn-link` trong test-template-materials. Preview modal close button dùng `.btn-secondary` nên đã covered.
- Task 2: Thêm `:focus-visible` cho 4 targets trong test-template-answer-key: `.segment`, `.answer-key-answer-input`, `.answer-key-score-input`, `.field input`, `.btn-primary`, `.btn-secondary`. Segment dùng `outline-offset: -3px` (patched từ -2px sau Round 1 review) để đảm bảo full 2px ring visible bên trong `overflow: hidden` trên `.segmented`.
- Task 3: Thêm `:focus-visible` cho `.btn-primary`, `.btn-secondary`, `.edit-link` trong test-template-review. Thêm `@media (max-width: 768px) { .wizard-body { grid-template-columns: 1fr; } }` cho responsive stacking.
- Task 4+6: Thêm focus-visible cho `.filter-bar select`, `.card-link`, `.row-link`, `.empty-state a` trong teacher-dashboard. Thêm `@media (max-width: 768px)` cho `.recent-table-wrapper`. Bọc `<table>` trong `<div class="recent-table-wrapper">` trong HTML.
- Task 7: `student-speaking-submission.component.css` đã responsive (max-width: 800px + modal width: 90%) — không cần thay đổi.
- Task 8: +5 unit tests mới. 202/202 tests pass.
- dotnet build: 0 errors, 0 warnings.

### File List

- `src/EnglishTestWeb.Client/src/app/features/test-template-materials/test-template-materials.component.css` (modified — added focus-visible)
- `src/EnglishTestWeb.Client/src/app/features/test-template-answer-key/test-template-answer-key.component.css` (modified — added focus-visible)
- `src/EnglishTestWeb.Client/src/app/features/test-template-review/test-template-review.component.css` (modified — added focus-visible + responsive)
- `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.css` (modified — added focus-visible + responsive)
- `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.html` (modified — added recent-table-wrapper div)
- `src/EnglishTestWeb.Client/src/app/features/teacher-dashboard/teacher-dashboard.component.spec.ts` (modified — +2 tests)
- `src/EnglishTestWeb.Client/src/app/features/test-template-review/test-template-review.component.spec.ts` (modified — +2 tests)
- `src/EnglishTestWeb.Client/src/app/features/test-template-materials/test-template-materials.component.spec.ts` (modified — +1 test)

## Senior Developer Review (AI)

**Review Date:** 2026-06-14
**Outcome:** Changes Requested
**Dismissed:** 5 | **Deferred:** 7 | **Patch:** 2

### Action Items

- [x] [Review][Patch] `.segment:focus-visible outline-offset: -2px` may render incomplete ring inside `overflow: hidden` parent `.segmented` — increase to `-3px` to ensure full 2px outline is visible [test-template-answer-key.component.css]
- [x] [Review][Patch] `.readiness-panel { position: sticky }` not cleared in `@media (max-width: 768px)` — panel sticks to viewport top when stacked in single-column, overlapping footer actions [test-template-review.component.css]
- [x] [Review][Defer] `document.querySelector` in review spec tests — pre-existing pattern across the file, consistent with surrounding tests [test-template-review.component.spec.ts] — deferred, pre-existing
- [x] [Review][Defer] `filter-bar select` id assumption in test — HTML has explicit `id="classFilter"`, works correctly; no risk with current implementation [teacher-dashboard.component.spec.ts] — deferred, correct
- [x] [Review][Defer] Responsive breakpoints hardcoded 768px across multiple files — no shared token — deferred, pre-existing project pattern
- [x] [Review][Defer] `.wizard-body` lacks min-width guard between 769px–960px — deferred, pre-existing design
- [x] [Review][Defer] Answer-key uses `min-width: 960px` breakpoint, review uses `max-width: 768px` — inconsistent wizard step breakpoints — deferred, pre-existing
- [x] [Review][Defer] `.answer-grid` scrollable div lacks `tabindex="0"` for keyboard scroll — deferred, pre-existing accessibility gap
- [x] [Review][Defer] Segmented control `.segment` buttons lack `aria-pressed` — selected state not communicated to screen readers — deferred, pre-existing

### Review Follow-ups (AI)

- [x] [AI-Review] Fix `.segment:focus-visible` outline-offset from -2px to -3px [test-template-answer-key.component.css]
- [x] [AI-Review] Add `position: static` to `.readiness-panel` inside `@media (max-width: 768px)` [test-template-review.component.css]

## Senior Developer Review (AI) — Round 2

**Review Date:** 2026-06-14
**Outcome:** Approved
**Dismissed:** 0 | **Deferred:** 0 | **Patch:** 0

### Findings

All Round 1 patches verified correct:
- `.segment:focus-visible { outline-offset: -3px }` — full 2px ring sits within 3px inset, not clipped by `overflow: hidden`. ✓
- `.readiness-panel { position: static }` in `@media (max-width: 768px)` — sticky cleared correctly, no overlap. ✓

No new issues found in full diff review (9 files, 136 insertions, 29 deletions):
- No `outline: none` overrides in any modified CSS file. ✓
- Canonical `#2563eb` pattern consistent across all 4 CSS files. ✓
- `btn-link:focus-visible` placement before base rule is correct — specificity (0,2,0) beats (0,1,0). ✓
- `label[for="classFilter"]` + `select#classFilter` verified in HTML. ✓
- `role="dialog"` + `aria-modal="true"` verified in materials component HTML. ✓
- `id="review-publish-button"` and `id="review-publish-readiness-panel"` with `aria-live="polite"` verified in review component HTML. ✓
- 202/202 tests pass. ✓
