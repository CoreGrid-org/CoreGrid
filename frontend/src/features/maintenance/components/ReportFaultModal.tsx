import { useState, type RefObject } from "react";
import {
  ComposedModal,
  ModalHeader,
  ModalBody,
  ModalFooter,
  TextArea,
  Button,
  InlineNotification,
  ComboBox,
  TileGroup,
  RadioTile,
  Tag,
} from "@carbon/react";
import { WarningAlt, ErrorOutline, Information } from "@carbon/icons-react";
import { useReportFault } from "../hooks/useMaintenance";
import MaintenancePhotoField from "./MaintenancePhotoField";
import type { MaintenanceRecord } from "../types/maintenance";
import { useAssetsList } from "@/features/assets/hooks/useAssets";
import type { Asset } from "@/features/assets/types/asset";
import AssetSummary from "@/features/assets/components/AssetSummary";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";

// Matches ReportFaultRequest's [MaxLength(2000)] on the backend.
const DESCRIPTION_MAX = 2000;

type FaultCondition = "POOR" | "UNSERVICEABLE";

const CONDITION_OPTIONS: { value: FaultCondition; title: string; description: string; icon: typeof WarningAlt }[] = [
  {
    value: "POOR",
    title: "Needs repair",
    description: "Still usable, but damaged or not working properly.",
    icon: WarningAlt,
  },
  {
    value: "UNSERVICEABLE",
    title: "Unserviceable",
    description: "Broken or unsafe - can't be used until it's fixed.",
    icon: ErrorOutline,
  },
];

interface ReportFaultModalProps {
  onClose: () => void;
  onReported: (record: MaintenanceRecord) => void;
  launcherButtonRef?: RefObject<HTMLButtonElement | null>;
}

// POST /api/maintenance/faults. Mount it only while it's open — every open
// then starts from a blank form without any manual reset.
export default function ReportFaultModal({ onClose, onReported, launcherButtonRef }: ReportFaultModalProps) {
  const reportFault = useReportFault();

  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const assets = assetsData?.items || [];

  const [assetId, setAssetId] = useState("");
  const [description, setDescription] = useState("");
  const [observedCondition, setCondition] = useState<FaultCondition | "">("");
  const [photo, setPhoto] = useState<File | null>(null);
  const [submitted, setSubmitted] = useState(false);

  const selectedAsset = assets.find((a) => a.id === assetId) ?? null;
  const errors = {
    asset: !assetId ? "Choose the asset that has the fault." : null,
    description: !description.trim() ? "Describe what's wrong." : null,
    condition: !observedCondition ? "Choose how bad the fault is." : null,
  };
  const hasErrors = Object.values(errors).some(Boolean);

  const handleSubmit = () => {
    setSubmitted(true);
    if (hasErrors || !observedCondition || reportFault.isPending) return;
    reportFault.mutate(
      {
        payload: {
          asset_id: assetId,
          description: description.trim(),
          observed_condition: observedCondition,
        },
        photo,
      },
      { onSuccess: onReported },
    );
  };

  const handleClose = () => {
    if (reportFault.isPending) return;
    onClose();
  };

  return (
    <ComposedModal
      open
      size="md"
      onClose={handleClose}
      preventCloseOnClickOutside
      launcherButtonRef={launcherButtonRef}
      aria-label="Report a fault"
      className="cg-fault-modal"
    >
      <ModalHeader
        label="Maintenance"
        title="Report a fault"
        closeModal={handleClose}
      />
      <ModalBody hasScrollingContent>
        <form
          className="cg-fault-modal__form"
          noValidate
          onSubmit={(e) => {
            e.preventDefault();
            handleSubmit();
          }}
        >
          <p className="cg-fault-modal__intro">
            Tell us which asset has a problem and what's wrong. It becomes a maintenance request for review.
          </p>

          {reportFault.isError && (
            <InlineNotification
              kind="error"
              title="Could not report the fault"
              subtitle={getErrorMessage(reportFault.error, "Something went wrong. Please try again.")}
              lowContrast
              hideCloseButton
            />
          )}

          <div className="cg-fault-modal__field">
            <ComboBox<Asset>
              id="report-fault-asset"
              titleText="Asset"
              helperText={selectedAsset ? undefined : "Search by asset code or name."}
              placeholder={isLoadingAssets ? "Loading assets…" : "Type to search assets…"}
              disabled={isLoadingAssets}
              autoAlign
              items={assets}
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
                  { label: "Location", value: selectedAsset.location_name },
                  { label: "Department", value: selectedAsset.department_name },
                  {
                    label: "Recorded condition",
                    value: (
                      <Tag type={statusTagColor(selectedAsset.condition)} size="sm">
                        {formatStatusLabel(selectedAsset.condition)}
                      </Tag>
                    ),
                  },
                ]}
              />
            )}
          </div>

          <fieldset className="cg-fault-modal__field cg-fault-modal__severity">
            <legend className="cds--label">How bad is it?</legend>
            <TileGroup
              name="report-fault-condition"
              legend=""
              valueSelected={observedCondition}
              onChange={(value) => setCondition(value as FaultCondition)}
            >
              {CONDITION_OPTIONS.map(({ value, title, description: optionDescription, icon: Icon }) => (
                <RadioTile key={value} id={`report-fault-condition-${value}`} value={value}>
                  <span className={`cg-fault-modal__tile cg-fault-modal__tile--${value.toLowerCase()}`}>
                    <Icon size={20} />
                    <span>
                      <span className="cg-fault-modal__tile-title">{title}</span>
                      <span className="cg-fault-modal__tile-text">{optionDescription}</span>
                    </span>
                  </span>
                </RadioTile>
              ))}
            </TileGroup>
            {submitted && errors.condition && <p className="cg-fault-modal__error">{errors.condition}</p>}
          </fieldset>

          <div className="cg-fault-modal__field">
            <TextArea
              id="report-fault-description"
              labelText="What's wrong?"
              placeholder="e.g. Screen flickers and goes black after about 10 minutes of use. Started yesterday after the power cut."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={4}
              enableCounter
              maxCount={DESCRIPTION_MAX}
              invalid={submitted && Boolean(errors.description)}
              invalidText={errors.description ?? undefined}
            />
          </div>

          <MaintenancePhotoField file={photo} onChange={setPhoto} disabled={reportFault.isPending} />

          <p className="cg-fault-modal__next">
            <Information size={16} />
            After you submit, the request is reviewed and approved for repair. You can follow it in Maintenance records.
          </p>
        </form>
      </ModalBody>
      <ModalFooter>
        <Button kind="secondary" onClick={handleClose} disabled={reportFault.isPending}>
          Cancel
        </Button>
        <Button kind="primary" onClick={handleSubmit} disabled={reportFault.isPending}>
          {reportFault.isPending ? (photo ? "Uploading & submitting…" : "Submitting…") : "Report fault"}
        </Button>
      </ModalFooter>
    </ComposedModal>
  );
}
