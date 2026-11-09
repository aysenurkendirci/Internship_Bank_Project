// Request and response shapes of the sign-in endpoints; they mirror Bank.Contracts.Auth.
export interface LoginRequest {
  tcNo: string;
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

