import { Link } from "react-router-dom";
import type { ComponentType } from "react";

interface FeatureCardProps {
  to: string;
  icon: ComponentType<{ size?: number; className?: string }>;
  title: string;
  description: string;
}

export default function FeatureCard({ to, icon: Icon, title, description }: FeatureCardProps) {
  return (
    <Link to={to} className="cg-quick-card">
      <Icon size={24} className="cg-quick-card__icon" />
      <p className="cg-quick-card__title">{title}</p>
      <p className="cg-quick-card__description">{description}</p>
    </Link>
  );
}
