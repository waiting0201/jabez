import {
  ApprovalService,
  JobTitleService,
  NgbModal,
  PaymentRequestService,
  ProjectService,
  UserService,
  require_heic2any
} from "./chunk-YMVGBRT2.js";
import {
  ApprovalTimeline,
  FilePreviewModal
} from "./chunk-AQCL77US.js";
import "./chunk-RJOQCPVG.js";
import {
  ApprovalTaskService
} from "./chunk-E72ZCXMI.js";
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
} from "./chunk-GY4FAHXD.js";
import {
  APPROVAL_STATUS_CLASSES,
  APPROVAL_STATUS_LABELS,
  HolidayTravelRequestService,
  ITEM_CATEGORIES
} from "./chunk-KT3QJDIT.js";
import {
  ActivatedRoute,
  Router,
  RouterLink
} from "./chunk-UAVMLPEF.js";
import {
  DomSanitizer
} from "./chunk-K2EJQVOR.js";
import {
  ChangeDetectorRef,
  Component,
  DecimalPipe,
  ViewChild,
  firstValueFrom,
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
  ɵɵdeclareLet,
  ɵɵdefineComponent,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵgetCurrentView,
  ɵɵinterpolate,
  ɵɵlistener,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind2,
  ɵɵproperty,
  ɵɵpureFunction0,
  ɵɵqueryAdvance,
  ɵɵreadContextLet,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵrepeaterTrackByIdentity,
  ɵɵrepeaterTrackByIndex,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵstoreLet,
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
} from "./chunk-FX7BMVKQ.js";
import {
  __toESM
} from "./chunk-KWSTWQNB.js";

