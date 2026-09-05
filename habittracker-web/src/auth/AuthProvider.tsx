// src/auth/AuthProvider.tsx
import { useState, type ReactNode } from 'react';
import { AuthContext } from './AuthContext';
import { setTokens, clearTokens } from '../api/client';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  function loginSuccess(accessToken: string, refreshToken: string) {
    setTokens(accessToken, refreshToken);
    setIsAuthenticated(true);
  }

  function logout() {
    clearTokens();
    setIsAuthenticated(false);
  }

  return (
    <AuthContext.Provider value={{ isAuthenticated, loginSuccess, logout }}>
      {children}
    </AuthContext.Provider>
  );
}