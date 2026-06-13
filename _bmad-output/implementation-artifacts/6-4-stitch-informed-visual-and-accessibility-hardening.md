---
baseline_commit: a744189
---

# Story 6.4: Stitch-Informed Visual And Accessibility Hardening

Status: done

## Story

Là giáo viên hoặc học sinh,
tôi muốn các màn hình MVP cảm thấy nhất quán, rõ ràng và dễ tiếp cận,
để các quy trình kiểm tra vận hành dễ scan và an toàn khi sử dụng.

## Acceptance Criteria

1. **Given** implementation dùng Stitch references
   **When** UI copy và layout conflict với PRD/DD/WDS domain semantics
   **Then** PRD/DD/WDS semantics thắng
   **And** Stitch chỉ là visual/layout inspiration.

2. **Given** Thư viện đề, create-template wizard, assigned tests, exam workspace, Speaking submission, và Results pages đã implemented
   **When** visual review được thực hiện
   **Then** layout patterns align với approved references: sidebar/nav, table density, wizard stepper, upload panels, split workspace, badges, và master-detail.

3. **Given** status badges được render
   **When** states bao gồm Draft, Ready, Homework, Thi trực tiếp, Not open, Open now, Submitted, Needs grading, Graded
   **Then** màu sắc và label nhất quán và accessible
   **And** mode wording KHÔNG collapse Homework và Live Exam vào generic "Bài thi".

4. **Given** forms, tables, modals, players, upload zones, và split panels được sử dụng
   **When** accessibility checks chạy
   **Then** labels visible/programmatic, contrast WCAG AA, focus visible, keyboard navigation đến được tất cả critical controls.

5. **Given** desktop, tablet, và narrow web viewports
   **When** responsive checks chạy
   **Then** text và controls không overlap, critical actions reachable, không page nào block core completion.

6. **Given** final UI dùng shared primitives
   **When** repeated components được inspect
   **Then** status badges, empty states, error banners, và shell navigation nhất quán hoặc intentionally consistent.

## Tasks / Subtasks

- [x] Task 1: Fix `teacher-results.component.css` — typo và focus states (AC3, AC4)
  - [x] 1.1 Fix typo: thay `border-radius: 9999py` → `border-radius: 9999px` trong rule `.status-badge`
  - [x] 1.2 Thêm `:focus-visible` outline cho filter inputs, buttons, table rows:
    ```css
    .filter-bar select:focus-visible,
    .filter-bar input:focus-visible,
    .filter-bar button:focus-visible,
    .pagination button:focus-visible,
    .close-btn:focus-visible,
    .save-btn:focus-visible,
    .next-btn:focus-visible,
    .empty-state button:focus-visible {
      outline: 2px solid #2563eb;
      outline-offset: 2px;
    }
    ```
  - [x] 1.3 Table row focus — đã có `results-table tbody tr:focus-visible` với `outline: 2px solid #059669` — giữ nguyên (accessible). Verify tabindex="0" đã có trong HTML ✓

- [x] Task 2: Harmonize `teacher-speaking-grading.component.css` badge colors (AC3, AC6)
  - [x] 2.1 Fix `.status-submitted`: hiện tại là amber (`#fef3cd / #856404`), cần là blue để match `teacher-results`:
    ```css
    .status-submitted { background: #dbeafe; color: #1e40af; }
    ```
  - [x] 2.2 Fix `.status-graded`: hiện `#d4edda / #155724`, cần match canonical green:
    ```css
    .status-graded { background: #d1fae5; color: #065f46; }
    ```
  - [x] 2.3 Fix `.status-draft`: hiện là RED (`#f8d7da / #721c24`) — semantically sai (draft không phải error), cần là gray:
    ```css
    .status-draft { background: #f3f4f6; color: #4b5563; }
    ```
  - [x] 2.4 Thêm `:focus-visible` cho `.primary-button`:
    ```css
    .primary-button:focus-visible {
      outline: 2px solid #2563eb;
      outline-offset: 2px;
    }
    ```

