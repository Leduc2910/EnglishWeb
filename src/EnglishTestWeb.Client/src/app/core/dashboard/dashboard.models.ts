export interface TeacherDashboardSummaryDto {
  templateCount: number;
  activeHomeworkCount: number;
  openLiveExamCount: number;
  recentSubmissionCount: number;
  pendingSpeakingCount: number;
}

export interface TeacherRecentWorkItemDto {
  type: string;
  id: string;
  title: string;
  className: string;
  mode: string;
  status: string;
  timestamp: string;
}

export interface TeacherDashboardDto {
  summary: TeacherDashboardSummaryDto;
  recentWork: TeacherRecentWorkItemDto[];
}

export const RECENT_WORK_MODE_LABELS: Record<string, string> = {
  homework: 'Homework',
  'live-exam': 'Thi trực tiếp',
};

export const RECENT_WORK_STATUS_LABELS: Record<string, string> = {
  submitted: 'Đã nộp',
  'auto-graded': 'Đã chấm tự động',
  graded: 'Đã chấm',
};
