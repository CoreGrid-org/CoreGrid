import { useState } from "react";
import { Tabs, TabList, Tab, TabPanels, TabPanel, Button, InlineNotification } from "@carbon/react";
import { Add, Checkmark, DeliveryTruck, Restart, View, Warning } from "@carbon/icons-react";
import type { CoreGridRole } from "@/features/auth/lib/roles";
import { formatStatusLabel } from "@/shared/lib/statusTag";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { useTransfersList, useApproveTransfer, useConfirmTransferReceipt } from "../hooks/useTransfers";
import { useDisposalsList, useApproveDisposal } from "../hooks/useDisposals";
import { transferCapabilities } from "../lib/capabilities";
import PagedSection from "../components/PagedSection";
import TransfersTable from "../components/TransfersTable";
import DisposalsTable from "../components/DisposalsTable";
import PreconditionChecklist from "../components/PreconditionChecklist";
import InitiateTransferModal from "../components/InitiateTransferModal";
import CondemnAssetModal from "../components/CondemnAssetModal";
import SubmitDisposalModal from "../components/SubmitDisposalModal";
import RequestRevisionModal from "../components/RequestRevisionModal";
import DisposalDetailModal from "../components/DisposalDetailModal";
import { formatLkr } from "../lib/format";
import type { DisposalResponse, TransferResponse } from "../types";

type OpenModal = "transfer" | "condemn" | "disposal" | null;

