import { useEffect, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router";
import { ApiError } from "@/api/client";
import { moviesApi } from "@/api/endpoints";
import type { MovieSort, MovieSummary, Page } from "@/api/types";
import { Pagination } from "@/components/Pagination";
import { StarRating } from "@/components/StarRating";

const sorts: { value: MovieSort; label: string }[] = [
  { value: "Title", label: "Title A-Z" },
  { value: "Newest", label: "Newest first" },
  { value: "Oldest", label: "Oldest first" },
  { value: "TopRated", label: "Top rated" },
];

const pageSize = 20;

export function BrowsePage() {
  const [params, setParams] = useSearchParams();
  const q = params.get("q") ?? "";
  const year = params.get("year") ?? "";
  const minRating = params.get("minRating") ?? "";
  const sort = (params.get("sort") as MovieSort | null) ?? "Title";
  const page = Number(params.get("page") ?? "1") || 1;

  const [draft, setDraft] = useState({ q, year, minRating });
  const [result, setResult] = useState<Page<MovieSummary> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => setDraft({ q, year, minRating }), [q, year, minRating]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    moviesApi
      .search({
        q: q || undefined,
        year: year ? Number(year) : undefined,
        minRating: minRating ? Number(minRating) : undefined,
        sort,
        page,
        pageSize,
      })
      .then((r) => !cancelled && setResult(r))
      .catch((e) => !cancelled && setError(e instanceof ApiError ? e.describe() : "Something went wrong."))
      .finally(() => !cancelled && setLoading(false));
    return () => {
      cancelled = true;
    };
  }, [q, year, minRating, sort, page]);

  const update = (changes: Record<string, string>) => {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(changes)) {
      if (value) next.set(key, value);
      else next.delete(key);
    }
    if (!("page" in changes)) next.delete("page");
    setParams(next);
  };

  const onSubmit = (e: FormEvent) => {
    e.preventDefault();
    update({ q: draft.q.trim(), year: draft.year, minRating: draft.minRating });
  };

  return (
    <>
      <h1 className="h3 mb-3">Browse movies</h1>

      <form className="row g-2 align-items-end mb-3" onSubmit={onSubmit} role="search">
        <div className="col-md-5">
          <label className="form-label small text-muted" htmlFor="search-q">
            Title or cast
          </label>
          <input
            id="search-q"
            className="form-control"
            placeholder="e.g. spirited, keanu"
            value={draft.q}
            onChange={(e) => setDraft({ ...draft, q: e.target.value })}
          />
        </div>
        <div className="col-6 col-md-2">
          <label className="form-label small text-muted" htmlFor="search-year">
            Year
          </label>
          <input
            id="search-year"
            className="form-control"
            type="number"
            min={1870}
            max={2100}
            value={draft.year}
            onChange={(e) => setDraft({ ...draft, year: e.target.value })}
          />
        </div>
        <div className="col-6 col-md-2">
          <label className="form-label small text-muted" htmlFor="search-rating">
            Min rating
          </label>
          <select
            id="search-rating"
            className="form-select"
            value={draft.minRating}
            onChange={(e) => setDraft({ ...draft, minRating: e.target.value })}
          >
            <option value="">Any</option>
            {[4, 3, 2, 1].map((n) => (
              <option key={n} value={n}>
                {n}+ stars
              </option>
            ))}
          </select>
        </div>
        <div className="col-md-3 d-flex gap-2">
          <button className="btn btn-primary flex-grow-1" type="submit">
            Search
          </button>
          {(q || year || minRating) && (
            <button className="btn btn-outline-secondary" type="button" onClick={() => setParams(new URLSearchParams())}>
              Clear
            </button>
          )}
        </div>
      </form>

      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-2">
        <span className="text-muted small" aria-live="polite">
          {result && !loading
            ? `${result.total.toLocaleString()} movie${result.total === 1 ? "" : "s"} in ${result.elapsedMs} ms`
            : "Searching…"}
        </span>
        <select
          className="form-select form-select-sm w-auto"
          aria-label="Sort by"
          value={sort}
          onChange={(e) => update({ sort: e.target.value === "Title" ? "" : e.target.value })}
        >
          {sorts.map((s) => (
            <option key={s.value} value={s.value}>
              {s.label}
            </option>
          ))}
        </select>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      {result && result.items.length === 0 && !loading && (
        <div className="alert alert-info">No movies match those filters.</div>
      )}

      {result && result.items.length > 0 && (
        <div className="table-responsive bg-white rounded shadow-sm">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Title</th>
                <th>Released</th>
                <th className="d-none d-md-table-cell">Cast</th>
                <th>Rating</th>
              </tr>
            </thead>
            <tbody>
              {result.items.map((m) => (
                <tr key={m.id}>
                  <td>
                    <Link to={`/movies/${m.id}`}>{m.title}</Link>
                  </td>
                  <td>{m.releaseDate.slice(0, 4)}</td>
                  <td className="d-none d-md-table-cell text-muted small">{m.cast}</td>
                  <td className="text-nowrap">
                    <StarRating value={m.averageRating} label={`Average rating for ${m.title}`} />
                    <span className="small text-muted ms-1">({m.ratingCount})</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {result && (
        <div className="d-flex justify-content-end mt-3">
          <Pagination page={page} totalPages={result.totalPages} onChange={(p) => update({ page: String(p) })} />
        </div>
      )}
    </>
  );
}
