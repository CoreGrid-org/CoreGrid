import { InlineNotification } from "@carbon/react";

interface MockNoticeProps {
  /** What the real, wired-up version of this tab will do once its backend exists. */
  children: string;
  /** Requirement identifiers this tab implements, e.g. ["FR-021", "FR-022"]. */
  requirements?: string[];
}

// Every tab below this notice is static mock data — there is no API behind
// it yet (see doc/PROGRESS.md). This banner is the one place that explains,
// right where a reviewer or teammate is looking, what the tab is a stand-in
// for and which SRS requirement it will eventually satisfy.
export default function MockNotice({ children, requirements }: MockNoticeProps) {
  const subtitle = requirements?.length ? `${children} (${requirements.join(", ")})` : children;

  return (
    <InlineNotification
      kind="info"
      lowContrast
      hideCloseButton
      title="Mock data — not wired to the backend yet"
      subtitle={subtitle}
      style={{ marginBottom: "1rem", maxWidth: "100%" }}
    />
  );
}
