import { useEffect, useState } from "react";
import { Modal, InlineNotification, Tag, Select, SelectItem, Button } from "@carbon/react";
import { useAssetDetail, useUpdateAssetCondition } from "../hooks/useAssets";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { formatAttributeValue, formatCurrency, formatDate } from "../utils/format";
import { ASSET_CONDITIONS, type AssetCondition } from "../types/asset";
import QrCode from "./QrCode";

interface AssetDetailModalProps {
  assetId: string;
  onClose: () => void;
  onConditionUpdated: () => void;
}

// GET /api/assets/{id} for the detail payload; condition changes go through
// the separate PATCH /api/assets/{id}/condition endpoint (status has no
// endpoint yet — every asset the API can produce is currently ACTIVE).
export default function AssetDetailModal({ assetId, onClose, onConditionUpdated }: AssetDetailModalProps) {
  const { data: asset, isLoading, isError, error, refetch } = useAssetDetail(assetId);
  const updateCondition = useUpdateAssetCondition();
  const [condition, setCondition] = useState<AssetCondition | undefined>(undefined);

  useEffect(() => {
    if (asset) setCondition(asset.condition);
  }, [asset]);

  const handleUpdateCondition = () => {
    if (!asset || !condition || condition === asset.condition || updateCondition.isPending) return;
    updateCondition.mutate(
      { id: asset.id, payload: { condition } },
      {
        onSuccess: () => {
          refetch();
          onConditionUpdated();
        },
      },
    );
  };

  return (
    <Modal
      open
      modalLabel="Asset"
      modalHeading={asset ? asset.name : "Asset detail"}
      passiveModal
      onRequestClose={onClose}
      size="md"
    >
      {isLoading && (
        <div className="cg-placeholder">
          <p>Loading asset…</p>
        </div>
      )}

      {isError && (
        <InlineNotification
          kind="error"
          title="Could not load asset"
          subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      {asset && (
        <>
          <div style={{ display: "flex", gap: "1.5rem", alignItems: "flex-start", flexWrap: "wrap", marginBottom: "1.5rem" }}>
            <div style={{ flex: "1 1 20rem" }}>
              <div className="cg-kv-grid cg-kv-grid--three">
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Asset code</p>
              <p className="cg-kv-item__value cg-table__mono">{asset.asset_code}</p>
            </div>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Type</p>
              <p className="cg-kv-item__value">{asset.asset_type_name}</p>
            </div>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Status</p>
              <Tag type={statusTagColor(asset.status)}>{formatStatusLabel(asset.status)}</Tag>
            </div>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Department</p>
              <p className="cg-kv-item__value">{asset.department_name}</p>
            </div>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Location</p>
              <p className="cg-kv-item__value">{asset.location_name}</p>
            </div>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Condition</p>
              <Tag type={statusTagColor(asset.condition)}>{formatStatusLabel(asset.condition)}</Tag>
            </div>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Acquisition date</p>
              <p className="cg-kv-item__value">{formatDate(asset.acquisition_date)}</p>
            </div>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Acquisition cost</p>
              <p className="cg-kv-item__value">{formatCurrency(asset.acquisition_cost)}</p>
            </div>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">Residual value</p>
              <p className="cg-kv-item__value">{formatCurrency(asset.residual_value)}</p>
            </div>
            <div className="cg-kv-item">
              <p className="cg-kv-item__label">QR payload</p>
              <p className="cg-kv-item__value cg-table__mono">{asset.qr_payload}</p>
            </div>
              </div>
            </div>
            <div style={{ flex: "none", textAlign: "center" }}>
              <QrCode value={`${window.location.origin}/assets/${asset.asset_code}`}size={140}/>
              
              <p style={{ margin: "0.5rem 0 0", fontSize: "0.75rem", color: "#8d8d8d" }}>Scan to look up this asset</p>
            </div>
          </div>

          {updateCondition.isError && (
            <InlineNotification
              kind="error"
              title="Could not update condition"
              subtitle={getErrorMessage(updateCondition.error, "Something went wrong. Please try again.")}
              lowContrast
              hideCloseButton
              style={{ marginBottom: "1rem", maxWidth: "100%" }}
            />
          )}

          <div style={{ display: "flex", alignItems: "flex-end", gap: "0.75rem", marginBottom: "1.5rem" }}>
            <div style={{ minWidth: "12rem" }}>
              <Select
                id="asset-condition"
                labelText="Update condition"
                value={condition}
                onChange={(e) => setCondition(e.target.value as AssetCondition)}
              >
                {ASSET_CONDITIONS.map((c) => (
                  <SelectItem key={c} value={c} text={formatStatusLabel(c)} />
                ))}
              </Select>
            </div>
            <Button
              size="md"
              disabled={!condition || condition === asset.condition || updateCondition.isPending}
              onClick={handleUpdateCondition}
            >
              {updateCondition.isPending ? "Saving…" : "Save condition"}
            </Button>
          </div>

          <p className="cg-section__title">Attributes</p>
          {asset.attributes.length > 0 ? (
            <table className="cg-table cg-table--no-hover">
              <thead>
                <tr>
                  <th>Attribute</th>
                  <th>Value</th>
                  <th>Required</th>
                </tr>
              </thead>
              <tbody>
                {asset.attributes.map((attr) => (
                  <tr key={attr.attribute_definition_id}>
                    <td>{attr.name}</td>
                    <td>{formatAttributeValue(attr)}</td>
                    <td className="cg-table__muted">{attr.is_required ? "Required" : "Optional"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p className="cg-table__muted">No custom attributes recorded for this asset.</p>
          )}
        </>
      )}
    </Modal>
  );
}
