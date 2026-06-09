---
design_intent: D
design_status: specified
---

# 02: Học sinh làm bài được giao trong đúng lớp

**Project:** EnglishTestWeb
**Created:** 2026-06-08
**Method:** Whiteport Design Studio (WDS)

---

## Transaction (Q1)

**What this scenario covers:**
Học sinh nhập mã lớp, đăng nhập, mở đúng bài được giao, làm bài Reading/Listening hoặc upload Speaking và nộp với xác nhận rõ ràng.

---

## Business Goal (Q2)

**Goal:** Học sinh có thể vào đúng lớp bằng mã lớp, mở bài được giao, làm bài và nộp mà không cần giáo viên hướng dẫn từng bước.
**Objective:** Học sinh vào đúng lớp, thấy đúng bài test, làm bài và nộp rõ ràng để giảm hỗ trợ thủ công cho giáo viên.

---

## User & Situation (Q3)

**Persona:** Học sinh cần làm bài đúng lớp, ít bị rối thao tác (Core participant)
**Situation:** Học sinh nhận mã lớp và yêu cầu làm bài từ giáo viên, mở website để vào đúng lớp và hoàn thành bài được giao.

---

## Driving Forces (Q4)

**Trigger:** Học sinh nhận mã lớp và yêu cầu làm bài từ giáo viên.

**Hope:** Vào đúng lớp, thấy đúng bài, làm và nộp bài không bị nhầm.

**Worry:** Nhập nhầm lớp, mất đáp án đang làm, hoặc không biết bài đã nộp thành công chưa.

---

## Device & Starting Point (Q5 + Q6)

**Device:** Laptop/PC trên website
**Entry:** Học sinh mở EnglishTestWeb sau khi nhận mã lớp từ giáo viên. Học sinh bắt đầu ở màn nhập mã lớp, sau đó đăng nhập tài khoản học sinh.

---

## Best Outcome (Q7)

**User Success:**
Học sinh hoàn thành đúng bài được giao, answers/file Speaking được lưu, và thấy xác nhận nộp rõ ràng.

**Business Success:**
Submission có cấu trúc đúng để hệ thống tự chấm Reading/Listening và lưu kết quả tập trung cho giáo viên.

---

## Shortest Path (Q8)

1. **Student Class Code Entry** — Học sinh nhập mã lớp để vào đúng không gian lớp.
2. **Student Login / Account Access** — Học sinh đăng nhập tài khoản học sinh.
3. **Student Assigned Tests** — Học sinh thấy danh sách bài được giao và mở bài cần làm.
4. **Student Exam Taking: Reading/Listening** — Học sinh xem PDF, nghe audio nếu có, nhập đáp án, theo dõi tiến độ và nộp bài.
5. **Student Speaking Submission** — Học sinh xem đề Speaking, upload file nói và nộp với xác nhận rõ ràng. ✓

---

## Trigger Map Connections

**Persona:** Học sinh cần làm bài đúng lớp, ít bị rối thao tác (Core participant)

**Driving Forces Addressed:**
- ✅ **Want:** Muốn vào đúng lớp và thấy đúng bài được giao.
- ❌ **Fear:** Sợ mất đáp án hoặc không biết bài đã nộp thành công chưa.

**Business Goal:** Học sinh tự vào lớp, làm bài và nộp bài rõ ràng để giảm hỗ trợ thủ công cho giáo viên và tạo dữ liệu submission đúng cấu trúc.

---

## Scenario Steps

Steps are outlined one at a time after scenario creation. The first step is processed automatically.

| Step | Folder | Purpose | Exit Action |
|------|--------|---------|-------------|
| 02.1 | `2.1-student-class-code-entry/` | Học sinh nhập mã lớp để vào đúng không gian lớp. | Mã lớp hợp lệ và tiếp tục sang đăng nhập học sinh. |
| 02.2 | `2.2-student-login-account-access/` | Học sinh đăng nhập đúng tài khoản học sinh. | Đăng nhập thành công để xem bài được giao. |
| 02.3 | `2.3-student-assigned-tests/` | Học sinh thấy danh sách bài được giao trong lớp. | Mở bài cần làm. |
| 02.4 | `2.4-student-exam-taking-reading-listening/` | Học sinh làm bài Reading/Listening với PDF/audio và answer form. | Nộp bài Reading/Listening thành công. |
| 02.5 | `2.5-student-speaking-submission/` | Học sinh upload file Speaking cho bài được giao. | Nộp file Speaking thành công. ✓ |

**First step** (02.1) includes full entry context (Q3 + Q4 + Q5 + Q6).
**On-step interactions** (that don't leave the step) are documented as storyboard items within each page spec.
