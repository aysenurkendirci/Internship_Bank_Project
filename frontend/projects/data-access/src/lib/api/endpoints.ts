export const ENDPOINTS = {
  auth: {
    // Backend portun 5164 olduğu için tam adresi buraya yazıyoruz
    login: 'http://localhost:5164/api/auth/login',
    register: 'http://localhost:5164/api/auth/register',
  },
} as const;