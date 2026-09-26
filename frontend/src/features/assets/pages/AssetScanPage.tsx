import { useRef, useState } from "react";
import { Button, TextInput, InlineNotification, Tag } from "@carbon/react";
import { ArrowRight, QrCode, Launch, Reset } from "@carbon/icons-react";
import { useAssetByQrCode } from "../hooks/useAssets";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import AssetDetailModal from "../components/AssetDetailModal";

// GET /api/assets/qr/{code} — a scanned or typed code resolves straight to
// the asset. The input keeps focus so a handheld/USB scanner (which types
// the code followed by Enter) works without clicking anything.

const MAX_RECENT = 5;
const lkrFormatter = new Intl.NumberFormat("en-LK", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export default function AssetScanPage() {
  const [code, setCode] = useState("");
  const [recentCodes, setRecentCodes] = useState<string[]>([]);
  const [openAssetId, setOpenAssetId] = useState<string | undefined>(undefined);
  const inputRef = useRef<HTMLInputElement>(null);
  const lookup = useAssetByQrCode();

  const resolve = (value: string) => {
    const trimmed = value.trim();
    if (!trimmed || lookup.isPending) return;
    setCode(trimmed);
    lookup.mutate(trimmed, {
      onSuccess: (asset) => {
        setRecentCodes((prev) => [asset.asset_code, ...prev.filter((c) => c !== asset.asset_code)].slice(0, MAX_RECENT));
        inputRef.current?.select();
      },
    });
  };

  const reset = () => {
    setCode("");
    lookup.reset();
    inputRef.current?.focus();
  };

  const asset = lookup.data;

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Scan QR</h1>
          <p className="cg-page__subtitle">Look up an asset by its QR code or asset code.</p>
        </div>
      </div>

      <div className="cg-asset-scan">
        <section className="cg-section cg-asset-scan__card">
          <div className="cg-section__body cg-asset-scan__lookup">
            <div className="cg-asset-scan__icon" aria-hidden="true">
              <QrCode size={32} />
            </div>
            <div className="cg-asset-scan__form">
              <TextInput
                id="asset-scan-code"
                ref={inputRef}
                labelText="QR or asset code"
                placeholder="e.g. ORG-LAP-0142"
                size="lg"
                autoFocus
                autoComplete="off"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") resolve(code);
                }}
                helperText="Scan with a handheld scanner or type the code, then press Enter."
              />
              <Button
                size="lg"
                renderIcon={ArrowRight}
                disabled={!code.trim() || lookup.isPending}
                onClick={() => resolve(code)}
              >
                {lookup.isPending ? "Looking up…" : "Look up"}
              </Button>
            </div>

            {recentCodes.length > 0 && (
              <div className="cg-asset-scan__recent">
                <span className="cg-asset-scan__recent-label">Recent</span>
                {recentCodes.map((recent) => (
                  <button key={recent} type="button" className="cg-asset-scan__chip" onClick={() => resolve(recent)}>
                    {recent}
                  </button>
                ))}
              </div>
            )}
          </div>
        </section>

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

        {asset && (
          <section className="cg-section cg-asset-scan__card cg-asset-scan__result">
            <header className="cg-section__header cg-asset-scan__result-header">
              <div>
                <p className="cg-asset-scan__result-name">{asset.name}</p>
                <p className="cg-asset-scan__result-code">{asset.asset_code}</p>
              </div>
              <Tag type={statusTagColor(asset.status)}>{formatStatusLabel(asset.status)}</Tag>
            </header>

            <dl className="cg-section__body cg-asset-scan__details">
              <div>
                <dt>Type</dt>
                <dd>{asset.asset_type_name}</dd>
              </div>
              <div>
                <dt>Department</dt>
                <dd>{asset.department_name}</dd>
              </div>
              <div>
                <dt>Location</dt>
                <dd>{asset.location_name}</dd>
              </div>
              <div>
                <dt>Condition</dt>
                <dd>
                  <Tag type={statusTagColor(asset.condition)} size="sm">{formatStatusLabel(asset.condition)}</Tag>
                </dd>
              </div>
              <div>
                <dt>Purchase date</dt>
                <dd>{asset.acquisition_date.slice(0, 10)}</dd>
              </div>
              <div>
                <dt>Residual value (LKR)</dt>
                <dd>{lkrFormatter.format(asset.residual_value)}</dd>
              </div>
            </dl>

            <div className="cg-asset-scan__actions">
              <Button kind="ghost" renderIcon={Reset} onClick={reset}>
                Look up another
              </Button>
              <Button kind="primary" renderIcon={Launch} onClick={() => setOpenAssetId(asset.id)}>
                Open asset detail
              </Button>
            </div>
          </section>
        )}
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
