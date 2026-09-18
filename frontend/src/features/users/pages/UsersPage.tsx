import { useEffect, useState } from "react";
import { Button, InlineNotification, Pagination, Tag } from "@carbon/react";
import { Add, Edit, Search } from "@carbon/icons-react";
import { useUsersPage, useSetUserActive } from "../hooks/useUsers";
import { useDepartments } from "@/features/assets/hooks/useAssets";
import CreateUserModal from "../components/CreateUserModal";
import EditUserModal from "../components/EditUserModal";
import { getRoleLabel } from "@/features/auth/lib/roles";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import type { CoreGridUser } from "../services/users";

// FR-013/FR-014: invite a user by email and role, provisioned through
// ThunderID; change an existing user's role/department or deactivate them
// (never hard-deleted).
export default function UsersPage() {
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  useEffect(() => {
    const timer = setTimeout(() => setSearch(searchInput.trim()), 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    setPage(1);
  }, [search]);

  const { data: usersPage, isLoading, isError, error, refetch } = useUsersPage({
    search: search || undefined,
    page,
    pageSize,
  });
  const users = usersPage.items;
  const departments = useDepartments();
  const setUserActive = useSetUserActive();

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<CoreGridUser | null>(null);

  const departmentName = (id: string | null) => departments.data?.find((d) => d.id === id)?.name ?? "—";

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

      {setUserActive.isError && (
        <InlineNotification
          kind="error"
          title="Could not update user"
          subtitle={getErrorMessage(
            setUserActive.error,
            "It may be the organisation's last active Administrator.",
          )}
          lowContrast
          hideCloseButton
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

      <div className="cg-section">
        <div className="cg-toolbar" style={{ marginBottom: "1rem" }}>
          <div className="cg-search" style={{ minWidth: "18rem" }}>
            <Search size={16} className="cg-search__icon" />
            <input
              className="cg-search__input"
              placeholder="Search by name or email…"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              aria-label="Search users"
            />
          </div>
        </div>
        {isLoading ? (
          <div className="cg-placeholder">
            <p>Loading users…</p>
          </div>
        ) : users.length > 0 ? (
          <table className="cg-table cg-table--no-hover">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Department</th>
                <th>Status</th>
                <th>Joined</th>
                <th></th>
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
                  <td className="cg-table__muted">{departmentName(u.department_id)}</td>
                  <td>
                    <Tag type={u.is_active ? "green" : "gray"}>{u.is_active ? "Active" : "Inactive"}</Tag>
                  </td>
                  <td className="cg-table__muted">{new Date(u.created_at).toLocaleDateString()}</td>
                  <td style={{ display: "flex", gap: "0.5rem", justifyContent: "flex-end" }}>
                    <Button kind="ghost" size="sm" onClick={() => setEditingUser(u)}>
                      <Edit size={16} />
                    </Button>
                    <Button
                      kind="ghost"
                      size="sm"
                      disabled={setUserActive.isPending}
                      onClick={() =>
                        setUserActive.mutate({ id: u.id, isActive: !u.is_active }, { onSuccess: refetch })
                      }
                    >
                      {u.is_active ? "Deactivate" : "Activate"}
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <div className="cg-placeholder">
            <p>{search ? "No users match this search." : "No users yet. Add the first one to get started."}</p>
          </div>
        )}
      </div>

      {usersPage.total_count > 0 && (
        <Pagination
          page={page}
          pageSize={pageSize}
          pageSizes={[10, 20, 50, 100]}
          totalItems={usersPage.total_count}
          onChange={({ page: nextPage, pageSize: nextPageSize }) => {
            setPage(nextPage);
            setPageSize(nextPageSize);
          }}
        />
      )}

      {isAddOpen && (
        <CreateUserModal
          onClose={() => setIsAddOpen(false)}
          onCreated={() => {
            setIsAddOpen(false);
            refetch();
          }}
        />
      )}

      {editingUser && (
        <EditUserModal
          user={editingUser}
          departments={departments.data ?? []}
          onClose={() => setEditingUser(null)}
          onSaved={() => {
            setEditingUser(null);
            refetch();
          }}
        />
      )}
    </div>
  );
}
