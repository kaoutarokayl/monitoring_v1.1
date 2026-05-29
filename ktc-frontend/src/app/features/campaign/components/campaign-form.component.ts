import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { CampaignService, BusinessDto } from '../services/campaign.service';
import { Campaign, CreateCampaignRequest } from '../models/campaign.models';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-campaign-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './campaign-form.component.html',
  styleUrls: ['./campaign-form.component.css']
})
export class CampaignFormComponent implements OnInit {
  private readonly campaignService = inject(CampaignService);
  private readonly router          = inject(Router);
  private readonly route           = inject(ActivatedRoute);
  private readonly fb              = inject(FormBuilder);

  // ── State ─────────────────────────────────────────────────────────────────
  form!: FormGroup;
  isLoading          = signal(false);
  isSubmitting       = signal(false);
  error              = signal<string | null>(null);
  isEditing          = signal(false);
  activeTab          = signal<'general' | 'display' | 'data'>('general');
  campaignId: number | null = null;

  // ── Businesses ────────────────────────────────────────────────────────────
  availableBusinesses = signal<BusinessDto[]>([]);
  selectedBusinessIds = signal<number[]>([]);

  // ── Options ───────────────────────────────────────────────────────────────
  campaignTypes = [
    { value: 0, label: 'General — Shown to all customers' },
    { value: 1, label: 'Targeted — Shown to specific customers' },
    { value: 2, label: 'External — Determined by external system' }
  ];

  campaignStatuses = [
    { value: 0, label: 'Enabled' },
    { value: 1, label: 'Disabled' },
    { value: 2, label: 'Expired' },
    { value: 3, label: 'Purged' },
    { value: 4, label: 'Cancelled' }
  ];

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.initForm();
    this.loadBusinesses();

