import React from "react";
import { render, screen } from "@testing-library/react";
import AdminLayout from "./AdminLayout";

// Mock window.location
const originalLocation = window.location;

beforeAll(() => {
  delete window.location;
  window.location = {
    ...originalLocation,
    pathname: "/dashboard",
    href: "",
  };
});

afterAll(() => {
  window.location = originalLocation;
});

describe("AdminLayout", () => {
  it("renders children correctly", () => {
    render(
      <AdminLayout>
        <div data-testid="child-content">Child Content</div>
      </AdminLayout>,
    );
    expect(screen.getByTestId("child-content")).toBeInTheDocument();
    expect(screen.getByText("Child Content")).toBeInTheDocument();
  });

  it("renders title and subtitle when provided", () => {
    const title = "Admin Dashboard";
    const subtitle = "Welcome to the admin panel";

    render(
      <AdminLayout title={title} subtitle={subtitle}>
        <div>Content</div>
      </AdminLayout>,
    );

    expect(screen.getByText(title)).toBeInTheDocument();
    expect(screen.getByText(subtitle)).toBeInTheDocument();
  });

  it("renders topActions when provided", () => {
    const topActions = <button data-testid="top-action">Save</button>;

    render(
      <AdminLayout topActions={topActions}>
        <div>Content</div>
      </AdminLayout>,
    );

    expect(screen.getByTestId("top-action")).toBeInTheDocument();
    expect(screen.getByText("Save")).toBeInTheDocument();
  });

  it("highlights the active navigation link based on current path", () => {
    // Current path is mocked to '/dashboard'
    render(
      <AdminLayout>
        <div>Content</div>
      </AdminLayout>,
    );

    // Find the dashboard link
    const dashboardLink = screen.getByText("Gestion des usagers").closest("a");
    expect(dashboardLink).toHaveClass("bg-cyan-400/20");
    expect(dashboardLink).toHaveClass("text-cyan-400");
    expect(dashboardLink).toHaveClass("border-cyan-400");

    // Find a non-active link
    const groupsLink = screen.getByText("Gestion des groupes").closest("a");
    expect(groupsLink).toHaveClass("text-slate-500");
    expect(groupsLink).not.toHaveClass("bg-cyan-400/20");
  });
});
