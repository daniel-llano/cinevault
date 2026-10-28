import { request } from "./client";
import type { AuthResponse, MovieDetails, MovieQuery, MovieSummary, Page, RatingSummary, SaveMovie, User } from "./types";

export const authApi = {
  login: (email: string, password: string) =>
    request<AuthResponse>("/api/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }),
  register: (email: string, password: string, displayName: string) =>
    request<AuthResponse>("/api/auth/register", { method: "POST", body: JSON.stringify({ email, password, displayName }) }),
  me: () => request<User>("/api/auth/me"),
};

export function toSearchParams(query: MovieQuery): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && value !== "") params.set(key, String(value));
  }
  return params.toString();
}

export const moviesApi = {
  search: (query: MovieQuery) => request<Page<MovieSummary>>(`/api/movies?${toSearchParams(query)}`),
  get: (id: number) => request<MovieDetails>(`/api/movies/${id}`),
  create: (movie: SaveMovie) => request<MovieDetails>("/api/movies", { method: "POST", body: JSON.stringify(movie) }),
  update: (id: number, movie: SaveMovie) =>
    request<MovieDetails>(`/api/movies/${id}`, { method: "PUT", body: JSON.stringify(movie) }),
  remove: (id: number) => request<void>(`/api/movies/${id}`, { method: "DELETE" }),
  rate: (id: number, stars: number) =>
    request<RatingSummary>(`/api/movies/${id}/rating`, { method: "PUT", body: JSON.stringify({ stars }) }),
  unrate: (id: number) => request<RatingSummary>(`/api/movies/${id}/rating`, { method: "DELETE" }),
};
