export function normalizeClassCode(raw: string): string | null {
  const normalized = raw.trim().replace(/[\s-]/g, '').toUpperCase();
  if (!/^[A-Z0-9]{4,12}$/.test(normalized)) {
    return null;
  }

  return normalized;
}

export function formatClassCodeInput(raw: string): string {
  return raw.replace(/[\s-]/g, '').toUpperCase();
}
