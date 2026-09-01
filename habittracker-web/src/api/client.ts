import axios from "axios";

const apiClient = axios.create({
  baseURL: "http://localhost:8080/api/v1"
});

let accessToken: string | null = null;
let refreshToken: string | null = null;

export function setTokens(access: string | null, refresh: string | null) {
  accessToken = access;
  refreshToken = refresh;
}