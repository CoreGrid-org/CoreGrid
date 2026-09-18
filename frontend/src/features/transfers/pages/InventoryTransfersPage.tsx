import { useState } from "react";
import {
  Tabs,
  TabList,
  Tab,
  TabPanels,
  TabPanel,
  Tag,
  Button,
  InlineNotification,
  Modal,
  ComboBox,
  Select,
  SelectItem,
  NumberInput,
  TextInput,
  TextArea,
  Pagination,
} from "@carbon/react";
import {
  Add,
  DeliveryTruck,
  Warning,
  Checkmark,
} from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import {
  useTransfersList,
  useInitiateTransfer,
  useConfirmTransferReceipt,
} from "../hooks/useTransfers";
import {
  useDisposalsList,
  useCondemnAsset,
  useSubmitDisposal,
} from "../hooks/useDisposals";
import {
  useAssetsList,
  useDepartments,
  useLocations,
} from "@/features/assets/hooks/useAssets";
import type { DisposalMethod, TransferResponse } from "../types";

export default function InventoryTransfersPage() {
  const [transferPage, setTransferPage] = useState(1);
  const [transferPageSize, setTransferPageSize] = useState(20);
  const [disposalPage, setDisposalPage] = useState(1);
  const [disposalPageSize, setDisposalPageSize] = useState(20);

  const {
    data: transfers,
    isLoading: isLoadingTransfers,
    isError: isErrorTransfers,
    error: transfersError,
    refetch: refetchTransfers,
  } = useTransfersList({ page: transferPage, pageSize: transferPageSize });

  const {
    data: disposals,
    isLoading: isLoadingDisposals,
    isError: isErrorDisposals,
    error: disposalsError,
    refetch: refetchDisposals,
  } = useDisposalsList({ page: disposalPage, pageSize: disposalPageSize });

  // Modal States
  const [isTransferModalOpen, setIsTransferModalOpen] = useState(false);
  const [isCondemnModalOpen, setIsCondemnModalOpen] = useState(false);
  const [isDisposalModalOpen, setIsDisposalModalOpen] = useState(false);

  // Form State: Initiate Transfer (FR-043/044)
  const [transferAssetId, setTransferAssetId] = useState("");
  const [transferDeptId, setTransferDeptId] = useState("");
  const [transferLocId, setTransferLocId] = useState("");
  const [transferFormError, setTransferFormError] = useState<string | null>(null);

  // Reference lookups for forms
  const { data: assetsData, isLoading: isLoadingAssets } = useAssetsList({ pageSize: 100 });
  const assets = assetsData?.items || [];
  const departments = useDepartments();
  const locations = useLocations(transferDeptId || undefined);

  // Mutations
  const initiateTransfer = useInitiateTransfer();
  const confirmReceipt = useConfirmTransferReceipt();
  const condemnAsset = useCondemnAsset();
  const submitDisposal = useSubmitDisposal();

  // Form State: Condemn Asset (FR-049)
  const [condemnAssetId, setCondemnAssetId] = useState("");
  const [condemnReason, setCondemnReason] = useState("");
  const [condemnEvidenceUrl, setCondemnEvidenceUrl] = useState("");
  const [condemnFormError, setCondemnFormError] = useState<string | null>(null);

  // Form State: Submit Disposal (FR-050)
  const [disposalAssetId, setDisposalAssetId] = useState("");
  const [disposalMethod, setDisposalMethod] = useState<DisposalMethod>("AUCTION");
  const [residualValue, setResidualValue] = useState<number>(0);
  const [valuationDate, setValuationDate] = useState<string>("");
  const [disposalNotes, setDisposalNotes] = useState("");
  const [disposalFormError, setDisposalFormError] = useState<string | null>(null);

  // Handlers
  const handleInitiateTransferSubmit = () => {
    if (!transferAssetId || !transferDeptId || !transferLocId) {
      setTransferFormError("Please select an asset, destination department, and destination location.");
      return;
    }
    setTransferFormError(null);
    initiateTransfer.mutate(
      {
        asset_id: transferAssetId,
        to_department_id: transferDeptId,
        to_location_id: transferLocId,
      },
      {
        onSuccess: () => {
          setIsTransferModalOpen(false);
          setTransferAssetId("");
          setTransferDeptId("");
          setTransferLocId("");
          refetchTransfers();
        },
      }
    );
  };

  const handleConfirmReceipt = (transfer: TransferResponse) => {
    confirmReceipt.mutate(transfer.id, {
      onSuccess: () => refetchTransfers(),
    });
  };

  const handleCondemnSubmit = () => {
    if (!condemnAssetId || !condemnReason.trim()) {
      setCondemnFormError("Please select an asset and specify the condemnation reason.");
      return;
    }
    setCondemnFormError(null);
    condemnAsset.mutate(
      {
        assetId: condemnAssetId,
        payload: {
          reason: condemnReason.trim(),
          evidence_url: condemnEvidenceUrl.trim() || undefined,
        },
      },
      {
        onSuccess: () => {
          setIsCondemnModalOpen(false);
          setCondemnAssetId("");
          setCondemnReason("");
          setCondemnEvidenceUrl("");
        },
      }
    );
  };

  const handleSubmitDisposal = () => {
    if (!disposalAssetId) {
      setDisposalFormError("Please select a condemned asset.");
      return;
    }
    if (residualValue < 0) {
      setDisposalFormError("Residual value must be non-negative.");
      return;
    }
    setDisposalFormError(null);
    submitDisposal.mutate(
      {
        asset_id: disposalAssetId,
        disposal_method: disposalMethod,
        estimated_residual_value: residualValue,
        valuation_date: valuationDate ? valuationDate : null,
        notes: disposalNotes.trim() || null,
      },
      {
        onSuccess: () => {
          setIsDisposalModalOpen(false);
          setDisposalAssetId("");
          setDisposalMethod("AUCTION");
          setResidualValue(0);
          setValuationDate("");
          setDisposalNotes("");
          refetchDisposals();
        },
      }
    );
  };

  // Filter available active/transferrable assets
  const transferrableAssets = assets.filter((a) => a.status === "ACTIVE");
  const condemnCandidateAssets = assets.filter(
    (a) => a.status === "ACTIVE" || a.status === "UNDER_MAINTENANCE"
  );
  const condemnedAssets = assets.filter((a) => a.status === "CONDEMNED");

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Transfers & Disposals</h1>
          <p className="cg-page__subtitle">
            Initiate transfers, confirm asset arrivals, condemn unserviceable assets, and submit disposal requests (FR-043–050).
          </p>
        </div>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button
            renderIcon={DeliveryTruck}
            kind="primary"
            onClick={() => setIsTransferModalOpen(true)}
          >
            Initiate transfer
          </Button>
          <Button
            renderIcon={Warning}
            kind="secondary"
            onClick={() => setIsCondemnModalOpen(true)}
          >
            Condemn asset
          </Button>
          <Button
            renderIcon={Add}
            kind="tertiary"
            onClick={() => setIsDisposalModalOpen(true)}
          >
            Submit disposal
          </Button>
        </div>
      </div>

      {/* Action Error Banners */}
      {confirmReceipt.isError && (
        <InlineNotification
          kind="error"
          title="Could not confirm receipt"
          subtitle={getErrorMessage(
            confirmReceipt.error,
            "An error occurred while confirming asset receipt."
          )}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      {initiateTransfer.isError && (
        <InlineNotification
          kind="error"
          title="Could not initiate transfer"
          subtitle={getErrorMessage(
            initiateTransfer.error,
            "Transfer initiation failed."
          )}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      {condemnAsset.isError && (
        <InlineNotification
          kind="error"
          title="Could not condemn asset"
          subtitle={getErrorMessage(
            condemnAsset.error,
            "Asset condemnation failed."
          )}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      {submitDisposal.isError && (
        <InlineNotification
          kind="error"
          title="Could not submit disposal request"
          subtitle={getErrorMessage(
            submitDisposal.error,
            "Disposal submission failed."
          )}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <Tabs>
        <TabList aria-label="Transfer and disposal operations">
          <Tab>Transfers</Tab>
          <Tab>Disposals</Tab>
        </TabList>
        <TabPanels>
          {/* ── Transfers Panel ───────────────────────────────────────────────── */}
          <TabPanel>
            {isErrorTransfers && (
              <InlineNotification
                kind="error"
                title="Could not load transfers"
                subtitle={getErrorMessage(
                  transfersError,
                  "Something went wrong loading transfers."
                )}
                lowContrast
                hideCloseButton
                style={{ marginBottom: "1rem", maxWidth: "100%" }}
              />
            )}

            <div className="cg-section">
              {isLoadingTransfers ? (
                <div className="cg-placeholder">
                  <p>Loading transfers…</p>
                </div>
              ) : transfers && transfers.items.length > 0 ? (
                <>
                  <table className="cg-table cg-table--no-hover">
                    <thead>
                      <tr>
                        <th>Asset</th>
                        <th>From</th>
                        <th>To</th>
                        <th>Status</th>
                        <th>Requested by</th>
                        <th>Requested</th>
                        <th style={{ textAlign: "right" }}>Receipt</th>
                      </tr>
                    </thead>
                    <tbody>
                      {transfers.items.map((t) => {
                        const canConfirm = t.status === "APPROVED" || t.status === "IN_TRANSIT";
                        return (
                          <tr key={t.id}>
                            <td>
                              <span className="cg-table__mono">{t.asset_code}</span>
                              <br />
                              <span className="cg-table__muted">{t.asset_name}</span>
                            </td>
                            <td className="cg-table__muted">
                              {t.from_department_name || "—"}
                              {t.from_location_name ? ` (${t.from_location_name})` : ""}
                            </td>
                            <td className="cg-table__muted">
                              {t.to_department_name || "—"}
                              {t.to_location_name ? ` (${t.to_location_name})` : ""}
                            </td>
                            <td>
                              <Tag type={statusTagColor(t.status)}>
                                {formatStatusLabel(t.status)}
                              </Tag>
                            </td>
                            <td className="cg-table__muted">
                              {t.initiated_by_user_email || "—"}
                            </td>
                            <td className="cg-table__muted">
                              {new Date(t.requested_at).toLocaleDateString()}
                            </td>
                            <td style={{ textAlign: "right" }}>
                              {canConfirm ? (
                                <Button
                                  size="sm"
                                  kind="primary"
                                  renderIcon={Checkmark}
                                  disabled={confirmReceipt.isPending}
                                  onClick={() => handleConfirmReceipt(t)}
                                >
                                  Confirm receipt
                                </Button>
                              ) : (
                                <span className="cg-table__muted">—</span>
                              )}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                  <Pagination
                    page={transfers.page}
                    pageSize={transfers.page_size}
                    pageSizes={[10, 20, 50, 100]}
                    totalItems={transfers.total_count}
                    onChange={({ page: nextPage, pageSize: nextPageSize }) => {
                      setTransferPage(nextPage);
                      setTransferPageSize(nextPageSize);
                    }}
                    style={{ marginTop: "1rem" }}
                  />
                </>
              ) : (
                <div className="cg-placeholder">
                  <p>No transfer requests found.</p>
                </div>
              )}
            </div>
          </TabPanel>

          {/* ── Disposals Panel ───────────────────────────────────────────────── */}
          <TabPanel>
            {isErrorDisposals && (
              <InlineNotification
                kind="error"
                title="Could not load disposals"
                subtitle={getErrorMessage(
                  disposalsError,
                  "Something went wrong loading disposal requests."
                )}
                lowContrast
                hideCloseButton
                style={{ marginBottom: "1rem", maxWidth: "100%" }}
              />
            )}

            <div className="cg-section">
              {isLoadingDisposals ? (
                <div className="cg-placeholder">
                  <p>Loading disposals…</p>
                </div>
              ) : disposals && disposals.items.length > 0 ? (
                <>
                  <table className="cg-table cg-table--no-hover">
                    <thead>
                      <tr>
                        <th>Asset</th>
                        <th>Proposed method</th>
                        <th>Status</th>
                        <th>Estimated residual value</th>
                        <th>Requested by</th>
                        <th>Notes / Revision</th>
                        <th>Requested</th>
                      </tr>
                    </thead>
                    <tbody>
                      {disposals.items.map((d) => (
                        <tr key={d.id}>
                          <td>
                            <span className="cg-table__mono">{d.asset_code}</span>
                            <br />
                            <span className="cg-table__muted">{d.asset_name}</span>
                          </td>
                          <td className="cg-table__muted">
                            {formatStatusLabel(d.disposal_method)}
                          </td>
                          <td>
                            <Tag type={statusTagColor(d.status)}>
                              {formatStatusLabel(d.status)}
                            </Tag>
                          </td>
                          <td className="cg-table__muted">
                            {d.estimated_residual_value != null
                              ? `LKR ${Number(d.estimated_residual_value).toLocaleString()}`
                              : "—"}
                          </td>
                          <td className="cg-table__muted">
                            {d.initiated_by_user_email || "—"}
                          </td>
                          <td className="cg-table__muted" style={{ maxWidth: "240px", fontSize: "0.8125rem" }}>
                            {d.notes || "—"}
                          </td>
                          <td className="cg-table__muted">
                            {new Date(d.requested_at).toLocaleDateString()}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                  <Pagination
                    page={disposals.page}
                    pageSize={disposals.page_size}
                    pageSizes={[10, 20, 50, 100]}
                    totalItems={disposals.total_count}
                    onChange={({ page: nextPage, pageSize: nextPageSize }) => {
                      setDisposalPage(nextPage);
                      setDisposalPageSize(nextPageSize);
                    }}
                    style={{ marginTop: "1rem" }}
                  />
                </>
              ) : (
                <div className="cg-placeholder">
                  <p>No disposal requests found.</p>
                </div>
              )}
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>

      {/* Initiate Transfer Modal */}
      <Modal
        open={isTransferModalOpen}
        modalHeading="Initiate Asset Transfer (FR-043)"
        primaryButtonText="Submit Request"
        secondaryButtonText="Cancel"
        primaryButtonDisabled={initiateTransfer.isPending}
        onRequestClose={() => setIsTransferModalOpen(false)}
        onRequestSubmit={handleInitiateTransferSubmit}
      >
        <p style={{ marginBottom: "1rem", fontSize: "0.875rem" }}>
          Select an active asset and designate the target destination department and location.
        </p>

        {transferFormError && (
          <InlineNotification
            kind="error"
            title="Validation error"
            subtitle={transferFormError}
            lowContrast
            hideCloseButton
            style={{ marginBottom: "1rem" }}
          />
        )}

        <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <ComboBox
            id="transfer-asset-select"
            titleText="Asset *"
            placeholder={isLoadingAssets ? "Loading assets..." : "Select an asset to transfer"}
            items={transferrableAssets}
            itemToString={(item) => (item ? `${item.asset_code} — ${item.name} (${item.department_name})` : "")}
            onChange={({ selectedItem }) => setTransferAssetId(selectedItem?.id ?? "")}
          />

          <Select
            id="transfer-dept-select"
            labelText="Destination Department *"
            value={transferDeptId}
            onChange={(e) => {
              setTransferDeptId(e.target.value);
              setTransferLocId("");
            }}
          >
            <SelectItem value="" text="Choose destination department" />
            {departments.data?.map((d) => (
              <SelectItem key={d.id} value={d.id} text={d.name} />
            ))}
          </Select>

          <Select
            id="transfer-loc-select"
            labelText="Destination Location *"
            value={transferLocId}
            disabled={!transferDeptId}
            onChange={(e) => setTransferLocId(e.target.value)}
          >
            <SelectItem
              value=""
              text={
                !transferDeptId
                  ? "Select a destination department first"
                  : "Choose destination location"
              }
            />
            {locations.data?.map((l) => (
              <SelectItem
                key={l.id}
                value={l.id}
                text={l.type ? `${l.name} (${l.type})` : l.name}
              />
            ))}
          </Select>
        </div>
      </Modal>

      {/* Condemn Asset Modal */}
      <Modal
        open={isCondemnModalOpen}
        modalHeading="Condemn Unserviceable Asset (FR-049)"
        primaryButtonText="Condemn Asset"
        secondaryButtonText="Cancel"
        danger
        primaryButtonDisabled={condemnAsset.isPending}
        onRequestClose={() => setIsCondemnModalOpen(false)}
        onRequestSubmit={handleCondemnSubmit}
      >
        <p style={{ marginBottom: "1rem", fontSize: "0.875rem" }}>
          Condemning transitions the asset condition to <code>UNSERVICEABLE</code> and status to <code>CONDEMNED</code>, releasing it for the disposal workflow.
        </p>

        {condemnFormError && (
          <InlineNotification
            kind="error"
            title="Validation error"
            subtitle={condemnFormError}
            lowContrast
            hideCloseButton
            style={{ marginBottom: "1rem" }}
          />
        )}

        <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <ComboBox
            id="condemn-asset-select"
            titleText="Asset to Condemn *"
            placeholder="Select asset"
            items={condemnCandidateAssets}
            itemToString={(item) => (item ? `${item.asset_code} — ${item.name} (${item.condition})` : "")}
            onChange={({ selectedItem }) => setCondemnAssetId(selectedItem?.id ?? "")}
          />

          <TextArea
            id="condemn-reason"
            labelText="Condemnation Justification / Reason *"
            placeholder="Detailed assessment why this asset cannot be repaired or used..."
            rows={3}
            value={condemnReason}
            onChange={(e) => setCondemnReason(e.target.value)}
          />

          <TextInput
            id="condemn-evidence"
            labelText="Evidence Document / Photo URL (Optional)"
            placeholder="https://..."
            value={condemnEvidenceUrl}
            onChange={(e) => setCondemnEvidenceUrl(e.target.value)}
          />
        </div>
      </Modal>

      {/* Submit Disposal Request Modal */}
      <Modal
        open={isDisposalModalOpen}
        modalHeading="Submit Disposal Request (FR-050)"
        primaryButtonText="Submit Disposal"
        secondaryButtonText="Cancel"
        primaryButtonDisabled={submitDisposal.isPending}
        onRequestClose={() => setIsDisposalModalOpen(false)}
        onRequestSubmit={handleSubmitDisposal}
      >
        <p style={{ marginBottom: "1rem", fontSize: "0.875rem" }}>
          Submit an asset for disposal evaluation. Only <code>CONDEMNED</code> assets can be submitted for disposal.
        </p>

        {disposalFormError && (
          <InlineNotification
            kind="error"
            title="Validation error"
            subtitle={disposalFormError}
            lowContrast
            hideCloseButton
            style={{ marginBottom: "1rem" }}
          />
        )}

        <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <ComboBox
            id="disposal-asset-select"
            titleText="Condemned Asset *"
            placeholder="Select condemned asset"
            items={condemnedAssets}
            itemToString={(item) => (item ? `${item.asset_code} — ${item.name}` : "")}
            onChange={({ selectedItem }) => setDisposalAssetId(selectedItem?.id ?? "")}
          />

          <Select
            id="disposal-method-select"
            labelText="Proposed Disposal Method *"
            value={disposalMethod}
            onChange={(e) => setDisposalMethod(e.target.value as DisposalMethod)}
          >
            <SelectItem value="AUCTION" text="Auction / Public Sale" />
            <SelectItem value="SCRAP" text="Scrap / Salvage" />
            <SelectItem value="DONATION" text="Donation" />
            <SelectItem value="DESTROY" text="Destruction / Recycling" />
          </Select>

          <NumberInput
            id="disposal-residual-value"
            label="Estimated Residual Value (LKR) *"
            min={0}
            step={100}
            value={residualValue}
            onChange={(_e, { value }) => setResidualValue(Number(value) || 0)}
          />

          <TextInput
            id="disposal-valuation-date"
            labelText="Valuation Date (YYYY-MM-DD)"
            placeholder="2026-09-12"
            value={valuationDate}
            onChange={(e) => setValuationDate(e.target.value)}
          />

          <TextArea
            id="disposal-notes"
            labelText="Notes / Comments (Optional)"
            placeholder="Additional context, disposal committee notes..."
            rows={3}
            value={disposalNotes}
            onChange={(e) => setDisposalNotes(e.target.value)}
          />
        </div>
      </Modal>
    </div>
  );
}
