import { Injectable, inject } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { SignalrService } from '../../../core/services/signalr.service';

/**
 * Service pour écouter les mises à jour en temps réel via SignalR pour:
 * - Status des composants
 * - Asset History
 * - Cash cassettes et status
 */
@Injectable({ providedIn: 'root' })
export class AtmRealtimeService {
  private signalr = inject(SignalrService);

  // Subjects pour les différents types de mises à jour
  private statusUpdateSubject = new Subject<CurrentStatusUpdate>();
  private assetHistorySubject = new Subject<AssetHistoryUpdate>();
  private cassetteUpdateSubject = new Subject<CassetteUpdate>();
  private cashStatusSubject = new Subject<CashStatusUpdate>();
  private journalSubject = new Subject<ClientUpdate>();
  private transactionSubject = new Subject<ClientUpdate>();
  private videoJournalSubject = new Subject<ClientUpdate>();

  // Observables publics pour les composants
  statusUpdates$ = this.statusUpdateSubject.asObservable();
  assetHistoryUpdates$ = this.assetHistorySubject.asObservable();
  cassetteUpdates$ = this.cassetteUpdateSubject.asObservable();
  cashStatusUpdates$ = this.cashStatusSubject.asObservable();
  journalUpdates$ = this.journalSubject.asObservable();
  transactionUpdates$ = this.transactionSubject.asObservable();
  videoJournalUpdates$ = this.videoJournalSubject.asObservable();

  constructor() {
    this.setupSignalRListeners();
  }

  /**
   * Configure les récepteurs SignalR pour les différents types de mises à jour.
   */
  private setupSignalRListeners(): void {
    this.signalr.connectMonitoringHub().then(conn => {
      // Écouter les mises à jour de status
      conn.on('ReceiveStatusUpdate', (payload: unknown) => {
        const update = this.normalizeStatusUpdate(payload);
        if (update) {
          this.statusUpdateSubject.next(update);
        }
      });

      // Écouter les mises à jour d'asset history
      conn.on('ReceiveAssetHistoryUpdate', (payload: unknown) => {
        const update = this.normalizeAssetHistoryUpdate(payload);
        if (update) {
          this.assetHistorySubject.next(update);
        }
      });

      // Écouter les mises à jour de cassettes
      conn.on('ReceiveCassetteUpdate', (payload: unknown) => {
        const update = this.normalizeCassetteUpdate(payload);
        if (update) {
          this.cassetteUpdateSubject.next(update);
        }
      });

      // Écouter les mises à jour de status cash
      conn.on('ReceiveCashStatusUpdate', (payload: unknown) => {
        const update = this.normalizeCashStatusUpdate(payload);
        if (update) {
          this.cashStatusSubject.next(update);
        }
      });

      // Écouter les mises à jour du journal électronique
      conn.on('ReceiveJournalUpdate', (payload: unknown) => {
        const update = this.normalizeClientUpdate(payload);
        if (update) {
          this.journalSubject.next(update);
        }
      });

      // Écouter les mises à jour des transactions
      conn.on('ReceiveTransactionUpdate', (payload: unknown) => {
        const update = this.normalizeClientUpdate(payload);
        if (update) {
          this.transactionSubject.next(update);
        }
      });

      // Écouter les mises à jour du video journal
      conn.on('ReceiveVideoJournalUpdate', (payload: unknown) => {
        const update = this.normalizeClientUpdate(payload);
        if (update) {
          this.videoJournalSubject.next(update);
        }
      });
    }).catch(err => {
      console.warn('Failed to setup SignalR listeners', err);
    });
  }

  // ── Normalization methods ────────────────────────────────────────────

