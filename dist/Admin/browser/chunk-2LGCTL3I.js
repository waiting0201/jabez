import {
  LineService
} from "./chunk-3W3BTVDJ.js";
import {
  ToastrService
} from "./chunk-ZQ3QUCLL.js";
import {
  ActivatedRoute,
  Router
} from "./chunk-UAVMLPEF.js";
import "./chunk-K2EJQVOR.js";
import {
  Component,
  environment,
  inject,
  setClassMetadata,
  signal,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdomElement,
  ɵɵdomElementEnd,
  ɵɵdomElementStart,
  ɵɵdomListener,
  ɵɵgetCurrentView,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵnextContext,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵtext,
  ɵɵtextInterpolate
} from "./chunk-FX7BMVKQ.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/account/pages/line-bind-callback/line-bind-callback.ts
function LineBindCallback_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElement(0, "div", 2);
    \u0275\u0275domElementStart(1, "p", 3);
    \u0275\u0275text(2, "\u6B63\u5728\u7D81\u5B9A LINE \u5E33\u865F...");
    \u0275\u0275domElementEnd();
  }
}
function LineBindCallback_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275domElementStart(0, "div", 4);
    \u0275\u0275namespaceSVG();
    \u0275\u0275domElementStart(1, "svg", 5);
    \u0275\u0275domElement(2, "circle", 6)(3, "path", 7);
    \u0275\u0275domElementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275domElementStart(4, "p", 8);
    \u0275\u0275text(5, "\u7D81\u5B9A\u5931\u6557");
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(6, "p", 9);
    \u0275\u0275text(7);
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(8, "button", 10);
    \u0275\u0275domListener("click", function LineBindCallback_Conditional_3_Template_button_click_8_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.goBack());
    });
    \u0275\u0275text(9, "\u8FD4\u56DE");
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(ctx_r1.errorMsg());
  }
}
var LineBindCallback = class _LineBindCallback {
  route = inject(ActivatedRoute);
  router = inject(Router);
  lineService = inject(LineService);
  toastr = inject(ToastrService);
  isLoading = signal(true, ...ngDevMode ? [{ debugName: "isLoading" }] : []);
  errorMsg = signal("", ...ngDevMode ? [{ debugName: "errorMsg" }] : []);
  ngOnInit() {
    const code = this.route.snapshot.queryParamMap.get("code");
    const state = this.route.snapshot.queryParamMap.get("state");
    const savedState = sessionStorage.getItem("line_bind_state");
    if (!code || !state || state !== savedState) {
      this.isLoading.set(false);
      this.errorMsg.set("\u9A57\u8B49\u5931\u6557\uFF0C\u8ACB\u91CD\u65B0\u64CD\u4F5C\u3002");
      sessionStorage.removeItem("line_bind_state");
      return;
    }
    sessionStorage.removeItem("line_bind_state");
    this.lineService.bind(code, environment.lineCallbackUrl).subscribe({
      next: () => {
        this.toastr.success("LINE \u5E33\u865F\u7D81\u5B9A\u6210\u529F\uFF01");
        this.router.navigate(["/dashboard"]);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMsg.set(err?.error?.message || "\u7D81\u5B9A\u5931\u6557\uFF0C\u8ACB\u91CD\u8A66\u3002");
      }
    });
  }
  goBack() {
    this.router.navigate(["/dashboard"]);
  }
  static \u0275fac = function LineBindCallback_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _LineBindCallback)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _LineBindCallback, selectors: [["app-line-bind-callback"]], decls: 4, vars: 1, consts: [[1, "flex", "items-center", "justify-center", 2, "min-height", "60vh"], [1, "text-center"], ["role", "status", 1, "spinner-border", "text-primary", "mb-3"], [1, "text-[--text-secondary]"], [1, "text-[--red]", "mb-3"], ["width", "48", "height", "48", "viewBox", "0 0 24 24", "fill", "none", "stroke", "currentColor", "stroke-width", "2"], ["cx", "12", "cy", "12", "r", "10"], ["d", "M15 9l-6 6M9 9l6 6"], [1, "text-[--red]", "fw-600", "mb-2"], [1, "text-[--text-secondary]", "text-sm", "mb-4"], [1, "btn", "btn-outline-secondary", "btn-sm", 3, "click"]], template: function LineBindCallback_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275domElementStart(0, "div", 0)(1, "div", 1);
      \u0275\u0275conditionalCreate(2, LineBindCallback_Conditional_2_Template, 3, 0)(3, LineBindCallback_Conditional_3_Template, 10, 1);
      \u0275\u0275domElementEnd()();
    }
    if (rf & 2) {
      \u0275\u0275advance(2);
      \u0275\u0275conditional(ctx.isLoading() ? 2 : ctx.errorMsg() ? 3 : -1);
    }
  }, encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(LineBindCallback, [{
    type: Component,
    args: [{
      selector: "app-line-bind-callback",
      imports: [],
      template: `
    <div class="flex items-center justify-center" style="min-height: 60vh;">
      <div class="text-center">
        @if (isLoading()) {
          <div class="spinner-border text-primary mb-3" role="status"></div>
          <p class="text-[--text-secondary]">\u6B63\u5728\u7D81\u5B9A LINE \u5E33\u865F...</p>
        } @else if (errorMsg()) {
          <div class="text-[--red] mb-3">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/><path d="M15 9l-6 6M9 9l6 6"/>
            </svg>
          </div>
          <p class="text-[--red] fw-600 mb-2">\u7D81\u5B9A\u5931\u6557</p>
          <p class="text-[--text-secondary] text-sm mb-4">{{ errorMsg() }}</p>
          <button class="btn btn-outline-secondary btn-sm" (click)="goBack()">\u8FD4\u56DE</button>
        }
      </div>
    </div>
  `
    }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(LineBindCallback, { className: "LineBindCallback", filePath: "src/app/features/account/pages/line-bind-callback/line-bind-callback.ts", lineNumber: 30 });
})();
export {
  LineBindCallback
};
//# sourceMappingURL=chunk-2LGCTL3I.js.map
