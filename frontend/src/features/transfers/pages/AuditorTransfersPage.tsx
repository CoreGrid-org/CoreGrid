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
  Pagination,
} from "@carbon/react";
import {
  CheckmarkFilled,
  CloseFilled,
  View,
} from "@carbon/icons-react";
import { statusTagColor, formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useTransfersList } from "../hooks/useTransfers";
import { useDisposalsList, useDisposalDetail } from "../hooks/useDisposals";

export default function AuditorTransfersPage() {
  const [transferPage, setTransferPage] = useState(1);
  const [transferPageSize, setTransferPageSize] = useState(25);
  const [disposalPage, setDisposalPage] = useState(1);
  const [disposalPageSize, setDisposalPageSize] = useState(25);

  const {
    data: transfers,
    isLoading: isLoadingTransfers,
    isError: isErrorTransfers,
    error: transfersError,
  } = useTransfersList({ page: transferPage, pageSize: transferPageSize });

  const {
    data: disposals,
    isLoading: isLoadingDisposals,
    isError: isErrorDisposals,
    error: disposalsError,
  } = useDisposalsList({ page: disposalPage, pageSize: disposalPageSize });

  // Selected disposal ID for viewing full compliance audit details & P1–P6 checklist
  const [selectedDisposalId, setSelectedDisposalId] = useState<string | null>(null);
  const {
    data: disposalDetail,
    isLoading: isLoadingDetail,
    isError: isErrorDetail,
    error: detailError,
  } = useDisposalDetail(selectedDisposalId);

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Transfers & Disposals Audit</h1>
          <p className="cg-page__subtitle">
            Read-only audit trail and compliance verification for transfers (FR-043–047) and disposals (FR-049–055).
          </p>
        </div>
      </div>

      <Tabs>
        <TabList aria-label="Auditor transfer and disposal sections">
          <Tab>Transfer History</Tab>
          <Tab>Disposal Compliance</Tab>
        </TabList>
        <TabPanels>
          {/* ── Transfer History Tab ────────────────────────────────────────── */}
          <TabPanel>
            {isErrorTransfers && (
              <InlineNotification
                kind="error"
                title="Could not load transfer history"
                subtitle={getErrorMessage(
                  transfersError,
                  "Something went wrong loading organization transfers."
                )}
                lowContrast
                hideCloseButton
                style={{ marginBottom: "1rem", maxWidth: "100%" }}
              />
            )}

            <div className="cg-section">
              {isLoadingTransfers ? (
                <div className="cg-placeholder">
                  <p>Loading transfer audit records…</p>
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
                        <th>Requested By</th>
                        <th>Approved By</th>
                        <th>Confirmed By</th>
                        <th>Requested At</th>
                        <th>Notes / Reason</th>
                      </tr>
                    </thead>
                    <tbody>
                      {transfers.items.map((t) => (
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
                            {t.approved_by_user_email ? (
                              <div>
                                <div>{t.approved_by_user_email}</div>
                                {t.approved_at && (
                                  <span style={{ fontSize: "0.75rem", color: "#8d8d8d" }}>
                                    {new Date(t.approved_at).toLocaleDateString()}
                                  </span>
                                )}
                              </div>
                            ) : (
                              "—"
                            )}
                          </td>
                          <td className="cg-table__muted">
                            {t.confirmed_by_user_email ? (
                              <div>
                                <div>{t.confirmed_by_user_email}</div>
                                {t.confirmed_at && (
                                  <span style={{ fontSize: "0.75rem", color: "#8d8d8d" }}>
                                    {new Date(t.confirmed_at).toLocaleDateString()}
                                  </span>
                                )}
                              </div>
                            ) : (
                              "—"
                            )}
                          </td>
                          <td className="cg-table__muted">
                            {new Date(t.requested_at).toLocaleDateString()}
                          </td>
                          <td className="cg-table__muted" style={{ maxWidth: "200px", fontSize: "0.8125rem" }}>
                            {t.rejection_reason || "—"}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                  <Pagination
                    page={transfers.page}
                    pageSize={transfers.page_size}
                    pageSizes={[10, 25, 50, 100]}
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
                  <p>No transfer records found in organization audit trail.</p>
                </div>
              )}
            </div>
          </TabPanel>

          {/* ── Disposal Compliance Tab ────────────────────────────────────── */}
          <TabPanel>
            {isErrorDisposals && (
              <InlineNotification
                kind="error"
                title="Could not load disposals"
                subtitle={getErrorMessage(
                  disposalsError,
                  "Something went wrong loading disposal compliance records."
                )}
                lowContrast
                hideCloseButton
                style={{ marginBottom: "1rem", maxWidth: "100%" }}
              />
            )}

            <div className="cg-section">
              {isLoadingDisposals ? (
                <div className="cg-placeholder">
                  <p>Loading disposal audit records…</p>
                </div>
              ) : disposals && disposals.items.length > 0 ? (
                <>
                  <table className="cg-table cg-table--no-hover">
                    <thead>
                      <tr>
                        <th>Asset</th>
                        <th>Proposed Method</th>
                        <th>Status</th>
                        <th>Valuation (LKR)</th>
                        <th>Requested By</th>
                        <th>Approved By</th>
                        <th>Requested At</th>
                        <th style={{ textAlign: "right" }}>Compliance Details</th>
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
                          <td className="cg-table__muted">
                            {d.approved_by_user_email ? (
                              <div>
                                <div>{d.approved_by_user_email}</div>
                                {d.approved_at && (
                                  <span style={{ fontSize: "0.75rem", color: "#8d8d8d" }}>
                                    {new Date(d.approved_at).toLocaleDateString()}
                                  </span>
                                )}
                              </div>
                            ) : (
                              "—"
                            )}
                          </td>
                          <td className="cg-table__muted">
                            {new Date(d.requested_at).toLocaleDateString()}
                          </td>
                          <td style={{ textAlign: "right" }}>
                            <Button
                              size="sm"
                              kind="ghost"
                              renderIcon={View}
                              iconDescription="Inspect compliance preconditions"
                              hasIconOnly
                              onClick={() => setSelectedDisposalId(d.id)}
                            />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                  <Pagination
                    page={disposals.page}
                    pageSize={disposals.page_size}
                    pageSizes={[10, 25, 50, 100]}
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
                  <p>No disposal records found in organization audit trail.</p>
                </div>
              )}
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>

      {/* Compliance Inspection Modal (Read-Only) */}
      {selectedDisposalId && (
        <Modal
          open={Boolean(selectedDisposalId)}
          modalHeading={`Disposal Compliance Audit — ${disposalDetail?.asset_code || "Loading..."}`}
          passiveModal
          onRequestClose={() => setSelectedDisposalId(null)}
        >
          {isLoadingDetail ? (
            <div className="cg-placeholder">
              <p>Evaluating and loading live compliance record…</p>
            </div>
          ) : isErrorDetail || !disposalDetail ? (
            <InlineNotification
              kind="error"
              title="Could not load disposal details"
              subtitle={getErrorMessage(detailError, "Failed to load detailed record.")}
              lowContrast
              hideCloseButton
            />
          ) : (
            <div style={{ display: "flex", flexDirection: "column", gap: "1.25rem" }}>
              {/* Asset & Valuation Info */}
              <div
                style={{
                  display: "grid",
                  gridTemplateColumns: "1fr 1fr",
                  gap: "0.75rem",
                  padding: "1rem",
                  background: "#f4f4f4",
                  borderRadius: "4px",
                }}
              >
                <div>
                  <span style={{ fontSize: "0.75rem", color: "#525252", fontWeight: 600 }}>Asset</span>
                  <p style={{ fontWeight: 600 }}>{disposalDetail.asset_name} ({disposalDetail.asset_code})</p>
                </div>
                <div>
                  <span style={{ fontSize: "0.75rem", color: "#525252", fontWeight: 600 }}>Status</span>
                  <p>
                    <Tag type={statusTagColor(disposalDetail.status)}>
                      {formatStatusLabel(disposalDetail.status)}
                    </Tag>
                  </p>
                </div>
                <div>
                  <span style={{ fontSize: "0.75rem", color: "#525252", fontWeight: 600 }}>Disposal Method</span>
                  <p>{formatStatusLabel(disposalDetail.disposal_method)}</p>
                </div>
                <div>
                  <span style={{ fontSize: "0.75rem", color: "#525252", fontWeight: 600 }}>Estimated Residual Value</span>
                  <p>LKR {Number(disposalDetail.estimated_residual_value).toLocaleString()}</p>
                </div>
                <div>
                  <span style={{ fontSize: "0.75rem", color: "#525252", fontWeight: 600 }}>Valuation Date</span>
                  <p>{disposalDetail.valuation_date || "—"}</p>
                </div>
                <div>
                  <span style={{ fontSize: "0.75rem", color: "#525252", fontWeight: 600 }}>Disposed Date</span>
                  <p>{disposalDetail.disposed_at ? new Date(disposalDetail.disposed_at).toLocaleDateString() : "—"}</p>
                </div>
              </div>

              {/* Notes & Revision Audit Trail */}
              {disposalDetail.notes && (
                <div>
                  <span style={{ fontSize: "0.8125rem", fontWeight: 600, color: "#161616" }}>
                    Recorded Notes & Revision History
                  </span>
                  <div
                    style={{
                      marginTop: "0.5rem",
                      padding: "0.75rem",
                      background: "#fff",
                      border: "1px solid #e0e0e0",
                      fontSize: "0.8125rem",
                      whiteSpace: "pre-wrap",
                      fontFamily: "monospace",
                    }}
                  >
                    {disposalDetail.notes}
                  </div>
                </div>
              )}

              {/* Separation of Duties Assessment */}
              {disposalDetail.precondition_evaluation && (
                <div>
                  <span style={{ fontSize: "0.875rem", fontWeight: 600, color: "#161616", marginBottom: "0.5rem", display: "block" }}>
                    Precondition Checks Evaluation (FR-051 §6.6)
                  </span>

                  {!disposalDetail.precondition_evaluation.separation_of_duties_passed && (
                    <InlineNotification
                      kind="warning"
                      title="Separation of Duties Check"
                      subtitle={
                        disposalDetail.precondition_evaluation.separation_of_duties_failure_reason ||
                        "Approver cannot be the requester of the disposal."
                      }
                      lowContrast
                      hideCloseButton
                      style={{ marginBottom: "0.75rem", maxWidth: "100%" }}
                    />
                  )}

                  <div style={{ display: "flex", flexDirection: "column", gap: "0.625rem" }}>
                    {disposalDetail.precondition_evaluation.checks?.map((check) => (
                      <div
                        key={check.code}
                        style={{
                          display: "flex",
                          alignItems: "flex-start",
                          gap: "0.625rem",
                          padding: "0.5rem",
                          background: "#fff",
                          border: "1px solid #e0e0e0",
                        }}
                      >
                        {check.passed ? (
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
                              {check.code}
                            </span>
                            <span style={{ fontSize: "0.875rem", fontWeight: 500 }}>
                              {check.description}
                            </span>
                          </div>
                          {!check.passed && check.failure_reason && (
                            <p
                              style={{
                                fontSize: "0.8125rem",
                                color: "#da1e28",
                                marginTop: "0.125rem",
                              }}
                            >
                              Failure reason: {check.failure_reason}
                            </p>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </Modal>
      )}
    </div>
  );
}
