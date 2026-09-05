import axios from "axios";

export const apiClient = axios.create({
  baseURL: "http://localhost:8080/api/v1"
});

let accessToken: string | null = null;
let refreshToken: string | null = null;

export function setTokens(access: string | null, refresh: string | null) {
  accessToken = access;
  refreshToken = refresh;
}

export function clearTokens() {
    accessToken = null;
    refreshToken = null;
}

apiClient.interceptors.request.use((config) => {
    if (accessToken) {
        config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
});

apiClient.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        if(error.response && error.response?.status === 401 && !originalRequest._retry && refreshToken) {
            originalRequest._retry = true;
            try {
                const {data} = await axios.post("http://localhost:8080/api/v1/auth/refresh", { refreshToken });
                setTokens(data.accessToken, data.refreshToken);
                originalRequest.headers['Authorization'] = `Bearer ${data.accessToken}`;
                return apiClient(originalRequest);
            } catch {
                clearTokens();
                window.location.href = "/login";
                return Promise.reject(error);
            }
        }
    }
)