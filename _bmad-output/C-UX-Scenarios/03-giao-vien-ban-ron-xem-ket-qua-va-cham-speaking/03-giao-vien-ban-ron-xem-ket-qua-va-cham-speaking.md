---
design_intent: D
design_status: specified
---

# 03: Giáo viên bận rộn xem kết quả và chấm Speaking

**Project:** EnglishTestWeb
**Created:** 2026-06-08
**Method:** Whiteport Design Studio (WDS)

---

## Transaction (Q1)

**What this scenario covers:**
Giáo viên mở dashboard, xem kết quả theo lớp/bài/học sinh, rồi chấm Speaking bằng cách nghe file, nhập điểm và feedback trong cùng một màn hình.

---

## Business Goal (Q2)

**Goal:** Giáo viên xem được kết quả tập trung theo lớp, học sinh, bài test và lần nộp; Speaking submission, nghe file, nhập điểm và feedback được xử lý trong cùng một màn hình.
**Objective:** Dashboard kết quả tập trung và Speaking feedback tập trung trong MVP.

---

## User & Situation (Q3)

**Persona:** Giáo viên bận rộn, đã có sẵn đề PDF/audio (Primary)
**Situation:** Sau deadline hoặc khi học sinh đã nộp bài, giáo viên cần xem nhanh tình hình lớp và xử lý các bài Speaking còn chờ chấm.

---

## Driving Forces (Q4)

**Trigger:** Học sinh đã nộp bài và giáo viên cần xem kết quả hoặc chấm Speaking.

**Hope:** Xem kết quả lớp nhanh và chấm Speaking mà không phải tải file hoặc ghi feedback ở nơi khác.

**Worry:** Điểm, file nói và feedback bị rời rạc qua nhiều kênh, khiến việc tổng hợp kết quả mất thời gian.

---

## Device & Starting Point (Q5 + Q6)

**Device:** Laptop/PC trên website
**Entry:** Giáo viên mở EnglishTestWeb trên laptop/PC, đăng nhập vào Teacher Dashboard sau khi học sinh đã nộp bài. Từ navbar, giáo viên vào module "Kết quả" để xem bài cần xử lý.

---

## Best Outcome (Q7)

**User Success:**
Giáo viên xem được kết quả lớp, mở đúng submission Speaking, nghe file, nhập điểm và feedback trong một phiên làm việc.

**Business Success:**
MVP hoàn thiện vòng quản lý kết quả tập trung, giảm thao tác tổng hợp thủ công sau khi học sinh nộp bài.

---

## Shortest Path (Q8)

1. **Teacher Login / Account Access** — Giáo viên đăng nhập tài khoản trên laptop/PC.
2. **Teacher Dashboard** — Giáo viên thấy tổng quan lớp/bài nộp và chọn module "Kết quả" trên navbar.
3. **Results & Grading** — Giáo viên lọc theo lớp/bài/học sinh, mở submission Speaking, nghe file, nhập điểm và feedback. ✓

---

## Trigger Map Connections

**Persona:** Giáo viên bận rộn, đã có sẵn đề PDF/audio (Primary)

**Driving Forces Addressed:**
- ✅ **Want:** Muốn xem kết quả theo lớp/học sinh/bài test ở một nơi duy nhất.
- ❌ **Fear:** Sợ điểm số, bài nộp, file nói và feedback bị rời rạc qua nhiều kênh.

**Business Goal:** Kết quả được lưu tập trung theo lớp, học sinh, bài test và lần nộp; Speaking được nghe, chấm điểm và feedback trong cùng một màn hình.

---

## Scenario Steps

Steps are outlined one at a time after scenario creation. The first step is processed automatically.

| Step | Folder | Purpose | Exit Action |
|------|--------|---------|-------------|
| 03.1 | `3.1-teacher-login-account-access/` | Giáo viên đăng nhập để vào khu vực quản lý sau khi học sinh đã nộp bài. | Đăng nhập thành công để vào Teacher Dashboard. |
| 03.2 | `3.2-teacher-dashboard/` | Giáo viên thấy tổng quan lớp/bài nộp và chuyển sang module "Kết quả". | Chọn module "Kết quả" trên navbar. |
| 03.3 | `3.3-results-grading/` | Giáo viên lọc kết quả, mở submission Speaking, nghe file, nhập điểm và feedback. | Lưu điểm và feedback Speaking thành công. ✓ |

**First step** (03.1) includes full entry context (Q3 + Q4 + Q5 + Q6).
**On-step interactions** (that don't leave the step) are documented as storyboard items within each page spec.
