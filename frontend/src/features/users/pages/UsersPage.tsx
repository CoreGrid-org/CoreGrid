import { useState } from "react";
import { Button, InlineNotification, Tag } from "@carbon/react";
import { Add } from "@carbon/icons-react";
import { useUsersList } from "../hooks/useUsers";
import CreateUserModal from "../components/CreateUserModal";
import { getRoleLabel } from "@/features/auth/lib/roles";
import { getErrorMessage } from "@/shared/lib/errorMessage";

// The one real feature on the Admin Dashboard today — everything else there
// is mock (FR-013: invite a user by email and role, provisioning through
// ThunderID).
export default function UsersPage() {
  const { data: users, isLoading, isError, error, refetch } = useUsersList();
  const [isAddOpen, setIsAddOpen] = useState(false);

  return (
    <div className="cg-page">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">Users & Roles</h1>
          <p className="cg-page__subtitle">Invite users into your organisation and manage their roles.</p>
        </div>
        <Button renderIcon={Add} onClick={() => setIsAddOpen(true)}>
          Add user
        </Button>
      </div>

      {isError && (
        <InlineNotification
          kind="error"
          title="Could not load users"
          subtitle={getErrorMessage(error, "Something went wrong. Please try again.")}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div className="cg-section">
        {isLoading ? (
          <div className="cg-placeholder">
            <p>Loading users…</p>
          </div>
        ) : users && users.length > 0 ? (
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Status</th>
                <th>Joined</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id}>
                  <td>
                    {u.given_name} {u.family_name}
                  </td>
                  <td className="cg-table__muted">{u.email}</td>
                  <td>
                    <Tag type="blue">{getRoleLabel(u.role)}</Tag>
                  </td>
                  <td>
                    <Tag type={u.is_active ? "green" : "gray"}>{u.is_active ? "Active" : "Inactive"}</Tag>
                  </td>
                  <td className="cg-table__muted">{new Date(u.created_at).toLocaleDateString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <div className="cg-placeholder">
            <p>No users yet. Add the first one to get started.</p>
          </div>
        )}
      </div>

      {isAddOpen && (
        <CreateUserModal
          onClose={() => setIsAddOpen(false)}
          onCreated={() => {
            setIsAddOpen(false);
            refetch();
          }}
        />
      )}
    </div>
  );
}
