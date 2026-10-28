import { Link, Route, Routes } from "react-router";
import { Layout } from "@/components/Layout";
import { RequireAdmin } from "@/components/RequireAuth";
import { LoginPage, RegisterPage } from "@/pages/AuthPages";
import { BrowsePage } from "@/pages/BrowsePage";
import { MovieFormPage } from "@/pages/MovieFormPage";
import { MoviePage } from "@/pages/MoviePage";

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<BrowsePage />} />
        <Route path="movies/:id" element={<MoviePage />} />
        <Route
          path="movies/new"
          element={
            <RequireAdmin>
              <MovieFormPage key="new" />
            </RequireAdmin>
          }
        />
        <Route
          path="movies/:id/edit"
          element={
            <RequireAdmin>
              <MovieFormPage key="edit" />
            </RequireAdmin>
          }
        />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route
          path="*"
          element={
            <>
              <h1 className="h3">Page not found</h1>
              <Link to="/">Back to browsing</Link>
            </>
          }
        />
      </Route>
    </Routes>
  );
}
