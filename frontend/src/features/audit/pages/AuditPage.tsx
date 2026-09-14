import { Tabs, TabList, Tab, TabPanels, TabPanel } from "@carbon/react";
import CampaignsPanel from "../components/CampaignsPanel";
import DiscrepanciesPanel from "../components/DiscrepanciesPanel";
import AuditLogPanel from "../components/AuditLogPanel";

export default function AuditPage() {
  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Audit & Compliance</h1>
          <p className="cg-page__subtitle">
            Verification campaigns, discrepancies and the audit log.
          </p>
        </div>
      </div>

      <Tabs>
        <TabList aria-label="Audit sections">
          <Tab>Verification Campaigns</Tab>
          <Tab>Discrepancies</Tab>
          <Tab>Audit Log</Tab>
        </TabList>
        <TabPanels>
          <TabPanel>
            <CampaignsPanel />
          </TabPanel>
          <TabPanel>
            <DiscrepanciesPanel />
          </TabPanel>
          <TabPanel>
            <AuditLogPanel />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  );
}
