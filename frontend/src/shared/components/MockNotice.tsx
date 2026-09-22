import { InlineNotification } from "@carbon/react";

interface MockNoticeProps {
  /** What the real, wired-up version of this tab will do once its backend exists. */
  children: string;
}


export default function MockNotice({ children }: MockNoticeProps) {
  return (
    <InlineNotification
      kind="info"
      lowContrast
      hideCloseButton
      title="Mock data: not wired to the backend yet"
      subtitle={children}
      style={{ marginBottom: "1rem", maxWidth: "100%" }}
    />
  );
}
