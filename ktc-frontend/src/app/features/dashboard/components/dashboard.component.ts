import {
  Component, ChangeDetectionStrategy, inject,
  signal, computed, OnInit, OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';

interface DateRange { label: string; from: string; to: string; }

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  private sanitizer = inject(DomSanitizer);
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);

  // ── Config ────────────────────────────────────────────────────────────────
  private readonly DASHBOARD_UID  = 'adfzvx2';
  private readonly DASHBOARD_SLUG = 'dashboard-ktc';
  private readonly GRAFANA_BASE   = 'http://localhost:3000';

  // ── Grafana panel IDs used by the dashboard menu ───────────────────────
  // These values match the panel IDs exposed in the Grafana iframe URLs.
  private readonly PANEL_IDS: Record<string, number> = {
    overallAvailability:      2,
    transactionAvailability:  4,
    deviceAvailability:       5,
    transactions:             6,
    supervisorSessionLengths: 12,
    EtatCassetteparATM:       15,
    AssetHistoryByATM:        9,
  };

  // ── Active panel (from query params) ──────────────────────────────────────
  activePanel = signal<string | null>(null);

  // ── Ranges ────────────────────────────────────────────────────────────────
  readonly ranges: DateRange[] = [
    { label: '24h',  from: 'now-24h', to: 'now' },
    { label: '7j',   from: 'now-7d',  to: 'now' },
    { label: '30j',  from: 'now-30d', to: 'now' },
    { label: '1 an', from: 'now-1y',  to: 'now' },
    { label: 'Tout', from: '2021-05-15T00:00:00.000Z', to: 'now' },
  ];

  // ── State ─────────────────────────────────────────────────────────────────
  activeRange  = signal<DateRange>(this.ranges[2]);
  isRefreshing = signal(false);
  isFullscreen = signal(false);

  // ── Clock ─────────────────────────────────────────────────────────────────
  private _now    = signal<Date>(new Date());
  private _clockInterval?: ReturnType<typeof setInterval>;

  currentTime = computed(() =>
    this._now().toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
  );

  currentDate = computed(() =>
    this._now().toLocaleDateString('fr-FR', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' })
  );

  // ── Grafana URL ───────────────────────────────────────────────────────────
  readonly grafanaUrl = computed<SafeResourceUrl>(() => {
    const r = this.activeRange();
    const panel = this.activePanel();
    const panelId = panel ? this.PANEL_IDS[panel] : null;

    const baseParams =
      `?orgId=1` +
      `&from=${encodeURIComponent(r.from)}` +
      `&to=${encodeURIComponent(r.to)}` +
      `&timezone=browser` +
      `&refresh=30s`;

    const url = panelId
      ? `${this.GRAFANA_BASE}/d-solo/${this.DASHBOARD_UID}/${this.DASHBOARD_SLUG}${baseParams}&panelId=panel-${panelId}`
      : `${this.GRAFANA_BASE}/d/${this.DASHBOARD_UID}/${this.DASHBOARD_SLUG}${baseParams}&kiosk`;

    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this._clockInterval = setInterval(() => this._now.set(new Date()), 1000);

    // Read ?panel=xxx query param and update activePanel signal
    this.route.queryParamMap.subscribe(params => {
      const panel = params.get('panel');
      this.activePanel.set(panel);
      if (panel) {
        // Reset to full dashboard after viewing a panel
        // (optional: remove if you want panel view to persist)
      }
    });
  }

  ngOnDestroy(): void {
    if (this._clockInterval) clearInterval(this._clockInterval);
  }

  // ── Actions ───────────────────────────────────────────────────────────────
  setRange(range: DateRange): void {
    this.activeRange.set(range);
  }

  refresh(): void {
    this.isRefreshing.set(true);
    // Force iframe reload by briefly resetting the range
    const current = this.activeRange();
    setTimeout(() => {
      this.activeRange.set({ ...current });
      setTimeout(() => this.isRefreshing.set(false), 800);
    }, 100);
  }

  exportPdf(): void {
    const range = this.activeRange();
    const fileName = `dashboard-ktc-${range.label.toLowerCase().replace(/\s+/g, '-')}.pdf`;

    this.isRefreshing.set(true);

    this.http
      .get(`http://localhost:5239/api/dashboard/export-pdf?dashboardUid=${this.DASHBOARD_UID}&slug=${this.DASHBOARD_SLUG}&from=${encodeURIComponent(range.from)}&to=${encodeURIComponent(range.to)}`, {
        responseType: 'blob'
      })
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = fileName;
          link.click();
          window.URL.revokeObjectURL(url);
          this.isRefreshing.set(false);
        },
        error: () => {
          this.isRefreshing.set(false);
          alert('La génération du PDF a échoué. Vérifiez que le serveur Grafana et l’API backend sont démarrés.');
        }
      });
  }

  onFrameLoad(): void {
    this.isRefreshing.set(false);
  }

  toggleFullscreen(): void {
    this.isFullscreen.update(v => !v);
  }
}