# Persona: Giáo viên bận rộn, đã có sẵn đề PDF/audio

**Priority:** 1  
**Role in MVP:** Primary user  
**Impact:** High  
**Feasibility:** High

---

## Who They Are

Giáo viên tiếng Anh đang có sẵn đề kiểm tra ở dạng PDF, đôi khi kèm audio. Họ cần giao bài cho lớp, thu bài, chấm điểm, xem kết quả và xử lý feedback Speaking mà không muốn nhập lại toàn bộ đề.

## Psychological Profile

Họ đề cao **tốc độ, độ tin cậy và sự quen thuộc**. PDF/audio là workflow hiện tại, nên sản phẩm phải tôn trọng cách họ đã chuẩn bị đề. Nếu hệ thống bắt họ chuyển đổi đề sang cấu trúc phức tạp hoặc thao tác quá nhiều, họ sẽ so sánh ngay với cách làm thủ công.

Họ không chỉ cần "tính năng tạo test"; họ cần cảm giác rằng hệ thống đang **giảm việc**, không tạo thêm việc. Điểm tự chấm và dashboard chỉ có giá trị khi việc thiết lập answer key đủ rõ và kết quả đủ dễ xem lại.

## Internal State

Khi nghĩ đến kiểm tra và chấm bài, họ thường ở trạng thái **bận, áp lực thời gian, muốn chắc chắn**. Họ muốn giảm việc lặp lại nhưng vẫn sợ mất kiểm soát nếu hệ thống làm sai hoặc lưu trữ rời rạc.

## Usage Context

- **Access:** đăng nhập trên desktop/laptop khi chuẩn bị bài hoặc xem kết quả.
- **Emotional state:** muốn nhanh, rõ, đáng tin.
- **Behavior pattern:** upload PDF/audio, cấu hình test, nhập answer key, gán lớp, publish, xem dashboard.
- **Decision criteria:** ít bước, không nhập lại đề, answer key dễ kiểm tra, kết quả dễ xem.
- **Success outcome:** tạo được bài Reading/Listening dưới 10 phút và không phải tổng hợp điểm thủ công.

## Driving Forces

### Positive

1. Muốn tạo bài test nhanh từ tài liệu đã có, không phải nhập lại nội dung đề.
2. Muốn hệ thống tự chấm Listening/Reading để giảm thời gian chấm thủ công.
3. Muốn xem kết quả theo lớp/học sinh/bài test ở một nơi duy nhất.
4. Muốn chấm Speaking thuận tiện: nghe file, nhập điểm, ghi feedback ngay trên web.

### Negative

1. Sợ mất thời gian vì phải thao tác quá nhiều bước khi tạo bài.
2. Sợ hệ thống bắt nhập lại đề hoặc parse PDF sai, làm chậm hơn cách làm cũ.
3. Sợ điểm số, bài nộp, file nói và feedback bị rời rạc qua nhiều kênh.
4. Sợ học sinh không vào đúng bài/lớp, dẫn đến phải hỗ trợ thủ công nhiều.

## Relationship to Strategic Objectives

- **Objective 1:** trực tiếp quyết định test creation có đạt mốc dưới 10 phút hay không.
- **Objective 2:** khai báo answer key để hệ thống tự chấm.
- **Objective 3:** dùng dashboard để xem kết quả tập trung.
- **Objective 5:** xử lý Speaking trong cùng một màn hình.

## Design Notes

- Ưu tiên workflow ngắn, có draft/publish rõ.
- PDF/audio upload phải là path chính, không phải fallback.
- Answer key editor cần dễ nhập nhanh và review lại.
- Dashboard cần phục vụ scanning trước, analytics nâng cao sau.

