import { useThunderID } from "@thunderid/react";
import { Button } from "@carbon/react";
import { Locked } from "@carbon/icons-react";
import StatusView from "@/shared/components/StatusView";
import { useMe } from "../hooks/useMe";

// Staff are mobile-only (SRS §3.4.1), so a Staff account lands here by
// design — say so, rather than implying their account is misconfigured.
export default function AccessRestricted() {
  const { signOut } = useThunderID();
  const { data: me } = useMe();
  const isStaff = me?.role === "Staff";

  return (
    <StatusView
      variant="fullscreen"
      icon={Locked}
      title={isStaff ? "Use the CoreGrid mobile app" : "Access restricted"}
      subtitle={
        isStaff
          ? "Staff accounts use the CoreGrid mobile app to look up assets and report faults. The web portal is for Inventory Officers, Auditors and Administrators."
          : "Your account doesn't have access to this portal yet. If you think this is a mistake, contact your organisation administrator."
      }
      actions={
        <Button kind="tertiary" onClick={() => signOut()}>
          Sign out
        </Button>
      }
    />
  );
}
