import { useState } from "react";
import {
  ComposedModal,
  ModalHeader,
  ModalBody,
  ModalFooter,
  Button,
  InlineNotification,
  ComboBox,
  NumberInput,
  TextInput,
  TextArea,
  TileGroup,
  RadioTile,
} from "@carbon/react";
import { Money, Recycle, Gift, TrashCan } from "@carbon/icons-react";
import { useSubmitDisposal } from "../hooks/useDisposals";
import { useAssetDetail, useAssetsList } from "@/features/assets/hooks/useAssets";
import type { Asset } from "@/features/assets/types/asset";
import AssetSummary from "@/features/assets/components/AssetSummary";
import { localTodayIso } from "@/features/assets/utils/depreciation";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import type { DisposalMethod } from "../types";

const NOTES_MAX = 2000;
const lkr = new Intl.NumberFormat("en-LK", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const METHOD_OPTIONS: { value: DisposalMethod; title: string; description: string; icon: typeof Money }[] = [
  { value: "AUCTION", title: "Auction / public sale", description: "Sell to the highest bidder.", icon: Money },
  { value: "SCRAP", title: "Scrap / salvage", description: "Recover parts or material value.", icon: Recycle },
  { value: "DONATION", title: "Donation", description: "Give to another organisation.", icon: Gift },
  { value: "DESTROY", title: "Destruction / recycling", description: "Securely destroy or recycle.", icon: TrashCan },
];

// Provides the asset disposal submission form. Only CONDEMNED assets can be
// submitted — condemn one first from the Condemn asset form.
export default function SubmitDisposalModal({ onClose, onSubmitted }: { onClose: () => void; onSubmitted: () => void }) {
  const [assetId, setAssetId] = useState("");
  const [method, setMethod] = useState<DisposalMethod>("AUCTION");
  const [residualValue, setResidualValue] = useState<number | "">("");
  const [valuationDate, setValuationDate] = useState("");
  const [notes, setNotes] = useState("");
  const [submitted, setSubmitted] = useState(false);

  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const condemnedAssets = (assetsData?.items ?? []).filter((a) => a.status === "CONDEMNED");
  const selectedAsset = condemnedAssets.find((a) => a.id === assetId) ?? null;
  // Detail carries the server-calculated book value, offered as a starting estimate.
  const { data: assetDetail } = useAssetDetail(assetId || undefined);
  const bookValue = assetDetail?.id === assetId ? assetDetail.residual_value : undefined;

  const today = localTodayIso();
  const submitDisposal = useSubmitDisposal();

  const errors = {
    asset: !assetId ? "Choose the condemned asset to dispose of." : null,
    residual: residualValue === "" ? "Enter an estimated residual value (0 if it has none)." : residualValue < 0 ? "Value can't be negative." : null,
    valuationDate: valuationDate && valuationDate > today ? "Valuation date can't be in the future." : null,
  };

  const handleSubmit = () => {
    setSubmitted(true);
    if (Object.values(errors).some(Boolean) || residualValue === "" || submitDisposal.isPending) return;
    submitDisposal.mutate(
      {
        asset_id: assetId,
        disposal_method: method,
        estimated_residual_value: residualValue,
        valuation_date: valuationDate || null,
        notes: notes.trim() || null,
      },
      { onSuccess: onSubmitted },
    );
  };

  const handleClose = () => {
    if (!submitDisposal.isPending) onClose();
  };

  const noCandidates = !isLoadingAssets && condemnedAssets.length === 0;

  return (
    <ComposedModal open size="md" onClose={handleClose} preventCloseOnClickOutside aria-label="Submit disposal request">
      <ModalHeader label="Disposals" title="Submit disposal request" closeModal={handleClose} />
      <ModalBody hasScrollingContent>
        <div className="cg-modal-form">
          <p className="cg-modal-form__intro">
            Propose how a condemned asset should be disposed of. The request is checked and approved before anything
            happens to the asset.
          </p>

          {noCandidates && (
            <InlineNotification
              kind="info"
              title="No condemned assets"
              subtitle="Only condemned assets can be disposed of. Condemn an asset first, then come back here."
              lowContrast
              hideCloseButton
              style={{ maxWidth: "100%" }}
            />
          )}

          {submitDisposal.isError && (
            <InlineNotification
              kind="error"
              title="Could not submit disposal request"
              subtitle={getErrorMessage(submitDisposal.error, "Disposal submission failed.")}
              lowContrast
              hideCloseButton
              style={{ maxWidth: "100%" }}
            />
          )}

          <div>
            <ComboBox<Asset>
              id="disposal-asset-select"
              titleText="Condemned asset"
              placeholder={isLoadingAssets ? "Loading assets…" : "Type to search by code or name…"}
              disabled={isLoadingAssets || noCandidates}
              autoAlign
              items={condemnedAssets}
              itemToString={(item) => (item ? `${item.asset_code} - ${item.name}` : "")}
              selectedItem={selectedAsset}
              shouldFilterItem={comboBoxFilter(selectedAsset)}
              onChange={({ selectedItem }) => setAssetId(selectedItem?.id ?? "")}
              invalid={submitted && Boolean(errors.asset)}
              invalidText={errors.asset ?? undefined}
            />
            {selectedAsset && (
              <AssetSummary
                items={[
                  { label: "Type", value: selectedAsset.asset_type_name },
                  { label: "Department", value: selectedAsset.department_name },
                  { label: "Purchase cost (LKR)", value: lkr.format(selectedAsset.acquisition_cost) },
                  { label: "Book value (LKR)", value: bookValue !== undefined ? lkr.format(bookValue) : "…" },
                ]}
              />
            )}
          </div>

          <fieldset className="cg-modal-form__tiles">
            <legend className="cds--label">Proposed disposal method</legend>
            <TileGroup
              name="disposal-method"
              legend=""
              valueSelected={method}
              onChange={(value) => setMethod(value as DisposalMethod)}
            >
              {METHOD_OPTIONS.map(({ value, title, description, icon: Icon }) => (
                <RadioTile key={value} id={`disposal-method-${value}`} value={value}>
                  <span className="cg-modal-form__tile">
                    <Icon size={20} />
                    <span>
                      <span className="cg-modal-form__tile-title">{title}</span>
                      <span className="cg-modal-form__tile-text">{description}</span>
                    </span>
                  </span>
                </RadioTile>
              ))}
            </TileGroup>
          </fieldset>

          <div className="cg-modal-form__row">
            <div>
              <NumberInput
                id="disposal-residual-value"
                label="Estimated residual value (LKR)"
                min={0}
                hideSteppers
                allowEmpty
                value={residualValue}
                onChange={(_e, { value }) => setResidualValue(value === "" || value === undefined ? "" : Number(value))}
                invalid={submitted && Boolean(errors.residual)}
                invalidText={errors.residual ?? undefined}
              />
              {bookValue !== undefined && residualValue !== bookValue && (
                <Button kind="ghost" size="sm" className="cg-modal-form__inline-action" onClick={() => setResidualValue(bookValue)}>
                  Use book value ({lkr.format(bookValue)})
                </Button>
              )}
            </div>

            <TextInput
              id="disposal-valuation-date"
              labelText="Valuation date (optional)"
              type="date"
              max={today}
              value={valuationDate}
              onChange={(e) => setValuationDate(e.target.value)}
              invalid={submitted && Boolean(errors.valuationDate)}
              invalidText={errors.valuationDate ?? undefined}
            />
          </div>

          <TextArea
            id="disposal-notes"
            labelText="Notes (optional)"
            placeholder="e.g. Disposal committee meeting 12 Sep - two quotes received from licensed scrap dealers."
            rows={3}
            enableCounter
            maxCount={NOTES_MAX}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </div>
      </ModalBody>
      <ModalFooter>
        <Button kind="secondary" onClick={handleClose} disabled={submitDisposal.isPending}>
          Cancel
        </Button>
        <Button kind="primary" onClick={handleSubmit} disabled={submitDisposal.isPending || noCandidates}>
          {submitDisposal.isPending ? "Submitting…" : "Submit for disposal"}
        </Button>
      </ModalFooter>
    </ComposedModal>
  );
}
