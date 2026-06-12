export interface DraftFileDto {
  fileId: string;
  originalFileName: string;
  sizeBytes: number;
  uploadedAt: string;
}

export interface SpeakingSubmissionDto {
  id: string;
  status: 'draft' | 'submitted' | 'graded';
  mode: 'homework' | 'live-exam';
  templateTitle: string;
  templateSkill: string;
  className: string;
  isSourceOpen: boolean;
  cueMaterialFileId: string | null;
  cueMaterialFileName: string | null;
  draftFile: DraftFileDto | null;
}

export interface CreateSpeakingSubmissionRequest {
  homeworkAssignmentId: string | null;
  liveExamSessionId: string | null;
}

export const SPEAKING_ERROR_MESSAGES: Record<string, string> = {
  'speaking.invalidSource': 'Nguồn bài thi không hợp lệ.',
  'speaking.sourceUnavailable': 'Bài thi này hiện không còn khả dụng.',
  'speaking.notFound': 'Không tìm thấy bài làm nói.',
  'speaking.emptyFile': 'Vui lòng chọn file trước khi tải lên.',
  'speaking.invalidFileType': 'Loại file không được hỗ trợ. Vui lòng tải lên file âm thanh hoặc video.',
  'speaking.fileTooLarge': 'File vượt quá giới hạn 100MB.',
  'speaking.alreadySubmitted': 'Bài làm đã được nộp.',
};

export const ALLOWED_SPEAKING_MIME_TYPES = [
  'audio/mpeg',
  'audio/wav',
  'audio/ogg',
  'audio/webm',
  'audio/mp4',
  'video/mp4',
  'video/webm',
];

export const MAX_SPEAKING_FILE_SIZE_BYTES = 104_857_600; // 100MB
