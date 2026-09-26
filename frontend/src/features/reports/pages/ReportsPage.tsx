import { Tabs, TabList, Tab, TabPanels, TabPanel } from "@carbon/react";
import { useMe } from "@/features/auth/hooks/useMe";
import AuditReportPanel from "../components/AuditReportPanel";
import InventoryReportPanel from "../components/InventoryReportPanel";
import MaintenanceReportPanel from "../components/MaintenanceReportPanel";
import DisposalReportPanel from "../components/DisposalReportPanel";

// Controls report visibility based on the current user's role.
export default function ReportsPage() {
  const { data: me } = useMe();
  const canSeeAudit = me?.role === "Auditor" || me?.role === "Administrator";

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Reports</h1>
          <p className="cg-page__subtitle">
            Generate, filter, and export comprehensive asset inventory, maintenance, disposal, and audit compliance reports.
          </p>
        </div>
      </div>

      <Tabs key={canSeeAudit ? "reports-audit-enabled" : "reports-standard"}>
        <TabList aria-label="Report sections">
          <Tab>Asset Inventory</Tab>
          <Tab>Maintenance</Tab>
          <Tab>Disposal</Tab>
          {canSeeAudit && <Tab>Audit</Tab>}
        </TabList>
        <TabPanels>
          <TabPanel>
            <InventoryReportPanel />
          </TabPanel>
          <TabPanel>
            <MaintenanceReportPanel />
          </TabPanel>
          <TabPanel>
            <DisposalReportPanel />
          </TabPanel>

          {canSeeAudit && (
            <TabPanel>
              <AuditReportPanel />
            </TabPanel>
          )}
        </TabPanels>
      </Tabs>
    </div>
  );
}
