import { Routes } from '@angular/router';

import { LoginComponent } from './features/auth/components/login.component';
import { AccessDeniedComponent } from './features/auth/components/access-denied.component';
import { DashboardComponent } from './features/dashboard/components/dashboard.component';
import { AdminLayoutComponent } from './features/admin/components/admin-layout.component';
import { AtmListComponent } from './features/atm/components/atm-list.component';
import { AtmFormComponent } from './features/atm/components/atm-form.component';
import { AtmMapComponent } from './features/atm/components/atm-map.component';
import { AtmDetailLayoutComponent } from './features/atm/components/atm-detail-layout.component';
import { AtmStatusComponent } from './features/atm/components/atm-status.component';
import { AtmAssetHistoryComponent } from './features/atm/components/atm-asset-history.component';
import { GroupComponent } from './features/group/components/group.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { BusinessListComponent } from './features/business/components/business-list.component';
import { BusinessFormComponent } from './features/business/components/business-form.component';
import { RegionListComponent } from './features/region/components/region-list.component';
import { RegionFormComponent } from './features/region/components/region-form.component';
import { BranchFormComponent } from './features/branch/components/branch-form.component';
import { BranchListComponent } from './features/branch/components/branch-list.component';
import { GroupFormComponent } from './features/group/components/group-form.component';
import { AtmGeneralComponent } from './features/atm/components/atm-general.component';
import { AtmSoftwareInfoComponent } from './features/atm/components/atm-software-info.component';
import { AtmCertificatesComponent } from './features/atm/components/atm-certificates.component';
import { AtmTicketsComponent } from './features/atm/components/atm-tickets.component';
import { AtmActionsComponent } from './features/atm/components/atm-actions.component';
import { AtmElectronicJournalComponent } from './features/atm/components/atm-electronic-journal.component';
import { AtmSchedulesComponent } from './features/atm/components/atm-schedules.component';
import { AtmUploadsComponent } from './features/atm/components/atm-uploads.component';
import { AtmTransactionsComponent } from './features/atm/components/atm-transactions.component';
import { AtmVideoJournalComponent } from './features/atm/components/atm-video-journal.component';
import { AtmAvailabilityComponent } from './features/atm/components/atm-availability.component';
import { AtmCashCassetteComponent } from './features/atm/components/atm-cash-cassette.component';

// ── Campaign imports ──────────────────────────────────────────────────────────
import { CampaignListComponent } from './features/campaign/components/campaign-list.component';
import { CampaignFormComponent } from './features/campaign/components/campaign-form.component';
import { CampaignMarketingControlComponent } from './features/campaign/components/campaign-marketing-control.component';
import { TicketSearchComponent } from './features/ticket-search/components/ticket-search.component';

/** Rôles AD — doit correspondre aux groupes déclarés dans Program.cs */
const WRITE_ROLES = ['Support'];

export const routes: Routes = [
  { path: 'login',          component: LoginComponent },
  { path: 'access-denied',  component: AccessDeniedComponent },

  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },

      // ── Campagnes Marketing (hors AdminLayout, route racine) ───────────────
      // IMPORTANT: 'marketing' et 'create' DOIVENT être avant ':id' pour éviter que Angular
      // interprète ces mots comme un campaignId
      { path: 'ticket-search',         component: TicketSearchComponent },
      { path: 'campaign',              component: CampaignListComponent },
      { path: 'campaign/create',       component: CampaignFormComponent },
      { path: 'campaign/:id/edit',     component: CampaignFormComponent },
      { path: 'campaign/:id',          component: CampaignListComponent },
      { path: 'marketing',             component: CampaignMarketingControlComponent, outlet: 'modal' },

      // === ADMINISTRATION LAYOUT ===
      {
        path: 'admin',
        component: AdminLayoutComponent,
        children: [
          { path: '', redirectTo: 'atms', pathMatch: 'full' },

          // ── ATMs ──────────────────────────────────────────────────────────
          { path: 'atms',           component: AtmListComponent },
          { path: 'atms/map',       component: AtmMapComponent },
          { path: 'atms/create',    component: AtmFormComponent,    canActivate: [roleGuard], data: { roles: WRITE_ROLES } },
          { path: 'atms/:id/edit',  component: AtmFormComponent,    canActivate: [roleGuard], data: { roles: WRITE_ROLES } },
          {
            path: 'atms/:id',
            component: AtmDetailLayoutComponent,
            children: [
              { path: '', redirectTo: 'general', pathMatch: 'full' },
              { path: 'general',            component: AtmGeneralComponent },
              { path: 'status',             component: AtmStatusComponent },
              { path: 'asset-history',      component: AtmAssetHistoryComponent },
              { path: 'software-info',      component: AtmSoftwareInfoComponent },
              { path: 'certificates',       component: AtmCertificatesComponent },
              { path: 'tickets',            component: AtmTicketsComponent },
              { path: 'uploads',            component: AtmUploadsComponent },
              { path: 'actions',            component: AtmActionsComponent },
              { path: 'schedules',          component: AtmSchedulesComponent },
              { path: 'electronic-journal', component: AtmElectronicJournalComponent },
              { path: 'transactions',       component: AtmTransactionsComponent },
              { path: 'videojournal',       component: AtmVideoJournalComponent },
              { path: 'availability',       component: AtmAvailabilityComponent },
              { path: 'cash-cassettes',     component: AtmCashCassetteComponent }
            ]
          },

          // ── Businesses ────────────────────────────────────────────────────
          { path: 'businesses',          component: BusinessListComponent },
          { path: 'businesses/create',   component: BusinessFormComponent,  canActivate: [roleGuard], data: { roles: WRITE_ROLES } },
          { path: 'businesses/:id/edit', component: BusinessFormComponent,  canActivate: [roleGuard], data: { roles: WRITE_ROLES } },

          // ── Regions ───────────────────────────────────────────────────────
          { path: 'regions',          component: RegionListComponent },
          { path: 'regions/create',   component: RegionFormComponent,   canActivate: [roleGuard], data: { roles: WRITE_ROLES } },
          { path: 'regions/:id/edit', component: RegionFormComponent,   canActivate: [roleGuard], data: { roles: WRITE_ROLES } },

          // ── Branches ──────────────────────────────────────────────────────
          { path: 'branches',          component: BranchListComponent },
          { path: 'branches/create',   component: BranchFormComponent,  canActivate: [roleGuard], data: { roles: WRITE_ROLES } },
          { path: 'branches/:id/edit', component: BranchFormComponent,  canActivate: [roleGuard], data: { roles: WRITE_ROLES } },

          // ── Groups ────────────────────────────────────────────────────────
          { path: 'groups',          component: GroupComponent },
          { path: 'groups/create',   component: GroupFormComponent,    canActivate: [roleGuard], data: { roles: WRITE_ROLES } },
          { path: 'groups/:id/edit', component: GroupFormComponent,    canActivate: [roleGuard], data: { roles: WRITE_ROLES } }
        ]
      }
    ]
  },

  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**', redirectTo: '/login' }
];
