import { useEffect, useState, type RefObject } from "react";
import {
  ComposedModal,
  ModalHeader,
  ModalBody,
  ModalFooter,
  TextArea,
  Button,
  InlineNotification,
  FileUploaderDropContainer,
  ComboBox,
  TileGroup,
  RadioTile,
  Tag,
  InlineLoading,
} from "@carbon/react";
import { Close, WarningAlt, ErrorOutline, Image as ImageIcon, Information } from "@carbon/icons-react";
import { useReportFault, useUploadMaintenancePhoto } from "../hooks/useMaintenance";
import type { MaintenanceRecord } from "../types/maintenance";
import { useAssetsList } from "@/features/assets/hooks/useAssets";
import type { Asset } from "@/features/assets/types/asset";
import AssetSummary from "@/features/assets/components/AssetSummary";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { comboBoxFilter } from "@/shared/lib/comboBoxFilter";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";

// Matches ReportFaultRequest's [MaxLength(2000)] on the backend.
const DESCRIPTION_MAX = 2000;
const PHOTO_MAX_BYTES = 5 * 1024 * 1024;

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
  const uploadPhoto = useUploadMaintenancePhoto();

  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const assets = assetsData?.items || [];

  const [assetId, setAssetId] = useState("");
  const [description, setDescription] = useState("");
  const [observedCondition, setCondition] = useState<FaultCondition | "">("");
  const [photoUrl, setPhotoUrl] = useState<string | null>(null);
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [photoPreview, setPhotoPreview] = useState<string | null>(null);
  const [photoError, setPhotoError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);

  const selectedAsset = assets.find((a) => a.id === assetId) ?? null;
  const isBusy = reportFault.isPending || uploadPhoto.isPending;

  // Free the local preview's object URL when it's replaced or the modal closes.
  useEffect(() => {
    return () => {
      if (photoPreview) URL.revokeObjectURL(photoPreview);
    };
  }, [photoPreview]);

  const handlePhotoAdded = (file: File | undefined) => {
    if (!file) return;
    if (file.size > PHOTO_MAX_BYTES) {
      setPhotoError("That photo is larger than 5 MB. Choose a smaller one.");
      return;
    }
    setPhotoError(null);
    setPhotoFile(file);
    setPhotoPreview(URL.createObjectURL(file));
    setPhotoUrl(null);
    uploadPhoto.mutate(file, {
      onSuccess: (url) => setPhotoUrl(url),
    });
  };

  const handlePhotoRemove = () => {
    setPhotoFile(null);
    setPhotoPreview(null);
    setPhotoUrl(null);
    setPhotoError(null);
  };

  const errors = {
    asset: !assetId ? "Choose the asset that has the fault." : null,
    description: !description.trim() ? "Describe what's wrong." : null,
    condition: !observedCondition ? "Choose how bad the fault is." : null,
  };
  const hasErrors = Object.values(errors).some(Boolean);

  const handleSubmit = () => {
    setSubmitted(true);
    if (hasErrors || !observedCondition || isBusy) return;
    reportFault.mutate(
      {
        asset_id: assetId,
        description: description.trim(),
        observed_condition: observedCondition,
        photo_url: photoUrl ?? undefined,
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
              style={{ maxWidth: "100%" }}
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

          <div className="cg-fault-modal__field">
            <p className="cds--label">
              Photo <span className="cg-fault-modal__optional">(optional)</span>
            </p>
            {photoFile && photoPreview ? (
              <div className="cg-fault-modal__photo">
                <img src={photoPreview} alt="Attached fault photo" />
                <div className="cg-fault-modal__photo-meta">
                  <p className="cg-fault-modal__photo-name">{photoFile.name}</p>
                  <p className="cg-fault-modal__photo-size">{(photoFile.size / 1024 / 1024).toFixed(2)} MB</p>
                  {uploadPhoto.isPending && <InlineLoading description="Uploading…" />}
                  {!uploadPhoto.isPending && photoUrl && <InlineLoading status="finished" description="Uploaded" />}
                  {uploadPhoto.isError && (
                    <p className="cg-fault-modal__error">
                      {getErrorMessage(uploadPhoto.error, "Photo upload failed. You can still submit without one.")}
                    </p>
                  )}
                </div>
                <Button
                  kind="ghost"
                  size="sm"
                  hasIconOnly
                  renderIcon={Close}
                  iconDescription="Remove photo"
                  tooltipPosition="left"
                  onClick={handlePhotoRemove}
                  disabled={uploadPhoto.isPending}
                />
              </div>
            ) : (
              <>
                <div className="cg-fault-modal__drop">
                  <ImageIcon size={24} />
                  <FileUploaderDropContainer
                    labelText="Drag a photo here or click to choose one"
                    accept={[".jpg", ".jpeg", ".png", ".webp"]}
                    multiple={false}
                    onAddFiles={(_event, { addedFiles }) => handlePhotoAdded(addedFiles[0])}
                  />
                  <span className="cg-fault-modal__drop-hint">JPEG, PNG or WebP, up to 5 MB</span>
                </div>
                {photoError && <p className="cg-fault-modal__error">{photoError}</p>}
              </>
            )}
          </div>

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
        <Button kind="primary" onClick={handleSubmit} disabled={isBusy}>
          {reportFault.isPending ? "Submitting…" : uploadPhoto.isPending ? "Uploading photo…" : "Report fault"}
        </Button>
      </ModalFooter>
    </ComposedModal>
  );
}
