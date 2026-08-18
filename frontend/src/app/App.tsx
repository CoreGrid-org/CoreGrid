import { Route, Routes } from "react-router-dom";
import RoleRoute from "@/features/auth/components/RoleRoute";
import SignIn from "@/features/auth/pages/SignIn";
import ForgotPassword from "@/features/auth/pages/ForgotPassword";
import AccessRestricted from "@/features/auth/pages/AccessRestricted";
import Setup from "@/features/setup/pages/Setup";
import Dashboard from "@/features/dashboard/pages/Dashboard";
import AdminDashboard from "@/features/dashboard/pages/AdminDashboard";
import AdminLayout from "@/features/dashboard/components/AdminLayout";
import InventoryLayout from "@/features/dashboard/components/InventoryLayout";
import AuditLayout from "@/features/dashboard/components/AuditLayout";
import StaffLayout from "@/features/dashboard/components/StaffLayout";
import InventoryDashboard from "@/features/dashboard/pages/InventoryDashboard";
import AuditDashboard from "@/features/dashboard/pages/AuditDashboard";
import StaffDashboard from "@/features/dashboard/pages/StaffDashboard";
import UsersPage from "@/features/users/pages/UsersPage";
import AssetsPage from "@/features/assets/pages/AssetsPage";
import AssetRegisterPage from "@/features/assets/pages/AssetRegisterPage";
import AssetScanPage from "@/features/assets/pages/AssetScanPage";
import AssetConfigPage from "@/features/assets/pages/AssetConfigPage";
import MaintenancePage from "@/features/maintenance/pages/MaintenancePage";
import MaintenanceDetailPage from "@/features/maintenance/pages/MaintenanceDetailPage";
import CreateMaintenancePage from "@/features/maintenance/pages/CreateMaintenancePage";
import ReportFaultPage from "@/features/maintenance/pages/ReportFaultPage";
import TransfersPage from "@/features/transfers/pages/TransfersPage";
import AuditPage from "@/features/audit/pages/AuditPage";
import WorkflowsPage from "@/features/workflows/pages/WorkflowsPage";
import ReportsPage from "@/features/reports/pages/ReportsPage";
import SettingsPage from "@/features/settings/pages/SettingsPage";
import ComingSoon from "@/shared/components/ComingSoon";
import NotFound from "@/shared/pages/NotFound";

export default function App() {
  return (
    <Routes>
      <Route index element={<Dashboard />} />

      <Route
        path="admin"
        element={
          <RoleRoute role="Administrator">
            <AdminLayout />
          </RoleRoute>
        }
      >
        <Route index element={<AdminDashboard />} />
        <Route path="assets" element={<AssetsPage />} />
        <Route path="assets/new" element={<AssetRegisterPage />} />
        <Route path="assets/:id/edit" element={<AssetRegisterPage />} />
        <Route path="assets/scan" element={<AssetScanPage />} />
        <Route path="assets/config" element={<AssetConfigPage />} />
        <Route path="maintenance" element={<MaintenancePage />} />
        <Route path="maintenance/new" element={<CreateMaintenancePage />} />
        <Route path="maintenance/:id" element={<MaintenanceDetailPage />} />
        <Route path="transfers" element={<TransfersPage />} />
        <Route path="audit" element={<AuditPage />} />
        <Route path="workflows" element={<WorkflowsPage />} />
        <Route path="users" element={<UsersPage />} />
        <Route path="reports" element={<ReportsPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>

      <Route
        path="inventory"
        element={
          <RoleRoute role="InventoryOfficer">
            <InventoryLayout />
          </RoleRoute>
        }
      >
        <Route index element={<InventoryDashboard />} />
        <Route path="reports" element={<ReportsPage />} />
        <Route path="assets" element={<AssetsPage />} />
        <Route path="assets/new" element={<AssetRegisterPage />} />
        <Route path="assets/:id/edit" element={<AssetRegisterPage />} />
        <Route path="assets/scan" element={<AssetScanPage />} />
        <Route path="maintenance" element={<MaintenancePage />} />
        <Route path="maintenance/new" element={<CreateMaintenancePage />} />
        <Route path="maintenance/:id" element={<MaintenanceDetailPage />} />
        <Route path="transfers" element={<ComingSoon feature="Transfers & Disposals" />} />
        <Route path="workflows" element={<ComingSoon feature="Agentic Workflows" />} />
      </Route>

      <Route
        path="audit"
        element={
          <RoleRoute role="Auditor">
            <AuditLayout />
          </RoleRoute>
        }
      >
        <Route index element={<AuditDashboard />} />
        <Route path="audit" element={<AuditPage />} />
        <Route path="reports" element={<ReportsPage />} />
        <Route path="assets" element={<ComingSoon feature="Asset Registry" />} />
        <Route path="maintenance" element={<MaintenancePage />} />
        <Route path="maintenance/:id" element={<MaintenanceDetailPage />} />
        <Route path="transfers" element={<ComingSoon feature="Transfers & Disposals" />} />
        <Route path="workflows" element={<ComingSoon feature="Agentic Workflows" />} />
      </Route>

      <Route
        path="staff"
        element={
          <RoleRoute role="Staff">
            <StaffLayout />
          </RoleRoute>
        }
      >
        <Route index element={<StaffDashboard />} />
        <Route path="assets" element={<ComingSoon feature="My Assets" />} />
        <Route path="maintenance" element={<MaintenancePage />} />
        <Route path="maintenance/report" element={<ReportFaultPage />} />
        <Route path="maintenance/:id" element={<MaintenanceDetailPage />} />
      </Route>
      <Route path="signin" element={<SignIn />} />
      <Route path="setup" element={<Setup />} />
      <Route path="forgot-password" element={<ForgotPassword />} />
      <Route path="access-restricted" element={<AccessRestricted />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}
