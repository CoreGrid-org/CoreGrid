import { useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Tag, Button, InlineNotification, Pagination } from "@carbon/react";
import { Add, DeliveryTruck, Warning, Checkmark } from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useTransfersList, useConfirmTransferReceipt } from "../hooks/useTransfers";
import { useDisposalsList } from "../hooks/useDisposals";
import InitiateTransferModal from "../components/InitiateTransferModal";
import CondemnAssetModal from "../components/CondemnAssetModal";
import SubmitDisposalModal from "../components/SubmitDisposalModal";
import type { TransferResponse } from "../types";

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

  const [isTransferModalOpen, setIsTransferModalOpen] = useState(false);
  const [isCondemnModalOpen, setIsCondemnModalOpen] = useState(false);
  const [isDisposalModalOpen, setIsDisposalModalOpen] = useState(false);

  const confirmReceipt = useConfirmTransferReceipt();

  const handleConfirmReceipt = (transfer: TransferResponse) => {
    confirmReceipt.mutate(transfer.id, {
      onSuccess: () => refetchTransfers(),
    });
  };

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Transfers & Disposals</h1>
          <p className="cg-page__subtitle">
            Initiate inter-departmental transfers, confirm physical asset arrivals, condemn unserviceable assets, and submit disposal requests.
          </p>
        </div>
        <div style={{ display: "flex", gap: "0.5rem" }}>
          <Button renderIcon={DeliveryTruck} kind="primary" onClick={() => setIsTransferModalOpen(true)}>
            Initiate transfer
          </Button>
          <Button renderIcon={Warning} kind="secondary" onClick={() => setIsCondemnModalOpen(true)}>
            Condemn asset
          </Button>
          <Button renderIcon={Add} kind="tertiary" onClick={() => setIsDisposalModalOpen(true)}>
            Submit disposal
          </Button>
        </div>
      </div>

      {confirmReceipt.isError && (
        <InlineNotification
          kind="error"
          title="Could not confirm receipt"
          subtitle={getErrorMessage(confirmReceipt.error, "An error occurred while confirming asset receipt.")}
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
          {/* ── Transfers Panel ───────────────────────────────────────────── */}
          <TabPanel>
            {isErrorTransfers && (
              <InlineNotification
                kind="error"
                title="Could not load transfers"
                subtitle={getErrorMessage(transfersError, "Something went wrong loading transfers.")}
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
                              <Tag type={statusTagColor(t.status)}>{formatStatusLabel(t.status)}</Tag>
                            </td>
                            <td className="cg-table__muted">{t.initiated_by_user_email || "—"}</td>
                            <td className="cg-table__muted">{new Date(t.requested_at).toLocaleDateString()}</td>
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

          {/* ── Disposals Panel ───────────────────────────────────────────── */}
          <TabPanel>
            {isErrorDisposals && (
              <InlineNotification
                kind="error"
                title="Could not load disposals"
                subtitle={getErrorMessage(disposalsError, "Something went wrong loading disposal requests.")}
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
                          <td className="cg-table__muted">{formatStatusLabel(d.disposal_method)}</td>
                          <td>
                            <Tag type={statusTagColor(d.status)}>{formatStatusLabel(d.status)}</Tag>
                          </td>
                          <td className="cg-table__muted">
                            {d.estimated_residual_value != null ? `LKR ${Number(d.estimated_residual_value).toLocaleString()}` : "—"}
                          </td>
                          <td className="cg-table__muted">{d.initiated_by_user_email || "—"}</td>
                          <td className="cg-table__muted" style={{ maxWidth: "240px", fontSize: "0.8125rem" }}>
                            {d.notes || "—"}
                          </td>
                          <td className="cg-table__muted">{new Date(d.requested_at).toLocaleDateString()}</td>
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

      {isTransferModalOpen && (
        <InitiateTransferModal
          onClose={() => setIsTransferModalOpen(false)}
          onInitiated={() => {
            setIsTransferModalOpen(false);
            refetchTransfers();
          }}
        />
      )}

      {isCondemnModalOpen && (
        <CondemnAssetModal onClose={() => setIsCondemnModalOpen(false)} onCondemned={() => setIsCondemnModalOpen(false)} />
      )}

      {isDisposalModalOpen && (
        <SubmitDisposalModal
          onClose={() => setIsDisposalModalOpen(false)}
          onSubmitted={() => {
            setIsDisposalModalOpen(false);
            refetchDisposals();
          }}
        />
      )}
    </div>
  );
}
