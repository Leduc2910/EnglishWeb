export interface CurrentUser {
  userId: string;
  email: string | null;
  userName: string | null;
  roles: string[];
}

export interface LoginRequest {
  identifier: string;
  password: string;
  rememberMe: boolean;
}

export interface StudentLoginRequest {
  identifier: string;
  password: string;
  classCode: string;
  rememberMe: boolean;
}

export interface StudentLoginResponse extends CurrentUser {
  activeClass: {
    classId: string;
    className: string;
    classCode: string;
  };
}

export const LOGIN_ERROR_MESSAGES: Record<string, string> = {
  ERR_LOGIN_IDENTIFIER_REQUIRED: 'Nhập email hoặc tên đăng nhập.',
  ERR_LOGIN_PASSWORD_REQUIRED: 'Nhập mật khẩu.',
  ERR_LOGIN_INVALID: 'Thông tin đăng nhập chưa đúng. Kiểm tra lại email và mật khẩu.',
  ERR_LOGIN_NETWORK: 'Chưa thể kết nối. Vui lòng thử lại.',
};

export const API_AUTH_ERROR_MESSAGES: Record<string, string> = {
  'auth.loginInvalid': LOGIN_ERROR_MESSAGES['ERR_LOGIN_INVALID'],
};
