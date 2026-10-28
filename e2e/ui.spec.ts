import { test, expect, type Page } from "playwright/test";

async function signIn(page: Page, email: string, password: string) {
  await page.goto("/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
}

test.describe("CineVault", () => {
  test("guests can browse and search the catalog", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Browse movies" })).toBeVisible();
    await expect(page.getByText(/movies in \d+ ms/)).toBeVisible();

    await page.getByLabel("Title or cast").fill("toy story");
    await page.getByRole("button", { name: "Search" }).click();
    await expect(page.getByRole("link", { name: "Toy Story", exact: true })).toBeVisible();
    await expect(page).toHaveURL(/q=toy\+story/);
  });

  test("a new member can rate a movie", async ({ page }) => {
    const email = `fan${Date.now()}@example.com`;
    await page.goto("/register");
    await page.getByLabel("Display name").fill("Film Fan");
    await page.getByLabel("Email").fill(email);
    await page.getByLabel("Password").fill("FilmFan123");
    await page.getByRole("button", { name: "Create account" }).click();
    await expect(page.getByText("Film Fan")).toBeVisible();

    await page.getByLabel("Title or cast").fill("jumanji");
    await page.getByRole("button", { name: "Search" }).click();
    await page.getByRole("link", { name: "Jumanji", exact: true }).first().click();
    await page.getByRole("radio", { name: "4 stars" }).click();
    await expect(page.getByRole("radio", { name: "4 stars" })).toHaveAttribute("aria-checked", "true");
    await expect(page.getByRole("button", { name: "Remove my rating" })).toBeVisible();
  });

  test("an admin can add, edit, and delete a movie", async ({ page }) => {
    const title = `E2E Feature ${Date.now()}`;
    await signIn(page, "admin@cinevault.local", "Admin@1234");
    await expect(page.getByText("Admin", { exact: true })).toBeVisible();

    await page.getByRole("link", { name: "Add movie" }).click();
    await expect(page.getByRole("heading", { name: "Add a movie" })).toBeVisible();
    await page.getByLabel("Title", { exact: true }).fill(title);
    await page.getByLabel("Release date").fill("2024-05-01");
    await page.getByLabel("Cast").fill("Ann Example\nBen Example");
    await page.getByRole("button", { name: "Add movie" }).click();
    await expect(page.getByRole("heading", { name: title })).toBeVisible();
    await expect(page.getByText("Ann Example, Ben Example")).toBeVisible();

    await page.getByRole("link", { name: "Edit" }).click();
    await page.getByLabel("Overview").fill("Edited in the end-to-end suite.");
    await page.getByRole("button", { name: "Save changes" }).click();
    await expect(page.getByText("Edited in the end-to-end suite.")).toBeVisible();

    await page.getByRole("button", { name: "Delete" }).click();
    await page.getByRole("button", { name: "Delete movie" }).click();
    await expect(page.getByRole("heading", { name: "Browse movies" })).toBeVisible();
  });

  test("wrong credentials show an error", async ({ page }) => {
    await signIn(page, "admin@cinevault.local", "nope");
    await expect(page.getByRole("alert")).toHaveText("Wrong email or password.");
  });
});
