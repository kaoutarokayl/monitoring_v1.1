import { Directive, Input, TemplateRef, ViewContainerRef, effect, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

/**
 * Directive structurelle RBAC — usage :
 *
 *   <button *appHasRole="['Support']">Supprimer</button>
 *   <div *appHasRole="['Superviseur', 'Support']">Lecture</div>
 *
 * Masque l'élément si l'utilisateur n'appartient à AUCUN des rôles listés.
 */
@Directive({
  selector: '[appHasRole]',
  standalone: true
})
export class HasRoleDirective {
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private readonly auth = inject(AuthService);

  private requiredRoles: string[] = [];
  private hasView = false;

  @Input() set appHasRole(roles: string[]) {
    this.requiredRoles = roles ?? [];
    this.updateView();
  }

  constructor() {
    // Re-render quand les rôles changent (Signal reactivity)
    effect(() => {
      // Accès au signal pour enregistrer la dépendance
      this.auth.currentUserRoles();
      this.updateView();
    });
  }

  private updateView(): void {
    const hasAccess = this.auth.hasRole(this.requiredRoles);
    if (hasAccess && !this.hasView) {
      this.vcr.createEmbeddedView(this.tpl);
      this.hasView = true;
    } else if (!hasAccess && this.hasView) {
      this.vcr.clear();
      this.hasView = false;
    }
  }
}

