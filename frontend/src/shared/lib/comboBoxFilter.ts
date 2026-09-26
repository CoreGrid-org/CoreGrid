type FilterArgs<T> = {
  item: T;
  itemToString?: (item: T | null) => string;
  inputValue: string | null;
};

// Carbon's ComboBox doesn't narrow its list as the user types unless a
// `shouldFilterItem` is passed. This builds one that does case-insensitive
// "contains" matching. Pass the currently selected item: while the input
// still shows exactly that item's label (the menu was re-opened after a
// pick), every item is listed so the user can switch without clearing the
// field first.
export function comboBoxFilter<T>(selectedItem: T | null | undefined) {
  return ({ item, itemToString, inputValue }: FilterArgs<T>): boolean => {
    const toLabel = (value: T | null) => (itemToString ? itemToString(value) : String(value ?? ""));
    const query = (inputValue ?? "").trim().toLowerCase();
    if (!query) return true;
    if (selectedItem != null && toLabel(selectedItem).trim().toLowerCase() === query) return true;
    return toLabel(item).toLowerCase().includes(query);
  };
}
