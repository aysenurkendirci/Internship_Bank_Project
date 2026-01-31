import { API_BASE_URL } from '../../api.config';

const base = API_BASE_URL;

export const ENDPOINTS = {
  auth: {
    login: `${base}/api/auth/login`,
    register: `${base}/api/auth/register`,
  },
  dashboard: {
    summary: `${base}/api/dashboard`,
  },
} as const;
