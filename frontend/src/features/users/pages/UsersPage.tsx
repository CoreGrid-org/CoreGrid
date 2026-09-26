import { useEffect, useState } from "react";
import { Button, InlineNotification, Modal, OverflowMenu, OverflowMenuItem, Pagination, Search, Tag } from "@carbon/react";
import { Add, UserMultiple } from "@carbon/icons-react";
import { useUsersPage, useSetUserActive } from "../hooks/useUsers";
import { useDepartments } from "@/features/assets/hooks/useAssets";
import { useMe } from "@/features/auth/hooks/useMe";
import CreateUserModal from "../components/CreateUserModal";
import EditUserModal from "../components/EditUserModal";
import UserIdentity from "../components/UserIdentity";
import { getRoleLabel } from "@/features/auth/lib/roles";
import { getErrorMessage } from "@/shared/lib/errorMessage";
import { formatDate } from "@/shared/lib/dates";
import { ASSIGNABLE_ROLES, ROLE_INFO } from "../lib/roles";
import type { CoreGridUser } from "../services/users";

// Invite users by email and role (provisioned through ThunderID), change a
// user's role or department, or deactivate them (never hard-deleted).
export default function UsersPage() {
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [paging, setPaging] = useState({ page: 1, pageSize: 20 });

  // Debounce typing; a new search starts from the first page.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPaging((p) => ({ ...p, page: 1 }));
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const { data: usersPage, isLoading, isError, error, refetch } = useUsersPage({ search: search || undefined, ...paging });
  const users = usersPage.items;
  const departments = useDepartments();
  const { data: me } = useMe();
  const setUserActive = useSetUserActive();

  const [isAddOpen, setIsAddOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<CoreGridUser | null>(null);
  const [statusTarget, setStatusTarget] = useState<CoreGridUser | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const departmentName = (id: string | null) => departments.data?.find((d) => d.id === id)?.name ?? "-";
  const isSelf = (u: CoreGridUser) => u.id === me?.id;

  const confirmStatusChange = () => {
    if (!statusTarget || setUserActive.isPending) return;
    const target = statusTarget;
    setUserActive.mutate(
      { id: target.id, isActive: !target.is_active },
      {
        onSuccess: () => {
          setStatusTarget(null);
          setNotice(`${target.given_name} ${target.family_name} was ${target.is_active ? "deactivated" : "reactivated"}.`);
          refetch();
        },
      },
    );
  };

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

      {notice && (
        <InlineNotification
          kind="success"
          title={notice}
          lowContrast
          onClose={() => setNotice(null)}
          style={{ marginBottom: "1rem", maxWidth: "100%" }}
        />
      )}

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

      <div className="cg-users">
        <section className="cg-section cg-users__list">
          <header className="cg-section__header cg-users__toolbar">
            <div>
              <p className="cg-section__title">People</p>
              <p className="cg-users__count">
                {isLoading ? "Loading…" : `${usersPage.total_count.toLocaleString()} ${search ? "matching" : "in your organisation"}`}
              </p>
            </div>
            <Search
              id="users-search"
              size="md"
              labelText="Search users"
              placeholder="Search by name or email"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              className="cg-users__search"
            />
          </header>

          {isLoading && users.length === 0 ? (
            <div className="cg-placeholder">
              <p>Loading users…</p>
            </div>
          ) : users.length > 0 ? (
            <div style={{ overflowX: "auto" }}>
              <table className="cg-table cg-table--no-hover">
                <thead>
                  <tr>
                    <th>User</th>
                    <th>Role</th>
                    <th>Department</th>
                    <th>Status</th>
                    <th>Joined</th>
                    <th aria-label="Actions" />
                  </tr>
                </thead>
                <tbody>
                  {users.map((u) => (
                    <tr key={u.id}>
                      <td>
                        <UserIdentity user={u} isSelf={isSelf(u)} />
                      </td>
                      <td>
                        <Tag type={ROLE_INFO[u.role].tag}>{getRoleLabel(u.role)}</Tag>
                      </td>
                      <td className="cg-table__muted">{departmentName(u.department_id)}</td>
                      <td>
                        <Tag type={u.is_active ? "green" : "gray"}>{u.is_active ? "Active" : "Inactive"}</Tag>
                      </td>
                      <td className="cg-table__muted">{formatDate(u.created_at)}</td>
                      <td style={{ textAlign: "right" }}>
                        <OverflowMenu aria-label={`Actions for ${u.given_name} ${u.family_name}`} flipped size="sm">
                          <OverflowMenuItem itemText="Edit role & department" onClick={() => setEditingUser(u)} />
                          {!isSelf(u) && (
                            <OverflowMenuItem
                              itemText={u.is_active ? "Deactivate" : "Reactivate"}
                              isDelete={u.is_active}
                              hasDivider
                              onClick={() => {
                                setUserActive.reset();
                                setStatusTarget(u);
                              }}
                            />
                          )}
                        </OverflowMenu>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="cg-placeholder">
              <UserMultiple size={32} />
              <p>{search ? `No users match "${search}".` : "No users yet. Add the first one to get started."}</p>
            </div>
          )}

          {usersPage.total_count > 0 && (
            <Pagination
              page={paging.page}
              pageSize={paging.pageSize}
              pageSizes={[10, 20, 50, 100]}
              totalItems={usersPage.total_count}
              onChange={({ page, pageSize }) => setPaging({ page, pageSize })}
            />
          )}
        </section>

        <aside className="cg-section cg-users__roles">
          <header className="cg-section__header">
            <p className="cg-section__title">Roles</p>
          </header>
          <ul className="cg-section__body cg-users__role-list">
            {ASSIGNABLE_ROLES.map((role) => (
              <li key={role}>
                <Tag type={ROLE_INFO[role].tag} size="sm">
                  {getRoleLabel(role)}
                </Tag>
                <p>{ROLE_INFO[role].summary}</p>
              </li>
            ))}
          </ul>
        </aside>
      </div>

      {isAddOpen && (
        <CreateUserModal
          onClose={() => setIsAddOpen(false)}
          onCreated={(user) => {
            setIsAddOpen(false);
            setNotice(`${user.given_name} ${user.family_name} was added as ${getRoleLabel(user.role)}.`);
            refetch();
          }}
        />
      )}

      {editingUser && (
        <EditUserModal
          user={editingUser}
          departments={departments.data ?? []}
          isSelf={isSelf(editingUser)}
          onClose={() => setEditingUser(null)}
          onSaved={() => {
            setNotice(`${editingUser.given_name} ${editingUser.family_name} was updated.`);
            setEditingUser(null);
            refetch();
          }}
        />
      )}

      {statusTarget && (
        <Modal
          open
          size="sm"
          danger={statusTarget.is_active}
          modalLabel="Users & Roles"
          modalHeading={statusTarget.is_active ? "Deactivate user?" : "Reactivate user?"}
          primaryButtonText={
            setUserActive.isPending ? "Saving…" : statusTarget.is_active ? "Deactivate" : "Reactivate"
          }
          secondaryButtonText="Cancel"
          primaryButtonDisabled={setUserActive.isPending}
          onRequestClose={() => setStatusTarget(null)}
          onRequestSubmit={confirmStatusChange}
        >
          <div className="cg-modal-form">
            <UserIdentity user={statusTarget} isSelf={false} />
            <p className="cg-modal-form__intro">
              {statusTarget.is_active
                ? "They won't be able to sign in until reactivated. Their history and records are kept."
                : "They'll be able to sign in again with their existing account and role."}
            </p>
            {setUserActive.isError && (
              <InlineNotification
                kind="error"
                title={statusTarget.is_active ? "Could not deactivate" : "Could not reactivate"}
                subtitle={getErrorMessage(setUserActive.error, "It may be the organisation's last active Administrator.")}
                lowContrast
                hideCloseButton
                style={{ maxWidth: "100%" }}
              />
            )}
          </div>
        </Modal>
      )}
    </div>
  );
}
