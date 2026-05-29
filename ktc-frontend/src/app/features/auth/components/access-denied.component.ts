import { Component, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

/**
 * Page 403 – Accès Refusé
 * Affichée quand le RoleGuard bloque l'accès à une route protégée.
 */
@Component({
  selector: 'app-access-denied',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="denied-wrapper">
      <div class="denied-card">
        <!-- Icône -->
        <div class="denied-icon">
          <svg viewBox="0 0 64 64" fill="none" xmlns="http://www.w3.org/2000/svg">
            <circle cx="32" cy="32" r="30" stroke="currentColor" stroke-width="3"/>
            <path d="M20 20 L44 44 M44 20 L20 44" stroke="currentColor" stroke-width="3" stroke-linecap="round"/>
          </svg>
        </div>

        <!-- Code HTTP -->
        <div class="denied-code">403</div>

        <!-- Titre -->
        <h1 class="denied-title">Accès Refusé</h1>

        <!-- Message -->
        <p class="denied-message">
          Vous n'avez pas les droits nécessaires pour accéder à cette page.<br/>
          Votre rôle actuel est : <strong>{{ roleLabel() }}</strong>
        </p>

        <!-- Rôles manquants -->
        <div class="denied-hint">
          <span class="hint-icon">🔐</span>
          Cette action requiert le rôle <code>Support</code>
        </div>

        <!-- Actions -->
        <div class="denied-actions">
          <button class="btn-back" (click)="goBack()">
            ← Retour
          </button>
          <a class="btn-home" routerLink="/dashboard">
            Tableau de bord
          </a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: var(--bg-primary, #0f172a);
      font-family: 'Inter', sans-serif;
    }

    .denied-wrapper {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 100%;
      padding: 2rem;
    }

    .denied-card {
      background: var(--bg-secondary, #1e293b);
      border: 1px solid rgba(239, 68, 68, 0.3);
      border-radius: 1.5rem;
      padding: 3rem 2.5rem;
      max-width: 480px;
      width: 100%;
      text-align: center;
      box-shadow:
        0 0 0 1px rgba(239, 68, 68, 0.1),
        0 25px 50px rgba(0, 0, 0, 0.5);
      animation: fadeSlideIn 0.4s ease-out;
    }

    @keyframes fadeSlideIn {
      from { opacity: 0; transform: translateY(-20px); }
      to   { opacity: 1; transform: translateY(0);     }
    }

    .denied-icon {
      width: 72px;
      height: 72px;
      margin: 0 auto 1.5rem;
      color: #ef4444;
      background: rgba(239, 68, 68, 0.1);
      border-radius: 50%;
      padding: 16px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .denied-icon svg {
      width: 100%;
      height: 100%;
    }

    .denied-code {
      font-size: 5rem;
      font-weight: 800;
      line-height: 1;
      background: linear-gradient(135deg, #ef4444, #f97316);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
      margin-bottom: 0.5rem;
      letter-spacing: -4px;
    }

    .denied-title {
      font-size: 1.75rem;
      font-weight: 700;
      color: var(--text-primary, #f8fafc);
      margin: 0 0 1rem;
    }

    .denied-message {
      color: var(--text-muted, #94a3b8);
      font-size: 0.95rem;
      line-height: 1.6;
      margin-bottom: 1.5rem;
    }

    .denied-message strong {
      color: #f97316;
      font-weight: 600;
    }

    .denied-hint {
      background: rgba(239, 68, 68, 0.08);
      border: 1px solid rgba(239, 68, 68, 0.2);
      border-radius: 0.75rem;
      padding: 0.875rem 1rem;
      font-size: 0.875rem;
      color: var(--text-muted, #94a3b8);
      margin-bottom: 2rem;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      justify-content: center;
    }

    .denied-hint code {
      background: rgba(239, 68, 68, 0.15);
      color: #ef4444;
      padding: 0.1rem 0.4rem;
      border-radius: 0.25rem;
      font-size: 0.8rem;
      font-weight: 600;
    }

    .denied-actions {
      display: flex;
      gap: 1rem;
      justify-content: center;
    }

    .btn-back {
      padding: 0.65rem 1.5rem;
      border-radius: 0.75rem;
      border: 1px solid rgba(148, 163, 184, 0.3);
      background: transparent;
      color: var(--text-secondary, #cbd5e1);
      font-size: 0.9rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;
    }

    .btn-back:hover {
      background: rgba(148, 163, 184, 0.1);
      border-color: rgba(148, 163, 184, 0.5);
    }

    .btn-home {
      padding: 0.65rem 1.5rem;
      border-radius: 0.75rem;
      background: linear-gradient(135deg, #3b82f6, #6366f1);
      color: #fff;
      font-size: 0.9rem;
      font-weight: 500;
      text-decoration: none;
      transition: all 0.2s;
      box-shadow: 0 4px 12px rgba(99, 102, 241, 0.3);
    }

    .btn-home:hover {
      transform: translateY(-1px);
      box-shadow: 0 6px 16px rgba(99, 102, 241, 0.4);
    }
  `]
})
export class AccessDeniedComponent {
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);

  roleLabel = () => {
    if (this.auth.isSupport()) return 'Support';
    if (this.auth.isReadOnly()) return 'Superviseur (Lecture seule)';
    return 'Aucun rôle assigné';
  };

  goBack(): void {
    window.history.back();
  }
}

