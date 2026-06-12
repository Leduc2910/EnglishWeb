export interface AssignedTestItem {
  id: string;
  mode: 'homework' | 'live-exam';
  title: string;
  skill: string;
  classId: string;
  className: string;
  status: string;
  studentStatus: 'available' | 'not-open' | 'expired' | 'closed';
  deadlineAt: string | null;
  timeLimitMinutes: number | null;
  scheduledStartAt: string | null;
  openedAt: string | null;
  closedAt: string | null;
  createdAt: string;
}

export const STUDENT_STATUS_LABELS: Record<string, string> = {
  available: 'Đang mở',
  'not-open': 'Chưa mở',
  expired: 'Đã hết hạn',
  closed: 'Đã đóng',
};

export const ASSIGNED_TEST_ERROR_MESSAGES: Record<string, string> = {
  ERR_LIVE_EXAM_NOT_OPEN: 'Bài thi trực tiếp chưa được mở. Vui lòng chờ giáo viên mở bài.',
};