  private normalizeStatusUpdate(payload: unknown): CurrentStatusUpdate | null {
    if (!payload || typeof payload !== 'object') return null;
    const p = payload as Record<string, unknown>;
    const g = (c: string, pascal: string) => p[c] !== undefined ? p[c] : p[pascal];

    return {
      clientId: Number(g('clientId', 'ClientId')),
      componentId: Number(g('componentId', 'ComponentId')),
      propertyId: Number(g('propertyId', 'PropertyId')),
      valueId: Number(g('valueId', 'ValueId')),
      numericValue: g('numericValue', 'NumericValue') ? Number(g('numericValue', 'NumericValue')) : undefined
    };
  }

  private normalizeAssetHistoryUpdate(payload: unknown): AssetHistoryUpdate | null {
    if (!payload || typeof payload !== 'object') return null;
    const p = payload as Record<string, unknown>;
    const g = (c: string, pascal: string) => p[c] !== undefined ? p[c] : p[pascal];

    return {
      clientId: Number(g('clientId', 'ClientId')),
      date: new Date(String(g('date', 'Date'))),
      componentId: Number(g('componentId', 'ComponentId')),
      propertyId: Number(g('propertyId', 'PropertyId')),
      oldValueId: Number(g('oldValueId', 'OldValueId')),
      newValueId: Number(g('newValueId', 'NewValueId')),
      oldNumericValue: g('oldNumericValue', 'OldNumericValue') ? Number(g('oldNumericValue', 'OldNumericValue')) : undefined,
      newNumericValue: g('newNumericValue', 'NewNumericValue') ? Number(g('newNumericValue', 'NewNumericValue')) : undefined,
      comments: String(g('comments', 'Comments') ?? '')
    };
  }

  private normalizeCassetteUpdate(payload: unknown): CassetteUpdate | null {
    if (!payload || typeof payload !== 'object') return null;
    const p = payload as Record<string, unknown>;
    const g = (c: string, pascal: string) => p[c] !== undefined ? p[c] : p[pascal];

    return {
      clientId: Number(g('clientId', 'ClientId')),
      cassetteId: Number(g('cassetteId', 'CassetteId')),
      cassetteNumber: Number(g('cassetteNumber', 'CassetteNumber')),
      cashUnitId: Number(g('cashUnitId', 'CashUnitId'))
    };
  }

  private normalizeCashStatusUpdate(payload: unknown): CashStatusUpdate | null {
    if (!payload || typeof payload !== 'object') return null;
    const p = payload as Record<string, unknown>;
    const g = (c: string, pascal: string) => p[c] !== undefined ? p[c] : p[pascal];

    return {
      clientId: Number(g('clientId', 'ClientId')),
      cashUnitId: Number(g('cashUnitId', 'CashUnitId')),
      logicalStatusId: Number(g('logicalStatusId', 'LogicalStatusId')),
      physicalStatusId: Number(g('physicalStatusId', 'PhysicalStatusId'))
    };
  }

  private normalizeClientUpdate(payload: unknown): ClientUpdate | null {
    if (!payload || typeof payload !== 'object') return null;
    const p = payload as Record<string, unknown>;
    const clientId = p['clientId'] !== undefined ? p['clientId'] : p['ClientId'];
    if (clientId === undefined) return null;
    return { clientId: Number(clientId) };
  }
}

// ── Type definitions ─────────────────────────────────────────────────────

export interface CurrentStatusUpdate {
  clientId: number;
  componentId: number;
  propertyId: number;
  valueId: number;
  numericValue?: number;
}

export interface AssetHistoryUpdate {
  clientId: number;
  date: Date;
  componentId: number;
  propertyId: number;
  oldValueId: number;
  newValueId: number;
  oldNumericValue?: number;
  newNumericValue?: number;
  comments: string;
}

export interface CassetteUpdate {
  clientId: number;
  cassetteId: number;
  cassetteNumber: number;
  cashUnitId: number;
}

export interface CashStatusUpdate {
  clientId: number;
  cashUnitId: number;
  logicalStatusId: number;
  physicalStatusId: number;
}

export interface ClientUpdate {
  clientId: number;
}
