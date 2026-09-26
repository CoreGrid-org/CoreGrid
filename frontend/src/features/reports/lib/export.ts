import { jsPDF } from "jspdf";
import { formatDateTime } from "@/shared/lib/dates";

export interface ExportColumn<T> {
  header: string;
  /** Column width in the PDF, in mm (landscape A4 fits ~277mm). */
  width: number;
  value: (row: T) => string | number;
}

export interface ReportExport<T> {
  title: string;
  /** Base of the downloaded file name, e.g. "maintenance-report". */
  fileName: string;
  columns: ExportColumn<T>[];
  rows: T[];
  /** Headline figures printed under the PDF title, e.g. "Total cost: LKR 12,000". */
  summary: string[];
  /** Human-readable active filters, printed in the PDF so it's clear what it covers. */
  filters: string[];
}

export function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-LK", { style: "currency", currency: "LKR", maximumFractionDigits: 0 }).format(value);
}

const datedFileName = (base: string, extension: string) => `${base}-${new Date().toISOString().slice(0, 10)}.${extension}`;

function csvEscape(value: string | number) {
  const text = String(value);
  return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

function triggerDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

export function downloadCsv<T>({ fileName, columns, rows }: ReportExport<T>) {
  const lines = [columns.map((c) => c.header), ...rows.map((row) => columns.map((c) => c.value(row)))];
  const csv = lines.map((line) => line.map(csvEscape).join(",")).join("\n");
  // Leading BOM so Excel opens it as UTF-8.
  triggerDownload(new Blob([`\uFEFF${csv}`], { type: "text/csv;charset=utf-8" }), datedFileName(fileName, "csv"));
}

export function downloadPdf<T>({ title, fileName, columns, rows, summary, filters }: ReportExport<T>) {
  const pdf = new jsPDF({ orientation: "landscape", unit: "mm", format: "a4" });
  const pageHeight = pdf.internal.pageSize.getHeight();
  const left = 10;
  const tableWidth = columns.reduce((sum, c) => sum + c.width, 0);
  let y = 15;

  const drawHeader = () => {
    pdf.setFillColor(224, 224, 224);
    pdf.rect(left, y - 5, tableWidth, 8, "F");
    pdf.setFont("helvetica", "bold");
    pdf.setFontSize(6.5);
    let x = left + 1;
    for (const column of columns) {
      pdf.text(column.header, x, y);
      x += column.width;
    }
    y += 7;
    pdf.setFont("helvetica", "normal");
  };

  pdf.setFont("helvetica", "bold");
  pdf.setFontSize(16);
  pdf.text(title, left, y);
  y += 6;
  pdf.setFont("helvetica", "normal");
  pdf.setFontSize(8);
  pdf.text(`Generated ${formatDateTime(new Date())}`, left, y);
  y += 5;
  pdf.text(`Filters: ${filters.length > 0 ? filters.join("; ") : "none"}`, left, y);
  y += 7;
  pdf.setFontSize(9);
  pdf.text(summary.join("     "), left, y);
  y += 9;
  drawHeader();

  for (const row of rows) {
    const cells = columns.map((c) => pdf.splitTextToSize(String(c.value(row)), c.width - 2));
    const rowHeight = Math.max(...cells.map((lines) => lines.length)) * 3.2 + 3;
    if (y + rowHeight > pageHeight - 10) {
      pdf.addPage();
      y = 15;
      drawHeader();
    }
    let x = left + 1;
    cells.forEach((lines, index) => {
      pdf.text(lines, x, y, { baseline: "top" });
      x += columns[index].width;
    });
    pdf.setDrawColor(210, 210, 210);
    pdf.line(left, y + rowHeight - 1, left + tableWidth, y + rowHeight - 1);
    y += rowHeight;
  }

  pdf.save(datedFileName(fileName, "pdf"));
}

// Why the export buttons are disabled right now, or undefined when they aren't.
// Exports only ever cover a result that matches the filters on screen.
export function exportBlockReason(filtersValid: boolean, isRefreshing: boolean, count: number): string | undefined {
  if (!filtersValid) return "Fix the highlighted filters to export.";
  if (isRefreshing) return "Updating results…";
  if (count === 0) return "Nothing to export for these filters.";
  return undefined;
}
