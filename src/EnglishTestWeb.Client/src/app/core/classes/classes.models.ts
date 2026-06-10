export interface ClassLookupPreview {
  classId: string;
  className: string;
  classCode: string;
  teacherDisplayName: string;
  status: string;
}

export interface ActiveClass {
  classId: string;
  className: string;
  classCode: string;
}

export interface ClassSummary {
  classId: string;
  className: string;
  classCode: string;
  status: string;
  enrolledStudentCount: number;
}

export interface ClassStudent {
  studentId: string;
  displayName: string;
  email: string | null;
  membershipStatus: string;
}

export interface ClassDetail {
  classId: string;
  className: string;
  classCode: string;
  status: string;
  students: ClassStudent[];
}

export const CLASS_ERROR_MESSAGES: Record<string, string> = {
  ERR_CLASS_CODE_REQUIRED: 'Nhập mã lớp.',
  ERR_CLASS_CODE_FORMAT: 'Mã lớp chưa đúng định dạng.',
  ERR_CLASS_CODE_INVALID: 'Không tìm thấy lớp với mã này. Kiểm tra lại mã lớp giáo viên gửi.',
  ERR_CLASS_CODE_EXPIRED: 'Mã lớp này đã hết hiệu lực. Hãy hỏi lại giáo viên.',
};

export const STUDENT_LOGIN_ERROR_MESSAGES: Record<string, string> = {
  ERR_STUDENT_IDENTIFIER_REQUIRED: 'Nhập tài khoản học sinh.',
  ERR_STUDENT_PASSWORD_REQUIRED: 'Nhập mật khẩu.',
  ERR_STUDENT_NOT_IN_CLASS:
    'Tài khoản này chưa thuộc lớp đã chọn. Kiểm tra lại với giáo viên.',
  ERR_STUDENT_LOGIN_INVALID: 'Tài khoản hoặc mật khẩu chưa đúng.',
  ERR_STUDENT_NETWORK: 'Chưa thể kết nối. Vui lòng thử lại.',
};

export const API_CLASS_ERROR_MESSAGES: Record<string, string> = {
  'classes.codeNotFound': CLASS_ERROR_MESSAGES['ERR_CLASS_CODE_INVALID'],
  'classes.codeInactive': CLASS_ERROR_MESSAGES['ERR_CLASS_CODE_EXPIRED'],
  'auth.notInClass': STUDENT_LOGIN_ERROR_MESSAGES['ERR_STUDENT_NOT_IN_CLASS'],
  'auth.loginInvalid': STUDENT_LOGIN_ERROR_MESSAGES['ERR_STUDENT_LOGIN_INVALID'],
};
