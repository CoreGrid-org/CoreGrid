import { Button, InlineNotification, Tag } from "@carbon/react";
import { Copy, Email, Locked, UserAvatar } from "@carbon/icons-react";
import { SignOutButton } from "@thunderid/react";
import { useState } from "react";
import { useMe } from "@/features/auth/hooks/useMe";
import { getRoleLabel } from "@/features/auth/lib/roles";
import { getErrorMessage } from "@/shared/lib/errorMessage";

function initials(givenName: string, familyName: string) {
  return `${givenName.charAt(0)}${familyName.charAt(0)}`.toUpperCase() || "U";
}

export default function ProfilePage() {
  const { data: me, isLoading, isError, error } = useMe();
  const [copied, setCopied] = useState(false);

  const copyAccountId = async () => {
    if (!navigator.clipboard) return;
    await navigator.clipboard.writeText(me?.id ?? "");
    setCopied(true);
    window.setTimeout(() => setCopied(false), 2000);
  };

  if (isLoading) {
    return (
      <div className="cg-page">
        <div className="cg-page__header"><div className="cg-page__header-left"><h1 className="cg-page__title">My Profile</h1><p className="cg-page__subtitle">Loading your account details…</p></div></div>
      </div>
    );
  }

  if (isError || !me) {
    return (
      <div className="cg-page">
        <div className="cg-page__header"><div className="cg-page__header-left"><h1 className="cg-page__title">My Profile</h1></div></div>
        <InlineNotification kind="error" title="Could not load your profile" subtitle={getErrorMessage(error, "Please refresh the page and try again.")} lowContrast hideCloseButton />
      </div>
    );
  }

  const fullName = `${me.given_name} ${me.family_name}`.trim();

  return (
    <div className="cg-page cg-user-profile">
      <div className="cg-page__header">
        <div className="cg-page__header-left">
          <h1 className="cg-page__title">My Profile</h1>
          <p className="cg-page__subtitle">Your CoreGrid identity, access and account security.</p>
        </div>
      </div>

      <section className="cg-section cg-user-profile__hero" aria-label="Profile summary">
        <div className="cg-profile__banner">
          <div className="cg-profile__avatar" aria-hidden="true">{initials(me.given_name, me.family_name)}</div>
          <div>
            <h2 className="cg-profile__name">{fullName || "CoreGrid user"}</h2>
            <p className="cg-profile__meta cg-user-profile__summary-line">
              <span>{me.email}</span>
              <span aria-hidden="true">•</span>
              <span>{me.organization_name || "Organisation unavailable"}</span>
            </p>
          </div>
          <div className="cg-user-profile__badges">
            <Tag type="blue">{getRoleLabel(me.role)}</Tag>
            <Tag type={me.is_active ? "green" : "gray"}>{me.is_active ? "Active account" : "Inactive account"}</Tag>
          </div>
        </div>
      </section>

      <div className="cg-user-profile__grid">
        <section className="cg-section" aria-labelledby="identity-heading">
          <div className="cg-section__header"><h2 id="identity-heading" className="cg-section__title">Identity</h2></div>
          <div className="cg-kv-grid">
            <div className="cg-kv-item"><p className="cg-kv-item__label">First name</p><p className="cg-kv-item__value">{me.given_name || "—"}</p></div>
            <div className="cg-kv-item"><p className="cg-kv-item__label">Last name</p><p className="cg-kv-item__value">{me.family_name || "—"}</p></div>
            <div className="cg-kv-item cg-user-profile__email"><p className="cg-kv-item__label">Work email</p><p className="cg-kv-item__value"><Email size={16} />{me.email}</p></div>
            <div className="cg-kv-item"><p className="cg-kv-item__label">Organisation</p><p className="cg-kv-item__value">{me.organization_name || "—"}</p></div>
            <div className="cg-kv-item"><p className="cg-kv-item__label">Account ID</p><div className="cg-user-profile__id"><code>{me.id}</code><Button kind="ghost" size="sm" renderIcon={Copy} iconDescription={copied ? "Account ID copied" : "Copy account ID"} hasIconOnly onClick={copyAccountId} /></div></div>
          </div>
        </section>

        <section className="cg-section" aria-labelledby="access-heading">
          <div className="cg-section__header"><h2 id="access-heading" className="cg-section__title">Access & security</h2></div>
          <div className="cg-user-profile__security">
            <div className="cg-user-profile__security-icon"><Locked size={24} /></div>
            <div>
              <h3>Account security is managed by ThunderID</h3>
              <p>Your sign-in method and password are protected by your organisation’s identity provider. Contact an administrator if your name, email or access role needs to change.</p>
            </div>
          </div>
          <div className="cg-kv-grid">
            <div className="cg-kv-item"><p className="cg-kv-item__label">Assigned role</p><p className="cg-kv-item__value">{getRoleLabel(me.role)}</p></div>
            <div className="cg-kv-item"><p className="cg-kv-item__label">Account status</p><p className="cg-kv-item__value">{me.is_active ? "Enabled" : "Disabled"}</p></div>
          </div>
          <div className="cg-user-profile__actions">
            <SignOutButton>{({ signOut }) => <Button kind="secondary" renderIcon={UserAvatar} onClick={() => signOut()}>Sign out</Button>}</SignOutButton>
          </div>
        </section>
      </div>
    </div>
  );
}
