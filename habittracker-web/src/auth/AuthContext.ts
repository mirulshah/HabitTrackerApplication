// src/auth/AuthContext.ts
import { createContext } from 'react';

export interface AuthContextValue {
  isAuthenticated: boolean;
  loginSuccess: (accessToken: string, refreshToken: string) => void;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);