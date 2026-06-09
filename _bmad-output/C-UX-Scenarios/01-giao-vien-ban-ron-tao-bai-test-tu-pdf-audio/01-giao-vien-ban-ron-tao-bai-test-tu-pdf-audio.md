---
design_intent: D
design_status: specified
corrected_at: 2026-06-08
---

# 01: Giáo viên bận rộn tạo đề gốc và dùng cho homework/thi trực tiếp

**Project:** EnglishTestWeb  
**Created:** 2026-06-08  
**Method:** Whiteport Design Studio (WDS)

---

## Transaction (Q1)

**What this scenario covers:**  
Giáo viên tạo một đề gốc/template từ PDF/audio có sẵn trong Thư viện đề, khai báo answer key/scoring, đánh dấu đề sẵn sàng, rồi chọn dùng đề đó để giao Homework hoặc tạo phiên Thi trực tiếp.

---

## Business Goal (Q2)

**Goal:** Giáo viên tạo đề gốc nhanh từ tài liệu có sẵn, sau đó dùng lại đề đó cho homework có hạn nộp hoặc bài thi trực tiếp trên lớp.  
**Objective:** Tạo Reading/Listening template từ PDF/audio trong dưới 10 phút; hệ thống giữ answer key để tự chấm 100% bài Reading/Listening khi học sinh nộp.

---

## User & Situation (Q3)

**Persona:** Giáo viên bận rộn, đã có sẵn đề PDF/audio (Primary)  
**Situation:** Giáo viên cần chuẩn bị một đề Reading/Listening/Speaking để dùng linh hoạt: giao về nhà hoặc mở làm trực tiếp trong lớp.

---

## Driving Forces (Q4)

**Trigger:** Giáo viên có sẵn file đề và cần đưa lên hệ thống nhanh mà không nhập lại toàn bộ nội dung.  
**Hope:** Tạo một lần, dùng được nhiều lần cho nhiều lớp/ngữ cảnh.  
**Worry:** Nhập answer key hoặc chọn sai mode giao bài khiến học sinh không làm được bài đúng thời điểm.

---

## Device & Starting Point (Q5 + Q6)

**Device:** Laptop/PC trên website  
**Entry:** Giáo viên mở EnglishTestWeb, đăng nhập vào Teacher Dashboard, chọn module "Thư viện đề" trên navbar để tạo đề gốc mới hoặc dùng lại đề có sẵn.

---

## Best Outcome (Q7)

**User Success:**  
Giáo viên tạo được đề gốc có PDF/audio, answer key hợp lệ và trạng thái sẵn sàng trong dưới 10 phút; sau đó có thể chọn "Giao homework" hoặc "Tạo phiên thi trực tiếp".

**Business Success:**  
MVP chứng minh workflow tạo đề và tự chấm Reading/Listening đủ nhanh, đồng thời phân biệt rõ nguồn đề với các lần giao/làm bài thực tế.

---

## Shortest Path (Q8)

1. **Teacher Login / Account Access** - Giáo viên đăng nhập tài khoản trên laptop/PC.
2. **Teacher Dashboard** - Giáo viên thấy tổng quan sau đăng nhập và chọn module "Thư viện đề" trên navbar.
3. **Question/Test Library** - Giáo viên xem kho đề gốc và bắt đầu tạo đề Reading mới.
4. **Create Template: Setup** - Giáo viên nhập tên đề, chọn kỹ năng, thêm mô tả/tag nếu cần.
5. **Create Template: Upload Materials** - Giáo viên upload PDF/audio/cue card.
6. **Create Template: Answer Key & Scoring** - Giáo viên nhập số câu, answer key và điểm.
7. **Review Template & Next Action** - Giáo viên kiểm tra đề, đánh dấu sẵn sàng, rồi chọn giao Homework hoặc tạo Thi trực tiếp.

---

## Trigger Map Connections

**Persona:** Giáo viên bận rộn, đã có sẵn đề PDF/audio (Primary)

**Driving Forces Addressed:**
- **Want:** Muốn tạo bài test nhanh từ tài liệu đã có, không phải nhập lại nội dung đề.
- **Fear:** Sợ mất thời gian vì phải thao tác nhiều bước hoặc cấu hình sai khiến học sinh không làm được bài.

**Business Goal:** Tạo đề gốc từ PDF/audio trong dưới 10 phút, có answer key để tự chấm khi học sinh nộp bài; Homework và Thi trực tiếp là hai lần sử dụng riêng của đề gốc.

---

## Scenario Steps

Steps are outlined one at a time after scenario creation. The first step is processed automatically.

| Step | Folder | Purpose | Exit Action |
|------|--------|---------|-------------|
| 01.1 | `1.1-teacher-login-account-access/` | Giáo viên truy cập website và đăng nhập đúng tài khoản giáo viên. | Đăng nhập thành công để vào Teacher Dashboard. |
| 01.2 | `1.2-teacher-dashboard/` | Giáo viên thấy tổng quan và chọn module "Thư viện đề" từ navbar. | Chọn module "Thư viện đề". |
| 01.3 | `1.3-test-list-test-library/` | Giáo viên xem kho đề gốc và bắt đầu tạo đề Reading mới. | Chọn tạo đề mới. |
| 01.4 | `1.4-create-test-setup/` | Giáo viên khai báo thông tin nền tảng của đề gốc. | Tiếp tục sang upload tài liệu. |
| 01.5 | `1.5-create-test-upload-materials/` | Giáo viên upload PDF/audio/cue card cho đề. | Upload tài liệu hợp lệ và tiếp tục sang answer key. |
| 01.6 | `1.6-create-test-answer-key-scoring/` | Giáo viên nhập số câu, answer key và điểm. | Hoàn tất answer key hợp lệ và chuyển sang review. |
| 01.7 | `1.7-create-test-review-publish/` | Giáo viên kiểm tra đề gốc và chọn hành động tiếp theo. | Đề sẵn sàng; có thể giao Homework hoặc tạo Thi trực tiếp. |

**First step** (01.1) includes full entry context (Q3 + Q4 + Q5 + Q6).  
**On-step interactions** (that don't leave the step) are documented as storyboard items within each page spec.