// One Transfers & Disposals page for every role that has it (Administrator,
// InventoryOfficer, Auditor) — the route passes the role, and
// transferCapabilities() decides which actions and audit columns show.
export default function TransfersPage({ role }: { role: CoreGridRole }) {
  const can = transferCapabilities(role);

  const [transferPaging, setTransferPaging] = useState({ page: 1, pageSize: 20 });
  const [disposalPaging, setDisposalPaging] = useState({ page: 1, pageSize: 20 });
  const [openModal, setOpenModal] = useState<OpenModal>(null);
  const [revisionTarget, setRevisionTarget] = useState<DisposalResponse | null>(null);
  const [detailDisposalId, setDetailDisposalId] = useState<string | null>(null);

  const transfers = useTransfersList(transferPaging);
  const disposals = useDisposalsList(disposalPaging);

  const approveTransfer = useApproveTransfer();
  const confirmReceipt = useConfirmTransferReceipt();
  const approveDisposal = useApproveDisposal();

  const mutationErrors = [
    { failed: approveTransfer.isError, title: "Could not approve transfer", error: approveTransfer.error },
    { failed: confirmReceipt.isError, title: "Could not confirm receipt", error: confirmReceipt.error },
    {
      failed: approveDisposal.isError,
      title: "Could not approve disposal",
      error: approveDisposal.error,
      fallback: "Make sure every precondition passes and the approver isn't the requester.",
    },
  ].filter((e) => e.failed);

  const renderTransferAction = (t: TransferResponse) => {
    if (can.canApprove && t.status === "REQUESTED") {
      return (
        <Button
          size="sm"
          renderIcon={Checkmark}
          disabled={approveTransfer.isPending}
          onClick={() => approveTransfer.mutate(t.id, { onSuccess: transfers.refetch })}
        >
          Approve
        </Button>
      );
    }
    if (can.canConfirmReceipt && (t.status === "APPROVED" || t.status === "IN_TRANSIT")) {
      return (
        <Button
          size="sm"
          renderIcon={Checkmark}
          disabled={confirmReceipt.isPending}
          onClick={() => confirmReceipt.mutate(t.id, { onSuccess: transfers.refetch })}
        >
          Confirm receipt
        </Button>
      );
    }
    return <span className="cg-table__muted">-</span>;
  };

  const renderDisposalAction = (d: DisposalResponse) => {
    if (can.isAuditView) {
      return (
        <Button
          size="sm"
          kind="ghost"
          renderIcon={View}
          iconDescription="View compliance details"
          hasIconOnly
          onClick={() => setDetailDisposalId(d.id)}
        />
      );
    }
    if (can.canApprove && d.status === "PENDING") {
      return (
        <Button size="sm" kind="ghost" renderIcon={Restart} onClick={() => setRevisionTarget(d)}>
          Request revision
        </Button>
      );
    }
    return null;
  };

  const pendingReviews = can.canApprove
    ? (disposals.data?.items ?? []).filter((d) => d.status === "PENDING" && d.precondition_evaluation?.checks)
    : [];

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">{can.isAuditView ? "Transfers & Disposals Audit" : "Transfers & Disposals"}</h1>
          <p className="cg-page__subtitle">
            {can.isAuditView
              ? "Read-only audit trail and compliance verification for transfers and disposals."
              : can.canApprove
                ? "Initiate and approve inter-departmental asset transfers, confirm receipts, condemn damaged equipment, and manage disposal requests."
                : "Initiate inter-departmental transfers, confirm asset arrivals, condemn unserviceable assets, and submit disposal requests."}
          </p>
        </div>
        {can.canCreate && (
          <div style={{ display: "flex", gap: "0.5rem" }}>
            <Button renderIcon={DeliveryTruck} onClick={() => setOpenModal("transfer")}>
              Initiate transfer
            </Button>
            <Button renderIcon={Warning} kind="secondary" onClick={() => setOpenModal("condemn")}>
              Condemn asset
            </Button>
            <Button renderIcon={Add} kind="tertiary" onClick={() => setOpenModal("disposal")}>
              Submit disposal
            </Button>
          </div>
        )}
      </div>

      {mutationErrors.map((e) => (
        <InlineNotification
          key={e.title}
          kind="error"
          title={e.title}
          subtitle={getErrorMessage(e.error, e.fallback ?? "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      ))}

      <Tabs>
        <TabList aria-label="Transfer and disposal sections">
          <Tab>{can.isAuditView ? "Transfer history" : "Transfers"}</Tab>
          <Tab>{can.isAuditView ? "Disposal compliance" : "Disposals"}</Tab>
        </TabList>
        <TabPanels>
          <TabPanel>
            <PagedSection
              result={transfers.data}
              isLoading={transfers.isLoading}
              isError={transfers.isError}
              error={transfers.error}
              noun="transfers"
              onPageChange={(page, pageSize) => setTransferPaging({ page, pageSize })}
            >
              {(items) => (
                <TransfersTable
                  transfers={items}
                  showAuditTrail={can.isAuditView}
                  renderAction={can.isAuditView ? undefined : renderTransferAction}
                />
              )}
            </PagedSection>
          </TabPanel>

          <TabPanel>
            <PagedSection
              result={disposals.data}
              isLoading={disposals.isLoading}
              isError={disposals.isError}
              error={disposals.error}
              noun="disposal requests"
              onPageChange={(page, pageSize) => setDisposalPaging({ page, pageSize })}
            >
              {(items) => (
                <DisposalsTable
                  disposals={items}
                  showAuditTrail={can.isAuditView}
                  showNotes={!can.canApprove && !can.isAuditView}
                  renderAction={can.isAuditView || can.canApprove ? renderDisposalAction : undefined}
                />
              )}
            </PagedSection>

            {pendingReviews.map((d) => {
              const evaluation = d.precondition_evaluation!;
              const canApproveThis = evaluation.all_passed && evaluation.separation_of_duties_passed;
              return (
                <section className="cg-section" key={d.id}>
                  <header className="cg-section__header">
                    <div>
                      <p className="cg-section__title">Precondition checklist: {d.asset_code}</p>
                      <p className="cg-table__muted" style={{ margin: "0.125rem 0 0", fontSize: "0.8125rem" }}>
                        {formatStatusLabel(d.disposal_method)} · {formatLkr(d.estimated_residual_value)}
                      </p>
                    </div>
                    <div style={{ display: "flex", gap: "0.5rem" }}>
                      <Button kind="secondary" size="sm" renderIcon={Restart} onClick={() => setRevisionTarget(d)}>
                        Request revision
                      </Button>
                      <Button
                        kind="danger"
                        size="sm"
                        renderIcon={Checkmark}
                        disabled={!canApproveThis || approveDisposal.isPending}
                        onClick={() => approveDisposal.mutate(d.id, { onSuccess: disposals.refetch })}
                      >
                        Approve disposal
                      </Button>
                    </div>
                  </header>
                  <div className="cg-section__body">
                    <PreconditionChecklist evaluation={evaluation} />
                  </div>
                </section>
              );
            })}
          </TabPanel>
        </TabPanels>
      </Tabs>

      {openModal === "transfer" && (
        <InitiateTransferModal
          onClose={() => setOpenModal(null)}
          onInitiated={() => {
            setOpenModal(null);
            transfers.refetch();
          }}
        />
      )}
      {openModal === "condemn" && (
        <CondemnAssetModal onClose={() => setOpenModal(null)} onCondemned={() => setOpenModal(null)} />
      )}
      {openModal === "disposal" && (
        <SubmitDisposalModal
          onClose={() => setOpenModal(null)}
          onSubmitted={() => {
            setOpenModal(null);
            disposals.refetch();
          }}
        />
      )}
      {revisionTarget && (
        <RequestRevisionModal
          disposal={revisionTarget}
          onClose={() => setRevisionTarget(null)}
          onRequested={() => {
            setRevisionTarget(null);
            disposals.refetch();
          }}
        />
      )}
      {detailDisposalId && <DisposalDetailModal disposalId={detailDisposalId} onClose={() => setDetailDisposalId(null)} />}
    </div>
  );
}
