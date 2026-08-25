import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { GalleryModals } from "./Gallery";

vi.mock("../components/UploadPhoto", () => ({
  default: () => <input aria-label="upload-title" />,
}));
vi.mock("../components/ImageModal", () => ({ default: () => null }));
vi.mock("../components/InviteModal", () => ({ default: () => null }));
vi.mock("../components/GroupRequestModal", () => ({ default: () => null }));

const t = (key, fallback) => fallback || key;

function renderUploadModal(overrides = {}) {
  const setIsUploadOpen = vi.fn();
  render(
    <GalleryModals
      isUploadOpen
      setIsUploadOpen={setIsUploadOpen}
      activeGroupId={1}
      onUploadSuccess={vi.fn()}
      selectedPhotoIndex={null}
      setSelectedPhotoIndex={vi.fn()}
      filteredPhotos={[]}
      setSelectedTag={vi.fn()}
      setSelectedAuthor={vi.fn()}
      isInviteOpen={false}
      setIsInviteOpen={vi.fn()}
      isGroupRequestOpen={false}
      setIsGroupRequestOpen={vi.fn()}
      t={t}
      {...overrides}
    />,
  );
  return { setIsUploadOpen };
}

function getBackdropButton() {
  const dialog = screen.getByRole("dialog", { name: "Upload a photo" });
  return dialog.querySelector("button.fixed.inset-0");
}

describe("GalleryModals upload overlay", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("closes when the backdrop button is clicked", () => {
    const { setIsUploadOpen } = renderUploadModal();
    fireEvent.click(getBackdropButton());
    expect(setIsUploadOpen).toHaveBeenCalledWith(false);
  });

  it("does not close when the panel contents are clicked", () => {
    const { setIsUploadOpen } = renderUploadModal();
    fireEvent.click(screen.getByLabelText("upload-title"));
    expect(setIsUploadOpen).not.toHaveBeenCalled();
  });

  it("closes on Escape when focus is inside the upload form", () => {
    const { setIsUploadOpen } = renderUploadModal();
    const input = screen.getByLabelText("upload-title");
    input.focus();
    fireEvent.keyDown(input, { key: "Escape", bubbles: true });
    expect(setIsUploadOpen).toHaveBeenCalledWith(false);
  });

  it("does not render role=document or overlay keyboard shims", () => {
    renderUploadModal();
    expect(document.querySelector('[role="document"]')).toBeNull();
    const dialog = screen.getByRole("dialog", { name: "Upload a photo" });
    expect(dialog.getAttribute("tabIndex")).toBeNull();
  });
});
