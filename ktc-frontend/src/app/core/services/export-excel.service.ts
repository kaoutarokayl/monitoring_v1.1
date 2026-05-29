import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';

@Injectable({ providedIn: 'root' })
export class ExportExcelService {

  /**
   * Exports a JSON array to an .xlsx file and triggers browser download.
   * @param json     Array of objects (table rows)
   * @param fileName Desired file name WITHOUT extension (date will be appended)
   * @param sheetName Optional worksheet name (defaults to 'Data')
   */
  exportAsExcelFile(json: any[], fileName: string, sheetName: string = 'Data'): void {
    if (!json || json.length === 0) return;

    // 1. Create worksheet from JSON
    const worksheet: XLSX.WorkSheet = XLSX.utils.json_to_sheet(json);

    // 2. Auto-size columns based on content width
    const colWidths = this.getColumnWidths(json);
    worksheet['!cols'] = colWidths;

    // 3. Style header row (bold)
    const range = XLSX.utils.decode_range(worksheet['!ref'] ?? 'A1');
    for (let col = range.s.c; col <= range.e.c; col++) {
      const cellAddr = XLSX.utils.encode_cell({ r: 0, c: col });
      if (!worksheet[cellAddr]) continue;
      worksheet[cellAddr].s = {
        font: { bold: true, color: { rgb: 'FFFFFF' } },
        fill: { fgColor: { rgb: '1a6b3c' } },
        alignment: { horizontal: 'center' }
      };
    }

    // 4. Create workbook and append worksheet
    const workbook: XLSX.WorkBook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, sheetName);

    // 5. Build filename with date suffix: e.g. "tickets_20260509.xlsx"
    const dateSuffix = new Date().toISOString().slice(0, 10).replace(/-/g, '');
    const fullFileName = `${fileName}_${dateSuffix}.xlsx`;

    // 6. Trigger download
    XLSX.writeFile(workbook, fullFileName);
  }

  /** Computes optimal column widths from data values */
  private getColumnWidths(json: any[]): XLSX.ColInfo[] {
    if (!json.length) return [];
    const keys = Object.keys(json[0]);
    return keys.map(key => {
      const maxLen = Math.max(
        key.length,
        ...json.map(row => String(row[key] ?? '').length)
      );
      return { wch: Math.min(maxLen + 4, 60) }; // cap at 60 chars
    });
  }
}
