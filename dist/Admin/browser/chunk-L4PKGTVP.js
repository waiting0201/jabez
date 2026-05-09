import {
  ToastrService
} from "./chunk-X3EGCDLG.js";
import {
  DefaultValueAccessor,
  FormBuilder,
  FormControlName,
  FormGroupDirective,
  NgControlStatus,
  NgControlStatusGroup,
  ReactiveFormsModule,
  Validators,
  ɵNgNoValidate
} from "./chunk-4LFECYTV.js";
import {
  ActivatedRoute,
  Router
} from "./chunk-DUW2WF5C.js";
import "./chunk-JDEYLUO2.js";
import {
  AuthService
} from "./chunk-ZSGTQ3YJ.js";
import {
  Component,
  inject,
  setClassMetadata,
  signal,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵattribute,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵlistener,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵproperty,
  ɵɵtext,
  ɵɵtextInterpolate1
} from "./chunk-IFQ7CN6S.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/account/pages/change-password/change-password.ts
function ChangePassword_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 3);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 21);
    \u0275\u0275element(2, "use", 22);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u57FA\u65BC\u5B89\u5168\u6027\u8003\u91CF\uFF0C\u8ACB\u7ACB\u5373\u4FEE\u6539\u60A8\u7684\u5BC6\u78BC\u5F8C\u624D\u80FD\u7E7C\u7E8C\u4F7F\u7528\u7CFB\u7D71\u3002 ");
    \u0275\u0275elementEnd();
  }
}
function ChangePassword_Conditional_30_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 17);
    \u0275\u0275text(1, "\u5BC6\u78BC\u9577\u5EA6\u81F3\u5C11 6 \u78BC\u3002");
    \u0275\u0275elementEnd();
  }
}
var ChangePassword = class _ChangePassword {
  fb = inject(FormBuilder);
  auth = inject(AuthService);
  router = inject(Router);
  route = inject(ActivatedRoute);
  toastr = inject(ToastrService);
  isForced = this.route.snapshot.queryParamMap.get("forced") === "1";
  submitting = signal(false, ...ngDevMode ? [{ debugName: "submitting" }] : []);
  showCurrentPassword = signal(false, ...ngDevMode ? [{ debugName: "showCurrentPassword" }] : []);
  showNewPassword = signal(false, ...ngDevMode ? [{ debugName: "showNewPassword" }] : []);
  showConfirmPassword = signal(false, ...ngDevMode ? [{ debugName: "showConfirmPassword" }] : []);
  form = this.fb.nonNullable.group({
    currentPassword: ["", [Validators.required]],
    newPassword: ["", [Validators.required, Validators.minLength(6)]],
    confirmPassword: ["", [Validators.required]]
  });
  submit() {
    if (this.form.invalid)
      return;
    const { currentPassword, newPassword, confirmPassword } = this.form.getRawValue();
    if (newPassword !== confirmPassword) {
      this.toastr.error("\u65B0\u5BC6\u78BC\u8207\u78BA\u8A8D\u5BC6\u78BC\u4E0D\u4E00\u81F4\u3002");
      return;
    }
    if (currentPassword === newPassword) {
      this.toastr.error("\u65B0\u5BC6\u78BC\u4E0D\u53EF\u8207\u820A\u5BC6\u78BC\u76F8\u540C\u3002");
      return;
    }
    this.submitting.set(true);
    this.auth.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.toastr.success("\u5BC6\u78BC\u4FEE\u6539\u6210\u529F\uFF0C\u8ACB\u91CD\u65B0\u767B\u5165\u3002");
        this.auth.logout();
        this.router.navigate(["/auth/login"]);
      },
      error: (err) => {
        this.submitting.set(false);
        const msg = err?.error?.message || err?.message || "\u5BC6\u78BC\u4FEE\u6539\u5931\u6557\u3002";
        this.toastr.error(msg);
      }
    });
  }
  static \u0275fac = function ChangePassword_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ChangePassword)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ChangePassword, selectors: [["app-change-password"]], decls: 44, vars: 11, consts: [[1, "container-fluid", "py-3"], [1, "flex", "items-center", "gap-2", "mb-6"], [1, "mb-0"], ["role", "alert", 1, "alert", "alert-warning", "flex", "items-center", "gap-2", "mb-6", "py-2"], [3, "ngSubmit", "formGroup"], [1, "row", "g-4"], [1, "col-12", "col-lg-8", "col-xl-6"], [1, "card", "border-0", "shadow-sm"], [1, "card-body"], [1, "mb-4"], [1, "form-label", "fw-500"], [1, "text-danger"], [1, "relative"], ["formControlName", "currentPassword", "placeholder", "\u8ACB\u8F38\u5165\u76EE\u524D\u7684\u5BC6\u78BC", 1, "form-control", "pr-10", 3, "type"], ["type", "button", 1, "absolute", "right-2", "top-1/2", "-translate-y-1/2", "bg-transparent", "border-0", "cursor-pointer", "p-1", "text-[--text-muted]", "hover:text-[--text-primary]", 3, "click"], [1, "sa-icon", 2, "width", "1.1rem", "height", "1.1rem", "stroke", "currentColor"], ["formControlName", "newPassword", "placeholder", "\u81F3\u5C11 6 \u78BC", 1, "form-control", "pr-10", 3, "type"], [1, "text-danger", "small", "mt-1"], ["formControlName", "confirmPassword", "placeholder", "\u518D\u6B21\u8F38\u5165\u65B0\u5BC6\u78BC", 1, "form-control", "pr-10", 3, "type"], [1, "mt-6", "flex", "gap-2"], ["type", "submit", 1, "btn", "btn-primary", 3, "disabled"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#alert-triangle"]], template: function ChangePassword_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "h4", 2);
      \u0275\u0275text(3, "\u4FEE\u6539\u5BC6\u78BC");
      \u0275\u0275elementEnd()();
      \u0275\u0275conditionalCreate(4, ChangePassword_Conditional_4_Template, 4, 0, "div", 3);
      \u0275\u0275elementStart(5, "form", 4);
      \u0275\u0275listener("ngSubmit", function ChangePassword_Template_form_ngSubmit_5_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(6, "div", 5)(7, "div", 6)(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "label", 10);
      \u0275\u0275text(12, "\u820A\u5BC6\u78BC ");
      \u0275\u0275elementStart(13, "span", 11);
      \u0275\u0275text(14, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(15, "div", 12);
      \u0275\u0275element(16, "input", 13);
      \u0275\u0275elementStart(17, "button", 14);
      \u0275\u0275listener("click", function ChangePassword_Template_button_click_17_listener() {
        return ctx.showCurrentPassword.set(!ctx.showCurrentPassword());
      });
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(18, "svg", 15);
      \u0275\u0275element(19, "use");
      \u0275\u0275elementEnd()()()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(20, "div", 9)(21, "label", 10);
      \u0275\u0275text(22, "\u65B0\u5BC6\u78BC ");
      \u0275\u0275elementStart(23, "span", 11);
      \u0275\u0275text(24, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(25, "div", 12);
      \u0275\u0275element(26, "input", 16);
      \u0275\u0275elementStart(27, "button", 14);
      \u0275\u0275listener("click", function ChangePassword_Template_button_click_27_listener() {
        return ctx.showNewPassword.set(!ctx.showNewPassword());
      });
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(28, "svg", 15);
      \u0275\u0275element(29, "use");
      \u0275\u0275elementEnd()()();
      \u0275\u0275conditionalCreate(30, ChangePassword_Conditional_30_Template, 2, 0, "div", 17);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(31, "div", 2)(32, "label", 10);
      \u0275\u0275text(33, "\u78BA\u8A8D\u65B0\u5BC6\u78BC ");
      \u0275\u0275elementStart(34, "span", 11);
      \u0275\u0275text(35, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(36, "div", 12);
      \u0275\u0275element(37, "input", 18);
      \u0275\u0275elementStart(38, "button", 14);
      \u0275\u0275listener("click", function ChangePassword_Template_button_click_38_listener() {
        return ctx.showConfirmPassword.set(!ctx.showConfirmPassword());
      });
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(39, "svg", 15);
      \u0275\u0275element(40, "use");
      \u0275\u0275elementEnd()()()()()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(41, "div", 19)(42, "button", 20);
      \u0275\u0275text(43);
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      let tmp_6_0;
      \u0275\u0275advance(4);
      \u0275\u0275conditional(ctx.isForced ? 4 : -1);
      \u0275\u0275advance();
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(11);
      \u0275\u0275property("type", ctx.showCurrentPassword() ? "text" : "password");
      \u0275\u0275advance(3);
      \u0275\u0275attribute("href", "/assets/icons/sprite.svg#" + (ctx.showCurrentPassword() ? "eye-off" : "eye"));
      \u0275\u0275advance(7);
      \u0275\u0275property("type", ctx.showNewPassword() ? "text" : "password");
      \u0275\u0275advance(3);
      \u0275\u0275attribute("href", "/assets/icons/sprite.svg#" + (ctx.showNewPassword() ? "eye-off" : "eye"));
      \u0275\u0275advance();
      \u0275\u0275conditional(((tmp_6_0 = ctx.form.get("newPassword")) == null ? null : tmp_6_0.hasError("minlength")) && ((tmp_6_0 = ctx.form.get("newPassword")) == null ? null : tmp_6_0.touched) ? 30 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275property("type", ctx.showConfirmPassword() ? "text" : "password");
      \u0275\u0275advance(3);
      \u0275\u0275attribute("href", "/assets/icons/sprite.svg#" + (ctx.showConfirmPassword() ? "eye-off" : "eye"));
      \u0275\u0275advance(2);
      \u0275\u0275property("disabled", ctx.form.invalid || ctx.submitting());
      \u0275\u0275advance();
      \u0275\u0275textInterpolate1(" ", ctx.submitting() ? "\u8655\u7406\u4E2D..." : "\u78BA\u8A8D\u4FEE\u6539", " ");
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, DefaultValueAccessor, NgControlStatus, NgControlStatusGroup, FormGroupDirective, FormControlName], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ChangePassword, [{
    type: Component,
    args: [{ selector: "app-change-password", imports: [ReactiveFormsModule], template: `<div class="container-fluid py-3">\r
  <div class="flex items-center gap-2 mb-6">\r
    <h4 class="mb-0">\u4FEE\u6539\u5BC6\u78BC</h4>\r
  </div>\r
\r
  @if (isForced) {\r
    <div class="alert alert-warning flex items-center gap-2 mb-6 py-2" role="alert">\r
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>\r
      \u57FA\u65BC\u5B89\u5168\u6027\u8003\u91CF\uFF0C\u8ACB\u7ACB\u5373\u4FEE\u6539\u60A8\u7684\u5BC6\u78BC\u5F8C\u624D\u80FD\u7E7C\u7E8C\u4F7F\u7528\u7CFB\u7D71\u3002\r
    </div>\r
  }\r
\r
  <form [formGroup]="form" (ngSubmit)="submit()">\r
    <div class="row g-4">\r
      <div class="col-12 col-lg-8 col-xl-6">\r
        <div class="card border-0 shadow-sm">\r
          <div class="card-body">\r
\r
            <!-- \u820A\u5BC6\u78BC -->\r
            <div class="mb-4">\r
              <label class="form-label fw-500">\u820A\u5BC6\u78BC <span class="text-danger">*</span></label>\r
              <div class="relative">\r
                <input [type]="showCurrentPassword() ? 'text' : 'password'"\r
                       class="form-control pr-10" formControlName="currentPassword"\r
                       placeholder="\u8ACB\u8F38\u5165\u76EE\u524D\u7684\u5BC6\u78BC">\r
                <button type="button"\r
                        class="absolute right-2 top-1/2 -translate-y-1/2 bg-transparent border-0 cursor-pointer p-1 text-[--text-muted] hover:text-[--text-primary]"\r
                        (click)="showCurrentPassword.set(!showCurrentPassword())">\r
                  <svg class="sa-icon" style="width:1.1rem;height:1.1rem;stroke:currentColor">\r
                    <use [attr.href]="'/assets/icons/sprite.svg#' + (showCurrentPassword() ? 'eye-off' : 'eye')"></use>\r
                  </svg>\r
                </button>\r
              </div>\r
            </div>\r
\r
            <!-- \u65B0\u5BC6\u78BC -->\r
            <div class="mb-4">\r
              <label class="form-label fw-500">\u65B0\u5BC6\u78BC <span class="text-danger">*</span></label>\r
              <div class="relative">\r
                <input [type]="showNewPassword() ? 'text' : 'password'"\r
                       class="form-control pr-10" formControlName="newPassword"\r
                       placeholder="\u81F3\u5C11 6 \u78BC">\r
                <button type="button"\r
                        class="absolute right-2 top-1/2 -translate-y-1/2 bg-transparent border-0 cursor-pointer p-1 text-[--text-muted] hover:text-[--text-primary]"\r
                        (click)="showNewPassword.set(!showNewPassword())">\r
                  <svg class="sa-icon" style="width:1.1rem;height:1.1rem;stroke:currentColor">\r
                    <use [attr.href]="'/assets/icons/sprite.svg#' + (showNewPassword() ? 'eye-off' : 'eye')"></use>\r
                  </svg>\r
                </button>\r
              </div>\r
              @if (form.get('newPassword')?.hasError('minlength') && form.get('newPassword')?.touched) {\r
                <div class="text-danger small mt-1">\u5BC6\u78BC\u9577\u5EA6\u81F3\u5C11 6 \u78BC\u3002</div>\r
              }\r
            </div>\r
\r
            <!-- \u78BA\u8A8D\u5BC6\u78BC -->\r
            <div class="mb-0">\r
              <label class="form-label fw-500">\u78BA\u8A8D\u65B0\u5BC6\u78BC <span class="text-danger">*</span></label>\r
              <div class="relative">\r
                <input [type]="showConfirmPassword() ? 'text' : 'password'"\r
                       class="form-control pr-10" formControlName="confirmPassword"\r
                       placeholder="\u518D\u6B21\u8F38\u5165\u65B0\u5BC6\u78BC">\r
                <button type="button"\r
                        class="absolute right-2 top-1/2 -translate-y-1/2 bg-transparent border-0 cursor-pointer p-1 text-[--text-muted] hover:text-[--text-primary]"\r
                        (click)="showConfirmPassword.set(!showConfirmPassword())">\r
                  <svg class="sa-icon" style="width:1.1rem;height:1.1rem;stroke:currentColor">\r
                    <use [attr.href]="'/assets/icons/sprite.svg#' + (showConfirmPassword() ? 'eye-off' : 'eye')"></use>\r
                  </svg>\r
                </button>\r
              </div>\r
            </div>\r
\r
          </div>\r
        </div>\r
\r
        <div class="mt-6 flex gap-2">\r
          <button type="submit" class="btn btn-primary" [disabled]="form.invalid || submitting()">\r
            {{ submitting() ? '\u8655\u7406\u4E2D...' : '\u78BA\u8A8D\u4FEE\u6539' }}\r
          </button>\r
        </div>\r
\r
      </div>\r
    </div>\r
  </form>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ChangePassword, { className: "ChangePassword", filePath: "src/app/features/account/pages/change-password/change-password.ts", lineNumber: 12 });
})();
export {
  ChangePassword
};
//# sourceMappingURL=chunk-L4PKGTVP.js.map
