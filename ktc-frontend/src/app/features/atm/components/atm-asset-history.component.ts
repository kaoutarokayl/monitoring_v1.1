import { Component, Input, OnInit, OnDestroy, inject, signal, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { AtmService, AtmAssetHistoryDto } from '../services/atm.service';
import { AtmRealtimeService } from '../services/atm-realtime.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-atm-asset-history',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './atm-asset-history.component.html',
  styleUrls: ['./atm-asset-history.component.css']
})
export class AtmAssetHistoryComponent implements OnInit, OnDestroy {
  @Input() clientId?: number;
  
  private route = inject(ActivatedRoute);
  private atmService = inject(AtmService);
  private realtimeService = inject(AtmRealtimeService);
  private ngZone = inject(NgZone);
  private destroy$ = new Subject<void>();

  isLoading = signal(true);
  error = signal<string | null>(null);

  historyData = signal<AtmAssetHistoryDto[]>([]);

  ngOnInit(): void {
    let idStr = this.route.snapshot.paramMap.get('id');
    if (!idStr && this.route.parent) {
      idStr = this.route.parent.snapshot.paramMap.get('id');
    }

    const finalId = this.clientId ?? (idStr ? Number(idStr) : null);

    if (finalId) {
      this.loadHistory(finalId);
      this.subscribeToRealtimeUpdates(finalId);
    } else {
      this.error.set("Aucun identifiant d'ATM fourni.");
      this.isLoading.set(false);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private subscribeToRealtimeUpdates(clientId: number): void {
    this.realtimeService.assetHistoryUpdates$
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        if (update.clientId !== clientId) return;
        this.ngZone.run(() => {
          // Reload history when new asset history record is added
          this.loadHistory(clientId);
        });
      });
  }

  loadHistory(id: number): void {
    this.isLoading.set(true);
    this.atmService.getAtmAssetHistory(id).subscribe({
      next: (data) => {
        this.historyData.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set("Erreur lors de la récupération de l'historique.");
        this.isLoading.set(false);
      }
    });
  }
}