    this.route.params.subscribe(params => {
      if (params['id']) {
        this.campaignId = +params['id'];
        this.isEditing.set(true);
        this.loadCampaign();
      }
    });
  }

  // ── Form init ──────────────────────────────────────────────────────────────
  private initForm(): void {
    this.form = this.fb.group({
      name:                 ['', [Validators.required, Validators.minLength(3)]],
      packageName:          [''],
      campaignType:         [0, Validators.required],
      campaignStatus:       [0, Validators.required],
      priority:             [5, [Validators.required, Validators.min(1), Validators.max(10)]],
      startDate:            ['', Validators.required],
      endDate:              ['', Validators.required],
      purgeDate:            ['', Validators.required],
      downloadId:           [0],
      interactive:          [false],
      campaignInTestmode:   [false],
      maxShows:             [0, Validators.min(0)],
      restHours:            [0, Validators.min(0)],
      maxShowMeLaterShows:  [0, Validators.min(0)],
      showMeLaterRestHours: [0, Validators.min(0)],
      externalId:           [''],
      campaignData:         [''],
      dynamicCampaignData:  ['']
    });
  }

  // ── Load businesses (pour la création) ────────────────────────────────────
  private loadBusinesses(): void {
    this.campaignService.getAllBusinesses().subscribe({
      next: (b) => {
        const businesses = b.map(x => ({
          ...x,
          businessId: Number(x.businessId)
        }));
        this.availableBusinesses.set(this.addSpecialBusinessOptions(businesses));
      },
      error: () => {} // non bloquant
    });
  }

  private addSpecialBusinessOptions(businesses: BusinessDto[]): BusinessDto[] {
    const items = [...businesses];

    if (!items.some(x => x.businessId === 0)) {
      items.unshift({
        businessId: 0,
        businessName: 'All Businesses',
        displayId: '0'
      });
    }

    if (!items.some(x => x.businessId === -1)) {
      items.unshift({
        businessId: -1,
        businessName: 'Default business',
        displayId: '-1'
      });
    }

    return items;
  }

  // ── Toggle business selection ───────────────────────────────────────────────
  toggleBusiness(id: number | string): void {
    const businessId = Number(id);
    this.selectedBusinessIds.update(ids =>
      ids.includes(businessId) ? ids.filter(x => x !== businessId) : [...ids, businessId]
    );
  }

  isBusinessSelected(id: number | string): boolean {
    return this.selectedBusinessIds().includes(Number(id));
  }

  // ── Load (edit mode) ───────────────────────────────────────────────────────
  private loadCampaign(): void {
    if (!this.campaignId) return;
    this.isLoading.set(true);

    forkJoin({
      campaign: this.campaignService.getCampaignById(this.campaignId),
      campaignBusinesses: this.campaignService.getCampaignBusinesses(this.campaignId).pipe(
        catchError((err) => {
          console.warn('Unable to load campaign businesses:', err);
          return of([]);
        })
      )
    }).subscribe({
      next: ({ campaign, campaignBusinesses }) => {
        this.populateForm(campaign);
        const businessIds = campaignBusinesses.map(cb => Number(cb.businessId)).filter(id => !Number.isNaN(id));
        this.selectedBusinessIds.set(businessIds);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Load error:', err);
        this.error.set('Unable to load campaign');
        this.isLoading.set(false);
      }
    });
  }

  // ── Populate ───────────────────────────────────────────────────────────────
  private populateForm(campaign: Campaign): void {
    this.form.patchValue({
      name:                 campaign.name                 ?? '',
      packageName:          campaign.packageName          ?? '',
      campaignType:         Number(campaign.campaignType  ?? 0),
      campaignStatus:       Number(campaign.campaignStatus ?? 0),
      priority:             Number(campaign.priority       ?? 5),
      startDate:            this.formatDate(campaign.startDate),
      endDate:              this.formatDate(campaign.endDate),
      purgeDate:            this.formatDate(campaign.purgeDate),
      downloadId:           campaign.downloadId           ?? 0,
      interactive:          campaign.interactive          ?? false,
      campaignInTestmode:   campaign.campaignInTestmode   ?? false,
      maxShows:             campaign.maxShows             ?? 0,
      restHours:            campaign.restHours            ?? 0,
      maxShowMeLaterShows:  campaign.maxShowMeLaterShows  ?? 0,
      showMeLaterRestHours: campaign.showMeLaterRestHours ?? 0,
      externalId:           campaign.externalId           ?? '',
      campaignData:         campaign.campaignData         ?? '',
      dynamicCampaignData:  campaign.dynamicCampaignData  ?? ''
    });
  }

  // ── Submit ─────────────────────────────────────────────────────────────────
  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Please fill in all required fields correctly');
      if (this.form.get('name')?.invalid || this.form.get('startDate')?.invalid ||
          this.form.get('endDate')?.invalid || this.form.get('purgeDate')?.invalid ||
          this.form.get('priority')?.invalid) {
        this.activeTab.set('general');
      }
      return;
    }

    // Validation : au moins un business requis en mode création
    if (!this.isEditing() && this.selectedBusinessIds().length === 0) {
      this.error.set('Veuillez sélectionner au moins un business pour cette campagne');
      this.activeTab.set('general');
      return;
    }

    this.isSubmitting.set(true);
    this.error.set(null);

    const raw = this.form.value;
    const request: CreateCampaignRequest = {
      ...raw,
      campaignType:         Number(raw.campaignType),
      campaignStatus:       Number(raw.campaignStatus),
      priority:             Number(raw.priority),
      downloadId:           Number(raw.downloadId),
      maxShows:             Number(raw.maxShows),
      restHours:            Number(raw.restHours),
      maxShowMeLaterShows:  Number(raw.maxShowMeLaterShows),
      showMeLaterRestHours: Number(raw.showMeLaterRestHours),
      startDate: raw.startDate ? new Date(raw.startDate) : undefined,
      endDate:   raw.endDate   ? new Date(raw.endDate)   : undefined,
      purgeDate: raw.purgeDate ? new Date(raw.purgeDate) : undefined,
      businessIds: this.selectedBusinessIds(),
    };

    const operation = this.isEditing() && this.campaignId
      ? this.campaignService.updateCampaign(this.campaignId, request)
      : this.campaignService.createCampaign(request);

    operation.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigate(['/campaign']);
      },
      error: (err) => {
        console.error('Save error:', err);
        this.error.set(err.error?.error || 'Error while saving');
        this.isSubmitting.set(false);
      }
    });
  }

  // ── Cancel ─────────────────────────────────────────────────────────────────
  cancel(): void {
    this.router.navigate(['/campaign']);
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  private formatDate(date: any): string {
    if (!date) return '';
    try { return new Date(date).toISOString().split('T')[0]; }
    catch { return ''; }
  }

  getFormError(field: string): string | null {
    const control = this.form.get(field);
    if (!control || !control.errors || !control.touched) return null;
    if (control.errors['required'])  return 'This field is required';
    if (control.errors['minlength']) return `Minimum ${control.errors['minlength'].requiredLength} characters`;
    if (control.errors['min'])       return `Minimum value: ${control.errors['min'].min}`;
    if (control.errors['max'])       return `Maximum value: ${control.errors['max'].max}`;
    return 'Invalid value';
  }
}