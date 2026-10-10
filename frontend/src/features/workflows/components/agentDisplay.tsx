import { agentDisplay } from "../lib/agentDisplay";

export function AgentDisplay({ agent, size = 16 }: { agent: string; size?: number }) {
  const { label, icon: Icon } = agentDisplay(agent);
  return (
    <>
      <Icon size={size} /> {label}
    </>
  );
}
