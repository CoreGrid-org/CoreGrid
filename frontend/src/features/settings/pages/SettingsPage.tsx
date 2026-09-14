import { Tabs, TabList, Tab, TabPanels, TabPanel } from "@carbon/react";
import DepartmentsPanel from "../components/DepartmentsPanel";
import LocationsPanel from "../components/LocationsPanel";
import PolicyParametersPanel from "../components/PolicyParametersPanel";
import AboutPanel from "../components/AboutPanel";

export default function SettingsPage() {
  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Organisation Settings</h1>
          <p className="cg-page__subtitle">Departments, locations and policy thresholds.</p>
        </div>
      </div>

      <Tabs>
        <TabList aria-label="Organisation settings sections">
          <Tab>Departments</Tab>
          <Tab>Locations</Tab>
          <Tab>Policy Parameters</Tab>
          <Tab>About</Tab>
        </TabList>
        <TabPanels>
          <TabPanel>
            <DepartmentsPanel />
          </TabPanel>
          <TabPanel>
            <LocationsPanel />
          </TabPanel>
          <TabPanel>
            <PolicyParametersPanel />
          </TabPanel>
          <TabPanel>
            <AboutPanel />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>
  );
}
