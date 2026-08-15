import { useState } from "react";
import { Button, TextInput, InlineNotification, Tag } from "@carbon/react";
import { QrCode } from "@carbon/icons-react";
import { useAssetByQrCode } from "../hooks/useAssets";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import AssetDetailModal from "../components/AssetDetailModal";

// GET /api/assets/qr/{code} — a scanned or typed code resolves straight to
// the asset. Codes belonging to another organisation come back as a plain
// 404, same as a code that doesn't exist at all (never leaks cross-org data).
export default function AssetScanPage() {
  const [code, setCode] = useState("");
  const lookup = useAssetByQrCode();
  const [openAssetId, setOpenAssetId] = useState<string | undefined>(undefined);

  const handleResolve = () => {
    const trimmed = code.trim();
    if (!trimmed || lookup.isPending) return;
    lookup.mutate(trimmed);
  };

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Scan QR</h1>
          <p className="cg-page__subtitle">Look up an asset by its QR code or asset code.</p>
        </div>
      </div>

      <div style={{ display: "flex", gap: "1.5rem", alignItems: "flex-start", flexWrap: "wrap" }}>
        <div className="cg-section" style={{ width: "22rem", flex: "none", margin: 0 }}>
          <TextInput
            id="asset-scan-code"
            labelText="Asset code"
            placeholder="e.g. ORG-LAP-0142"
            value={code}
            onChange={(e) => setCode(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") handleResolve();
            }}
          />
          <Button
            renderIcon={QrCode}
            style={{ width: "100%", marginTop: "1rem" }}
            disabled={!code.trim() || lookup.isPending}
            onClick={handleResolve}
          >
            {lookup.isPending ? "Resolving…" : "Resolve code"}
          </Button>
        </div>

        <div style={{ flex: 1, minWidth: "20rem" }}>
          {!lookup.data && !lookup.isError && (
            <div className="cg-placeholder">
              <p>Awaiting a scan or a typed code. The camera scan flow is the Flutter app; this is the web fallback.</p>
            </div>
          )}

          {lookup.isError && (
            <InlineNotification
              kind="error"
              title="No asset for this code"
              subtitle={getErrorMessage(
                lookup.error,
                "Either the code doesn't exist, or it belongs to another organisation.",
              )}
              lowContrast
              hideCloseButton
              style={{ maxWidth: "100%" }}
            />
          )}

          {lookup.data && (
            <div className="cg-section" style={{ margin: 0, borderLeft: "3px solid #24a148" }}>
              <p className="cg-section__title" style={{ margin: 0 }}>{lookup.data.name}</p>
              <p className="cg-table__mono cg-table__muted" style={{ margin: "0.25rem 0 0" }}>{lookup.data.asset_code}</p>
              <div className="cg-kv-grid cg-kv-grid--three" style={{ marginTop: "1rem" }}>
                <div className="cg-kv-item">
                  <p className="cg-kv-item__label">Type</p>
                  <p className="cg-kv-item__value">{lookup.data.asset_type_name}</p>
                </div>
                <div className="cg-kv-item">
                  <p className="cg-kv-item__label">Location</p>
                  <p className="cg-kv-item__value">{lookup.data.location_name}</p>
                </div>
                <div className="cg-kv-item">
                  <p className="cg-kv-item__label">Condition</p>
                  <div className="cg-kv-item__value">
                    <Tag type={statusTagColor(lookup.data.condition)}>{formatStatusLabel(lookup.data.condition)}</Tag>
                  </div>
                </div>
              </div>
              <Button kind="tertiary" style={{ marginTop: "1.25rem" }} onClick={() => setOpenAssetId(lookup.data!.id)}>
                Open asset detail
              </Button>
            </div>
          )}
        </div>
      </div>

      {openAssetId && (
        <AssetDetailModal
          assetId={openAssetId}
          onClose={() => setOpenAssetId(undefined)}
          onConditionUpdated={() => {}}
        />
      )}
    </div>
  );
}
