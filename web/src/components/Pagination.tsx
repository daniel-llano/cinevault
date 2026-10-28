interface Props {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}

export function Pagination({ page, totalPages, onChange }: Props) {
  if (totalPages <= 1) return null;
  return (
    <nav aria-label="Pages">
      <ul className="pagination pagination-sm mb-0">
        <li className={`page-item ${page <= 1 ? "disabled" : ""}`}>
          <button className="page-link" onClick={() => onChange(page - 1)} disabled={page <= 1}>
            Previous
          </button>
        </li>
        <li className="page-item disabled">
          <span className="page-link">
            Page {page.toLocaleString()} of {totalPages.toLocaleString()}
          </span>
        </li>
        <li className={`page-item ${page >= totalPages ? "disabled" : ""}`}>
          <button className="page-link" onClick={() => onChange(page + 1)} disabled={page >= totalPages}>
            Next
          </button>
        </li>
      </ul>
    </nav>
  );
}
