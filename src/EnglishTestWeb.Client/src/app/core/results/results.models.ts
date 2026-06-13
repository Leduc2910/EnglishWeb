export interface ResultRowDto {
  id: string;
  type: 'reading-listening' | 'speaking';
  mode: 'homework' | 'live-exam';
  studentName: string;
  studentId: string;
  classId: string;
  className: string;
  templateId: string;
  templateTitle: string;
  skill: 'reading' | 'listening' | 'speaking';
  status: string;
  score: number | null;
  submittedAt: string | null;
  createdAt: string;
}

export interface ResultsPageDto {
  items: ResultRowDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  needsGrading: number;
}

export interface ResultsFilter {
  classId?: string;
  mode?: 'homework' | 'live-exam';
  templateId?: string;
  q?: string;
  skill?: 'reading' | 'listening' | 'speaking';
  status?: string;
  page: number;
  pageSize: number;
  sort: string;
  direction: 'asc' | 'desc';
}

export const RESULT_STATUS_LABELS: Record<string, string> = {
  draft: 'Nháp',
  submitted: 'Đã nộp',
  'auto-graded': 'Đã chấm tự động',
  graded: 'Đã chấm',
};

export const RESULT_MODE_LABELS: Record<string, string> = {
  homework: 'Bài tập',
  'live-exam': 'Thi trực tiếp',
};

export const RESULT_SKILL_LABELS: Record<string, string> = {
  reading: 'Reading',
  listening: 'Listening',
  speaking: 'Speaking',
};
