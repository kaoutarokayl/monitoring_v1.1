import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExportExcelService } from '../../../core/services/export-excel.service';

@Component({
  selector: 'app-export-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      class="export-btn"
      [class.export-btn--disabled]="!data || data.length === 0"
      [disabled]="!data || data.length === 0"
      (click)="onExport()"
      [title]="data.length ? 'Exporter ' + data.length + ' ligne(s) en Excel' : 'Aucune donnée à exporter'"
    >
      <!-- Excel icon SVG -->
      <svg class="export-btn__icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6z" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
        <path d="M14 2v6h6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
        <path d="M8 13l2.5 3L13 13" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
        <path d="M10.5 16v-6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      <span class="export-btn__label">Export Excel</span>
      @if (data.length) {
        <span class="export-btn__badge">{{ data.length }}</span>
      }
    </button>
  `,
  styles: [`
    .export-btn {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 7px 14px;
      border: 1.5px solid #1a6b3c;
      border-radius: 6px;
      background: transparent;
      color: #1a6b3c;
      font-size: 0.82rem;
      font-weight: 600;
      font-family: inherit;
      cursor: pointer;
      transition: all 0.2s ease;
      white-space: nowrap;
      user-select: none;
    }

    .export-btn:hover:not(:disabled) {
      background: #1a6b3c;
      color: #fff;
      box-shadow: 0 2px 8px rgba(26, 107, 60, 0.35);
      transform: translateY(-1px);
    }

    .export-btn:active:not(:disabled) {
      transform: translateY(0);
      box-shadow: none;
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

    .export-btn__label {
      letter-spacing: 0.02em;
    }

    .export-btn__badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 18px;
      height: 18px;
      padding: 0 5px;
      background: #1a6b3c;
      color: #fff;
      border-radius: 9px;
      font-size: 0.7rem;
      font-weight: 700;
      transition: background 0.2s;
    }

    .export-btn:hover:not(:disabled) .export-btn__badge {
      background: rgba(255,255,255,0.25);
    }

    /* Dark mode */
    :host-context(.dark-theme) .export-btn {
      border-color: #4caf7d;
      color: #4caf7d;
    }

    :host-context(.dark-theme) .export-btn:hover:not(:disabled) {
      background: #4caf7d;
      color: #0f1b14;
    }

    :host-context(.dark-theme) .export-btn__badge {
      background: #4caf7d;
      color: #0f1b14;
    }
  `]
})
export class ExportButtonComponent {
  /** Data array to export */
  @Input() data: any[] = [];

  /** File name without extension (date appended automatically) */
  @Input() fileName: string = 'export';

  /** Optional Excel sheet name */
  @Input() sheetName: string = 'Data';

  private exportService = inject(ExportExcelService);

  onExport(): void {
    if (!this.data || this.data.length === 0) return;
    this.exportService.exportAsExcelFile(this.data, this.fileName, this.sheetName);
  }
}
