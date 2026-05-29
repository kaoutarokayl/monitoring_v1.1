import { Injectable, inject } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType
} from '@microsoft/signalr';
import { AuthService } from './auth.service';

/** Même origine que l’API REST (voir AtmService). */
const API_ORIGIN = 'http://localhost:5239';

/**
 * Connexion au hub `/hubs/monitoring` avec authentification JWT.
 * Le client SignalR envoie le jeton comme paramètre de requête `access_token`
 * (voir configuration JwtBearer OnMessageReceived côté ASP.NET Core).
 */
@Injectable({ providedIn: 'root' })
export class SignalrService {
  private readonly auth = inject(AuthService);
  private connection: HubConnection | null = null;

  /**
   * Démarre la connexion (idempotent). Utilise `accessTokenFactory` :
   * en transport WebSocket, le SDK ajoute `?access_token=<jwt>` à l’URL.
   */
  async connectMonitoringHub(): Promise<HubConnection> {
    const token = this.auth.getToken();
    if (!token) {
      throw new Error('Non authentifié : impossible de se connecter au hub SignalR.');
    }

    if (
      this.connection?.state === HubConnectionState.Connected ||
      this.connection?.state === HubConnectionState.Connecting
    ) {
      return this.connection;
    }

    const url = `${API_ORIGIN}/hubs/monitoring`;

    this.connection = new HubConnectionBuilder()
      .withUrl(url, {
        // Équivalent explicite à : `${url}?access_token=${encodeURIComponent(jwt)}`
        accessTokenFactory: () => this.auth.getToken() ?? '',
        skipNegotiation: false,
        transport:
          HttpTransportType.WebSockets |
          HttpTransportType.ServerSentEvents |
          HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build();

    await this.connection.start();
    return this.connection;
  }

  async disconnectMonitoringHub(): Promise<void> {
    if (!this.connection) return;
    try {
      await this.connection.stop();
    } finally {
      this.connection = null;
    }
  }

  getConnection(): HubConnection | null {
    return this.connection;
  }
}
