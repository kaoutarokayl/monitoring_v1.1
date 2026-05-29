import { Component, NgZone, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AtmService, ClientAtm } from '../services/atm.service';
import { AuthService } from '../../../core/services/auth.service';
import { SignalrService } from '../../../core/services/signalr.service';
import { HasRoleDirective } from '../../../shared/directives/has-role.directive';

@Component({
  selector: 'app-atm-list',
  standalone: true,
  imports: [CommonModule, FormsModule, HasRoleDirective],
  templateUrl: './atm-list.component.html',
  styleUrls: ['./atm-list.component.css']
})
export class AtmListComponent implements OnInit, OnDestroy {
  private atmService = inject(AtmService);
  private router = inject(Router);
  private signalr = inject(SignalrService);
  private ngZone = inject(NgZone);
  readonly auth = inject(AuthService);

  // ── State ──────────────────────────────────────────────────────────────────
  atms = signal<ClientAtm[]>([]);
  isLoading = signal(true);
  error = signal<string | null>(null);
  searchQuery = signal('');
  filterStatus = signal<'all' | 'active' | 'inactive'>('all');
  sortField = signal<keyof ClientAtm>('clientName');
  sortAsc = signal(true);

  // ── Computed ───────────────────────────────────────────────────────────────
  filtered = computed(() => {
    const q = this.searchQuery().toLowerCase();
    const status = this.filterStatus();
    const field = this.sortField();
    const asc = this.sortAsc();

    return [...this.atms()]
      .filter(a => {
        const matchQ = !q ||
          a.clientName.toLowerCase().includes(q) ||
          a.networkAddress.includes(q) ||
          String(a.clientId).includes(q);
        const matchS =
          status === 'all' ||
          (status === 'active' && a.active) ||
          (status === 'inactive' && !a.active);
        return matchQ && matchS;
      })
      .sort((a, b) => {
        const va = a[field] ?? '';
        const vb = b[field] ?? '';
        const cmp = va < vb ? -1 : va > vb ? 1 : 0;
        return asc ? cmp : -cmp;
      });
  });

  totalCount = computed(() => this.atms().length);
  activeCount = computed(() => this.atms().filter(a => a.active).length);
  inactiveCount = computed(() => this.atms().filter(a => !a.active).length);

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.load();
    void this.subscribeRealtimeAtmInserts();
  }

  ngOnDestroy(): void {
    void this.signalr.disconnectMonitoringHub();
  }

  /** Nouvel ATM inséré en base (automate) → mise à jour instantanée de la liste. */
  private async subscribeRealtimeAtmInserts(): Promise<void> {
    try {
      const conn = await this.signalr.connectMonitoringHub();
      conn.on('ReceiveNewData', (payload: unknown) => {
        const row = this.normalizeRealtimePayload(payload);
        if (row == null || row.clientId == null || Number.isNaN(row.clientId)) return;
        // Les callbacks SignalR peuvent passer hors NgZone selon la version / le transport.
        this.ngZone.run(() => {
          this.atms.update(list => {
            const i = list.findIndex(a => a.clientId === row.clientId);
            if (i < 0) return [...list, row];
            const copy = [...list];
            copy[i] = row;
            return copy;
          });
        });
      });
    } catch {
      // Non connecté ou hub indisponible : la liste reste chargée via HTTP uniquement.
    }
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.atmService.getClients().subscribe({
      next: data => {
        // Ne pas écraser une ligne déjà poussée par SignalR avant la fin du GET (course HTTP / temps réel).
        this.atms.update(existing => {
          const serverIds = new Set(data.map(a => a.clientId));
          const notYetInResponse = existing.filter(a => !serverIds.has(a.clientId));
          return [...data, ...notYetInResponse];
        });
        this.isLoading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.message ?? 'Impossible de charger les ATMs');
        this.isLoading.set(false);
      }
    });
  }

  // ── Actions ────────────────────────────────────────────────────────────────
  goCreate(): void {
    this.router.navigate(['/admin/atms/create']);
  }

  goEdit(id: number): void {
    this.router.navigate(['/admin/atms', id]);
  }

  goMap(): void {
    this.router.navigate(['/admin/atms/map']);
  }

  confirmDelete(atm: ClientAtm): void {
    if (!confirm(`Supprimer l'ATM "${atm.clientName}" (#${atm.clientId}) ?`)) return;
    this.atmService.deleteClient(atm.clientId).subscribe({
      next: () => this.atms.update(list => list.filter(a => a.clientId !== atm.clientId)),
      error: err => alert(err?.error?.message ?? 'Erreur lors de la suppression')
    });
  }

  // ── Sorting ────────────────────────────────────────────────────────────────
  sort(field: keyof ClientAtm): void {
    if (this.sortField() === field) {
      this.sortAsc.update(v => !v);
    } else {
      this.sortField.set(field);
      this.sortAsc.set(true);
    }
  }

  sortIcon(field: keyof ClientAtm): string {
    if (this.sortField() !== field) return '↕';
    return this.sortAsc() ? '↑' : '↓';
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  trackByClientId(_: number, atm: ClientAtm): number {
    return atm.clientId;
  }

  connectableLabel(val: number): string {
    return ['—', 'Non connectable', 'IP Statique', 'IP Dynamique'][val] ?? String(val);
  }

  /** Accepte camelCase (API) ou PascalCase si le sérialiseur SignalR diffère du contrôleur. */
  private normalizeRealtimePayload(payload: unknown): ClientAtm | null {
    if (!payload || typeof payload !== 'object') return null;
    const r = payload as Record<string, unknown>;
    const g = (camel: string, pascal: string): unknown =>
      r[camel] !== undefined ? r[camel] : r[pascal];

    return {
      clientId: Number(g('clientId', 'ClientId')),
      ktcGuid: String(g('ktcGuid', 'KtcGuid') ?? ''),
      clientName: String(g('clientName', 'ClientName') ?? ''),
      networkAddress: String(g('networkAddress', 'NetworkAddress') ?? ''),
      connectable: Number(g('connectable', 'Connectable')),
      detailsUnknown: Boolean(g('detailsUnknown', 'DetailsUnknown')),
      latitude: Number(g('latitude', 'Latitude')),
      longitude: Number(g('longitude', 'Longitude')),
      timezone: String(g('timezone', 'Timezone') ?? ''),
      comments:
        g('comments', 'Comments') !== undefined ? String(g('comments', 'Comments')) : undefined,
      businessId: Number(g('businessId', 'BusinessId')),
      branchId: Number(g('branchId', 'BranchId')),
      hardwareTypeId: Number(g('hardwareTypeId', 'HardwareTypeId')),
      hardwareTypeName:
        g('hardwareTypeName', 'HardwareTypeName') !== undefined
          ? String(g('hardwareTypeName', 'HardwareTypeName'))
          : undefined,
      active: Boolean(g('active', 'Active')),
      clientType: Number(g('clientType', 'ClientType'))
    };
  }
}
