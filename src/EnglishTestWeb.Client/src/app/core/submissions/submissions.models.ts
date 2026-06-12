export interface SubmissionDto {
  id: string;
  status: string;
  mode: 'homework' | 'live-exam';
}

export interface AnswerRowDto {
  questionNumber: number;
  answer: string | null;
}

export interface SubmissionWorkspace {
  id: string;
  status: string;
  mode: 'homework' | 'live-exam';
  templateTitle: string;
  skill: string;
  classId: string;
  className: string;
  homeworkAssignmentId: string | null;
  liveExamSessionId: string | null;
  deadlineAt: string | null;
  timeLimitMinutes: number | null;
  sessionOpenedAt: string | null;
  sessionClosedAt: string | null;
  pdfMaterialId: string;
  audioMaterialId: string | null;
  questionCount: number;
  answerRows: AnswerRowDto[];
}

export interface CreateSubmissionRequest {
  homeworkAssignmentId: string | null;
  liveExamSessionId: string | null;
}

export interface AutosaveAnswersRow {
  questionNumber: number;
  answer: string | null;
}

export const SUBMISSION_MODE_LABELS: Record<string, string> = {
  homework: 'Bài tập về nhà',
  'live-exam': 'Thi trực tiếp',
};

export const SUBMISSION_ERROR_MESSAGES: Record<string, string> = {
  'submission.invalidSource': 'Nguồn bài thi không hợp lệ.',
  'submission.sourceUnavailable': 'Bài thi này hiện không còn khả dụng.',
  'submission.notFound': 'Không tìm thấy bài làm.',
  'submission.notDraft': 'Bài làm đã được nộp, không thể lưu thêm.',
  'files.notFound': 'File không tải được. Vui lòng thử lại.',
};
