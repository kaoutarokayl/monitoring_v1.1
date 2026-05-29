import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit, OnDestroy, NgZone, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, finalize } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AtmService } from '../services/atm.service';
import { AtmRealtimeService } from '../services/atm-realtime.service';
import { LookupItemDto, TransactionAuditDto, TransactionSearchCriteria } from '../models/atm.models';
import { ExportButtonComponent } from '../../../shared/components/export-button/export-button.component';

type FindMode = 'session' | 'transaction' | 'guid';

@Component({
  selector: 'app-atm-transactions',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe, ExportButtonComponent],
  templateUrl: './atm-transactions.component.html',
  styleUrl: './atm-transactions.component.css'
})
export class AtmTransactionsComponent implements OnInit, OnDestroy {
  private readonly atmService = inject(AtmService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly realtimeService = inject(AtmRealtimeService);
  private readonly ngZone = inject(NgZone);
  private readonly destroy$ = new Subject<void>();

  readonly clientId = computed(() => {
    // The ATM id is on the PARENT route /atm/:id, not on the child tab route
    const fromParent = this.route.parent?.snapshot.paramMap.get('id');
    const fromSelf   = this.route.snapshot.paramMap.get('id');
    return Number(fromParent ?? fromSelf ?? 0);
  });

  readonly isLoadingLookups = signal(false);
  readonly isSearching = signal(false);
  readonly error = signal<string | null>(null);

  readonly typeCodes = signal<LookupItemDto[]>([]);
  readonly reasonCodes = signal<LookupItemDto[]>([]);
  readonly completionCodes = signal<LookupItemDto[]>([]);

  readonly results = signal<TransactionAuditDto[]>([]);

  /** Export des transactions trouvées */
  readonly exportData = computed(() =>
    this.results().map(r => ({
      'Session ID':      r.sessionId,
      'Transaction ID':  r.transactionId,
      'Timestamp':       r.timestamp,
      'Type':            r.type ?? '',
      'Amount':          r.amount ?? '',
      'Completion':      r.completion ?? '',
      'Reason':          r.reason ?? '',
      'Has EJ':          r.hasEj ? 'Oui' : 'Non'
    }))
  );

  // UI state for direct find
  readonly findMode = signal<FindMode>('session');

  private readonly nowLocalIso = () => {
    const d = new Date();
    const pad = (n: number) => `${n}`.padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  };

  private readonly daysAgoLocalIso = (days: number) => {
    const d = new Date();
    d.setDate(d.getDate() - days);
    const pad = (n: number) => `${n}`.padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  };

  readonly filterForm = this.fb.group({
    from: [this.daysAgoLocalIso(7), Validators.required],
    to: [this.nowLocalIso(), Validators.required],
    amount: [null as number | null],
    typeLookupId: [null as number | null],
    reasonLookupId: [null as number | null],
    completionLookupId: [null as number | null]
  });

  readonly findForm = this.fb.group({
    mode: ['session' as FindMode, Validators.required],
    value: ['' as string, Validators.required]
  });

  ngOnInit() {
    this.loadLookups();
    // Auto-search on tab open so user sees data immediately
    this.applyFilter();
    this.subscribeToRealtimeUpdates(this.clientId());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private subscribeToRealtimeUpdates(clientId: number): void {
    this.realtimeService.transactionUpdates$
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        if (update.clientId !== clientId) return;
        this.ngZone.run(() => this.applyFilter());
      });
  }

  loadLookups() {
    this.isLoadingLookups.set(true);
    this.error.set(null);

    let pending = 3;
    const done = () => {
      pending--;
      if (pending <= 0) {
        this.isLoadingLookups.set(false);
        // If no results yet, re-trigger search now that clientId is fully resolved
        if (this.results().length === 0) this.applyFilter();
      }
    };

    this.atmService.getTransactionTypeCodes().pipe(finalize(done)).subscribe({
      next: rows => this.typeCodes.set(rows),
      error: e => this.error.set(e?.error?.message ?? 'Erreur lors du chargement des Type codes')
    });

    this.atmService.getTransactionReasonCodes().pipe(finalize(done)).subscribe({
      next: rows => this.reasonCodes.set(rows),
      error: e => this.error.set(e?.error?.message ?? 'Erreur lors du chargement des Reason codes')
    });

    this.atmService.getTransactionCompletionCodes().pipe(finalize(done)).subscribe({
      next: rows => this.completionCodes.set(rows),
      error: e => this.error.set(e?.error?.message ?? 'Erreur lors du chargement des Completion codes')
    });
  }

  applyFilter() {
    if (this.filterForm.invalid) return;
    this.search(this.buildCriteriaFromFilter());
  }

  findDirect() {
    if (this.findForm.invalid) return;
    const mode = this.findForm.value.mode ?? 'session';
    const raw = (this.findForm.value.value ?? '').trim();

    const criteria: TransactionSearchCriteria = {};
    if (mode === 'guid') {
      criteria.transactionGuid = raw || null;
    } else if (mode === 'transaction') {
      const id = Number(raw);
      criteria.transactionId = Number.isFinite(id) ? id : null;
    } else {
      const id = Number(raw);
      criteria.sessionId = Number.isFinite(id) ? id : null;
    }

    this.search(criteria);
  }

  private buildCriteriaFromFilter(): TransactionSearchCriteria {
    const v = this.filterForm.value;
    return {
      from: v.from ?? null,
      to: v.to ?? null,
      amount: v.amount ?? null,
      typeLookupId: v.typeLookupId ?? null,
      reasonLookupId: v.reasonLookupId ?? null,
      completionLookupId: v.completionLookupId ?? null
    };
  }

  private search(criteria: TransactionSearchCriteria) {
    const clientId = this.clientId();
    if (!clientId) return;

    this.isSearching.set(true);
    this.error.set(null);

    this.atmService.searchAtmTransactions(clientId, criteria)
      .pipe(finalize(() => this.isSearching.set(false)))
      .subscribe({
        next: rows => this.results.set(rows),
        error: e => this.error.set(e?.error?.message ?? 'Erreur lors de la recherche transactions')
      });
  }
}

