import { DatePicker, DatePickerInput } from "@carbon/react";
import { toDateOnly, type DateRange, type DateRangeErrors } from "@/shared/lib/dates";

interface DateRangeFilterProps {
  idPrefix: string;
  value: DateRange;
  onChange: (range: DateRange) => void;
  /** From validateDateRange() — the parent decides whether to apply the range. */
  errors: DateRangeErrors;
  fromLabel?: string;
  toLabel?: string;
  allowFuture?: boolean;
}

// A controlled pair of date pickers. The calendars themselves block future
// days and an end before the start; `errors` covers dates typed by hand.
export default function DateRangeFilter({
  idPrefix,
  value,
  onChange,
  errors,
  fromLabel = "From",
  toLabel = "To",
  allowFuture = false,
}: DateRangeFilterProps) {
  const today = allowFuture ? undefined : toDateOnly();

  return (
    <>
      <DatePicker
        // Remount when cleared from outside (e.g. "Clear filters") — Carbon's
        // picker doesn't reliably empty itself when `value` goes blank.
        key={value.from ? "from-set" : "from-empty"}
        className="cg-date-range__picker"
        datePickerType="single"
        dateFormat="Y-m-d"
        allowInput
        value={value.from ?? ""}
        maxDate={value.to ?? today}
        onChange={([date]) => onChange({ ...value, from: date ? toDateOnly(date) : undefined })}
      >
        <DatePickerInput
          id={`${idPrefix}-from`}
          labelText={fromLabel}
          placeholder="yyyy-mm-dd"
          invalid={Boolean(errors.from)}
          invalidText={errors.from}
        />
      </DatePicker>
      <DatePicker
        key={value.to ? "to-set" : "to-empty"}
        className="cg-date-range__picker"
        datePickerType="single"
        dateFormat="Y-m-d"
        allowInput
        value={value.to ?? ""}
        minDate={value.from}
        maxDate={today}
        onChange={([date]) => onChange({ ...value, to: date ? toDateOnly(date) : undefined })}
      >
        <DatePickerInput
          id={`${idPrefix}-to`}
          labelText={toLabel}
          placeholder="yyyy-mm-dd"
          invalid={Boolean(errors.to)}
          invalidText={errors.to}
        />
      </DatePicker>
    </>
  );
}
