# Persona: Học sinh cần làm bài đúng lớp, ít bị rối thao tác

**Priority:** 2  
**Role in MVP:** Core participant  
**Impact:** High  
**Feasibility:** Medium

---

## Who They Are

Học sinh được giáo viên giao bài theo lớp. Họ cần nhập mã lớp, đăng nhập, thấy đúng bài được giao, làm bài Reading/Listening/Speaking và nộp đúng hạn.

## Psychological Profile

Họ cần **sự rõ ràng và cảm giác an toàn khi làm bài**. Với Reading/Listening, họ phải vừa theo dõi PDF/audio vừa nhập đáp án vào form riêng, nên layout và trạng thái bài làm phải giúp họ hiểu đang trả lời câu nào và còn thiếu gì.

Họ ít quan tâm đến cấu trúc quản trị phía sau. Điều quan trọng là không bị lạc lớp, không mất đáp án, không nộp nhầm, và có xác nhận rõ khi bài đã nộp.

## Internal State

Khi làm bài, họ có thể **căng thẳng, sợ sai thao tác, sợ mất bài**. Nếu giao diện không rõ, lỗi nhỏ như reload trang, mất mạng hoặc không thấy thông báo nộp bài có thể làm mất niềm tin.

## Usage Context

- **Access:** nhập mã lớp, đăng nhập, mở bài được giao.
- **Emotional state:** muốn chắc là mình đang ở đúng bài và nộp đúng.
- **Behavior pattern:** xem PDF theo trang, nghe audio nếu có, nhập đáp án, kiểm tra trạng thái, nộp bài.
- **Decision criteria:** rõ lớp/bài, rõ câu hỏi/đáp án, lưu được tiến trình, xác nhận nộp rõ.
- **Success outcome:** hoàn thành bài mà không cần giáo viên hướng dẫn từng bước.

## Driving Forces

### Positive

1. Muốn vào đúng lớp và thấy đúng bài được giao.
2. Muốn xem đề PDF, nghe audio và điền đáp án trong một luồng rõ ràng.
3. Muốn biết bài đã được nộp thành công.
4. Muốn không bị mất đáp án trong lúc làm bài.

### Negative

1. Sợ nhập nhầm lớp hoặc không thấy bài cần làm.
2. Sợ giao diện làm bài rối: PDF, audio và form đáp án không rõ liên hệ với nhau.
3. Sợ nộp lỗi hoặc không biết đã nộp chưa.
4. Sợ mất bài/đáp án do thao tác nhầm, lỗi mạng hoặc reload trang.

## Relationship to Strategic Objectives

- **Objective 2:** submission đúng cấu trúc giúp hệ thống tự chấm Listening/Reading.
- **Objective 3:** dữ liệu bài làm đầy đủ giúp dashboard có kết quả chính xác.
- **Objective 4:** trực tiếp quyết định học sinh có thể tự vào lớp, làm bài và nộp không cần hỗ trợ.

## Design Notes

- Class code entry and assigned-test list must be unambiguous.
- Exam screen should stabilize PDF/audio/form layout.
- Autosave or clear draft persistence is strategically important.
- Submission confirmation should be explicit and recoverable.