- [x] Task 3: Fix `student-attempt-workspace.component.css` — focus states (AC4)
  - [x] 3.1 Fix `.answer-input:focus` → dùng `:focus-visible` và thêm proper outline:
    ```css
    /* Xóa: */
    .answer-input:focus {
      outline: none;
      border-color: #4299e1;
      box-shadow: 0 0 0 2px rgba(66, 153, 225, 0.2);
    }
    /* Thay bằng: */
    .answer-input:focus-visible {
      outline: 2px solid #2563eb;
      outline-offset: 1px;
      border-color: #4299e1;
    }
    ```
  - [x] 3.2 Thêm `:focus-visible` cho tất cả các buttons chưa có (back-button, primary-button, secondary-button, text-button):
    ```css
    .back-button:focus-visible,
    .primary-button:focus-visible,
    .secondary-button:focus-visible,
    .text-button:focus-visible {
      outline: 2px solid #2563eb;
      outline-offset: 2px;
    }
    ```

- [x] Task 4: Harmonize `mode-badge` colors — student-attempt và student-speaking (AC3, AC6)
  - [x] 4.1 Canonical mode badge palette:
    - `mode-homework`: `background: #dbeafe; color: #1e40af` (blue — "planned/structured")
    - `mode-live-exam`: `background: #fef3c7; color: #92400e` (amber — "urgent/live")
  - [x] 4.2 Update `student-attempt-workspace.component.css`:
    ```css
    /* Thay: */
    .mode-homework { background: #ebf8ff; color: #2b6cb0; }
    .mode-live-exam { background: #fff5f5; color: #c53030; }
    /* Thành: */
    .mode-homework { background: #dbeafe; color: #1e40af; }
    .mode-live-exam { background: #fef3c7; color: #92400e; }
    ```
  - [x] 4.3 Update `student-speaking-submission.component.css`:
    ```css
    /* Thay: */
    .mode-homework { background: #cce5ff; color: #004085; }
    .mode-live-exam { background: #fff3cd; color: #856404; }
    /* Thành: */
    .mode-homework { background: #dbeafe; color: #1e40af; }
    .mode-live-exam { background: #fef3c7; color: #92400e; }
    ```

- [x] Task 5: Harmonize `skill-badge` — student-assigned-tests (AC6)
  - [x] 5.1 `student-assigned-tests.component.css`: `.skill-badge` hiện là gray (`#f3f4f6 / #374151`), cần match library blue:
    ```css
    /* Thay: */
    .skill-badge { background: #f3f4f6; color: #374151; ... }
    /* Thành: */
    .skill-badge { background: #eff6ff; color: #1d4ed8; ... }
    ```
  - [x] 5.2 Verify `test-template-library.component.css` đã có `skill-badge` blue — không cần thay đổi ✓

- [x] Task 6: Add responsive behavior — `teacher-results.component.css` (AC5)
  - [x] 6.1 Thêm media query cho split panel trên tablet/narrow:
    ```css
    @media (max-width: 768px) {
      .workspace.has-detail {
        flex-direction: column;
      }
      .workspace.has-detail .list-panel {
        max-width: 100%;
      }
      .detail-panel {
        flex: none;
        width: 100%;
        min-width: 0;
        border-left: none;
        border-top: 1px solid #e5e7eb;
        max-height: none;
      }
    }
    ```

- [x] Task 7: Verify mode wording does NOT collapse (AC3)
  - [x] 7.1 Kiểm tra `teacher-results.component.html` filter options:
    - `<option value="homework">Bài tập</option>` — acceptable (specific to homework)
    - `<option value="live-exam">Thi trực tiếp</option>` — correct
    - Đảm bảo không có option nào là generic "Bài thi" bao gồm cả hai loại.
    - Nếu hiện tại là "Bài tập" → acceptable; nếu là "Bài thi" → sửa thành "Homework" hoặc "Bài tập về nhà".
  - [x] 7.2 Đảm bảo `dashboard.models.ts` `RECENT_WORK_MODE_LABELS` dùng: `homework: 'Homework'`, `'live-exam': 'Thi trực tiếp'` — không thay đổi ✓

