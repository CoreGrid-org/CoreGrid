import type { ComponentType, ReactNode } from "react";

interface StatusViewProps {
  icon: ComponentType<{ size?: number }>;
  title: string;
  subtitle: string;
  actions?: ReactNode;
  variant?: "inline" | "fullscreen";
}

export default function StatusView({ icon: Icon, title, subtitle, actions, variant = "inline" }: StatusViewProps) {
  const body = (
    <div className={variant === "fullscreen" ? "cg-status-card" : "cg-status"}>
      <div className="cg-status__icon">
        <Icon size={28} />
      </div>
      <h1 className="cg-status__title">{title}</h1>
      <p className="cg-status__subtitle">{subtitle}</p>
      {actions && <div className="cg-status__actions">{actions}</div>}
    </div>
  );

  if (variant === "fullscreen") {
    return <div className="cg-signin-wrapper">{body}</div>;
  }

  return body;
}
