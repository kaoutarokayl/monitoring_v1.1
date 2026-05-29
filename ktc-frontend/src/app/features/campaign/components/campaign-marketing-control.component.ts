import { Component, OnInit, inject, Input, Output, EventEmitter, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { CampaignService } from '../services/campaign.service';
import { AuthService } from '../../../core/services/auth.service';

type PendingAction = 'global' | 'business' | null;

@Component({
  selector: 'app-campaign-marketing-control',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './campaign-marketing-control.component.html',
  styleUrls: ['./campaign-marketing-control.component.css']
})
export class CampaignMarketingControlComponent implements OnInit {
  private readonly campaignService = inject(CampaignService);
  private readonly authService = inject(AuthService);
  private readonly route           = inject(ActivatedRoute);
  private readonly router          = inject(Router);

  @Input() businessId: number | null = null;
  @Output() close = new EventEmitter<void>();

  // ── State ─────────────────────────────────────────────────────────────────
  businessName    = signal<string>('No business selected');
  globalEnabled   = signal(false);
  businessEnabled = signal(false);
  isLoading       = signal(true);
  error           = signal<string | null>(null);
  statusMessage   = signal<string | null>(null);

  // ── RBAC: Vérifier si Support (peut éditer) ─────────────────────────────────
  canEditMarketing = computed(() => this.authService.isSupport());

  // ── Confirmation dialog ────────────────────────────────────────────────────
  /** Which action is pending confirmation: 'global', 'business', or null */
  pendingAction = signal<PendingAction>(null);

  /** True when the pending action would DISABLE marketing (→ red/warning style) */
  isDisableAction = computed<boolean>(() => {
    const action = this.pendingAction();
    if (action === 'global')   return this.globalEnabled();
    if (action === 'business') return this.businessEnabled();
    return false;
  });

  /** Human-readable confirmation message */
  confirmMessage = computed<string>(() => {
    const action = this.pendingAction();
    if (action === 'global') {
      return this.globalEnabled()
        ? 'Are you sure you want to stop global marketing?'
        : 'Are you sure you want to enable global marketing?';
    }
    if (action === 'business') {
      return this.businessEnabled()
        ? `Are you sure you want to stop marketing for this business code?`
        : `Are you sure you want to enable marketing for ${this.businessName()}?`;
    }
    return '';
  });

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      const businessIdParam = params.get('businessId');
      const businessId = businessIdParam ? Number(businessIdParam) : null;

      this.businessId = businessId;
      this.loadStatus();
      this.loadBusinessName();
    });
  }

  // ── Load ───────────────────────────────────────────────────────────────────
  private loadBusinessName(): void {
    const id = this.businessId;
    if (!id) { this.businessName.set('No business selected'); return; }

    this.campaignService.getAllBusinesses().subscribe({
      next: (businesses) => {
        const biz = businesses.find(b => b.businessId === id);
        this.businessName.set(biz?.businessName ?? `Business #${id}`);
      },
      error: () => this.businessName.set(`Business #${id}`)
    });
  }

  private loadStatus(): void {
    this.error.set(null);
    this.isLoading.set(true);

    this.campaignService.getGlobalMarketingState().subscribe({
      next: (global) => {
        this.globalEnabled.set(global.enabled);
        const id = this.businessId;
        if (id) {
          this.campaignService.getBusinessMarketingState(id).subscribe({
            next:  (biz) => { this.businessEnabled.set(biz.enabled); this.isLoading.set(false); },
            error: ()    => { this.error.set('Unable to load business marketing state.'); this.isLoading.set(false); }
          });
        } else {
          this.businessEnabled.set(false);
          this.isLoading.set(false);
        }
      },
      error: () => {
        this.error.set('Unable to load global marketing state.');
        this.isLoading.set(false);
      }
    });
  }

  // ── Request confirmation ───────────────────────────────────────────────────
  requestGlobalToggle(): void {
    if (!this.canEditMarketing()) return;  // Vérification de sécurité
    this.statusMessage.set(null);
    this.pendingAction.set('global');
  }

  requestBusinessToggle(): void {
    if (!this.businessId || !this.canEditMarketing()) return;  // Vérification de sécurité
    this.statusMessage.set(null);
    this.pendingAction.set('business');
  }

  // ── Confirm / Cancel ───────────────────────────────────────────────────────
  cancelConfirm(): void {
    this.pendingAction.set(null);
  }

  confirmAction(): void {
    const action = this.pendingAction();
    this.pendingAction.set(null);

    if (action === 'global')   this.executeGlobalToggle();
    if (action === 'business') this.executeBusinessToggle();
  }

  // ── Execute actions ────────────────────────────────────────────────────────
  private executeGlobalToggle(): void {
    const target = !this.globalEnabled();
    this.campaignService.setGlobalMarketingState(target).subscribe({
      next: (result) => {
        this.globalEnabled.set(result.enabled);
        this.statusMessage.set(
          `Global marketing is now ${result.enabled ? 'enabled' : 'disabled'}.`
        );
      },
      error: () => this.error.set('Error updating global marketing state.')
    });
  }

  private executeBusinessToggle(): void {
    const id = this.businessId;
    if (!id) return;
    const target = !this.businessEnabled();
    this.campaignService.setBusinessMarketingState(id, target).subscribe({
      next: (result) => {
        this.businessEnabled.set(result.enabled);
        this.statusMessage.set(
          `Marketing for ${this.businessName()} is now ${result.enabled ? 'enabled' : 'disabled'}.`
        );
      },
      error: () => this.error.set('Error updating business marketing state.')
    });
  }

  // ── Close ──────────────────────────────────────────────────────────────────
  closeOverlay(): void {
    if (this.close.observers.length) {
      this.close.emit();
      return;
    }

    this.router.navigate([{ outlets: { modal: null } }]);
  }
}