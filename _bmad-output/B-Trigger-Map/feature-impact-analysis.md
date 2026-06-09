# Feature Impact Analysis: EnglishTestWeb

**Ngày tạo:** 2026-06-08  
**Method:** Frequency x Intensity x Product Fit, mỗi tiêu chí 1-5 điểm  
**Nguồn:** Trigger Map đã xác nhận

---

## Scoring Scale

- **Frequency:** lực này xuất hiện thường xuyên đến mức nào trong usage context.
- **Intensity:** cảm xúc/rủi ro mạnh đến mức nào nếu không xử lý.
- **Fit:** EnglishTestWeb MVP có thể xử lý lực này trực tiếp đến mức nào.
- **Total:** Frequency + Intensity + Fit, tối đa 15.

Priority:

- **14-15:** High, phải xử lý trong core MVP.
- **11-13:** Medium, nên xử lý nếu không làm loãng scope.
- **8-10:** Low, để sau core experience.
- **<8:** Deprioritize.

---

## Ranked Driving Forces

| Rank | Persona | Driving Force | Type | Frequency | Intensity | Fit | Total | Priority |
|---:|---|---|---|---:|---:|---:|---:|---|
| 1 | Giáo viên | Tạo bài test nhanh từ PDF/audio có sẵn, không nhập lại đề | Positive | 5 | 5 | 5 | 15 | High |
| 2 | Giáo viên | Tự chấm Listening/Reading và lưu kết quả tập trung | Positive | 5 | 5 | 5 | 15 | High |
| 3 | Giáo viên | Sợ thao tác tạo bài quá nhiều bước hoặc chậm hơn cách cũ | Negative | 5 | 5 | 5 | 15 | High |
| 4 | Học sinh | Vào đúng lớp, thấy đúng bài, làm bài và nộp rõ ràng | Positive | 5 | 5 | 4 | 14 | High |
| 5 | Học sinh | Sợ mất đáp án hoặc không biết bài đã nộp thành công chưa | Negative | 4 | 5 | 5 | 14 | High |
| 6 | Giáo viên | Xem kết quả theo lớp/học sinh/bài test ở một nơi | Positive | 4 | 5 | 5 | 14 | High |
| 7 | Giáo viên | Sợ điểm, file nói và feedback bị rời rạc qua nhiều kênh | Negative | 4 | 4 | 5 | 13 | Medium |
| 8 | Học sinh | Xem PDF, nghe audio và điền đáp án trong một luồng rõ ràng | Positive | 4 | 4 | 5 | 13 | Medium |
| 9 | Giáo viên | Chấm Speaking thuận tiện trên web | Positive | 3 | 4 | 5 | 12 | Medium |
| 10 | Giáo viên | Sợ học sinh không vào đúng bài/lớp và phải hỗ trợ thủ công | Negative | 3 | 4 | 4 | 11 | Medium |
| 11 | Học sinh | Sợ nhập nhầm lớp hoặc không thấy bài cần làm | Negative | 3 | 4 | 4 | 11 | Medium |
| 12 | Học sinh | Sợ giao diện làm bài rối giữa PDF, audio và answer form | Negative | 3 | 4 | 4 | 11 | Medium |

---

## MVP Design Priority

### Must Address

1. Fast teacher test creation from existing PDF/audio.
2. Answer key setup and automatic grading for Listening/Reading.
3. Clear student class-code access and assigned-test discovery.
4. Reliable answer persistence and explicit submission confirmation.
5. Results dashboard organized by class, student, test and submission attempt.

### Should Address

1. Speaking review surface with audio player, score input and feedback.
2. Guardrails to prevent publishing incomplete answer keys or misassigned tests.
3. Student progress indicators for answered/unanswered questions.
4. Clear recovery behavior for reload/network interruption.

### Defer

1. AI Speaking grading.
2. PDF parsing into structured questions.
3. Advanced item-level analytics by question type without teacher metadata.
4. Mobile app-specific flows.

---

## Product Decisions Supported

- Keep PDF/audio workflow central in MVP.
- Use a separate answer form rather than PDF parsing.
- Treat submission state, autosave/draft behavior and confirmation as core reliability features.
- Make dashboard scanning more important than advanced analytics in the first release.
- Keep Speaking manual but centralized.

