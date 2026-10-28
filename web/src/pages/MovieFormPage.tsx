import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { ApiError } from "@/api/client";
import { moviesApi } from "@/api/endpoints";

interface FormState {
  title: string;
  releaseDate: string;
  overview: string;
  cast: string;
}

const empty: FormState = { title: "", releaseDate: "", overview: "", cast: "" };

export function MovieFormPage() {
  const params = useParams();
  const id = params.id ? Number(params.id) : null;
  const navigate = useNavigate();
  const [form, setForm] = useState<FormState>(empty);
  const [loading, setLoading] = useState(id !== null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (id === null) return;
    moviesApi
      .get(id)
      .then((m) => setForm({ title: m.title, releaseDate: m.releaseDate, overview: m.overview ?? "", cast: m.cast.join("\n") }))
      .catch((e) => setError(e instanceof ApiError ? e.describe() : "Couldn't load the movie."))
      .finally(() => setLoading(false));
  }, [id]);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    const payload = {
      title: form.title.trim(),
      releaseDate: form.releaseDate,
      overview: form.overview.trim() || null,
      cast: form.cast
        .split(/[\n,]/)
        .map((s) => s.trim())
        .filter(Boolean),
    };
    try {
      const saved = id === null ? await moviesApi.create(payload) : await moviesApi.update(id, payload);
      navigate(`/movies/${saved.id}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.describe() : "Couldn't save the movie.");
    } finally {
      setBusy(false);
    }
  };

  if (loading) return <p className="text-muted">Loading…</p>;

  return (
    <div className="card shadow-sm" style={{ maxWidth: "44rem" }}>
      <div className="card-body">
        <h1 className="h4 mb-3">{id === null ? "Add a movie" : "Edit movie"}</h1>
        {error && <div className="alert alert-danger">{error}</div>}
        <form onSubmit={submit}>
          <div className="mb-3">
            <label className="form-label" htmlFor="movie-title">
              Title
            </label>
            <input id="movie-title" className="form-control" required maxLength={300} value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="movie-date">
              Release date
            </label>
            <input id="movie-date" type="date" className="form-control" required value={form.releaseDate} onChange={(e) => setForm({ ...form, releaseDate: e.target.value })} />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="movie-overview">
              Overview
            </label>
            <textarea id="movie-overview" className="form-control" rows={3} maxLength={2000} value={form.overview} onChange={(e) => setForm({ ...form, overview: e.target.value })} />
          </div>
          <div className="mb-3">
            <label className="form-label" htmlFor="movie-cast">
              Cast
            </label>
            <textarea id="movie-cast" className="form-control" rows={4} placeholder="One name per line, in billing order" value={form.cast} onChange={(e) => setForm({ ...form, cast: e.target.value })} />
          </div>
          <div className="d-flex gap-2">
            <button type="submit" className="btn btn-primary" disabled={busy}>
              {id === null ? "Add movie" : "Save changes"}
            </button>
            <Link to={id === null ? "/" : `/movies/${id}`} className="btn btn-outline-secondary">
              Cancel
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
}
