import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router";
import { useAuth } from "@/auth/AuthContext";

export function RequireAdmin({ children }: { children: ReactNode }) {
  const { user, isAdmin, loading } = useAuth();
  const location = useLocation();

  if (loading) return <p className="text-muted">Checking your session…</p>;
  if (!user) return <Navigate to={`/login?returnTo=${encodeURIComponent(location.pathname)}`} replace />;
  if (!isAdmin) return <div className="alert alert-warning">Only admins can manage the catalog.</div>;
  return <>{children}</>;
}
