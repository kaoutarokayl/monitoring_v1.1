import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CampaignService, BusinessDto } from '../services/campaign.service';
import { AuthService } from '../../../core/services/auth.service';
import { Campaign } from '../models/campaign.models';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-campaign-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './campaign-list.component.html',
  styleUrls: ['./campaign-list.component.css']
})
export class CampaignListComponent implements OnInit {
  private readonly campaignService = inject(CampaignService);
  private readonly authService = inject(AuthService);
  private readonly router          = inject(Router);

  // ── State ──────────────────────────────────────────────────────────────────
  campaigns             = signal<Campaign[]>([]);
  businesses            = signal<BusinessDto[]>([]);
  selectedCampaignId    = signal<number | null>(null);
  isLoading             = signal(false);
  error                 = signal<string | null>(null);
  campaignBusinessesMap = signal<Map<number, string>>(new Map());
  campaignBusinessIdsMap= signal<Map<number, number[]>>(new Map());

  // Filters
  searchQuery    = signal('');
  statusFilter   = signal<number | null>(null);
  typeFilter     = signal<number | null>(null);
  businessFilter = signal<string>('');

  // Sort
  sortField = signal<keyof Campaign>('name');
  sortAsc   = signal(true);

  // RBAC: Vérifier si Support (peut éditer marketing et campagnes)
  canEditMarketing = computed(() => this.authService.isSupport());
  canEditCampaign = computed(() => this.canEditMarketing());

  // ── Computed ───────────────────────────────────────────────────────────────
  filtered = computed(() => {
    const q       = this.searchQuery().toLowerCase();
    const statusF = this.statusFilter();
    const typeF   = this.typeFilter();
    const field   = this.sortField();
    const asc     = this.sortAsc();

    return [...this.campaigns()]
      .filter(c => {
        if (q && !(
          c.name?.toLowerCase().includes(q) ||
          c.packageName?.toLowerCase().includes(q) ||
          c.externalId?.toLowerCase().includes(q) ||
          String(c.campaignId).includes(q)
        )) return false;
        if (statusF !== null && Number(c.campaignStatus) !== statusF) return false;
        if (typeF   !== null && Number(c.campaignType)   !== typeF)   return false;

        if (this.businessFilter()) {
          const ids = this.campaignBusinessIdsMap().get(c.campaignId) ?? [];
          if (!ids.some(id => String(id) === String(this.businessFilter()))) return false;
        }

        return true;
      })
      .sort((a, b) => {
        let va: any = (a as any)[field];
        let vb: any = (b as any)[field];
        if (field === 'startDate' || field === 'endDate' || field === 'purgeDate') {
          va = va ? new Date(va).getTime() : 0;
          vb = vb ? new Date(vb).getTime() : 0;
        }
        const cmp = va < vb ? -1 : va > vb ? 1 : 0;
        return asc ? cmp : -cmp;
      });
  });

  // KPI counts (on filtered list)
  enabledCount  = computed(() => this.filtered().filter(c => Number(c.campaignStatus) === 0).length);
  disabledCount = computed(() => this.filtered().filter(c => Number(c.campaignStatus) === 1).length);
  expiredCount  = computed(() => this.filtered().filter(c => Number(c.campaignStatus) === 2).length);

  currentBusiness = computed(() => {
    const id = this.businessFilter();
    if (!id) return null;
    return this.businesses().find(b => String(b.businessId) === id)?.businessName ?? null;
  });

