import {
  ApprovalService,
  JobTitleService,
  NgbModal,
  ProjectService,
  UserService
} from "./chunk-R2JZPDEJ.js";
import {
  ApprovalTimeline
} from "./chunk-4EDIXUYL.js";
import "./chunk-JZXKSRAX.js";
import {
  ApprovalTaskService
} from "./chunk-WOBG5YTX.js";
import {
  DefaultValueAccessor,
  FormArrayName,
  FormBuilder,
  FormControlName,
  FormGroupDirective,
  FormGroupName,
  FormsModule,
  MinValidator,
  NgControlStatus,
  NgControlStatusGroup,
  NgModel,
  NgSelectOption,
  NumberValueAccessor,
  ReactiveFormsModule,
  SelectControlValueAccessor,
  Validators,
  ɵNgNoValidate,
  ɵNgSelectMultipleOption
} from "./chunk-B47DR4M3.js";
import {
  APPROVAL_STATUS_CLASSES,
  APPROVAL_STATUS_LABELS,
  ITEM_CATEGORIES
} from "./chunk-GUKP6DWR.js";
import {
  TravelPaymentRequestService
} from "./chunk-XO7HHABQ.js";
import {
  ActivatedRoute,
  Router,
  RouterLink
} from "./chunk-YFLF2WG5.js";
import "./chunk-OM24C6VF.js";
import {
  ChangeDetectorRef,
  Component,
  DatePipe,
  DecimalPipe,
  ViewChild,
  inject,
  setClassMetadata,
  signal,
  viewChild,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵattribute,
  ɵɵclassMap,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵgetCurrentView,
  ɵɵlistener,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind2,
  ɵɵproperty,
  ɵɵpureFunction0,
  ɵɵqueryAdvance,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵrepeaterTrackByIdentity,
  ɵɵrepeaterTrackByIndex,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵtemplate,
  ɵɵtemplateRefExtractor,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1,
  ɵɵtextInterpolate3,
  ɵɵtwoWayBindingSet,
  ɵɵtwoWayListener,
  ɵɵtwoWayProperty,
  ɵɵviewQuerySignal
} from "./chunk-EZPNPJLO.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/admin/travel-payment-requests/pages/travel-payment-form/travel-payment-form.ts
var _c0 = ["successModal"];
var _c1 = () => ({ standalone: true });
var _forTrack0 = ($index, $item) => $item.id;
function TravelPaymentForm_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 7);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 50);
    \u0275\u0275element(2, "use", 51);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" ", ctx_r1.errorMsg(), " ");
  }
}
function TravelPaymentForm_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 52);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 50);
    \u0275\u0275element(3, "use", 53);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5BE9\u6838\u4E2D\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function TravelPaymentForm_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 54);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 50);
    \u0275\u0275element(3, "use", 51);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5DF2\u88AB\u9000\u56DE\uFF0C\u8ACB\u4FEE\u6539\u5F8C\u91CD\u65B0\u9001\u51FA\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function TravelPaymentForm_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 55);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 50);
    \u0275\u0275element(3, "use", 56);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5DF2\u6838\u51C6\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function TravelPaymentForm_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 57);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 50);
    \u0275\u0275element(3, "use", 58);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5DF2\u88AB\u62D2\u7D55\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function TravelPaymentForm_Conditional_28_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u586B\u5BEB\u51FA\u5DEE\u5730\u9EDE\u3002");
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentForm_For_36_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 24);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const p_r3 = ctx.$implicit;
    \u0275\u0275property("ngValue", p_r3.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate3("", p_r3.code, " - ", p_r3.name, "", p_r3.departmentName ? "\uFF08" + p_r3.departmentName + "\uFF09" : "");
  }
}
function TravelPaymentForm_Conditional_37_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 25);
    \u0275\u0275text(1, "\u60A8\u76EE\u524D\u53EF\u7533\u8ACB\u7684\u5C08\u6848\u6E05\u55AE\u70BA\u7A7A\uFF0C\u8ACB\u806F\u7D61\u4E3B\u7BA1\u6216\u78BA\u8A8D\u90E8\u9580\u8A2D\u5B9A\u3002");
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentForm_Conditional_45_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u51FA\u767C\u65E5\u671F\u3002");
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentForm_Conditional_52_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u8FD4\u56DE\u65E5\u671F\u3002");
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentForm_Conditional_59_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u586B\u5BEB\u51FA\u5DEE\u76EE\u7684\u3002");
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentForm_Conditional_60_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 28)(1, "label", 19);
    \u0275\u0275text(2, "\u7C3D\u6838\u72C0\u614B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div")(4, "span", 59);
    \u0275\u0275text(5);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275classMap(ctx_r1.statusClass[ctx_r1.approvalStatus]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.statusLabel[ctx_r1.approvalStatus], " ");
  }
}
function TravelPaymentForm_Conditional_67_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 60);
    \u0275\u0275listener("click", function TravelPaymentForm_Conditional_67_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.addItem());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 50);
    \u0275\u0275element(2, "use", 61);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u65B0\u589E\u9805\u76EE ");
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentForm_Conditional_91_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "th", 44);
  }
}
function TravelPaymentForm_For_94_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 63);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_13_0;
    const ctrl_r5 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate((tmp_13_0 = ctrl_r5.get("category")) == null ? null : tmp_13_0.value);
  }
}
function TravelPaymentForm_For_94_Conditional_3_For_4_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 77);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const cat_r6 = ctx.$implicit;
    \u0275\u0275property("value", cat_r6);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(cat_r6);
  }
}
function TravelPaymentForm_For_94_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "select", 64)(1, "option", 76);
    \u0275\u0275text(2, "\u9078\u64C7");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(3, TravelPaymentForm_For_94_Conditional_3_For_4_Template, 2, 2, "option", 77, \u0275\u0275repeaterTrackByIdentity);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(3);
    \u0275\u0275repeater(ctx_r1.categories);
  }
}
function TravelPaymentForm_For_94_Conditional_5_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 63);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_13_0;
    const ctrl_r5 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate((tmp_13_0 = ctrl_r5.get("seqNo")) == null ? null : tmp_13_0.value);
  }
}
function TravelPaymentForm_For_94_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 65);
  }
}
function TravelPaymentForm_For_94_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 63);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_13_0;
    const ctrl_r5 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate((tmp_13_0 = ctrl_r5.get("itemName")) == null ? null : tmp_13_0.value);
  }
}
function TravelPaymentForm_For_94_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 66);
  }
}
function TravelPaymentForm_For_94_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 63);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "number");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_13_0;
    const ctrl_r5 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(2, 1, (tmp_13_0 = ctrl_r5.get("unitPrice")) == null ? null : tmp_13_0.value, "1.0-0"));
  }
}
function TravelPaymentForm_For_94_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    const _r7 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "input", 78);
    \u0275\u0275listener("input", function TravelPaymentForm_For_94_Conditional_12_Template_input_input_0_listener() {
      \u0275\u0275restoreView(_r7);
      const ctrl_r5 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.calcTotal(ctrl_r5));
    });
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentForm_For_94_Conditional_14_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 63);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_13_0;
    const ctrl_r5 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate((tmp_13_0 = ctrl_r5.get("quantity")) == null ? null : tmp_13_0.value);
  }
}
function TravelPaymentForm_For_94_Conditional_15_Template(rf, ctx) {
  if (rf & 1) {
    const _r8 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "input", 79);
    \u0275\u0275listener("input", function TravelPaymentForm_For_94_Conditional_15_Template_input_input_0_listener() {
      \u0275\u0275restoreView(_r8);
      const ctrl_r5 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.calcTotal(ctrl_r5));
    });
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentForm_For_94_Conditional_17_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 69);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "number");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_13_0;
    const ctrl_r5 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(2, 1, (tmp_13_0 = ctrl_r5.get("totalPrice")) == null ? null : tmp_13_0.value, "1.0-0"));
  }
}
function TravelPaymentForm_For_94_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 70);
  }
}
function TravelPaymentForm_For_94_Conditional_20_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 63);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_13_0;
    const ctrl_r5 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(((tmp_13_0 = ctrl_r5.get("note")) == null ? null : tmp_13_0.value) || "\u2014");
  }
}
function TravelPaymentForm_For_94_Conditional_21_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 71);
  }
}
function TravelPaymentForm_For_94_Conditional_23_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 72);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_13_0;
    const ctrl_r5 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(((tmp_13_0 = ctrl_r5.get("invoiceNo")) == null ? null : tmp_13_0.value) || "\u2014");
  }
}
function TravelPaymentForm_For_94_Conditional_24_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 73);
  }
}
function TravelPaymentForm_For_94_Conditional_26_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 63);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_13_0;
    const ctrl_r5 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(((tmp_13_0 = ctrl_r5.get("invoiceDate")) == null ? null : tmp_13_0.value) || "\u2014");
  }
}
function TravelPaymentForm_For_94_Conditional_27_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 74);
  }
}
function TravelPaymentForm_For_94_Conditional_28_Template(rf, ctx) {
  if (rf & 1) {
    const _r9 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "td", 75)(1, "button", 80);
    \u0275\u0275listener("click", function TravelPaymentForm_For_94_Conditional_28_Template_button_click_1_listener() {
      \u0275\u0275restoreView(_r9);
      const \u0275$index_229_r10 = \u0275\u0275nextContext().$index;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.removeItem(\u0275$index_229_r10));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 50);
    \u0275\u0275element(3, "use", 81);
    \u0275\u0275elementEnd()()();
  }
}
function TravelPaymentForm_For_94_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr", 46)(1, "td", 62);
    \u0275\u0275conditionalCreate(2, TravelPaymentForm_For_94_Conditional_2_Template, 2, 1, "span", 63)(3, TravelPaymentForm_For_94_Conditional_3_Template, 5, 0, "select", 64);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "td", 62);
    \u0275\u0275conditionalCreate(5, TravelPaymentForm_For_94_Conditional_5_Template, 2, 1, "span", 63)(6, TravelPaymentForm_For_94_Conditional_6_Template, 1, 0, "input", 65);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "td", 62);
    \u0275\u0275conditionalCreate(8, TravelPaymentForm_For_94_Conditional_8_Template, 2, 1, "span", 63)(9, TravelPaymentForm_For_94_Conditional_9_Template, 1, 0, "input", 66);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "td", 62);
    \u0275\u0275conditionalCreate(11, TravelPaymentForm_For_94_Conditional_11_Template, 3, 4, "span", 63)(12, TravelPaymentForm_For_94_Conditional_12_Template, 1, 0, "input", 67);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "td", 62);
    \u0275\u0275conditionalCreate(14, TravelPaymentForm_For_94_Conditional_14_Template, 2, 1, "span", 63)(15, TravelPaymentForm_For_94_Conditional_15_Template, 1, 0, "input", 68);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(16, "td", 62);
    \u0275\u0275conditionalCreate(17, TravelPaymentForm_For_94_Conditional_17_Template, 3, 4, "span", 69)(18, TravelPaymentForm_For_94_Conditional_18_Template, 1, 0, "input", 70);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "td", 62);
    \u0275\u0275conditionalCreate(20, TravelPaymentForm_For_94_Conditional_20_Template, 2, 1, "span", 63)(21, TravelPaymentForm_For_94_Conditional_21_Template, 1, 0, "input", 71);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(22, "td", 62);
    \u0275\u0275conditionalCreate(23, TravelPaymentForm_For_94_Conditional_23_Template, 2, 1, "span", 72)(24, TravelPaymentForm_For_94_Conditional_24_Template, 1, 0, "input", 73);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(25, "td", 62);
    \u0275\u0275conditionalCreate(26, TravelPaymentForm_For_94_Conditional_26_Template, 2, 1, "span", 63)(27, TravelPaymentForm_For_94_Conditional_27_Template, 1, 0, "input", 74);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(28, TravelPaymentForm_For_94_Conditional_28_Template, 4, 0, "td", 75);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const \u0275$index_229_r10 = ctx.$index;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275property("formGroupName", \u0275$index_229_r10);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 2 : 3);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 5 : 6);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 8 : 9);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 11 : 12);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 14 : 15);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 17 : 18);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 20 : 21);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 23 : 24);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 26 : 27);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(!ctx_r1.isReadOnly ? 28 : -1);
  }
}
function TravelPaymentForm_ForEmpty_95_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 82);
    \u0275\u0275text(2, "\u5C1A\u672A\u65B0\u589E\u8CBB\u7528\u9805\u76EE");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275attribute("colspan", ctx_r1.isReadOnly ? 9 : 10);
  }
}
function TravelPaymentForm_Conditional_96_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "td");
  }
}
function TravelPaymentForm_Conditional_96_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tfoot")(1, "tr", 38)(2, "td", 83);
    \u0275\u0275text(3, "\u5408\u8A08");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "td", 84);
    \u0275\u0275text(5);
    \u0275\u0275pipe(6, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275element(7, "td", 85);
    \u0275\u0275conditionalCreate(8, TravelPaymentForm_Conditional_96_Conditional_8_Template, 1, 0, "td");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(6, 2, ctx_r1.grandTotal, "1.0-0"));
    \u0275\u0275advance(3);
    \u0275\u0275conditional(!ctx_r1.isReadOnly ? 8 : -1);
  }
}
function TravelPaymentForm_Conditional_97_For_10_For_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 24);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const jt_r15 = ctx.$implicit;
    \u0275\u0275property("ngValue", jt_r15.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(jt_r15.name);
  }
}
function TravelPaymentForm_Conditional_97_For_10_For_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 24);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const user_r16 = ctx.$implicit;
    \u0275\u0275property("ngValue", user_r16.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(user_r16.name);
  }
}
function TravelPaymentForm_Conditional_97_For_10_Template(rf, ctx) {
  if (rf & 1) {
    const _r12 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 87)(1, "span", 89);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "select", 90);
    \u0275\u0275twoWayListener("ngModelChange", function TravelPaymentForm_Conditional_97_For_10_Template_select_ngModelChange_3_listener($event) {
      const entry_r13 = \u0275\u0275restoreView(_r12).$implicit;
      \u0275\u0275twoWayBindingSet(entry_r13.selectedJobTitleId, $event) || (entry_r13.selectedJobTitleId = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275listener("ngModelChange", function TravelPaymentForm_Conditional_97_For_10_Template_select_ngModelChange_3_listener() {
      const \u0275$index_371_r14 = \u0275\u0275restoreView(_r12).$index;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.onEntryJobTitleChange(\u0275$index_371_r14));
    });
    \u0275\u0275elementStart(4, "option", 24);
    \u0275\u0275text(5, "\u2014 \u8077\u7A31 \u2014");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(6, TravelPaymentForm_Conditional_97_For_10_For_7_Template, 2, 2, "option", 24, _forTrack0);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "select", 91);
    \u0275\u0275twoWayListener("ngModelChange", function TravelPaymentForm_Conditional_97_For_10_Template_select_ngModelChange_8_listener($event) {
      const entry_r13 = \u0275\u0275restoreView(_r12).$implicit;
      \u0275\u0275twoWayBindingSet(entry_r13.selectedUserId, $event) || (entry_r13.selectedUserId = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementStart(9, "option", 24);
    \u0275\u0275text(10, "\u2014 \u4EBA\u54E1 \u2014");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(11, TravelPaymentForm_Conditional_97_For_10_For_12_Template, 2, 2, "option", 24, _forTrack0);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "button", 92);
    \u0275\u0275listener("click", function TravelPaymentForm_Conditional_97_For_10_Template_button_click_13_listener() {
      const \u0275$index_371_r14 = \u0275\u0275restoreView(_r12).$index;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.removeDesignatedEntry(\u0275$index_371_r14));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(14, "svg", 50);
    \u0275\u0275element(15, "use", 81);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const entry_r13 = ctx.$implicit;
    const \u0275$index_371_r14 = ctx.$index;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1("", \u0275$index_371_r14 + 1, ".");
    \u0275\u0275advance();
    \u0275\u0275twoWayProperty("ngModel", entry_r13.selectedJobTitleId);
    \u0275\u0275property("ngModelOptions", \u0275\u0275pureFunction0(7, _c1));
    \u0275\u0275advance();
    \u0275\u0275property("ngValue", null);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(ctx_r1.jobTitles);
    \u0275\u0275advance(2);
    \u0275\u0275twoWayProperty("ngModel", entry_r13.selectedUserId);
    \u0275\u0275property("ngModelOptions", \u0275\u0275pureFunction0(8, _c1));
    \u0275\u0275advance();
    \u0275\u0275property("ngValue", null);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(entry_r13.filteredUsers);
  }
}
function TravelPaymentForm_Conditional_97_Template(rf, ctx) {
  if (rf & 1) {
    const _r11 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 30)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 86);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6307\u5B9A\u5BE9\u6838\u8005 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "div", 6)(7, "label", 19);
    \u0275\u0275text(8, "\u6307\u5B9A\u5BE9\u6838\u8005\uFF08\u4F9D\u5E8F\u5BE9\u6838\uFF09");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(9, TravelPaymentForm_Conditional_97_For_10_Template, 16, 9, "div", 87, \u0275\u0275repeaterTrackByIndex);
    \u0275\u0275elementStart(11, "button", 88);
    \u0275\u0275listener("click", function TravelPaymentForm_Conditional_97_Template_button_click_11_listener() {
      \u0275\u0275restoreView(_r11);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.addDesignatedEntry());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(12, "svg", 50);
    \u0275\u0275element(13, "use", 61);
    \u0275\u0275elementEnd();
    \u0275\u0275text(14, " \u65B0\u589E\u5BE9\u6838\u4EBA ");
    \u0275\u0275elementEnd()()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(9);
    \u0275\u0275repeater(ctx_r1.designatedEntries);
  }
}
function TravelPaymentForm_Conditional_98_For_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "li", 63);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const entry_r17 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.getUserName(entry_r17.selectedUserId));
  }
}
function TravelPaymentForm_Conditional_98_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 30)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 86);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6307\u5B9A\u5BE9\u6838\u8005 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "div", 6)(7, "label", 19);
    \u0275\u0275text(8, "\u6307\u5B9A\u5BE9\u6838\u8005\uFF08\u4F9D\u5E8F\u5BE9\u6838\uFF09");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "ol", 93);
    \u0275\u0275repeaterCreate(10, TravelPaymentForm_Conditional_98_For_11_Template, 2, 1, "li", 63, \u0275\u0275repeaterTrackByIndex);
    \u0275\u0275elementEnd()()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(10);
    \u0275\u0275repeater(ctx_r1.designatedEntries);
  }
}
function TravelPaymentForm_Conditional_99_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 97)(1, "div", 98);
    \u0275\u0275text(2, "\u9810\u8A08\u64A5\u6B3E\u65E5");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 99);
    \u0275\u0275text(4);
    \u0275\u0275pipe(5, "date");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(5, 1, ctx_r1.existingRequest.estimatedPaymentDate, "yyyy-MM-dd"));
  }
}
function TravelPaymentForm_Conditional_99_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 97)(1, "div", 98);
    \u0275\u0275text(2, "\u64A5\u6B3E\u65E5");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 100);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(4, "svg", 101);
    \u0275\u0275element(5, "use", 56);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(6, "span", 102);
    \u0275\u0275text(7);
    \u0275\u0275pipe(8, "date");
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(8, 1, ctx_r1.existingRequest.paidAt, "yyyy-MM-dd"));
  }
}
function TravelPaymentForm_Conditional_99_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 97)(1, "div", 98);
    \u0275\u0275text(2, "\u64A5\u6B3E\u65E5");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "span", 98);
    \u0275\u0275text(4, "\u5C1A\u672A\u64A5\u6B3E");
    \u0275\u0275elementEnd()();
  }
}
function TravelPaymentForm_Conditional_99_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 30)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 94);
    \u0275\u0275element(3, "use", 95);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u64A5\u6B3E\u8CC7\u8A0A ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "div", 96);
    \u0275\u0275conditionalCreate(7, TravelPaymentForm_Conditional_99_Conditional_7_Template, 6, 4, "div", 97);
    \u0275\u0275conditionalCreate(8, TravelPaymentForm_Conditional_99_Conditional_8_Template, 9, 4, "div", 97)(9, TravelPaymentForm_Conditional_99_Conditional_9_Template, 5, 0, "div", 97);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(7);
    \u0275\u0275conditional(ctx_r1.existingRequest.estimatedPaymentDate ? 7 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.existingRequest.paidAt ? 8 : ctx_r1.existingRequest.estimatedPaymentDate ? 9 : -1);
  }
}
function TravelPaymentForm_Conditional_101_Template(rf, ctx) {
  if (rf & 1) {
    const _r18 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 48)(1, "button", 103);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "button", 104);
    \u0275\u0275listener("click", function TravelPaymentForm_Conditional_101_Template_button_click_3_listener() {
      \u0275\u0275restoreView(_r18);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.submitForApproval());
    });
    \u0275\u0275text(4, " \u9001\u51FA\u7533\u8ACB ");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "a", 105);
    \u0275\u0275text(6, "\u53D6\u6D88");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r1.form.invalid || ctx_r1.itemArray.length === 0);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.isEdit ? "\u5132\u5B58" : "\u5132\u5B58\u8349\u7A3F", " ");
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r1.form.invalid || ctx_r1.itemArray.length === 0);
  }
}
function TravelPaymentForm_Conditional_102_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 49)(1, "a", 105);
    \u0275\u0275text(2, "\u8FD4\u56DE\u5217\u8868");
    \u0275\u0275elementEnd()();
  }
}
function TravelPaymentForm_ng_template_103_Template(rf, ctx) {
  if (rf & 1) {
    const _r19 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 106)(1, "button", 107);
    \u0275\u0275listener("click", function TravelPaymentForm_ng_template_103_Template_button_click_1_listener() {
      const modal_r20 = \u0275\u0275restoreView(_r19).$implicit;
      return \u0275\u0275resetView(modal_r20.close());
    });
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(2, "div", 108);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(3, "svg", 109);
    \u0275\u0275element(4, "use", 56);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "h5", 110);
    \u0275\u0275text(6, "\u7533\u8ACB\u6210\u529F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "p", 111);
    \u0275\u0275text(8, "\u8ACB\u76E1\u65E9\u5C07\u6B63\u672C\u8CC7\u6599\u9001\u56DE\u7BA1\u7406\u8655");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(9, "div", 112)(10, "button", 113);
    \u0275\u0275listener("click", function TravelPaymentForm_ng_template_103_Template_button_click_10_listener() {
      const modal_r20 = \u0275\u0275restoreView(_r19).$implicit;
      return \u0275\u0275resetView(modal_r20.close());
    });
    \u0275\u0275text(11, "\u78BA\u5B9A");
    \u0275\u0275elementEnd()();
  }
}
var TravelPaymentForm = class _TravelPaymentForm {
  fb = inject(FormBuilder);
  service = inject(TravelPaymentRequestService);
  projects$ = inject(ProjectService);
  jobTitleSvc = inject(JobTitleService);
  userSvc = inject(UserService);
  approvalSvc = inject(ApprovalService);
  taskSvc = inject(ApprovalTaskService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  cdr = inject(ChangeDetectorRef);
  modal = inject(NgbModal);
  successModal = viewChild("successModal", ...ngDevMode ? [{ debugName: "successModal" }] : []);
  isEdit = false;
  requestId = 0;
  isReadOnly = false;
  isReturned = false;
  isDraft = true;
  approvalStatus = "draft";
  existingRequest = null;
  errorMsg = signal("", ...ngDevMode ? [{ debugName: "errorMsg" }] : []);
  projects = [];
  loadingProjects = true;
  categories = ITEM_CATEGORIES;
  /** 簽核流程時間軸 */
  approvalFlow = null;
  approvalRecords = [];
  taskCurrentStepOrder = 0;
  taskStatus = "";
  /** 指定審核者相關 */
  hasDesignatedStep = false;
  jobTitles = [];
  allUsers = [];
  /** 指定審核者條目清單（多人） */
  designatedEntries = [];
  addDesignatedEntry() {
    const nextOrder = this.designatedEntries.length + 1;
    this.designatedEntries.push({
      stepOrder: nextOrder,
      selectedJobTitleId: null,
      selectedUserId: null,
      filteredUsers: []
    });
  }
  removeDesignatedEntry(i) {
    this.designatedEntries.splice(i, 1);
    this.designatedEntries.forEach((e, idx) => e.stepOrder = idx + 1);
  }
  onEntryJobTitleChange(i) {
    const e = this.designatedEntries[i];
    e.filteredUsers = e.selectedJobTitleId ? this.allUsers.filter((u) => u.jobTitleId === e.selectedJobTitleId && u.status === "active") : [];
    e.selectedUserId = null;
  }
  getUserName(userId) {
    if (!userId)
      return "\u2014";
    return this.allUsers.find((u) => u.id === userId)?.name ?? userId;
  }
  statusLabel = APPROVAL_STATUS_LABELS;
  statusClass = APPROVAL_STATUS_CLASSES;
  form = this.fb.group({
    destination: ["", Validators.required],
    startDate: ["", Validators.required],
    endDate: ["", Validators.required],
    purpose: ["", Validators.required],
    projectId: [null],
    items: this.fb.array([])
  });
  get itemArray() {
    return this.form.get("items");
  }
  get itemControls() {
    return this.itemArray.controls;
  }
  get grandTotal() {
    return this.itemArray.controls.reduce((s, c) => s + (+c.get("totalPrice")?.value || 0), 0);
  }
  ngOnInit() {
    this.approvalSvc.getAll().subscribe((items) => {
      this.hasDesignatedStep = items.filter((i) => i.isActive && i.applicationType === "travel_payment").some((i) => i.steps.some((s) => s.useApplicantDesignated));
      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: (jts) => {
          this.jobTitles = jts;
        } });
        this.userSvc.getLookup().subscribe({
          next: (users) => {
            this.allUsers = users;
            this.designatedEntries.forEach((e) => {
              if (!e.selectedJobTitleId && e.selectedUserId) {
                e.selectedJobTitleId = users.find((u) => u.id === e.selectedUserId)?.jobTitleId ?? null;
              }
              if (e.selectedJobTitleId) {
                e.filteredUsers = users.filter((u) => u.jobTitleId === e.selectedJobTitleId && u.status === "active");
              }
            });
            this.cdr.markForCheck();
          }
        });
      }
      this.cdr.markForCheck();
    });
    this.projects$.getActive().subscribe({
      next: (p) => {
        this.projects = p;
        this.loadingProjects = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingProjects = false;
        this.errorMsg.set("\u8F09\u5165\u5C08\u6848\u8CC7\u6599\u5931\u6557\u3002");
      }
    });
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEdit = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe((r) => {
        if (!r)
          return;
        this.existingRequest = r;
        this.approvalStatus = r.approvalStatus;
        this.isDraft = r.approvalStatus === "draft";
        this.isReturned = r.approvalStatus === "returned";
        this.isReadOnly = r.approvalStatus !== "draft" && r.approvalStatus !== "returned";
        this.form.patchValue({
          destination: r.destination,
          startDate: r.startDate instanceof Date ? r.startDate.toISOString().split("T")[0] : String(r.startDate),
          endDate: r.endDate instanceof Date ? r.endDate.toISOString().split("T")[0] : String(r.endDate),
          purpose: r.purpose,
          projectId: r.projectId ?? null
        });
        if (r.designatedReviewers?.length) {
          this.designatedEntries = r.designatedReviewers.map((dr) => ({
            stepOrder: dr.stepOrder,
            selectedJobTitleId: this.allUsers.find((u) => u.id === dr.reviewerId)?.jobTitleId ?? null,
            selectedUserId: dr.reviewerId,
            filteredUsers: []
          }));
          if (this.allUsers.length > 0) {
            this.designatedEntries.forEach((e) => {
              if (e.selectedJobTitleId) {
                e.filteredUsers = this.allUsers.filter((u) => u.jobTitleId === e.selectedJobTitleId && u.status === "active");
              }
            });
          }
        }
        (r.items ?? []).forEach((item, idx) => this.itemArray.push(this._itemGroup(item.category, item.seqNo, item.itemName, item.unitPrice, item.quantity, item.totalPrice, item.note ?? "", item.invoiceNo ?? "", item.invoiceDate ?? "", idx)));
        if (this.isReadOnly)
          this.form.disable();
        if (r.approvalStatus !== "draft") {
          this.taskSvc.getById(this.requestId, "travel_payment").subscribe({
            next: (task) => {
              this.approvalFlow = task.flow ?? null;
              this.approvalRecords = task.approvalRecords ?? [];
              this.taskCurrentStepOrder = task.currentStepOrder;
              this.taskStatus = task.status;
              this.cdr.markForCheck();
            }
          });
        }
        this.cdr.markForCheck();
      });
    }
  }
  addItem() {
    this.itemArray.push(this._itemGroup("", 0, "", 0, "", 0, "", "", "", this.itemArray.length));
  }
  removeItem(i) {
    this.itemArray.removeAt(i);
  }
  /** 單價 × 數量（嘗試解析數量前面的數字） */
  calcTotal(ctrl) {
    const unitPrice = +ctrl.get("unitPrice")?.value || 0;
    const qtyStr = (ctrl.get("quantity")?.value ?? "").toString();
    const qtyNum = parseFloat(qtyStr) || 0;
    const total = Math.round(unitPrice * qtyNum);
    ctrl.get("totalPrice")?.setValue(total, { emitEvent: false });
  }
  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.form.invalid || this.itemArray.length === 0 || this.isReadOnly)
      return;
    const payload = this._buildPayload();
    const obs = this.isEdit ? this.service.update(this.requestId, payload) : this.service.create(payload);
    this.errorMsg.set("");
    obs.subscribe({
      next: (saved) => {
        if (!this.isEdit)
          this.requestId = saved.id;
        this.router.navigate(["/admin/travel-payment-requests"]);
      },
      error: (err) => {
        this.errorMsg.set(err.error?.message || "\u5132\u5B58\u5931\u6557\uFF0C\u8ACB\u7A0D\u5F8C\u518D\u8A66\u3002");
      }
    });
  }
  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.form.invalid || this.itemArray.length === 0 || this.isReadOnly)
      return;
    const payload = this._buildPayload();
    const save$ = this.isEdit ? this.service.update(this.requestId, payload) : this.service.create(payload);
    this.errorMsg.set("");
    save$.subscribe({
      next: (saved) => {
        this.service.submit(saved.id).subscribe({
          next: () => {
            const tpl = this.successModal();
            if (tpl) {
              const ref = this.modal.open(tpl, { centered: true, backdrop: "static", keyboard: false });
              ref.result.then(() => this.router.navigate(["/admin/travel-payment-requests"])).catch(() => this.router.navigate(["/admin/travel-payment-requests"]));
            } else {
              this.router.navigate(["/admin/travel-payment-requests"]);
            }
          },
          error: (err) => {
            this.errorMsg.set(err.error?.message || "\u9001\u51FA\u5931\u6557\uFF0C\u8ACB\u7A0D\u5F8C\u518D\u8A66\u3002");
          }
        });
      },
      error: (err) => {
        this.errorMsg.set(err.error?.message || "\u5132\u5B58\u5931\u6557\uFF0C\u8ACB\u7A0D\u5F8C\u518D\u8A66\u3002");
      }
    });
  }
  _buildPayload() {
    const v = this.form.value;
    const project = this.projects.find((p) => p.id === v.projectId);
    const reviewers = this.designatedEntries.filter((e) => e.selectedUserId).map((e) => ({ reviewerId: e.selectedUserId, stepOrder: e.stepOrder }));
    const items = this.itemArray.controls.map((c, idx) => ({
      category: c.get("category")?.value || "",
      seqNo: +c.get("seqNo")?.value || 0,
      itemName: c.get("itemName")?.value || "",
      unitPrice: +c.get("unitPrice")?.value || 0,
      quantity: c.get("quantity")?.value || "",
      totalPrice: +c.get("totalPrice")?.value || 0,
      note: c.get("note")?.value || "",
      invoiceNo: c.get("invoiceNo")?.value || "",
      invoiceDate: c.get("invoiceDate")?.value || "",
      sortOrder: idx
    }));
    const grandTotal = items.reduce((s, i) => s + i.totalPrice, 0);
    return {
      destination: v.destination,
      startDate: new Date(v.startDate),
      endDate: new Date(v.endDate),
      purpose: v.purpose,
      projectId: v.projectId ?? void 0,
      projectCode: project?.code,
      grandTotal,
      designatedReviewers: reviewers.length > 0 ? reviewers : void 0,
      items
    };
  }
  _itemGroup(category, seqNo, itemName, unitPrice, quantity, totalPrice, note, invoiceNo, invoiceDate, sortOrder) {
    return this.fb.group({
      category: [category, Validators.required],
      seqNo: [seqNo],
      itemName: [itemName, Validators.required],
      unitPrice: [unitPrice, [Validators.required, Validators.min(0)]],
      quantity: [quantity, Validators.required],
      totalPrice: [totalPrice],
      note: [note],
      invoiceNo: [invoiceNo],
      invoiceDate: [invoiceDate],
      sortOrder: [sortOrder]
    });
  }
  static \u0275fac = function TravelPaymentForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _TravelPaymentForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _TravelPaymentForm, selectors: [["app-travel-payment-form"]], viewQuery: function TravelPaymentForm_Query(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275viewQuerySignal(ctx.successModal, _c0, 5);
    }
    if (rf & 2) {
      \u0275\u0275queryAdvance();
    }
  }, decls: 105, vars: 23, consts: [["successModal", ""], [1, "container-fluid", "py-3"], [1, "flex", "items-center", "gap-2", "mb-6"], ["routerLink", "/admin/travel-payment-requests", 1, "btn", "btn-sm", "btn-outline-secondary"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], ["role", "alert", 1, "alert", "alert-danger", "flex", "items-center", "gap-2", "mb-6", "py-2"], [1, "card", "border-0", "shadow-sm", "mb-6"], [3, "ngSubmit", "formGroup"], [1, "row", "g-4"], [1, "col-12", "col-xl-10"], [1, "card", "border-0", "shadow-sm"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#map-pin"], [1, "card-body"], [1, "row", "g-3", "mb-4"], [1, "col-12", "col-md-6"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "destination", "placeholder", "\u4F8B\u5982\uFF1A\u53F0\u5357\u3001\u53F0\u4E2D", 1, "form-control"], [1, "text-danger", "small", "mt-1"], ["formControlName", "projectId", 1, "form-select"], [3, "ngValue"], [1, "text-muted", "small", "mt-1"], ["type", "date", "formControlName", "startDate", 1, "form-control"], ["type", "date", "formControlName", "endDate", 1, "form-control"], [1, "mb-4"], ["formControlName", "purpose", "rows", "3", "placeholder", "\u8ACB\u586B\u5BEB\u51FA\u5DEE\u76EE\u7684...", 1, "form-control"], [1, "card", "border-0", "shadow-sm", "mt-6"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "justify-between"], [1, "flex", "items-center", "gap-2", "fw-600"], ["href", "/assets/icons/sprite.svg#file-text"], ["type", "button", 1, "btn", "btn-sm", "btn-outline-primary", "inline-flex", "items-center", "gap-1"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-sm", "mb-0"], [1, "table-light"], [2, "min-width", "100px"], [2, "min-width", "60px"], [2, "min-width", "160px"], [2, "min-width", "80px"], [2, "min-width", "120px"], [2, "width", "48px"], ["formArrayName", "items"], [3, "formGroupName"], [3, "flow", "approvalRecords", "currentStepOrder", "status"], [1, "mt-6", "flex", "gap-2"], [1, "mt-6"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#alert-triangle"], [1, "card-header", "bg-[rgba(13,110,253,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-primary", "py-3"], ["href", "/assets/icons/sprite.svg#clock"], [1, "card-header", "bg-[rgba(255,193,7,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-warning", "py-3"], [1, "card-header", "bg-[rgba(37,162,68,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-success", "py-3"], ["href", "/assets/icons/sprite.svg#check-circle"], [1, "card-header", "bg-[rgba(220,53,69,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-danger", "py-3"], ["href", "/assets/icons/sprite.svg#x-circle"], [1, "badge", "rounded-pill", "px-3", "py-2"], ["type", "button", 1, "btn", "btn-sm", "btn-outline-primary", "inline-flex", "items-center", "gap-1", 3, "click"], ["href", "/assets/icons/sprite.svg#plus"], [1, "align-middle"], [1, "small"], ["formControlName", "category", 1, "form-select", "form-select-sm"], ["type", "number", "formControlName", "seqNo", "min", "1", 1, "form-control", "form-control-sm", 2, "width", "60px"], ["formControlName", "itemName", "placeholder", "\u9805\u76EE\u8AAA\u660E", 1, "form-control", "form-control-sm"], ["type", "number", "formControlName", "unitPrice", "min", "0", 1, "form-control", "form-control-sm"], ["formControlName", "quantity", "placeholder", "\u5982\uFF1A1\u5F0F", 1, "form-control", "form-control-sm"], [1, "small", "fw-500"], ["type", "number", "formControlName", "totalPrice", "min", "0", 1, "form-control", "form-control-sm"], ["formControlName", "note", "placeholder", "", 1, "form-control", "form-control-sm"], [1, "small", "font-monospace"], ["formControlName", "invoiceNo", "placeholder", "AB-12345678", 1, "form-control", "form-control-sm", "font-monospace"], ["type", "date", "formControlName", "invoiceDate", 1, "form-control", "form-control-sm"], [1, "text-right", "align-middle"], ["value", ""], [3, "value"], ["type", "number", "formControlName", "unitPrice", "min", "0", 1, "form-control", "form-control-sm", 3, "input"], ["formControlName", "quantity", "placeholder", "\u5982\uFF1A1\u5F0F", 1, "form-control", "form-control-sm", 3, "input"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-danger", "inline-flex", "items-center", 3, "click"], ["href", "/assets/icons/sprite.svg#x"], [1, "text-center", "text-muted", "py-4", "small"], ["colspan", "5", 1, "text-right", "fw-500", "small"], [1, "fw-600"], ["colspan", "3"], ["href", "/assets/icons/sprite.svg#users"], [1, "flex", "items-center", "gap-2", "mb-2"], ["type", "button", 1, "btn", "btn-sm", "btn-outline-secondary", "mt-1", 3, "click"], [1, "text-muted", "small", 2, "min-width", "1.5rem"], [1, "form-select", "form-select-sm", 2, "max-width", "160px", 3, "ngModelChange", "ngModel", "ngModelOptions"], [1, "form-select", "form-select-sm", 2, "max-width", "200px", 3, "ngModelChange", "ngModel", "ngModelOptions"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-danger", 3, "click"], [1, "mb-0", "ps-4"], [1, "sa-icon", "text-muted", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#credit-card"], [1, "row", "g-3"], [1, "col-6", "col-md-3"], [1, "text-muted", "small"], [1, "fw-500"], [1, "flex", "items-center", "gap-1"], [1, "sa-icon", 2, "color", "var(--green)", "stroke", "currentColor", "width", "16px", "height", "16px"], [1, "fw-500", 2, "color", "var(--green)"], ["type", "submit", 1, "btn", "btn-outline-secondary", 3, "disabled"], ["type", "button", 1, "btn", "btn-primary", 3, "click", "disabled"], ["routerLink", "/admin/travel-payment-requests", 1, "btn", "btn-outline-secondary"], [1, "modal-header", "border-0", "pb-0"], ["type", "button", 1, "btn-close", 3, "click"], [1, "modal-body", "text-center", "py-6"], [1, "sa-icon", "sa-icon-3x", "text-success", "mb-4", 2, "stroke", "currentColor"], [1, "fw-600", "mb-2"], [1, "text-secondary", "mb-0"], [1, "modal-footer", "border-0", "justify-center", "pt-0"], ["type", "button", 1, "btn", "btn-primary", "px-6", 3, "click"]], template: function TravelPaymentForm_Template(rf, ctx) {
    if (rf & 1) {
      const _r1 = \u0275\u0275getCurrentView();
      \u0275\u0275elementStart(0, "div", 1)(1, "div", 2)(2, "a", 3);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 4);
      \u0275\u0275element(4, "use", 5);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 6);
      \u0275\u0275text(6);
      \u0275\u0275elementEnd()();
      \u0275\u0275conditionalCreate(7, TravelPaymentForm_Conditional_7_Template, 4, 1, "div", 7);
      \u0275\u0275conditionalCreate(8, TravelPaymentForm_Conditional_8_Template, 5, 0, "div", 8)(9, TravelPaymentForm_Conditional_9_Template, 5, 0, "div", 8)(10, TravelPaymentForm_Conditional_10_Template, 5, 0, "div", 8)(11, TravelPaymentForm_Conditional_11_Template, 5, 0, "div", 8);
      \u0275\u0275elementStart(12, "form", 9);
      \u0275\u0275listener("ngSubmit", function TravelPaymentForm_Template_form_ngSubmit_12_listener() {
        \u0275\u0275restoreView(_r1);
        return \u0275\u0275resetView(ctx.save());
      });
      \u0275\u0275elementStart(13, "div", 10)(14, "div", 11)(15, "div", 12)(16, "div", 13);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(17, "svg", 14);
      \u0275\u0275element(18, "use", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275text(19, " \u51FA\u5DEE\u8CC7\u8A0A ");
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(20, "div", 16)(21, "div", 17)(22, "div", 18)(23, "label", 19);
      \u0275\u0275text(24, "\u51FA\u5DEE\u5730\u9EDE ");
      \u0275\u0275elementStart(25, "span", 20);
      \u0275\u0275text(26, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(27, "input", 21);
      \u0275\u0275conditionalCreate(28, TravelPaymentForm_Conditional_28_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(29, "div", 18)(30, "label", 19);
      \u0275\u0275text(31, "\u95DC\u806F\u5C08\u6848\uFF08\u9078\u586B\uFF09");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(32, "select", 23)(33, "option", 24);
      \u0275\u0275text(34);
      \u0275\u0275elementEnd();
      \u0275\u0275repeaterCreate(35, TravelPaymentForm_For_36_Template, 2, 4, "option", 24, _forTrack0);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(37, TravelPaymentForm_Conditional_37_Template, 2, 0, "div", 25);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(38, "div", 17)(39, "div", 18)(40, "label", 19);
      \u0275\u0275text(41, "\u51FA\u767C\u65E5\u671F ");
      \u0275\u0275elementStart(42, "span", 20);
      \u0275\u0275text(43, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(44, "input", 26);
      \u0275\u0275conditionalCreate(45, TravelPaymentForm_Conditional_45_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(46, "div", 18)(47, "label", 19);
      \u0275\u0275text(48, "\u8FD4\u56DE\u65E5\u671F ");
      \u0275\u0275elementStart(49, "span", 20);
      \u0275\u0275text(50, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(51, "input", 27);
      \u0275\u0275conditionalCreate(52, TravelPaymentForm_Conditional_52_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(53, "div", 28)(54, "label", 19);
      \u0275\u0275text(55, "\u51FA\u5DEE\u76EE\u7684 ");
      \u0275\u0275elementStart(56, "span", 20);
      \u0275\u0275text(57, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(58, "textarea", 29);
      \u0275\u0275conditionalCreate(59, TravelPaymentForm_Conditional_59_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(60, TravelPaymentForm_Conditional_60_Template, 6, 3, "div", 28);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(61, "div", 30)(62, "div", 31)(63, "div", 32);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(64, "svg", 14);
      \u0275\u0275element(65, "use", 33);
      \u0275\u0275elementEnd();
      \u0275\u0275text(66, " \u8CBB\u7528\u660E\u7D30 ");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(67, TravelPaymentForm_Conditional_67_Template, 4, 0, "button", 34);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(68, "div", 35)(69, "div", 36)(70, "table", 37)(71, "thead", 38)(72, "tr")(73, "th", 39);
      \u0275\u0275text(74, "\u5206\u985E");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(75, "th", 40);
      \u0275\u0275text(76, "\u9805\u6B21");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(77, "th", 41);
      \u0275\u0275text(78, "\u9805\u76EE\u8AAA\u660E");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(79, "th", 39);
      \u0275\u0275text(80, "\u55AE\u50F9");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(81, "th", 42);
      \u0275\u0275text(82, "\u6578\u91CF/\u55AE\u4F4D");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(83, "th", 39);
      \u0275\u0275text(84, "\u7E3D\u50F9");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(85, "th", 39);
      \u0275\u0275text(86, "\u5099\u8A3B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(87, "th", 43);
      \u0275\u0275text(88, "\u767C\u7968\u865F\u78BC");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(89, "th", 43);
      \u0275\u0275text(90, "\u767C\u7968\u65E5\u671F");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(91, TravelPaymentForm_Conditional_91_Template, 1, 0, "th", 44);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(92, "tbody", 45);
      \u0275\u0275repeaterCreate(93, TravelPaymentForm_For_94_Template, 29, 11, "tr", 46, \u0275\u0275repeaterTrackByIndex, false, TravelPaymentForm_ForEmpty_95_Template, 3, 1, "tr");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(96, TravelPaymentForm_Conditional_96_Template, 9, 5, "tfoot");
      \u0275\u0275elementEnd()()()();
      \u0275\u0275conditionalCreate(97, TravelPaymentForm_Conditional_97_Template, 15, 0, "div", 30)(98, TravelPaymentForm_Conditional_98_Template, 12, 0, "div", 30);
      \u0275\u0275conditionalCreate(99, TravelPaymentForm_Conditional_99_Template, 10, 2, "div", 30);
      \u0275\u0275element(100, "app-approval-timeline", 47);
      \u0275\u0275conditionalCreate(101, TravelPaymentForm_Conditional_101_Template, 7, 3, "div", 48)(102, TravelPaymentForm_Conditional_102_Template, 3, 0, "div", 49);
      \u0275\u0275elementEnd()()()();
      \u0275\u0275template(103, TravelPaymentForm_ng_template_103_Template, 12, 0, "ng-template", null, 0, \u0275\u0275templateRefExtractor);
    }
    if (rf & 2) {
      let tmp_5_0;
      let tmp_10_0;
      let tmp_11_0;
      let tmp_12_0;
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? ctx.isReadOnly ? "\u6AA2\u8996\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB" : ctx.isReturned ? "\u4FEE\u6539\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB" : "\u7DE8\u8F2F\u51FA\u5DEE\u8ACB\u6B3E\u8349\u7A3F" : "\u65B0\u589E\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB");
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.errorMsg() ? 7 : -1);
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.isReadOnly && ctx.approvalStatus === "pending" ? 8 : ctx.approvalStatus === "returned" ? 9 : ctx.isReadOnly && ctx.approvalStatus === "approved" ? 10 : ctx.isReadOnly && ctx.approvalStatus === "rejected" ? 11 : -1);
      \u0275\u0275advance(4);
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(16);
      \u0275\u0275conditional(((tmp_5_0 = ctx.form.get("destination")) == null ? null : tmp_5_0.invalid) && ((tmp_5_0 = ctx.form.get("destination")) == null ? null : tmp_5_0.touched) ? 28 : -1);
      \u0275\u0275advance(5);
      \u0275\u0275property("ngValue", null);
      \u0275\u0275advance();
      \u0275\u0275textInterpolate(ctx.loadingProjects ? "\u8F09\u5165\u4E2D\u2026" : "\u2014 \u4E0D\u95DC\u806F\u5C08\u6848 \u2014");
      \u0275\u0275advance();
      \u0275\u0275repeater(ctx.projects);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(!ctx.loadingProjects && ctx.projects.length === 0 ? 37 : -1);
      \u0275\u0275advance(8);
      \u0275\u0275conditional(((tmp_10_0 = ctx.form.get("startDate")) == null ? null : tmp_10_0.invalid) && ((tmp_10_0 = ctx.form.get("startDate")) == null ? null : tmp_10_0.touched) ? 45 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_11_0 = ctx.form.get("endDate")) == null ? null : tmp_11_0.invalid) && ((tmp_11_0 = ctx.form.get("endDate")) == null ? null : tmp_11_0.touched) ? 52 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_12_0 = ctx.form.get("purpose")) == null ? null : tmp_12_0.invalid) && ((tmp_12_0 = ctx.form.get("purpose")) == null ? null : tmp_12_0.touched) ? 59 : -1);
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.isEdit ? 60 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(!ctx.isReadOnly ? 67 : -1);
      \u0275\u0275advance(24);
      \u0275\u0275conditional(!ctx.isReadOnly ? 91 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275repeater(ctx.itemControls);
      \u0275\u0275advance(3);
      \u0275\u0275conditional(ctx.itemControls.length > 0 ? 96 : -1);
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.hasDesignatedStep && !ctx.isReadOnly ? 97 : ctx.isReadOnly && ctx.designatedEntries.length > 0 ? 98 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(ctx.existingRequest && (ctx.existingRequest.estimatedPaymentDate || ctx.existingRequest.paidAt) ? 99 : -1);
      \u0275\u0275advance();
      \u0275\u0275property("flow", ctx.approvalFlow)("approvalRecords", ctx.approvalRecords)("currentStepOrder", ctx.taskCurrentStepOrder)("status", ctx.taskStatus);
      \u0275\u0275advance();
      \u0275\u0275conditional(!ctx.isReadOnly ? 101 : 102);
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, NumberValueAccessor, SelectControlValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, FormGroupDirective, FormControlName, FormGroupName, FormArrayName, FormsModule, NgModel, RouterLink, ApprovalTimeline, DecimalPipe, DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(TravelPaymentForm, [{
    type: Component,
    args: [{ selector: "app-travel-payment-form", imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, DatePipe, ApprovalTimeline], template: `<div class="container-fluid py-3">
  <div class="flex items-center gap-2 mb-6">
    <a routerLink="/admin/travel-payment-requests" class="btn btn-sm btn-outline-secondary">
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
    </a>
    <h4 class="mb-0">{{ isEdit ? (isReadOnly ? '\u6AA2\u8996\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB' : (isReturned ? '\u4FEE\u6539\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB' : '\u7DE8\u8F2F\u51FA\u5DEE\u8ACB\u6B3E\u8349\u7A3F')) : '\u65B0\u589E\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB' }}</h4>
  </div>

  @if (errorMsg()) {
    <div class="alert alert-danger flex items-center gap-2 mb-6 py-2" role="alert">
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>
      {{ errorMsg() }}
    </div>
  }

  @if (isReadOnly && approvalStatus === 'pending') {
    <div class="card border-0 shadow-sm mb-6">
      <div class="card-header bg-[rgba(13,110,253,0.08)] border-bottom flex items-center gap-2 fw-600 text-primary py-3">
        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#clock"></use></svg>
        \u6B64\u7533\u8ACB\u5BE9\u6838\u4E2D\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002
      </div>
    </div>
  } @else if (approvalStatus === 'returned') {
    <div class="card border-0 shadow-sm mb-6">
      <div class="card-header bg-[rgba(255,193,7,0.08)] border-bottom flex items-center gap-2 fw-600 text-warning py-3">
        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>
        \u6B64\u7533\u8ACB\u5DF2\u88AB\u9000\u56DE\uFF0C\u8ACB\u4FEE\u6539\u5F8C\u91CD\u65B0\u9001\u51FA\u3002
      </div>
    </div>
  } @else if (isReadOnly && approvalStatus === 'approved') {
    <div class="card border-0 shadow-sm mb-6">
      <div class="card-header bg-[rgba(37,162,68,0.08)] border-bottom flex items-center gap-2 fw-600 text-success py-3">
        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#check-circle"></use></svg>
        \u6B64\u7533\u8ACB\u5DF2\u6838\u51C6\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002
      </div>
    </div>
  } @else if (isReadOnly && approvalStatus === 'rejected') {
    <div class="card border-0 shadow-sm mb-6">
      <div class="card-header bg-[rgba(220,53,69,0.08)] border-bottom flex items-center gap-2 fw-600 text-danger py-3">
        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#x-circle"></use></svg>
        \u6B64\u7533\u8ACB\u5DF2\u88AB\u62D2\u7D55\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002
      </div>
    </div>
  }

  <form [formGroup]="form" (ngSubmit)="save()">
    <div class="row g-4">
      <div class="col-12 col-xl-10">

        <!-- \u51FA\u5DEE\u8CC7\u8A0A -->
        <div class="card border-0 shadow-sm">
          <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
            <svg class="sa-icon text-primary" style="stroke: currentColor">
              <use href="/assets/icons/sprite.svg#map-pin"></use>
            </svg>
            \u51FA\u5DEE\u8CC7\u8A0A
          </div>
          <div class="card-body">

            <div class="row g-3 mb-4">
              <div class="col-12 col-md-6">
                <label class="form-label fw-500">\u51FA\u5DEE\u5730\u9EDE <span class="text-danger">*</span></label>
                <input type="text" class="form-control" formControlName="destination" placeholder="\u4F8B\u5982\uFF1A\u53F0\u5357\u3001\u53F0\u4E2D">
                @if (form.get('destination')?.invalid && form.get('destination')?.touched) {
                  <div class="text-danger small mt-1">\u8ACB\u586B\u5BEB\u51FA\u5DEE\u5730\u9EDE\u3002</div>
                }
              </div>
              <div class="col-12 col-md-6">
                <label class="form-label fw-500">\u95DC\u806F\u5C08\u6848\uFF08\u9078\u586B\uFF09</label>
                <select class="form-select" formControlName="projectId">
                  <option [ngValue]="null">{{ loadingProjects ? '\u8F09\u5165\u4E2D\u2026' : '\u2014 \u4E0D\u95DC\u806F\u5C08\u6848 \u2014' }}</option>
                  @for (p of projects; track p.id) {
                    <option [ngValue]="p.id">{{ p.code }} - {{ p.name }}{{ p.departmentName ? '\uFF08' + p.departmentName + '\uFF09' : '' }}</option>
                  }
                </select>
                @if (!loadingProjects && projects.length === 0) {
                  <div class="text-muted small mt-1">\u60A8\u76EE\u524D\u53EF\u7533\u8ACB\u7684\u5C08\u6848\u6E05\u55AE\u70BA\u7A7A\uFF0C\u8ACB\u806F\u7D61\u4E3B\u7BA1\u6216\u78BA\u8A8D\u90E8\u9580\u8A2D\u5B9A\u3002</div>
                }
              </div>
            </div>

            <div class="row g-3 mb-4">
              <div class="col-12 col-md-6">
                <label class="form-label fw-500">\u51FA\u767C\u65E5\u671F <span class="text-danger">*</span></label>
                <input type="date" class="form-control" formControlName="startDate">
                @if (form.get('startDate')?.invalid && form.get('startDate')?.touched) {
                  <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u51FA\u767C\u65E5\u671F\u3002</div>
                }
              </div>
              <div class="col-12 col-md-6">
                <label class="form-label fw-500">\u8FD4\u56DE\u65E5\u671F <span class="text-danger">*</span></label>
                <input type="date" class="form-control" formControlName="endDate">
                @if (form.get('endDate')?.invalid && form.get('endDate')?.touched) {
                  <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u8FD4\u56DE\u65E5\u671F\u3002</div>
                }
              </div>
            </div>

            <div class="mb-4">
              <label class="form-label fw-500">\u51FA\u5DEE\u76EE\u7684 <span class="text-danger">*</span></label>
              <textarea class="form-control" formControlName="purpose" rows="3"
                        placeholder="\u8ACB\u586B\u5BEB\u51FA\u5DEE\u76EE\u7684..."></textarea>
              @if (form.get('purpose')?.invalid && form.get('purpose')?.touched) {
                <div class="text-danger small mt-1">\u8ACB\u586B\u5BEB\u51FA\u5DEE\u76EE\u7684\u3002</div>
              }
            </div>

            @if (isEdit) {
              <div class="mb-4">
                <label class="form-label fw-500">\u7C3D\u6838\u72C0\u614B</label>
                <div>
                  <span class="badge rounded-pill px-3 py-2" [class]="statusClass[approvalStatus]">
                    {{ statusLabel[approvalStatus] }}
                  </span>
                </div>
              </div>
            }

          </div>
        </div>

        <!-- \u8CBB\u7528\u660E\u7D30\uFF08\u542B\u767C\u7968\u6B04\u4F4D\uFF09 -->
        <div class="card border-0 shadow-sm mt-6">
          <div class="card-header bg-transparent border-bottom flex items-center justify-between">
            <div class="flex items-center gap-2 fw-600">
              <svg class="sa-icon text-primary" style="stroke: currentColor">
                <use href="/assets/icons/sprite.svg#file-text"></use>
              </svg>
              \u8CBB\u7528\u660E\u7D30
            </div>
            @if (!isReadOnly) {
              <button type="button" class="btn btn-sm btn-outline-primary inline-flex items-center gap-1" (click)="addItem()">
                <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
                \u65B0\u589E\u9805\u76EE
              </button>
            }
          </div>
          <div class="card-body p-0">
            <div class="table-responsive">
              <table class="table table-sm mb-0">
                <thead class="table-light">
                  <tr>
                    <th style="min-width:100px">\u5206\u985E</th>
                    <th style="min-width:60px">\u9805\u6B21</th>
                    <th style="min-width:160px">\u9805\u76EE\u8AAA\u660E</th>
                    <th style="min-width:100px">\u55AE\u50F9</th>
                    <th style="min-width:80px">\u6578\u91CF/\u55AE\u4F4D</th>
                    <th style="min-width:100px">\u7E3D\u50F9</th>
                    <th style="min-width:100px">\u5099\u8A3B</th>
                    <th style="min-width:120px">\u767C\u7968\u865F\u78BC</th>
                    <th style="min-width:120px">\u767C\u7968\u65E5\u671F</th>
                    @if (!isReadOnly) {
                      <th style="width:48px"></th>
                    }
                  </tr>
                </thead>
                <tbody formArrayName="items">
                  @for (ctrl of itemControls; track $index; let i = $index) {
                    <tr [formGroupName]="i">
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small">{{ ctrl.get('category')?.value }}</span>
                        } @else {
                          <select class="form-select form-select-sm" formControlName="category">
                            <option value="">\u9078\u64C7</option>
                            @for (cat of categories; track cat) {
                              <option [value]="cat">{{ cat }}</option>
                            }
                          </select>
                        }
                      </td>
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small">{{ ctrl.get('seqNo')?.value }}</span>
                        } @else {
                          <input type="number" class="form-control form-control-sm" formControlName="seqNo" min="1" style="width:60px">
                        }
                      </td>
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small">{{ ctrl.get('itemName')?.value }}</span>
                        } @else {
                          <input class="form-control form-control-sm" formControlName="itemName" placeholder="\u9805\u76EE\u8AAA\u660E">
                        }
                      </td>
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small">{{ ctrl.get('unitPrice')?.value | number:'1.0-0' }}</span>
                        } @else {
                          <input type="number" class="form-control form-control-sm" formControlName="unitPrice" min="0"
                                 (input)="calcTotal(ctrl)">
                        }
                      </td>
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small">{{ ctrl.get('quantity')?.value }}</span>
                        } @else {
                          <input class="form-control form-control-sm" formControlName="quantity" placeholder="\u5982\uFF1A1\u5F0F"
                                 (input)="calcTotal(ctrl)">
                        }
                      </td>
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small fw-500">{{ ctrl.get('totalPrice')?.value | number:'1.0-0' }}</span>
                        } @else {
                          <input type="number" class="form-control form-control-sm" formControlName="totalPrice" min="0">
                        }
                      </td>
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small">{{ ctrl.get('note')?.value || '\u2014' }}</span>
                        } @else {
                          <input class="form-control form-control-sm" formControlName="note" placeholder="">
                        }
                      </td>
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small font-monospace">{{ ctrl.get('invoiceNo')?.value || '\u2014' }}</span>
                        } @else {
                          <input class="form-control form-control-sm font-monospace" formControlName="invoiceNo" placeholder="AB-12345678">
                        }
                      </td>
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small">{{ ctrl.get('invoiceDate')?.value || '\u2014' }}</span>
                        } @else {
                          <input type="date" class="form-control form-control-sm" formControlName="invoiceDate">
                        }
                      </td>
                      @if (!isReadOnly) {
                        <td class="text-right align-middle">
                          <button type="button" class="btn btn-sm btn-ghost-danger inline-flex items-center" (click)="removeItem(i)">
                            <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#x"></use></svg>
                          </button>
                        </td>
                      }
                    </tr>
                  } @empty {
                    <tr>
                      <td [attr.colspan]="isReadOnly ? 9 : 10" class="text-center text-muted py-4 small">\u5C1A\u672A\u65B0\u589E\u8CBB\u7528\u9805\u76EE</td>
                    </tr>
                  }
                </tbody>
                @if (itemControls.length > 0) {
                  <tfoot>
                    <tr class="table-light">
                      <td colspan="5" class="text-right fw-500 small">\u5408\u8A08</td>
                      <td class="fw-600">{{ grandTotal | number:'1.0-0' }}</td>
                      <td colspan="3"></td>
                      @if (!isReadOnly) { <td></td> }
                    </tr>
                  </tfoot>
                }
              </table>
            </div>
          </div>
        </div>

        <!-- \u6307\u5B9A\u5BE9\u6838\u8005 -->
        @if (hasDesignatedStep && !isReadOnly) {
          <div class="card border-0 shadow-sm mt-6">
            <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
              <svg class="sa-icon text-primary" style="stroke: currentColor">
                <use href="/assets/icons/sprite.svg#users"></use>
              </svg>
              \u6307\u5B9A\u5BE9\u6838\u8005
            </div>
            <div class="card-body">
              <div class="mb-0">
                <label class="form-label fw-500">\u6307\u5B9A\u5BE9\u6838\u8005\uFF08\u4F9D\u5E8F\u5BE9\u6838\uFF09</label>
                @for (entry of designatedEntries; track $index; let i = $index) {
                  <div class="flex items-center gap-2 mb-2">
                    <span class="text-muted small" style="min-width:1.5rem">{{ i + 1 }}.</span>
                    <select class="form-select form-select-sm" style="max-width:160px"
                            [(ngModel)]="entry.selectedJobTitleId" [ngModelOptions]="{standalone: true}"
                            (ngModelChange)="onEntryJobTitleChange(i)">
                      <option [ngValue]="null">\u2014 \u8077\u7A31 \u2014</option>
                      @for (jt of jobTitles; track jt.id) {
                        <option [ngValue]="jt.id">{{ jt.name }}</option>
                      }
                    </select>
                    <select class="form-select form-select-sm" style="max-width:200px"
                            [(ngModel)]="entry.selectedUserId" [ngModelOptions]="{standalone: true}">
                      <option [ngValue]="null">\u2014 \u4EBA\u54E1 \u2014</option>
                      @for (user of entry.filteredUsers; track user.id) {
                        <option [ngValue]="user.id">{{ user.name }}</option>
                      }
                    </select>
                    <button type="button" class="btn btn-sm btn-ghost-danger" (click)="removeDesignatedEntry(i)">
                      <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#x"></use></svg>
                    </button>
                  </div>
                }
                <button type="button" class="btn btn-sm btn-outline-secondary mt-1" (click)="addDesignatedEntry()">
                  <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
                  \u65B0\u589E\u5BE9\u6838\u4EBA
                </button>
              </div>
            </div>
          </div>
        } @else if (isReadOnly && designatedEntries.length > 0) {
          <div class="card border-0 shadow-sm mt-6">
            <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
              <svg class="sa-icon text-primary" style="stroke: currentColor">
                <use href="/assets/icons/sprite.svg#users"></use>
              </svg>
              \u6307\u5B9A\u5BE9\u6838\u8005
            </div>
            <div class="card-body">
              <div class="mb-0">
                <label class="form-label fw-500">\u6307\u5B9A\u5BE9\u6838\u8005\uFF08\u4F9D\u5E8F\u5BE9\u6838\uFF09</label>
                <ol class="mb-0 ps-4">
                  @for (entry of designatedEntries; track $index) {
                    <li class="small">{{ getUserName(entry.selectedUserId) }}</li>
                  }
                </ol>
              </div>
            </div>
          </div>
        }

        <!-- \u64A5\u6B3E\u8CC7\u8A0A\uFF08\u5DF2\u6838\u51C6\u6642\u986F\u793A\uFF09 -->
        @if (existingRequest && (existingRequest.estimatedPaymentDate || existingRequest.paidAt)) {
          <div class="card border-0 shadow-sm mt-6">
            <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
              <svg class="sa-icon text-muted" style="stroke: currentColor">
                <use href="/assets/icons/sprite.svg#credit-card"></use>
              </svg>
              \u64A5\u6B3E\u8CC7\u8A0A
            </div>
            <div class="card-body">
              <div class="row g-3">
                @if (existingRequest.estimatedPaymentDate) {
                  <div class="col-6 col-md-3">
                    <div class="text-muted small">\u9810\u8A08\u64A5\u6B3E\u65E5</div>
                    <div class="fw-500">{{ existingRequest.estimatedPaymentDate | date:'yyyy-MM-dd' }}</div>
                  </div>
                }
                @if (existingRequest.paidAt) {
                  <div class="col-6 col-md-3">
                    <div class="text-muted small">\u64A5\u6B3E\u65E5</div>
                    <div class="flex items-center gap-1">
                      <svg class="sa-icon" style="color: var(--green); stroke: currentColor; width: 16px; height: 16px">
                        <use href="/assets/icons/sprite.svg#check-circle"></use>
                      </svg>
                      <span class="fw-500" style="color: var(--green)">{{ existingRequest.paidAt | date:'yyyy-MM-dd' }}</span>
                    </div>
                  </div>
                } @else if (existingRequest.estimatedPaymentDate) {
                  <div class="col-6 col-md-3">
                    <div class="text-muted small">\u64A5\u6B3E\u65E5</div>
                    <span class="text-muted small">\u5C1A\u672A\u64A5\u6B3E</span>
                  </div>
                }
              </div>
            </div>
          </div>
        }

        <!-- \u7C3D\u6838\u6D41\u7A0B\u6642\u9593\u8EF8 -->
        <app-approval-timeline
          [flow]="approvalFlow"
          [approvalRecords]="approvalRecords"
          [currentStepOrder]="taskCurrentStepOrder"
          [status]="taskStatus" />

        @if (!isReadOnly) {
          <div class="mt-6 flex gap-2">
            <button type="submit" class="btn btn-outline-secondary" [disabled]="form.invalid || itemArray.length === 0">
              {{ isEdit ? '\u5132\u5B58' : '\u5132\u5B58\u8349\u7A3F' }}
            </button>
            <button type="button" class="btn btn-primary" [disabled]="form.invalid || itemArray.length === 0"
                    (click)="submitForApproval()">
              \u9001\u51FA\u7533\u8ACB
            </button>
            <a routerLink="/admin/travel-payment-requests" class="btn btn-outline-secondary">\u53D6\u6D88</a>
          </div>
        } @else {
          <div class="mt-6">
            <a routerLink="/admin/travel-payment-requests" class="btn btn-outline-secondary">\u8FD4\u56DE\u5217\u8868</a>
          </div>
        }

      </div>
    </div>
  </form>
</div>

<ng-template #successModal let-modal>
  <div class="modal-header border-0 pb-0">
    <button type="button" class="btn-close" (click)="modal.close()"></button>
  </div>
  <div class="modal-body text-center py-6">
    <svg class="sa-icon sa-icon-3x text-success mb-4" style="stroke: currentColor">
      <use href="/assets/icons/sprite.svg#check-circle"></use>
    </svg>
    <h5 class="fw-600 mb-2">\u7533\u8ACB\u6210\u529F</h5>
    <p class="text-secondary mb-0">\u8ACB\u76E1\u65E9\u5C07\u6B63\u672C\u8CC7\u6599\u9001\u56DE\u7BA1\u7406\u8655</p>
  </div>
  <div class="modal-footer border-0 justify-center pt-0">
    <button type="button" class="btn btn-primary px-6" (click)="modal.close()">\u78BA\u5B9A</button>
  </div>
</ng-template>
` }]
  }], null, { successModal: [{ type: ViewChild, args: ["successModal", { isSignal: true }] }] });
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(TravelPaymentForm, { className: "TravelPaymentForm", filePath: "src/app/features/admin/travel-payment-requests/pages/travel-payment-form/travel-payment-form.ts", lineNumber: 25 });
})();
export {
  TravelPaymentForm
};
//# sourceMappingURL=chunk-5P5SMTBM.js.map
