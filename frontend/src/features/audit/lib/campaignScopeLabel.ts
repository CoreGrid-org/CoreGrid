export interface CampaignScope {
  scope_department_name: string | null;
  scope_location_name: string | null;
  scope_asset_category_name: string | null;
  scope_asset_type_name: string | null;
}

export function campaignScopeLabel(c: CampaignScope): string {
  const parts = [c.scope_department_name, c.scope_location_name, c.scope_asset_category_name, c.scope_asset_type_name].filter(
    (p): p is string => !!p,
  );
  return parts.length > 0 ? parts.join(" · ") : "Whole register";
}
