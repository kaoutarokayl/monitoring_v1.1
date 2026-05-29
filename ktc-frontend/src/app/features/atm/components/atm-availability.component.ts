import { CommonModule } from '@angular/common';
import { Component, OnInit, OnDestroy, NgZone, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, finalize } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AtmService } from '../services/atm.service';
import { AtmRealtimeService } from '../services/atm-realtime.service';
import { AtmAvailabilityReportDto } from '../models/atm.models';
import { ExportButtonComponent } from '../../../shared/components/export-button/export-button.component';
import { ExportPdfButtonComponent } from '../../../shared/components/export-pdf-button/export-pdf-button.component';

@Component({
  selector: 'app-atm-availability',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ExportButtonComponent, ExportPdfButtonComponent],
  templateUrl: './atm-availability.component.html',
  styleUrl: './atm-availability.component.css'
})
export class AtmAvailabilityComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly atmService = inject(AtmService);
  private readonly fb = inject(FormBuilder);
  private readonly realtimeService = inject(AtmRealtimeService);
  private readonly ngZone = inject(NgZone);
  private readonly destroy$ = new Subject<void>();

  readonly clientId = computed(() => Number(this.route.parent?.snapshot.paramMap.get('id') ?? this.route.snapshot.paramMap.get('id') ?? 0));

  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly report = signal<AtmAvailabilityReportDto | null>(null);

  readonly exportData = computed(() => {
    const r = this.report();
    if (!r) return [];
    const rows: any[] = [];
    rows.push({ Section: 'Summary', Field: 'Total duration', Value: r.totalDuration });
    rows.push({ Section: 'Summary', Field: 'Covering', Value: r.coveringText });
    rows.push({ Section: 'Summary', Field: 'Uptime', Value: `${r.uptimePercent}% (${r.uptimeDuration})` });
    rows.push({ Section: 'Summary', Field: 'Downtime', Value: `${r.downtimePercent}% (${r.downtimeDuration})` });

    (r.serviceStates || []).forEach(s => rows.push({ Section: 'ServiceState', Field: s.state, Value: `${s.percent}%`, Duration: s.duration }));
    (r.topUnavailableReasons || []).forEach(u => rows.push({ Section: 'UnavailableReason', Field: u.reason, Value: `${u.percent}%`, Duration: u.duration }));
    (r.topErrorCodes || []).forEach(e => rows.push({ Section: 'ErrorCode', Field: e.code, Value: `${e.percent}%`, Duration: e.duration, Reason: e.reason }));

    return rows;
  });

  readonly filterForm = this.fb.group({
    from: ['', Validators.required],
    to: ['', Validators.required]
  });

  ngOnInit(): void {
    const now = new Date();
    this.filterForm.patchValue({
      from: this.toLocalInput(new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000)),
      to: this.toLocalInput(now)
    });
    this.refresh();
    this.subscribeToRealtimeUpdates(this.clientId());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private subscribeToRealtimeUpdates(clientId: number): void {
    // Availability is computed from AssetHistory — reuse the existing signal
    this.realtimeService.assetHistoryUpdates$
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        if (update.clientId !== clientId) return;
        this.ngZone.run(() => this.refresh());
      });
  }

  refresh(): void {
    const id = this.clientId();
    if (!id || this.filterForm.invalid) return;

    const { from, to } = this.filterForm.value;
    this.isLoading.set(true);
    this.error.set(null);

    this.atmService.getAvailability(id, from!, to!)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (r) => this.report.set(r),
        error: () => this.error.set('Erreur lors du chargement du rapport Availability.')
      });
  }

  pctWidth(p: number): string {
    const v = Math.max(0, Math.min(100, Number(p ?? 0)));
    return `${v}%`;
  }

  private toLocalInput(date: Date): string {
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}

