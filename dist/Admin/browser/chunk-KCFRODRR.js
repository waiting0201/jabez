import {
  Component,
  setClassMetadata,
  ɵsetClassDebugInfo,
  ɵɵdefineComponent,
  ɵɵdomElement,
  ɵɵdomElementEnd,
  ɵɵdomElementStart,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵtext
} from "./chunk-PZSIC2U3.js";

// src/app/features/dashboard/pages/dashboard/dashboard.ts
var Dashboard = class _Dashboard {
  static \u0275fac = function Dashboard_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _Dashboard)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _Dashboard, selectors: [["app-dashboard"]], decls: 24, vars: 0, consts: [[1, "container-fluid", "py-3"], [1, "row"], [1, "col-12"], [1, "flex", "items-center", "gap-4", "mb-6"], [1, "sa-icon", "sa-icon-3x", "text-primary"], ["href", "/assets/icons/sprite.svg#home"], [1, "mb-0"], [1, "text-muted", "mb-0"], [1, "row", "g-3"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "py-5", "text-center"], [1, "sa-icon", "sa-icon-3x", "text-secondary", "mb-4"], ["href", "/assets/icons/sprite.svg#trello"], [1, "text-muted"], [1, "text-muted", "small"]], template: function Dashboard_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275domElementStart(0, "div", 0)(1, "div", 1)(2, "div", 2)(3, "div", 3);
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(4, "svg", 4);
      \u0275\u0275domElement(5, "use", 5);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(6, "div")(7, "h4", 6);
      \u0275\u0275text(8, "\u5100\u8868\u677F");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(9, "p", 7);
      \u0275\u0275text(10, "\u6B61\u8FCE\u56DE\u4F86\uFF01\u60A8\u7684\u5DE5\u4F5C\u5340\u5DF2\u6E96\u5099\u5C31\u7DD2\u3002");
      \u0275\u0275domElementEnd()()()()();
      \u0275\u0275domElementStart(11, "div", 8)(12, "div", 2)(13, "div", 9)(14, "div", 10);
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(15, "svg", 11);
      \u0275\u0275domElement(16, "use", 12);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(17, "h5", 13);
      \u0275\u0275text(18, "\u958B\u59CB\u5EFA\u7ACB\u60A8\u7684\u529F\u80FD");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(19, "p", 14);
      \u0275\u0275text(20, "\u5728 ");
      \u0275\u0275domElementStart(21, "code");
      \u0275\u0275text(22, "src/app/features/");
      \u0275\u0275domElementEnd();
      \u0275\u0275text(23, " \u4E2D\u52A0\u5165\u696D\u52D9\u908F\u8F2F");
      \u0275\u0275domElementEnd()()()()()();
    }
  }, encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(Dashboard, [{
    type: Component,
    args: [{ selector: "app-dashboard", template: '<div class="container-fluid py-3">\r\n  <div class="row">\r\n    <div class="col-12">\r\n      <div class="flex items-center gap-4 mb-6">\r\n        <svg class="sa-icon sa-icon-3x text-primary">\r\n          <use href="/assets/icons/sprite.svg#home"></use>\r\n        </svg>\r\n        <div>\r\n          <h4 class="mb-0">\u5100\u8868\u677F</h4>\r\n          <p class="text-muted mb-0">\u6B61\u8FCE\u56DE\u4F86\uFF01\u60A8\u7684\u5DE5\u4F5C\u5340\u5DF2\u6E96\u5099\u5C31\u7DD2\u3002</p>\r\n        </div>\r\n      </div>\r\n    </div>\r\n  </div>\r\n  <div class="row g-3">\r\n    <div class="col-12">\r\n      <div class="card border-0 shadow-sm">\r\n        <div class="card-body py-5 text-center">\r\n          <svg class="sa-icon sa-icon-3x text-secondary mb-4">\r\n            <use href="/assets/icons/sprite.svg#trello"></use>\r\n          </svg>\r\n          <h5 class="text-muted">\u958B\u59CB\u5EFA\u7ACB\u60A8\u7684\u529F\u80FD</h5>\r\n          <p class="text-muted small">\u5728 <code>src/app/features/</code> \u4E2D\u52A0\u5165\u696D\u52D9\u908F\u8F2F</p>\r\n        </div>\r\n      </div>\r\n    </div>\r\n  </div>\r\n</div>\r\n' }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(Dashboard, { className: "Dashboard", filePath: "src/app/features/dashboard/pages/dashboard/dashboard.ts", lineNumber: 7 });
})();

// src/app/features/dashboard/dashboard.routes.ts
var DASHBOARD_ROUTES = [
  {
    path: "",
    component: Dashboard,
    data: { title: "Dashboard" }
  }
];
export {
  DASHBOARD_ROUTES
};
//# sourceMappingURL=chunk-KCFRODRR.js.map
