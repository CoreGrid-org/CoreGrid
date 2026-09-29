import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import AssetSummary from "./AssetSummary";
import CreateAssetCategoryModal from "./CreateAssetCategoryModal";
import EditAssetCategoryModal from "./EditAssetCategoryModal";
import ConfirmDeleteModal from "./ConfirmDeleteModal";
import CreateAssetAttributeModal from "./CreateAssetAttributeModal";
import type { AssetCategory } from "../types/asset";

const { mutateCategoryMock, updateCategoryMock, createAttributeMock } = vi.hoisted(() => ({
  mutateCategoryMock: vi.fn(),
  updateCategoryMock: vi.fn(),
  createAttributeMock: vi.fn(),
}));

vi.mock("../hooks/useAssets", () => ({
  useCreateAssetCategory: () => ({
    mutate: mutateCategoryMock,
    isPending: false,
    isError: false,
    error: null,
  }),
  useUpdateAssetCategory: () => ({
    mutate: updateCategoryMock,
    isPending: false,
    isError: false,
    error: null,
  }),
  useCreateAssetAttributeDefinition: () => ({
    mutate: createAttributeMock,
    isPending: false,
    isError: false,
    error: null,
  }),
  useAssetCategories: () => ({
    data: [{ id: "cat-1", code: "IT", name: "Information Technology", is_active: true }],
    isLoading: false,
  }),
}));

describe("AssetSummary Component", () => {
  it("renders all label-value items passed in props", () => {
    const items = [
      { label: "Asset Code", value: "IT-LAP-001" },
      { label: "Category", value: "Information Technology" },
      { label: "Status", value: "Active" },
    ];

    render(<AssetSummary items={items} />);

    expect(screen.getByText("Asset Code")).toBeInTheDocument();
    expect(screen.getByText("IT-LAP-001")).toBeInTheDocument();
    expect(screen.getByText("Category")).toBeInTheDocument();
    expect(screen.getByText("Information Technology")).toBeInTheDocument();
    expect(screen.getByText("Status")).toBeInTheDocument();
    expect(screen.getByText("Active")).toBeInTheDocument();
  });
});

describe("CreateAssetCategoryModal Component", () => {
  it("renders modal heading and inputs", () => {
    const handleClose = vi.fn();
    const handleCreated = vi.fn();

    render(<CreateAssetCategoryModal onClose={handleClose} onCreated={handleCreated} />);

    expect(screen.getByText("New category")).toBeInTheDocument();
    expect(screen.getByLabelText(/Code/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Name/i)).toBeInTheDocument();
  });

  it("disables primary button when inputs are invalid/empty", () => {
    const handleClose = vi.fn();
    const handleCreated = vi.fn();

    render(<CreateAssetCategoryModal onClose={handleClose} onCreated={handleCreated} />);

    const createButton = screen.getByRole("button", { name: "Create category" });
    expect(createButton).toBeDisabled();
  });

  it("enables primary button and calls mutate on valid form submit", async () => {
    const user = userEvent.setup();
    const handleClose = vi.fn();
    const handleCreated = vi.fn();

    render(<CreateAssetCategoryModal onClose={handleClose} onCreated={handleCreated} />);

    const codeInput = screen.getByLabelText(/Code/i);
    const nameInput = screen.getByLabelText(/Name/i);
    const createButton = screen.getByRole("button", { name: "Create category" });

    await user.type(codeInput, "IT");
    await user.type(nameInput, "Information Technology");

    expect(createButton).not.toBeDisabled();

    await user.click(createButton);

    expect(mutateCategoryMock).toHaveBeenCalledWith(
      { code: "IT", name: "Information Technology" },
      expect.objectContaining({ onSuccess: handleCreated })
    );
  });
});

describe("EditAssetCategoryModal Component", () => {
  const mockCategory: AssetCategory = {
    id: "cat-1",
    code: "IT",
    name: "Information Tech",
    is_active: true,
    created_at: "2026-01-01T00:00:00Z",
  };

  it("populates inputs with existing category data", () => {
    render(
      <EditAssetCategoryModal
        category={mockCategory}
        onClose={vi.fn()}
        onUpdated={vi.fn()}
      />
    );

    expect(screen.getByDisplayValue("IT")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Information Tech")).toBeInTheDocument();
  });

  it("submits updated category code and name", async () => {
    const user = userEvent.setup();
    const handleUpdated = vi.fn();

    render(
      <EditAssetCategoryModal
        category={mockCategory}
        onClose={vi.fn()}
        onUpdated={handleUpdated}
      />
    );

    const nameInput = screen.getByLabelText(/Name/i);
    await user.clear(nameInput);
    await user.type(nameInput, "IT Equipment");

    const saveButton = screen.getByRole("button", { name: "Save changes" });
    await user.click(saveButton);

    expect(updateCategoryMock).toHaveBeenCalledWith(
      { id: "cat-1", payload: { code: "IT", name: "IT Equipment" } },
      expect.objectContaining({ onSuccess: handleUpdated })
    );
  });
});

describe("ConfirmDeleteModal Component", () => {
  it("renders confirmation heading and item name", () => {
    const handleConfirm = vi.fn();
    const handleClose = vi.fn();

    render(
      <ConfirmDeleteModal
        heading="Delete Category"
        itemName="IT Equipment"
        isPending={false}
        isError={false}
        error={null}
        onConfirm={handleConfirm}
        onClose={handleClose}
      />
    );

    expect(screen.getByText("Delete Category")).toBeInTheDocument();
    expect(screen.getByText("IT Equipment")).toBeInTheDocument();
  });

  it("triggers onConfirm when Delete button is clicked", async () => {
    const user = userEvent.setup();
    const handleConfirm = vi.fn();
    const handleClose = vi.fn();

    render(
      <ConfirmDeleteModal
        heading="Delete Category"
        itemName="IT Equipment"
        isPending={false}
        isError={false}
        error={null}
        onConfirm={handleConfirm}
        onClose={handleClose}
      />
    );

    const deleteBtn = screen.getByRole("button", { name: "Delete" });
    await user.click(deleteBtn);

    expect(handleConfirm).toHaveBeenCalledTimes(1);
  });
});

describe("CreateAssetAttributeModal Component", () => {
  it("renders new attribute modal with label and type dropdown", () => {
    render(
      <CreateAssetAttributeModal
        assetTypeId="type-1"
        assetTypeName="Laptop"
        onClose={vi.fn()}
        onCreated={vi.fn()}
      />
    );

    expect(screen.getByText("New attribute")).toBeInTheDocument();
    expect(screen.getByLabelText(/Field label/i)).toBeInTheDocument();
  });

  it("submits new attribute definition on valid input", async () => {
    const user = userEvent.setup();
    const handleCreated = vi.fn();

    render(
      <CreateAssetAttributeModal
        assetTypeId="type-1"
        assetTypeName="Laptop"
        onClose={vi.fn()}
        onCreated={handleCreated}
      />
    );

    const labelInput = screen.getByLabelText(/Field label/i);
    await user.type(labelInput, "RAM Size (GB)");

    const createBtn = screen.getByRole("button", { name: "Create attribute" });
    await user.click(createBtn);

    expect(createAttributeMock).toHaveBeenCalledWith(
      {
        assetTypeId: "type-1",
        payload: {
          name: "RAM Size (GB)",
          data_type: "TEXT",
          is_required: false,
          validation_rule: null,
          select_options: null,
          display_order: null,
        },
      },
      expect.objectContaining({ onSuccess: handleCreated })
    );
  });
});
