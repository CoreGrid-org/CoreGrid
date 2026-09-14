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
  TextArea,
} from "@carbon/react";
import { CheckmarkFilled, CloseFilled, Restart, Checkmark } from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useTransfersList, useApproveTransfer } from "../hooks/useTransfers";
import {
  useDisposalsList,
  useApproveDisposal,
  useRequestDisposalRevision,
} from "../hooks/useDisposals";
import type { DisposalResponse, TransferResponse } from "../types";

export default function TransfersPage() {
  const {
    data: transfers,
    isLoading: isLoadingTransfers,
    isError: isErrorTransfers,
    error: transfersError,
    refetch: refetchTransfers,
  } = useTransfersList();

  const {
    data: disposals,
    isLoading: isLoadingDisposals,
    isError: isErrorDisposals,
    error: disposalsError,
    refetch: refetchDisposals,
  } = useDisposalsList();

  const approveTransfer = useApproveTransfer();
  const approveDisposal = useApproveDisposal();
  const requestRevision = useRequestDisposalRevision();

  // Selected item for revision modal
  const [revisionTarget, setRevisionTarget] = useState<DisposalResponse | null>(null);
  const [revisionComments, setRevisionComments] = useState("");
  const [revisionError, setRevisionError] = useState<string | null>(null);

  const handleApproveTransfer = (transfer: TransferResponse) => {
    approveTransfer.mutate(transfer.id, {
      onSuccess: () => refetchTransfers(),
    });
  };

  const handleApproveDisposal = (disposal: DisposalResponse) => {
    approveDisposal.mutate(disposal.id, {
      onSuccess: () => refetchDisposals(),
    });
  };

  const handleOpenRevisionModal = (disposal: DisposalResponse) => {
    setRevisionTarget(disposal);
    setRevisionComments("");
    setRevisionError(null);
  };

  const handleSubmitRevision = () => {
    if (!revisionTarget) return;
    if (!revisionComments.trim()) {
      setRevisionError("Comments are required to request revision.");
      return;
    }
    setRevisionError(null);
    requestRevision.mutate(
      { id: revisionTarget.id, payload: { comments: revisionComments.trim() } },
      {
        onSuccess: () => {
          setRevisionTarget(null);
          refetchDisposals();
        },
      }
    );
  };

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Transfers & Disposals</h1>
          <p className="cg-page__subtitle">
            Administrator queue for transfer approvals (FR-045) and disposal evaluations (FR-051–055).
          </p>
        </div>
      </div>

      {/* Global Mutation Notifications */}
      {approveTransfer.isError && (
        <InlineNotification
          kind="error"
          title="Could not approve transfer"
          subtitle={getErrorMessage(
            approveTransfer.error,
            "An error occurred while approving the transfer."
          )}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      {approveDisposal.isError && (
        <InlineNotification
          kind="error"
          title="Could not approve disposal"
          subtitle={getErrorMessage(
            approveDisposal.error,
            "An error occurred while approving the disposal. Ensure all preconditions (P1–P6) pass and separation-of-duties is satisfied."
          )}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      {requestRevision.isError && (
        <InlineNotification
          kind="error"
          title="Could not request revision"
          subtitle={getErrorMessage(
            requestRevision.error,
            "An error occurred while returning the disposal for revision."
          )}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <Tabs>
        <TabList aria-label="Transfer and disposal sections">
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
              ) : transfers && transfers.length > 0 ? (
                <table className="cg-table cg-table--no-hover">
                  <thead>
                    <tr>
                      <th>Asset</th>
                      <th>From</th>
                      <th>To</th>
                      <th>Status</th>
                      <th>Requested by</th>
                      <th>Requested</th>
                      <th style={{ textAlign: "right" }}>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {transfers.map((t) => (
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
                          {t.status === "REQUESTED" ? (
                            <Button
                              size="sm"
                              kind="primary"
                              renderIcon={Checkmark}
                              disabled={approveTransfer.isPending}
                              onClick={() => handleApproveTransfer(t)}
                            >
                              Approve
                            </Button>
                          ) : (
                            <span className="cg-table__muted">—</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
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
              ) : disposals && disposals.length > 0 ? (
                <table className="cg-table cg-table--no-hover">
                  <thead>
                    <tr>
                      <th>Asset</th>
                      <th>Proposed method</th>
                      <th>Status</th>
                      <th>Estimated residual value</th>
                      <th>Requested by</th>
                      <th>Requested</th>
                      <th style={{ textAlign: "right" }}>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {disposals.map((d) => (
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
                        <td className="cg-table__muted">
                          {new Date(d.requested_at).toLocaleDateString()}
                        </td>
                        <td style={{ textAlign: "right" }}>
                          {d.status === "PENDING" && (
                            <Button
                              size="sm"
                              kind="ghost"
                              renderIcon={Restart}
                              disabled={requestRevision.isPending}
                              onClick={() => handleOpenRevisionModal(d)}
                            >
                              Request revision
                            </Button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              ) : (
                <div className="cg-placeholder">
                  <p>No disposal requests found.</p>
                </div>
              )}
            </div>

            {/* Live Precondition Checklists for PENDING Disposals */}
            {disposals
              ?.filter((d) => d.status === "PENDING" && d.precondition_evaluation?.checks)
              .map((d) => {
                const evalResult = d.precondition_evaluation;
                const checks = evalResult?.checks ?? [];
                const allPassed = Boolean(evalResult?.all_passed);
                const sodPassed = evalResult?.separation_of_duties_passed ?? true;
                const canApprove = allPassed && sodPassed;

                return (
                  <div className="cg-section" key={d.id}>
                    <div className="cg-section__header">
                      <div>
                        <p className="cg-section__title">
                          Precondition checklist — {d.asset_code} ({formatStatusLabel(d.status)})
                        </p>
                        <p className="cg-section__subtitle" style={{ fontSize: "0.8125rem", color: "#525252" }}>
                          Proposed Method: {formatStatusLabel(d.disposal_method)} | Residual Value: LKR {Number(d.estimated_residual_value).toLocaleString()}
                        </p>
                      </div>
                      <div style={{ display: "flex", gap: "0.5rem" }}>
                        <Button
                          kind="secondary"
                          size="sm"
                          renderIcon={Restart}
                          disabled={requestRevision.isPending}
                          onClick={() => handleOpenRevisionModal(d)}
                        >
                          Request revision
                        </Button>
                        <Button
                          kind="danger"
                          size="sm"
                          renderIcon={Checkmark}
                          disabled={!canApprove || approveDisposal.isPending}
                          onClick={() => handleApproveDisposal(d)}
                        >
                          Approve disposal
                        </Button>
                      </div>
                    </div>

                    {!sodPassed && (
                      <InlineNotification
                        kind="warning"
                        title="Separation of Duties"
                        subtitle={
                          evalResult?.separation_of_duties_failure_reason ||
                          "Approver cannot be the requester of the disposal."
                        }
                        lowContrast
                        hideCloseButton
                        style={{ marginBottom: "0.75rem", maxWidth: "100%" }}
                      />
                    )}

                    <div
                      className="cg-section__body"
                      style={{
                        display: "flex",
                        flexDirection: "column",
                        gap: "0.75rem",
                      }}
                    >
                      {checks.map((p) => (
                        <div
                          key={p.code}
                          style={{
                            display: "flex",
                            alignItems: "flex-start",
                            gap: "0.625rem",
                          }}
                        >
                          {p.passed ? (
                            <CheckmarkFilled
                              size={18}
                              style={{ fill: "#24a148", flexShrink: 0, marginTop: "2px" }}
                            />
                          ) : (
                            <CloseFilled
                              size={18}
                              style={{ fill: "#da1e28", flexShrink: 0, marginTop: "2px" }}
                            />
                          )}
                          <div>
                            <div>
                              <span
                                style={{
                                  fontSize: "0.8125rem",
                                  color: "#525252",
                                  fontWeight: 600,
                                  marginRight: "0.5rem",
                                }}
                              >
                                {p.code}
                              </span>
                              <span style={{ fontSize: "0.875rem" }}>
                                {p.description}
                              </span>
                            </div>
                            {!p.passed && p.failure_reason && (
                              <p
                                style={{
                                  fontSize: "0.8125rem",
                                  color: "#da1e28",
                                  marginTop: "0.125rem",
                                }}
                              >
                                {p.failure_reason}
                              </p>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })}
          </TabPanel>
        </TabPanels>
      </Tabs>

      {/* Revision Modal */}
      {revisionTarget && (
        <Modal
          open={Boolean(revisionTarget)}
          modalHeading={`Request Revision — ${revisionTarget.asset_code}`}
          primaryButtonText="Submit Revision Request"
          secondaryButtonText="Cancel"
          primaryButtonDisabled={requestRevision.isPending || !revisionComments.trim()}
          onRequestClose={() => setRevisionTarget(null)}
          onRequestSubmit={handleSubmitRevision}
        >
          <p style={{ marginBottom: "1rem", fontSize: "0.875rem" }}>
            Return the disposal request for <strong>{revisionTarget.asset_name}</strong> to the requester with comments.
            The request status will be updated to <code>REVISION_REQUESTED</code>.
          </p>

          {revisionError && (
            <InlineNotification
              kind="error"
              title="Validation error"
              subtitle={revisionError}
              lowContrast
              hideCloseButton
              style={{ marginBottom: "1rem" }}
            />
          )}

          <TextArea
            id="revision-comments"
            labelText="Revision Comments / Requirements *"
            placeholder="Specify what changes or justifications are needed..."
            rows={4}
            value={revisionComments}
            onChange={(e) => setRevisionComments(e.target.value)}
          />
        </Modal>
      )}
    </div>
  );
}
