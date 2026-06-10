export type TemplateSkill = 'reading' | 'listening' | 'speaking' | '';
export type TemplateStatus = 'draft' | 'ready' | 'archived' | '';

export interface TestTemplateListItem {
  templateId: string;
  title: string;
  skill: string;
  status: string;
  lastUsedAt: string | null;
  updatedAt: string;
}

export interface TestTemplateDetail {
  templateId: string;
  title: string;
  skill: string;
  description: string | null;
  status: string;
  createdAt: string;
  updatedAt: string;
  lastUsedAt: string | null;
  archivedAt: string | null;
}

export interface TestTemplateListFilters {
  skill: TemplateSkill;
  status: TemplateStatus;
  q: string;
}

export const TEMPLATE_ERROR_MESSAGES: Record<string, string> = {
  ERR_TEMPLATE_NOT_READY: 'Đề chưa sẵn sàng. Hoàn thiện và đánh dấu Ready trước khi giao bài.',
  'templates.notFound': 'Không tìm thấy đề.',
  'templates.forbidden': 'Bạn không có quyền truy cập đề này.',
};

export const SKILL_LABELS: Record<string, string> = {
  reading: 'Reading',
  listening: 'Listening',
  speaking: 'Speaking',
};

export const STATUS_LABELS: Record<string, string> = {
  draft: 'Nháp',
  ready: 'Sẵn sàng sử dụng',
  archived: 'Đã lưu trữ',
};
