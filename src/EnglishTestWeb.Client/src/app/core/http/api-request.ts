export function isApiRequest(url: string): boolean {
  try {
    const path = url.startsWith('http') ? new URL(url).pathname : url;
    return path.toLowerCase().startsWith('/api');
  } catch {
    return url.toLowerCase().startsWith('/api');
  }
}
