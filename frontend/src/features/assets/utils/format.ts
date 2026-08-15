export function formatCurrency(amount: number): string {
  return `$${amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function formatDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString();
}

export function formatAttributeValue(attribute: {
  data_type: string;
  value_text: string | null;
  value_number: number | null;
  value_date: string | null;
  value_boolean: boolean | null;
}): string {
  switch (attribute.data_type) {
    case "NUMBER":
      return attribute.value_number !== null ? attribute.value_number.toLocaleString() : "—";
    case "DATE":
      return attribute.value_date ? formatDate(attribute.value_date) : "—";
    case "BOOLEAN":
      return attribute.value_boolean === null ? "—" : attribute.value_boolean ? "Yes" : "No";
    default:
      return attribute.value_text ?? "—";
  }
}
