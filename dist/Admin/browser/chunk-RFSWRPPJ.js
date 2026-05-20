import {
  ApprovalService,
  JobTitleService,
  NgbModal,
  ProjectService,
  UserService
} from "./chunk-TWHKNLSN.js";
import {
  APPROVAL_STATUS_CLASSES,
  APPROVAL_STATUS_LABELS,
  HolidayTravelRequestService
} from "./chunk-X5WVMTZ5.js";
import "./chunk-W4RXF7YW.js";
import {
  DefaultValueAccessor,
  FormBuilder,
  FormControlName,
  FormGroupDirective,
  FormsModule,
  NgControlStatus,
  NgControlStatusGroup,
  NgModel,
  NgSelectOption,
  ReactiveFormsModule,
  SelectControlValueAccessor,
  Validators,
  ɵNgNoValidate,
  ɵNgSelectMultipleOption
} from "./chunk-4LFECYTV.js";
import {
  ApprovalTimeline
} from "./chunk-B4OWGIJG.js";
import {
  ApprovalTaskService
} from "./chunk-HXO5P7BO.js";
import {
  ActivatedRoute,
  Router,
  RouterLink
} from "./chunk-DUW2WF5C.js";
import "./chunk-JDEYLUO2.js";
import {
  ChangeDetectorRef,
  Component,
  ViewChild,
  inject,
  setClassMetadata,
  signal,
  viewChild,
  ɵsetClassDebugInfo,
  ɵɵadvance,
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
  ɵɵproperty,
  ɵɵpureFunction0,
  ɵɵqueryAdvance,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
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
} from "./chunk-IFQ7CN6S.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/admin/holiday-travel-requests/pages/holiday-travel-request-form/holiday-travel-request-form.ts
var _c0 = ["successModal"];
var _c1 = () => ({ standalone: true });
var _forTrack0 = ($index, $item) => $item.id;
function HolidayTravelRequestForm_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 7);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 42);
    \u0275\u0275element(2, "use", 43);
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
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 44);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 42);
    \u0275\u0275element(3, "use", 45);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5BE9\u6838\u4E2D\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function HolidayTravelRequestForm_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 46);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 42);
    \u0275\u0275element(3, "use", 43);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5DF2\u88AB\u9000\u56DE\uFF0C\u8ACB\u4FEE\u6539\u5F8C\u91CD\u65B0\u9001\u51FA\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function HolidayTravelRequestForm_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 47);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 42);
    \u0275\u0275element(3, "use", 48);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6B64\u7533\u8ACB\u5DF2\u6838\u51C6\uFF0C\u4E0D\u53EF\u518D\u4FEE\u6539\u3002 ");
    \u0275\u0275elementEnd()();
  }
}
function HolidayTravelRequestForm_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8)(1, "div", 49);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 42);
    \u0275\u0275element(3, "use", 50);
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
function HolidayTravelRequestForm_Conditional_37_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 25);
    \u0275\u0275text(1, "\u60A8\u76EE\u524D\u53EF\u7533\u8ACB\u7684\u5C08\u6848\u6E05\u55AE\u70BA\u7A7A\uFF0C\u8ACB\u806F\u7D61\u4E3B\u7BA1\u6216\u78BA\u8A8D\u90E8\u9580\u8A2D\u5B9A\u3002");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_45_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u958B\u59CB\u65E5\u671F\u3002");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_52_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u7D50\u675F\u65E5\u671F\u3002");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_57_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 51);
    \u0275\u0275text(1, " \u67E5\u8A62\u4E2D\u2026 ");
  }
}
function HolidayTravelRequestForm_Conditional_58_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 30);
    \u0275\u0275text(1, "\u884C\u4E8B\u66C6\u8CC7\u6599\u5C1A\u672A\u532F\u5165");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_59_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 31);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("", ctx_r1.holidayDays(), " \u5929");
  }
}
function HolidayTravelRequestForm_Conditional_60_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span");
    \u0275\u0275text(1, "\u2014 \u8ACB\u5148\u9078\u64C7\u65E5\u671F");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_69_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1, "\u8ACB\u586B\u5BEB\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9\u3002");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_70_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 6)(1, "label", 19);
    \u0275\u0275text(2, "\u7C3D\u6838\u72C0\u614B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div")(4, "span", 52);
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
function HolidayTravelRequestForm_Conditional_77_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 53);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_77_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.addParticipant());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 42);
    \u0275\u0275element(2, "use", 54);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u65B0\u589E\u4EBA\u54E1 ");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelRequestForm_Conditional_79_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u7121\u53C3\u8207\u57F7\u884C\u4EBA\u54E1\u8A18\u9304\u3002");
  }
}
function HolidayTravelRequestForm_Conditional_79_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u9EDE\u64CA\u300C\u65B0\u589E\u4EBA\u54E1\u300D\u65B0\u589E\u53C3\u8207\u57F7\u884C\u4EBA\u54E1\u3002");
  }
}
function HolidayTravelRequestForm_Conditional_79_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 39);
    \u0275\u0275conditionalCreate(1, HolidayTravelRequestForm_Conditional_79_Conditional_1_Template, 1, 0)(2, HolidayTravelRequestForm_Conditional_79_Conditional_2_Template, 1, 0);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 1 : 2);
  }
}
function HolidayTravelRequestForm_Conditional_80_For_1_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 57);
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
function HolidayTravelRequestForm_Conditional_80_For_1_Conditional_4_For_4_Template(rf, ctx) {
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
function HolidayTravelRequestForm_Conditional_80_For_1_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    const _r6 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "select", 58);
    \u0275\u0275twoWayListener("ngModelChange", function HolidayTravelRequestForm_Conditional_80_For_1_Conditional_4_Template_select_ngModelChange_0_listener($event) {
      \u0275\u0275restoreView(_r6);
      const entry_r5 = \u0275\u0275nextContext().$implicit;
      \u0275\u0275twoWayBindingSet(entry_r5.selectedUserId, $event) || (entry_r5.selectedUserId = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementStart(1, "option", 24);
    \u0275\u0275text(2, "\u2014 \u9078\u64C7\u4EBA\u54E1 \u2014");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(3, HolidayTravelRequestForm_Conditional_80_For_1_Conditional_4_For_4_Template, 2, 2, "option", 24, _forTrack0);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "button", 59);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_80_For_1_Conditional_4_Template_button_click_5_listener() {
      \u0275\u0275restoreView(_r6);
      const \u0275$index_223_r8 = \u0275\u0275nextContext().$index;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.removeParticipant(\u0275$index_223_r8));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(6, "svg", 42);
    \u0275\u0275element(7, "use", 60);
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
function HolidayTravelRequestForm_Conditional_80_For_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 55)(1, "span", 56);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(3, HolidayTravelRequestForm_Conditional_80_For_1_Conditional_3_Template, 2, 1, "span", 57)(4, HolidayTravelRequestForm_Conditional_80_For_1_Conditional_4_Template, 8, 4);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const \u0275$index_223_r8 = ctx.$index;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1("", \u0275$index_223_r8 + 1, ".");
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.isReadOnly ? 3 : 4);
  }
}
function HolidayTravelRequestForm_Conditional_80_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275repeaterCreate(0, HolidayTravelRequestForm_Conditional_80_For_1_Template, 5, 2, "div", 55, \u0275\u0275repeaterTrackByIndex);
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275repeater(ctx_r1.participantEntries);
  }
}
function HolidayTravelRequestForm_Conditional_81_For_10_For_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 24);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const jt_r13 = ctx.$implicit;
    \u0275\u0275property("ngValue", jt_r13.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(jt_r13.name);
  }
}
function HolidayTravelRequestForm_Conditional_81_For_10_For_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 24);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const user_r14 = ctx.$implicit;
    \u0275\u0275property("ngValue", user_r14.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(user_r14.name);
  }
}
function HolidayTravelRequestForm_Conditional_81_For_10_Template(rf, ctx) {
  if (rf & 1) {
    const _r10 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 55)(1, "span", 56);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "select", 62);
    \u0275\u0275twoWayListener("ngModelChange", function HolidayTravelRequestForm_Conditional_81_For_10_Template_select_ngModelChange_3_listener($event) {
      const entry_r11 = \u0275\u0275restoreView(_r10).$implicit;
      \u0275\u0275twoWayBindingSet(entry_r11.selectedJobTitleId, $event) || (entry_r11.selectedJobTitleId = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275listener("ngModelChange", function HolidayTravelRequestForm_Conditional_81_For_10_Template_select_ngModelChange_3_listener() {
      const \u0275$index_266_r12 = \u0275\u0275restoreView(_r10).$index;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.onEntryJobTitleChange(\u0275$index_266_r12));
    });
    \u0275\u0275elementStart(4, "option", 24);
    \u0275\u0275text(5, "\u2014 \u8077\u7A31 \u2014");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(6, HolidayTravelRequestForm_Conditional_81_For_10_For_7_Template, 2, 2, "option", 24, _forTrack0);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "select", 63);
    \u0275\u0275twoWayListener("ngModelChange", function HolidayTravelRequestForm_Conditional_81_For_10_Template_select_ngModelChange_8_listener($event) {
      const entry_r11 = \u0275\u0275restoreView(_r10).$implicit;
      \u0275\u0275twoWayBindingSet(entry_r11.selectedUserId, $event) || (entry_r11.selectedUserId = $event);
      return \u0275\u0275resetView($event);
    });
    \u0275\u0275elementStart(9, "option", 24);
    \u0275\u0275text(10, "\u2014 \u4EBA\u54E1 \u2014");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(11, HolidayTravelRequestForm_Conditional_81_For_10_For_12_Template, 2, 2, "option", 24, _forTrack0);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "button", 59);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_81_For_10_Template_button_click_13_listener() {
      const \u0275$index_266_r12 = \u0275\u0275restoreView(_r10).$index;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.removeDesignatedEntry(\u0275$index_266_r12));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(14, "svg", 42);
    \u0275\u0275element(15, "use", 60);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const entry_r11 = ctx.$implicit;
    const \u0275$index_266_r12 = ctx.$index;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1("", \u0275$index_266_r12 + 1, ".");
    \u0275\u0275advance();
    \u0275\u0275twoWayProperty("ngModel", entry_r11.selectedJobTitleId);
    \u0275\u0275property("ngModelOptions", \u0275\u0275pureFunction0(7, _c1));
    \u0275\u0275advance();
    \u0275\u0275property("ngValue", null);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(ctx_r1.jobTitles);
    \u0275\u0275advance(2);
    \u0275\u0275twoWayProperty("ngModel", entry_r11.selectedUserId);
    \u0275\u0275property("ngModelOptions", \u0275\u0275pureFunction0(8, _c1));
    \u0275\u0275advance();
    \u0275\u0275property("ngValue", null);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(entry_r11.filteredUsers);
  }
}
function HolidayTravelRequestForm_Conditional_81_Template(rf, ctx) {
  if (rf & 1) {
    const _r9 = \u0275\u0275getCurrentView();
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
    \u0275\u0275repeaterCreate(9, HolidayTravelRequestForm_Conditional_81_For_10_Template, 16, 9, "div", 55, \u0275\u0275repeaterTrackByIndex);
    \u0275\u0275elementStart(11, "button", 61);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_81_Template_button_click_11_listener() {
      \u0275\u0275restoreView(_r9);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.addDesignatedEntry());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(12, "svg", 42);
    \u0275\u0275element(13, "use", 54);
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
function HolidayTravelRequestForm_Conditional_82_For_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "li", 65);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const entry_r15 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.getUserName(entry_r15.selectedUserId));
  }
}
function HolidayTravelRequestForm_Conditional_82_Template(rf, ctx) {
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
    \u0275\u0275elementStart(8, "ol", 64);
    \u0275\u0275repeaterCreate(9, HolidayTravelRequestForm_Conditional_82_For_10_Template, 2, 1, "li", 65, \u0275\u0275repeaterTrackByIndex);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(9);
    \u0275\u0275repeater(ctx_r1.designatedEntries);
  }
}
function HolidayTravelRequestForm_Conditional_84_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 70);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 71);
    \u0275\u0275element(2, "use", 43);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" ", ctx, " ");
  }
}
function HolidayTravelRequestForm_Conditional_84_Template(rf, ctx) {
  if (rf & 1) {
    const _r16 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 66)(1, "button", 67);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "button", 68);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_Conditional_84_Template_button_click_3_listener() {
      \u0275\u0275restoreView(_r16);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.submitForApproval());
    });
    \u0275\u0275text(4, " \u9001\u51FA\u7533\u8ACB ");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "a", 69);
    \u0275\u0275text(6, "\u53D6\u6D88");
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(7, HolidayTravelRequestForm_Conditional_84_Conditional_7_Template, 4, 1, "div", 70);
  }
  if (rf & 2) {
    let tmp_5_0;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r1.disabledReason !== null);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.isEdit ? "\u5132\u5B58" : "\u5132\u5B58\u8349\u7A3F", " ");
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r1.disabledReason !== null);
    \u0275\u0275advance(4);
    \u0275\u0275conditional((tmp_5_0 = ctx_r1.disabledReason) ? 7 : -1, tmp_5_0);
  }
}
function HolidayTravelRequestForm_Conditional_85_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 41)(1, "a", 69);
    \u0275\u0275text(2, "\u8FD4\u56DE\u5217\u8868");
    \u0275\u0275elementEnd()();
  }
}
function HolidayTravelRequestForm_ng_template_86_Template(rf, ctx) {
  if (rf & 1) {
    const _r17 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 72)(1, "button", 73);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_ng_template_86_Template_button_click_1_listener() {
      const modal_r18 = \u0275\u0275restoreView(_r17).$implicit;
      return \u0275\u0275resetView(modal_r18.close());
    });
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(2, "div", 74);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(3, "svg", 75);
    \u0275\u0275element(4, "use", 48);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "h5", 76);
    \u0275\u0275text(6, "\u7533\u8ACB\u6210\u529F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "p", 77);
    \u0275\u0275text(8, "\u8ACB\u76E1\u65E9\u5C07\u6B63\u672C\u8CC7\u6599\u9001\u56DE\u7BA1\u7406\u8655");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(9, "div", 78)(10, "button", 79);
    \u0275\u0275listener("click", function HolidayTravelRequestForm_ng_template_86_Template_button_click_10_listener() {
      const modal_r18 = \u0275\u0275restoreView(_r17).$implicit;
      return \u0275\u0275resetView(modal_r18.close());
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
  statusLabel = APPROVAL_STATUS_LABELS;
  statusClass = APPROVAL_STATUS_CLASSES;
  form = this.fb.group({
    destination: ["", Validators.required],
    startDate: ["", Validators.required],
    endDate: ["", Validators.required],
    purpose: ["", Validators.required],
    projectId: [null]
  });
  /** 按鈕 disabled 時的提示訊息，null 表示可提交 */
  get disabledReason() {
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
  ngOnInit() {
    this.userSvc.getLookup().subscribe({
      next: (users) => {
        this.allUsers = users;
        this.cdr.markForCheck();
      }
    });
    this.approvalSvc.getActiveByType("holiday_travel").subscribe((flow) => {
      this.hasDesignatedStep = flow?.steps.some((s) => s.useApplicantDesignated) ?? false;
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
    if (this.form.invalid || this.isReadOnly)
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
    if (this.form.invalid || this.isReadOnly)
      return;
    if (this.hasDesignatedStep) {
      const validEntries = this.designatedEntries.filter((e) => e.selectedUserId);
      if (validEntries.length === 0) {
        this.errorMsg.set("\u6B64\u7C3D\u6838\u6D41\u7A0B\u5305\u542B\u7533\u8ACB\u4EBA\u6307\u5B9A\u5BE9\u6838\u6B65\u9A5F\uFF0C\u8ACB\u65BC\u4E0B\u65B9\u300C\u6307\u5B9A\u5BE9\u6838\u8005\u300D\u5340\u584A\u65B0\u589E\u81F3\u5C11 1 \u4F4D\u5BE9\u6838\u8005\u3002");
        return;
      }
    }
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
    return fd;
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
  }, decls: 88, vars: 21, consts: [["successModal", ""], [1, "container-fluid", "py-3"], [1, "flex", "items-center", "gap-2", "mb-6"], ["routerLink", "/admin/holiday-travel-requests", 1, "btn", "btn-sm", "btn-outline-secondary"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], ["role", "alert", 1, "alert", "alert-danger", "flex", "items-center", "gap-2", "mb-6", "py-2"], [1, "card", "border-0", "shadow-sm", "mb-6"], [3, "ngSubmit", "formGroup"], [1, "row", "g-4"], [1, "col-12", "col-lg-10", "col-xl-8"], [1, "card", "border-0", "shadow-sm"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#sun"], [1, "card-body"], [1, "row", "g-3", "mb-4"], [1, "col-12", "col-md-6"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "destination", "placeholder", "\u4F8B\u5982\uFF1A\u53F0\u5357\u3001\u53F0\u4E2D", 1, "form-control"], [1, "text-danger", "small", "mt-1"], ["formControlName", "projectId", 1, "form-select"], [3, "ngValue"], [1, "text-muted", "small", "mt-1"], [1, "col-12", "col-md-4"], ["type", "date", "formControlName", "startDate", 1, "form-control", 3, "change"], ["type", "date", "formControlName", "endDate", 1, "form-control", 3, "change"], [1, "form-control", "bg-elevated", "text-muted", 2, "cursor", "default"], [1, "text-warning"], [1, "fw-500", 2, "color", "var(--forest)"], [1, "mb-4"], ["formControlName", "purpose", "rows", "3", "placeholder", "\u8ACB\u586B\u5BEB\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9...", 1, "form-control"], [1, "card", "border-0", "shadow-sm", "mt-6"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "justify-between"], [1, "flex", "items-center", "gap-2", "fw-600"], ["href", "/assets/icons/sprite.svg#users"], ["type", "button", 1, "btn", "btn-sm", "btn-outline-primary", "inline-flex", "items-center", "gap-1"], [1, "text-muted", "small"], [3, "flow", "approvalRecords", "currentStepOrder", "status"], [1, "mt-6"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#alert-triangle"], [1, "card-header", "bg-[rgba(13,110,253,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-primary", "py-3"], ["href", "/assets/icons/sprite.svg#clock"], [1, "card-header", "bg-[rgba(255,193,7,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-warning", "py-3"], [1, "card-header", "bg-[rgba(37,162,68,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-success", "py-3"], ["href", "/assets/icons/sprite.svg#check-circle"], [1, "card-header", "bg-[rgba(220,53,69,0.08)]", "border-bottom", "flex", "items-center", "gap-2", "fw-600", "text-danger", "py-3"], ["href", "/assets/icons/sprite.svg#x-circle"], ["role", "status", 1, "spinner-border", "spinner-border-sm", "me-1"], [1, "badge", "rounded-pill", "px-3", "py-2"], ["type", "button", 1, "btn", "btn-sm", "btn-outline-primary", "inline-flex", "items-center", "gap-1", 3, "click"], ["href", "/assets/icons/sprite.svg#plus"], [1, "flex", "items-center", "gap-2", "mb-2"], [1, "text-muted", "small", 2, "min-width", "1.5rem"], [1, "small", "fw-500"], [1, "form-select", "form-select-sm", 2, "max-width", "240px", 3, "ngModelChange", "ngModel", "ngModelOptions"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-danger", 3, "click"], ["href", "/assets/icons/sprite.svg#x"], ["type", "button", 1, "btn", "btn-sm", "btn-outline-secondary", "mt-1", 3, "click"], [1, "form-select", "form-select-sm", 2, "max-width", "160px", 3, "ngModelChange", "ngModel", "ngModelOptions"], [1, "form-select", "form-select-sm", 2, "max-width", "200px", 3, "ngModelChange", "ngModel", "ngModelOptions"], [1, "mb-0", "ps-4"], [1, "small"], [1, "mt-6", "flex", "gap-2"], ["type", "submit", 1, "btn", "btn-outline-secondary", 3, "disabled"], ["type", "button", 1, "btn", "btn-primary", 3, "click", "disabled"], ["routerLink", "/admin/holiday-travel-requests", 1, "btn", "btn-outline-secondary"], [1, "text-warning", "small", "mt-2", "flex", "items-center", "gap-1"], [1, "sa-icon", "shrink-0", 2, "stroke", "currentColor"], [1, "modal-header", "border-0", "pb-0"], ["type", "button", 1, "btn-close", 3, "click"], [1, "modal-body", "text-center", "py-6"], [1, "sa-icon", "sa-icon-3x", "text-success", "mb-4", 2, "stroke", "currentColor"], [1, "fw-600", "mb-2"], [1, "text-secondary", "mb-0"], [1, "modal-footer", "border-0", "justify-center", "pt-0"], ["type", "button", 1, "btn", "btn-primary", "px-6", 3, "click"]], template: function HolidayTravelRequestForm_Template(rf, ctx) {
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
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(37, HolidayTravelRequestForm_Conditional_37_Template, 2, 0, "div", 25);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(38, "div", 17)(39, "div", 26)(40, "label", 19);
      \u0275\u0275text(41, "\u958B\u59CB\u65E5\u671F ");
      \u0275\u0275elementStart(42, "span", 20);
      \u0275\u0275text(43, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(44, "input", 27);
      \u0275\u0275listener("change", function HolidayTravelRequestForm_Template_input_change_44_listener() {
        \u0275\u0275restoreView(_r1);
        return \u0275\u0275resetView(ctx.onDateChange());
      });
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(45, HolidayTravelRequestForm_Conditional_45_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(46, "div", 26)(47, "label", 19);
      \u0275\u0275text(48, "\u7D50\u675F\u65E5\u671F ");
      \u0275\u0275elementStart(49, "span", 20);
      \u0275\u0275text(50, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(51, "input", 28);
      \u0275\u0275listener("change", function HolidayTravelRequestForm_Template_input_change_51_listener() {
        \u0275\u0275restoreView(_r1);
        return \u0275\u0275resetView(ctx.onDateChange());
      });
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(52, HolidayTravelRequestForm_Conditional_52_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(53, "div", 26)(54, "label", 19);
      \u0275\u0275text(55, "\u5047\u65E5\u5929\u6578");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(56, "div", 29);
      \u0275\u0275conditionalCreate(57, HolidayTravelRequestForm_Conditional_57_Template, 2, 0)(58, HolidayTravelRequestForm_Conditional_58_Template, 2, 0, "span", 30)(59, HolidayTravelRequestForm_Conditional_59_Template, 2, 1, "span", 31)(60, HolidayTravelRequestForm_Conditional_60_Template, 2, 0, "span");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(61, "div", 25);
      \u0275\u0275text(62, "\u7CFB\u7D71\u4F9D\u884C\u4E8B\u66C6\u8CC7\u6599\u81EA\u52D5\u8A08\u7B97");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(63, "div", 32)(64, "label", 19);
      \u0275\u0275text(65, "\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9 ");
      \u0275\u0275elementStart(66, "span", 20);
      \u0275\u0275text(67, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(68, "textarea", 33);
      \u0275\u0275conditionalCreate(69, HolidayTravelRequestForm_Conditional_69_Template, 2, 0, "div", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(70, HolidayTravelRequestForm_Conditional_70_Template, 6, 3, "div", 6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(71, "div", 34)(72, "div", 35)(73, "div", 36);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(74, "svg", 14);
      \u0275\u0275element(75, "use", 37);
      \u0275\u0275elementEnd();
      \u0275\u0275text(76, " \u53C3\u8207\u57F7\u884C\u4EBA\u54E1 ");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(77, HolidayTravelRequestForm_Conditional_77_Template, 4, 0, "button", 38);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(78, "div", 16);
      \u0275\u0275conditionalCreate(79, HolidayTravelRequestForm_Conditional_79_Template, 3, 1, "div", 39)(80, HolidayTravelRequestForm_Conditional_80_Template, 2, 0);
      \u0275\u0275elementEnd()();
      \u0275\u0275conditionalCreate(81, HolidayTravelRequestForm_Conditional_81_Template, 15, 0, "div", 34)(82, HolidayTravelRequestForm_Conditional_82_Template, 11, 0, "div", 34);
      \u0275\u0275element(83, "app-approval-timeline", 40);
      \u0275\u0275conditionalCreate(84, HolidayTravelRequestForm_Conditional_84_Template, 8, 4)(85, HolidayTravelRequestForm_Conditional_85_Template, 3, 0, "div", 41);
      \u0275\u0275elementEnd()()()();
      \u0275\u0275template(86, HolidayTravelRequestForm_ng_template_86_Template, 12, 0, "ng-template", null, 0, \u0275\u0275templateRefExtractor);
    }
    if (rf & 2) {
      let tmp_5_0;
      let tmp_10_0;
      let tmp_11_0;
      let tmp_13_0;
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
      \u0275\u0275advance(2);
      \u0275\u0275conditional(!ctx.loadingProjects && ctx.projects.length === 0 ? 37 : -1);
      \u0275\u0275advance(8);
      \u0275\u0275conditional(((tmp_10_0 = ctx.form.get("startDate")) == null ? null : tmp_10_0.invalid) && ((tmp_10_0 = ctx.form.get("startDate")) == null ? null : tmp_10_0.touched) ? 45 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_11_0 = ctx.form.get("endDate")) == null ? null : tmp_11_0.invalid) && ((tmp_11_0 = ctx.form.get("endDate")) == null ? null : tmp_11_0.touched) ? 52 : -1);
      \u0275\u0275advance(5);
      \u0275\u0275conditional(ctx.holidayDaysLoading() ? 57 : ctx.holidayDaysNoCalendar() ? 58 : ctx.holidayDays() !== null ? 59 : 60);
      \u0275\u0275advance(12);
      \u0275\u0275conditional(((tmp_13_0 = ctx.form.get("purpose")) == null ? null : tmp_13_0.invalid) && ((tmp_13_0 = ctx.form.get("purpose")) == null ? null : tmp_13_0.touched) ? 69 : -1);
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.isEdit ? 70 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(!ctx.isReadOnly ? 77 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(ctx.participantEntries.length === 0 ? 79 : 80);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(ctx.hasDesignatedStep && !ctx.isReadOnly ? 81 : ctx.isReadOnly && ctx.designatedEntries.length > 0 ? 82 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275property("flow", ctx.approvalFlow)("approvalRecords", ctx.approvalRecords)("currentStepOrder", ctx.taskCurrentStepOrder)("status", ctx.taskStatus);
      \u0275\u0275advance();
      \u0275\u0275conditional(!ctx.isReadOnly ? 84 : 85);
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, SelectControlValueAccessor, NgControlStatus, NgControlStatusGroup, FormGroupDirective, FormControlName, FormsModule, NgModel, RouterLink, ApprovalTimeline], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(HolidayTravelRequestForm, [{
    type: Component,
    args: [{ selector: "app-holiday-travel-request-form", imports: [ReactiveFormsModule, FormsModule, RouterLink, ApprovalTimeline], template: `<div class="container-fluid py-3">
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
      <div class="col-12 col-lg-10 col-xl-8">

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
                @if (!loadingProjects && projects.length === 0) {
                  <div class="text-muted small mt-1">\u60A8\u76EE\u524D\u53EF\u7533\u8ACB\u7684\u5C08\u6848\u6E05\u55AE\u70BA\u7A7A\uFF0C\u8ACB\u806F\u7D61\u4E3B\u7BA1\u6216\u78BA\u8A8D\u90E8\u9580\u8A2D\u5B9A\u3002</div>
                }
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
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(HolidayTravelRequestForm, { className: "HolidayTravelRequestForm", filePath: "src/app/features/admin/holiday-travel-requests/pages/holiday-travel-request-form/holiday-travel-request-form.ts", lineNumber: 31 });
})();
export {
  HolidayTravelRequestForm
};
//# sourceMappingURL=chunk-RFSWRPPJ.js.map
