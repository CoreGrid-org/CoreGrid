import { Route, Routes } from "react-router-dom";
import RoleRoute from "@/features/auth/components/RoleRoute";
import SignIn from "@/features/auth/pages/SignIn";
import ForgotPassword from "@/features/auth/pages/ForgotPassword";
import AccessRestricted from "@/features/auth/pages/AccessRestricted";
import Setup from "@/features/setup/pages/Setup";
import Dashboard from "@/features/dashboard/pages/Dashboard";
import AdminDashboard from "@/features/dashboard/pages/AdminDashboard";
import AdminLayout from "@/features/dashboard/components/AdminLayout";
import InventoryDashboard from "@/features/dashboard/pages/InventoryDashboard";
import AuditDashboard from "@/features/dashboard/pages/AuditDashboard";
import StaffDashboard from "@/features/dashboard/pages/StaffDashboard";
import UsersPage from "@/features/users/pages/UsersPage";
import AssetsPage from "@/features/assets/pages/AssetsPage";
import MaintenancePage from "@/features/maintenance/pages/MaintenancePage";
import TransfersPage from "@/features/transfers/pages/TransfersPage";
import AuditPage from "@/features/audit/pages/AuditPage";
import WorkflowsPage from "@/features/workflows/pages/WorkflowsPage";
import ReportsPage from "@/features/reports/pages/ReportsPage";
import SettingsPage from "@/features/settings/pages/SettingsPage";
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
        <Route path="maintenance" element={<MaintenancePage />} />
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
            <InventoryDashboard />
          </RoleRoute>
        }
      />
      <Route
        path="audit"
        element={
          <RoleRoute role="Auditor">
            <AuditDashboard />
          </RoleRoute>
        }
      />
      <Route
        path="staff"
        element={
          <RoleRoute role="Staff">
            <StaffDashboard />
          </RoleRoute>
        }
      />
      <Route path="signin" element={<SignIn />} />
      <Route path="setup" element={<Setup />} />
      <Route path="forgot-password" element={<ForgotPassword />} />
      <Route path="access-restricted" element={<AccessRestricted />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}
