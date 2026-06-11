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
  tags: string[];
  status: string;
  createdAt: string;
  updatedAt: string;
  lastUsedAt: string | null;
  archivedAt: string | null;
}

export interface TestTemplateSetupPayload {
  title: string;
  skill: string;
  description?: string | null;
  tags?: string[];
}

export interface TestTemplateListFilters {
  skill: TemplateSkill;
  status: TemplateStatus;
  q: string;
}

export type MaterialRole = 'pdf' | 'audio' | 'cue';

export interface TestMaterialItem {
  materialId: string;
  fileId: string;
  role: MaterialRole;
  originalFileName: string;
  sizeBytes: number;
  contentType: string;
  uploadedAt: string;
}

export interface TestMaterialListResponse {
  items: TestMaterialItem[];
}

export const PDF_MAX_BYTES = 25 * 1024 * 1024;

export const AUDIO_MAX_BYTES = 50 * 1024 * 1024;

export const TEMPLATE_ERROR_MESSAGES: Record<string, string> = {
  ERR_TEMPLATE_NOT_READY: 'Đề chưa sẵn sàng. Hoàn thiện và đánh dấu Ready trước khi giao bài.',
  ERR_TEMPLATE_NAME_REQUIRED: 'Nhập tên đề.',
  ERR_SKILL_REQUIRED: 'Chọn kỹ năng cho bài test.',
  ERR_TAG_LIMIT: 'Tối đa 10 tag cho một đề.',
  'templates.notFound': 'Không tìm thấy đề.',
  'templates.forbidden': 'Bạn không có quyền truy cập đề này.',
  'templates.nameRequired': 'Nhập tên đề.',
  'templates.skillRequired': 'Chọn kỹ năng cho bài test.',
  'templates.skillInvalid': 'Kỹ năng không hợp lệ.',
  'templates.tagLimit': 'Tối đa 10 tag cho một đề.',
  'templates.tagsStorageLimit': 'Tags quá dài để lưu. Rút ngắn tag hoặc bỏ bớt tag.',
  'templates.tagTooLong': 'Mỗi tag tối đa 32 ký tự.',
  'templates.titleTooLong': 'Tên đề quá dài (tối đa 120 ký tự).',
  'templates.descriptionTooLong': 'Mô tả quá dài (tối đa 2000 ký tự).',
  'templates.notEditable': 'Chỉ có thể chỉnh sửa đề ở trạng thái Nháp.',
  ERR_FILE_TYPE: 'Chỉ hỗ trợ file PDF.',
  ERR_FILE_SIZE: 'File vượt quá dung lượng cho phép.',
  ERR_PDF_REQUIRED: 'Upload file PDF đề Reading trước khi tiếp tục.',
  ERR_UPLOAD_INCOMPLETE: 'File chưa upload xong.',
  'files.invalidType': 'Chỉ hỗ trợ file PDF.',
  'files.tooLarge': 'File vượt quá dung lượng cho phép.',
  'materials.pdfRequired': 'Upload file PDF đề Reading trước khi tiếp tục.',
  'materials.roleInvalid': 'Loại tài liệu không hợp lệ cho kỹ năng này.',
  'materials.uploadFailed': 'Upload thất bại. Vui lòng thử lại.',
  'materials.notFound': 'Không tìm thấy tài liệu.',
  'files.notFound': 'Không tìm thấy file.',
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

export function mapTemplateApiError(error: unknown): string {
  if (error && typeof error === 'object' && 'error' in error) {
    const httpError = error as { error?: { code?: string; extensions?: { code?: string } } };
    const body = httpError.error;
    if (body && typeof body === 'object') {
      const code = body.code ?? body.extensions?.code;
      if (code && TEMPLATE_ERROR_MESSAGES[code]) {
        return TEMPLATE_ERROR_MESSAGES[code];
      }
    }
  }

  return 'Không thể lưu đề. Vui lòng thử lại.';
}

export function mapMaterialApiError(error: unknown): string {
  if (error && typeof error === 'object' && 'error' in error) {
    const httpError = error as { error?: { code?: string; extensions?: { code?: string } } };
    const body = httpError.error;
    if (body && typeof body === 'object') {
      const code = body.code ?? body.extensions?.code;
      if (code && TEMPLATE_ERROR_MESSAGES[code]) {
        return TEMPLATE_ERROR_MESSAGES[code];
      }
    }
  }

  return 'Không thể upload tài liệu. Vui lòng thử lại.';
}

export function materialContinueRequiredMessage(skill: string): string {
  switch (skill) {
    case 'listening':
      return 'Upload file PDF đề Listening trước khi tiếp tục.';
    case 'speaking':
      return 'Upload file cue PDF trước khi tiếp tục.';
    default:
      return TEMPLATE_ERROR_MESSAGES['ERR_PDF_REQUIRED'];
  }
}

export function previewLabelForRole(role: MaterialRole): string {
  return role === 'audio' ? 'Nghe thử audio' : 'Xem nhanh PDF';
}

export function materialSlotsForSkill(skill: string): {
  role: MaterialRole;
  label: string;
  required: boolean;
  accept: string;
  pickerLabel: string;
}[] {
  switch (skill) {
    case 'listening':
      return [
        {
          role: 'pdf',
          label: 'PDF đề Listening',
          required: true,
          accept: 'application/pdf,.pdf',
          pickerLabel: 'Chọn file PDF',
        },
        {
          role: 'audio',
          label: 'Audio (tùy chọn)',
          required: false,
          accept: 'audio/mpeg,audio/mp4,audio/wav,.mp3,.m4a,.wav',
          pickerLabel: 'Chọn file audio',
        },
      ];
    case 'speaking':
      return [
        {
          role: 'cue',
          label: 'Cue/prompt PDF',
          required: true,
          accept: 'application/pdf,.pdf',
          pickerLabel: 'Chọn file PDF cue',
        },
      ];
    case 'reading':
    default:
      return [
        {
          role: 'pdf',
          label: 'PDF đề Reading',
          required: true,
          accept: 'application/pdf,.pdf',
          pickerLabel: 'Chọn file PDF',
        },
      ];
  }
}

export function validateMaterialFile(role: MaterialRole, file: File): string | null {
  const extension = file.name.includes('.') ? file.name.slice(file.name.lastIndexOf('.')).toLowerCase() : '';

  if (role === 'pdf' || role === 'cue') {
    if (extension !== '.pdf') {
      return TEMPLATE_ERROR_MESSAGES['ERR_FILE_TYPE'];
    }

    const allowedPdfTypes = new Set([
      'application/pdf',
      'application/octet-stream',
      'application/x-pdf',
      '',
    ]);
    if (file.type && !allowedPdfTypes.has(file.type)) {
      return TEMPLATE_ERROR_MESSAGES['ERR_FILE_TYPE'];
    }

    if (file.size > PDF_MAX_BYTES) {
      return TEMPLATE_ERROR_MESSAGES['ERR_FILE_SIZE'];
    }

    return null;
  }

  const allowedExtensions = new Set(['.mp3', '.m4a', '.wav']);
  const allowedTypes = new Set([
    'audio/mpeg',
    'audio/x-mpeg',
    'audio/mp4',
    'audio/wav',
    'audio/x-m4a',
    'audio/x-wav',
    'application/octet-stream',
    '',
  ]);
  if (!allowedExtensions.has(extension)) {
    return TEMPLATE_ERROR_MESSAGES['ERR_FILE_TYPE'];
  }

  if (file.type && !allowedTypes.has(file.type)) {
    return TEMPLATE_ERROR_MESSAGES['ERR_FILE_TYPE'];
  }

  if (file.size > AUDIO_MAX_BYTES) {
    return TEMPLATE_ERROR_MESSAGES['ERR_FILE_SIZE'];
  }

  return null;
}

export function parseTagsInput(value: string): string[] {
  const result: string[] = [];

  for (const part of value.split(',')) {
    const trimmed = part.trim();
    if (!trimmed) {
      continue;
    }

    if (result.some((existing) => existing.toLowerCase() === trimmed.toLowerCase())) {
      continue;
    }

    result.push(trimmed);
  }

  return result;
}

export interface SkillChecklistItem {
  label: string;
  required: boolean;
}

export function skillChecklist(skill: string): SkillChecklistItem[] {
  switch (skill) {
    case 'listening':
      return [
        { label: 'PDF đề (bắt buộc ở bước 2)', required: true },
        { label: 'Audio (tùy chọn ở bước 2)', required: false },
        { label: 'Answer key (bắt buộc ở bước 3)', required: true },
      ];
    case 'speaking':
      return [
        { label: 'Cue/prompt material (bước 2)', required: true },
        { label: 'Chấm thủ công (không auto-grade)', required: false },
      ];
    case 'reading':
    default:
      return [
        { label: 'PDF đề (bắt buộc ở bước 2)', required: true },
        { label: 'Answer key (bắt buộc ở bước 3)', required: true },
      ];
  }
}
