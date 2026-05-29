import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  BranchDto, BusinessDto, GroupDto,
  TicketSearchCriteria, TicketSearchResult,
  TicketTypeLookupDto, ErrorCodeLookupDto, SlaStatusOption
} from '../models/ticket-search.models';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { TicketSearchService } from '../services/ticket-search.service';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-ticket-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ticket-search.component.html',
  styleUrls: ['./ticket-search.component.css']
})
export class TicketSearchComponent implements OnInit {
  private readonly ticketSearchService = inject(TicketSearchService);

  groups       = signal<GroupDto[]>([]);
  businesses   = signal<BusinessDto[]>([]);
  branches     = signal<BranchDto[]>([]);
  ticketTypes  = signal<TicketTypeLookupDto[]>([]);
  errorCodes   = signal<ErrorCodeLookupDto[]>([]);
  results      = signal<TicketSearchResult[]>([]);
  isLoading    = signal(false);
  error        = signal<string | null>(null);

  slaStatusOptions = signal<SlaStatusOption[]>([
    { value: 'No Filter',               label: 'No Filter' },
    { value: 'No Ticket SLAs',          label: 'No Ticket SLAs' },
    { value: 'Has any open SLAs',       label: 'Has any open SLAs' },
    { value: 'Has any due in <X hours', label: 'Has any due in <X hours' },
    { value: 'Has open exceeded SLAs',  label: 'Has open exceeded SLAs' },
    { value: 'All SLAs are closed',     label: 'All SLAs are closed' }
  ]);

  searchCriteria: TicketSearchCriteria = {
    ticketStatus: 'Open/Dispatched',
    slaStatus: 'No Filter',
    slaHours: 0
  };

  // ── Computed KPI signals ──────────────────────────────────────────────────
  openCount = computed(() =>
    this.results().filter(t => t.status === 'Open' || t.status === 'Dispatched').length
  );

  closedCount = computed(() =>
    this.results().filter(t => t.status === 'Closed').length
  );

