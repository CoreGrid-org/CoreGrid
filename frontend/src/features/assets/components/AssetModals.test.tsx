import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import CreateAssetTypeModal from "./CreateAssetTypeModal";
import EditAssetTypeModal from "./EditAssetTypeModal";
import EditAssetAttributeModal from "./EditAssetAttributeModal";
import AssetDetailModal from "./AssetDetailModal";
import CategoryDetailModal from "./CategoryDetailModal";
import AssetHistoryModal from "./AssetHistoryModal";
import type { AssetCategory, AssetType, AssetAttributeDefinition } from "../types/asset";

const {
  createAssetTypeMock,
  updateAssetTypeMock,
  updateAttributeMock,
  updateConditionMock,
} = vi.hoisted(() => ({
  createAssetTypeMock: vi.fn(),
  updateAssetTypeMock: vi.fn(),
  updateAttributeMock: vi.fn(),
  updateConditionMock: vi.fn(),
}));

vi.mock("../hooks/useAssets", () => ({
  useAssetCategories: () => ({
    data: [{ id: "cat-1", code: "IT", name: "Information Technology", is_active: true }],
    isLoading: false,
  }),
  useCreateAssetType: () => ({
    mutate: createAssetTypeMock,
    isPending: false,
    isError: false,
    error: null,
  }),
  useUpdateAssetType: () => ({
    mutate: updateAssetTypeMock,
    isPending: false,
    isError: false,
    error: null,
  }),
  useUpdateAssetAttributeDefinition: () => ({
    mutate: updateAttributeMock,
    isPending: false,
    isError: false,
    error: null,
  }),
  useAssetDetail: (assetId: string) => ({
    data: {
      id: assetId,
      asset_code: "IT-LAP-001",
      name: "Dell XPS 15",
      asset_type_id: "type-1",
      asset_type_name: "Laptop",
      department_id: "dept-1",
      department_name: "Engineering",
      location_id: "loc-1",
      location_name: "Floor 2",
      status: "ACTIVE",
      condition: "NEW",
      acquisition_date: "2024-01-01",
      acquisition_cost: 250000,
      residual_value: 150000,
      attributes: [],
      qr_payload: "QR-PAYLOAD-001",
    },
    isLoading: false,
    isError: false,
    refetch: vi.fn(),
  }),
  useAssetHistory: () => ({
    data: {
      items: [
        {
          id: "hist-1",
          asset_id: "asset-1",
          event_type: "CREATED",
          description: "Asset registered",
          previous_value: null,
          new_value: null,
          actor_user_id: "user-1",
          actor_email: "admin@test.local",
          created_at: "2024-01-01T10:00:00Z",
        },
      ],
      total_count: 1,
      page: 1,
      page_size: 10,
      total_pages: 1,
    },
    isLoading: false,
    isError: false,
    refetch: vi.fn(),
  }),
  useUpdateAssetCondition: () => ({
    mutate: updateConditionMock,
    isPending: false,
    isError: false,
  }),
  useAssetTypes: () => ({
    data: [{ id: "type-1", code: "LAP", name: "Laptop", asset_category_id: "cat-1", is_active: true }],
    isLoading: false,
  }),
  useAssetsList: () => ({
    data: { items: [], total_count: 0 },
    isLoading: false,
  }),
}));

vi.mock("@/features/auth/hooks/usePermissions", () => ({
  usePermissions: () => ({ can: () => true }),
}));

describe("CreateAssetTypeModal Component", () => {
  it("renders new asset type inputs and heading", () => {
    render(<CreateAssetTypeModal onClose={vi.fn()} onCreated={vi.fn()} />);

    expect(screen.getByText("New type")).toBeInTheDocument();
    expect(screen.getByLabelText(/Code/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Name/i)).toBeInTheDocument();
  });

  it("submits asset type creation payload", async () => {
    const user = userEvent.setup();
    const handleCreated = vi.fn();

    render(<CreateAssetTypeModal onClose={vi.fn()} onCreated={handleCreated} />);

    const codeInput = screen.getByLabelText(/Code/i);
    const nameInput = screen.getByLabelText(/Name/i);

    await user.type(codeInput, "LAP");
    await user.type(nameInput, "Laptop");

    // Select category IT
    const combo = screen.getByPlaceholderText("Search categories…");
    await user.click(combo);
    const option = await screen.findByText("Information Technology (IT)");
    await user.click(option);

    const createBtn = screen.getByRole("button", { name: "Create type" });
    await user.click(createBtn);

    expect(createAssetTypeMock).toHaveBeenCalledWith(
      {
        code: "LAP",
        name: "Laptop",
        asset_category_id: "cat-1",
        useful_life_years: 5,
        default_maintenance_interval_days: null,
      },
      expect.objectContaining({ onSuccess: handleCreated })
    );
  });
});

