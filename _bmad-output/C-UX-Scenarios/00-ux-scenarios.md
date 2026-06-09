# UX Scenarios: EnglishTestWeb

> Scenario outlines connecting Trigger Map personas to concrete user journeys

**Created:** 2026-06-08
**Author:** Đức with Codex
**Method:** Whiteport Design Studio (WDS)

---

## Scenario Summary

| ID | Scenario | Persona | Pages | Priority | Status |
|----|----------|---------|-------|----------|--------|
| 01 | Giáo viên bận rộn tạo đề gốc và dùng cho homework/thi trực tiếp | Giáo viên bận rộn, đã có sẵn đề PDF/audio | 7 | ⭐ P1 | ✅ Outlined |
| 02 | Học sinh làm homework hoặc bài thi trực tiếp trong đúng lớp | Học sinh cần làm bài đúng lớp, ít bị rối thao tác | 5 | ⭐ P1 | ✅ Outlined |
| 03 | Giáo viên bận rộn xem kết quả và chấm Speaking | Giáo viên bận rộn, đã có sẵn đề PDF/audio | 3 | ⭐ P1 | ✅ Outlined |

---

## Scenarios

### [01: Giáo viên bận rộn tạo đề gốc và dùng cho homework/thi trực tiếp](01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio/01-giao-vien-ban-ron-tao-bai-test-tu-pdf-audio.md)
**Persona:** Giáo viên bận rộn, đã có sẵn đề PDF/audio — muốn tạo bài nhanh từ tài liệu có sẵn, không phải nhập lại đề
**Pages:** Teacher Login / Account Access, Teacher Dashboard, Question/Test Library, Create Template: Setup, Create Template: Upload Materials, Create Template: Answer Key & Scoring, Review Template & Next Action
**User Value:** Giáo viên tạo được đề gốc Reading có PDF và answer key hợp lệ trong dưới 10 phút, sau đó dùng đề để giao homework hoặc tạo phiên thi trực tiếp.
**Business Value:** MVP chứng minh workflow tạo đề gốc và tái sử dụng đề cho hai dạng bài tập/thi đủ rõ để giảm thao tác thủ công.

---

### [02: Học sinh làm homework hoặc bài thi trực tiếp trong đúng lớp](02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop/02-hoc-sinh-lam-bai-duoc-giao-trong-dung-lop.md)
**Persona:** Học sinh cần làm bài đúng lớp, ít bị rối thao tác — muốn thấy đúng bài, không mất đáp án, biết chắc đã nộp thành công
**Pages:** Student Class Code Entry, Student Login / Account Access, Student Assigned Tests, Student Exam Taking: Reading/Listening, Student Speaking Submission
**User Value:** Học sinh phân biệt được bài về nhà và bài thi trực tiếp, hoàn thành đúng bài, answers/file Speaking được lưu, và thấy xác nhận nộp rõ ràng.
**Business Value:** Submission có cấu trúc đúng để hệ thống tự chấm Reading/Listening và lưu kết quả tập trung cho giáo viên.

---

### [03: Giáo viên bận rộn xem kết quả và chấm Speaking](03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking/03-giao-vien-ban-ron-xem-ket-qua-va-cham-speaking.md)
**Persona:** Giáo viên bận rộn, đã có sẵn đề PDF/audio — muốn kết quả, file nói, điểm và feedback tập trung một nơi
**Pages:** Teacher Login / Account Access, Teacher Dashboard, Results & Grading
**User Value:** Giáo viên xem được kết quả lớp, mở đúng submission Speaking, nghe file, nhập điểm và feedback trong một phiên làm việc.
**Business Value:** MVP hoàn thiện vòng quản lý kết quả tập trung, giảm thao tác tổng hợp thủ công sau khi học sinh nộp bài.

---

## Page Coverage Matrix

| Page | Scenario | Purpose in Flow |
|------|----------|----------------|
| Teacher Login / Account Access | 01 | Giáo viên đăng nhập để bắt đầu tạo bài Reading từ PDF. |
| Teacher Dashboard | 01 | Giáo viên thấy tổng quan và chọn module "Thư viện đề" trên navbar. |
| Question/Test Library | 01 | Giáo viên xem kho đề gốc và bắt đầu tạo đề Reading mới. |
| Create Template: Setup | 01 | Giáo viên khai báo tên đề gốc và kỹ năng Reading. |
| Create Template: Upload Materials | 01 | Giáo viên upload PDF đề Reading cho đề gốc. |
| Create Template: Answer Key & Scoring | 01 | Giáo viên nhập số câu, answer key và điểm cho đề gốc. |
| Review Template & Next Action | 01 | Giáo viên kiểm tra đề gốc và chọn dùng để giao homework hoặc tạo phiên thi trực tiếp. |
| Student Class Code Entry | 02 | Học sinh nhập mã lớp để vào đúng không gian lớp. |
| Student Login / Account Access | 02 | Học sinh đăng nhập đúng tài khoản học sinh. |
| Student Assigned Tests | 02 | Học sinh thấy danh sách homework và bài thi trực tiếp trong lớp. |
| Student Exam Taking: Reading/Listening | 02 | Học sinh làm homework hoặc bài thi trực tiếp với PDF/audio, answer form, autosave và submit. |
| Student Speaking Submission | 02 | Học sinh upload file Speaking và nộp với xác nhận rõ ràng. |
| Teacher Login / Account Access | 03 | Giáo viên đăng nhập để xem kết quả sau khi học sinh nộp bài. |
| Teacher Dashboard | 03 | Giáo viên thấy tổng quan và chọn module "Kết quả" trên navbar. |
| Results & Grading | 03 | Giáo viên lọc kết quả, nghe Speaking, nhập điểm và feedback. |

**Coverage:** 15/15 pages assigned to scenarios

---

## Next Phase

These scenario outlines feed into **Phase 4: UX Design** where each page gets:
- Detailed page specifications
- Wireframe sketches
- Component definitions
- Interaction details

---

_Generated with Whiteport Design Studio framework_
