interface Props {
  value: number | null;
  onChange?: (stars: number) => void;
  disabled?: boolean;
  label: string;
}

/** Five stars. Read-only unless onChange is passed. */
export function StarRating({ value, onChange, disabled, label }: Props) {
  const filled = Math.round(value ?? 0);
  return (
    <span className="stars" role={onChange ? "radiogroup" : "img"} aria-label={onChange ? label : `${label}: ${value ?? "no"} stars`}>
      {[1, 2, 3, 4, 5].map((star) =>
        onChange ? (
          <button
            key={star}
            type="button"
            role="radio"
            aria-checked={filled === star}
            aria-label={`${star} star${star > 1 ? "s" : ""}`}
            className={`star-button ${star <= filled ? "on" : ""}`}
            disabled={disabled}
            onClick={() => onChange(star)}
          >
            ★
          </button>
        ) : (
          <span key={star} aria-hidden="true" className={star <= filled ? "on" : ""}>
            ★
          </span>
        ),
      )}
    </span>
  );
}
