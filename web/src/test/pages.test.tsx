import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import App from "@/App";
import { writeSession } from "@/api/client";
import { AuthProvider } from "@/auth/AuthContext";
import { StarRating } from "@/components/StarRating";

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });
}

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider>
        <App />
      </AuthProvider>
    </MemoryRouter>,
  );
}

const page = {
  items: [{ id: 1, title: "The Matrix", releaseDate: "1999-03-31", cast: "Keanu Reeves", averageRating: 4.5, ratingCount: 2 }],
  total: 1,
  page: 1,
  pageSize: 20,
  totalPages: 1,
  elapsedMs: 3,
};

describe("BrowsePage", () => {
  it("lists movies and sends the search query", async () => {
    const fetch = vi.spyOn(globalThis, "fetch").mockImplementation(async () => json(page));
    renderAt("/");

    expect(await screen.findByRole("link", { name: "The Matrix" })).toBeInTheDocument();
    expect(screen.getByText("1 movie in 3 ms")).toBeInTheDocument();

    await userEvent.type(screen.getByLabelText("Title or cast"), "keanu");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));

    await waitFor(() => expect(String(fetch.mock.calls.at(-1)![0])).toContain("q=keanu"));
  });
});

describe("MoviePage", () => {
  it("asks guests to sign in before rating", async () => {
    vi.spyOn(globalThis, "fetch").mockImplementation(async () =>
      json({ id: 1, title: "The Matrix", releaseDate: "1999-03-31", overview: null, cast: ["Keanu Reeves"], averageRating: null, ratingCount: 0, myRating: null }),
    );
    renderAt("/movies/1");

    expect(await screen.findByRole("heading", { name: "The Matrix" })).toBeInTheDocument();
    expect(screen.getByText("No ratings yet")).toBeInTheDocument();
    expect(screen.getByRole("main").querySelector("a[href^='/login']")).toHaveAttribute("href", "/login?returnTo=/movies/1");
  });

  it("lets a signed-in user rate", async () => {
    writeSession({ token: "t", expiresAt: new Date(Date.now() + 60_000).toISOString() });
    const fetch = vi.spyOn(globalThis, "fetch").mockImplementation(async (input, init) => {
      const url = String(input);
      if (url.endsWith("/api/auth/me")) return json({ id: "u1", email: "a@b.c", displayName: "Ann", role: "Member" });
      if (init?.method === "PUT") return json({ averageRating: 4, ratingCount: 1, myRating: 4 });
      return json({ id: 1, title: "The Matrix", releaseDate: "1999-03-31", overview: null, cast: [], averageRating: null, ratingCount: 0, myRating: null });
    });
    renderAt("/movies/1");

    await userEvent.click(await screen.findByRole("radio", { name: "4 stars" }));

    await waitFor(() => expect(screen.getByText(/from 1 rating/)).toBeInTheDocument());
    const put = fetch.mock.calls.find(([, init]) => init?.method === "PUT")!;
    expect(put[1]!.body).toBe(JSON.stringify({ stars: 4 }));
  });
});

describe("Admin routes", () => {
  it("send guests to the sign-in page", async () => {
    vi.spyOn(globalThis, "fetch").mockImplementation(async () => json(page));
    renderAt("/movies/new");
    expect(await screen.findByRole("heading", { name: "Sign in" })).toBeInTheDocument();
  });
});

describe("StarRating", () => {
  it("marks the chosen star", () => {
    render(<StarRating value={3} onChange={() => {}} label="Your rating" />);
    expect(screen.getByRole("radio", { name: "3 stars" })).toHaveAttribute("aria-checked", "true");
  });
});