- [x] Task 8: Angular component tests (AC3, AC4)
  - [x] 8.1 Update hoặc tạo test trong `teacher-results.component.spec.ts` (nếu không tồn tại, tạo mới):
    - Test: status-badge `status-submitted` có background blue (KHÔNG amber)
    - Test: focus states visible trên filter bar selects
  - [x] 8.2 Update `teacher-speaking-grading.component.spec.ts` (nếu không tồn tại, tạo mới):
    - Test: `.status-submitted` badge color là blue, không phải amber
  - [x] 8.3 Chạy `npm test` trong `src/EnglishTestWeb.Client` — tất cả tests pass

- [x] Task 9: Quality gate
  - [x] 9.1 `dotnet build` — build thành công (CSS-only changes, no backend impact)
  - [x] 9.2 `npm test` — tất cả Angular tests pass

## Dev Notes

### Bối cảnh và mục đích

Story 6.4 là **CSS/HTML-only hardening pass** — không có backend thay đổi, không có Angular service mới, không có route mới. Mục tiêu:
1. Sửa inconsistencies về badge colors giữa các components
2. Sửa broken/missing focus states (WCAG 2.1 AA requirement)
3. Harmonize mode/skill badge colors thành canonical palette
4. Add responsive breakpoint cho split panel trong teacher-results

### Canonical Status Badge Palette (PHẢI dùng nhất quán)

| Semantic State | Background | Color | Applied When |
|---|---|---|---|
| `draft` | `#f3f4f6` | `#4b5563` | Template draft, submission not started |
| `ready` | `#dcfce7` | `#166534` | Template ready to assign |
| `submitted` | `#dbeafe` | `#1e40af` | Student has submitted |
| `auto-graded` | `#d1fae5` | `#065f46` | Auto-graded (Reading/Listening) |
| `graded` | `#d1fae5` | `#065f46` | Manually graded (Speaking) |
| `needs-grading` | `#fef3c7` | `#92400e` | Speaking submitted, awaiting grade |
| `not-open` / `scheduled` | `#fef3c7` | `#92400e` | Not yet open |
| `open` / `available` | `#dcfce7` | `#166534` | Currently open/available |
| `closed` / `archived` / `expired` | `#f3f4f6` | `#4b5563` | Ended/archived |

**WCAG AA đã verified**: tất cả text/background combinations trên đạt contrast ratio ≥ 4.5:1.

### Canonical Mode Badge Palette

| Mode | Background | Color |
|---|---|---|
| `mode-homework` | `#dbeafe` | `#1e40af` (blue — planned) |
| `mode-live-exam` | `#fef3c7` | `#92400e` (amber — live/urgent) |

### Canonical Skill Badge Palette

| Skill | Background | Color |
|---|---|---|
| Generic / all | `#eff6ff` | `#1d4ed8` (blue) |
| Reading-specific | `#f0fff4` | `#276749` (green) |
| Listening-specific | `#faf5ff` | `#6b46c1` (purple) |
| Speaking-specific | `#d4edda` | `#155724` (green) — student-speaking dùng |

**Note:** `student-attempt-workspace` đã có `.skill-reading` và `.skill-listening` với skill-specific colors — giữ nguyên, chỉ cần update general `.skill-badge` trong student-assigned-tests.

### Focus State Pattern (PHẢI dùng `:focus-visible`, KHÔNG `:focus`)

```css
/* Đúng: */
.btn:focus-visible {
  outline: 2px solid #2563eb;
  outline-offset: 2px;
}

/* Sai (kills keyboard focus for click users): */
.input:focus {
  outline: none; /* ← NEVER do this without :focus-visible fallback */
}
```

**QUAN TRỌNG:** `outline: none` trong `:focus` (không phải `:focus-visible`) removes focus indicator cho cả keyboard và mouse users. `student-attempt-workspace.component.css` hiện có bug này — phải fix.