describe("EditAssetTypeModal Component", () => {
  const mockType: AssetType = {
    id: "type-1",
    code: "LAP",
    name: "Laptop",
    asset_category_id: "cat-1",
    category_name: "IT",
    category_code: "IT",
    useful_life_years: 5,
    default_maintenance_interval_days: 180,
    attribute_count: 0,
    is_active: true,
  };

  it("populates inputs with existing asset type details", () => {
    render(<EditAssetTypeModal assetType={mockType} onClose={vi.fn()} onUpdated={vi.fn()} />);

    expect(screen.getByDisplayValue("LAP")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Laptop")).toBeInTheDocument();
  });

  it("submits updated asset type payload", async () => {
    const user = userEvent.setup();
    const handleUpdated = vi.fn();

    render(<EditAssetTypeModal assetType={mockType} onClose={vi.fn()} onUpdated={handleUpdated} />);

    const nameInput = screen.getByLabelText(/Name/i);
    await user.clear(nameInput);
    await user.type(nameInput, "Notebook Computer");

    const saveBtn = screen.getByRole("button", { name: "Save changes" });
    await user.click(saveBtn);

    expect(updateAssetTypeMock).toHaveBeenCalledWith(
      {
        id: "type-1",
        payload: {
          code: "LAP",
          name: "Notebook Computer",
          asset_category_id: "cat-1",
          useful_life_years: 5,
          default_maintenance_interval_days: 180,
        },
      },
      expect.objectContaining({ onSuccess: handleUpdated })
    );
  });
});

describe("EditAssetAttributeModal Component", () => {
  const mockAttribute: AssetAttributeDefinition = {
    id: "attr-1",
    asset_type_id: "type-1",
    name: "RAM Size",
    data_type: "TEXT",
    is_required: true,
    validation_rule: "minLength:2,maxLength:10",
    select_options: null,
    display_order: 1,
    is_active: true,
  };

  it("populates existing attribute details and submits changes", async () => {
    const user = userEvent.setup();
    const handleUpdated = vi.fn();

    render(
      <EditAssetAttributeModal
        assetTypeId="type-1"
        assetTypeName="Laptop"
        attribute={mockAttribute}
        onClose={vi.fn()}
        onUpdated={handleUpdated}
      />
    );

    expect(screen.getByDisplayValue("RAM Size")).toBeInTheDocument();

    const labelInput = screen.getByLabelText(/Field label/i);
    await user.clear(labelInput);
    await user.type(labelInput, "System Memory");

    const saveBtn = screen.getByRole("button", { name: "Save changes" });
    await user.click(saveBtn);

    expect(updateAttributeMock).toHaveBeenCalledWith(
      {
        assetTypeId: "type-1",
        attributeId: "attr-1",
        payload: {
          name: "System Memory",
          data_type: "TEXT",
          is_required: true,
          validation_rule: "minLength:2,maxLength:10",
          select_options: null,
          display_order: 1,
        },
      },
      expect.objectContaining({ onSuccess: handleUpdated })
    );
  });
});

describe("AssetDetailModal Component", () => {
  it("renders asset details and information", () => {
    render(<AssetDetailModal assetId="asset-1" onClose={vi.fn()} onConditionUpdated={vi.fn()} />);

    expect(screen.getByText("Dell XPS 15")).toBeInTheDocument();
    expect(screen.getByText("IT-LAP-001")).toBeInTheDocument();
  });
});

describe("CategoryDetailModal Component", () => {
  const mockCategory: AssetCategory = {
    id: "cat-1",
    code: "IT",
    name: "Information Technology",
    type_count: 1,
    asset_count: 5,
    is_active: true,
  };

  it("renders category details tab and types", () => {
    render(<CategoryDetailModal category={mockCategory} onClose={vi.fn()} />);

    expect(screen.getByText("Information Technology")).toBeInTheDocument();
  });
});

describe("AssetHistoryModal Component", () => {
  it("renders history entries", () => {
    render(
      <AssetHistoryModal
        assetId="asset-1"
        assetName="Dell XPS 15"
        assetCode="IT-LAP-001"
        onClose={vi.fn()}
      />
    );

    expect(screen.getByText("Dell XPS 15 - history")).toBeInTheDocument();
    expect(screen.getByText("Asset registered")).toBeInTheDocument();
  });
});
