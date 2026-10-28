import { Link, NavLink, Outlet, useNavigate } from "react-router";
import { useAuth } from "@/auth/AuthContext";

export function Layout() {
  const { user, isAdmin, logout } = useAuth();
  const navigate = useNavigate();

  return (
    <>
      <header className="app-header">
        <nav className="container d-flex flex-wrap align-items-center gap-3 py-2">
          <Link to="/" className="brand">
            CineVault
          </Link>
          <NavLink to="/" end className="nav-link">
            Browse
          </NavLink>
          {isAdmin && (
            <NavLink to="/movies/new" className="nav-link">
              Add movie
            </NavLink>
          )}
          <div className="ms-auto d-flex align-items-center gap-2">
            {user ? (
              <>
                <span className="small text-white-50">
                  {user.displayName}
                  {isAdmin && <span className="badge text-bg-warning ms-2">Admin</span>}
                </span>
                <button
                  className="btn btn-sm btn-outline-light"
                  onClick={() => {
                    logout();
                    navigate("/");
                  }}
                >
                  Sign out
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="btn btn-sm btn-light">
                  Sign in
                </Link>
                <Link to="/register" className="btn btn-sm btn-outline-light">
                  Create account
                </Link>
              </>
            )}
          </div>
        </nav>
      </header>
      <main className="container py-4">
        <Outlet />
      </main>
    </>
  );
}
