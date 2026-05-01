import {
  AuthService
} from "./chunk-ZSGTQ3YJ.js";
import {
  Directive,
  Input,
  TemplateRef,
  ViewContainerRef,
  inject,
  setClassMetadata,
  ɵɵdefineDirective
} from "./chunk-IFQ7CN6S.js";

// src/app/shared/directives/has-permission.directive.ts
var HasPermissionDirective = class _HasPermissionDirective {
  templateRef = inject(TemplateRef);
  viewContainer = inject(ViewContainerRef);
  authService = inject(AuthService);
  set appHasPermission(permission) {
    this.viewContainer.clear();
    if (!permission || this.authService.hasPermission(permission)) {
      this.viewContainer.createEmbeddedView(this.templateRef);
    }
  }
  static \u0275fac = function HasPermissionDirective_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _HasPermissionDirective)();
  };
  static \u0275dir = /* @__PURE__ */ \u0275\u0275defineDirective({ type: _HasPermissionDirective, selectors: [["", "appHasPermission", ""]], inputs: { appHasPermission: "appHasPermission" } });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(HasPermissionDirective, [{
    type: Directive,
    args: [{
      selector: "[appHasPermission]"
    }]
  }], null, { appHasPermission: [{
    type: Input
  }] });
})();

export {
  HasPermissionDirective
};
//# sourceMappingURL=chunk-M7DGJIC4.js.map
