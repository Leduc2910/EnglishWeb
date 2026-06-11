export interface CreateLiveExamRequest {
  templateId: string;
  classId: string;
  scheduledStartAt: string | null;
  scheduledEndAt: string | null;
}

export interface LiveExamSession {
  id: string;
  templateId: string;
  templateTitle: string;
  templateSkill: string;
  classId: string;
  className: string;
  status: string;
  scheduledStartAt: string | null;
  scheduledEndAt: string | null;
  openedAt: string | null;
  closedAt: string | null;
  createdAt: string;
}

export const LIVE_EXAM_ERROR_MESSAGES: Record<string, string> = {
  'liveExam.templateNotFound': 'Không tìm thấy đề gốc hoặc đề không thuộc quyền quản lý của bạn.',
  'liveExam.templateNotReady': 'Đề gốc chưa ở trạng thái Sẵn sàng. Hãy đánh dấu đề sẵn sàng trước.',
  'liveExam.classNotFound': 'Không tìm thấy lớp hoặc lớp không thuộc quyền quản lý của bạn.',
  'liveExam.classNotActive': 'Lớp học đã không còn hoạt động. Vui lòng chọn lớp khác.',
  'liveExam.createFailed': 'Không thể tạo phiên thi. Vui lòng thử lại.',
  'liveExam.sessionNotFound': 'Không tìm thấy phiên thi.',
  'liveExam.alreadyOpen': 'Phiên thi đã đang mở.',
  'liveExam.sessionClosed': 'Phiên thi đã đóng, không thể mở lại.',
  'liveExam.alreadyClosed': 'Phiên thi đã đóng.',
  'liveExam.sessionNotOpen': 'Phiên thi chưa mở, không thể đóng.',
  'liveExam.transitionFailed': 'Không thể thay đổi trạng thái phiên. Vui lòng thử lại.',
};

export function mapLiveExamError(error: unknown): string {
  const extensions = (error as { error?: { extensions?: { code?: string } } })?.error?.extensions;
  const code = extensions?.code;
  if (code && Object.prototype.hasOwnProperty.call(LIVE_EXAM_ERROR_MESSAGES, code)) {
    return LIVE_EXAM_ERROR_MESSAGES[code];
  }
  return 'Có lỗi xảy ra. Vui lòng thử lại.';
}
