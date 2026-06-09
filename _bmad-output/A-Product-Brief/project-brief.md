# Project Brief: EnglishTestWeb

> Brief rút gọn - Ngữ cảnh cốt lõi cho thiết kế và phát triển

**Ngày tạo:** 2026-06-08  
**Tác giả:** Đức  
**Loại brief:** Rút gọn  
**Ngôn ngữ tài liệu:** Tiếng Việt

---

## Phạm Vi Dự Án

EnglishTestWeb là website giúp giáo viên giao bài kiểm tra tiếng Anh online cho học sinh. Học sinh làm bài trên web, hệ thống lưu bài làm và điểm số để giáo viên xem lại.

Phiên bản đầu tập trung vào 3 kỹ năng:

- **Listening**: giáo viên upload file PDF đề bài và file audio. Học sinh xem PDF theo trang, nghe audio trên web, điền đáp án vào form riêng và nộp bài. Hệ thống tự chấm dựa trên bảng đáp án giáo viên khai báo.
- **Reading**: giáo viên upload file PDF đề bài. Học sinh xem PDF theo trang, điền đáp án vào form riêng và nộp bài. Hệ thống tự chấm dựa trên bảng đáp án giáo viên khai báo.
- **Speaking**: giáo viên tạo đề nói/cue card/hướng dẫn. Học sinh upload file nói. Giáo viên nghe trực tiếp trên web, nhập điểm và feedback thủ công.

Hướng tạo đề đã chốt cho MVP: **upload PDF và hiển thị theo trang, đáp án được nhập ở khu vực riêng**. Hệ thống không cần parse nội dung PDF thành từng câu hỏi trong phiên bản đầu.

---

## Vấn Đề / Cơ Hội

Giáo viên cần giảm thời gian chấm bài và quản lý kết quả học sinh dễ hơn. Listening và Reading có thể tự động chấm nếu giáo viên khai báo đáp án đúng; điều này giúp giảm việc chấm thủ công và tổng hợp điểm bằng tay.

Speaking vẫn cần giáo viên đánh giá chất lượng nói, nhưng hệ thống sẽ gom các bước nghe file, chấm điểm, feedback và lưu lịch sử vào cùng một nơi, tránh tình trạng file nói và nhận xét bị rời rạc qua nhiều kênh.

Cơ hội của sản phẩm là tạo một quy trình kiểm tra gọn cho giáo viên:

1. Tạo hoặc lưu đề gốc trong **Thư viện đề**.
2. Upload PDF/audio nếu cần.
3. Khai báo đáp án và thang điểm cho đề gốc.
4. Sử dụng đề gốc theo một trong hai dạng:
   - **Homework**: giao cho học sinh làm ở nhà, có hạn nộp.
   - **Thi trực tiếp**: mở một phiên thi trên lớp, học sinh làm trong thời gian/phiên được giáo viên kiểm soát.
5. Học sinh làm bài và nộp theo đúng dạng bài.
6. Hệ thống chấm Listening/Reading, lưu kết quả.
7. Giáo viên xem dashboard và feedback Speaking.

---

## Mục Tiêu Thiết Kế

- Giáo viên tạo đề gốc nhanh, đặc biệt khi đã có sẵn đề PDF.
- Giáo viên có **Thư viện đề** để lưu đề gốc, sau đó dùng lại đề đó cho homework hoặc phiên thi trực tiếp.
- Hệ thống phân biệt rõ **Homework** và **Thi trực tiếp** để học sinh hiểu bối cảnh làm bài.
- Học sinh vào đúng lớp bằng **mã lớp**, sau đó đăng nhập tài khoản học sinh.
- Quyền truy cập bài test được phân theo lớp/mã lớp.
- Học sinh có trải nghiệm làm bài rõ ràng: xem PDF, nghe audio nếu có, điền đáp án, nộp bài.
- Giáo viên xem kết quả theo lớp, học sinh, bài test và lần nộp.
- Listening và Reading được tự chấm bằng bảng đáp án.
- Speaking có player nghe file nói, ô nhập điểm và feedback trực tiếp trên web.
- Kết quả được lưu tập trung để giáo viên không phải tổng hợp thủ công.
- Giao diện web ưu tiên sử dụng trên desktop/laptop; chưa cần mobile app trong phiên bản đầu.

---

## Tính Năng MVP Cốt Lõi

### Tài Khoản Và Truy Cập Theo Lớp

- Tài khoản giáo viên.
- Tài khoản học sinh.
- Lớp học có mã lớp.
- Học sinh vào theo luồng: **Nhập mã lớp -> đăng nhập tài khoản -> thấy homework/bài thi trực tiếp của lớp**.
- Bài làm chỉ hiển thị với học sinh thuộc lớp được giao và đúng trạng thái cho phép.
- Homework hiển thị khi học sinh thuộc lớp được giao và còn trong phạm vi hạn nộp/trạng thái cho phép.
- Bài thi trực tiếp chỉ hiển thị hoặc chỉ cho bắt đầu khi phiên thi đang được mở cho lớp.

