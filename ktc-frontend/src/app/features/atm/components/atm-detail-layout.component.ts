import { Component, OnInit, OnDestroy, inject, signal, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { AtmService, ClientAtm } from '../services/atm.service';
import { LayoutService } from '../../../core/services/layout.service';
import { SignalrService } from '../../../core/services/signalr.service';

@Component({
  selector: 'app-atm-detail-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './atm-detail-layout.component.html',
  styleUrls: ['./atm-detail-layout.component.css']
})
export class AtmDetailLayoutComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private atmService = inject(AtmService);
  private layoutService = inject(LayoutService);
  private signalr = inject(SignalrService);
  private ngZone = inject(NgZone);

  atmId = signal<number | null>(null);
  atm = signal<ClientAtm | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    // Minimiser la sidebar automatiquement quand on entre dans les détails ATM
    this.layoutService.setSidebarCollapsed(true);

    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        this.atmId.set(Number(idParam));
        this.loadAtm();
        this.setupRealtimeUpdates(Number(idParam));
      } else {
        this.router.navigate(['/admin/atms']);
      }
    });
  }

  ngOnDestroy(): void {
    // Restaurer la sidebar à la sortie des détails
    this.layoutService.setSidebarCollapsed(false);
    
    // Laisser le groupe quand on quitte la page
    const id = this.atmId();
    if (id) {
      void this.leaveAtmGroup(id);
    }
  }

  /** Établir la connexion SignalR et rejoindre les groupes pour cet ATM. */
  private async setupRealtimeUpdates(clientId: number): Promise<void> {
    try {
      const conn = await this.signalr.connectMonitoringHub();
      
      // Rejoindre les groupes spécifiques à cet ATM
      await conn.invoke('JoinAtmGroup', clientId);
    } catch (err) {
      // Hub non disponible ou erreur de connexion - pas grave, on continue avec HTTP
      console.warn('SignalR not available for real-time updates', err);
    }
  }

  /** Quitter les groupes SignalR pour cet ATM. */
  private async leaveAtmGroup(clientId: number): Promise<void> {
    try {
      const conn = this.signalr.getConnection();
      if (conn) {
        await conn.invoke('LeaveAtmGroup', clientId);
      }
    } catch {
      // Ignorer les erreurs de déconnexion
    }
  }

  loadAtm(): void {
    const id = this.atmId();
    if (!id) return;
    this.atmService.getClientById(id).subscribe({
      next: (data) => {
        this.atm.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set("ATM introuvable");
        this.isLoading.set(false);
      }
    });
  }
}

