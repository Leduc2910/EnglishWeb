export function sanitizeTeacherReturnUrl(returnUrl: string | null | undefined): string | null {
  if (!returnUrl) {
    return null;
  }

  let decoded: string;
  try {
    decoded = decodeURIComponent(returnUrl);
  } catch {
    return null;
  }

  if (!decoded.startsWith('/teacher/')) {
    return null;
  }

  if (decoded.startsWith('//') || decoded.includes('://')) {
    return null;
  }

  return decoded;
}
