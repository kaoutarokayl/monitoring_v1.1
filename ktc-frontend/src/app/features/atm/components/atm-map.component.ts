import {
  Component,
  OnInit,
  OnDestroy,
  AfterViewInit,
  inject,
  signal,
  computed,
  ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';
import { AtmService, ClientAtm } from '../services/atm.service';

// Leaflet est chargé via CDN (à ajouter dans index.html si ce n'est pas déjà fait) :
//   <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
//   <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
declare const L: any;

@Component({
  selector: 'app-atm-map',
  standalone: true,
  imports: [CommonModule, DecimalPipe],
  templateUrl: './atm-map.component.html',
  styleUrls: ['./atm-map.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AtmMapComponent implements OnInit, AfterViewInit, OnDestroy {
  private atmService = inject(AtmService);
  private router     = inject(Router);

  // ── State ──────────────────────────────────────────────────────────────────
  atms             = signal<ClientAtm[]>([]);
  isLoading        = signal(true);
  error            = signal<string | null>(null);
  searchQuery      = signal('');
  filterStatus     = signal<'all' | 'active' | 'inactive'>('all');
  selectedAtm      = signal<ClientAtm | null>(null);
  sidebarCollapsed = signal(false);

  // ── Computed ───────────────────────────────────────────────────────────────
  filtered = computed(() => {
    const q      = this.searchQuery().toLowerCase();
    const status = this.filterStatus();
    return this.atms().filter(a => {
      const matchQ =
        !q ||
        a.clientName.toLowerCase().includes(q) ||
        a.networkAddress.includes(q) ||
        String(a.clientId).includes(q);
      const matchS =
        status === 'all' ||
        (status === 'active'   && a.active) ||
        (status === 'inactive' && !a.active);
      return matchQ && matchS;
    });
  });

  totalCount    = computed(() => this.atms().length);
  activeCount   = computed(() => this.atms().filter(a => a.active).length);
  inactiveCount = computed(() => this.atms().filter(a => !a.active).length);

  // ── Leaflet internals ──────────────────────────────────────────────────────
  private map: any;
  private markersLayer: any;
  private markerMap = new Map<number, any>(); // clientId → L.Marker

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.load();
  }

  ngAfterViewInit(): void {
    this.initMap();
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

  // ── Data ───────────────────────────────────────────────────────────────────
  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.atmService.getClients().subscribe({
      next: data => {
        this.atms.set(data);
        this.isLoading.set(false);
        this.renderMarkers();
      },
      error: err => {
        this.error.set(err?.error?.message ?? 'Impossible de charger les ATMs');
        this.isLoading.set(false);
      }
    });
  }

  // ── Map init ───────────────────────────────────────────────────────────────
  private initMap(): void {
    if (typeof L === 'undefined') {
      console.error('Leaflet non chargé. Ajoutez les liens CDN dans index.html.');
      return;
    }

    this.map = L.map('atm-map', {
      center: [31.7917, -7.0926], // Centre du Maroc
      zoom: 6,
      zoomControl: true,
      // Style de zoom plus épuré
      zoomAnimation: true
    });

    // Tuile OpenStreetMap avec style plus clair
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      maxZoom: 19,
      opacity: 0.9
    }).addTo(this.map);

    this.markersLayer = L.featureGroup().addTo(this.map);

    // Injecte les styles Leaflet popup pour correspondre à l'interface
    this.injectLeafletStyles();

    // Si les données sont déjà chargées avant l'init de la carte
    if (this.atms().length > 0) {
      this.renderMarkers();
    }
  }

  // ── Popup styles injection ─────────────────────────────────────────────────
  private injectLeafletStyles(): void {
    const style = document.createElement('style');
    style.textContent = `
      .leaflet-popup-content-wrapper {
        border-radius: 10px !important;
        box-shadow: 0 4px 6px -1px rgba(0,0,0,0.07), 0 10px 30px -4px rgba(0,0,0,0.12) !important;
        border: 1px solid var(--border, #e5e7eb) !important;
        padding: 0 !important;
        overflow: hidden;
      }
      .leaflet-popup-content {
        margin: 0 !important;
        min-width: 220px;
      }
      .leaflet-popup-tip-container { margin-top: -1px; }
      .leaflet-popup-tip { box-shadow: none !important; }
    `;
    document.head.appendChild(style);
  }

  // ── Markers ────────────────────────────────────────────────────────────────
  private renderMarkers(): void {
    if (!this.map || !this.markersLayer) return;

    this.markersLayer.clearLayers();
    this.markerMap.clear();

    for (const atm of this.atms()) {
      if (atm.latitude === 0 && atm.longitude === 0) continue;
      if (!atm.latitude && !atm.longitude) continue;

      const icon = this.buildIcon(atm.active);

      const marker = L.marker([atm.latitude, atm.longitude], { icon })
        .bindPopup(this.buildPopupHtml(atm), { maxWidth: 280, minWidth: 220 })
        .on('click', () => this.selectAtm(atm));

      this.markersLayer.addLayer(marker);
      this.markerMap.set(atm.clientId, marker);
    }

    if (this.markersLayer.getLayers().length > 0) {
      this.map.fitBounds(this.markersLayer.getBounds(), { padding: [48, 48] });
    }
  }

  private buildIcon(active: boolean): any {
    // Couleurs alignées sur l'interface existante
    const color  = active ? '#16a34a' : '#dc2626';
    const shadow = active ? 'rgba(22,163,74,0.25)' : 'rgba(220,38,38,0.25)';

    const svg = `
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 42" width="32" height="42">
        <filter id="pin-shadow">
          <feDropShadow dx="0" dy="2" stdDeviation="2" flood-color="${shadow}"/>
        </filter>
        <path d="M16 1C8.268 1 2 7.268 2 15c0 10.5 14 26 14 26S30 25.5 30 15C30 7.268 23.732 1 16 1z"
              fill="${color}" filter="url(#pin-shadow)"/>
        <path d="M16 1C8.268 1 2 7.268 2 15c0 10.5 14 26 14 26S30 25.5 30 15C30 7.268 23.732 1 16 1z"
              fill="none" stroke="white" stroke-width="1.5" opacity="0.6"/>
        <circle cx="16" cy="15" r="5.5" fill="white" opacity="0.95"/>
        <circle cx="16" cy="15" r="2.5" fill="${color}"/>
      </svg>`;

    return L.divIcon({
      html: svg,
      className: '',
      iconSize: [32, 42],
      iconAnchor: [16, 42],
      popupAnchor: [0, -44]
    });
  }

  private buildPopupHtml(atm: ClientAtm): string {
    const statusColor = atm.active ? '#16a34a' : '#dc2626';
    const statusBg    = atm.active ? '#f0fdf4' : '#fef2f2';
    const statusLabel = atm.active ? 'Actif' : 'Inactif';
    const conn = this.connectableLabel(atm.connectable);

    return `
      <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; overflow: hidden;">

        <!-- Header -->
        <div style="padding: 12px 14px 10px; background: #f9fafb; border-bottom: 1px solid #f0f0f0; display: flex; align-items: center; gap: 8px;">
          <span style="width: 8px; height: 8px; border-radius: 50%; background: ${statusColor}; display: inline-block; box-shadow: 0 0 0 3px ${statusBg}; flex-shrink:0;"></span>
          <div style="overflow: hidden; flex: 1;">
            <div style="font-weight: 800; font-size: 13px; color: #111827; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${atm.clientName}</div>
            <div style="font-size: 11px; color: #9ca3af; font-family: 'SFMono-Regular', Consolas, monospace; margin-top: 1px;">#${atm.clientId}</div>
          </div>
          <span style="padding: 2px 8px; border-radius: 999px; font-size: 10px; font-weight: 700; background: ${statusBg}; color: ${statusColor}; letter-spacing: 0.03em; border: 1px solid ${statusColor}22; flex-shrink: 0;">${statusLabel}</span>
        </div>

        <!-- Body -->
        <div style="padding: 10px 14px 12px;">
          <table style="font-size: 12px; border-collapse: collapse; width: 100%;">
            <tr>
              <td style="padding: 3px 10px 3px 0; color: #9ca3af; font-weight: 700; font-size: 10px; text-transform: uppercase; letter-spacing: 0.06em; white-space: nowrap;">IP</td>
              <td style="padding: 3px 0;">
                <code style="background: #f3f4f6; border: 1px solid #e5e7eb; border-radius: 4px; padding: 1px 6px; font-size: 11px; color: #111827; font-family: 'SFMono-Regular', Consolas, monospace;">${atm.networkAddress}</code>
              </td>
            </tr>
            <tr>
              <td style="padding: 3px 10px 3px 0; color: #9ca3af; font-weight: 700; font-size: 10px; text-transform: uppercase; letter-spacing: 0.06em; white-space: nowrap;">Connexion</td>
              <td style="padding: 3px 0; color: #374151;">${conn}</td>
            </tr>
            ${atm.hardwareTypeName ? `
            <tr>
              <td style="padding: 3px 10px 3px 0; color: #9ca3af; font-weight: 700; font-size: 10px; text-transform: uppercase; letter-spacing: 0.06em; white-space: nowrap;">Hardware</td>
              <td style="padding: 3px 0; color: #374151;">${atm.hardwareTypeName}</td>
            </tr>` : ''}
          </table>
        </div>

        <!-- Footer -->
        <div style="padding: 8px 14px 12px; display: flex; justify-content: flex-end;">
          <a href="/admin/atms/${atm.clientId}/edit"
             style="display: inline-flex; align-items: center; gap: 5px; background: #4f46e5; color: white; padding: 5px 12px; border-radius: 6px; font-size: 11px; font-weight: 700; text-decoration: none; letter-spacing: 0.02em; transition: background 120ms;"
             onmouseover="this.style.background='#4338ca'"
             onmouseout="this.style.background='#4f46e5'">
            ✏️ Modifier
          </a>
        </div>

      </div>`;
  }

  // ── Public actions ─────────────────────────────────────────────────────────
  selectAtm(atm: ClientAtm): void {
    this.selectedAtm.set(atm);
    const marker = this.markerMap.get(atm.clientId);
    if (marker && this.map) {
      this.map.setView([atm.latitude, atm.longitude], 14, { animate: true });
      marker.openPopup();
    }
  }

  flyTo(atm: ClientAtm): void {
    if (this.map) {
      this.map.flyTo([atm.latitude, atm.longitude], 14, { animate: true, duration: 1 });
    }
    this.selectedAtm.set(atm);
  }

  fitAll(): void {
    if (this.map && this.markersLayer?.getLayers().length > 0) {
      this.map.fitBounds(this.markersLayer.getBounds(), { padding: [48, 48], animate: true });
    }
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
    // Invalidate map size après l'animation de la sidebar
    setTimeout(() => this.map?.invalidateSize(), 240);
  }

  goBack(): void              { this.router.navigate(['/admin/atms']); }
  goEdit(id: number): void   { this.router.navigate(['/admin/atms', id, 'edit']); }

  connectableLabel(val: number): string {
    return ['—', 'Non connectable', 'IP Statique', 'IP Dynamique'][val] ?? String(val);
  }
}