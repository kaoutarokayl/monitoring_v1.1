import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FleetHealth {
  totalAtms: number;
  onlineCount: number;
  offlineCount: number;
  warningCount: number;
  unknownCount: number;
  onlinePercent: number;
  offlinePercent: number;
  warningPercent: number;
}

export interface NetworkCashSummary {
  totalCashAvailable: number;
  atmsLowCash: number;
  atmsEmptyCash: number;
  totalAtmsMonitored: number;
}

export interface NetworkSla {
  availabilityPercent: number;
  totalSeconds: number;
  uptimeSeconds: number;
  downtimeSeconds: number;
  from: string;
  to: string;
}

export interface AtmStatusRow {
  clientId: number;
  clientName: string;
  status: string;
  statusLabel: string;
  networkAddress: string;
  branchName: string | null;
  active: boolean;
}

export interface NocSummary {
  fleetHealth: FleetHealth;
  cashSummary: NetworkCashSummary;
  sla: NetworkSla;
  atmStatuses: AtmStatusRow[];
  generatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class NocDashboardService {
  private readonly BASE = 'http://localhost:5239/api/noc';
  private http = inject(HttpClient);

  getSummary(from?: Date, to?: Date): Observable<NocSummary> {
    let params = new HttpParams();
    if (from) params = params.set('from', from.toISOString());
    if (to)   params = params.set('to',   to.toISOString());
    return this.http.get<NocSummary>(`${this.BASE}/summary`, { params });
  }
}