### Thư Viện Đề, Homework Và Thi Trực Tiếp

- Giáo viên tạo **đề gốc** trong Thư viện đề theo kỹ năng: Listening, Reading, Speaking.
- Đề gốc chứa tên đề, kỹ năng, PDF/audio/cue card nếu có, answer key và thang điểm.
- Đề gốc có thể lưu nháp, chỉnh sửa, dùng lại.
- Từ một đề gốc, giáo viên có thể:
  - **Giao homework** cho lớp/học sinh, có hạn nộp và thời gian làm bài nếu cần.
  - **Tạo phiên thi trực tiếp** trên lớp, có thời điểm mở/đóng hoặc trạng thái giáo viên kiểm soát.
- Homework và phiên thi trực tiếp là các lần sử dụng đề gốc, không phải bản thân đề gốc.

### Listening Và Reading Dựa Trên PDF

- Upload PDF đề bài.
- Hiển thị PDF theo trang trên web.
- Form điền đáp án riêng, có danh sách câu theo số thứ tự.
- Giáo viên khai báo số câu, đáp án đúng, điểm từng câu hoặc tổng điểm.
- Hỗ trợ các dạng bài hiện có bằng form đáp án chung:
  - Note Completion
  - Form Completion
  - True / False / Not Given
  - Yes / No / Not Given
  - Matching Headings
  - Multiple Choice
- Hệ thống tự chấm khi học sinh nộp bài.

### Audio Cho Listening

- Upload file audio cho bài Listening.
- Học sinh nghe audio trên web trong lúc xem PDF và điền đáp án.
- Giáo viên có thể cấu hình audio được nghe tự do hay giới hạn theo bài test nếu cần ở phiên bản sau.

### Nộp Bài Và Feedback Speaking

- Giáo viên tạo đề Speaking bằng text/PDF nếu cần.
- Học sinh upload file nói.
- Giáo viên nghe file trực tiếp trên web.
- Giáo viên nhập điểm và feedback.
- Có thể mở rộng sau này thành rubric chấm điểm theo Fluency, Vocabulary, Grammar, Pronunciation.

### Quản Lý Kết Quả

- Lưu bài làm, đáp án học sinh, điểm, thời gian nộp.
- Dashboard cho giáo viên xem kết quả theo lớp, học sinh và bài test.
- Giáo viên xem chi tiết bài làm của từng học sinh.
- Xuất kết quả ra Excel/PDF là tính năng nên có sau MVP nếu cần.

---

## Ràng Buộc

- Ưu tiên làm **web trước**.
- Chưa cần mobile app.
- Có deadline nhưng chưa có ngày cụ thể.
- Hình thức tạo đề gốc MVP chốt theo hướng **upload PDF + form đáp án riêng**, không parse PDF thành câu hỏi tự động.
- MVP cần tách nghiệp vụ **đề gốc** khỏi **homework assignment** và **live exam session**.
- Speaking không tự chấm trong phiên bản đầu; giáo viên chấm và feedback thủ công.
- Các file mẫu hiện có nằm trong `docs/`:
  - `Listening_temp.pdf`: Listening Note/Form Completion.
  - `READING_TEMP.pdf`: Reading worksheet gồm Note Completion, True/False/Not Given, Matching Headings, Multiple Choice, Yes/No/Not Given.

---

## Ngoài Phạm Vi MVP

- Gợi ý bài luyện tập dựa trên lỗi sai của học sinh.
- Mobile app riêng.
- Tự động đọc/parse PDF để tách passage, câu hỏi và đáp án.
- Tự chấm Speaking bằng AI.
- Phân tích lỗi chi tiết theo từng dạng câu hỏi nếu giáo viên chưa khai báo metadata.

---

## Bước Tiếp Theo Được Đề Xuất

- Tạo UX flow cho 3 luồng chính: giáo viên tạo đề gốc và dùng cho homework/thi trực tiếp, học sinh làm homework/live exam, giáo viên xem/chấm kết quả.
- Định nghĩa data model cho lớp, tài khoản, đề gốc, homework assignment, live exam session, PDF, audio, answer key, submission và feedback.
- Chốt MVP backlog theo thứ tự:
  1. Đăng nhập và mã lớp.
  2. Tạo lớp và tài khoản học sinh.
  3. Tạo Thư viện đề và đề gốc PDF cho Reading.
  4. Giao homework và tạo phiên thi trực tiếp từ đề gốc.
  5. Thêm Listening audio.
  6. Tự chấm và lưu kết quả.
  7. Speaking upload và feedback.
  8. Dashboard kết quả giáo viên.

---

_Generated by Whiteport Design Studio_
