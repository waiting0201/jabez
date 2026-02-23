import {Directive, inject, Input, TemplateRef, ViewContainerRef} from '@angular/core';
import {AuthService} from '@core/auth/services/auth.service';

@Directive({
  selector: '[appHasPermission]',
})
export class HasPermissionDirective {
  private templateRef = inject(TemplateRef<unknown>);
  private viewContainer = inject(ViewContainerRef);
  private authService = inject(AuthService);

  @Input() set appHasPermission(permission: string | undefined) {
    this.viewContainer.clear();
    if (!permission || this.authService.hasPermission(permission)) {
      this.viewContainer.createEmbeddedView(this.templateRef);
    }
  }
}
