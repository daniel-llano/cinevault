import { useState, type FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router";
import { ApiError } from "@/api/client";
import { useAuth } from "@/auth/AuthContext";

function useReturnTo() {
  const [params] = useSearchParams();
  const target = params.get("returnTo") ?? "/";
  // Only follow same-site paths.
  return target.startsWith("/") && !target.startsWith("//") ? target : "/";
}

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const returnTo = useReturnTo();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await login(email, password);
      navigate(returnTo, { replace: true });
    } catch (err) {
      setError(err instanceof ApiError && err.status === 401 ? "Wrong email or password." : (err as Error).message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="card shadow-sm card-narrow">
      <div className="card-body p-4">
        <h1 className="h4 mb-3">Sign in</h1>
        {error && (
          <div className="alert alert-danger py-2" role="alert">
            {error}
          </div>
        )}
        <form onSubmit={submit}>
          <div className="mb-3">
            <label className="form-label" htmlFor="login-email">
              Email
            </label>
            <input id="login-email" type="email" className="form-control" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="login-password">
              Password
            </label>
            <input id="login-password" type="password" className="form-control" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>
          <button type="submit" className="btn btn-primary w-100" disabled={busy}>
            {busy ? "Signing in…" : "Sign in"}
          </button>
        </form>
        <p className="text-center small mt-3 mb-0">
          New here? <Link to={`/register?returnTo=${encodeURIComponent(returnTo)}`}>Create an account</Link>
        </p>
      </div>
    </div>
  );
}

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const returnTo = useReturnTo();
  const [form, setForm] = useState({ displayName: "", email: "", password: "" });
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await register(form.email, form.password, form.displayName);
      navigate(returnTo, { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.describe() : (err as Error).message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="card shadow-sm card-narrow">
      <div className="card-body p-4">
        <h1 className="h4 mb-3">Create an account</h1>
        {error && (
          <div className="alert alert-danger py-2" role="alert">
            {error}
          </div>
        )}
        <form onSubmit={submit}>
          <div className="mb-3">
            <label className="form-label" htmlFor="register-name">
              Display name
            </label>
            <input id="register-name" className="form-control" required minLength={2} maxLength={100} value={form.displayName} onChange={(e) => setForm({ ...form, displayName: e.target.value })} />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="register-email">
              Email
            </label>
            <input id="register-email" type="email" className="form-control" autoComplete="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="register-password">
              Password
            </label>
            <input id="register-password" type="password" className="form-control" autoComplete="new-password" required minLength={8} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
            <div className="form-text">At least 8 characters, with a letter and a digit.</div>
          </div>
          <button type="submit" className="btn btn-primary w-100" disabled={busy}>
            {busy ? "Creating account…" : "Create account"}
          </button>
        </form>
        <p className="text-center small mt-3 mb-0">
          Already registered? <Link to="/login">Sign in</Link>
        </p>
      </div>
    </div>
  );
}
