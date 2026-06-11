export interface CreateHomeworkRequest {
  templateId: string;
  classId: string;
  deadlineAt: string;
  timeLimitMinutes: number | null;
}

export interface HomeworkAssignment {
  id: string;
  templateId: string;
  templateTitle: string;
  templateSkill: string;
  classId: string;
  className: string;
  deadlineAt: string;
  timeLimitMinutes: number | null;
  status: string;
  mode: string;
  allowedActions: string[];
  createdAt: string;
}

export const HOMEWORK_ERROR_MESSAGES: Record<string, string> = {
  'homework.templateNotFound': 'Không tìm thấy đề gốc hoặc đề không thuộc quyền quản lý của bạn.',
  'homework.templateNotReady': 'Đề gốc chưa ở trạng thái Sẵn sàng. Hãy đánh dấu đề sẵn sàng trước.',
  'homework.classNotFound': 'Không tìm thấy lớp hoặc lớp không thuộc quyền quản lý của bạn.',
  'homework.deadlinePast': 'Hạn nộp phải là thời điểm trong tương lai.',
  'homework.timeLimitInvalid': 'Giới hạn thời gian phải từ 1 đến 600 phút.',
  'homework.classNotActive': 'Lớp học đã không còn hoạt động. Vui lòng chọn lớp khác.',
  'homework.createFailed': 'Không thể tạo bài tập. Vui lòng thử lại.',
};

export function mapHomeworkCreateError(error: unknown): string {
  const extensions = (error as { error?: { extensions?: { code?: string } } })?.error?.extensions;
  const code = extensions?.code;
  if (code && Object.prototype.hasOwnProperty.call(HOMEWORK_ERROR_MESSAGES, code)) {
    return HOMEWORK_ERROR_MESSAGES[code];
  }
  return 'Có lỗi xảy ra. Vui lòng thử lại.';
}
