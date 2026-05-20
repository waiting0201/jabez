import {
  PAYMENT_INSTALLMENT_STATUS_CLASSES,
  PAYMENT_INSTALLMENT_STATUS_LABELS
} from "./chunk-KP52QFLC.js";
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
  ɵɵelementEnd,
  ɵɵelementStart,
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
function InstallmentsTable_Conditional_0_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 3);
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
function InstallmentsTable_Conditional_0_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 5);
    \u0275\u0275text(1, " \u64A5\u6B3E\u7E3D\u984D\uFF1A");
    \u0275\u0275elementStart(2, "span", 13);
    \u0275\u0275text(3);
    \u0275\u0275pipe(4, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275text(5, " / ");
    \u0275\u0275elementStart(6, "span", 2);
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
function InstallmentsTable_Conditional_0_For_25_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0);
    \u0275\u0275pipe(1, "date");
  }
  if (rf & 2) {
    const ins_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind2(1, 1, ins_r2.paidAt, "yyyy-MM-dd"), " ");
  }
}
function InstallmentsTable_Conditional_0_For_25_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 15);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function InstallmentsTable_Conditional_0_For_25_Conditional_15_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 18);
    \u0275\u0275text(1, "\u5DF2\u64A5");
    \u0275\u0275elementEnd();
  }
}
function InstallmentsTable_Conditional_0_For_25_Conditional_16_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 19);
    \u0275\u0275text(1, "\u672A\u64A5");
    \u0275\u0275elementEnd();
  }
}
function InstallmentsTable_Conditional_0_For_25_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 14);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td");
    \u0275\u0275text(4);
    \u0275\u0275pipe(5, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "td");
    \u0275\u0275conditionalCreate(7, InstallmentsTable_Conditional_0_For_25_Conditional_7_Template, 2, 4)(8, InstallmentsTable_Conditional_0_For_25_Conditional_8_Template, 2, 0, "span", 15);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 16);
    \u0275\u0275text(10);
    \u0275\u0275pipe(11, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "td", 15);
    \u0275\u0275text(13);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(14, "td", 17);
    \u0275\u0275conditionalCreate(15, InstallmentsTable_Conditional_0_For_25_Conditional_15_Template, 2, 0, "span", 18)(16, InstallmentsTable_Conditional_0_For_25_Conditional_16_Template, 2, 0, "span", 19);
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
    \u0275\u0275text(3, " \u64A5\u6B3E\u660E\u7D30 ");
    \u0275\u0275conditionalCreate(4, InstallmentsTable_Conditional_0_Conditional_4_Template, 2, 2, "span", 3);
    \u0275\u0275elementStart(5, "span", 4);
    \u0275\u0275text(6);
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(7, InstallmentsTable_Conditional_0_Conditional_7_Template, 10, 8, "div", 5);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "table", 6)(9, "thead", 7)(10, "tr")(11, "th", 8);
    \u0275\u0275text(12, "\u671F\u6578");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "th", 9);
    \u0275\u0275text(14, "\u9810\u8A08\u64A5\u6B3E\u65E5");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "th", 9);
    \u0275\u0275text(16, "\u5BE6\u969B\u64A5\u6B3E\u65E5");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(17, "th", 10);
    \u0275\u0275text(18, "\u91D1\u984D");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "th");
    \u0275\u0275text(20, "\u5099\u8A3B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(21, "th", 11);
    \u0275\u0275text(22, "\u72C0\u614B");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(23, "tbody");
    \u0275\u0275repeaterCreate(24, InstallmentsTable_Conditional_0_For_25_Template, 17, 14, "tr", 12, _forTrack0);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275conditional(ctx_r0.paymentStatus ? 4 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate2("\u5DF2\u64A5 ", ctx_r0.paidCount(), "/", ctx_r0.installments().length, " \u671F");
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r0.totalAmount != null ? 7 : -1);
    \u0275\u0275advance(17);
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
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _InstallmentsTable, selectors: [["app-installments-table"]], inputs: { installmentsInput: "installmentsInput", paymentStatus: "paymentStatus", totalAmount: "totalAmount" }, decls: 1, vars: 1, consts: [[1, "border", "rounded", "overflow-hidden", "bg-white"], [1, "px-4", "py-3", "bg-[--bg-base]", "flex", "items-center", "justify-between", "flex-wrap", "gap-2", "border-bottom"], [1, "fw-500"], [1, "badge", "ml-2", 3, "ngClass"], [1, "text-muted", "small", "fw-400", "ml-2"], [1, "text-muted", "small"], [1, "table", "table-sm", "mb-0", "small"], [1, "bg-[--bg-base]"], [1, "text-center", 2, "width", "60px"], [2, "width", "140px"], [1, "text-right", 2, "width", "140px"], [1, "text-center", 2, "width", "80px"], [3, "bg-[--bg-base]"], [1, "fw-500", "text-success"], [1, "text-center", "align-middle", "fw-500"], [1, "text-muted"], [1, "text-right"], [1, "text-center"], [1, "badge", "bg-success"], [1, "badge", "bg-secondary"]], template: function InstallmentsTable_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275conditionalCreate(0, InstallmentsTable_Conditional_0_Template, 26, 4, "div", 0);
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
      <div class="border rounded overflow-hidden bg-white">
        <div class="px-4 py-3 bg-[--bg-base] flex items-center justify-between flex-wrap gap-2 border-bottom">
          <div class="fw-500">
            \u64A5\u6B3E\u660E\u7D30
            @if (paymentStatus) {
              <span class="badge ml-2" [ngClass]="statusClass">{{ statusLabel }}</span>
            }
            <span class="text-muted small fw-400 ml-2">\u5DF2\u64A5 {{ paidCount() }}/{{ installments()!.length }} \u671F</span>
          </div>
          @if (totalAmount != null) {
            <div class="text-muted small">
              \u64A5\u6B3E\u7E3D\u984D\uFF1A<span class="fw-500 text-success">{{ paidSum() | number:'1.0-2' }}</span> /
              <span class="fw-500">{{ totalAmount | number:'1.0-2' }}</span> \u5143
            </div>
          }
        </div>
        <table class="table table-sm mb-0 small">
          <thead class="bg-[--bg-base]">
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
                <td class="text-center align-middle fw-500">{{ ins.installmentNo }}</td>
                <td>{{ ins.expectedDate | date:'yyyy-MM-dd' }}</td>
                <td>
                  @if (ins.paidAt) {
                    {{ ins.paidAt | date:'yyyy-MM-dd' }}
                  } @else {
                    <span class="text-muted">\u2014</span>
                  }
                </td>
                <td class="text-right">{{ ins.amount | number:'1.0-2' }}</td>
                <td class="text-muted">{{ ins.note || '\u2014' }}</td>
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
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(InstallmentsTable, { className: "InstallmentsTable", filePath: "src/app/shared/components/installments-table.ts", lineNumber: 76 });
})();

export {
  InstallmentsTable
};
//# sourceMappingURL=chunk-QRTEGJBM.js.map
