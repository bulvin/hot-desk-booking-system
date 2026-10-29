const baseUrl = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/$/, '');

export const apiUrl = (path: string) => `${baseUrl}${path}`;