  slaBreachCount = computed(() =>
    this.results().filter(t => t.slaSummary === 'Breached').length
  );

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.loadLookups();
  }

  // ── Data loading ──────────────────────────────────────────────────────────
  loadLookups(): void {
    this.isLoading.set(true);
    this.error.set(null);

    forkJoin({
      groups:      this.ticketSearchService.getGroups().pipe(catchError(e => { console.error(e); return of([] as GroupDto[]); })),
      businesses:  this.ticketSearchService.getBusinesses().pipe(catchError(e => { console.error(e); return of([] as BusinessDto[]); })),
      branches:    this.ticketSearchService.getBranches().pipe(catchError(e => { console.error(e); return of([] as BranchDto[]); })),
      ticketTypes: this.ticketSearchService.getTicketTypes().pipe(catchError(e => { console.error(e); return of([] as TicketTypeLookupDto[]); })),
      errorCodes:  this.ticketSearchService.getErrorCodes().pipe(catchError(e => { console.error(e); return of([] as ErrorCodeLookupDto[]); }))
    }).subscribe({
      next: ({ groups, businesses, branches, ticketTypes, errorCodes }) => {
        this.groups.set(groups);
        this.businesses.set(businesses);
        this.branches.set(branches);
        this.ticketTypes.set(ticketTypes);
        this.errorCodes.set(errorCodes);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.error.set('Erreur lors du chargement des données. Veuillez réessayer.');
        this.isLoading.set(false);
      }
    });
  }

  search(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.results.set([]);

    this.ticketSearchService.searchTickets(this.searchCriteria).subscribe({
      next: (rows) => {
        this.results.set(rows);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.error.set('Erreur lors de la recherche. Vérifiez vos critères et réessayez.');
        this.isLoading.set(false);
      }
    });
  }

  clear(): void {
    this.searchCriteria = {
      ticketStatus: 'Open/Dispatched',
      slaStatus: 'No Filter',
      slaHours: 0
    };
    this.results.set([]);
    this.error.set(null);
  }

  showSlaHours(): boolean {
    return this.searchCriteria.slaStatus === 'Has any due in <X hours';
  }

  // ── Export helpers ────────────────────────────────────────────────────────

  /** Builds the flat row array used by both exporters */
  private buildExportRows(): Record<string, string | number>[] {
    return this.results().map(t => ({
      'Ticket ID':      t.ticketId,
      'Type':           t.ticketType      ?? '',
      'Statut':         t.status          ?? '',
      'ATM':            t.atmName         ?? '',
      'Business':       t.businessName    ?? '',
      'Branche':        t.branchName      ?? '',
      'Dispatché à':    t.dispatchedTo    ?? '—',
      'Owner':          t.owner           ?? '—',
      'Code Erreur':    t.errorCode       ?? '—',
      'Texte Erreur':   t.errorText       ?? '—',
      'Créé le':        t.created         ? new Date(t.created).toLocaleString('fr-FR') : '',
      'Modifié le':     t.lastChangeDate  ? new Date(t.lastChangeDate).toLocaleString('fr-FR') : '',
      'Fermé le':       t.closedDate      ? new Date(t.closedDate).toLocaleString('fr-FR')     : '—',
      'Durée':          t.duration        ?? '',
      'SLA':            t.slaSummary      ?? '—'
    }));
  }

  // ── Excel export ──────────────────────────────────────────────────────────
  exportExcel(): void {
    const rows = this.buildExportRows();
    if (!rows.length) return;

    const ws = XLSX.utils.json_to_sheet(rows);

    // Column widths
    ws['!cols'] = [
      { wch: 12 }, { wch: 14 }, { wch: 13 }, { wch: 18 },
      { wch: 16 }, { wch: 18 }, { wch: 18 }, { wch: 16 },
      { wch: 12 }, { wch: 22 }, { wch: 18 }, { wch: 18 },
      { wch: 18 }, { wch: 10 }, { wch: 10 }
    ];

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Tickets');

    const fileName = `tickets_${new Date().toISOString().slice(0, 10)}.xlsx`;
    XLSX.writeFile(wb, fileName);
  }

  // ── PDF export ────────────────────────────────────────────────────────────
  exportPdf(): void {
    const rows = this.buildExportRows();
    if (!rows.length) return;

    const doc = new jsPDF({ orientation: 'landscape', unit: 'pt', format: 'a4' });

    // Header band
    doc.setFillColor(26, 58, 92);          // #1A3A5C
    doc.rect(0, 0, doc.internal.pageSize.getWidth(), 38, 'F');
    doc.setTextColor(255, 255, 255);
    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text('Ticket Search — Export', 18, 25);

    // Meta line
    doc.setFontSize(8);
    doc.setFont('helvetica', 'normal');
    const meta = `Exporté le ${new Date().toLocaleString('fr-FR')}  |  ${rows.length} ticket(s)`;
    doc.text(meta, doc.internal.pageSize.getWidth() - 18, 25, { align: 'right' });

    const columns = Object.keys(rows[0]);
    const body    = rows.map(r => columns.map(c => r[c] ?? ''));

    autoTable(doc, {
      startY: 48,
      head:   [columns],
      body,
      styles: {
        font:      'helvetica',
        fontSize:  7,
        cellPadding: 4,
        overflow:  'ellipsize'
      },
      headStyles: {
        fillColor:  [26, 58, 92],
        textColor:  255,
        fontStyle:  'bold',
        fontSize:   7.5
      },
      alternateRowStyles: { fillColor: [245, 244, 241] },
      columnStyles: {
        0:  { fontStyle: 'bold', textColor: [26, 58, 92] },  // Ticket ID
        14: { fontStyle: 'bold' }                             // SLA
      },
      margin: { top: 48, left: 18, right: 18 },
      didDrawPage: (data) => {
        // Footer with page number
        const pageCount = (doc as any).internal.getNumberOfPages();
        doc.setFontSize(7);
        doc.setTextColor(140);
        doc.text(
          `Page ${data.pageNumber} / ${pageCount}`,
          doc.internal.pageSize.getWidth() / 2,
          doc.internal.pageSize.getHeight() - 10,
          { align: 'center' }
        );
      }
    });

    const fileName = `tickets_${new Date().toISOString().slice(0, 10)}.pdf`;
    doc.save(fileName);
  }
}