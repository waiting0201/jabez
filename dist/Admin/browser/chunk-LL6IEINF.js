import {
  Component,
  DatePipe,
  Input,
  input,
  setClassMetadata,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵclassProp,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdomElement,
  ɵɵdomElementEnd,
  ɵɵdomElementStart,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind2,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1
} from "./chunk-GQPYF5UN.js";

// src/app/shared/components/approval-timeline.ts
var _forTrack0 = ($index, $item) => $item.stepOrder;
function ApprovalTimeline_Conditional_0_For_8_Conditional_1_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 14);
    \u0275\u0275text(1, "\u2713");
    \u0275\u0275domElementEnd();
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_1_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 15);
    \u0275\u0275text(1, "\u2717");
    \u0275\u0275domElementEnd();
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275conditionalCreate(0, ApprovalTimeline_Conditional_0_For_8_Conditional_1_Conditional_0_Template, 2, 0, "span", 14)(1, ApprovalTimeline_Conditional_0_For_8_Conditional_1_Conditional_1_Template, 2, 0, "span", 15);
  }
  if (rf & 2) {
    \u0275\u0275conditional(ctx.action === "approved" ? 0 : 1);
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 8);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const step_r1 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(step_r1.stepOrder);
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 9);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const step_r1 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(step_r1.stepOrder);
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u6307\u5B9A\u5BE9\u6838 ");
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u4E0A\u5C64\u7D1A ");
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_8_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 16);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const step_r1 = \u0275\u0275nextContext(2).$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\uFF08", step_r1.departmentName, "\uFF09");
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0);
    \u0275\u0275conditionalCreate(1, ApprovalTimeline_Conditional_0_For_8_Conditional_8_Conditional_1_Template, 2, 1, "span", 16);
  }
  if (rf & 2) {
    const step_r1 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275textInterpolate1(" ", step_r1.jobTitleName || "\u2014", " ");
    \u0275\u0275advance();
    \u0275\u0275conditional(step_r1.departmentName ? 1 : -1);
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 12);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const step_r1 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(step_r1.note);
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 18);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const rec_r2 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\u4EE3\u7406 ", rec_r2.onBehalfOf);
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 19);
    \u0275\u0275text(1, "\u5347\u7D1A\u5BE9\u6838");
    \u0275\u0275domElementEnd();
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 20);
    \u0275\u0275text(1, "\u5DF2\u6838\u51C6");
    \u0275\u0275domElementEnd();
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 21);
    \u0275\u0275text(1, "\u9000\u56DE\u4FEE\u6539");
    \u0275\u0275domElementEnd();
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 22);
    \u0275\u0275text(1, "\u5DF2\u62D2\u7D55");
    \u0275\u0275domElementEnd();
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 23);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const rec_r2 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\u300C", rec_r2.reviewNote, "\u300D");
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 17);
    \u0275\u0275text(1);
    \u0275\u0275conditionalCreate(2, ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_2_Template, 2, 1, "span", 18)(3, ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_3_Template, 2, 0, "span", 19);
    \u0275\u0275text(4);
    \u0275\u0275pipe(5, "date");
    \u0275\u0275conditionalCreate(6, ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_6_Template, 2, 0, "span", 20)(7, ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_7_Template, 2, 0, "span", 21)(8, ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_8_Template, 2, 0, "span", 22);
    \u0275\u0275domElementEnd();
    \u0275\u0275conditionalCreate(9, ApprovalTimeline_Conditional_0_For_8_Conditional_10_Conditional_9_Template, 2, 1, "div", 23);
  }
  if (rf & 2) {
    const rec_r2 = ctx;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", rec_r2.reviewedBy, " ");
    \u0275\u0275advance();
    \u0275\u0275conditional(rec_r2.isEscalated && rec_r2.onBehalfOf ? 2 : rec_r2.isEscalated ? 3 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" \xB7 ", \u0275\u0275pipeBind2(5, 5, rec_r2.reviewedAt, "yyyy-MM-dd"), " \xB7 ");
    \u0275\u0275advance(2);
    \u0275\u0275conditional(rec_r2.action === "approved" ? 6 : rec_r2.action === "returned" ? 7 : 8);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(rec_r2.reviewNote ? 9 : -1);
  }
}
function ApprovalTimeline_Conditional_0_For_8_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 13);
    \u0275\u0275text(1, "\u5BE9\u6838\u4E2D\u2026");
    \u0275\u0275domElementEnd();
  }
}
function ApprovalTimeline_Conditional_0_For_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "li", 7);
    \u0275\u0275conditionalCreate(1, ApprovalTimeline_Conditional_0_For_8_Conditional_1_Template, 2, 1)(2, ApprovalTimeline_Conditional_0_For_8_Conditional_2_Template, 2, 1, "span", 8)(3, ApprovalTimeline_Conditional_0_For_8_Conditional_3_Template, 2, 1, "span", 9);
    \u0275\u0275domElementStart(4, "div", 10)(5, "div", 11);
    \u0275\u0275conditionalCreate(6, ApprovalTimeline_Conditional_0_For_8_Conditional_6_Template, 1, 0)(7, ApprovalTimeline_Conditional_0_For_8_Conditional_7_Template, 1, 0)(8, ApprovalTimeline_Conditional_0_For_8_Conditional_8_Template, 2, 2);
    \u0275\u0275domElementEnd();
    \u0275\u0275conditionalCreate(9, ApprovalTimeline_Conditional_0_For_8_Conditional_9_Template, 2, 1, "div", 12);
    \u0275\u0275conditionalCreate(10, ApprovalTimeline_Conditional_0_For_8_Conditional_10_Template, 10, 8)(11, ApprovalTimeline_Conditional_0_For_8_Conditional_11_Template, 2, 0, "div", 13);
    \u0275\u0275domElementEnd()();
  }
  if (rf & 2) {
    let tmp_13_0;
    let tmp_16_0;
    const step_r1 = ctx.$implicit;
    const \u0275$index_15_r3 = ctx.$index;
    const \u0275$count_15_r4 = ctx.$count;
    const ctx_r4 = \u0275\u0275nextContext(2);
    \u0275\u0275classProp("mb-6", !(\u0275$index_15_r3 === \u0275$count_15_r4 - 1));
    \u0275\u0275advance();
    \u0275\u0275conditional((tmp_13_0 = ctx_r4.getRecord(step_r1.stepOrder)) ? 1 : ctx_r4.currentStepOrder() === step_r1.stepOrder && ctx_r4.status() === "pending" ? 2 : 3, tmp_13_0);
    \u0275\u0275advance(5);
    \u0275\u0275conditional(step_r1.useApplicantDesignated ? 6 : step_r1.useDirectSupervisor ? 7 : 8);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(step_r1.note ? 9 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional((tmp_16_0 = ctx_r4.getRecord(step_r1.stepOrder)) ? 10 : ctx_r4.currentStepOrder() === step_r1.stepOrder && ctx_r4.status() === "pending" ? 11 : -1, tmp_16_0);
  }
}
function ApprovalTimeline_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 0)(1, "div", 1);
    \u0275\u0275namespaceSVG();
    \u0275\u0275domElementStart(2, "svg", 2);
    \u0275\u0275domElement(3, "use", 3);
    \u0275\u0275domElementEnd();
    \u0275\u0275text(4, " \u7C3D\u6838\u6D41\u7A0B ");
    \u0275\u0275domElementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275domElementStart(5, "div", 4)(6, "ol", 5);
    \u0275\u0275repeaterCreate(7, ApprovalTimeline_Conditional_0_For_8_Template, 12, 6, "li", 6, _forTrack0);
    \u0275\u0275domElementEnd()()();
  }
  if (rf & 2) {
    const ctx_r4 = \u0275\u0275nextContext();
    \u0275\u0275advance(7);
    \u0275\u0275repeater(ctx_r4.flow().steps);
  }
}
var ApprovalTimeline = class _ApprovalTimeline {
  flow = input(null, ...ngDevMode ? [{ debugName: "flow" }] : []);
  approvalRecords = input([], ...ngDevMode ? [{ debugName: "approvalRecords" }] : []);
  currentStepOrder = input(0, ...ngDevMode ? [{ debugName: "currentStepOrder" }] : []);
  status = input("", ...ngDevMode ? [{ debugName: "status" }] : []);
  getRecord(stepOrder) {
    return this.approvalRecords().find((r) => r.stepOrder === stepOrder);
  }
  static \u0275fac = function ApprovalTimeline_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ApprovalTimeline)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ApprovalTimeline, selectors: [["app-approval-timeline"]], inputs: { flow: [1, "flow"], approvalRecords: [1, "approvalRecords"], currentStepOrder: [1, "currentStepOrder"], status: [1, "status"] }, decls: 1, vars: 1, consts: [[1, "card", "border-0", "shadow-sm", "mt-6"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#git-merge"], [1, "card-body"], [1, "list-none", "p-0", "mb-0"], [1, "flex", "items-start", "gap-3", 3, "mb-6"], [1, "flex", "items-start", "gap-3"], [1, "badge", "bg-primary", "rounded-circle", "flex", "items-center", "justify-center", "shrink-0", 2, "width", "28px", "height", "28px", "min-width", "28px", "font-size", ".75rem"], [1, "badge", "bg-[--bg-base]", "text-[--text-muted]", "rounded-circle", "flex", "items-center", "justify-center", "shrink-0", 2, "width", "28px", "height", "28px", "min-width", "28px", "font-size", ".75rem"], [1, "grow"], [1, "fw-500"], [1, "text-muted", "small"], [1, "text-primary", "small", "mt-1"], [1, "badge", "bg-success", "rounded-circle", "flex", "items-center", "justify-center", "shrink-0", 2, "width", "28px", "height", "28px", "min-width", "28px", "font-size", ".85rem"], [1, "badge", "bg-danger", "rounded-circle", "flex", "items-center", "justify-center", "shrink-0", 2, "width", "28px", "height", "28px", "min-width", "28px", "font-size", ".85rem"], [1, "text-muted", "font-normal"], [1, "text-muted", "small", "mt-1"], [1, "badge", "bg-[--bg-elevated]", "text-[--accent]", "ms-1", 2, "font-size", ".7rem"], [1, "badge", "bg-[--bg-elevated]", "text-[--purple]", "ms-1", 2, "font-size", ".7rem"], [1, "text-success"], [1, "text-[--yellow]"], [1, "text-danger"], [1, "text-muted", "small", "italic", "mt-1"]], template: function ApprovalTimeline_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275conditionalCreate(0, ApprovalTimeline_Conditional_0_Template, 9, 0, "div", 0);
    }
    if (rf & 2) {
      \u0275\u0275conditional(ctx.flow() ? 0 : -1);
    }
  }, dependencies: [DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ApprovalTimeline, [{
    type: Component,
    args: [{
      selector: "app-approval-timeline",
      standalone: true,
      imports: [DatePipe],
      template: `
    @if (flow()) {
      <div class="card border-0 shadow-sm mt-6">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-primary" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#git-merge"></use></svg>
          \u7C3D\u6838\u6D41\u7A0B
        </div>
        <div class="card-body">
          <ol class="list-none p-0 mb-0">
            @for (step of flow()!.steps; track step.stepOrder; let last = $last) {
              <li class="flex items-start gap-3" [class.mb-6]="!last">
                <!-- \u6B65\u9A5F\u5713\u5708 -->
                @if (getRecord(step.stepOrder); as rec) {
                  @if (rec.action === 'approved') {
                    <span class="badge bg-success rounded-circle flex items-center justify-center shrink-0"
                          style="width:28px;height:28px;min-width:28px;font-size:.85rem">\u2713</span>
                  } @else {
                    <span class="badge bg-danger rounded-circle flex items-center justify-center shrink-0"
                          style="width:28px;height:28px;min-width:28px;font-size:.85rem">\u2717</span>
                  }
                } @else if (currentStepOrder() === step.stepOrder && status() === 'pending') {
                  <span class="badge bg-primary rounded-circle flex items-center justify-center shrink-0"
                        style="width:28px;height:28px;min-width:28px;font-size:.75rem">{{ step.stepOrder }}</span>
                } @else {
                  <span class="badge bg-[--bg-base] text-[--text-muted] rounded-circle flex items-center justify-center shrink-0"
                        style="width:28px;height:28px;min-width:28px;font-size:.75rem">{{ step.stepOrder }}</span>
                }
                <!-- \u6B65\u9A5F\u5167\u5BB9 -->
                <div class="grow">
                  <div class="fw-500">
                    @if (step.useApplicantDesignated) {
                      \u6307\u5B9A\u5BE9\u6838
                    } @else if (step.useDirectSupervisor) {
                      \u4E0A\u5C64\u7D1A
                    } @else {
                      {{ step.jobTitleName || '\u2014' }}
                      @if (step.departmentName) {
                        <span class="text-muted font-normal">\uFF08{{ step.departmentName }}\uFF09</span>
                      }
                    }
                  </div>
                  @if (step.note) {
                    <div class="text-muted small">{{ step.note }}</div>
                  }
                  @if (getRecord(step.stepOrder); as rec) {
                    <div class="text-muted small mt-1">
                      {{ rec.reviewedBy }}
                      @if (rec.isEscalated && rec.onBehalfOf) {
                        <span class="badge bg-[--bg-elevated] text-[--accent] ms-1" style="font-size:.7rem">\u4EE3\u7406 {{ rec.onBehalfOf }}</span>
                      } @else if (rec.isEscalated) {
                        <span class="badge bg-[--bg-elevated] text-[--purple] ms-1" style="font-size:.7rem">\u5347\u7D1A\u5BE9\u6838</span>
                      }
                      \xB7 {{ rec.reviewedAt | date:'yyyy-MM-dd' }} \xB7
                      @if (rec.action === 'approved') {
                        <span class="text-success">\u5DF2\u6838\u51C6</span>
                      } @else if (rec.action === 'returned') {
                        <span class="text-[--yellow]">\u9000\u56DE\u4FEE\u6539</span>
                      } @else {
                        <span class="text-danger">\u5DF2\u62D2\u7D55</span>
                      }
                    </div>
                    @if (rec.reviewNote) {
                      <div class="text-muted small italic mt-1">\u300C{{ rec.reviewNote }}\u300D</div>
                    }
                  } @else if (currentStepOrder() === step.stepOrder && status() === 'pending') {
                    <div class="text-primary small mt-1">\u5BE9\u6838\u4E2D\u2026</div>
                  }
                </div>
              </li>
            }
          </ol>
        </div>
      </div>
    }
  `
    }]
  }], null, { flow: [{ type: Input, args: [{ isSignal: true, alias: "flow", required: false }] }], approvalRecords: [{ type: Input, args: [{ isSignal: true, alias: "approvalRecords", required: false }] }], currentStepOrder: [{ type: Input, args: [{ isSignal: true, alias: "currentStepOrder", required: false }] }], status: [{ type: Input, args: [{ isSignal: true, alias: "status", required: false }] }] });
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ApprovalTimeline, { className: "ApprovalTimeline", filePath: "src/app/shared/components/approval-timeline.ts", lineNumber: 85 });
})();

export {
  ApprovalTimeline
};
//# sourceMappingURL=chunk-LL6IEINF.js.map