  testCampaignName = computed(() => {
    const id = this.selectedCampaignId();
    if (!id) return null;
    const c = this.campaigns().find(x => x.campaignId === id);
    return c?.campaignInTestmode ? c.name : null;
  });

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.loadAll();
  }

  // ── Load ───────────────────────────────────────────────────────────────────
  loadAll(): void {
    this.isLoading.set(true);
    this.error.set(null);

    forkJoin({
      campaigns:  this.campaignService.getAllCampaigns(),
      businesses: this.campaignService.getAllBusinesses()
    }).subscribe({
      next: ({ campaigns, businesses }) => {
        this.campaigns.set(campaigns);
        this.businesses.set(businesses);
        this.loadCampaignBusinesses(campaigns);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Load error:', err);
        this.error.set('Impossible de charger les données. Vérifiez la connexion au serveur.');
        this.isLoading.set(false);
      }
    });
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.campaignService.getAllCampaigns().subscribe({
      next: (campaigns) => {
        this.campaigns.set(campaigns);
        this.loadCampaignBusinesses(campaigns);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.error.set('Impossible de charger les campagnes.');
        this.isLoading.set(false);
      }
    });
  }

  private loadCampaignBusinesses(campaigns: Campaign[]): void {
    if (!campaigns.length) return;
    campaigns.forEach(campaign => {
      this.campaignService.getCampaignBusinesses(campaign.campaignId).subscribe({
        next: (businesses) => {
          const ids   = businesses.map(b => Number((b as any).businessId ?? 0)).filter(id => !Number.isNaN(id));
          const names = businesses.map(b => b.businessName ?? String((b as any).businessId ?? '')).filter(Boolean).join(', ');
          this.campaignBusinessIdsMap.update(m => { const n = new Map(m); n.set(campaign.campaignId, ids); return n; });
          this.campaignBusinessesMap.update(m => { const n = new Map(m); n.set(campaign.campaignId, names || '—'); return n; });
        },
        error: () => {
          this.campaignBusinessesMap.update(m => { const n = new Map(m); n.set(campaign.campaignId, '—'); return n; });
        }
      });
    });
  }

  // ── Selection ──────────────────────────────────────────────────────────────
  selectCampaign(id: number): void {
    this.selectedCampaignId.set(this.selectedCampaignId() === id ? null : id);
  }

  isSelected(id: number): boolean {
    return this.selectedCampaignId() === id;
  }

  // ── Sort ───────────────────────────────────────────────────────────────────
  sort(field: keyof Campaign): void {
    if (this.sortField() === field) { this.sortAsc.update(v => !v); }
    else { this.sortField.set(field); this.sortAsc.set(true); }
  }

  sortIcon(field: keyof Campaign): string {
    if (this.sortField() !== field) return '↕';
    return this.sortAsc() ? '↑' : '↓';
  }

  // ── Navigation ─────────────────────────────────────────────────────────────
  goCreate():             void { if (!this.canEditCampaign()) return; this.router.navigate(['/campaign/create']); }
  goEdit(id: number):     void { this.router.navigate(['/campaign', id, 'edit']); }
  goDetails(id: number):  void { this.router.navigate(['/campaign', id]); }
  openMarketingControl(): void {
    const businessId = this.businessFilter();
    this.router.navigate([{ outlets: { modal: ['marketing'] } }], {
      queryParams: { businessId: businessId || undefined }
    });
  }

  // ── Delete ─────────────────────────────────────────────────────────────────
  deleteCampaign(id: number, event: Event): void {
    event.stopPropagation();
    if (!confirm('Êtes-vous sûr de vouloir supprimer cette campagne ?')) return;
    this.campaignService.deleteCampaign(id).subscribe({
      next: () => {
        this.campaigns.update(list => list.filter(c => c.campaignId !== id));
        if (this.selectedCampaignId() === id) this.selectedCampaignId.set(null);
      },
      error: () => alert('Erreur lors de la suppression')
    });
  }

  // ── Label & badge helpers ──────────────────────────────────────────────────
  getStatusLabel(s: number): string {
    return ['Activée', 'Désactivée', 'Expirée', 'Purgée', 'Annulée'][s] ?? 'Inconnu';
  }

  getTypeLabel(t: number): string {
    return ['Générale', 'Ciblée', 'Externe'][t] ?? 'Inconnu';
  }

  statusBadgeClass(s: number): string {
    const map: Record<number, string> = { 0: 'badge badge--enabled', 1: 'badge badge--disabled', 2: 'badge badge--expired', 3: 'badge badge--purged', 4: 'badge badge--cancelled' };
    return map[s] ?? 'badge';
  }

  typeBadgeClass(t: number): string {
    const map: Record<number, string> = { 0: 'badge badge--general', 1: 'badge badge--targeted', 2: 'badge badge--external' };
    return map[t] ?? 'badge';
  }

  getCampaignBusinessNames(id: number): string {
    return this.campaignBusinessesMap().get(id) ?? '—';
  }
}