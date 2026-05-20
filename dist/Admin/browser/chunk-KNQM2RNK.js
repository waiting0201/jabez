import {
  PAYMENT_INSTALLMENT_STATUS_CLASSES,
  PAYMENT_INSTALLMENT_STATUS_LABELS
} from "./chunk-YGQK3CZP.js";
import {
  Component,
  DatePipe,
  DecimalPipe,
  Input,
  NgClass,
  computed,
  setClassMetadata,
  signal,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵclassProp,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind2,
  ɵɵproperty,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1,
  ɵɵtextInterpolate2
} from "./chunk-IFQ7CN6S.js";

// src/app/shared/components/installments-table.ts
var _forTrack0 = ($index, $item) => $item.id;
function InstallmentsTable_Conditional_0_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 5);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext(2);
    \u0275\u0275property("ngClass", ctx_r0.statusClass);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r0.statusLabel);
  }
}
function InstallmentsTable_Conditional_0_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 6);
    \u0275\u0275text(1, " \u64A5\u6B3E\u7E3D\u984D\uFF1A");
    \u0275\u0275elementStart(2, "span", 16);
    \u0275\u0275text(3);
    \u0275\u0275pipe(4, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275text(5, " / ");
    \u0275\u0275elementStart(6, "span", 17);
    \u0275\u0275text(7);
    \u0275\u0275pipe(8, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275text(9, " \u5143 ");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(4, 2, ctx_r0.paidSum(), "1.0-2"));
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(8, 5, ctx_r0.totalAmount, "1.0-2"));
  }
}
function InstallmentsTable_Conditional_0_For_29_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0);
    \u0275\u0275pipe(1, "date");
  }
  if (rf & 2) {
    const ins_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind2(1, 1, ins_r2.paidAt, "yyyy-MM-dd"), " ");
  }
}
function InstallmentsTable_Conditional_0_For_29_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 20);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function InstallmentsTable_Conditional_0_For_29_Conditional_15_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 24);
    \u0275\u0275text(1, "\u5DF2\u64A5");
    \u0275\u0275elementEnd();
  }
}
function InstallmentsTable_Conditional_0_For_29_Conditional_16_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 25);
    \u0275\u0275text(1, "\u672A\u64A5");
    \u0275\u0275elementEnd();
  }
}
function InstallmentsTable_Conditional_0_For_29_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 18);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td", 19);
    \u0275\u0275text(4);
    \u0275\u0275pipe(5, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "td", 19);
    \u0275\u0275conditionalCreate(7, InstallmentsTable_Conditional_0_For_29_Conditional_7_Template, 2, 4)(8, InstallmentsTable_Conditional_0_For_29_Conditional_8_Template, 2, 0, "span", 20);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 21);
    \u0275\u0275text(10);
    \u0275\u0275pipe(11, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "td", 22);
    \u0275\u0275text(13);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(14, "td", 23);
    \u0275\u0275conditionalCreate(15, InstallmentsTable_Conditional_0_For_29_Conditional_15_Template, 2, 0, "span", 24)(16, InstallmentsTable_Conditional_0_For_29_Conditional_16_Template, 2, 0, "span", 25);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ins_r2 = ctx.$implicit;
    \u0275\u0275classProp("bg-[--bg-base]", ins_r2.paidAt);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(ins_r2.installmentNo);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(5, 8, ins_r2.expectedDate, "yyyy-MM-dd"));
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ins_r2.paidAt ? 7 : 8);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(11, 11, ins_r2.amount, "1.0-2"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(ins_r2.note || "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ins_r2.paidAt ? 15 : 16);
  }
}
function InstallmentsTable_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(3, "svg", 3);
    \u0275\u0275element(4, "use", 4);
    \u0275\u0275elementEnd();
    \u0275\u0275text(5, " \u64A5\u6B3E\u660E\u7D30 ");
    \u0275\u0275conditionalCreate(6, InstallmentsTable_Conditional_0_Conditional_6_Template, 2, 2, "span", 5);
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(7, "span", 6);
    \u0275\u0275text(8);
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(9, InstallmentsTable_Conditional_0_Conditional_9_Template, 10, 8, "div", 6);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "div", 7)(11, "div", 8)(12, "table", 9)(13, "thead", 10)(14, "tr")(15, "th", 11);
    \u0275\u0275text(16, "\u671F\u6578");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(17, "th", 12);
    \u0275\u0275text(18, "\u9810\u8A08\u64A5\u6B3E\u65E5");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "th", 12);
    \u0275\u0275text(20, "\u5BE6\u969B\u64A5\u6B3E\u65E5");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(21, "th", 13);
    \u0275\u0275text(22, "\u91D1\u984D");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(23, "th");
    \u0275\u0275text(24, "\u5099\u8A3B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(25, "th", 14);
    \u0275\u0275text(26, "\u72C0\u614B");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(27, "tbody");
    \u0275\u0275repeaterCreate(28, InstallmentsTable_Conditional_0_For_29_Template, 17, 14, "tr", 15, _forTrack0);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance(6);
    \u0275\u0275conditional(ctx_r0.paymentStatus ? 6 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate2("\u5DF2\u64A5 ", ctx_r0.paidCount(), " / ", ctx_r0.installments().length, " \u671F");
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r0.totalAmount != null ? 9 : -1);
    \u0275\u0275advance(19);
    \u0275\u0275repeater(ctx_r0.installments());
  }
}
var InstallmentsTable = class _InstallmentsTable {
  _installments = signal(void 0, ...ngDevMode ? [{ debugName: "_installments" }] : []);
  set installmentsInput(v) {
    this._installments.set(v);
  }
  installments = computed(() => this._installments(), ...ngDevMode ? [{ debugName: "installments" }] : []);
  paymentStatus;
  /** 申請總額（用於顯示已撥/總額對照）*/
  totalAmount;
  get statusLabel() {
    return this.paymentStatus ? PAYMENT_INSTALLMENT_STATUS_LABELS[this.paymentStatus] : "";
  }
  get statusClass() {
    return this.paymentStatus ? PAYMENT_INSTALLMENT_STATUS_CLASSES[this.paymentStatus] : "";
  }
  paidCount = computed(() => (this._installments() ?? []).filter((i) => !!i.paidAt).length, ...ngDevMode ? [{ debugName: "paidCount" }] : []);
  paidSum = computed(() => (this._installments() ?? []).filter((i) => !!i.paidAt).reduce((s, i) => s + (i.amount || 0), 0), ...ngDevMode ? [{ debugName: "paidSum" }] : []);
  static \u0275fac = function InstallmentsTable_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _InstallmentsTable)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _InstallmentsTable, selectors: [["app-installments-table"]], inputs: { installmentsInput: "installmentsInput", paymentStatus: "paymentStatus", totalAmount: "totalAmount" }, decls: 1, vars: 1, consts: [[1, "card", "border-0", "shadow-sm", "mb-6"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "justify-between", "gap-2", "fw-600", "flex-wrap"], [1, "flex", "items-center", "gap-2", "flex-wrap"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#credit-card"], [1, "badge", 3, "ngClass"], [1, "text-muted", "small", "fw-400"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-sm", "mb-0"], [1, "table-light"], [1, "text-center", 2, "width", "60px"], [2, "width", "140px"], [1, "text-right", 2, "width", "140px"], [1, "text-center", 2, "width", "80px"], [3, "bg-[--bg-base]"], [1, "fw-500", "text-success"], [1, "fw-500"], [1, "text-center", "align-middle", "fw-500", "small"], [1, "small"], [1, "text-muted"], [1, "text-right", "small", "fw-500"], [1, "small", "text-muted"], [1, "text-center"], [1, "badge", "bg-success"], [1, "badge", "bg-secondary"]], template: function InstallmentsTable_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275conditionalCreate(0, InstallmentsTable_Conditional_0_Template, 30, 4, "div", 0);
    }
    if (rf & 2) {
      \u0275\u0275conditional(ctx.installments() && ctx.installments().length > 0 ? 0 : -1);
    }
  }, dependencies: [NgClass, DatePipe, DecimalPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(InstallmentsTable, [{
    type: Component,
    args: [{
      selector: "app-installments-table",
      imports: [DatePipe, DecimalPipe, NgClass],
      template: `
    @if (installments() && installments()!.length > 0) {
      <div class="card border-0 shadow-sm mb-6">
        <div class="card-header bg-transparent border-bottom flex items-center justify-between gap-2 fw-600 flex-wrap">
          <div class="flex items-center gap-2 flex-wrap">
            <svg class="sa-icon text-primary" style="stroke: currentColor">
              <use href="/assets/icons/sprite.svg#credit-card"></use>
            </svg>
            \u64A5\u6B3E\u660E\u7D30
            @if (paymentStatus) {
              <span class="badge" [ngClass]="statusClass">{{ statusLabel }}</span>
            }
            <span class="text-muted small fw-400">\u5DF2\u64A5 {{ paidCount() }} / {{ installments()!.length }} \u671F</span>
          </div>
          @if (totalAmount != null) {
            <div class="text-muted small fw-400">
              \u64A5\u6B3E\u7E3D\u984D\uFF1A<span class="fw-500 text-success">{{ paidSum() | number:'1.0-2' }}</span> /
              <span class="fw-500">{{ totalAmount | number:'1.0-2' }}</span> \u5143
            </div>
          }
        </div>
        <div class="card-body p-0">
          <div class="table-responsive">
            <table class="table table-sm mb-0">
              <thead class="table-light">
                <tr>
                  <th class="text-center" style="width: 60px">\u671F\u6578</th>
                  <th style="width: 140px">\u9810\u8A08\u64A5\u6B3E\u65E5</th>
                  <th style="width: 140px">\u5BE6\u969B\u64A5\u6B3E\u65E5</th>
                  <th class="text-right" style="width: 140px">\u91D1\u984D</th>
                  <th>\u5099\u8A3B</th>
                  <th class="text-center" style="width: 80px">\u72C0\u614B</th>
                </tr>
              </thead>
              <tbody>
                @for (ins of installments(); track ins.id) {
                  <tr [class.bg-[--bg-base]]="ins.paidAt">
                    <td class="text-center align-middle fw-500 small">{{ ins.installmentNo }}</td>
                    <td class="small">{{ ins.expectedDate | date:'yyyy-MM-dd' }}</td>
                    <td class="small">
                      @if (ins.paidAt) {
                        {{ ins.paidAt | date:'yyyy-MM-dd' }}
                      } @else {
                        <span class="text-muted">\u2014</span>
                      }
                    </td>
                    <td class="text-right small fw-500">{{ ins.amount | number:'1.0-2' }}</td>
                    <td class="small text-muted">{{ ins.note || '\u2014' }}</td>
                    <td class="text-center">
                      @if (ins.paidAt) {
                        <span class="badge bg-success">\u5DF2\u64A5</span>
                      } @else {
                        <span class="badge bg-secondary">\u672A\u64A5</span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    }
  `
    }]
  }], null, { installmentsInput: [{
    type: Input,
    args: [{ required: true }]
  }], paymentStatus: [{
    type: Input
  }], totalAmount: [{
    type: Input
  }] });
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(InstallmentsTable, { className: "InstallmentsTable", filePath: "src/app/shared/components/installments-table.ts", lineNumber: 84 });
})();

export {
  InstallmentsTable
};
//# sourceMappingURL=chunk-KNQM2RNK.js.map
