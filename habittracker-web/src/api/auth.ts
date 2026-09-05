import { apiClient } from "./client";

export interface LoginRequest {
    email: string;
    password: string;
}

export interface AuthResponse {
    accessToken: string;
    refreshToken: string;
    accessTokenExpiresAt: string;
}

export async function login(request: LoginRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>("/auth/login", request);
    return data;
}