### Files cần thay đổi

| File | Type | Changes |
|---|---|---|
| `teacher-results.component.css` | UPDATE | Fix `9999py` typo, add `:focus-visible`, add responsive |
| `teacher-speaking-grading.component.css` | UPDATE | Fix status badge colors (submitted=blue, graded=green, draft=gray) |
| `student-attempt-workspace.component.css` | UPDATE | Fix `answer-input:focus` → `:focus-visible`, add button focus |
| `student-speaking-submission.component.css` | UPDATE | Harmonize mode-badge colors |
| `student-assigned-tests.component.css` | UPDATE | Harmonize skill-badge to blue |

**Backend:** KHÔNG thay đổi gì.

**Frontend:** KHÔNG tạo file mới (trừ spec files nếu chưa có).

### Patterns phải follow từ story trước

**Test pattern:** Angular component tests chỉ test CSS class application, KHÔNG test computed colors (không access `getComputedStyle` trong unit test — chỉ verify class name applied).

**Từ Story 6.1–6.3:** Tất cả component dùng Angular signals pattern. Story 6.4 KHÔNG thay đổi TypeScript logic — chỉ CSS/HTML attributes.

### Bẫy cần tránh

1. **Đừng thay đổi `.focus` → `.focus-visible` trong Angular Material** — app này không dùng Angular Material, không liên quan.
2. **Không dùng `!important`** trong CSS overrides — sửa specificity thay vì override.
3. **`teacher-speaking-grading.component.css` dùng px (không rem)** cho padding — giữ style hiện tại khi sửa colors, chỉ thay hex values.
4. **Không break `student-attempt-workspace` autosave status** — chỉ sửa `.answer-input:focus-visible`, không đụng `.autosave-status`.
5. **`test-template-library.component.css` dùng `[data-status='draft']` attribute selector** — khác với các component khác dùng class selector. KHÔNG thay đổi library sang class selector — quá nhiều regression risk. Chỉ harmonize các component khác.
6. **Mode filter trong teacher-results:** `<option value="homework">Bài tập</option>` là acceptable (specific term). Đừng đổi thành "Bài thi" (generic). Nếu muốn thay đổi → "Homework" là canonical từ spec nhưng "Bài tập" cũng OK — đừng waste time nếu không yêu cầu cụ thể.

### Không cần làm trong story này

- Thêm global CSS variables (`--color-primary: ...`) — out of scope cho MVP
- Thay đổi component structure hoặc HTML hierarchy
- Thêm animations hoặc transitions
- Tạo shared Badge component — CSS-only changes đủ cho MVP
- Dark mode — out of scope
- Test e2e (Playwright) — đó là story 6.6

### References

