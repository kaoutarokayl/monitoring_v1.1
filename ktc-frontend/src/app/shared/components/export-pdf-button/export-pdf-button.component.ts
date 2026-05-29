import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-export-pdf-button',
  standalone: true,
  imports: [CommonModule],

  template: `
    <button
      class="export-btn"
      [class.export-btn--disabled]="!data || data.length === 0"
      [disabled]="!data || data.length === 0"
      (click)="onExport()"
      [title]="data && data.length
        ? 'Exporter ' + data.length + ' ligne(s) en PDF'
        : 'Aucune donnée à exporter'"
    >

      <!-- PDF icon -->
      <svg
        class="export-btn__icon"
        viewBox="0 0 24 24"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          d="M12 2v7h7"
          stroke="currentColor"
          stroke-width="1.6"
          stroke-linecap="round"
          stroke-linejoin="round"
        />

        <path
          d="M21 21H3V3h7v4a2 2 0 0 0 2 2h4v12z"
          stroke="currentColor"
          stroke-width="1.6"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>

      <span class="export-btn__label">
        Export PDF
      </span>

      @if (data && data.length) {
        <span class="export-btn__badge">
          {{ data.length }}
        </span>
      }

    </button>
  `,

  styles: [`
    :host {
      display: inline-block;
    }

    .export-btn {
      display: inline-flex;
      align-items: center;
      gap: 6px;

      padding: 7px 12px;

      border: 1.5px solid #174b78;
      border-radius: 6px;

      background: transparent;
      color: #174b78;

      font-size: 0.82rem;
      font-weight: 600;

      cursor: pointer;

      transition: all 0.2s ease;
    }

    .export-btn:hover:not(:disabled) {
      background: #174b78;
      color: white;
    }

    .export-btn--disabled,
    .export-btn:disabled {
      border-color: #ccc;
      color: #aaa;
      cursor: not-allowed;
      opacity: 0.6;
    }

    .export-btn__icon {
      width: 15px;
      height: 15px;
      flex-shrink: 0;
    }

    .export-btn__badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;

      min-width: 18px;
      height: 18px;

      padding: 0 5px;

      background: #174b78;
      color: white;

      border-radius: 9px;

      font-size: 0.7rem;
      font-weight: 700;
    }
  `]
})
export class ExportPdfButtonComponent {

  @Input() data: any[] = [];

  @Input() fileName: string = 'export';

  @Input() title: string = 'Rapport d\'Export';

  @Input() showDate: boolean = true;

  async onExport(): Promise<void> {

    if (!this.data || this.data.length === 0) {
      return;
    }

    // Dynamic imports
    const { jsPDF } = await import('jspdf');

    const autoTableModule = await import('jspdf-autotable');

    const autoTable =
      (autoTableModule as any).default || autoTableModule;

    // Create PDF
    const doc = new jsPDF({
      orientation: 'landscape',
      unit: 'pt',
      format: 'a4'
    });

    // Page setup
    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();

    const margin = 15;

    // Header
    this.addHeader(doc, pageWidth, pageHeight, margin);

    // Title + date
    const startY = this.addTitleAndDate(
      doc,
      pageWidth,
      margin
    );

    // Table data
    const keys = Object.keys(this.data[0] || {});

    const head = [keys];

    const body = this.data.map(row =>
      keys.map(key => {

        const value = row[key];

        return value === undefined || value === null
          ? ''
          : String(value);

      })
    );

    // Generate table
    autoTable(doc, {

      head,
      body,

      startY,

      margin,

      styles: {
        fontSize: 9,
        cellPadding: 4,

        overflow: 'linebreak',

        halign: 'center',
        valign: 'middle'
      },

      headStyles: {
        fillColor: [23, 75, 120],

        textColor: 255,

        fontStyle: 'bold',

        halign: 'center',

        fontSize: 10
      },

      bodyStyles: {
        textColor: 50,

        lineColor: [200, 200, 200],

        lineWidth: 0.3
      },

      alternateRowStyles: {
        fillColor: [245, 247, 250]
      },

      columnStyles: this.getColumnStyles(keys),

      didDrawPage: () => {
        this.addFooter(
          doc,
          pageWidth,
          pageHeight,
          margin
        );
      }
    });

    // Save file
    const now = new Date()
      .toISOString()
      .slice(0, 19)
      .replace(/[:T]/g, '-');

    doc.save(`${this.fileName}_${now}.pdf`);
  }

  private addHeader(
    doc: any,
    pageWidth: number,
    pageHeight: number,
    margin: number
  ): void {

    doc.setFontSize(14);

    doc.setTextColor(23, 75, 120);

    doc.text(
      'KTC Monitoring',
      margin,
      margin + 8
    );

    // Line
    doc.setDrawColor(23, 75, 120);

    doc.setLineWidth(1);

    doc.line(
      margin,
      margin + 12,
      pageWidth - margin,
      margin + 12
    );
  }

  private addTitleAndDate(
    doc: any,
    pageWidth: number,
    margin: number
  ): number {

    let currentY = margin + 25;

    // Title
    doc.setFontSize(12);

    doc.setTextColor(30, 30, 30);

    doc.text(
      this.title,
      margin,
      currentY
    );

    currentY += 8;

    // Date
    if (this.showDate) {

      doc.setFontSize(9);

      doc.setTextColor(100, 100, 100);

      const now = new Date().toLocaleDateString(
        'fr-FR',
        {
          year: 'numeric',
          month: 'long',
          day: 'numeric',

          hour: '2-digit',
          minute: '2-digit'
        }
      );

      doc.text(
        `Généré le : ${now}`,
        margin,
        currentY
      );

      currentY += 12;

      doc.text(
        `Nombre de lignes : ${this.data.length}`,
        margin,
        currentY
      );

      currentY += 12;
    }

    return currentY;
  }

  private addFooter(
    doc: any,
    pageWidth: number,
    pageHeight: number,
    margin: number
  ): void {

    const pageCount = doc.getNumberOfPages();

    const pageNum =
      doc.internal.getCurrentPageInfo().pageNumber;

    // Footer line
    doc.setDrawColor(200, 200, 200);

    doc.setLineWidth(0.5);

    doc.line(
      margin,
      pageHeight - margin - 8,
      pageWidth - margin,
      pageHeight - margin - 8
    );

    // Footer text
    doc.setFontSize(8);

    doc.setTextColor(120, 120, 120);

    doc.text(
      `Page ${pageNum} sur ${pageCount}`,
      margin,
      pageHeight - margin
    );

    doc.text(
      '© KTC Monitoring',
      pageWidth - margin - 80,
      pageHeight - margin
    );
  }

  private getColumnStyles(
    keys: string[]
  ): { [key: number]: any } {

    const styles: { [key: number]: any } = {};

    keys.forEach((key, index) => {

      const lower = key.toLowerCase();

      if (
        lower.includes('percent') ||
        lower.includes('value') ||
        lower.includes('duration') ||
        lower.includes('count')
      ) {
        styles[index] = {
          halign: 'right'
        };
      }

      if (
        lower.includes('date') ||
        lower.includes('timestamp') ||
        lower.includes('time')
      ) {
        styles[index] = {
          halign: 'center'
        };
      }
    });

    return styles;
  }
}