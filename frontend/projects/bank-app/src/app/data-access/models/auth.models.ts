export interface LoginRequest {
  identifier: string; // email veya tcNo
  password: string;
}

export interface RegisterRequest {
  tcNo: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  password: string;
  membership: 'Corporate' | 'Personal';
}

export interface AuthResponse {
  userId: number;
  fullName: string;
  token: string;
}