- [Story 6.1] `teacher-results.component.css` — existing split panel pattern
- [Story 6.2] `teacher-speaking-grading.component.css` — status badges
- [Story 6.3] `teacher-dashboard.component.css` — canonical card/summary styles
- `src/EnglishTestWeb.Client/src/app/features/test-template-library/test-template-library.component.css` — canonical badge + focus pattern
- `src/EnglishTestWeb.Client/src/app/shared/layouts/teacher-shell/teacher-shell.component.css` — nav focus pattern
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.css` — focus bug to fix

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

Không có issue nào — CSS-only changes, tất cả straight-forward.

### Completion Notes List

- Task 1.1: File đã có `9999px` đúng — không cần thay. Focus-visible block đã được thêm cho filter-bar, pagination, close-btn, save-btn, next-btn, empty-state button.
- Task 2: badge colors trong teacher-speaking-grading.component.css đã harmonize: submitted=blue (#dbeafe/#1e40af), graded=green (#d1fae5/#065f46), draft=gray (#f3f4f6/#4b5563). Thêm focus-visible cho primary-button.
- Task 3: `answer-input:focus` (có `outline: none`) đã thay bằng `:focus-visible` với proper outline. Thêm focus-visible block cho 4 buttons.
- Task 4: mode-badge harmonized trong cả student-attempt-workspace và student-speaking-submission: homework=blue, live-exam=amber.
- Task 5: skill-badge trong student-assigned-tests đổi từ gray sang blue (#eff6ff/#1d4ed8).
- Task 6: Thêm responsive @media (max-width: 768px) cho split panel trong teacher-results.
- Task 7: Verified — "Bài tập" và "Thi trực tiếp" đã đúng, không có generic "Bài thi".
- Task 8: Thêm 2 tests vào teacher-results.spec.ts (badge class + filter bar elements). Thêm 1 test vào teacher-speaking-grading.spec.ts (badge class). 197/197 tests pass.
- Task 9: dotnet build OK (0 warnings, 0 errors). npm test 197 passed.

### File List

**Frontend (UPDATED):**
- `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.css`
- `src/EnglishTestWeb.Client/src/app/features/teacher-speaking-grading/teacher-speaking-grading.component.css`
- `src/EnglishTestWeb.Client/src/app/features/student-attempt-workspace/student-attempt-workspace.component.css`
- `src/EnglishTestWeb.Client/src/app/features/student-speaking-submission/student-speaking-submission.component.css`
- `src/EnglishTestWeb.Client/src/app/features/student-assigned-tests/student-assigned-tests.component.css`

**Frontend (NEW — if not exists):**
- `src/EnglishTestWeb.Client/src/app/features/teacher-results/teacher-results.component.spec.ts`
- `src/EnglishTestWeb.Client/src/app/features/teacher-speaking-grading/teacher-speaking-grading.component.spec.ts`

### Review Findings (AI) — Round 1 (2026-06-13)

#### Patch Items
- [x] [Review][Patch] student-speaking-submission.component.css — Add `:focus-visible` for `.primary-button`, `.secondary-button`, `.text-button`, `.back-button` (AC4) [student-speaking-submission.component.css]
- [x] [Review][Patch] teacher-results.component.css — `.status-draft` uses `#6b7280` instead of canonical `#4b5563` (AC3, AC6) [teacher-results.component.css:162]
- [x] [Review][Patch] teacher-speaking-grading.component.css — `.score-input` and `.feedback-input` missing `:focus-visible` (AC4) [teacher-speaking-grading.component.css:142-157]
- [x] [Review][Patch] teacher-results.component.css — Responsive `.detail-panel` sets `max-height: none` without `overflow-y: auto` (AC5) [teacher-results.component.css:420-427]

#### Defer Items
- [x] [Review][Defer] RESULT_STATUS_LABELS missing `needs-grading` entry — deferred, pre-existing
- [x] [Review][Defer] Focus color `#2563eb` hardcoded across files — deferred, pre-existing pattern
- [x] [Review][Defer] `.skill-badge` in student-attempt-workspace uncolored (no background/color) — deferred, pre-existing
- [x] [Review][Defer] `.skill-speaking` missing in student-attempt-workspace — deferred, pre-existing
- [x] [Review][Defer] student-speaking-submission `.skill-badge` uncolored for non-speaking skills — deferred, pre-existing

### Review Findings (AI) — Round 2 (2026-06-13)

#### Patch Items
- [x] [Review][Patch] teacher-results.component.css — `.grade-form input[type='number']` và `.grade-form textarea` thiếu `:focus-visible` (AC4) [teacher-results.component.css]
- [x] [Review][Patch] student-attempt-workspace.component.css — `.submit-button` thiếu `:focus-visible` (AC4) [student-attempt-workspace.component.css]
- [x] [Review][Patch] student-assigned-tests.component.css — `.filter-select` thiếu `:focus-visible` (AC4) [student-assigned-tests.component.css]

#### Defer Items
- [x] [Review][Defer] teacher-results.component.html inline detail-panel status badge không có `data-testid` — deferred, pre-existing, test-surface gap only

### Change Log

- 2026-06-13: CSS hardening pass hoàn thành — fix focus states (WCAG AA), harmonize status/mode/skill badge colors, thêm responsive split panel. 3 tests mới. 197/197 pass.