// src/app/features/admin/holiday-travel-requests/pages/holiday-travel-request-form/holiday-travel-request-form.ts
var import_heic2any = __toESM(require_heic2any());
var _c0 = ["successModal"];
var _c1 = () => ({ standalone: true });
var _forTrack0 = ($index, $item) => $item.id;
function _forTrack1($index, $item) {
  let tmp_0_0;
  return (tmp_0_0 = $item.get("id")) == null ? null : tmp_0_0.value;
}
function HolidayTravelRequestForm_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 7);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 60);
    \u0275\u0275element(2, "use", 61);
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
function HolidayTravelRequestForm_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 62);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 60);
    \u0275\u0275element(3, "use", 63);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5BE9\u6838\u4E2D\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function HolidayTravelRequestForm_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 64);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 60);
    \u0275\u0275element(3, "use", 61);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5DF2\u88AB\u9000\u56DE\uFF0C\u8ACB\u4FEE\u6539\u5F8C\u91CD\u65B0\u9001\u51FA\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function HolidayTravelRequestForm_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 65);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 60);
    \u0275\u0275element(3, "use", 66);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5DF2\u6838\u51C6\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function HolidayTravelRequestForm_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 67);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 60);
    \u0275\u0275element(3, "use", 68);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5DF2\u88AB\u62D2\u7D55\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function HolidayTravelRequestForm_Conditional_28_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u586B\u5BEB\u57F7\u884C\u6D3B\u52D5\u5730\u9EDE\u3002");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_For_36_Template(rf, ctx) {
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
function HolidayTravelRequestForm_Conditional_44_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u958B\u59CB\u65E5\u671F\u3002");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_51_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u7D50\u675F\u65E5\u671F\u3002");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_56_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 69);
    \u0275\u0275text(1, " \u67E5\u8A62\u4E2D\u2026 ");
  }
}
function HolidayTravelRequestForm_Conditional_57_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 29);
    \u0275\u0275text(1, "\u884C\u4E8B\u66C6\u8CC7\u6599\u5C1A\u672A\u532F\u5165");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_58_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 30);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("", ctx_r1.holidayDays(), " \u5929");
  }
}
function HolidayTravelRequestForm_Conditional_59_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span");
    \u0275\u0275text(1, "\u2014 \u8ACB\u5148\u9078\u64C7\u65E5\u671F");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_68_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u586B\u5BEB\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9\u3002");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_69_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 6)(1, "label", 19);
    \u0275\u0275text(2, "\u7C3D\u6838\u72C0\u614B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div")(4, "span", 70);
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
function HolidayTravelRequestForm_Conditional_76_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 71);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_76_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.addParticipant());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 60);
    \u0275\u0275element(2, "use", 72);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u65B0\u589E\u4EBA\u54E1 ");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_78_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u7121\u53C3\u8207\u57F7\u884C\u4EBA\u54E1\u8A18\u9304\u3002");
  }
}
function HolidayTravelRequestForm_Conditional_78_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u9EDE\u64CA\u300C\u65B0\u589E\u4EBA\u54E1\u300D\u65B0\u589E\u53C3\u8207\u57F7\u884C\u4EBA\u54E1\u3002");
  }
}
function HolidayTravelRequestForm_Conditional_78_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 39);
    \u0275\u0275conditionalCreate(1, HolidayTravelRequestForm_Conditional_78_Conditional_1_Template, 1, 0)(2, HolidayTravelRequestForm_Conditional_78_Conditional_2_Template, 1, 0);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 1 : 2);
  }
}
function HolidayTravelRequestForm_Conditional_79_For_1_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 75);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const entry_r5 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.getUserName(entry_r5.selectedUserId));
  }
}
function HolidayTravelRequestForm_Conditional_79_For_1_Conditional_4_For_4_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 24);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const user_r7 = ctx.$implicit;
    \u0275\u0275property("ngValue", user_r7.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(user_r7.name);
  }
}
function HolidayTravelRequestForm_Conditional_79_For_1_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    const _r6 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "select", 76);
    \u0275\u0275twoWayListener("ngModelChange", function HolidayTravelRequestForm_Conditional_79_For_1_Conditional_4_Template_select_ngModelChange_0_listener($event) {
      \u0275\u0275restoreView(_r6);
      const entry_r5 = \u0275\u0275nextContext().$implicit;
      \u0275\u0275twoWayBindingSet(entry_r5.selectedUserId, $event) || (entry_r5.selectedUserId = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementStart(1, "option", 24);
    \u0275\u0275text(2, "\u2014 \u9078\u64C7\u4EBA\u54E1 \u2014");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(3, HolidayTravelRequestForm_Conditional_79_For_1_Conditional_4_For_4_Template, 2, 2, "option", 24, _forTrack0);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "button", 77);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_79_For_1_Conditional_4_Template_button_click_5_listener() {
      \u0275\u0275restoreView(_r6);
      const \u0275$index_219_r8 = \u0275\u0275nextContext().$index;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.removeParticipant(\u0275$index_219_r8));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(6, "svg", 60);
    \u0275\u0275element(7, "use", 78);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const entry_r5 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275twoWayProperty("ngModel", entry_r5.selectedUserId);
    \u0275\u0275property("ngModelOptions", \u0275\u0275pureFunction0(3, _c1));
    \u0275\u0275advance();
    \u0275\u0275property("ngValue", null);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(ctx_r1.allUsers);
  }
}
function HolidayTravelRequestForm_Conditional_79_For_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 73)(1, "span", 74);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(3, HolidayTravelRequestForm_Conditional_79_For_1_Conditional_3_Template, 2, 1, "span", 75)(4, HolidayTravelRequestForm_Conditional_79_For_1_Conditional_4_Template, 8, 4);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const \u0275$index_219_r8 = ctx.$index;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1("", \u0275$index_219_r8 + 1, ".");
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 3 : 4);
  }
}
function HolidayTravelRequestForm_Conditional_79_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275repeaterCreate(0, HolidayTravelRequestForm_Conditional_79_For_1_Template, 5, 2, "div", 73, \u0275\u0275repeaterTrackByIndex);
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275repeater(ctx_r1.participantEntries);
  }
}
function HolidayTravelRequestForm_Conditional_80_Template(rf, ctx) {
  if (rf & 1) {
    const _r9 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 34)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 79);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u4E0A\u50B3\u767C\u7968 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "label", 80);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(7, "svg", 81);
    \u0275\u0275element(8, "use", 79);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(9, "span", 82);
    \u0275\u0275text(10, "\u9EDE\u64CA\u4E0A\u50B3\u767C\u7968\u5716\u6A94");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(11, "span", 31);
    \u0275\u0275text(12, "\u652F\u63F4 JPG\u3001PNG\u3001HEIC\u3001PDF\uFF0C\u53EF\u591A\u9078\u3002\u4E0A\u50B3\u5F8C\u81EA\u52D5\u65B0\u589E\u8CBB\u7528\u660E\u7D30\u884C\u4E26 OCR \u8B58\u5225");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "input", 83);
    \u0275\u0275listener("change", function HolidayTravelRequestForm_Conditional_80_Template_input_change_13_listener($event) {
      \u0275\u0275restoreView(_r9);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.onFilesSelected($event));
    });
    \u0275\u0275elementEnd()()()();
  }
}
function HolidayTravelRequestForm_Conditional_87_Template(rf, ctx) {
  if (rf & 1) {
    const _r10 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 71);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_87_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r10);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.addItem());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 60);
    \u0275\u0275element(2, "use", 72);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u624B\u52D5\u65B0\u589E\u884C ");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_110_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "th", 51);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    const _r11 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 101);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_For_113_Conditional_3_Template_button_click_0_listener() {
      let tmp_15_0;
      \u0275\u0275restoreView(_r11);
      const ctrl_r12 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.openPreview((tmp_15_0 = ctrl_r12.get("fileName")) == null ? null : tmp_15_0.value, ((tmp_15_0 = ctrl_r12.get("previewUrl")) == null ? null : tmp_15_0.value) || ((tmp_15_0 = ctrl_r12.get("fileUrl")) == null ? null : tmp_15_0.value)));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 60);
    \u0275\u0275element(2, "use", 40);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    let tmp_14_0;
    const ctrl_r12 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("title", \u0275\u0275interpolate((tmp_14_0 = ctrl_r12.get("fileName")) == null ? null : tmp_14_0.value));
  }
}
function HolidayTravelRequestForm_For_113_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 86);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 88);
    \u0275\u0275element(1, "span", 86);
    \u0275\u0275text(2, " \u8B58\u5225\u4E2D\u2026 ");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_For_113_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 89);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_14_0;
    const ctrl_r12 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(((tmp_14_0 = ctrl_r12.get("invoiceNo")) == null ? null : tmp_14_0.value) || "\u2014");
  }
}
function HolidayTravelRequestForm_For_113_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 90);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 88);
    \u0275\u0275element(1, "span", 86);
    \u0275\u0275text(2, " \u8B58\u5225\u4E2D\u2026 ");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_For_113_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 91);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_14_0;
    const ctrl_r12 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(((tmp_14_0 = ctrl_r12.get("invoiceDate")) == null ? null : tmp_14_0.value) || "\u2014");
  }
}
function HolidayTravelRequestForm_For_113_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 92);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_14_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 91);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_14_0;
    const ctrl_r12 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(((tmp_14_0 = ctrl_r12.get("category")) == null ? null : tmp_14_0.value) || "\u2014");
  }
}
function HolidayTravelRequestForm_For_113_Conditional_15_For_4_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 103);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const cat_r13 = ctx.$implicit;
    \u0275\u0275property("value", cat_r13);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(cat_r13);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_15_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "select", 93)(1, "option", 102);
    \u0275\u0275text(2, "\u9078\u64C7");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(3, HolidayTravelRequestForm_For_113_Conditional_15_For_4_Template, 2, 2, "option", 103, \u0275\u0275repeaterTrackByIdentity);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(3);
    \u0275\u0275repeater(ctx_r1.categories);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_17_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 91);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_14_0;
    const ctrl_r12 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate((tmp_14_0 = ctrl_r12.get("itemName")) == null ? null : tmp_14_0.value);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 94);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_20_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 95);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_For_113_Conditional_21_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 91);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "number");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_14_0;
    const ctrl_r12 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(2, 1, (tmp_14_0 = ctrl_r12.get("unitPrice")) == null ? null : tmp_14_0.value, "1.0-0"));
  }
}
function HolidayTravelRequestForm_For_113_Conditional_22_Template(rf, ctx) {
  if (rf & 1) {
    const _r14 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "input", 104);
    \u0275\u0275listener("input", function HolidayTravelRequestForm_For_113_Conditional_22_Template_input_input_0_listener() {
      \u0275\u0275restoreView(_r14);
      const ctrl_r12 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.calcTotal(ctrl_r12));
    });
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_For_113_Conditional_24_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 95);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_For_113_Conditional_25_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 91);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_14_0;
    const ctrl_r12 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate((tmp_14_0 = ctrl_r12.get("quantity")) == null ? null : tmp_14_0.value);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_26_Template(rf, ctx) {
  if (rf & 1) {
    const _r15 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "input", 105);
    \u0275\u0275listener("input", function HolidayTravelRequestForm_For_113_Conditional_26_Template_input_input_0_listener() {
      \u0275\u0275restoreView(_r15);
      const ctrl_r12 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.calcTotal(ctrl_r12));
    });
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_For_113_Conditional_28_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 95);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_For_113_Conditional_29_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 75);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "number");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_14_0;
    const ctrl_r12 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(2, 1, (tmp_14_0 = ctrl_r12.get("totalPrice")) == null ? null : tmp_14_0.value, "1.0-0"));
  }
}
function HolidayTravelRequestForm_For_113_Conditional_30_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 98);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_32_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 91);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_14_0;
    const ctrl_r12 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(((tmp_14_0 = ctrl_r12.get("note")) == null ? null : tmp_14_0.value) || "\u2014");
  }
}
function HolidayTravelRequestForm_For_113_Conditional_33_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "input", 99);
  }
}
function HolidayTravelRequestForm_For_113_Conditional_34_Template(rf, ctx) {
  if (rf & 1) {
    const _r16 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "td", 100)(1, "button", 106);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_For_113_Conditional_34_Template_button_click_1_listener() {
      \u0275\u0275restoreView(_r16);
      const \u0275$index_331_r17 = \u0275\u0275nextContext().$index;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.removeItem(\u0275$index_331_r17));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 60);
    \u0275\u0275element(3, "use", 78);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    \u0275\u0275nextContext();
    const isOcr_r18 = \u0275\u0275readContextLet(0);
    \u0275\u0275advance();
    \u0275\u0275property("disabled", isOcr_r18);
  }
}
function HolidayTravelRequestForm_For_113_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275declareLet(0);
    \u0275\u0275elementStart(1, "tr", 53)(2, "td", 84);
    \u0275\u0275conditionalCreate(3, HolidayTravelRequestForm_For_113_Conditional_3_Template, 3, 2, "button", 85)(4, HolidayTravelRequestForm_For_113_Conditional_4_Template, 1, 0, "span", 86);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "td", 87);
    \u0275\u0275conditionalCreate(6, HolidayTravelRequestForm_For_113_Conditional_6_Template, 3, 0, "div", 88)(7, HolidayTravelRequestForm_For_113_Conditional_7_Template, 2, 1, "span", 89)(8, HolidayTravelRequestForm_For_113_Conditional_8_Template, 1, 0, "input", 90);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 87);
    \u0275\u0275conditionalCreate(10, HolidayTravelRequestForm_For_113_Conditional_10_Template, 3, 0, "div", 88)(11, HolidayTravelRequestForm_For_113_Conditional_11_Template, 2, 1, "span", 91)(12, HolidayTravelRequestForm_For_113_Conditional_12_Template, 1, 0, "input", 92);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "td", 87);
    \u0275\u0275conditionalCreate(14, HolidayTravelRequestForm_For_113_Conditional_14_Template, 2, 1, "span", 91)(15, HolidayTravelRequestForm_For_113_Conditional_15_Template, 5, 0, "select", 93);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(16, "td", 87);
    \u0275\u0275conditionalCreate(17, HolidayTravelRequestForm_For_113_Conditional_17_Template, 2, 1, "span", 91)(18, HolidayTravelRequestForm_For_113_Conditional_18_Template, 1, 0, "input", 94);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "td", 87);
    \u0275\u0275conditionalCreate(20, HolidayTravelRequestForm_For_113_Conditional_20_Template, 2, 0, "div", 95)(21, HolidayTravelRequestForm_For_113_Conditional_21_Template, 3, 4, "span", 91)(22, HolidayTravelRequestForm_For_113_Conditional_22_Template, 1, 0, "input", 96);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(23, "td", 87);
    \u0275\u0275conditionalCreate(24, HolidayTravelRequestForm_For_113_Conditional_24_Template, 2, 0, "div", 95)(25, HolidayTravelRequestForm_For_113_Conditional_25_Template, 2, 1, "span", 91)(26, HolidayTravelRequestForm_For_113_Conditional_26_Template, 1, 0, "input", 97);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(27, "td", 87);
    \u0275\u0275conditionalCreate(28, HolidayTravelRequestForm_For_113_Conditional_28_Template, 2, 0, "div", 95)(29, HolidayTravelRequestForm_For_113_Conditional_29_Template, 3, 4, "span", 75)(30, HolidayTravelRequestForm_For_113_Conditional_30_Template, 1, 0, "input", 98);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(31, "td", 87);
    \u0275\u0275conditionalCreate(32, HolidayTravelRequestForm_For_113_Conditional_32_Template, 2, 1, "span", 91)(33, HolidayTravelRequestForm_For_113_Conditional_33_Template, 1, 0, "input", 99);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(34, HolidayTravelRequestForm_For_113_Conditional_34_Template, 4, 1, "td", 100);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    let tmp_12_0;
    let tmp_14_0;
    const ctrl_r12 = ctx.$implicit;
    const \u0275$index_331_r17 = ctx.$index;
    const ctx_r1 = \u0275\u0275nextContext();
    const isOcr_r19 = \u0275\u0275storeLet(ctx_r1.ocrLoadingIds.has((tmp_12_0 = ctrl_r12.get("id")) == null ? null : tmp_12_0.value));
    \u0275\u0275advance();
    \u0275\u0275property("formGroupName", \u0275$index_331_r17);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(((tmp_14_0 = ctrl_r12.get("previewUrl")) == null ? null : tmp_14_0.value) || ((tmp_14_0 = ctrl_r12.get("fileUrl")) == null ? null : tmp_14_0.value) ? 3 : isOcr_r19 ? 4 : -1);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(isOcr_r19 ? 6 : ctx_r1.isReadOnly ? 7 : 8);
    \u0275\u0275advance(4);
    \u0275\u0275conditional(isOcr_r19 ? 10 : ctx_r1.isReadOnly ? 11 : 12);
    \u0275\u0275advance(4);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 14 : 15);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 17 : 18);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(isOcr_r19 ? 20 : ctx_r1.isReadOnly ? 21 : 22);
    \u0275\u0275advance(4);
    \u0275\u0275conditional(isOcr_r19 ? 24 : ctx_r1.isReadOnly ? 25 : 26);
    \u0275\u0275advance(4);
    \u0275\u0275conditional(isOcr_r19 ? 28 : ctx_r1.isReadOnly ? 29 : 30);
    \u0275\u0275advance(4);
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 32 : 33);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(!ctx_r1.isReadOnly ? 34 : -1);
  }
}
function HolidayTravelRequestForm_ForEmpty_114_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u5C1A\u7121\u8CBB\u7528\u660E\u7D30\u3002");
  }
}
function HolidayTravelRequestForm_ForEmpty_114_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u8ACB\u4E0A\u50B3\u767C\u7968\u5716\u6A94\uFF0C\u6216\u9EDE\u64CA\u300C\u624B\u52D5\u65B0\u589E\u884C\u300D\u3002");
  }
}
function HolidayTravelRequestForm_ForEmpty_114_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 107);
    \u0275\u0275conditionalCreate(2, HolidayTravelRequestForm_ForEmpty_114_Conditional_2_Template, 1, 0)(3, HolidayTravelRequestForm_ForEmpty_114_Conditional_3_Template, 1, 0);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275attribute("colspan", ctx_r1.isReadOnly ? 9 : 10);
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 2 : 3);
  }
}
function HolidayTravelRequestForm_Conditional_115_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "td");
  }
}
function HolidayTravelRequestForm_Conditional_115_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tfoot")(1, "tr", 44)(2, "td", 108);
    \u0275\u0275text(3, "\u5408\u8A08");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "td", 109);
    \u0275\u0275text(5);
    \u0275\u0275pipe(6, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275element(7, "td");
    \u0275\u0275conditionalCreate(8, HolidayTravelRequestForm_Conditional_115_Conditional_8_Template, 1, 0, "td");
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
function HolidayTravelRequestForm_Conditional_120_For_10_For_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 24);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const jt_r24 = ctx.$implicit;
    \u0275\u0275property("ngValue", jt_r24.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(jt_r24.name);
  }
}
function HolidayTravelRequestForm_Conditional_120_For_10_For_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 24);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const user_r25 = ctx.$implicit;
    \u0275\u0275property("ngValue", user_r25.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(user_r25.name);
  }
}
function HolidayTravelRequestForm_Conditional_120_For_10_Template(rf, ctx) {
  if (rf & 1) {
    const _r21 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 73)(1, "span", 74);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "select", 111);
    \u0275\u0275twoWayListener("ngModelChange", function HolidayTravelRequestForm_Conditional_120_For_10_Template_select_ngModelChange_3_listener($event) {
      const entry_r22 = \u0275\u0275restoreView(_r21).$implicit;
      \u0275\u0275twoWayBindingSet(entry_r22.selectedJobTitleId, $event) || (entry_r22.selectedJobTitleId = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275listener("ngModelChange", function HolidayTravelRequestForm_Conditional_120_For_10_Template_select_ngModelChange_3_listener() {
      const \u0275$index_511_r23 = \u0275\u0275restoreView(_r21).$index;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.onEntryJobTitleChange(\u0275$index_511_r23));
    });
    \u0275\u0275elementStart(4, "option", 24);
    \u0275\u0275text(5, "\u2014 \u8077\u7A31 \u2014");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(6, HolidayTravelRequestForm_Conditional_120_For_10_For_7_Template, 2, 2, "option", 24, _forTrack0);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "select", 112);
    \u0275\u0275twoWayListener("ngModelChange", function HolidayTravelRequestForm_Conditional_120_For_10_Template_select_ngModelChange_8_listener($event) {
      const entry_r22 = \u0275\u0275restoreView(_r21).$implicit;
      \u0275\u0275twoWayBindingSet(entry_r22.selectedUserId, $event) || (entry_r22.selectedUserId = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementStart(9, "option", 24);
    \u0275\u0275text(10, "\u2014 \u4EBA\u54E1 \u2014");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(11, HolidayTravelRequestForm_Conditional_120_For_10_For_12_Template, 2, 2, "option", 24, _forTrack0);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "button", 77);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_120_For_10_Template_button_click_13_listener() {
      const \u0275$index_511_r23 = \u0275\u0275restoreView(_r21).$index;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.removeDesignatedEntry(\u0275$index_511_r23));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(14, "svg", 60);
    \u0275\u0275element(15, "use", 78);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const entry_r22 = ctx.$implicit;
    const \u0275$index_511_r23 = ctx.$index;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1("", \u0275$index_511_r23 + 1, ".");
    \u0275\u0275advance();
    \u0275\u0275twoWayProperty("ngModel", entry_r22.selectedJobTitleId);
    \u0275\u0275property("ngModelOptions", \u0275\u0275pureFunction0(7, _c1));
    \u0275\u0275advance();
    \u0275\u0275property("ngValue", null);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(ctx_r1.jobTitles);
    \u0275\u0275advance(2);
    \u0275\u0275twoWayProperty("ngModel", entry_r22.selectedUserId);
    \u0275\u0275property("ngModelOptions", \u0275\u0275pureFunction0(8, _c1));
    \u0275\u0275advance();
    \u0275\u0275property("ngValue", null);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(entry_r22.filteredUsers);
  }
}
function HolidayTravelRequestForm_Conditional_120_Template(rf, ctx) {
  if (rf & 1) {
    const _r20 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 34)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 37);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6307\u5B9A\u5BE9\u6838\u8005 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "div", 6)(7, "label", 19);
    \u0275\u0275text(8, "\u6307\u5B9A\u5BE9\u6838\u8005\uFF08\u4F9D\u5E8F\u5BE9\u6838\uFF09");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(9, HolidayTravelRequestForm_Conditional_120_For_10_Template, 16, 9, "div", 73, \u0275\u0275repeaterTrackByIndex);
    \u0275\u0275elementStart(11, "button", 110);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_120_Template_button_click_11_listener() {
      \u0275\u0275restoreView(_r20);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.addDesignatedEntry());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(12, "svg", 60);
    \u0275\u0275element(13, "use", 72);
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
function HolidayTravelRequestForm_Conditional_121_For_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "li", 91);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const entry_r26 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.getUserName(entry_r26.selectedUserId));
  }
}
function HolidayTravelRequestForm_Conditional_121_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 34)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 37);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6307\u5B9A\u5BE9\u6838\u8005 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "label", 19);
    \u0275\u0275text(7, "\u6307\u5B9A\u5BE9\u6838\u8005\uFF08\u4F9D\u5E8F\u5BE9\u6838\uFF09");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "ol", 113);
    \u0275\u0275repeaterCreate(9, HolidayTravelRequestForm_Conditional_121_For_10_Template, 2, 1, "li", 91, \u0275\u0275repeaterTrackByIndex);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(9);
    \u0275\u0275repeater(ctx_r1.designatedEntries);
  }
}
function HolidayTravelRequestForm_Conditional_123_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 69);
  }
}
function HolidayTravelRequestForm_Conditional_123_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 118);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 55);
    \u0275\u0275element(2, "use", 61);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" ", ctx, " ");
  }
}
function HolidayTravelRequestForm_Conditional_123_Template(rf, ctx) {
  if (rf & 1) {
    const _r27 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 114)(1, "button", 115);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "button", 116);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_123_Template_button_click_3_listener() {
      \u0275\u0275restoreView(_r27);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.submitForApproval());
    });
    \u0275\u0275conditionalCreate(4, HolidayTravelRequestForm_Conditional_123_Conditional_4_Template, 1, 0, "span", 69);
    \u0275\u0275text(5, " \u9001\u51FA\u7533\u8ACB ");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "a", 117);
    \u0275\u0275text(7, "\u53D6\u6D88");
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(8, HolidayTravelRequestForm_Conditional_123_Conditional_8_Template, 4, 1, "div", 118);
  }
  if (rf & 2) {
    let tmp_6_0;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r1.disabledReason !== null);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.isEdit ? "\u5132\u5B58" : "\u5132\u5B58\u8349\u7A3F", " ");
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r1.disabledReason !== null);
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.isAnyOcrPending ? 4 : -1);
    \u0275\u0275advance(4);
    \u0275\u0275conditional((tmp_6_0 = ctx_r1.disabledReason) ? 8 : -1, tmp_6_0);
  }
}
function HolidayTravelRequestForm_Conditional_124_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 58)(1, "a", 117);
    \u0275\u0275text(2, "\u8FD4\u56DE\u5217\u8868");
    \u0275\u0275elementEnd()();
  }
}
function HolidayTravelRequestForm_Conditional_125_Template(rf, ctx) {
  if (rf & 1) {
    const _r28 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "app-file-preview-modal", 119);
    \u0275\u0275listener("closed", function HolidayTravelRequestForm_Conditional_125_Template_app_file_preview_modal_closed_0_listener() {
      \u0275\u0275restoreView(_r28);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.closePreview());
    });
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275property("file", ctx_r1.previewFile);
  }
}
function HolidayTravelRequestForm_ng_template_126_Template(rf, ctx) {
  if (rf & 1) {
    const _r29 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 120)(1, "button", 121);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_ng_template_126_Template_button_click_1_listener() {
      const modal_r30 = \u0275\u0275restoreView(_r29).$implicit;
      return \u0275\u0275resetView(modal_r30.close());
    });
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(2, "div", 122);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(3, "svg", 123);
    \u0275\u0275element(4, "use", 66);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "h5", 124);
    \u0275\u0275text(6, "\u7533\u8ACB\u6210\u529F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "p", 125);
    \u0275\u0275text(8, "\u8ACB\u76E1\u65E9\u5C07\u6B63\u672C\u8CC7\u6599\u9001\u56DE\u7BA1\u7406\u8655");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(9, "div", 126)(10, "button", 127);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_ng_template_126_Template_button_click_10_listener() {
      const modal_r30 = \u0275\u0275restoreView(_r29).$implicit;
      return \u0275\u0275resetView(modal_r30.close());
    });
    \u0275\u0275text(11, "\u78BA\u5B9A");
    \u0275\u0275elementEnd()();
  }
}
var HolidayTravelRequestForm = class _HolidayTravelRequestForm {
  fb = inject(FormBuilder);
  service = inject(HolidayTravelRequestService);
  projects$ = inject(ProjectService);
  jobTitleSvc = inject(JobTitleService);
  userSvc = inject(UserService);
  approvalSvc = inject(ApprovalService);
  taskSvc = inject(ApprovalTaskService);
  paymentSvc = inject(PaymentRequestService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  cdr = inject(ChangeDetectorRef);
  modal = inject(NgbModal);
  sanitizer = inject(DomSanitizer);
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
  /** 假日天數（從行事曆 API 查詢） */
  holidayDays = signal(null, ...ngDevMode ? [{ debugName: "holidayDays" }] : []);
  holidayDaysLoading = signal(false, ...ngDevMode ? [{ debugName: "holidayDaysLoading" }] : []);
  holidayDaysNoCalendar = signal(false, ...ngDevMode ? [{ debugName: "holidayDaysNoCalendar" }] : []);
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
  /** 參與執行人員清單 */
  participantEntries = [];
  /** 發票檔案 id → File 物件（新上傳） */
  fileMap = /* @__PURE__ */ new Map();
  /** 正在 OCR 處理中的 row ids */
  ocrLoadingIds = /* @__PURE__ */ new Set();
  get isAnyOcrPending() {
    return this.ocrLoadingIds.size > 0;
  }
  /** 檔案預覽 modal */
  previewFile = null;
  openPreview(name, url) {
    this.previewFile = { name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url) };
  }
  closePreview() {
    this.previewFile = null;
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
  /** 按鈕 disabled 時的提示訊息，null 表示可提交 */
  get disabledReason() {
    if (this.isAnyOcrPending)
      return "\u767C\u7968\u8FA8\u8B58\u4E2D\uFF0C\u8ACB\u7A0D\u5019\u2026";
    if (this.itemArray.length === 0)
      return "\u8ACB\u65B0\u589E\u81F3\u5C11\u4E00\u7B46\u8CBB\u7528\u660E\u7D30\u3002";
    if (this.form.invalid) {
      const fields = [
        ["destination", "\u57F7\u884C\u6D3B\u52D5\u5730\u9EDE"],
        ["startDate", "\u958B\u59CB\u65E5\u671F"],
        ["endDate", "\u7D50\u675F\u65E5\u671F"],
        ["purpose", "\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9"]
      ];
      for (const [key, label] of fields) {
        if (this.form.get(key)?.invalid)
          return `\u8ACB\u586B\u5BEB\u300C${label}\u300D\u3002`;
      }
      const idx = this.itemControls.findIndex((c) => c.get("itemName")?.invalid);
      if (idx >= 0)
        return `\u7B2C ${idx + 1} \u7B46\u8CBB\u7528\u660E\u7D30\u7684\u300C\u9805\u76EE\u8AAA\u660E\u300D\u672A\u586B\u5BEB\u3002`;
      return "\u8868\u55AE\u8CC7\u6599\u4E0D\u5B8C\u6574\uFF0C\u8ACB\u6AA2\u67E5\u5FC5\u586B\u6B04\u4F4D\u3002";
    }
    return null;
  }
  /** 日期變更時查詢假日天數 */
  onDateChange() {
    const v = this.form.value;
    if (!v.startDate || !v.endDate) {
      this.holidayDays.set(null);
      return;
    }
    this.holidayDaysLoading.set(true);
    this.holidayDaysNoCalendar.set(false);
    this.service.countHolidays(v.startDate, v.endDate).subscribe({
      next: (res) => {
        this.holidayDays.set(res.holidayDays);
        this.holidayDaysNoCalendar.set(!res.hasCalendarData);
        this.holidayDaysLoading.set(false);
      },
      error: () => {
        this.holidayDays.set(null);
        this.holidayDaysLoading.set(false);
      }
    });
  }
  // ── 指定審核者操作 ──
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
  // ── 參與執行人員操作 ──
  addParticipant() {
    const nextOrder = this.participantEntries.length + 1;
    this.participantEntries.push({ sortOrder: nextOrder, selectedUserId: null });
  }
  removeParticipant(i) {
    this.participantEntries.splice(i, 1);
    this.participantEntries.forEach((e, idx) => e.sortOrder = idx + 1);
  }
  // ── 發票上傳 / OCR ──
  /** 發票檔案上傳 — 自動新增行、OCR 辨識 */
  async onFilesSelected(event) {
    const input = event.target;
    if (!input.files?.length)
      return;
    const rawFiles = Array.from(input.files);
    input.value = "";
    const files = await Promise.all(rawFiles.map((f) => this._convertHeicIfNeeded(f)));
    const entries = files.map((file) => {
      const id = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      const previewUrl = URL.createObjectURL(file);
      this.ocrLoadingIds.add(id);
      this.fileMap.set(id, file);
      this.itemArray.push(this._itemGroup(id, file.name, 0, "", 0, "", 0, "", this.itemArray.length, previewUrl));
      return { id, file };
    });
    await Promise.all(entries.map(async ({ id, file }) => {
      try {
        const result = await firstValueFrom(this.paymentSvc.ocrInvoice(file));
        const idx = this.itemArray.controls.findIndex((c) => c.get("id")?.value === id);
        if (idx >= 0) {
          this.itemArray.controls[idx].patchValue({
            invoiceNo: result.invoiceNo ?? "",
            invoiceDate: result.invoiceDate ?? "",
            unitPrice: result.amount ?? 0,
            totalPrice: result.amount ?? 0,
            quantity: "1\u5F0F",
            itemName: result.invoiceNo ? `\u767C\u7968 ${result.invoiceNo}` : file.name
          });
        }
      } catch {
      } finally {
        this.ocrLoadingIds.delete(id);
        this.cdr.markForCheck();
      }
    }));
  }
  async _convertHeicIfNeeded(file) {
    const name = file.name.toLowerCase();
    if (!name.endsWith(".heic") && !name.endsWith(".heif"))
      return file;
    try {
      const blob = await (0, import_heic2any.default)({ blob: file, toType: "image/jpeg", quality: 0.85 });
      const jpegName = file.name.replace(/\.heic$/i, ".jpg").replace(/\.heif$/i, ".jpg");
      return new File([blob], jpegName, { type: "image/jpeg" });
    } catch {
      return file;
    }
  }
  addItem() {
    this.itemArray.push(this._itemGroup("", "", 0, "", 0, "", 0, "", this.itemArray.length));
  }
  removeItem(i) {
    const ctrl = this.itemArray.at(i);
    const id = ctrl.get("id")?.value;
    const url = ctrl.get("previewUrl")?.value;
    if (url?.startsWith("blob:"))
      URL.revokeObjectURL(url);
    this.fileMap.delete(id);
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
  ngOnInit() {
    this.userSvc.getLookup().subscribe({
      next: (users) => {
        this.allUsers = users;
        this.cdr.markForCheck();
      }
    });
    this.approvalSvc.getAll().subscribe((items) => {
      this.hasDesignatedStep = items.filter((i) => i.isActive && i.applicationType === "holiday_travel").some((i) => i.steps.some((s) => s.useApplicantDesignated));
      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: (jts) => {
          this.jobTitles = jts;
        } });
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
        this.onDateChange();
        if (r.participants?.length) {
          this.participantEntries = r.participants.sort((a, b) => a.sortOrder - b.sortOrder).map((p) => ({ sortOrder: p.sortOrder, selectedUserId: p.userId }));
        }
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
        (r.items ?? []).forEach((item, idx) => {
          this.itemArray.push(this._itemGroup(`existing-${item.id}`, item.fileName ?? "", item.seqNo, item.itemName, item.unitPrice, item.quantity, item.totalPrice, item.note ?? "", idx, "", item.fileUrl ?? ""));
          const ctrl = this.itemArray.at(idx);
          ctrl.patchValue({
            invoiceNo: item.invoiceNo ?? "",
            invoiceDate: item.invoiceDate ?? "",
            category: item.category
          });
        });
        if (this.isReadOnly)
          this.form.disable();
        if (r.approvalStatus !== "draft") {
          this.taskSvc.getById(this.requestId, "holiday_travel").subscribe({
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
  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.form.invalid || this.itemArray.length === 0 || this.isReadOnly)
      return;
    const fd = this._buildFormData();
    const obs = this.isEdit ? this.service.update(this.requestId, fd) : this.service.create(fd);
    this.errorMsg.set("");
    obs.subscribe({
      next: (saved) => {
        if (!this.isEdit)
          this.requestId = saved.id;
        this.router.navigate(["/admin/holiday-travel-requests"]);
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
    const fd = this._buildFormData();
    const save$ = this.isEdit ? this.service.update(this.requestId, fd) : this.service.create(fd);
    this.errorMsg.set("");
    save$.subscribe({
      next: (saved) => {
        this.service.submit(saved.id).subscribe({
          next: () => {
            const tpl = this.successModal();
            if (tpl) {
              const ref = this.modal.open(tpl, { centered: true, backdrop: "static", keyboard: false });
              ref.result.then(() => this.router.navigate(["/admin/holiday-travel-requests"])).catch(() => this.router.navigate(["/admin/holiday-travel-requests"]));
            } else {
              this.router.navigate(["/admin/holiday-travel-requests"]);
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
  _buildFormData() {
    const v = this.form.value;
    const project = this.projects.find((p) => p.id === v.projectId);
    const fd = new FormData();
    fd.append("destination", v.destination);
    fd.append("startDate", v.startDate);
    fd.append("endDate", v.endDate);
    fd.append("purpose", v.purpose);
    if (v.projectId) {
      fd.append("projectId", String(v.projectId));
      if (project?.code)
        fd.append("projectCode", project.code);
    }
    const participants = this.participantEntries.filter((e) => e.selectedUserId).map((e) => ({ userId: e.selectedUserId, sortOrder: e.sortOrder }));
    if (participants.length > 0) {
      fd.append("participants", JSON.stringify(participants));
    }
    const reviewers = this.designatedEntries.filter((e) => e.selectedUserId).map((e) => ({ reviewerId: e.selectedUserId, stepOrder: e.stepOrder }));
    if (reviewers.length > 0) {
      fd.append("designatedReviewers", JSON.stringify(reviewers));
    }
    const itemsMeta = [];
    let fileIndex = 0;
    for (let i = 0; i < this.itemArray.controls.length; i++) {
      const ctrl = this.itemArray.at(i);
      const rowId = ctrl.get("id")?.value;
      const file = this.fileMap.get(rowId);
      itemsMeta.push({
        category: ctrl.get("category")?.value || "",
        seqNo: +ctrl.get("seqNo")?.value || 0,
        itemName: ctrl.get("itemName")?.value || "",
        unitPrice: +ctrl.get("unitPrice")?.value || 0,
        quantity: ctrl.get("quantity")?.value || "",
        totalPrice: +ctrl.get("totalPrice")?.value || 0,
        note: ctrl.get("note")?.value || null,
        invoiceNo: ctrl.get("invoiceNo")?.value || null,
        invoiceDate: ctrl.get("invoiceDate")?.value || null,
        fileName: ctrl.get("fileName")?.value || null,
        fileUrl: ctrl.get("fileUrl")?.value || null,
        fileIndex: file ? fileIndex : -1,
        sortOrder: i
      });
      if (file) {
        fd.append("files", file, file.name);
        fileIndex++;
      }
    }
    const grandTotal = this.itemArray.controls.reduce((s, c) => s + (+c.get("totalPrice")?.value || 0), 0);
    fd.append("grandTotal", String(grandTotal));
    fd.append("items", JSON.stringify(itemsMeta));
    return fd;
  }
  _itemGroup(id, fileName, seqNo, itemName, unitPrice, quantity, totalPrice, note, sortOrder, previewUrl = "", fileUrl = "") {
    return this.fb.group({
      id: [id || `${Date.now()}-${Math.random().toString(36).slice(2)}`],
      fileName: [fileName],
      invoiceNo: [""],
      invoiceDate: [""],
      category: [""],
      seqNo: [seqNo],
      itemName: [itemName, Validators.required],
      unitPrice: [unitPrice, [Validators.min(0)]],
      quantity: [quantity],
      totalPrice: [totalPrice],
      note: [note],
      previewUrl: [previewUrl],
      fileUrl: [fileUrl],
      sortOrder: [sortOrder]
    });
  }
  static \u0275fac = function HolidayTravelRequestForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _HolidayTravelRequestForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _HolidayTravelRequestForm, selectors: [["app-holiday-travel-request-form"]], viewQuery: function HolidayTravelRequestForm_Query(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275viewQuerySignal(ctx.successModal, _c0, 5);
    }
    if (rf & 2) {
      \u0275\u0275queryAdvance();
    }
  }, decls: 128, vars: 26, consts: [["successModal", ""], [1, "container-fluid", "py-3"], [1, "flex", "items-center", "gap-2", "mb-6"], ["routerLink", "/admin/holiday-travel-requests", 1, "btn", "btn-sm", "btn-outline-secondary"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], ["role", "alert", 1, "alert", "alert-danger", "flex", "items-center", "gap-2", "mb-6", "py-2"], [1, "card", "border-0", "shadow-sm", "mb-6"], [3, "ngSubmit", "formGroup"], [1, "row", "g-4"], [1, "col-12", "col-xl-10"], [1, "card", "border-0", "shadow-sm"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#sun"], [1, "card-body"], [1, "row", "g-3", "mb-4"], [1, "col-12", "col-md-6"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "destination", "placeholder", "\u4F8B\u5982\uFF1A\u53F0\u5357\u3001\u53F0\u4E2D", 1, "form-control"], [1, "text-danger", "small", "mt-1"], ["formControlName", "projectId", 1, "form-select"], [3, "ngValue"], [1, "col-12", "col-md-4"], ["type", "date", "formControlName", "startDate", 1, "form-control", 3, "change"], ["type", "date", "formControlName", "endDate", 1, "form-control", 3, "change"], [1, "form-control", "bg-elevated", "text-muted", 2, "cursor", "default"], [1, "text-warning"], [1, "fw-500", 2, "color", "var(--forest)"], [1, "text-muted", "small", "mt-1"], [1, "mb-4"], ["formControlName", "purpose", "rows", "3", "placeholder", "\u8ACB\u586B\u5BEB\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9...", 1, "form-control"], [1, "card", "border-0", "shadow-sm", "mt-6"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "justify-between"], [1, "flex", "items-center", "gap-2", "fw-600"], ["href", "/assets/icons/sprite.svg#users"], ["type", "button", 1, "btn", "btn-sm", "btn-outline-primary", "inline-flex", "items-center", "gap-1"], [1, "text-muted", "small"], ["href", "/assets/icons/sprite.svg#file-text"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-sm", "mb-0"], [1, "table-light"], [2, "width", "40px"], [2, "min-width", "140px"], [2, "min-width", "130px"], [2, "min-width", "100px"], [2, "min-width", "160px"], [2, "min-width", "80px"], [2, "width", "48px"], ["formArrayName", "items"], [3, "formGroupName"], [1, "px-3", "py-2", "flex", "items-center", "gap-2", "text-info", "small"], [1, "sa-icon", "shrink-0", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#info"], [3, "flow", "approvalRecords", "currentStepOrder", "status"], [1, "mt-6"], [3, "file"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#alert-triangle"], [1, "card-header", "bg-[rgba(13,110,253,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-primary", "py-3"], ["href", "/assets/icons/sprite.svg#clock"], [1, "card-header", "bg-[rgba(255,193,7,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-warning", "py-3"], [1, "card-header", "bg-[rgba(37,162,68,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-success", "py-3"], ["href", "/assets/icons/sprite.svg#check-circle"], [1, "card-header", "bg-[rgba(220,53,69,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-danger", "py-3"], ["href", "/assets/icons/sprite.svg#x-circle"], ["role", "status", 1, "spinner-border", "spinner-border-sm", "me-1"], [1, "badge", "rounded-pill", "px-3", "py-2"], ["type", "button", 1, "btn", "btn-sm", "btn-outline-primary", "inline-flex", "items-center", "gap-1", 3, "click"], ["href", "/assets/icons/sprite.svg#plus"], [1, "flex", "items-center", "gap-2", "mb-2"], [1, "text-muted", "small", 2, "min-width", "1.5rem"], [1, "small", "fw-500"], [1, "form-select", "form-select-sm", 2, "max-width", "240px", 3, "ngModelChange", "ngModel", "ngModelOptions"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-danger", 3, "click"], ["href", "/assets/icons/sprite.svg#x"], ["href", "/assets/icons/sprite.svg#upload"], [1, "flex", "flex-col", "items-center", "justify-center", "rounded-3", "py-4", "px-4", "mb-0", "text-center", 2, "cursor", "pointer", "border", "2px dashed var(--bs-border-color)"], [1, "sa-icon", "sa-icon-2x", "text-muted", "mb-2", 2, "stroke", "currentColor"], [1, "fw-500"], ["type", "file", "multiple", "", "accept", "image/*,.heic,.heif,application/pdf", 1, "hidden", 3, "change"], [1, "align-middle", "text-center"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-secondary", "p-1", 3, "title"], ["role", "status", 1, "spinner-border", "spinner-border-sm"], [1, "align-middle"], [1, "flex", "items-center", "gap-1", "text-muted", "small", "py-1"], [1, "small", "font-monospace"], ["formControlName", "invoiceNo", "placeholder", "AB12345678", 1, "form-control", "form-control-sm", "font-monospace"], [1, "small"], ["type", "date", "formControlName", "invoiceDate", 1, "form-control", "form-control-sm"], ["formControlName", "category", 1, "form-select", "form-select-sm"], ["formControlName", "itemName", "placeholder", "\u9805\u76EE\u8AAA\u660E", 1, "form-control", "form-control-sm"], [1, "py-1", "text-muted", "small"], ["type", "number", "formControlName", "unitPrice", "min", "0", 1, "form-control", "form-control-sm"], ["formControlName", "quantity", "placeholder", "\u5982\uFF1A1\u5F0F", 1, "form-control", "form-control-sm"], ["type", "number", "formControlName", "totalPrice", "min", "0", 1, "form-control", "form-control-sm"], ["formControlName", "note", "placeholder", "", 1, "form-control", "form-control-sm"], [1, "text-right", "align-middle"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-secondary", "p-1", 3, "click", "title"], ["value", ""], [3, "value"], ["type", "number", "formControlName", "unitPrice", "min", "0", 1, "form-control", "form-control-sm", 3, "input"], ["formControlName", "quantity", "placeholder", "\u5982\uFF1A1\u5F0F", 1, "form-control", "form-control-sm", 3, "input"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-danger", "inline-flex", "items-center", 3, "click", "disabled"], [1, "text-center", "text-muted", "py-4", "small"], ["colspan", "7", 1, "text-right", "fw-500", "small"], [1, "fw-600"], ["type", "button", 1, "btn", "btn-sm", "btn-outline-secondary", "mt-1", 3, "click"], [1, "form-select", "form-select-sm", 2, "max-width", "160px", 3, "ngModelChange", "ngModel", "ngModelOptions"], [1, "form-select", "form-select-sm", 2, "max-width", "200px", 3, "ngModelChange", "ngModel", "ngModelOptions"], [1, "mb-0", "ps-4"], [1, "mt-6", "flex", "gap-2"], ["type", "submit", 1, "btn", "btn-outline-secondary", 3, "disabled"], ["type", "button", 1, "btn", "btn-primary", 3, "click", "disabled"], ["routerLink", "/admin/holiday-travel-requests", 1, "btn", "btn-outline-secondary"], [1, "text-warning", "small", "mt-2", "flex", "items-center", "gap-1"], [3, "closed", "file"], [1, "modal-header", "border-0", "pb-0"], ["type", "button", 1, "btn-close", 3, "click"], [1, "modal-body", "text-center", "py-6"], [1, "sa-icon", "sa-icon-3x", "text-success", "mb-4", 2, "stroke", "currentColor"], [1, "fw-600", "mb-2"], [1, "text-secondary", "mb-0"], [1, "modal-footer", "border-0", "justify-center", "pt-0"], ["type", "button", 1, "btn", "btn-primary", "px-6", 3, "click"]], template: function HolidayTravelRequestForm_Template(rf, ctx) {
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
      \u0275\u0275conditionalCreate(7, HolidayTravelRequestForm_Conditional_7_Template, 4, 1, "div", 7);
      \u0275\u0275conditionalCreate(8, HolidayTravelRequestForm_Conditional_8_Template, 5, 0, "div", 8)(9, HolidayTravelRequestForm_Conditional_9_Template, 5, 0, "div", 8)(10, HolidayTravelRequestForm_Conditional_10_Template, 5, 0, "div", 8)(11, HolidayTravelRequestForm_Conditional_11_Template, 5, 0, "div", 8);
      \u0275\u0275elementStart(12, "form", 9);
      \u0275\u0275listener("ngSubmit", function HolidayTravelRequestForm_Template_form_ngSubmit_12_listener() {
        \u0275\u0275restoreView(_r1);
        return \u0275\u0275resetView(ctx.save());
      });
      \u0275\u0275elementStart(13, "div", 10)(14, "div", 11)(15, "div", 12)(16, "div", 13);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(17, "svg", 14);
      \u0275\u0275element(18, "use", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275text(19, " \u6D3B\u52D5\u57FA\u672C\u8CC7\u8A0A ");
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(20, "div", 16)(21, "div", 17)(22, "div", 18)(23, "label", 19);
      \u0275\u0275text(24, "\u57F7\u884C\u6D3B\u52D5\u5730\u9EDE ");
      \u0275\u0275elementStart(25, "span", 20);
      \u0275\u0275text(26, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(27, "input", 21);
      \u0275\u0275conditionalCreate(28, HolidayTravelRequestForm_Conditional_28_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(29, "div", 18)(30, "label", 19);
      \u0275\u0275text(31, "\u95DC\u806F\u5C08\u6848\uFF08\u9078\u586B\uFF09");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(32, "select", 23)(33, "option", 24);
      \u0275\u0275text(34);
      \u0275\u0275elementEnd();
      \u0275\u0275repeaterCreate(35, HolidayTravelRequestForm_For_36_Template, 2, 4, "option", 24, _forTrack0);
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(37, "div", 17)(38, "div", 25)(39, "label", 19);
      \u0275\u0275text(40, "\u958B\u59CB\u65E5\u671F ");
      \u0275\u0275elementStart(41, "span", 20);
      \u0275\u0275text(42, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(43, "input", 26);
      \u0275\u0275listener("change", function HolidayTravelRequestForm_Template_input_change_43_listener() {
        \u0275\u0275restoreView(_r1);
        return \u0275\u0275resetView(ctx.onDateChange());
      });
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(44, HolidayTravelRequestForm_Conditional_44_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(45, "div", 25)(46, "label", 19);
      \u0275\u0275text(47, "\u7D50\u675F\u65E5\u671F ");
      \u0275\u0275elementStart(48, "span", 20);
      \u0275\u0275text(49, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(50, "input", 27);
      \u0275\u0275listener("change", function HolidayTravelRequestForm_Template_input_change_50_listener() {
        \u0275\u0275restoreView(_r1);
        return \u0275\u0275resetView(ctx.onDateChange());
      });
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(51, HolidayTravelRequestForm_Conditional_51_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(52, "div", 25)(53, "label", 19);
      \u0275\u0275text(54, "\u5047\u65E5\u5929\u6578");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(55, "div", 28);
      \u0275\u0275conditionalCreate(56, HolidayTravelRequestForm_Conditional_56_Template, 2, 0)(57, HolidayTravelRequestForm_Conditional_57_Template, 2, 0, "span", 29)(58, HolidayTravelRequestForm_Conditional_58_Template, 2, 1, "span", 30)(59, HolidayTravelRequestForm_Conditional_59_Template, 2, 0, "span");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(60, "div", 31);
      \u0275\u0275text(61, "\u7CFB\u7D71\u4F9D\u884C\u4E8B\u66C6\u8CC7\u6599\u81EA\u52D5\u8A08\u7B97");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(62, "div", 32)(63, "label", 19);
      \u0275\u0275text(64, "\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9 ");
      \u0275\u0275elementStart(65, "span", 20);
      \u0275\u0275text(66, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(67, "textarea", 33);
      \u0275\u0275conditionalCreate(68, HolidayTravelRequestForm_Conditional_68_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(69, HolidayTravelRequestForm_Conditional_69_Template, 6, 3, "div", 6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(70, "div", 34)(71, "div", 35)(72, "div", 36);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(73, "svg", 14);
      \u0275\u0275element(74, "use", 37);
      \u0275\u0275elementEnd();
      \u0275\u0275text(75, " \u53C3\u8207\u57F7\u884C\u4EBA\u54E1 ");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(76, HolidayTravelRequestForm_Conditional_76_Template, 4, 0, "button", 38);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(77, "div", 16);
      \u0275\u0275conditionalCreate(78, HolidayTravelRequestForm_Conditional_78_Template, 3, 1, "div", 39)(79, HolidayTravelRequestForm_Conditional_79_Template, 2, 0);
      \u0275\u0275elementEnd()();
      \u0275\u0275conditionalCreate(80, HolidayTravelRequestForm_Conditional_80_Template, 14, 0, "div", 34);
      \u0275\u0275elementStart(81, "div", 34)(82, "div", 35)(83, "div", 36);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(84, "svg", 14);
      \u0275\u0275element(85, "use", 40);
      \u0275\u0275elementEnd();
      \u0275\u0275text(86, " \u8CBB\u7528\u660E\u7D30 ");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(87, HolidayTravelRequestForm_Conditional_87_Template, 4, 0, "button", 38);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(88, "div", 41)(89, "div", 42)(90, "table", 43)(91, "thead", 44)(92, "tr");
      \u0275\u0275element(93, "th", 45);
      \u0275\u0275elementStart(94, "th", 46);
      \u0275\u0275text(95, "\u767C\u7968\u865F\u78BC");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(96, "th", 47);
      \u0275\u0275text(97, "\u767C\u7968\u65E5\u671F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(98, "th", 48);
      \u0275\u0275text(99, "\u5206\u985E");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(100, "th", 49);
      \u0275\u0275text(101, "\u9805\u76EE\u8AAA\u660E");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(102, "th", 48);
      \u0275\u0275text(103, "\u55AE\u50F9");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(104, "th", 50);
      \u0275\u0275text(105, "\u6578\u91CF/\u55AE\u4F4D");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(106, "th", 48);
      \u0275\u0275text(107, "\u7E3D\u50F9");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(108, "th", 48);
      \u0275\u0275text(109, "\u5099\u8A3B");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(110, HolidayTravelRequestForm_Conditional_110_Template, 1, 0, "th", 51);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(111, "tbody", 52);
      \u0275\u0275repeaterCreate(112, HolidayTravelRequestForm_For_113_Template, 35, 12, "tr", 53, _forTrack1, false, HolidayTravelRequestForm_ForEmpty_114_Template, 4, 2, "tr");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(115, HolidayTravelRequestForm_Conditional_115_Template, 9, 5, "tfoot");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(116, "div", 54);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(117, "svg", 55);
      \u0275\u0275element(118, "use", 56);
      \u0275\u0275elementEnd();
      \u0275\u0275text(119, " \u82E5\u57F7\u884C\u8CBB\u7528\u5DF2\u7533\u8ACB\u9810\u652F\uFF0C\u8ACB\u4EE5\u9810\u652F\u6C96\u92B7\u7533\u8ACB\uFF0C\u5176\u4ED6\u672A\u7533\u8ACB\u9810\u652F\u7522\u751F\u4E4B\u8CBB\u7528\uFF0C\u53EF\u65BC\u6B64\u7533\u8ACB\u3002 ");
      \u0275\u0275elementEnd()()();
      \u0275\u0275conditionalCreate(120, HolidayTravelRequestForm_Conditional_120_Template, 15, 0, "div", 34)(121, HolidayTravelRequestForm_Conditional_121_Template, 11, 0, "div", 34);
      \u0275\u0275namespaceHTML();
      \u0275\u0275element(122, "app-approval-timeline", 57);
      \u0275\u0275conditionalCreate(123, HolidayTravelRequestForm_Conditional_123_Template, 9, 5)(124, HolidayTravelRequestForm_Conditional_124_Template, 3, 0, "div", 58);
      \u0275\u0275elementEnd()()()();
      \u0275\u0275conditionalCreate(125, HolidayTravelRequestForm_Conditional_125_Template, 1, 1, "app-file-preview-modal", 59);
      \u0275\u0275template(126, HolidayTravelRequestForm_ng_template_126_Template, 12, 0, "ng-template", null, 0, \u0275\u0275templateRefExtractor);
    }
    if (rf & 2) {
      let tmp_5_0;
      let tmp_9_0;
      let tmp_10_0;
      let tmp_12_0;
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? ctx.isReadOnly ? "\u6AA2\u8996\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u7533\u8ACB" : ctx.isReturned ? "\u4FEE\u6539\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u7533\u8ACB" : "\u7DE8\u8F2F\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u8349\u7A3F" : "\u65B0\u589E\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u7533\u8ACB");
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
      \u0275\u0275advance(9);
      \u0275\u0275conditional(((tmp_9_0 = ctx.form.get("startDate")) == null ? null : tmp_9_0.invalid) && ((tmp_9_0 = ctx.form.get("startDate")) == null ? null : tmp_9_0.touched) ? 44 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_10_0 = ctx.form.get("endDate")) == null ? null : tmp_10_0.invalid) && ((tmp_10_0 = ctx.form.get("endDate")) == null ? null : tmp_10_0.touched) ? 51 : -1);
      \u0275\u0275advance(5);
      \u0275\u0275conditional(ctx.holidayDaysLoading() ? 56 : ctx.holidayDaysNoCalendar() ? 57 : ctx.holidayDays() !== null ? 58 : 59);
      \u0275\u0275advance(12);
      \u0275\u0275conditional(((tmp_12_0 = ctx.form.get("purpose")) == null ? null : tmp_12_0.invalid) && ((tmp_12_0 = ctx.form.get("purpose")) == null ? null : tmp_12_0.touched) ? 68 : -1);
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.isEdit ? 69 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(!ctx.isReadOnly ? 76 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(ctx.participantEntries.length === 0 ? 78 : 79);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(!ctx.isReadOnly ? 80 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(!ctx.isReadOnly ? 87 : -1);
      \u0275\u0275advance(23);
      \u0275\u0275conditional(!ctx.isReadOnly ? 110 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275repeater(ctx.itemControls);
      \u0275\u0275advance(3);
      \u0275\u0275conditional(ctx.itemControls.length > 0 ? 115 : -1);
      \u0275\u0275advance(5);
      \u0275\u0275conditional(ctx.hasDesignatedStep && !ctx.isReadOnly ? 120 : ctx.isReadOnly && ctx.designatedEntries.length > 0 ? 121 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275property("flow", ctx.approvalFlow)("approvalRecords", ctx.approvalRecords)("currentStepOrder", ctx.taskCurrentStepOrder)("status", ctx.taskStatus);
      \u0275\u0275advance();
      \u0275\u0275conditional(!ctx.isReadOnly ? 123 : 124);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(ctx.previewFile ? 125 : -1);
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, NumberValueAccessor, SelectControlValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, FormGroupDirective, FormControlName, FormGroupName, FormArrayName, FormsModule, NgModel, RouterLink, ApprovalTimeline, FilePreviewModal, DecimalPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(HolidayTravelRequestForm, [{
    type: Component,
    args: [{ selector: "app-holiday-travel-request-form", imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, ApprovalTimeline, FilePreviewModal], template: `<div class="container-fluid py-3">
  <div class="flex items-center gap-2 mb-6">
    <a routerLink="/admin/holiday-travel-requests" class="btn btn-sm btn-outline-secondary">
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
    </a>
    <h4 class="mb-0">{{ isEdit ? (isReadOnly ? '\u6AA2\u8996\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u7533\u8ACB' : (isReturned ? '\u4FEE\u6539\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u7533\u8ACB' : '\u7DE8\u8F2F\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u8349\u7A3F')) : '\u65B0\u589E\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u7533\u8ACB' }}</h4>
  </div>

  @if (errorMsg()) {
    <div class="alert alert-danger flex items-center gap-2 mb-6 py-2" role="alert">
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>
      {{ errorMsg() }}
    </div>
  }

  <!-- \u72C0\u614B\u63D0\u793A\u5361 -->
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

        <!-- \u6D3B\u52D5\u57FA\u672C\u8CC7\u8A0A -->
        <div class="card border-0 shadow-sm">
          <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
            <svg class="sa-icon text-primary" style="stroke: currentColor">
              <use href="/assets/icons/sprite.svg#sun"></use>
            </svg>
            \u6D3B\u52D5\u57FA\u672C\u8CC7\u8A0A
          </div>
          <div class="card-body">

            <div class="row g-3 mb-4">
              <div class="col-12 col-md-6">
                <label class="form-label fw-500">\u57F7\u884C\u6D3B\u52D5\u5730\u9EDE <span class="text-danger">*</span></label>
                <input type="text" class="form-control" formControlName="destination" placeholder="\u4F8B\u5982\uFF1A\u53F0\u5357\u3001\u53F0\u4E2D">
                @if (form.get('destination')?.invalid && form.get('destination')?.touched) {
                  <div class="text-danger small mt-1">\u8ACB\u586B\u5BEB\u57F7\u884C\u6D3B\u52D5\u5730\u9EDE\u3002</div>
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
              </div>
            </div>

            <div class="row g-3 mb-4">
              <div class="col-12 col-md-4">
                <label class="form-label fw-500">\u958B\u59CB\u65E5\u671F <span class="text-danger">*</span></label>
                <input type="date" class="form-control" formControlName="startDate" (change)="onDateChange()">
                @if (form.get('startDate')?.invalid && form.get('startDate')?.touched) {
                  <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u958B\u59CB\u65E5\u671F\u3002</div>
                }
              </div>
              <div class="col-12 col-md-4">
                <label class="form-label fw-500">\u7D50\u675F\u65E5\u671F <span class="text-danger">*</span></label>
                <input type="date" class="form-control" formControlName="endDate" (change)="onDateChange()">
                @if (form.get('endDate')?.invalid && form.get('endDate')?.touched) {
                  <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u7D50\u675F\u65E5\u671F\u3002</div>
                }
              </div>
              <div class="col-12 col-md-4">
                <label class="form-label fw-500">\u5047\u65E5\u5929\u6578</label>
                <div class="form-control bg-elevated text-muted" style="cursor: default">
                  @if (holidayDaysLoading()) {
                    <span class="spinner-border spinner-border-sm me-1" role="status"></span> \u67E5\u8A62\u4E2D\u2026
                  } @else if (holidayDaysNoCalendar()) {
                    <span class="text-warning">\u884C\u4E8B\u66C6\u8CC7\u6599\u5C1A\u672A\u532F\u5165</span>
                  } @else if (holidayDays() !== null) {
                    <span class="fw-500" style="color: var(--forest)">{{ holidayDays() }} \u5929</span>
                  } @else {
                    <span>\u2014 \u8ACB\u5148\u9078\u64C7\u65E5\u671F</span>
                  }
                </div>
                <div class="text-muted small mt-1">\u7CFB\u7D71\u4F9D\u884C\u4E8B\u66C6\u8CC7\u6599\u81EA\u52D5\u8A08\u7B97</div>
              </div>
            </div>

            <div class="mb-4">
              <label class="form-label fw-500">\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9 <span class="text-danger">*</span></label>
              <textarea class="form-control" formControlName="purpose" rows="3"
                        placeholder="\u8ACB\u586B\u5BEB\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9..."></textarea>
              @if (form.get('purpose')?.invalid && form.get('purpose')?.touched) {
                <div class="text-danger small mt-1">\u8ACB\u586B\u5BEB\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9\u3002</div>
              }
            </div>

            @if (isEdit) {
              <div class="mb-0">
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

        <!-- \u53C3\u8207\u57F7\u884C\u4EBA\u54E1 -->
        <div class="card border-0 shadow-sm mt-6">
          <div class="card-header bg-transparent border-bottom flex items-center justify-between">
            <div class="flex items-center gap-2 fw-600">
              <svg class="sa-icon text-primary" style="stroke: currentColor">
                <use href="/assets/icons/sprite.svg#users"></use>
              </svg>
              \u53C3\u8207\u57F7\u884C\u4EBA\u54E1
            </div>
            @if (!isReadOnly) {
              <button type="button" class="btn btn-sm btn-outline-primary inline-flex items-center gap-1"
                      (click)="addParticipant()">
                <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
                \u65B0\u589E\u4EBA\u54E1
              </button>
            }
          </div>
          <div class="card-body">
            @if (participantEntries.length === 0) {
              <div class="text-muted small">
                @if (isReadOnly) { \u7121\u53C3\u8207\u57F7\u884C\u4EBA\u54E1\u8A18\u9304\u3002} @else { \u9EDE\u64CA\u300C\u65B0\u589E\u4EBA\u54E1\u300D\u65B0\u589E\u53C3\u8207\u57F7\u884C\u4EBA\u54E1\u3002}
              </div>
            } @else {
              @for (entry of participantEntries; track $index; let i = $index) {
                <div class="flex items-center gap-2 mb-2">
                  <span class="text-muted small" style="min-width:1.5rem">{{ i + 1 }}.</span>
                  @if (isReadOnly) {
                    <span class="small fw-500">{{ getUserName(entry.selectedUserId) }}</span>
                  } @else {
                    <select class="form-select form-select-sm" style="max-width:240px"
                            [(ngModel)]="entry.selectedUserId" [ngModelOptions]="{standalone: true}">
                      <option [ngValue]="null">\u2014 \u9078\u64C7\u4EBA\u54E1 \u2014</option>
                      @for (user of allUsers; track user.id) {
                        <option [ngValue]="user.id">{{ user.name }}</option>
                      }
                    </select>
                    <button type="button" class="btn btn-sm btn-ghost-danger" (click)="removeParticipant(i)">
                      <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#x"></use></svg>
                    </button>
                  }
                </div>
              }
            }
          </div>
        </div>

        <!-- \u4E0A\u50B3\u767C\u7968\uFF08\u7DE8\u8F2F\u6A21\u5F0F\uFF09 -->
        @if (!isReadOnly) {
          <div class="card border-0 shadow-sm mt-6">
            <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
              <svg class="sa-icon text-primary" style="stroke: currentColor">
                <use href="/assets/icons/sprite.svg#upload"></use>
              </svg>
              \u4E0A\u50B3\u767C\u7968
            </div>
            <div class="card-body">
              <label class="flex flex-col items-center justify-center rounded-3 py-4 px-4 mb-0 text-center"
                     style="cursor:pointer; border: 2px dashed var(--bs-border-color);">
                <svg class="sa-icon sa-icon-2x text-muted mb-2" style="stroke: currentColor">
                  <use href="/assets/icons/sprite.svg#upload"></use>
                </svg>
                <span class="fw-500">\u9EDE\u64CA\u4E0A\u50B3\u767C\u7968\u5716\u6A94</span>
                <span class="text-muted small mt-1">\u652F\u63F4 JPG\u3001PNG\u3001HEIC\u3001PDF\uFF0C\u53EF\u591A\u9078\u3002\u4E0A\u50B3\u5F8C\u81EA\u52D5\u65B0\u589E\u8CBB\u7528\u660E\u7D30\u884C\u4E26 OCR \u8B58\u5225</span>
                <input type="file" class="hidden" multiple accept="image/*,.heic,.heif,application/pdf"
                       (change)="onFilesSelected($event)">
              </label>
            </div>
          </div>
        }

        <!-- \u8CBB\u7528\u660E\u7D30 -->
        <div class="card border-0 shadow-sm mt-6">
          <div class="card-header bg-transparent border-bottom flex items-center justify-between">
            <div class="flex items-center gap-2 fw-600">
              <svg class="sa-icon text-primary" style="stroke: currentColor">
                <use href="/assets/icons/sprite.svg#file-text"></use>
              </svg>
              \u8CBB\u7528\u660E\u7D30
            </div>
            @if (!isReadOnly) {
              <button type="button" class="btn btn-sm btn-outline-primary inline-flex items-center gap-1"
                      (click)="addItem()">
                <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
                \u624B\u52D5\u65B0\u589E\u884C
              </button>
            }
          </div>
          <div class="card-body p-0">
            <div class="table-responsive">
              <table class="table table-sm mb-0">
                <thead class="table-light">
                  <tr>
                    <th style="width:40px"></th>
                    <th style="min-width:140px">\u767C\u7968\u865F\u78BC</th>
                    <th style="min-width:130px">\u767C\u7968\u65E5\u671F</th>
                    <th style="min-width:100px">\u5206\u985E</th>
                    <th style="min-width:160px">\u9805\u76EE\u8AAA\u660E</th>
                    <th style="min-width:100px">\u55AE\u50F9</th>
                    <th style="min-width:80px">\u6578\u91CF/\u55AE\u4F4D</th>
                    <th style="min-width:100px">\u7E3D\u50F9</th>
                    <th style="min-width:100px">\u5099\u8A3B</th>
                    @if (!isReadOnly) {
                      <th style="width:48px"></th>
                    }
                  </tr>
                </thead>
                <tbody formArrayName="items">
                  @for (ctrl of itemControls; track ctrl.get('id')?.value; let i = $index) {
                    @let isOcr = ocrLoadingIds.has(ctrl.get('id')?.value);
                    <tr [formGroupName]="i">
                      <!-- \u9810\u89BD\u5716\u793A -->
                      <td class="align-middle text-center">
                        @if (ctrl.get('previewUrl')?.value || ctrl.get('fileUrl')?.value) {
                          <button type="button" class="btn btn-sm btn-ghost-secondary p-1"
                                  (click)="openPreview(ctrl.get('fileName')?.value, ctrl.get('previewUrl')?.value || ctrl.get('fileUrl')?.value)"
                                  title="{{ ctrl.get('fileName')?.value }}">
                            <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#file-text"></use></svg>
                          </button>
                        } @else if (isOcr) {
                          <span class="spinner-border spinner-border-sm" role="status"></span>
                        }
                      </td>
                      <!-- \u767C\u7968\u865F\u78BC -->
                      <td class="align-middle">
                        @if (isOcr) {
                          <div class="flex items-center gap-1 text-muted small py-1">
                            <span class="spinner-border spinner-border-sm" role="status"></span>
                            \u8B58\u5225\u4E2D\u2026
                          </div>
                        } @else if (isReadOnly) {
                          <span class="small font-monospace">{{ ctrl.get('invoiceNo')?.value || '\u2014' }}</span>
                        } @else {
                          <input class="form-control form-control-sm font-monospace" formControlName="invoiceNo" placeholder="AB12345678">
                        }
                      </td>
                      <!-- \u767C\u7968\u65E5\u671F -->
                      <td class="align-middle">
                        @if (isOcr) {
                          <div class="flex items-center gap-1 text-muted small py-1">
                            <span class="spinner-border spinner-border-sm" role="status"></span>
                            \u8B58\u5225\u4E2D\u2026
                          </div>
                        } @else if (isReadOnly) {
                          <span class="small">{{ ctrl.get('invoiceDate')?.value || '\u2014' }}</span>
                        } @else {
                          <input type="date" class="form-control form-control-sm" formControlName="invoiceDate">
                        }
                      </td>
                      <!-- \u5206\u985E -->
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small">{{ ctrl.get('category')?.value || '\u2014' }}</span>
                        } @else {
                          <select class="form-select form-select-sm" formControlName="category">
                            <option value="">\u9078\u64C7</option>
                            @for (cat of categories; track cat) {
                              <option [value]="cat">{{ cat }}</option>
                            }
                          </select>
                        }
                      </td>
                      <!-- \u9805\u76EE\u8AAA\u660E -->
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small">{{ ctrl.get('itemName')?.value }}</span>
                        } @else {
                          <input class="form-control form-control-sm" formControlName="itemName" placeholder="\u9805\u76EE\u8AAA\u660E">
                        }
                      </td>
                      <!-- \u55AE\u50F9 -->
                      <td class="align-middle">
                        @if (isOcr) {
                          <div class="py-1 text-muted small">\u2014</div>
                        } @else if (isReadOnly) {
                          <span class="small">{{ ctrl.get('unitPrice')?.value | number:'1.0-0' }}</span>
                        } @else {
                          <input type="number" class="form-control form-control-sm" formControlName="unitPrice" min="0"
                                 (input)="calcTotal(ctrl)">
                        }
                      </td>
                      <!-- \u6578\u91CF -->
                      <td class="align-middle">
                        @if (isOcr) {
                          <div class="py-1 text-muted small">\u2014</div>
                        } @else if (isReadOnly) {
                          <span class="small">{{ ctrl.get('quantity')?.value }}</span>
                        } @else {
                          <input class="form-control form-control-sm" formControlName="quantity" placeholder="\u5982\uFF1A1\u5F0F"
                                 (input)="calcTotal(ctrl)">
                        }
                      </td>
                      <!-- \u7E3D\u50F9 -->
                      <td class="align-middle">
                        @if (isOcr) {
                          <div class="py-1 text-muted small">\u2014</div>
                        } @else if (isReadOnly) {
                          <span class="small fw-500">{{ ctrl.get('totalPrice')?.value | number:'1.0-0' }}</span>
                        } @else {
                          <input type="number" class="form-control form-control-sm" formControlName="totalPrice" min="0">
                        }
                      </td>
                      <!-- \u5099\u8A3B -->
                      <td class="align-middle">
                        @if (isReadOnly) {
                          <span class="small">{{ ctrl.get('note')?.value || '\u2014' }}</span>
                        } @else {
                          <input class="form-control form-control-sm" formControlName="note" placeholder="">
                        }
                      </td>
                      @if (!isReadOnly) {
                        <td class="text-right align-middle">
                          <button type="button" class="btn btn-sm btn-ghost-danger inline-flex items-center"
                                  [disabled]="isOcr" (click)="removeItem(i)">
                            <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#x"></use></svg>
                          </button>
                        </td>
                      }
                    </tr>
                  } @empty {
                    <tr>
                      <td [attr.colspan]="isReadOnly ? 9 : 10" class="text-center text-muted py-4 small">
                        @if (isReadOnly) { \u5C1A\u7121\u8CBB\u7528\u660E\u7D30\u3002} @else { \u8ACB\u4E0A\u50B3\u767C\u7968\u5716\u6A94\uFF0C\u6216\u9EDE\u64CA\u300C\u624B\u52D5\u65B0\u589E\u884C\u300D\u3002}
                      </td>
                    </tr>
                  }
                </tbody>
                @if (itemControls.length > 0) {
                  <tfoot>
                    <tr class="table-light">
                      <td colspan="7" class="text-right fw-500 small">\u5408\u8A08</td>
                      <td class="fw-600">{{ grandTotal | number:'1.0-0' }}</td>
                      <td></td>
                      @if (!isReadOnly) { <td></td> }
                    </tr>
                  </tfoot>
                }
              </table>
            </div>
            <div class="px-3 py-2 flex items-center gap-2 text-info small">
              <svg class="sa-icon shrink-0" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#info"></use></svg>
              \u82E5\u57F7\u884C\u8CBB\u7528\u5DF2\u7533\u8ACB\u9810\u652F\uFF0C\u8ACB\u4EE5\u9810\u652F\u6C96\u92B7\u7533\u8ACB\uFF0C\u5176\u4ED6\u672A\u7533\u8ACB\u9810\u652F\u7522\u751F\u4E4B\u8CBB\u7528\uFF0C\u53EF\u65BC\u6B64\u7533\u8ACB\u3002
            </div>
          </div>
        </div>

        <!-- \u6307\u5B9A\u5BE9\u6838\u8005\uFF08\u6709\u8A2D\u5B9A\u6307\u5B9A\u6B65\u9A5F\u6642\u624D\u986F\u793A\uFF09 -->
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
              <label class="form-label fw-500">\u6307\u5B9A\u5BE9\u6838\u8005\uFF08\u4F9D\u5E8F\u5BE9\u6838\uFF09</label>
              <ol class="mb-0 ps-4">
                @for (entry of designatedEntries; track $index) {
                  <li class="small">{{ getUserName(entry.selectedUserId) }}</li>
                }
              </ol>
            </div>
          </div>
        }

        <!-- \u7C3D\u6838\u6D41\u7A0B\u6642\u9593\u8EF8 -->
        <app-approval-timeline
          [flow]="approvalFlow"
          [approvalRecords]="approvalRecords"
          [currentStepOrder]="taskCurrentStepOrder"
          [status]="taskStatus" />

        <!-- \u5E95\u90E8\u6309\u9215 -->
        @if (!isReadOnly) {
          <div class="mt-6 flex gap-2">
            <button type="submit" class="btn btn-outline-secondary"
                    [disabled]="disabledReason !== null">
              {{ isEdit ? '\u5132\u5B58' : '\u5132\u5B58\u8349\u7A3F' }}
            </button>
            <button type="button" class="btn btn-primary"
                    [disabled]="disabledReason !== null"
                    (click)="submitForApproval()">
              @if (isAnyOcrPending) {
                <span class="spinner-border spinner-border-sm me-1" role="status"></span>
              }
              \u9001\u51FA\u7533\u8ACB
            </button>
            <a routerLink="/admin/holiday-travel-requests" class="btn btn-outline-secondary">\u53D6\u6D88</a>
          </div>
          @if (disabledReason; as reason) {
            <div class="text-warning small mt-2 flex items-center gap-1">
              <svg class="sa-icon shrink-0" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>
              {{ reason }}
            </div>
          }
        } @else {
          <div class="mt-6">
            <a routerLink="/admin/holiday-travel-requests" class="btn btn-outline-secondary">\u8FD4\u56DE\u5217\u8868</a>
          </div>
        }

      </div>
    </div>
  </form>
</div>

@if (previewFile) {
  <app-file-preview-modal [file]="previewFile" (closed)="closePreview()" />
}

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
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(HolidayTravelRequestForm, { className: "HolidayTravelRequestForm", filePath: "src/app/features/admin/holiday-travel-requests/pages/holiday-travel-request-form/holiday-travel-request-form.ts", lineNumber: 38 });
})();
export {
  HolidayTravelRequestForm
};
//# sourceMappingURL=chunk-ELIGXR4E.js.map
