export type Role = "Admin" | "Member";

export interface User {
  id: string;
  email: string;
  displayName: string;
  role: Role;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export type MovieSort = "Title" | "Newest" | "Oldest" | "TopRated";

export interface MovieSummary {
  id: number;
  title: string;
  releaseDate: string;
  cast: string;
  averageRating: number | null;
  ratingCount: number;
}

export interface MovieDetails {
  id: number;
  title: string;
  releaseDate: string;
  overview: string | null;
  cast: string[];
  averageRating: number | null;
  ratingCount: number;
  myRating: number | null;
}

export interface Page<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
  elapsedMs: number;
}

export interface RatingSummary {
  averageRating: number | null;
  ratingCount: number;
  myRating: number | null;
}

export interface SaveMovie {
  title: string;
  releaseDate: string;
  overview: string | null;
  cast: string[];
}

export interface MovieQuery {
  q?: string;
  year?: number;
  minRating?: number;
  sort?: MovieSort;
  page?: number;
  pageSize?: number;
}
