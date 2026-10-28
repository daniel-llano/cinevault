import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { ApiError } from "@/api/client";
import { moviesApi } from "@/api/endpoints";
import type { MovieDetails } from "@/api/types";
import { useAuth } from "@/auth/AuthContext";
import { StarRating } from "@/components/StarRating";

export function MoviePage() {
  const id = Number(useParams().id);
  const { user, isAdmin } = useAuth();
  const navigate = useNavigate();
  const [movie, setMovie] = useState<MovieDetails | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [busy, setBusy] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);

  useEffect(() => {
    setMovie(null);
    setNotFound(false);
    moviesApi
      .get(id)
      .then(setMovie)
      .catch((e) => (e instanceof ApiError && e.status === 404 ? setNotFound(true) : setError(String(e.message))));
  }, [id, user]);

  const run = async (action: () => Promise<void>) => {
    setBusy(true);
    setError(null);
    try {
      await action();
    } catch (e) {
      setError(e instanceof ApiError ? e.describe() : "Something went wrong.");
    } finally {
      setBusy(false);
    }
  };

  const rate = (stars: number) =>
    run(async () => {
      const summary = await moviesApi.rate(id, stars);
      setMovie((m) => (m ? { ...m, ...summary } : m));
    });

  const unrate = () =>
    run(async () => {
      const summary = await moviesApi.unrate(id);
      setMovie((m) => (m ? { ...m, ...summary } : m));
    });

  const remove = () =>
    run(async () => {
      await moviesApi.remove(id);
      navigate("/", { replace: true });
    });

  if (notFound)
    return (
      <>
        <h1 className="h3">Movie not found</h1>
        <p>
          It may have been removed. <Link to="/">Back to browsing</Link>
        </p>
      </>
    );

  if (!movie) return error ? <div className="alert alert-danger">{error}</div> : <p className="text-muted">Loading…</p>;

  return (
    <article style={{ maxWidth: "48rem" }}>
      <div className="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-2">
        <div>
          <h1 className="h3 mb-0">{movie.title}</h1>
          <span className="text-muted">
            {new Date(movie.releaseDate + "T00:00:00").toLocaleDateString(undefined, { dateStyle: "long" })}
          </span>
        </div>
        {isAdmin && (
          <div className="d-flex gap-2">
            <Link to={`/movies/${movie.id}/edit`} className="btn btn-outline-secondary btn-sm">
              Edit
            </Link>
            <button className="btn btn-outline-danger btn-sm" onClick={() => setConfirmDelete(true)}>
              Delete
            </button>
          </div>
        )}
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      {confirmDelete && (
        <div className="alert alert-warning d-flex flex-wrap align-items-center gap-2" role="alertdialog">
          <span className="me-auto">Delete this movie and all of its ratings?</span>
          <button className="btn btn-sm btn-outline-secondary" onClick={() => setConfirmDelete(false)} disabled={busy}>
            Cancel
          </button>
          <button className="btn btn-sm btn-danger" onClick={remove} disabled={busy}>
            Delete movie
          </button>
        </div>
      )}

      <section className="card shadow-sm mb-3">
        <div className="card-body">
          <div className="d-flex align-items-center gap-2 mb-2">
            <StarRating value={movie.averageRating} label="Average rating" />
            <span className="text-muted small">
              {movie.averageRating?.toFixed(1) ?? "No ratings yet"}
              {movie.ratingCount > 0 && ` from ${movie.ratingCount} rating${movie.ratingCount === 1 ? "" : "s"}`}
            </span>
          </div>
          {movie.overview && <p className="mb-2">{movie.overview}</p>}
          {movie.cast.length > 0 && (
            <p className="mb-0">
              <strong>Cast:</strong> {movie.cast.join(", ")}
            </p>
          )}
        </div>
      </section>

      <section className="card shadow-sm">
        <div className="card-body">
          <h2 className="h6">Your rating</h2>
          {user ? (
            <div className="d-flex align-items-center gap-3">
              <StarRating value={movie.myRating} onChange={rate} disabled={busy} label="Your rating" />
              {movie.myRating && (
                <button className="btn btn-link btn-sm p-0" onClick={unrate} disabled={busy}>
                  Remove my rating
                </button>
              )}
            </div>
          ) : (
            <p className="mb-0">
              <Link to={`/login?returnTo=/movies/${movie.id}`}>Sign in</Link> to rate this movie.
            </p>
          )}
        </div>
      </section>
    </article>
  );
}
