import {
  ActivatedRoute,
  AsyncPipe,
  CheckboxControlValueAccessor,
  CurrencyPipe,
  DatePipe,
  DecimalPipe,
  DefaultValueAccessor,
  FormArrayName,
  FormBuilder,
  FormControlName,
  FormGroupDirective,
  FormGroupName,
  HttpClient,
  MaxValidator,
  MinValidator,
  NgControlStatus,
  NgControlStatusGroup,
  NgSelectOption,
  NumberValueAccessor,
  RadioControlValueAccessor,
  ReactiveFormsModule,
  Router,
  RouterLink,
  SelectControlValueAccessor,
  Validators,
  ɵNgNoValidate,
  ɵNgSelectMultipleOption
} from "./chunk-TA2INFZE.js";
import {
  BehaviorSubject,
  Component,
  Injectable,
  __objRest,
  __spreadProps,
  __spreadValues,
  inject,
  of,
  setClassMetadata,
  take,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵattribute,
  ɵɵclassMap,
  ɵɵclassProp,
  ɵɵconditional,
  ɵɵconditionalBranchCreate,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdefineInjectable,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵgetCurrentView,
  ɵɵlistener,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind1,
  ɵɵpipeBind2,
  ɵɵpipeBind4,
  ɵɵproperty,
  ɵɵpureFunction1,
  ɵɵpureFunction2,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵrepeaterTrackByIdentity,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵsanitizeUrl,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1,
  ɵɵtextInterpolate2
} from "./chunk-U2WTJ2FP.js";

// src/app/features/admin/users/services/user.service.ts
var MOCK_USERS = [
  {
    id: "1",
    name: "Alice Chen",
    email: "alice@example.com",
    roleIds: ["admin"],
    status: "active",
    departmentId: 1,
    departmentName: "\u7BA1\u7406\u90E8",
    jobTitleId: 4,
    jobTitleName: "\u90E8\u9580\u4E3B\u7BA1",
    hireDate: /* @__PURE__ */ new Date("2022-03-01"),
    baseSalary: 8e4,
    createdAt: /* @__PURE__ */ new Date("2024-01-01")
  },
  {
    id: "2",
    name: "Bob Wang",
    email: "bob@example.com",
    roleIds: ["manager"],
    status: "active",
    departmentId: 2,
    departmentName: "\u8CC7\u8A0A\u90E8",
    jobTitleId: 2,
    jobTitleName: "\u8CC7\u6DF1\u5DE5\u7A0B\u5E2B",
    hireDate: /* @__PURE__ */ new Date("2023-06-15"),
    baseSalary: 6e4,
    agentUserId: "1",
    agentName: "Alice Chen",
    createdAt: /* @__PURE__ */ new Date("2024-02-10")
  },
  {
    id: "3",
    name: "Carol Liu",
    email: "carol@example.com",
    roleIds: ["viewer"],
    status: "inactive",
    departmentId: 3,
    departmentName: "\u696D\u52D9\u90E8",
    jobTitleId: 1,
    jobTitleName: "\u5DE5\u7A0B\u5E2B",
    hireDate: /* @__PURE__ */ new Date("2023-09-01"),
    resignDate: /* @__PURE__ */ new Date("2025-01-31"),
    baseSalary: 5e4,
    createdAt: /* @__PURE__ */ new Date("2024-03-05")
  }
];
var UserService = class _UserService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_USERS);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id) {
    return of(this.items$.getValue().find((u) => u.id === id));
  }
  create(data) {
    const item = __spreadProps(__spreadValues({}, data), { id: Date.now().toString(), createdAt: /* @__PURE__ */ new Date() });
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }
  update(id, changes) {
    const updated = this.items$.getValue().map((u) => u.id === id ? __spreadValues(__spreadValues({}, u), changes) : u);
    this.items$.next(updated);
    return of(updated.find((u) => u.id === id));
  }
  delete(id) {
    this.items$.next(this.items$.getValue().filter((u) => u.id !== id));
    return of(void 0);
  }
  static \u0275fac = function UserService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _UserService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _UserService, factory: _UserService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(UserService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/roles/services/role.service.ts
var MOCK_ROLES = [
  {
    id: "admin",
    name: "Administrator",
    description: "Full system access",
    permissionCodes: [
      "admin:access",
      "users:read",
      "users:write",
      "users:delete",
      "roles:read",
      "roles:write",
      "roles:delete",
      "permissions:read",
      "permissions:write",
      "permissions:delete",
      "departments:read",
      "departments:write",
      "departments:delete",
      "job-titles:read",
      "job-titles:write",
      "job-titles:delete",
      "approvals:read",
      "approvals:write",
      "approvals:delete",
      "projects:read",
      "projects:write",
      "projects:delete",
      "payment-requests:read",
      "payment-requests:write",
      "payment-requests:delete",
      "approval-tasks:read",
      "approval-tasks:write"
    ],
    createdAt: /* @__PURE__ */ new Date("2024-01-01")
  },
  {
    id: "manager",
    name: "Manager",
    description: "Can manage users and view reports",
    permissionCodes: ["admin:access", "users:read", "users:write", "roles:read"],
    createdAt: /* @__PURE__ */ new Date("2024-01-15")
  },
  {
    id: "viewer",
    name: "Viewer",
    description: "Read-only access",
    permissionCodes: ["users:read", "roles:read", "permissions:read"],
    createdAt: /* @__PURE__ */ new Date("2024-02-01")
  }
];
var RoleService = class _RoleService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_ROLES);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id) {
    return of(this.items$.getValue().find((r) => r.id === id));
  }
  create(data) {
    const item = __spreadProps(__spreadValues({}, data), { id: Date.now().toString(), createdAt: /* @__PURE__ */ new Date() });
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }
  update(id, changes) {
    const updated = this.items$.getValue().map((r) => r.id === id ? __spreadValues(__spreadValues({}, r), changes) : r);
    this.items$.next(updated);
    return of(updated.find((r) => r.id === id));
  }
  delete(id) {
    this.items$.next(this.items$.getValue().filter((r) => r.id !== id));
    return of(void 0);
  }
  static \u0275fac = function RoleService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _RoleService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _RoleService, factory: _RoleService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(RoleService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/users/pages/user-list/user-list.ts
var _c0 = (a0) => [a0, "edit"];
var _c1 = (a0) => [a0];
var _forTrack0 = ($index, $item) => $item.id;
function UserList_For_37_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 20);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const user_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\u4EE3\u7406\u4EBA\uFF1A", user_r2.agentName);
  }
}
function UserList_For_37_Conditional_14_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 23);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "date");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const user_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\u96E2\u8077\uFF1A", \u0275\u0275pipeBind2(2, 1, user_r2.resignDate, "yyyy-MM-dd"));
  }
}
function UserList_For_37_For_20_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 24);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const roleId_r3 = ctx.$implicit;
    const ctx_r3 = \u0275\u0275nextContext(2);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r3.getRoleNames(\u0275\u0275pureFunction1(1, _c1, roleId_r3)));
  }
}
function UserList_For_37_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td")(2, "div", 19);
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(4, UserList_For_37_Conditional_4_Template, 2, 1, "div", 20);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "td", 21);
    \u0275\u0275text(6);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "td", 15);
    \u0275\u0275text(8);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 15);
    \u0275\u0275text(10);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(11, "td", 22);
    \u0275\u0275text(12);
    \u0275\u0275pipe(13, "date");
    \u0275\u0275conditionalCreate(14, UserList_For_37_Conditional_14_Template, 3, 4, "div", 23);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "td", 16);
    \u0275\u0275text(16);
    \u0275\u0275pipe(17, "currency");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(18, "td");
    \u0275\u0275repeaterCreate(19, UserList_For_37_For_20_Template, 2, 3, "span", 24, \u0275\u0275repeaterTrackByIdentity);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(21, "td", 17)(22, "span", 25);
    \u0275\u0275text(23);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(24, "td", 26)(25, "div", 27)(26, "a", 28);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(27, "svg", 7);
    \u0275\u0275element(28, "use", 29);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(29, "button", 30);
    \u0275\u0275listener("click", function UserList_For_37_Template_button_click_29_listener() {
      const user_r2 = \u0275\u0275restoreView(_r1).$implicit;
      const ctx_r3 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r3.delete(user_r2));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(30, "svg", 7);
    \u0275\u0275element(31, "use", 31);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const user_r2 = ctx.$implicit;
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(user_r2.name);
    \u0275\u0275advance();
    \u0275\u0275conditional(user_r2.agentName ? 4 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(user_r2.email);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", user_r2.departmentName || "\u2014", " ");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", user_r2.jobTitleName || "\u2014", " ");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", user_r2.hireDate ? \u0275\u0275pipeBind2(13, 14, user_r2.hireDate, "yyyy-MM-dd") : "\u2014", " ");
    \u0275\u0275advance(2);
    \u0275\u0275conditional(user_r2.resignDate ? 14 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", user_r2.baseSalary ? \u0275\u0275pipeBind4(17, 17, user_r2.baseSalary, "TWD", "symbol", "1.0-0") : "\u2014", " ");
    \u0275\u0275advance(3);
    \u0275\u0275repeater(user_r2.roleIds);
    \u0275\u0275advance(3);
    \u0275\u0275classProp("bg-success", user_r2.status === "active")("bg-secondary", user_r2.status === "inactive");
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", user_r2.status === "active" ? "\u5728\u8077" : "\u96E2\u8077", " ");
    \u0275\u0275advance(3);
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(22, _c0, user_r2.id));
  }
}
function UserList_ForEmpty_38_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 32);
    \u0275\u0275text(2, "\u5C1A\u7121\u54E1\u5DE5\u8CC7\u6599\u3002");
    \u0275\u0275elementEnd()();
  }
}
var UserList = class _UserList {
  userService = inject(UserService);
  roleService = inject(RoleService);
  users$ = this.userService.getAll();
  roles = [];
  ngOnInit() {
    this.roleService.getAll().subscribe((r) => this.roles = r);
  }
  getRoleNames(roleIds) {
    return roleIds.map((id) => this.roles.find((r) => r.id === id)?.name ?? id).join(", ");
  }
  delete(user) {
    if (confirm(`\u78BA\u5B9A\u8981\u522A\u9664\u54E1\u5DE5\u300C${user.name}\u300D\u55CE\uFF1F`)) {
      this.userService.delete(user.id).subscribe();
    }
  }
  static \u0275fac = function UserList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _UserList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _UserList, selectors: [["app-user-list"]], decls: 40, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#user"], [1, "mb-0"], ["routerLink", "new", 1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "d-none", "d-md-table-cell"], [1, "d-none", "d-lg-table-cell"], [1, "d-none", "d-xl-table-cell"], [1, "d-none", "d-sm-table-cell"], [1, "text-end"], [1, "fw-500"], [1, "text-muted", "small"], [1, "text-muted", "d-none", "d-md-table-cell"], [1, "text-muted", "small", "d-none", "d-xl-table-cell"], [1, "text-danger"], [1, "badge", "bg-secondary", "me-1"], [1, "badge"], [1, "text-end", 2, "white-space", "nowrap"], [1, "d-flex", "justify-content-end", "gap-1"], [1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], ["colspan", "9", 1, "text-center", "text-muted", "py-4"]], template: function UserList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u54E1\u5DE5\u7BA1\u7406");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "a", 6);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(8, "svg", 7);
      \u0275\u0275element(9, "use", 8);
      \u0275\u0275elementEnd();
      \u0275\u0275text(10, " \u65B0\u589E\u54E1\u5DE5 ");
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(11, "div", 9)(12, "div", 10)(13, "div", 11)(14, "table", 12)(15, "thead", 13)(16, "tr")(17, "th");
      \u0275\u0275text(18, "\u59D3\u540D");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "th", 14);
      \u0275\u0275text(20, "Email");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(21, "th", 15);
      \u0275\u0275text(22, "\u90E8\u9580");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(23, "th", 15);
      \u0275\u0275text(24, "\u8077\u7A31");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(25, "th", 16);
      \u0275\u0275text(26, "\u5165\u8077\u65E5");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(27, "th", 16);
      \u0275\u0275text(28, "\u5E95\u85AA");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(29, "th");
      \u0275\u0275text(30, "\u89D2\u8272");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(31, "th", 17);
      \u0275\u0275text(32, "\u72C0\u614B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(33, "th", 18);
      \u0275\u0275text(34, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(35, "tbody");
      \u0275\u0275repeaterCreate(36, UserList_For_37_Template, 32, 24, "tr", null, _forTrack0, false, UserList_ForEmpty_38_Template, 3, 0, "tr");
      \u0275\u0275pipe(39, "async");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(36);
      \u0275\u0275repeater(\u0275\u0275pipeBind1(39, 1, ctx.users$));
    }
  }, dependencies: [RouterLink, AsyncPipe, DatePipe, CurrencyPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(UserList, [{
    type: Component,
    args: [{ selector: "app-user-list", imports: [RouterLink, AsyncPipe, DatePipe, CurrencyPipe], template: `<div class="container-fluid py-3">\r
  <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">\r
    <div class="d-flex align-items-center gap-2">\r
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">\r
        <use href="/assets/icons/sprite.svg#user"></use>\r
      </svg>\r
      <h4 class="mb-0">\u54E1\u5DE5\u7BA1\u7406</h4>\r
    </div>\r
    <a routerLink="new" class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light">\r
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>\r
      \u65B0\u589E\u54E1\u5DE5\r
    </a>\r
  </div>\r
\r
  <div class="card border-0 shadow-sm">\r
    <div class="card-body p-0">\r
      <div class="table-responsive">\r
        <table class="table table-hover mb-0">\r
          <thead class="table-light">\r
            <tr>\r
              <th>\u59D3\u540D</th>\r
              <th class="d-none d-md-table-cell">Email</th>\r
              <th class="d-none d-lg-table-cell">\u90E8\u9580</th>\r
              <th class="d-none d-lg-table-cell">\u8077\u7A31</th>\r
              <th class="d-none d-xl-table-cell">\u5165\u8077\u65E5</th>\r
              <th class="d-none d-xl-table-cell">\u5E95\u85AA</th>\r
              <th>\u89D2\u8272</th>\r
              <th class="d-none d-sm-table-cell">\u72C0\u614B</th>\r
              <th class="text-end">\u64CD\u4F5C</th>\r
            </tr>\r
          </thead>\r
          <tbody>\r
            @for (user of users$ | async; track user.id) {\r
              <tr>\r
                <td>\r
                  <div class="fw-500">{{ user.name }}</div>\r
                  @if (user.agentName) {\r
                    <div class="text-muted small">\u4EE3\u7406\u4EBA\uFF1A{{ user.agentName }}</div>\r
                  }\r
                </td>\r
                <td class="text-muted d-none d-md-table-cell">{{ user.email }}</td>\r
                <td class="d-none d-lg-table-cell">\r
                  {{ user.departmentName || '\u2014' }}\r
                </td>\r
                <td class="d-none d-lg-table-cell">\r
                  {{ user.jobTitleName || '\u2014' }}\r
                </td>\r
                <td class="text-muted small d-none d-xl-table-cell">\r
                  {{ user.hireDate ? (user.hireDate | date:'yyyy-MM-dd') : '\u2014' }}\r
                  @if (user.resignDate) {\r
                    <div class="text-danger">\u96E2\u8077\uFF1A{{ user.resignDate | date:'yyyy-MM-dd' }}</div>\r
                  }\r
                </td>\r
                <td class="d-none d-xl-table-cell">\r
                  {{ user.baseSalary ? (user.baseSalary | currency:'TWD':'symbol':'1.0-0') : '\u2014' }}\r
                </td>\r
                <td>\r
                  @for (roleId of user.roleIds; track roleId) {\r
                    <span class="badge bg-secondary me-1">{{ getRoleNames([roleId]) }}</span>\r
                  }\r
                </td>\r
                <td class="d-none d-sm-table-cell">\r
                  <span class="badge" [class.bg-success]="user.status === 'active'" [class.bg-secondary]="user.status === 'inactive'">\r
                    {{ user.status === 'active' ? '\u5728\u8077' : '\u96E2\u8077' }}\r
                  </span>\r
                </td>\r
                <td class="text-end" style="white-space: nowrap">\r
                  <div class="d-flex justify-content-end gap-1">\r
                    <a [routerLink]="[user.id, 'edit']" class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect">\r
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>\r
                    </a>\r
                    <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect" (click)="delete(user)">\r
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>\r
                    </button>\r
                  </div>\r
                </td>\r
              </tr>\r
            } @empty {\r
              <tr>\r
                <td colspan="9" class="text-center text-muted py-4">\u5C1A\u7121\u54E1\u5DE5\u8CC7\u6599\u3002</td>\r
              </tr>\r
            }\r
          </tbody>\r
        </table>\r
      </div>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(UserList, { className: "UserList", filePath: "src/app/features/admin/users/pages/user-list/user-list.ts", lineNumber: 14 });
})();

// src/app/features/admin/departments/services/department.service.ts
var MOCK_DEPARTMENTS = [
  { id: 1, name: "\u7BA1\u7406\u90E8", code: "MGT", sortOrder: 1, employeeCount: 1, createdAt: /* @__PURE__ */ new Date("2024-01-01") },
  { id: 2, name: "\u8CC7\u8A0A\u90E8", code: "IT", sortOrder: 2, employeeCount: 1, createdAt: /* @__PURE__ */ new Date("2024-01-01") },
  { id: 3, name: "\u696D\u52D9\u90E8", code: "SLS", sortOrder: 3, employeeCount: 1, createdAt: /* @__PURE__ */ new Date("2024-01-01") }
];
var DepartmentService = class _DepartmentService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_DEPARTMENTS);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id) {
    return of(this.items$.getValue().find((d) => d.id === id));
  }
  create(data) {
    const item = __spreadProps(__spreadValues({}, data), { id: Date.now(), employeeCount: 0, createdAt: /* @__PURE__ */ new Date() });
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }
  update(id, changes) {
    const updated = this.items$.getValue().map((d) => d.id === id ? __spreadValues(__spreadValues({}, d), changes) : d);
    this.items$.next(updated);
    return of(updated.find((d) => d.id === id));
  }
  delete(id) {
    this.items$.next(this.items$.getValue().filter((d) => d.id !== id));
    return of(void 0);
  }
  static \u0275fac = function DepartmentService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _DepartmentService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _DepartmentService, factory: _DepartmentService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(DepartmentService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/job-titles/services/job-title.service.ts
var MOCK_JOB_TITLES = [
  { id: 1, name: "\u5DE5\u7A0B\u5E2B", level: 1, employeeCount: 1, createdAt: /* @__PURE__ */ new Date("2024-01-01") },
  { id: 2, name: "\u8CC7\u6DF1\u5DE5\u7A0B\u5E2B", level: 2, employeeCount: 1, createdAt: /* @__PURE__ */ new Date("2024-01-01") },
  { id: 3, name: "\u4E3B\u4EFB\u5DE5\u7A0B\u5E2B", level: 3, employeeCount: 0, createdAt: /* @__PURE__ */ new Date("2024-01-01") },
  { id: 4, name: "\u90E8\u9580\u4E3B\u7BA1", level: 4, employeeCount: 1, createdAt: /* @__PURE__ */ new Date("2024-01-01") }
];
var JobTitleService = class _JobTitleService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_JOB_TITLES);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id) {
    return of(this.items$.getValue().find((j) => j.id === id));
  }
  create(data) {
    const item = __spreadProps(__spreadValues({}, data), { id: Date.now(), employeeCount: 0, createdAt: /* @__PURE__ */ new Date() });
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }
  update(id, changes) {
    const updated = this.items$.getValue().map((j) => j.id === id ? __spreadValues(__spreadValues({}, j), changes) : j);
    this.items$.next(updated);
    return of(updated.find((j) => j.id === id));
  }
  delete(id) {
    this.items$.next(this.items$.getValue().filter((j) => j.id !== id));
    return of(void 0);
  }
  static \u0275fac = function JobTitleService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _JobTitleService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _JobTitleService, factory: _JobTitleService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(JobTitleService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/users/pages/user-form/user-form.ts
var _forTrack02 = ($index, $item) => $item.id;
function UserForm_Conditional_21_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 17);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u59D3\u540D\u3002");
    \u0275\u0275elementEnd();
  }
}
function UserForm_Conditional_28_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 17);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u6709\u6548\u7684\u96FB\u5B50\u90F5\u4EF6\u3002");
    \u0275\u0275elementEnd();
  }
}
function UserForm_For_44_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 23);
    \u0275\u0275element(1, "input", 38);
    \u0275\u0275elementStart(2, "label", 39);
    \u0275\u0275text(3);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const role_r1 = ctx.$implicit;
    \u0275\u0275advance();
    \u0275\u0275property("id", "role-" + role_r1.id)("value", role_r1.id);
    \u0275\u0275advance();
    \u0275\u0275property("for", "role-" + role_r1.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(role_r1.name);
  }
}
function UserForm_Conditional_45_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 17);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u89D2\u8272\u3002");
    \u0275\u0275elementEnd();
  }
}
function UserForm_For_58_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 25);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const dept_r2 = ctx.$implicit;
    \u0275\u0275property("ngValue", dept_r2.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(dept_r2.name);
  }
}
function UserForm_For_66_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 25);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const jt_r3 = ctx.$implicit;
    \u0275\u0275property("ngValue", jt_r3.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate2("", jt_r3.name, "\uFF08L", jt_r3.level, "\uFF09");
  }
}
function UserForm_For_89_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 34);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const u_r4 = ctx.$implicit;
    \u0275\u0275property("value", u_r4.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(u_r4.name);
  }
}
var UserForm = class _UserForm {
  fb = inject(FormBuilder);
  userService = inject(UserService);
  roleService = inject(RoleService);
  deptService = inject(DepartmentService);
  jtService = inject(JobTitleService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  roles = [];
  departments = [];
  jobTitles = [];
  allUsers = [];
  isEdit = false;
  userId = "";
  form = this.fb.group({
    name: ["", Validators.required],
    email: ["", [Validators.required, Validators.email]],
    roleId: ["", Validators.required],
    status: ["active", Validators.required],
    departmentId: [null],
    jobTitleId: [null],
    hireDate: [""],
    resignDate: [""],
    baseSalary: [null],
    agentUserId: [""]
  });
  ngOnInit() {
    this.roleService.getAll().subscribe((r) => this.roles = r);
    this.deptService.getAll().subscribe((d) => this.departments = d);
    this.jtService.getAll().subscribe((j) => this.jobTitles = j);
    this.userService.getAll().subscribe((u) => this.allUsers = u);
    this.userId = this.route.snapshot.paramMap.get("id") ?? "";
    if (this.userId) {
      this.isEdit = true;
      this.userService.getById(this.userId).subscribe((user) => {
        if (!user)
          return;
        this.form.patchValue(__spreadProps(__spreadValues({}, user), {
          roleId: user.roleIds[0] ?? "",
          departmentId: user.departmentId ?? null,
          jobTitleId: user.jobTitleId ?? null,
          hireDate: user.hireDate ? this.toDateString(user.hireDate) : "",
          resignDate: user.resignDate ? this.toDateString(user.resignDate) : "",
          baseSalary: user.baseSalary ?? null,
          agentUserId: user.agentUserId ?? ""
        }));
      });
    }
  }
  getAvailableAgents() {
    return this.allUsers.filter((u) => u.id !== this.userId);
  }
  toDateString(d) {
    return new Date(d).toISOString().substring(0, 10);
  }
  submit() {
    if (this.form.invalid)
      return;
    const _a = this.form.value, { roleId, hireDate, resignDate, departmentId, jobTitleId, agentUserId } = _a, rest = __objRest(_a, ["roleId", "hireDate", "resignDate", "departmentId", "jobTitleId", "agentUserId"]);
    const deptId = departmentId || void 0;
    const jtId = jobTitleId || void 0;
    const deptName = deptId ? this.departments.find((d) => d.id === deptId)?.name : void 0;
    const jtName = jtId ? this.jobTitles.find((j) => j.id === jtId)?.name : void 0;
    const agent = agentUserId ? this.allUsers.find((u) => u.id === agentUserId) : void 0;
    const payload = __spreadProps(__spreadValues({}, rest), {
      roleIds: roleId ? [roleId] : [],
      departmentId: deptId,
      departmentName: deptName,
      jobTitleId: jtId,
      jobTitleName: jtName,
      hireDate: hireDate ? new Date(hireDate) : void 0,
      resignDate: resignDate ? new Date(resignDate) : void 0,
      agentUserId: agentUserId || void 0,
      agentName: agent?.name
    });
    const obs = this.isEdit ? this.userService.update(this.userId, payload) : this.userService.create(payload);
    obs.subscribe(() => this.router.navigate(["/admin/users"]));
  }
  static \u0275fac = function UserForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _UserForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _UserForm, selectors: [["app-user-form"]], decls: 95, vars: 9, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], ["routerLink", "/admin/users", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "row"], [1, "col-12", "col-xl-8"], [3, "ngSubmit", "formGroup"], [1, "card", "border-0", "shadow-sm", "mb-3"], [1, "card-header", "bg-transparent", "fw-500"], [1, "card-body"], [1, "row", "g-3"], [1, "col-12", "col-md-6"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "name", "placeholder", "\u8ACB\u8F38\u5165\u59D3\u540D", 1, "form-control"], [1, "text-danger", "small", "mt-1"], ["type", "email", "formControlName", "email", "placeholder", "user@example.com", 1, "form-control"], ["formControlName", "status", 1, "form-select"], ["value", "active"], ["value", "inactive"], [1, "d-flex", "flex-wrap", "gap-3", "mt-1"], [1, "form-check"], ["formControlName", "departmentId", 1, "form-select"], [3, "ngValue"], ["formControlName", "jobTitleId", 1, "form-select"], ["type", "date", "formControlName", "hireDate", 1, "form-control"], ["type", "date", "formControlName", "resignDate", 1, "form-control"], [1, "input-group"], [1, "input-group-text"], ["type", "number", "formControlName", "baseSalary", "placeholder", "\u4F8B\u5982\uFF1A50000", "min", "0", 1, "form-control"], ["formControlName", "agentUserId", 1, "form-select"], ["value", ""], [3, "value"], [1, "d-flex", "gap-2"], ["type", "submit", 1, "btn", "btn-primary", "waves-effect", "waves-light", 3, "disabled"], ["routerLink", "/admin/users", 1, "btn", "btn-outline-secondary", "waves-effect"], ["type", "radio", "name", "roleId", "formControlName", "roleId", 1, "form-check-input", 3, "id", "value"], [1, "form-check-label", 3, "for"]], template: function UserForm_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "a", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "div", 6)(8, "div", 7)(9, "form", 8);
      \u0275\u0275listener("ngSubmit", function UserForm_Template_form_ngSubmit_9_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(10, "div", 9)(11, "div", 10);
      \u0275\u0275text(12, "\u57FA\u672C\u8CC7\u6599");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(13, "div", 11)(14, "div", 12)(15, "div", 13)(16, "label", 14);
      \u0275\u0275text(17, "\u59D3\u540D ");
      \u0275\u0275elementStart(18, "span", 15);
      \u0275\u0275text(19, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(20, "input", 16);
      \u0275\u0275conditionalCreate(21, UserForm_Conditional_21_Template, 2, 0, "div", 17);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(22, "div", 13)(23, "label", 14);
      \u0275\u0275text(24, "Email ");
      \u0275\u0275elementStart(25, "span", 15);
      \u0275\u0275text(26, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(27, "input", 18);
      \u0275\u0275conditionalCreate(28, UserForm_Conditional_28_Template, 2, 0, "div", 17);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(29, "div", 13)(30, "label", 14);
      \u0275\u0275text(31, "\u72C0\u614B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(32, "select", 19)(33, "option", 20);
      \u0275\u0275text(34, "\u5728\u8077");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(35, "option", 21);
      \u0275\u0275text(36, "\u96E2\u8077");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(37, "div", 13)(38, "label", 14);
      \u0275\u0275text(39, "\u89D2\u8272 ");
      \u0275\u0275elementStart(40, "span", 15);
      \u0275\u0275text(41, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(42, "div", 22);
      \u0275\u0275repeaterCreate(43, UserForm_For_44_Template, 4, 4, "div", 23, _forTrack02);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(45, UserForm_Conditional_45_Template, 2, 0, "div", 17);
      \u0275\u0275elementEnd()()()();
      \u0275\u0275elementStart(46, "div", 9)(47, "div", 10);
      \u0275\u0275text(48, "\u54E1\u5DE5\u8CC7\u8A0A");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(49, "div", 11)(50, "div", 12)(51, "div", 13)(52, "label", 14);
      \u0275\u0275text(53, "\u90E8\u9580");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(54, "select", 24)(55, "option", 25);
      \u0275\u0275text(56, "\u2014 \u672A\u6307\u5B9A \u2014");
      \u0275\u0275elementEnd();
      \u0275\u0275repeaterCreate(57, UserForm_For_58_Template, 2, 2, "option", 25, _forTrack02);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(59, "div", 13)(60, "label", 14);
      \u0275\u0275text(61, "\u8077\u7A31");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(62, "select", 26)(63, "option", 25);
      \u0275\u0275text(64, "\u2014 \u672A\u6307\u5B9A \u2014");
      \u0275\u0275elementEnd();
      \u0275\u0275repeaterCreate(65, UserForm_For_66_Template, 2, 3, "option", 25, _forTrack02);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(67, "div", 13)(68, "label", 14);
      \u0275\u0275text(69, "\u5165\u8077\u65E5");
      \u0275\u0275elementEnd();
      \u0275\u0275element(70, "input", 27);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(71, "div", 13)(72, "label", 14);
      \u0275\u0275text(73, "\u96E2\u8077\u65E5");
      \u0275\u0275elementEnd();
      \u0275\u0275element(74, "input", 28);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(75, "div", 13)(76, "label", 14);
      \u0275\u0275text(77, "\u5E95\u85AA");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(78, "div", 29)(79, "span", 30);
      \u0275\u0275text(80, "NT$");
      \u0275\u0275elementEnd();
      \u0275\u0275element(81, "input", 31);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(82, "div", 13)(83, "label", 14);
      \u0275\u0275text(84, "\u8077\u52D9\u4EE3\u7406\u4EBA");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(85, "select", 32)(86, "option", 33);
      \u0275\u0275text(87, "\u2014 \u7121\u4EE3\u7406\u4EBA \u2014");
      \u0275\u0275elementEnd();
      \u0275\u0275repeaterCreate(88, UserForm_For_89_Template, 2, 2, "option", 34, _forTrack02);
      \u0275\u0275elementEnd()()()()();
      \u0275\u0275elementStart(90, "div", 35)(91, "button", 36);
      \u0275\u0275text(92);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(93, "a", 37);
      \u0275\u0275text(94, "\u53D6\u6D88");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      let tmp_2_0;
      let tmp_3_0;
      let tmp_5_0;
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? "\u7DE8\u8F2F\u54E1\u5DE5" : "\u65B0\u589E\u54E1\u5DE5");
      \u0275\u0275advance(3);
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(12);
      \u0275\u0275conditional(((tmp_2_0 = ctx.form.get("name")) == null ? null : tmp_2_0.invalid) && ((tmp_2_0 = ctx.form.get("name")) == null ? null : tmp_2_0.touched) ? 21 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_3_0 = ctx.form.get("email")) == null ? null : tmp_3_0.invalid) && ((tmp_3_0 = ctx.form.get("email")) == null ? null : tmp_3_0.touched) ? 28 : -1);
      \u0275\u0275advance(15);
      \u0275\u0275repeater(ctx.roles);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(((tmp_5_0 = ctx.form.get("roleId")) == null ? null : tmp_5_0.invalid) && ((tmp_5_0 = ctx.form.get("roleId")) == null ? null : tmp_5_0.touched) ? 45 : -1);
      \u0275\u0275advance(10);
      \u0275\u0275property("ngValue", null);
      \u0275\u0275advance(2);
      \u0275\u0275repeater(ctx.departments);
      \u0275\u0275advance(6);
      \u0275\u0275property("ngValue", null);
      \u0275\u0275advance(2);
      \u0275\u0275repeater(ctx.jobTitles);
      \u0275\u0275advance(23);
      \u0275\u0275repeater(ctx.getAvailableAgents());
      \u0275\u0275advance(3);
      \u0275\u0275property("disabled", ctx.form.invalid);
      \u0275\u0275advance();
      \u0275\u0275textInterpolate1(" ", ctx.isEdit ? "\u66F4\u65B0" : "\u5EFA\u7ACB", " ");
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, NumberValueAccessor, SelectControlValueAccessor, RadioControlValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, FormGroupDirective, FormControlName, RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(UserForm, [{
    type: Component,
    args: [{ selector: "app-user-form", imports: [ReactiveFormsModule, RouterLink], template: `<div class="container-fluid py-3">\r
  <div class="d-flex align-items-center gap-2 mb-4">\r
    <a routerLink="/admin/users" class="btn btn-sm btn-outline-secondary waves-effect">\r
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>\r
    </a>\r
    <h4 class="mb-0">{{ isEdit ? '\u7DE8\u8F2F\u54E1\u5DE5' : '\u65B0\u589E\u54E1\u5DE5' }}</h4>\r
  </div>\r
\r
  <div class="row">\r
    <div class="col-12 col-xl-8">\r
      <form [formGroup]="form" (ngSubmit)="submit()">\r
\r
        <!-- \u57FA\u672C\u8CC7\u6599 -->\r
        <div class="card border-0 shadow-sm mb-3">\r
          <div class="card-header bg-transparent fw-500">\u57FA\u672C\u8CC7\u6599</div>\r
          <div class="card-body">\r
            <div class="row g-3">\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u59D3\u540D <span class="text-danger">*</span></label>\r
                <input type="text" class="form-control" formControlName="name" placeholder="\u8ACB\u8F38\u5165\u59D3\u540D">\r
                @if (form.get('name')?.invalid && form.get('name')?.touched) {\r
                  <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u59D3\u540D\u3002</div>\r
                }\r
              </div>\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">Email <span class="text-danger">*</span></label>\r
                <input type="email" class="form-control" formControlName="email" placeholder="user@example.com">\r
                @if (form.get('email')?.invalid && form.get('email')?.touched) {\r
                  <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u6709\u6548\u7684\u96FB\u5B50\u90F5\u4EF6\u3002</div>\r
                }\r
              </div>\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u72C0\u614B</label>\r
                <select class="form-select" formControlName="status">\r
                  <option value="active">\u5728\u8077</option>\r
                  <option value="inactive">\u96E2\u8077</option>\r
                </select>\r
              </div>\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u89D2\u8272 <span class="text-danger">*</span></label>\r
                <div class="d-flex flex-wrap gap-3 mt-1">\r
                  @for (role of roles; track role.id) {\r
                    <div class="form-check">\r
                      <input class="form-check-input" type="radio"\r
                             name="roleId"\r
                             [id]="'role-' + role.id"\r
                             [value]="role.id"\r
                             formControlName="roleId">\r
                      <label class="form-check-label" [for]="'role-' + role.id">{{ role.name }}</label>\r
                    </div>\r
                  }\r
                </div>\r
                @if (form.get('roleId')?.invalid && form.get('roleId')?.touched) {\r
                  <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u89D2\u8272\u3002</div>\r
                }\r
              </div>\r
            </div>\r
          </div>\r
        </div>\r
\r
        <!-- \u54E1\u5DE5\u8CC7\u8A0A -->\r
        <div class="card border-0 shadow-sm mb-3">\r
          <div class="card-header bg-transparent fw-500">\u54E1\u5DE5\u8CC7\u8A0A</div>\r
          <div class="card-body">\r
            <div class="row g-3">\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u90E8\u9580</label>\r
                <select class="form-select" formControlName="departmentId">\r
                  <option [ngValue]="null">\u2014 \u672A\u6307\u5B9A \u2014</option>\r
                  @for (dept of departments; track dept.id) {\r
                    <option [ngValue]="dept.id">{{ dept.name }}</option>\r
                  }\r
                </select>\r
              </div>\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u8077\u7A31</label>\r
                <select class="form-select" formControlName="jobTitleId">\r
                  <option [ngValue]="null">\u2014 \u672A\u6307\u5B9A \u2014</option>\r
                  @for (jt of jobTitles; track jt.id) {\r
                    <option [ngValue]="jt.id">{{ jt.name }}\uFF08L{{ jt.level }}\uFF09</option>\r
                  }\r
                </select>\r
              </div>\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u5165\u8077\u65E5</label>\r
                <input type="date" class="form-control" formControlName="hireDate">\r
              </div>\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u96E2\u8077\u65E5</label>\r
                <input type="date" class="form-control" formControlName="resignDate">\r
              </div>\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u5E95\u85AA</label>\r
                <div class="input-group">\r
                  <span class="input-group-text">NT$</span>\r
                  <input type="number" class="form-control" formControlName="baseSalary" placeholder="\u4F8B\u5982\uFF1A50000" min="0">\r
                </div>\r
              </div>\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u8077\u52D9\u4EE3\u7406\u4EBA</label>\r
                <select class="form-select" formControlName="agentUserId">\r
                  <option value="">\u2014 \u7121\u4EE3\u7406\u4EBA \u2014</option>\r
                  @for (u of getAvailableAgents(); track u.id) {\r
                    <option [value]="u.id">{{ u.name }}</option>\r
                  }\r
                </select>\r
              </div>\r
            </div>\r
          </div>\r
        </div>\r
\r
        <div class="d-flex gap-2">\r
          <button type="submit" class="btn btn-primary waves-effect waves-light" [disabled]="form.invalid">\r
            {{ isEdit ? '\u66F4\u65B0' : '\u5EFA\u7ACB' }}\r
          </button>\r
          <a routerLink="/admin/users" class="btn btn-outline-secondary waves-effect">\u53D6\u6D88</a>\r
        </div>\r
\r
      </form>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(UserForm, { className: "UserForm", filePath: "src/app/features/admin/users/pages/user-form/user-form.ts", lineNumber: 18 });
})();

// src/app/features/admin/roles/pages/role-list/role-list.ts
var _c02 = (a0) => [a0, "edit"];
var _forTrack03 = ($index, $item) => $item.id;
function RoleList_For_29_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td", 17);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td", 18);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "td")(6, "span", 19);
    \u0275\u0275text(7);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(8, "td", 20);
    \u0275\u0275text(9);
    \u0275\u0275pipe(10, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(11, "td", 21)(12, "div", 22)(13, "a", 23);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(14, "svg", 7);
    \u0275\u0275element(15, "use", 24);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(16, "button", 25);
    \u0275\u0275listener("click", function RoleList_For_29_Template_button_click_16_listener() {
      const role_r2 = \u0275\u0275restoreView(_r1).$implicit;
      const ctx_r2 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r2.delete(role_r2));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(17, "svg", 7);
    \u0275\u0275element(18, "use", 26);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const role_r2 = ctx.$implicit;
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(role_r2.name);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(role_r2.description);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1("", role_r2.permissionCodes.length, " \u500B\u6B0A\u9650");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(10, 5, role_r2.createdAt, "yyyy-MM-dd"));
    \u0275\u0275advance(4);
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(8, _c02, role_r2.id));
  }
}
function RoleList_ForEmpty_30_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 27);
    \u0275\u0275text(2, "\u5C1A\u7121\u89D2\u8272\u8CC7\u6599\u3002");
    \u0275\u0275elementEnd()();
  }
}
var RoleList = class _RoleList {
  roleService = inject(RoleService);
  roles$ = this.roleService.getAll();
  delete(role) {
    if (confirm(`Delete role "${role.name}"?`)) {
      this.roleService.delete(role.id).subscribe();
    }
  }
  static \u0275fac = function RoleList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _RoleList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _RoleList, selectors: [["app-role-list"]], decls: 32, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#shield"], [1, "mb-0"], ["routerLink", "new", 1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "d-none", "d-md-table-cell"], [1, "d-none", "d-lg-table-cell"], [1, "text-end"], [1, "fw-500"], [1, "text-muted", "d-none", "d-md-table-cell"], [1, "badge", "bg-info"], [1, "text-muted", "d-none", "d-lg-table-cell"], [1, "text-end", 2, "white-space", "nowrap"], [1, "d-flex", "justify-content-end", "gap-1"], [1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], ["colspan", "5", 1, "text-center", "text-muted", "py-4"]], template: function RoleList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u89D2\u8272\u7BA1\u7406");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "a", 6);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(8, "svg", 7);
      \u0275\u0275element(9, "use", 8);
      \u0275\u0275elementEnd();
      \u0275\u0275text(10, " \u65B0\u589E\u89D2\u8272 ");
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(11, "div", 9)(12, "div", 10)(13, "div", 11)(14, "table", 12)(15, "thead", 13)(16, "tr")(17, "th");
      \u0275\u0275text(18, "\u540D\u7A31");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "th", 14);
      \u0275\u0275text(20, "\u63CF\u8FF0");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(21, "th");
      \u0275\u0275text(22, "\u6B0A\u9650");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(23, "th", 15);
      \u0275\u0275text(24, "\u5EFA\u7ACB\u65E5\u671F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(25, "th", 16);
      \u0275\u0275text(26, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(27, "tbody");
      \u0275\u0275repeaterCreate(28, RoleList_For_29_Template, 19, 10, "tr", null, _forTrack03, false, RoleList_ForEmpty_30_Template, 3, 0, "tr");
      \u0275\u0275pipe(31, "async");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(28);
      \u0275\u0275repeater(\u0275\u0275pipeBind1(31, 1, ctx.roles$));
    }
  }, dependencies: [RouterLink, AsyncPipe, DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(RoleList, [{
    type: Component,
    args: [{ selector: "app-role-list", imports: [RouterLink, AsyncPipe, DatePipe], template: `<div class="container-fluid py-3">\r
  <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">\r
    <div class="d-flex align-items-center gap-2">\r
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">\r
        <use href="/assets/icons/sprite.svg#shield"></use>\r
      </svg>\r
      <h4 class="mb-0">\u89D2\u8272\u7BA1\u7406</h4>\r
    </div>\r
    <a routerLink="new" class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light">\r
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>\r
      \u65B0\u589E\u89D2\u8272\r
    </a>\r
  </div>\r
\r
  <div class="card border-0 shadow-sm">\r
    <div class="card-body p-0">\r
      <div class="table-responsive">\r
        <table class="table table-hover mb-0">\r
          <thead class="table-light">\r
            <tr>\r
              <th>\u540D\u7A31</th>\r
              <th class="d-none d-md-table-cell">\u63CF\u8FF0</th>\r
              <th>\u6B0A\u9650</th>\r
              <th class="d-none d-lg-table-cell">\u5EFA\u7ACB\u65E5\u671F</th>\r
              <th class="text-end">\u64CD\u4F5C</th>\r
            </tr>\r
          </thead>\r
          <tbody>\r
            @for (role of roles$ | async; track role.id) {\r
              <tr>\r
                <td class="fw-500">{{ role.name }}</td>\r
                <td class="text-muted d-none d-md-table-cell">{{ role.description }}</td>\r
                <td>\r
                  <span class="badge bg-info">{{ role.permissionCodes.length }} \u500B\u6B0A\u9650</span>\r
                </td>\r
                <td class="text-muted d-none d-lg-table-cell">{{ role.createdAt | date:'yyyy-MM-dd' }}</td>\r
                <td class="text-end" style="white-space: nowrap">\r
                  <div class="d-flex justify-content-end gap-1">\r
                    <a [routerLink]="[role.id, 'edit']" class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect">\r
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>\r
                    </a>\r
                    <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect" (click)="delete(role)">\r
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>\r
                    </button>\r
                  </div>\r
                </td>\r
              </tr>\r
            } @empty {\r
              <tr>\r
                <td colspan="5" class="text-center text-muted py-4">\u5C1A\u7121\u89D2\u8272\u8CC7\u6599\u3002</td>\r
              </tr>\r
            }\r
          </tbody>\r
        </table>\r
      </div>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(RoleList, { className: "RoleList", filePath: "src/app/features/admin/roles/pages/role-list/role-list.ts", lineNumber: 12 });
})();

// src/app/features/admin/permissions/services/permission.service.ts
var MOCK_PERMISSIONS = [
  { id: "1", code: "admin:access", name: "Admin Access", module: "Admin" },
  { id: "2", code: "users:read", name: "View Users", module: "Users" },
  { id: "3", code: "users:write", name: "Create/Edit Users", module: "Users" },
  { id: "4", code: "users:delete", name: "Delete Users", module: "Users" },
  { id: "5", code: "roles:read", name: "View Roles", module: "Roles" },
  { id: "6", code: "roles:write", name: "Create/Edit Roles", module: "Roles" },
  { id: "7", code: "roles:delete", name: "Delete Roles", module: "Roles" },
  { id: "8", code: "permissions:read", name: "View Permissions", module: "Permissions" },
  { id: "9", code: "permissions:write", name: "Create/Edit Permissions", module: "Permissions" },
  { id: "10", code: "permissions:delete", name: "Delete Permissions", module: "Permissions" },
  { id: "11", code: "departments:read", name: "View Departments", module: "Departments" },
  { id: "12", code: "departments:write", name: "Create/Edit Departments", module: "Departments" },
  { id: "13", code: "departments:delete", name: "Delete Departments", module: "Departments" },
  { id: "14", code: "job-titles:read", name: "View Job Titles", module: "JobTitles" },
  { id: "15", code: "job-titles:write", name: "Create/Edit Job Titles", module: "JobTitles" },
  { id: "16", code: "job-titles:delete", name: "Delete Job Titles", module: "JobTitles" },
  { id: "17", code: "approvals:read", name: "View Approvals", module: "Approvals" },
  { id: "18", code: "approvals:write", name: "Create/Edit Approvals", module: "Approvals" },
  { id: "19", code: "approvals:delete", name: "Delete Approvals", module: "Approvals" },
  { id: "20", code: "projects:read", name: "View Projects", module: "Projects" },
  { id: "21", code: "projects:write", name: "Create/Edit Projects", module: "Projects" },
  { id: "22", code: "projects:delete", name: "Delete Projects", module: "Projects" },
  { id: "23", code: "payment-requests:read", name: "View Payment Requests", module: "PaymentRequests" },
  { id: "24", code: "payment-requests:write", name: "Create/Edit Payment Requests", module: "PaymentRequests" },
  { id: "25", code: "payment-requests:delete", name: "Delete Payment Requests", module: "PaymentRequests" },
  { id: "26", code: "approval-tasks:read", name: "View Approval Tasks", module: "ApprovalTasks" },
  { id: "27", code: "approval-tasks:write", name: "Review Approval Tasks", module: "ApprovalTasks" },
  { id: "28", code: "leave-requests:read", name: "View Leave Requests", module: "LeaveRequests" },
  { id: "29", code: "leave-requests:write", name: "Create/Edit Leave Requests", module: "LeaveRequests" },
  { id: "30", code: "leave-requests:delete", name: "Delete Leave Requests", module: "LeaveRequests" },
  { id: "31", code: "travel-requests:read", name: "View Travel Requests", module: "TravelRequests" },
  { id: "32", code: "travel-requests:write", name: "Create/Edit Travel Requests", module: "TravelRequests" },
  { id: "33", code: "travel-requests:delete", name: "Delete Travel Requests", module: "TravelRequests" }
];
var PermissionService = class _PermissionService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_PERMISSIONS);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id) {
    return of(this.items$.getValue().find((p) => p.id === id));
  }
  getModules() {
    return [...new Set(this.items$.getValue().map((p) => p.module))];
  }
  create(data) {
    const item = __spreadProps(__spreadValues({}, data), { id: Date.now().toString() });
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }
  update(id, changes) {
    const updated = this.items$.getValue().map((p) => p.id === id ? __spreadValues(__spreadValues({}, p), changes) : p);
    this.items$.next(updated);
    return of(updated.find((p) => p.id === id));
  }
  delete(id) {
    this.items$.next(this.items$.getValue().filter((p) => p.id !== id));
    return of(void 0);
  }
  static \u0275fac = function PermissionService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PermissionService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _PermissionService, factory: _PermissionService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PermissionService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/roles/pages/role-form/role-form.ts
var _forTrack04 = ($index, $item) => $item.id;
function RoleForm_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u89D2\u8272\u540D\u7A31\u3002");
    \u0275\u0275elementEnd();
  }
}
function RoleForm_For_28_For_7_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 27)(1, "input", 24);
    \u0275\u0275listener("change", function RoleForm_For_28_For_7_Template_input_change_1_listener() {
      const perm_r5 = \u0275\u0275restoreView(_r4).$implicit;
      const ctx_r2 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r2.togglePermission(perm_r5.code));
    });
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(2, "label", 28);
    \u0275\u0275text(3);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const perm_r5 = ctx.$implicit;
    const ctx_r2 = \u0275\u0275nextContext(2);
    \u0275\u0275advance();
    \u0275\u0275property("id", "perm-" + perm_r5.id)("checked", ctx_r2.isPermissionSelected(perm_r5.code));
    \u0275\u0275advance();
    \u0275\u0275property("for", "perm-" + perm_r5.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(perm_r5.name);
  }
}
function RoleForm_For_28_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 11)(1, "div", 23)(2, "input", 24);
    \u0275\u0275listener("change", function RoleForm_For_28_Template_input_change_2_listener($event) {
      const module_r2 = \u0275\u0275restoreView(_r1).$implicit;
      const ctx_r2 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r2.toggleModule(module_r2, $event.target.checked));
    });
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "label", 25);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(5, "div", 26);
    \u0275\u0275repeaterCreate(6, RoleForm_For_28_For_7_Template, 4, 4, "div", 27, _forTrack04);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const module_r2 = ctx.$implicit;
    const ctx_r2 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275property("id", "module-" + module_r2)("checked", ctx_r2.isModuleAllSelected(module_r2));
    \u0275\u0275advance();
    \u0275\u0275property("for", "module-" + module_r2);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(module_r2);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(ctx_r2.getPermissionsByModule(module_r2));
  }
}
var RoleForm = class _RoleForm {
  fb = inject(FormBuilder);
  roleService = inject(RoleService);
  permissionService = inject(PermissionService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  permissions = [];
  modules = [];
  isEdit = false;
  roleId = "";
  form = this.fb.group({
    name: ["", Validators.required],
    description: [""],
    permissionCodes: [[]]
  });
  ngOnInit() {
    this.permissionService.getAll().subscribe((p) => {
      this.permissions = p;
      this.modules = this.permissionService.getModules();
    });
    this.roleId = this.route.snapshot.paramMap.get("id") ?? "";
    if (this.roleId) {
      this.isEdit = true;
      this.roleService.getById(this.roleId).subscribe((role) => {
        if (role)
          this.form.patchValue(__spreadValues({}, role));
      });
    }
  }
  getPermissionsByModule(module) {
    return this.permissions.filter((p) => p.module === module);
  }
  isPermissionSelected(code) {
    return (this.form.value.permissionCodes ?? []).includes(code);
  }
  togglePermission(code) {
    const current = this.form.value.permissionCodes ?? [];
    const updated = current.includes(code) ? current.filter((c) => c !== code) : [...current, code];
    this.form.patchValue({ permissionCodes: updated });
  }
  toggleModule(module, checked) {
    const moduleCodes = this.getPermissionsByModule(module).map((p) => p.code);
    const current = this.form.value.permissionCodes ?? [];
    const updated = checked ? [.../* @__PURE__ */ new Set([...current, ...moduleCodes])] : current.filter((c) => !moduleCodes.includes(c));
    this.form.patchValue({ permissionCodes: updated });
  }
  isModuleAllSelected(module) {
    const moduleCodes = this.getPermissionsByModule(module).map((p) => p.code);
    return moduleCodes.every((c) => this.isPermissionSelected(c));
  }
  submit() {
    if (this.form.invalid)
      return;
    const value = this.form.value;
    const obs = this.isEdit ? this.roleService.update(this.roleId, value) : this.roleService.create(value);
    obs.subscribe(() => this.router.navigate(["/admin/roles"]));
  }
  static \u0275fac = function RoleForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _RoleForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _RoleForm, selectors: [["app-role-form"]], decls: 36, vars: 6, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], ["routerLink", "/admin/roles", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "row"], [1, "col-12"], [1, "card", "border-0", "shadow-sm"], [1, "card-body"], [3, "ngSubmit", "formGroup"], [1, "mb-3"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "name", "placeholder", "\u4F8B\u5982\uFF1A\u4E3B\u7BA1", 1, "form-control"], [1, "text-danger", "small", "mt-1"], [1, "mb-4"], ["type", "text", "formControlName", "description", "placeholder", "\u7C21\u77ED\u63CF\u8FF0", 1, "form-control"], [1, "border", "rounded", "p-3"], [1, "text-muted", "small", "mt-1"], [1, "d-flex", "gap-2"], ["type", "submit", 1, "btn", "btn-primary", "waves-effect", "waves-light", 3, "disabled"], ["routerLink", "/admin/roles", 1, "btn", "btn-outline-secondary", "waves-effect"], [1, "d-flex", "align-items-center", "gap-2", "mb-2"], ["type", "checkbox", 1, "form-check-input", 3, "change", "id", "checked"], [1, "form-check-label", "fw-600", 3, "for"], [1, "d-flex", "flex-wrap", "gap-3", "ms-4"], [1, "form-check"], [1, "form-check-label", "small", 3, "for"]], template: function RoleForm_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "a", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "div", 6)(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "form", 10);
      \u0275\u0275listener("ngSubmit", function RoleForm_Template_form_ngSubmit_11_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(12, "div", 11)(13, "label", 12);
      \u0275\u0275text(14, "\u89D2\u8272\u540D\u7A31 ");
      \u0275\u0275elementStart(15, "span", 13);
      \u0275\u0275text(16, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(17, "input", 14);
      \u0275\u0275conditionalCreate(18, RoleForm_Conditional_18_Template, 2, 0, "div", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "div", 16)(20, "label", 12);
      \u0275\u0275text(21, "\u63CF\u8FF0");
      \u0275\u0275elementEnd();
      \u0275\u0275element(22, "input", 17);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(23, "div", 16)(24, "label", 12);
      \u0275\u0275text(25, "\u6B0A\u9650");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(26, "div", 18);
      \u0275\u0275repeaterCreate(27, RoleForm_For_28_Template, 8, 4, "div", 11, \u0275\u0275repeaterTrackByIdentity);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(29, "div", 19);
      \u0275\u0275text(30);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(31, "div", 20)(32, "button", 21);
      \u0275\u0275text(33);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(34, "a", 22);
      \u0275\u0275text(35, "\u53D6\u6D88");
      \u0275\u0275elementEnd()()()()()()()();
    }
    if (rf & 2) {
      let tmp_2_0;
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? "\u7DE8\u8F2F\u89D2\u8272" : "\u65B0\u589E\u89D2\u8272");
      \u0275\u0275advance(5);
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_2_0 = ctx.form.get("name")) == null ? null : tmp_2_0.invalid) && ((tmp_2_0 = ctx.form.get("name")) == null ? null : tmp_2_0.touched) ? 18 : -1);
      \u0275\u0275advance(9);
      \u0275\u0275repeater(ctx.modules);
      \u0275\u0275advance(3);
      \u0275\u0275textInterpolate1(" \u5DF2\u9078\u53D6 ", (ctx.form.value.permissionCodes == null ? null : ctx.form.value.permissionCodes.length) ?? 0, " \u500B\u6B0A\u9650 ");
      \u0275\u0275advance(2);
      \u0275\u0275property("disabled", ctx.form.invalid);
      \u0275\u0275advance();
      \u0275\u0275textInterpolate1(" ", ctx.isEdit ? "\u66F4\u65B0" : "\u5EFA\u7ACB", " ");
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, DefaultValueAccessor, NgControlStatus, NgControlStatusGroup, FormGroupDirective, FormControlName, RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(RoleForm, [{
    type: Component,
    args: [{ selector: "app-role-form", imports: [ReactiveFormsModule, RouterLink], template: `<div class="container-fluid py-3">\r
  <div class="d-flex align-items-center gap-2 mb-4">\r
    <a routerLink="/admin/roles" class="btn btn-sm btn-outline-secondary waves-effect">\r
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>\r
    </a>\r
    <h4 class="mb-0">{{ isEdit ? '\u7DE8\u8F2F\u89D2\u8272' : '\u65B0\u589E\u89D2\u8272' }}</h4>\r
  </div>\r
\r
  <div class="row">\r
    <div class="col-12">\r
      <div class="card border-0 shadow-sm">\r
        <div class="card-body">\r
          <form [formGroup]="form" (ngSubmit)="submit()">\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u89D2\u8272\u540D\u7A31 <span class="text-danger">*</span></label>\r
              <input type="text" class="form-control" formControlName="name" placeholder="\u4F8B\u5982\uFF1A\u4E3B\u7BA1">\r
              @if (form.get('name')?.invalid && form.get('name')?.touched) {\r
                <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u89D2\u8272\u540D\u7A31\u3002</div>\r
              }\r
            </div>\r
\r
            <div class="mb-4">\r
              <label class="form-label fw-500">\u63CF\u8FF0</label>\r
              <input type="text" class="form-control" formControlName="description" placeholder="\u7C21\u77ED\u63CF\u8FF0">\r
            </div>\r
\r
            <div class="mb-4">\r
              <label class="form-label fw-500">\u6B0A\u9650</label>\r
              <div class="border rounded p-3">\r
                @for (module of modules; track module) {\r
                  <div class="mb-3">\r
                    <div class="d-flex align-items-center gap-2 mb-2">\r
                      <input type="checkbox" class="form-check-input"\r
                             [id]="'module-' + module"\r
                             [checked]="isModuleAllSelected(module)"\r
                             (change)="toggleModule(module, $any($event.target).checked)">\r
                      <label class="form-check-label fw-600" [for]="'module-' + module">{{ module }}</label>\r
                    </div>\r
                    <div class="d-flex flex-wrap gap-3 ms-4">\r
                      @for (perm of getPermissionsByModule(module); track perm.id) {\r
                        <div class="form-check">\r
                          <input class="form-check-input" type="checkbox"\r
                                 [id]="'perm-' + perm.id"\r
                                 [checked]="isPermissionSelected(perm.code)"\r
                                 (change)="togglePermission(perm.code)">\r
                          <label class="form-check-label small" [for]="'perm-' + perm.id">{{ perm.name }}</label>\r
                        </div>\r
                      }\r
                    </div>\r
                  </div>\r
                }\r
              </div>\r
              <div class="text-muted small mt-1">\r
                \u5DF2\u9078\u53D6 {{ form.value.permissionCodes?.length ?? 0 }} \u500B\u6B0A\u9650\r
              </div>\r
            </div>\r
\r
            <div class="d-flex gap-2">\r
              <button type="submit" class="btn btn-primary waves-effect waves-light" [disabled]="form.invalid">\r
                {{ isEdit ? '\u66F4\u65B0' : '\u5EFA\u7ACB' }}\r
              </button>\r
              <a routerLink="/admin/roles" class="btn btn-outline-secondary waves-effect">\u53D6\u6D88</a>\r
            </div>\r
\r
          </form>\r
        </div>\r
      </div>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(RoleForm, { className: "RoleForm", filePath: "src/app/features/admin/roles/pages/role-form/role-form.ts", lineNumber: 13 });
})();

// src/app/features/admin/permissions/pages/permission-list/permission-list.ts
var _c03 = (a0) => [a0, "edit"];
var _forTrack05 = ($index, $item) => $item.id;
function PermissionList_For_12_For_20_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td")(2, "code", 19);
    \u0275\u0275text(3);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(4, "td", 20);
    \u0275\u0275text(5);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "td", 21);
    \u0275\u0275text(7);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "td", 22)(9, "div", 23)(10, "a", 24);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(11, "svg", 7);
    \u0275\u0275element(12, "use", 25);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(13, "button", 26);
    \u0275\u0275listener("click", function PermissionList_For_12_For_20_Template_button_click_13_listener() {
      const perm_r2 = \u0275\u0275restoreView(_r1).$implicit;
      const ctx_r2 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r2.delete(perm_r2));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(14, "svg", 7);
    \u0275\u0275element(15, "use", 27);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const perm_r2 = ctx.$implicit;
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(perm_r2.code);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(perm_r2.name);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(perm_r2.description ?? "\u2014");
    \u0275\u0275advance(3);
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(4, _c03, perm_r2.id));
  }
}
function PermissionList_For_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 9)(1, "div", 10);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 11);
    \u0275\u0275element(3, "use", 12);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 13)(6, "div", 14)(7, "table", 15)(8, "thead", 16)(9, "tr")(10, "th");
    \u0275\u0275text(11, "\u4EE3\u78BC");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "th");
    \u0275\u0275text(13, "\u540D\u7A31");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(14, "th", 17);
    \u0275\u0275text(15, "\u63CF\u8FF0");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(16, "th", 18);
    \u0275\u0275text(17, "\u64CD\u4F5C");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(18, "tbody");
    \u0275\u0275repeaterCreate(19, PermissionList_For_12_For_20_Template, 16, 6, "tr", null, _forTrack05);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const module_r4 = ctx.$implicit;
    const ctx_r2 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate1(" ", module_r4, " ");
    \u0275\u0275advance(15);
    \u0275\u0275repeater(ctx_r2.getByModule(module_r4));
  }
}
var PermissionList = class _PermissionList {
  permissionService = inject(PermissionService);
  modules = this.permissionService.getModules();
  allPermissions = [];
  constructor() {
    this.permissionService.getAll().subscribe((p) => this.allPermissions = p);
  }
  getByModule(module) {
    return this.allPermissions.filter((p) => p.module === module);
  }
  delete(perm) {
    if (confirm(`Delete permission "${perm.name}"?`)) {
      this.permissionService.delete(perm.id).subscribe();
    }
  }
  static \u0275fac = function PermissionList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PermissionList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _PermissionList, selectors: [["app-permission-list"]], decls: 13, vars: 0, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#lock"], [1, "mb-0"], ["routerLink", "new", 1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm", "mb-3"], [1, "card-header", "bg-light", "fw-600", "d-flex", "align-items-center"], [1, "sa-icon", "me-2", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#folder"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "d-none", "d-md-table-cell"], [1, "text-end"], [1, "text-primary"], [1, "fw-500"], [1, "text-muted", "d-none", "d-md-table-cell"], [1, "text-end", 2, "white-space", "nowrap"], [1, "d-flex", "justify-content-end", "gap-1"], [1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"]], template: function PermissionList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u6B0A\u9650\u7BA1\u7406");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "a", 6);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(8, "svg", 7);
      \u0275\u0275element(9, "use", 8);
      \u0275\u0275elementEnd();
      \u0275\u0275text(10, " \u65B0\u589E\u6B0A\u9650 ");
      \u0275\u0275elementEnd()();
      \u0275\u0275repeaterCreate(11, PermissionList_For_12_Template, 21, 1, "div", 9, \u0275\u0275repeaterTrackByIdentity);
      \u0275\u0275elementEnd();
    }
    if (rf & 2) {
      \u0275\u0275advance(11);
      \u0275\u0275repeater(ctx.modules);
    }
  }, dependencies: [RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PermissionList, [{
    type: Component,
    args: [{ selector: "app-permission-list", imports: [RouterLink], template: `<div class="container-fluid py-3">\r
  <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">\r
    <div class="d-flex align-items-center gap-2">\r
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">\r
        <use href="/assets/icons/sprite.svg#lock"></use>\r
      </svg>\r
      <h4 class="mb-0">\u6B0A\u9650\u7BA1\u7406</h4>\r
    </div>\r
    <a routerLink="new" class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light">\r
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>\r
      \u65B0\u589E\u6B0A\u9650\r
    </a>\r
  </div>\r
\r
  @for (module of modules; track module) {\r
    <div class="card border-0 shadow-sm mb-3">\r
      <div class="card-header bg-light fw-600 d-flex align-items-center">\r
        <svg class="sa-icon me-2 text-primary" style="stroke: currentColor">\r
          <use href="/assets/icons/sprite.svg#folder"></use>\r
        </svg>\r
        {{ module }}\r
      </div>\r
      <div class="card-body p-0">\r
        <div class="table-responsive">\r
          <table class="table table-hover mb-0">\r
            <thead class="table-light">\r
              <tr>\r
                <th>\u4EE3\u78BC</th>\r
                <th>\u540D\u7A31</th>\r
                <th class="d-none d-md-table-cell">\u63CF\u8FF0</th>\r
                <th class="text-end">\u64CD\u4F5C</th>\r
              </tr>\r
            </thead>\r
            <tbody>\r
              @for (perm of getByModule(module); track perm.id) {\r
                <tr>\r
                  <td><code class="text-primary">{{ perm.code }}</code></td>\r
                  <td class="fw-500">{{ perm.name }}</td>\r
                  <td class="text-muted d-none d-md-table-cell">{{ perm.description ?? '\u2014' }}</td>\r
                  <td class="text-end" style="white-space: nowrap">\r
                    <div class="d-flex justify-content-end gap-1">\r
                      <a [routerLink]="[perm.id, 'edit']" class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect">\r
                        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>\r
                      </a>\r
                      <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect" (click)="delete(perm)">\r
                        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>\r
                      </button>\r
                    </div>\r
                  </td>\r
                </tr>\r
              }\r
            </tbody>\r
          </table>\r
        </div>\r
      </div>\r
    </div>\r
  }\r
</div>\r
` }]
  }], () => [], null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(PermissionList, { className: "PermissionList", filePath: "src/app/features/admin/permissions/pages/permission-list/permission-list.ts", lineNumber: 11 });
})();

// src/app/features/admin/permissions/pages/permission-form/permission-form.ts
var PermissionForm = class _PermissionForm {
  fb = inject(FormBuilder);
  service = inject(PermissionService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  isEdit = false;
  permId = "";
  form = this.fb.group({
    code: ["", Validators.required],
    name: ["", Validators.required],
    module: ["", Validators.required],
    description: [""]
  });
  ngOnInit() {
    this.permId = this.route.snapshot.paramMap.get("id") ?? "";
    if (this.permId) {
      this.isEdit = true;
      this.service.getById(this.permId).subscribe((p) => {
        if (p)
          this.form.patchValue(__spreadValues({}, p));
      });
    }
  }
  submit() {
    if (this.form.invalid)
      return;
    const value = this.form.value;
    const obs = this.isEdit ? this.service.update(this.permId, value) : this.service.create(value);
    obs.subscribe(() => this.router.navigate(["/admin/permissions"]));
  }
  static \u0275fac = function PermissionForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PermissionForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _PermissionForm, selectors: [["app-permission-form"]], decls: 46, vars: 4, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], ["routerLink", "/admin/permissions", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "row"], [1, "col-12"], [1, "card", "border-0", "shadow-sm"], [1, "card-body"], [3, "ngSubmit", "formGroup"], [1, "mb-3"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "code", "placeholder", "module:action", 1, "form-control", "font-monospace"], [1, "text-muted", "small", "mt-1"], ["type", "text", "formControlName", "name", "placeholder", "\u986F\u793A\u540D\u7A31", 1, "form-control"], ["type", "text", "formControlName", "module", "placeholder", "\u4F8B\u5982\uFF1AUsers\u3001Roles", 1, "form-control"], [1, "mb-4"], ["type", "text", "formControlName", "description", "placeholder", "\u9078\u586B\u8AAA\u660E", 1, "form-control"], [1, "d-flex", "gap-2"], ["type", "submit", 1, "btn", "btn-primary", "waves-effect", "waves-light", 3, "disabled"], ["routerLink", "/admin/permissions", 1, "btn", "btn-outline-secondary", "waves-effect"]], template: function PermissionForm_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "a", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "div", 6)(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "form", 10);
      \u0275\u0275listener("ngSubmit", function PermissionForm_Template_form_ngSubmit_11_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(12, "div", 11)(13, "label", 12);
      \u0275\u0275text(14, "\u4EE3\u78BC ");
      \u0275\u0275elementStart(15, "span", 13);
      \u0275\u0275text(16, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(17, "input", 14);
      \u0275\u0275elementStart(18, "div", 15);
      \u0275\u0275text(19, "\u4F8B\u5982\uFF1A");
      \u0275\u0275elementStart(20, "code");
      \u0275\u0275text(21, "users:read");
      \u0275\u0275elementEnd();
      \u0275\u0275text(22, "\u3001");
      \u0275\u0275elementStart(23, "code");
      \u0275\u0275text(24, "roles:write");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(25, "div", 11)(26, "label", 12);
      \u0275\u0275text(27, "\u540D\u7A31 ");
      \u0275\u0275elementStart(28, "span", 13);
      \u0275\u0275text(29, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(30, "input", 16);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(31, "div", 11)(32, "label", 12);
      \u0275\u0275text(33, "\u6A21\u7D44 ");
      \u0275\u0275elementStart(34, "span", 13);
      \u0275\u0275text(35, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(36, "input", 17);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(37, "div", 18)(38, "label", 12);
      \u0275\u0275text(39, "\u63CF\u8FF0");
      \u0275\u0275elementEnd();
      \u0275\u0275element(40, "input", 19);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(41, "div", 20)(42, "button", 21);
      \u0275\u0275text(43);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(44, "a", 22);
      \u0275\u0275text(45, "\u53D6\u6D88");
      \u0275\u0275elementEnd()()()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? "\u7DE8\u8F2F\u6B0A\u9650" : "\u65B0\u589E\u6B0A\u9650");
      \u0275\u0275advance(5);
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(31);
      \u0275\u0275property("disabled", ctx.form.invalid);
      \u0275\u0275advance();
      \u0275\u0275textInterpolate1(" ", ctx.isEdit ? "\u66F4\u65B0" : "\u5EFA\u7ACB", " ");
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, DefaultValueAccessor, NgControlStatus, NgControlStatusGroup, FormGroupDirective, FormControlName, RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PermissionForm, [{
    type: Component,
    args: [{ selector: "app-permission-form", imports: [ReactiveFormsModule, RouterLink], template: `<div class="container-fluid py-3">\r
  <div class="d-flex align-items-center gap-2 mb-4">\r
    <a routerLink="/admin/permissions" class="btn btn-sm btn-outline-secondary waves-effect">\r
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>\r
    </a>\r
    <h4 class="mb-0">{{ isEdit ? '\u7DE8\u8F2F\u6B0A\u9650' : '\u65B0\u589E\u6B0A\u9650' }}</h4>\r
  </div>\r
\r
  <div class="row">\r
    <div class="col-12">\r
      <div class="card border-0 shadow-sm">\r
        <div class="card-body">\r
          <form [formGroup]="form" (ngSubmit)="submit()">\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u4EE3\u78BC <span class="text-danger">*</span></label>\r
              <input type="text" class="form-control font-monospace" formControlName="code" placeholder="module:action">\r
              <div class="text-muted small mt-1">\u4F8B\u5982\uFF1A<code>users:read</code>\u3001<code>roles:write</code></div>\r
            </div>\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u540D\u7A31 <span class="text-danger">*</span></label>\r
              <input type="text" class="form-control" formControlName="name" placeholder="\u986F\u793A\u540D\u7A31">\r
            </div>\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u6A21\u7D44 <span class="text-danger">*</span></label>\r
              <input type="text" class="form-control" formControlName="module" placeholder="\u4F8B\u5982\uFF1AUsers\u3001Roles">\r
            </div>\r
\r
            <div class="mb-4">\r
              <label class="form-label fw-500">\u63CF\u8FF0</label>\r
              <input type="text" class="form-control" formControlName="description" placeholder="\u9078\u586B\u8AAA\u660E">\r
            </div>\r
\r
            <div class="d-flex gap-2">\r
              <button type="submit" class="btn btn-primary waves-effect waves-light" [disabled]="form.invalid">\r
                {{ isEdit ? '\u66F4\u65B0' : '\u5EFA\u7ACB' }}\r
              </button>\r
              <a routerLink="/admin/permissions" class="btn btn-outline-secondary waves-effect">\u53D6\u6D88</a>\r
            </div>\r
\r
          </form>\r
        </div>\r
      </div>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(PermissionForm, { className: "PermissionForm", filePath: "src/app/features/admin/permissions/pages/permission-form/permission-form.ts", lineNumber: 11 });
})();

// src/app/features/admin/settings/services/settings.service.ts
var MOCK_SETTINGS = {
  workStartTime: "09:00",
  workEndTime: "18:00",
  monthlyOvertimeLimit: 46
};
var SettingsService = class _SettingsService {
  http = inject(HttpClient);
  settings$ = new BehaviorSubject(MOCK_SETTINGS);
  get() {
    return this.settings$.asObservable();
  }
  save(changes) {
    const updated = __spreadValues(__spreadValues({}, this.settings$.getValue()), changes);
    this.settings$.next(updated);
    return of(updated);
  }
  static \u0275fac = function SettingsService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _SettingsService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _SettingsService, factory: _SettingsService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(SettingsService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/settings/pages/settings/settings.ts
function Settings_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 5);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 24);
    \u0275\u0275element(2, "use", 26);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u8A2D\u5B9A\u5DF2\u5132\u5B58\u6210\u529F\u3002 ");
    \u0275\u0275elementEnd();
  }
}
function Settings_Conditional_34_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 21);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165 0 \u5230 200 \u4E4B\u9593\u7684\u6578\u503C\u3002");
    \u0275\u0275elementEnd();
  }
}
var Settings = class _Settings {
  fb = inject(FormBuilder);
  settingsService = inject(SettingsService);
  saved = false;
  form = this.fb.group({
    workStartTime: ["09:00", Validators.required],
    workEndTime: ["18:00", Validators.required],
    monthlyOvertimeLimit: [46, [Validators.required, Validators.min(0), Validators.max(200)]]
  });
  ngOnInit() {
    this.settingsService.get().subscribe((s) => this.form.patchValue(s));
  }
  submit() {
    if (this.form.invalid)
      return;
    this.settingsService.save(this.form.value).subscribe(() => {
      this.saved = true;
      setTimeout(() => this.saved = false, 3e3);
    });
  }
  static \u0275fac = function Settings_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _Settings)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _Settings, selectors: [["app-settings"]], decls: 40, vars: 4, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#settings"], [1, "mb-0"], ["role", "alert", 1, "alert", "alert-success", "d-flex", "align-items-center", "gap-2", "mb-4", "py-2"], [3, "ngSubmit", "formGroup"], [1, "row", "g-4"], [1, "col-12", "col-md-6", "col-xl-4"], [1, "card", "border-0", "shadow-sm"], [1, "card-header", "bg-transparent", "border-bottom", "d-flex", "align-items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#clock"], [1, "card-body"], [1, "mb-3"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "time", "formControlName", "workStartTime", 1, "form-control"], ["type", "time", "formControlName", "workEndTime", 1, "form-control"], [1, "text-muted", "fw-400"], ["type", "number", "formControlName", "monthlyOvertimeLimit", "min", "0", "max", "200", 1, "form-control"], [1, "text-danger", "small", "mt-1"], [1, "col-12"], ["type", "submit", 1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light", 3, "disabled"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#save"], ["href", "/assets/icons/sprite.svg#check-circle"]], template: function Settings_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(2, "svg", 2);
      \u0275\u0275element(3, "use", 3);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(4, "h4", 4);
      \u0275\u0275text(5, "\u7CFB\u7D71\u8A2D\u5B9A");
      \u0275\u0275elementEnd()();
      \u0275\u0275conditionalCreate(6, Settings_Conditional_6_Template, 4, 0, "div", 5);
      \u0275\u0275elementStart(7, "form", 6);
      \u0275\u0275listener("ngSubmit", function Settings_Template_form_ngSubmit_7_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "div", 10);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(12, "svg", 11);
      \u0275\u0275element(13, "use", 12);
      \u0275\u0275elementEnd();
      \u0275\u0275text(14, " \u5DE5\u4F5C\u6642\u9593 ");
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(15, "div", 13)(16, "div", 14)(17, "label", 15);
      \u0275\u0275text(18, "\u4E0A\u73ED\u6642\u9593 ");
      \u0275\u0275elementStart(19, "span", 16);
      \u0275\u0275text(20, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(21, "input", 17);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(22, "div", 14)(23, "label", 15);
      \u0275\u0275text(24, "\u4E0B\u73ED\u6642\u9593 ");
      \u0275\u0275elementStart(25, "span", 16);
      \u0275\u0275text(26, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(27, "input", 18);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(28, "div", 4)(29, "label", 15);
      \u0275\u0275text(30, "\u6BCF\u6708\u52A0\u73ED\u6642\u6578\u9650\u5236 ");
      \u0275\u0275elementStart(31, "span", 19);
      \u0275\u0275text(32, "\uFF08\u5C0F\u6642\uFF09");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(33, "input", 20);
      \u0275\u0275conditionalCreate(34, Settings_Conditional_34_Template, 2, 0, "div", 21);
      \u0275\u0275elementEnd()()()();
      \u0275\u0275elementStart(35, "div", 22)(36, "button", 23);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(37, "svg", 24);
      \u0275\u0275element(38, "use", 25);
      \u0275\u0275elementEnd();
      \u0275\u0275text(39, " \u5132\u5B58\u8A2D\u5B9A ");
      \u0275\u0275elementEnd()()()()();
    }
    if (rf & 2) {
      let tmp_2_0;
      \u0275\u0275advance(6);
      \u0275\u0275conditional(ctx.saved ? 6 : -1);
      \u0275\u0275advance();
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(27);
      \u0275\u0275conditional(((tmp_2_0 = ctx.form.get("monthlyOvertimeLimit")) == null ? null : tmp_2_0.invalid) && ((tmp_2_0 = ctx.form.get("monthlyOvertimeLimit")) == null ? null : tmp_2_0.touched) ? 34 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275property("disabled", ctx.form.invalid);
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, DefaultValueAccessor, NumberValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, MaxValidator, FormGroupDirective, FormControlName], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(Settings, [{
    type: Component,
    args: [{ selector: "app-settings", imports: [ReactiveFormsModule], template: `<div class="container-fluid py-3">
  <div class="d-flex align-items-center gap-2 mb-4">
    <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">
      <use href="/assets/icons/sprite.svg#settings"></use>
    </svg>
    <h4 class="mb-0">\u7CFB\u7D71\u8A2D\u5B9A</h4>
  </div>

  @if (saved) {
    <div class="alert alert-success d-flex align-items-center gap-2 mb-4 py-2" role="alert">
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#check-circle"></use></svg>
      \u8A2D\u5B9A\u5DF2\u5132\u5B58\u6210\u529F\u3002
    </div>
  }

  <form [formGroup]="form" (ngSubmit)="submit()">
    <div class="row g-4">

      <div class="col-12 col-md-6 col-xl-4">
        <div class="card border-0 shadow-sm">
          <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">
            <svg class="sa-icon text-primary" style="stroke: currentColor">
              <use href="/assets/icons/sprite.svg#clock"></use>
            </svg>
            \u5DE5\u4F5C\u6642\u9593
          </div>
          <div class="card-body">
            <div class="mb-3">
              <label class="form-label fw-500">\u4E0A\u73ED\u6642\u9593 <span class="text-danger">*</span></label>
              <input type="time" class="form-control" formControlName="workStartTime">
            </div>
            <div class="mb-3">
              <label class="form-label fw-500">\u4E0B\u73ED\u6642\u9593 <span class="text-danger">*</span></label>
              <input type="time" class="form-control" formControlName="workEndTime">
            </div>
            <div class="mb-0">
              <label class="form-label fw-500">\u6BCF\u6708\u52A0\u73ED\u6642\u6578\u9650\u5236 <span class="text-muted fw-400">\uFF08\u5C0F\u6642\uFF09</span></label>
              <input type="number" class="form-control" formControlName="monthlyOvertimeLimit" min="0" max="200">
              @if (form.get('monthlyOvertimeLimit')?.invalid && form.get('monthlyOvertimeLimit')?.touched) {
                <div class="text-danger small mt-1">\u8ACB\u8F38\u5165 0 \u5230 200 \u4E4B\u9593\u7684\u6578\u503C\u3002</div>
              }
            </div>
          </div>
        </div>
      </div>

      <div class="col-12">
        <button type="submit" class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light"
                [disabled]="form.invalid">
          <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#save"></use></svg>
          \u5132\u5B58\u8A2D\u5B9A
        </button>
      </div>

    </div>
  </form>
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(Settings, { className: "Settings", filePath: "src/app/features/admin/settings/pages/settings/settings.ts", lineNumber: 10 });
})();

// src/app/features/admin/departments/pages/department-list/department-list.ts
var _c04 = (a0) => [a0, "edit"];
var _forTrack06 = ($index, $item) => $item.id;
function DepartmentList_For_31_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 18);
    \u0275\u0275text(1, "\u2514");
    \u0275\u0275elementEnd();
  }
}
function DepartmentList_For_31_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td", 17);
    \u0275\u0275conditionalCreate(2, DepartmentList_For_31_Conditional_2_Template, 2, 0, "span", 18);
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "td", 19)(5, "code");
    \u0275\u0275text(6);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(7, "td", 20);
    \u0275\u0275text(8);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 20);
    \u0275\u0275text(10);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(11, "td")(12, "span", 21);
    \u0275\u0275text(13);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(14, "td", 22)(15, "div", 23)(16, "a", 24);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(17, "svg", 7);
    \u0275\u0275element(18, "use", 25);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(19, "button", 26);
    \u0275\u0275listener("click", function DepartmentList_For_31_Template_button_click_19_listener() {
      const dept_r2 = \u0275\u0275restoreView(_r1).$implicit;
      const ctx_r2 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r2.delete(dept_r2));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(20, "svg", 7);
    \u0275\u0275element(21, "use", 27);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const dept_r2 = ctx.$implicit;
    \u0275\u0275advance(2);
    \u0275\u0275conditional(dept_r2.parentId ? 2 : -1);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", dept_r2.name, " ");
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(dept_r2.code ?? "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(dept_r2.parentName ?? "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(dept_r2.description ?? "\u2014");
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1("", dept_r2.employeeCount, " \u4EBA");
    \u0275\u0275advance(3);
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(7, _c04, dept_r2.id));
  }
}
function DepartmentList_ForEmpty_32_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 28);
    \u0275\u0275text(2, "\u5C1A\u7121\u90E8\u9580\u8CC7\u6599\u3002");
    \u0275\u0275elementEnd()();
  }
}
var DepartmentList = class _DepartmentList {
  deptService = inject(DepartmentService);
  departments$ = this.deptService.getAll();
  delete(dept) {
    if (confirm(`\u78BA\u5B9A\u8981\u522A\u9664\u90E8\u9580\u300C${dept.name}\u300D\u55CE\uFF1F`)) {
      this.deptService.delete(dept.id).subscribe();
    }
  }
  static \u0275fac = function DepartmentList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _DepartmentList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _DepartmentList, selectors: [["app-department-list"]], decls: 34, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#git-branch"], [1, "mb-0"], ["routerLink", "new", 1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "d-none", "d-sm-table-cell"], [1, "d-none", "d-md-table-cell"], [1, "text-end"], [1, "fw-500"], [1, "text-muted", "me-1"], [1, "text-muted", "d-none", "d-sm-table-cell"], [1, "text-muted", "d-none", "d-md-table-cell"], [1, "badge", "bg-info"], [1, "text-end", 2, "white-space", "nowrap"], [1, "d-flex", "justify-content-end", "gap-1"], [1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], ["colspan", "6", 1, "text-center", "text-muted", "py-4"]], template: function DepartmentList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u90E8\u9580\u7BA1\u7406");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "a", 6);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(8, "svg", 7);
      \u0275\u0275element(9, "use", 8);
      \u0275\u0275elementEnd();
      \u0275\u0275text(10, " \u65B0\u589E\u90E8\u9580 ");
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(11, "div", 9)(12, "div", 10)(13, "div", 11)(14, "table", 12)(15, "thead", 13)(16, "tr")(17, "th");
      \u0275\u0275text(18, "\u90E8\u9580\u540D\u7A31");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "th", 14);
      \u0275\u0275text(20, "\u4EE3\u78BC");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(21, "th", 15);
      \u0275\u0275text(22, "\u6BCD\u90E8\u9580");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(23, "th", 15);
      \u0275\u0275text(24, "\u63CF\u8FF0");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(25, "th");
      \u0275\u0275text(26, "\u4EBA\u6578");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(27, "th", 16);
      \u0275\u0275text(28, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(29, "tbody");
      \u0275\u0275repeaterCreate(30, DepartmentList_For_31_Template, 22, 9, "tr", null, _forTrack06, false, DepartmentList_ForEmpty_32_Template, 3, 0, "tr");
      \u0275\u0275pipe(33, "async");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(30);
      \u0275\u0275repeater(\u0275\u0275pipeBind1(33, 1, ctx.departments$));
    }
  }, dependencies: [RouterLink, AsyncPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(DepartmentList, [{
    type: Component,
    args: [{ selector: "app-department-list", imports: [RouterLink, AsyncPipe], template: `<div class="container-fluid py-3">\r
  <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">\r
    <div class="d-flex align-items-center gap-2">\r
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">\r
        <use href="/assets/icons/sprite.svg#git-branch"></use>\r
      </svg>\r
      <h4 class="mb-0">\u90E8\u9580\u7BA1\u7406</h4>\r
    </div>\r
    <a routerLink="new" class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light">\r
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>\r
      \u65B0\u589E\u90E8\u9580\r
    </a>\r
  </div>\r
\r
  <div class="card border-0 shadow-sm">\r
    <div class="card-body p-0">\r
      <div class="table-responsive">\r
        <table class="table table-hover mb-0">\r
          <thead class="table-light">\r
            <tr>\r
              <th>\u90E8\u9580\u540D\u7A31</th>\r
              <th class="d-none d-sm-table-cell">\u4EE3\u78BC</th>\r
              <th class="d-none d-md-table-cell">\u6BCD\u90E8\u9580</th>\r
              <th class="d-none d-md-table-cell">\u63CF\u8FF0</th>\r
              <th>\u4EBA\u6578</th>\r
              <th class="text-end">\u64CD\u4F5C</th>\r
            </tr>\r
          </thead>\r
          <tbody>\r
            @for (dept of departments$ | async; track dept.id) {\r
              <tr>\r
                <td class="fw-500">\r
                  @if (dept.parentId) {\r
                    <span class="text-muted me-1">\u2514</span>\r
                  }\r
                  {{ dept.name }}\r
                </td>\r
                <td class="text-muted d-none d-sm-table-cell"><code>{{ dept.code ?? '\u2014' }}</code></td>\r
                <td class="text-muted d-none d-md-table-cell">{{ dept.parentName ?? '\u2014' }}</td>\r
                <td class="text-muted d-none d-md-table-cell">{{ dept.description ?? '\u2014' }}</td>\r
                <td>\r
                  <span class="badge bg-info">{{ dept.employeeCount }} \u4EBA</span>\r
                </td>\r
                <td class="text-end" style="white-space: nowrap">\r
                  <div class="d-flex justify-content-end gap-1">\r
                    <a [routerLink]="[dept.id, 'edit']" class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect">\r
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>\r
                    </a>\r
                    <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect" (click)="delete(dept)">\r
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>\r
                    </button>\r
                  </div>\r
                </td>\r
              </tr>\r
            } @empty {\r
              <tr>\r
                <td colspan="6" class="text-center text-muted py-4">\u5C1A\u7121\u90E8\u9580\u8CC7\u6599\u3002</td>\r
              </tr>\r
            }\r
          </tbody>\r
        </table>\r
      </div>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(DepartmentList, { className: "DepartmentList", filePath: "src/app/features/admin/departments/pages/department-list/department-list.ts", lineNumber: 12 });
})();

// src/app/features/admin/departments/pages/department-form/department-form.ts
var _forTrack07 = ($index, $item) => $item.id;
function DepartmentForm_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u90E8\u9580\u540D\u7A31\u3002");
    \u0275\u0275elementEnd();
  }
}
function DepartmentForm_For_30_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 18);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const dept_r1 = ctx.$implicit;
    \u0275\u0275property("ngValue", dept_r1.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(dept_r1.name);
  }
}
var DepartmentForm = class _DepartmentForm {
  fb = inject(FormBuilder);
  deptService = inject(DepartmentService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  departments = [];
  isEdit = false;
  deptId = 0;
  form = this.fb.group({
    name: ["", Validators.required],
    code: [""],
    description: [""],
    parentId: [null],
    sortOrder: [0]
  });
  ngOnInit() {
    this.deptService.getAll().subscribe((d) => this.departments = d);
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEdit = true;
      this.deptId = +id;
      this.deptService.getById(this.deptId).subscribe((dept) => {
        if (dept)
          this.form.patchValue(__spreadProps(__spreadValues({}, dept), { parentId: dept.parentId ?? null }));
      });
    }
  }
  getAvailableParents() {
    return this.departments.filter((d) => d.id !== this.deptId);
  }
  submit() {
    if (this.form.invalid)
      return;
    const value = this.form.value;
    const obs = this.isEdit ? this.deptService.update(this.deptId, value) : this.deptService.create(value);
    obs.subscribe(() => this.router.navigate(["/admin/departments"]));
  }
  static \u0275fac = function DepartmentForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _DepartmentForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _DepartmentForm, selectors: [["app-department-form"]], decls: 46, vars: 6, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], ["routerLink", "/admin/departments", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "row"], [1, "col-12", "col-lg-8", "col-xl-6"], [1, "card", "border-0", "shadow-sm"], [1, "card-body"], [3, "ngSubmit", "formGroup"], [1, "mb-3"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "name", "placeholder", "\u4F8B\u5982\uFF1A\u7814\u767C\u90E8", 1, "form-control"], [1, "text-danger", "small", "mt-1"], ["type", "text", "formControlName", "code", "placeholder", "\u4F8B\u5982\uFF1ARD", 1, "form-control", "font-monospace"], ["formControlName", "parentId", 1, "form-select"], [3, "ngValue"], ["type", "number", "formControlName", "sortOrder", "min", "0", 1, "form-control"], [1, "text-muted", "small", "mt-1"], [1, "mb-4"], ["formControlName", "description", "rows", "2", "placeholder", "\u9078\u586B\u8AAA\u660E", 1, "form-control"], [1, "d-flex", "gap-2"], ["type", "submit", 1, "btn", "btn-primary", "waves-effect", "waves-light", 3, "disabled"], ["routerLink", "/admin/departments", 1, "btn", "btn-outline-secondary", "waves-effect"]], template: function DepartmentForm_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "a", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "div", 6)(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "form", 10);
      \u0275\u0275listener("ngSubmit", function DepartmentForm_Template_form_ngSubmit_11_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(12, "div", 11)(13, "label", 12);
      \u0275\u0275text(14, "\u90E8\u9580\u540D\u7A31 ");
      \u0275\u0275elementStart(15, "span", 13);
      \u0275\u0275text(16, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(17, "input", 14);
      \u0275\u0275conditionalCreate(18, DepartmentForm_Conditional_18_Template, 2, 0, "div", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "div", 11)(20, "label", 12);
      \u0275\u0275text(21, "\u90E8\u9580\u4EE3\u78BC");
      \u0275\u0275elementEnd();
      \u0275\u0275element(22, "input", 16);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(23, "div", 11)(24, "label", 12);
      \u0275\u0275text(25, "\u4E0A\u5C64\u90E8\u9580");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(26, "select", 17)(27, "option", 18);
      \u0275\u0275text(28, "\u2014 \u7121\uFF08\u9802\u5C64\u90E8\u9580\uFF09\u2014");
      \u0275\u0275elementEnd();
      \u0275\u0275repeaterCreate(29, DepartmentForm_For_30_Template, 2, 2, "option", 18, _forTrack07);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(31, "div", 11)(32, "label", 12);
      \u0275\u0275text(33, "\u6392\u5E8F\u865F");
      \u0275\u0275elementEnd();
      \u0275\u0275element(34, "input", 19);
      \u0275\u0275elementStart(35, "div", 20);
      \u0275\u0275text(36, "\u6578\u5B57\u8D8A\u5C0F\u6392\u8D8A\u524D\u9762\u3002");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(37, "div", 21)(38, "label", 12);
      \u0275\u0275text(39, "\u63CF\u8FF0");
      \u0275\u0275elementEnd();
      \u0275\u0275element(40, "textarea", 22);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(41, "div", 23)(42, "button", 24);
      \u0275\u0275text(43);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(44, "a", 25);
      \u0275\u0275text(45, "\u53D6\u6D88");
      \u0275\u0275elementEnd()()()()()()()();
    }
    if (rf & 2) {
      let tmp_2_0;
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? "\u7DE8\u8F2F\u90E8\u9580" : "\u65B0\u589E\u90E8\u9580");
      \u0275\u0275advance(5);
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_2_0 = ctx.form.get("name")) == null ? null : tmp_2_0.invalid) && ((tmp_2_0 = ctx.form.get("name")) == null ? null : tmp_2_0.touched) ? 18 : -1);
      \u0275\u0275advance(9);
      \u0275\u0275property("ngValue", null);
      \u0275\u0275advance(2);
      \u0275\u0275repeater(ctx.getAvailableParents());
      \u0275\u0275advance(13);
      \u0275\u0275property("disabled", ctx.form.invalid);
      \u0275\u0275advance();
      \u0275\u0275textInterpolate1(" ", ctx.isEdit ? "\u66F4\u65B0" : "\u5EFA\u7ACB", " ");
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, NumberValueAccessor, SelectControlValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, FormGroupDirective, FormControlName, RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(DepartmentForm, [{
    type: Component,
    args: [{ selector: "app-department-form", imports: [ReactiveFormsModule, RouterLink], template: `<div class="container-fluid py-3">\r
  <div class="d-flex align-items-center gap-2 mb-4">\r
    <a routerLink="/admin/departments" class="btn btn-sm btn-outline-secondary waves-effect">\r
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>\r
    </a>\r
    <h4 class="mb-0">{{ isEdit ? '\u7DE8\u8F2F\u90E8\u9580' : '\u65B0\u589E\u90E8\u9580' }}</h4>\r
  </div>\r
\r
  <div class="row">\r
    <div class="col-12 col-lg-8 col-xl-6">\r
      <div class="card border-0 shadow-sm">\r
        <div class="card-body">\r
          <form [formGroup]="form" (ngSubmit)="submit()">\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u90E8\u9580\u540D\u7A31 <span class="text-danger">*</span></label>\r
              <input type="text" class="form-control" formControlName="name" placeholder="\u4F8B\u5982\uFF1A\u7814\u767C\u90E8">\r
              @if (form.get('name')?.invalid && form.get('name')?.touched) {\r
                <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u90E8\u9580\u540D\u7A31\u3002</div>\r
              }\r
            </div>\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u90E8\u9580\u4EE3\u78BC</label>\r
              <input type="text" class="form-control font-monospace" formControlName="code" placeholder="\u4F8B\u5982\uFF1ARD">\r
            </div>\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u4E0A\u5C64\u90E8\u9580</label>\r
              <select class="form-select" formControlName="parentId">\r
                <option [ngValue]="null">\u2014 \u7121\uFF08\u9802\u5C64\u90E8\u9580\uFF09\u2014</option>\r
                @for (dept of getAvailableParents(); track dept.id) {\r
                  <option [ngValue]="dept.id">{{ dept.name }}</option>\r
                }\r
              </select>\r
            </div>\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u6392\u5E8F\u865F</label>\r
              <input type="number" class="form-control" formControlName="sortOrder" min="0">\r
              <div class="text-muted small mt-1">\u6578\u5B57\u8D8A\u5C0F\u6392\u8D8A\u524D\u9762\u3002</div>\r
            </div>\r
\r
            <div class="mb-4">\r
              <label class="form-label fw-500">\u63CF\u8FF0</label>\r
              <textarea class="form-control" formControlName="description" rows="2" placeholder="\u9078\u586B\u8AAA\u660E"></textarea>\r
            </div>\r
\r
            <div class="d-flex gap-2">\r
              <button type="submit" class="btn btn-primary waves-effect waves-light" [disabled]="form.invalid">\r
                {{ isEdit ? '\u66F4\u65B0' : '\u5EFA\u7ACB' }}\r
              </button>\r
              <a routerLink="/admin/departments" class="btn btn-outline-secondary waves-effect">\u53D6\u6D88</a>\r
            </div>\r
\r
          </form>\r
        </div>\r
      </div>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(DepartmentForm, { className: "DepartmentForm", filePath: "src/app/features/admin/departments/pages/department-form/department-form.ts", lineNumber: 12 });
})();

// src/app/features/admin/job-titles/pages/job-title-list/job-title-list.ts
var _c05 = (a0) => [a0, "edit"];
var _forTrack08 = ($index, $item) => $item.id;
function JobTitleList_For_31_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td", 17);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td")(4, "span", 18);
    \u0275\u0275text(5);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(6, "td", 19);
    \u0275\u0275text(7);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "td");
    \u0275\u0275text(9);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "td", 20);
    \u0275\u0275text(11);
    \u0275\u0275pipe(12, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "td", 21)(14, "div", 22)(15, "a", 23);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(16, "svg", 7);
    \u0275\u0275element(17, "use", 24);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(18, "button", 25);
    \u0275\u0275listener("click", function JobTitleList_For_31_Template_button_click_18_listener() {
      const jt_r2 = \u0275\u0275restoreView(_r1).$implicit;
      const ctx_r2 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r2.delete(jt_r2));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(19, "svg", 7);
    \u0275\u0275element(20, "use", 26);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const jt_r2 = ctx.$implicit;
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(jt_r2.name);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1("L", jt_r2.level);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(jt_r2.description || "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(jt_r2.employeeCount);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(12, 6, jt_r2.createdAt, "yyyy-MM-dd"));
    \u0275\u0275advance(4);
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(9, _c05, jt_r2.id));
  }
}
function JobTitleList_ForEmpty_32_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 27);
    \u0275\u0275text(2, "\u5C1A\u7121\u8077\u7A31\u8CC7\u6599\u3002");
    \u0275\u0275elementEnd()();
  }
}
var JobTitleList = class _JobTitleList {
  jobTitleService = inject(JobTitleService);
  jobTitles$ = this.jobTitleService.getAll();
  delete(jt) {
    if (confirm(`\u78BA\u5B9A\u8981\u522A\u9664\u8077\u7A31\u300C${jt.name}\u300D\u55CE\uFF1F`)) {
      this.jobTitleService.delete(jt.id).subscribe();
    }
  }
  static \u0275fac = function JobTitleList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _JobTitleList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _JobTitleList, selectors: [["app-job-title-list"]], decls: 34, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#briefcase"], [1, "mb-0"], ["routerLink", "new", 1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "d-none", "d-md-table-cell"], [1, "d-none", "d-lg-table-cell"], [1, "text-end"], [1, "fw-500"], [1, "badge", "bg-secondary-subtle", "text-secondary"], [1, "text-muted", "d-none", "d-md-table-cell"], [1, "text-muted", "small", "d-none", "d-lg-table-cell"], [1, "text-end", 2, "white-space", "nowrap"], [1, "d-flex", "justify-content-end", "gap-1"], [1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], ["colspan", "6", 1, "text-center", "text-muted", "py-4"]], template: function JobTitleList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u8077\u7A31\u7BA1\u7406");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "a", 6);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(8, "svg", 7);
      \u0275\u0275element(9, "use", 8);
      \u0275\u0275elementEnd();
      \u0275\u0275text(10, " \u65B0\u589E\u8077\u7A31 ");
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(11, "div", 9)(12, "div", 10)(13, "div", 11)(14, "table", 12)(15, "thead", 13)(16, "tr")(17, "th");
      \u0275\u0275text(18, "\u8077\u7A31\u540D\u7A31");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "th");
      \u0275\u0275text(20, "\u7B49\u7D1A");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(21, "th", 14);
      \u0275\u0275text(22, "\u63CF\u8FF0");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(23, "th");
      \u0275\u0275text(24, "\u54E1\u5DE5\u4EBA\u6578");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(25, "th", 15);
      \u0275\u0275text(26, "\u5EFA\u7ACB\u6642\u9593");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(27, "th", 16);
      \u0275\u0275text(28, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(29, "tbody");
      \u0275\u0275repeaterCreate(30, JobTitleList_For_31_Template, 21, 11, "tr", null, _forTrack08, false, JobTitleList_ForEmpty_32_Template, 3, 0, "tr");
      \u0275\u0275pipe(33, "async");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(30);
      \u0275\u0275repeater(\u0275\u0275pipeBind1(33, 1, ctx.jobTitles$));
    }
  }, dependencies: [RouterLink, AsyncPipe, DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(JobTitleList, [{
    type: Component,
    args: [{ selector: "app-job-title-list", imports: [RouterLink, AsyncPipe, DatePipe], template: `<div class="container-fluid py-3">
  <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">
    <div class="d-flex align-items-center gap-2">
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">
        <use href="/assets/icons/sprite.svg#briefcase"></use>
      </svg>
      <h4 class="mb-0">\u8077\u7A31\u7BA1\u7406</h4>
    </div>
    <a routerLink="new" class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light">
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
      \u65B0\u589E\u8077\u7A31
    </a>
  </div>

  <div class="card border-0 shadow-sm">
    <div class="card-body p-0">
      <div class="table-responsive">
        <table class="table table-hover mb-0">
          <thead class="table-light">
            <tr>
              <th>\u8077\u7A31\u540D\u7A31</th>
              <th>\u7B49\u7D1A</th>
              <th class="d-none d-md-table-cell">\u63CF\u8FF0</th>
              <th>\u54E1\u5DE5\u4EBA\u6578</th>
              <th class="d-none d-lg-table-cell">\u5EFA\u7ACB\u6642\u9593</th>
              <th class="text-end">\u64CD\u4F5C</th>
            </tr>
          </thead>
          <tbody>
            @for (jt of jobTitles$ | async; track jt.id) {
              <tr>
                <td class="fw-500">{{ jt.name }}</td>
                <td>
                  <span class="badge bg-secondary-subtle text-secondary">L{{ jt.level }}</span>
                </td>
                <td class="text-muted d-none d-md-table-cell">{{ jt.description || '\u2014' }}</td>
                <td>{{ jt.employeeCount }}</td>
                <td class="text-muted small d-none d-lg-table-cell">{{ jt.createdAt | date:'yyyy-MM-dd' }}</td>
                <td class="text-end" style="white-space: nowrap">
                  <div class="d-flex justify-content-end gap-1">
                    <a [routerLink]="[jt.id, 'edit']" class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>
                    </a>
                    <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect" (click)="delete(jt)">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>
                    </button>
                  </div>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="6" class="text-center text-muted py-4">\u5C1A\u7121\u8077\u7A31\u8CC7\u6599\u3002</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  </div>
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(JobTitleList, { className: "JobTitleList", filePath: "src/app/features/admin/job-titles/pages/job-title-list/job-title-list.ts", lineNumber: 12 });
})();

// src/app/features/admin/job-titles/pages/job-title-form/job-title-form.ts
function JobTitleForm_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u8077\u7A31\u540D\u7A31\u3002");
    \u0275\u0275elementEnd();
  }
}
function JobTitleForm_Conditional_27_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u6709\u6548\u7B49\u7D1A\uFF08\u6700\u5C0F\u503C\u70BA 1\uFF09\u3002");
    \u0275\u0275elementEnd();
  }
}
var JobTitleForm = class _JobTitleForm {
  fb = inject(FormBuilder);
  jobTitleService = inject(JobTitleService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  isEdit = false;
  jobTitleId = 0;
  form = this.fb.group({
    name: ["", Validators.required],
    level: [1, [Validators.required, Validators.min(1)]],
    description: [""]
  });
  ngOnInit() {
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEdit = true;
      this.jobTitleId = +id;
      this.jobTitleService.getById(this.jobTitleId).subscribe((jt) => {
        if (jt)
          this.form.patchValue(jt);
      });
    }
  }
  submit() {
    if (this.form.invalid)
      return;
    const value = this.form.value;
    const obs = this.isEdit ? this.jobTitleService.update(this.jobTitleId, value) : this.jobTitleService.create(value);
    obs.subscribe(() => this.router.navigate(["/admin/job-titles"]));
  }
  static \u0275fac = function JobTitleForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _JobTitleForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _JobTitleForm, selectors: [["app-job-title-form"]], decls: 37, vars: 6, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], ["routerLink", "/admin/job-titles", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "row"], [1, "col-12", "col-lg-6", "col-xl-4"], [1, "card", "border-0", "shadow-sm"], [1, "card-body"], [3, "ngSubmit", "formGroup"], [1, "mb-3"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "name", "placeholder", "\u4F8B\u5982\uFF1A\u8CC7\u6DF1\u5DE5\u7A0B\u5E2B", 1, "form-control"], [1, "text-danger", "small", "mt-1"], ["type", "number", "formControlName", "level", "min", "1", "placeholder", "\u4F8B\u5982\uFF1A2", 1, "form-control"], [1, "text-muted", "small", "mt-1"], [1, "mb-4"], ["formControlName", "description", "rows", "2", "placeholder", "\u9078\u586B\u8AAA\u660E", 1, "form-control"], [1, "d-flex", "gap-2"], ["type", "submit", 1, "btn", "btn-primary", "waves-effect", "waves-light", 3, "disabled"], ["routerLink", "/admin/job-titles", 1, "btn", "btn-outline-secondary", "waves-effect"]], template: function JobTitleForm_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "a", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "div", 6)(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "form", 10);
      \u0275\u0275listener("ngSubmit", function JobTitleForm_Template_form_ngSubmit_11_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(12, "div", 11)(13, "label", 12);
      \u0275\u0275text(14, "\u8077\u7A31\u540D\u7A31 ");
      \u0275\u0275elementStart(15, "span", 13);
      \u0275\u0275text(16, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(17, "input", 14);
      \u0275\u0275conditionalCreate(18, JobTitleForm_Conditional_18_Template, 2, 0, "div", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "div", 11)(20, "label", 12);
      \u0275\u0275text(21, "\u7B49\u7D1A ");
      \u0275\u0275elementStart(22, "span", 13);
      \u0275\u0275text(23, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(24, "input", 16);
      \u0275\u0275elementStart(25, "div", 17);
      \u0275\u0275text(26, "\u6578\u5B57\u8D8A\u5927\u7B49\u7D1A\u8D8A\u9AD8\u3002");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(27, JobTitleForm_Conditional_27_Template, 2, 0, "div", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(28, "div", 18)(29, "label", 12);
      \u0275\u0275text(30, "\u63CF\u8FF0");
      \u0275\u0275elementEnd();
      \u0275\u0275element(31, "textarea", 19);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(32, "div", 20)(33, "button", 21);
      \u0275\u0275text(34);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(35, "a", 22);
      \u0275\u0275text(36, "\u53D6\u6D88");
      \u0275\u0275elementEnd()()()()()()()();
    }
    if (rf & 2) {
      let tmp_2_0;
      let tmp_3_0;
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? "\u7DE8\u8F2F\u8077\u7A31" : "\u65B0\u589E\u8077\u7A31");
      \u0275\u0275advance(5);
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_2_0 = ctx.form.get("name")) == null ? null : tmp_2_0.invalid) && ((tmp_2_0 = ctx.form.get("name")) == null ? null : tmp_2_0.touched) ? 18 : -1);
      \u0275\u0275advance(9);
      \u0275\u0275conditional(((tmp_3_0 = ctx.form.get("level")) == null ? null : tmp_3_0.invalid) && ((tmp_3_0 = ctx.form.get("level")) == null ? null : tmp_3_0.touched) ? 27 : -1);
      \u0275\u0275advance(6);
      \u0275\u0275property("disabled", ctx.form.invalid);
      \u0275\u0275advance();
      \u0275\u0275textInterpolate1(" ", ctx.isEdit ? "\u66F4\u65B0" : "\u5EFA\u7ACB", " ");
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, DefaultValueAccessor, NumberValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, FormGroupDirective, FormControlName, RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(JobTitleForm, [{
    type: Component,
    args: [{ selector: "app-job-title-form", imports: [ReactiveFormsModule, RouterLink], template: `<div class="container-fluid py-3">\r
  <div class="d-flex align-items-center gap-2 mb-4">\r
    <a routerLink="/admin/job-titles" class="btn btn-sm btn-outline-secondary waves-effect">\r
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>\r
    </a>\r
    <h4 class="mb-0">{{ isEdit ? '\u7DE8\u8F2F\u8077\u7A31' : '\u65B0\u589E\u8077\u7A31' }}</h4>\r
  </div>\r
\r
  <div class="row">\r
    <div class="col-12 col-lg-6 col-xl-4">\r
      <div class="card border-0 shadow-sm">\r
        <div class="card-body">\r
          <form [formGroup]="form" (ngSubmit)="submit()">\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u8077\u7A31\u540D\u7A31 <span class="text-danger">*</span></label>\r
              <input type="text" class="form-control" formControlName="name" placeholder="\u4F8B\u5982\uFF1A\u8CC7\u6DF1\u5DE5\u7A0B\u5E2B">\r
              @if (form.get('name')?.invalid && form.get('name')?.touched) {\r
                <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u8077\u7A31\u540D\u7A31\u3002</div>\r
              }\r
            </div>\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u7B49\u7D1A <span class="text-danger">*</span></label>\r
              <input type="number" class="form-control" formControlName="level" min="1" placeholder="\u4F8B\u5982\uFF1A2">\r
              <div class="text-muted small mt-1">\u6578\u5B57\u8D8A\u5927\u7B49\u7D1A\u8D8A\u9AD8\u3002</div>\r
              @if (form.get('level')?.invalid && form.get('level')?.touched) {\r
                <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u6709\u6548\u7B49\u7D1A\uFF08\u6700\u5C0F\u503C\u70BA 1\uFF09\u3002</div>\r
              }\r
            </div>\r
\r
            <div class="mb-4">\r
              <label class="form-label fw-500">\u63CF\u8FF0</label>\r
              <textarea class="form-control" formControlName="description" rows="2" placeholder="\u9078\u586B\u8AAA\u660E"></textarea>\r
            </div>\r
\r
            <div class="d-flex gap-2">\r
              <button type="submit" class="btn btn-primary waves-effect waves-light" [disabled]="form.invalid">\r
                {{ isEdit ? '\u66F4\u65B0' : '\u5EFA\u7ACB' }}\r
              </button>\r
              <a routerLink="/admin/job-titles" class="btn btn-outline-secondary waves-effect">\u53D6\u6D88</a>\r
            </div>\r
\r
          </form>\r
        </div>\r
      </div>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(JobTitleForm, { className: "JobTitleForm", filePath: "src/app/features/admin/job-titles/pages/job-title-form/job-title-form.ts", lineNumber: 11 });
})();

// src/app/features/admin/approvals/services/approval.service.ts
var MOCK_ITEMS = [
  {
    id: 1,
    name: "\u8ACB\u5047\u7533\u8ACB",
    code: "leave_request",
    description: "\u54E1\u5DE5\u8ACB\u5047\u9700\u7D93\u4E3B\u7BA1\u5BE9\u6838",
    isActive: true,
    applicationType: "leave",
    steps: [
      { id: 1, stepOrder: 1, departmentId: 1, departmentName: "\u7BA1\u7406\u90E8", jobTitleId: 4, jobTitleName: "\u90E8\u9580\u4E3B\u7BA1", note: "\u76F4\u5C6C\u4E3B\u7BA1\u6838\u53EF" },
      { id: 2, stepOrder: 2, departmentId: 1, departmentName: "\u7BA1\u7406\u90E8", jobTitleId: 4, jobTitleName: "\u90E8\u9580\u4E3B\u7BA1", note: "\u4EBA\u4E8B\u90E8\u9580\u78BA\u8A8D" }
    ],
    createdAt: /* @__PURE__ */ new Date("2024-01-01")
  },
  {
    id: 2,
    name: "\u63A1\u8CFC\u7533\u8ACB",
    code: "purchase_request",
    description: "\u7269\u54C1\u63A1\u8CFC\u9700\u9010\u7D1A\u5BE9\u6838",
    isActive: true,
    applicationType: "payment_request",
    steps: [
      { id: 3, stepOrder: 1, jobTitleId: 4, jobTitleName: "\u90E8\u9580\u4E3B\u7BA1", note: "\u90E8\u9580\u4E3B\u7BA1\u5BE9\u6838" },
      { id: 4, stepOrder: 2, departmentId: 1, departmentName: "\u7BA1\u7406\u90E8", jobTitleId: 4, jobTitleName: "\u90E8\u9580\u4E3B\u7BA1", note: "\u7BA1\u7406\u90E8\u6838\u51C6" }
    ],
    createdAt: /* @__PURE__ */ new Date("2024-01-01")
  }
];
var ApprovalService = class _ApprovalService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_ITEMS);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id) {
    return of(this.items$.getValue().find((a) => a.id === id));
  }
  create(data) {
    const item = __spreadProps(__spreadValues({}, data), { id: Date.now(), steps: [], createdAt: /* @__PURE__ */ new Date() });
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }
  update(id, changes) {
    const updated = this.items$.getValue().map((a) => a.id === id ? __spreadValues(__spreadValues({}, a), changes) : a);
    this.items$.next(updated);
    return of(updated.find((a) => a.id === id));
  }
  delete(id) {
    this.items$.next(this.items$.getValue().filter((a) => a.id !== id));
    return of(void 0);
  }
  // ── Steps ──────────────────────────────────────────────────────────────
  addStep(itemId, step) {
    const newStep = __spreadProps(__spreadValues({}, step), { id: Date.now() });
    const updated = this.items$.getValue().map((a) => a.id === itemId ? __spreadProps(__spreadValues({}, a), { steps: [...a.steps, newStep].sort((x, y) => x.stepOrder - y.stepOrder) }) : a);
    this.items$.next(updated);
    return of(updated.find((a) => a.id === itemId));
  }
  updateStep(itemId, stepId, changes) {
    const updated = this.items$.getValue().map((a) => {
      if (a.id !== itemId)
        return a;
      const steps = a.steps.map((s) => s.id === stepId ? __spreadValues(__spreadValues({}, s), changes) : s).sort((x, y) => x.stepOrder - y.stepOrder);
      return __spreadProps(__spreadValues({}, a), { steps });
    });
    this.items$.next(updated);
    return of(updated.find((a) => a.id === itemId));
  }
  deleteStep(itemId, stepId) {
    const updated = this.items$.getValue().map((a) => a.id === itemId ? __spreadProps(__spreadValues({}, a), { steps: a.steps.filter((s) => s.id !== stepId) }) : a);
    this.items$.next(updated);
    return of(updated.find((a) => a.id === itemId));
  }
  static \u0275fac = function ApprovalService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ApprovalService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _ApprovalService, factory: _ApprovalService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ApprovalService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/approvals/models/approval.model.ts
var APPLICATION_TYPE_LABELS = {
  payment_request: "\u8ACB\u6B3E\u7533\u8ACB",
  leave: "\u8ACB\u5047\u7533\u8ACB",
  travel: "\u51FA\u5DEE\u7533\u8ACB"
};
var APPLICATION_TYPE_CLASSES = {
  payment_request: "bg-info-subtle text-info",
  leave: "bg-success-subtle text-success",
  travel: "bg-primary-subtle text-primary"
};

// src/app/features/admin/approvals/pages/approval-list/approval-list.ts
var _c06 = (a0) => [a0, "flow"];
var _forTrack09 = ($index, $item) => $item.id;
var _forTrack1 = ($index, $item) => $item.value;
function ApprovalList_Conditional_11_Conditional_5_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 30);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u9805\u76EE\u540D\u7A31\u3002");
    \u0275\u0275elementEnd();
  }
}
function ApprovalList_Conditional_11_Conditional_5_Conditional_14_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 30);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u4EE3\u78BC\u3002");
    \u0275\u0275elementEnd();
  }
}
function ApprovalList_Conditional_11_Conditional_5_For_24_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 35);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const opt_r3 = ctx.$implicit;
    const items_r4 = \u0275\u0275nextContext();
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275property("value", opt_r3.value)("disabled", ctx_r1.isTypeDisabled(opt_r3.value, items_r4));
    \u0275\u0275advance();
    \u0275\u0275textInterpolate2(" ", opt_r3.label, "", ctx_r1.isTypeDisabled(opt_r3.value, items_r4) ? "\uFF08\u5DF2\u8A2D\u5B9A\uFF09" : "", " ");
  }
}
function ApprovalList_Conditional_11_Conditional_5_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22)(1, "div", 26)(2, "label", 27);
    \u0275\u0275text(3, "\u9805\u76EE\u540D\u7A31 ");
    \u0275\u0275elementStart(4, "span", 28);
    \u0275\u0275text(5, "*");
    \u0275\u0275elementEnd()();
    \u0275\u0275element(6, "input", 29);
    \u0275\u0275conditionalCreate(7, ApprovalList_Conditional_11_Conditional_5_Conditional_7_Template, 2, 0, "div", 30);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "div", 31)(9, "label", 27);
    \u0275\u0275text(10, "\u4EE3\u78BC ");
    \u0275\u0275elementStart(11, "span", 28);
    \u0275\u0275text(12, "*");
    \u0275\u0275elementEnd()();
    \u0275\u0275element(13, "input", 32);
    \u0275\u0275conditionalCreate(14, ApprovalList_Conditional_11_Conditional_5_Conditional_14_Template, 2, 0, "div", 30);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "div", 26)(16, "label", 27);
    \u0275\u0275text(17, "\u63CF\u8FF0");
    \u0275\u0275elementEnd();
    \u0275\u0275element(18, "input", 33);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "div", 26)(20, "label", 27);
    \u0275\u0275text(21, "\u9069\u7528\u985E\u578B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(22, "select", 34);
    \u0275\u0275repeaterCreate(23, ApprovalList_Conditional_11_Conditional_5_For_24_Template, 2, 4, "option", 35, _forTrack1);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(25, "div", 36)(26, "div", 37);
    \u0275\u0275element(27, "input", 38);
    \u0275\u0275elementStart(28, "label", 39);
    \u0275\u0275text(29, "\u555F\u7528");
    \u0275\u0275elementEnd()()()();
  }
  if (rf & 2) {
    let tmp_3_0;
    let tmp_4_0;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(7);
    \u0275\u0275conditional(((tmp_3_0 = ctx_r1.form.get("name")) == null ? null : tmp_3_0.invalid) && ((tmp_3_0 = ctx_r1.form.get("name")) == null ? null : tmp_3_0.touched) ? 7 : -1);
    \u0275\u0275advance(7);
    \u0275\u0275conditional(((tmp_4_0 = ctx_r1.form.get("code")) == null ? null : tmp_4_0.invalid) && ((tmp_4_0 = ctx_r1.form.get("code")) == null ? null : tmp_4_0.touched) ? 14 : -1);
    \u0275\u0275advance(9);
    \u0275\u0275repeater(ctx_r1.appTypeOptions);
  }
}
function ApprovalList_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 9)(1, "div", 19);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 20)(4, "form", 21);
    \u0275\u0275listener("ngSubmit", function ApprovalList_Conditional_11_Template_form_ngSubmit_4_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.submit());
    });
    \u0275\u0275conditionalCreate(5, ApprovalList_Conditional_11_Conditional_5_Template, 30, 2, "div", 22);
    \u0275\u0275pipe(6, "async");
    \u0275\u0275elementStart(7, "div", 23)(8, "button", 24);
    \u0275\u0275text(9);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "button", 25);
    \u0275\u0275listener("click", function ApprovalList_Conditional_11_Template_button_click_10_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.closeForm());
    });
    \u0275\u0275text(11, "\u53D6\u6D88");
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    let tmp_3_0;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", ctx_r1.editItem ? "\u7DE8\u8F2F\u7C3D\u6838\u9805\u76EE" : "\u65B0\u589E\u7C3D\u6838\u9805\u76EE", " ");
    \u0275\u0275advance(2);
    \u0275\u0275property("formGroup", ctx_r1.form);
    \u0275\u0275advance();
    \u0275\u0275conditional((tmp_3_0 = \u0275\u0275pipeBind1(6, 5, ctx_r1.items$)) ? 5 : -1, tmp_3_0);
    \u0275\u0275advance(3);
    \u0275\u0275property("disabled", ctx_r1.form.invalid);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.editItem ? "\u66F4\u65B0" : "\u5EFA\u7ACB", " ");
  }
}
function ApprovalList_For_36_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span");
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const item_r6 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275classMap("badge " + ctx_r1.appTypeClasses[item_r6.applicationType]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.appTypeLabels[item_r6.applicationType], " ");
  }
}
function ApprovalList_For_36_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 41);
    \u0275\u0275text(1, "\u901A\u7528");
    \u0275\u0275elementEnd();
  }
}
function ApprovalList_For_36_Template(rf, ctx) {
  if (rf & 1) {
    const _r5 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td", 40);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td")(4, "code", 41);
    \u0275\u0275text(5);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(6, "td", 15);
    \u0275\u0275conditionalCreate(7, ApprovalList_For_36_Conditional_7_Template, 2, 3, "span", 42)(8, ApprovalList_For_36_Conditional_8_Template, 2, 0, "span", 41);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 43);
    \u0275\u0275text(10);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(11, "td")(12, "span", 44);
    \u0275\u0275text(13);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(14, "td")(15, "span");
    \u0275\u0275text(16);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(17, "td", 45);
    \u0275\u0275text(18);
    \u0275\u0275pipe(19, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(20, "td", 46)(21, "div", 47)(22, "a", 48);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(23, "svg", 7);
    \u0275\u0275element(24, "use", 49);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(25, "button", 50);
    \u0275\u0275listener("click", function ApprovalList_For_36_Template_button_click_25_listener() {
      const item_r6 = \u0275\u0275restoreView(_r5).$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.openEdit(item_r6));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(26, "svg", 7);
    \u0275\u0275element(27, "use", 51);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(28, "button", 52);
    \u0275\u0275listener("click", function ApprovalList_For_36_Template_button_click_28_listener() {
      const item_r6 = \u0275\u0275restoreView(_r5).$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.delete(item_r6));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(29, "svg", 7);
    \u0275\u0275element(30, "use", 53);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const item_r6 = ctx.$implicit;
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r6.name);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(item_r6.code);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(item_r6.applicationType ? 7 : 8);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(item_r6.description || "\u2014");
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1("", item_r6.steps.length, " \u6B65");
    \u0275\u0275advance(2);
    \u0275\u0275classMap("badge " + (item_r6.isActive ? "bg-success-subtle text-success" : "bg-secondary-subtle text-secondary"));
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", item_r6.isActive ? "\u555F\u7528" : "\u505C\u7528", " ");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(19, 10, item_r6.createdAt, "yyyy-MM-dd"));
    \u0275\u0275advance(4);
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(13, _c06, item_r6.id));
  }
}
function ApprovalList_ForEmpty_37_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 54);
    \u0275\u0275text(2, "\u5C1A\u7121\u7C3D\u6838\u9805\u76EE\u3002");
    \u0275\u0275elementEnd()();
  }
}
var ApprovalList = class _ApprovalList {
  approvalService = inject(ApprovalService);
  fb = inject(FormBuilder);
  items$ = this.approvalService.getAll();
  showForm = false;
  editItem = null;
  appTypeLabels = APPLICATION_TYPE_LABELS;
  appTypeClasses = APPLICATION_TYPE_CLASSES;
  appTypeOptions = [
    { value: "", label: "\u901A\u7528\uFF08\u4E0D\u7D81\u5B9A\uFF09" },
    { value: "payment_request", label: "\u8ACB\u6B3E\u7533\u8ACB" },
    { value: "leave", label: "\u8ACB\u5047\u7533\u8ACB" },
    { value: "travel", label: "\u51FA\u5DEE\u7533\u8ACB" }
  ];
  form = this.fb.group({
    name: ["", Validators.required],
    code: ["", Validators.required],
    description: [""],
    isActive: [true],
    applicationType: [""]
  });
  openCreate() {
    this.editItem = null;
    this.form.reset({ isActive: true, applicationType: "" });
    this.showForm = true;
  }
  openEdit(item) {
    this.editItem = item;
    this.form.patchValue(__spreadProps(__spreadValues({}, item), { applicationType: item.applicationType ?? "" }));
    this.showForm = true;
  }
  closeForm() {
    this.showForm = false;
  }
  isTypeDisabled(value, items) {
    if (!value)
      return false;
    return items.some((i) => i.applicationType === value && i.id !== this.editItem?.id);
  }
  submit() {
    if (this.form.invalid)
      return;
    const raw = this.form.value;
    const data = __spreadProps(__spreadValues({}, raw), {
      applicationType: raw.applicationType || void 0
    });
    const obs = this.editItem ? this.approvalService.update(this.editItem.id, data) : this.approvalService.create(data);
    obs.subscribe(() => this.showForm = false);
  }
  delete(item) {
    if (confirm(`\u78BA\u5B9A\u8981\u522A\u9664\u7C3D\u6838\u9805\u76EE\u300C${item.name}\u300D\u55CE\uFF1F`)) {
      this.approvalService.delete(item.id).subscribe();
    }
  }
  toggleActive(item) {
    this.approvalService.update(item.id, { isActive: !item.isActive }).subscribe();
  }
  static \u0275fac = function ApprovalList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ApprovalList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ApprovalList, selectors: [["app-approval-list"]], decls: 39, vars: 4, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#check-square"], [1, "mb-0"], [1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light", 3, "click"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm", "mb-4"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "d-none", "d-md-table-cell"], [1, "d-none", "d-xl-table-cell"], [1, "d-none", "d-lg-table-cell"], [1, "text-end"], [1, "card-header", "bg-transparent", "fw-500"], [1, "card-body"], [3, "ngSubmit", "formGroup"], [1, "row", "g-3"], [1, "d-flex", "gap-2", "mt-3"], ["type", "submit", 1, "btn", "btn-primary", "btn-sm", "waves-effect", "waves-light", 3, "disabled"], ["type", "button", 1, "btn", "btn-outline-secondary", "btn-sm", "waves-effect", 3, "click"], [1, "col-12", "col-md-3"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "name", "placeholder", "\u4F8B\u5982\uFF1A\u8ACB\u5047\u7533\u8ACB", 1, "form-control"], [1, "text-danger", "small", "mt-1"], [1, "col-12", "col-md-2"], ["type", "text", "formControlName", "code", "placeholder", "\u4F8B\u5982\uFF1Aleave_request", 1, "form-control", "font-monospace"], ["type", "text", "formControlName", "description", "placeholder", "\u9078\u586B\u8AAA\u660E", 1, "form-control"], ["formControlName", "applicationType", 1, "form-select"], [3, "value", "disabled"], [1, "col-12", "col-md-1", "d-flex", "align-items-end"], [1, "form-check"], ["type", "checkbox", "formControlName", "isActive", "id", "isActive", 1, "form-check-input"], ["for", "isActive", 1, "form-check-label"], [1, "fw-500"], [1, "text-muted", "small"], [3, "class"], [1, "text-muted", "d-none", "d-xl-table-cell"], [1, "badge", "bg-info-subtle", "text-info"], [1, "text-muted", "small", "d-none", "d-lg-table-cell"], [1, "text-end", 2, "white-space", "nowrap"], [1, "d-flex", "justify-content-end", "gap-1"], ["title", "\u7BA1\u7406\u6D41\u7A0B", 1, "btn", "btn-sm", "btn-ghost-info", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#git-merge"], ["title", "\u7DE8\u8F2F", 1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#edit"], ["title", "\u522A\u9664", 1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], ["colspan", "8", 1, "text-center", "text-muted", "py-4"]], template: function ApprovalList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u7C3D\u6838\u7BA1\u7406");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "button", 6);
      \u0275\u0275listener("click", function ApprovalList_Template_button_click_7_listener() {
        return ctx.openCreate();
      });
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(8, "svg", 7);
      \u0275\u0275element(9, "use", 8);
      \u0275\u0275elementEnd();
      \u0275\u0275text(10, " \u65B0\u589E\u9805\u76EE ");
      \u0275\u0275elementEnd()();
      \u0275\u0275conditionalCreate(11, ApprovalList_Conditional_11_Template, 12, 7, "div", 9);
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(12, "div", 10)(13, "div", 11)(14, "div", 12)(15, "table", 13)(16, "thead", 14)(17, "tr")(18, "th");
      \u0275\u0275text(19, "\u9805\u76EE\u540D\u7A31");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(20, "th");
      \u0275\u0275text(21, "\u4EE3\u78BC");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(22, "th", 15);
      \u0275\u0275text(23, "\u9069\u7528\u985E\u578B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(24, "th", 16);
      \u0275\u0275text(25, "\u63CF\u8FF0");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(26, "th");
      \u0275\u0275text(27, "\u6B65\u9A5F\u6578");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(28, "th");
      \u0275\u0275text(29, "\u72C0\u614B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(30, "th", 17);
      \u0275\u0275text(31, "\u5EFA\u7ACB\u6642\u9593");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(32, "th", 18);
      \u0275\u0275text(33, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(34, "tbody");
      \u0275\u0275repeaterCreate(35, ApprovalList_For_36_Template, 31, 15, "tr", null, _forTrack09, false, ApprovalList_ForEmpty_37_Template, 3, 0, "tr");
      \u0275\u0275pipe(38, "async");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(11);
      \u0275\u0275conditional(ctx.showForm ? 11 : -1);
      \u0275\u0275advance(24);
      \u0275\u0275repeater(\u0275\u0275pipeBind1(38, 2, ctx.items$));
    }
  }, dependencies: [RouterLink, ReactiveFormsModule, \u0275NgNoValidate, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, CheckboxControlValueAccessor, SelectControlValueAccessor, NgControlStatus, NgControlStatusGroup, FormGroupDirective, FormControlName, AsyncPipe, DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ApprovalList, [{
    type: Component,
    args: [{ selector: "app-approval-list", imports: [RouterLink, AsyncPipe, DatePipe, ReactiveFormsModule], template: `<div class="container-fluid py-3">
  <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">
    <div class="d-flex align-items-center gap-2">
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">
        <use href="/assets/icons/sprite.svg#check-square"></use>
      </svg>
      <h4 class="mb-0">\u7C3D\u6838\u7BA1\u7406</h4>
    </div>
    <button class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light" (click)="openCreate()">
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
      \u65B0\u589E\u9805\u76EE
    </button>
  </div>

  <!-- Inline Form Panel -->
  @if (showForm) {
    <div class="card border-0 shadow-sm mb-4">
      <div class="card-header bg-transparent fw-500">
        {{ editItem ? '\u7DE8\u8F2F\u7C3D\u6838\u9805\u76EE' : '\u65B0\u589E\u7C3D\u6838\u9805\u76EE' }}
      </div>
      <div class="card-body">
        <form [formGroup]="form" (ngSubmit)="submit()">
          @if (items$ | async; as items) {
            <div class="row g-3">
              <div class="col-12 col-md-3">
                <label class="form-label fw-500">\u9805\u76EE\u540D\u7A31 <span class="text-danger">*</span></label>
                <input type="text" class="form-control" formControlName="name" placeholder="\u4F8B\u5982\uFF1A\u8ACB\u5047\u7533\u8ACB">
                @if (form.get('name')?.invalid && form.get('name')?.touched) {
                  <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u9805\u76EE\u540D\u7A31\u3002</div>
                }
              </div>
              <div class="col-12 col-md-2">
                <label class="form-label fw-500">\u4EE3\u78BC <span class="text-danger">*</span></label>
                <input type="text" class="form-control font-monospace" formControlName="code" placeholder="\u4F8B\u5982\uFF1Aleave_request">
                @if (form.get('code')?.invalid && form.get('code')?.touched) {
                  <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u4EE3\u78BC\u3002</div>
                }
              </div>
              <div class="col-12 col-md-3">
                <label class="form-label fw-500">\u63CF\u8FF0</label>
                <input type="text" class="form-control" formControlName="description" placeholder="\u9078\u586B\u8AAA\u660E">
              </div>
              <div class="col-12 col-md-3">
                <label class="form-label fw-500">\u9069\u7528\u985E\u578B</label>
                <select class="form-select" formControlName="applicationType">
                  @for (opt of appTypeOptions; track opt.value) {
                    <option [value]="opt.value" [disabled]="isTypeDisabled(opt.value, items)">
                      {{ opt.label }}{{ isTypeDisabled(opt.value, items) ? '\uFF08\u5DF2\u8A2D\u5B9A\uFF09' : '' }}
                    </option>
                  }
                </select>
              </div>
              <div class="col-12 col-md-1 d-flex align-items-end">
                <div class="form-check">
                  <input type="checkbox" class="form-check-input" formControlName="isActive" id="isActive">
                  <label class="form-check-label" for="isActive">\u555F\u7528</label>
                </div>
              </div>
            </div>
          }
          <div class="d-flex gap-2 mt-3">
            <button type="submit" class="btn btn-primary btn-sm waves-effect waves-light" [disabled]="form.invalid">
              {{ editItem ? '\u66F4\u65B0' : '\u5EFA\u7ACB' }}
            </button>
            <button type="button" class="btn btn-outline-secondary btn-sm waves-effect" (click)="closeForm()">\u53D6\u6D88</button>
          </div>
        </form>
      </div>
    </div>
  }

  <!-- Items Table -->
  <div class="card border-0 shadow-sm">
    <div class="card-body p-0">
      <div class="table-responsive">
        <table class="table table-hover mb-0">
          <thead class="table-light">
            <tr>
              <th>\u9805\u76EE\u540D\u7A31</th>
              <th>\u4EE3\u78BC</th>
              <th class="d-none d-md-table-cell">\u9069\u7528\u985E\u578B</th>
              <th class="d-none d-xl-table-cell">\u63CF\u8FF0</th>
              <th>\u6B65\u9A5F\u6578</th>
              <th>\u72C0\u614B</th>
              <th class="d-none d-lg-table-cell">\u5EFA\u7ACB\u6642\u9593</th>
              <th class="text-end">\u64CD\u4F5C</th>
            </tr>
          </thead>
          <tbody>
            @for (item of items$ | async; track item.id) {
              <tr>
                <td class="fw-500">{{ item.name }}</td>
                <td><code class="text-muted small">{{ item.code }}</code></td>
                <td class="d-none d-md-table-cell">
                  @if (item.applicationType) {
                    <span [class]="'badge ' + appTypeClasses[item.applicationType]">
                      {{ appTypeLabels[item.applicationType] }}
                    </span>
                  } @else {
                    <span class="text-muted small">\u901A\u7528</span>
                  }
                </td>
                <td class="text-muted d-none d-xl-table-cell">{{ item.description || '\u2014' }}</td>
                <td>
                  <span class="badge bg-info-subtle text-info">{{ item.steps.length }} \u6B65</span>
                </td>
                <td>
                  <span [class]="'badge ' + (item.isActive ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary')">
                    {{ item.isActive ? '\u555F\u7528' : '\u505C\u7528' }}
                  </span>
                </td>
                <td class="text-muted small d-none d-lg-table-cell">{{ item.createdAt | date:'yyyy-MM-dd' }}</td>
                <td class="text-end" style="white-space: nowrap">
                  <div class="d-flex justify-content-end gap-1">
                    <a [routerLink]="[item.id, 'flow']"
                       class="btn btn-sm btn-ghost-info d-inline-flex align-items-center waves-effect"
                       title="\u7BA1\u7406\u6D41\u7A0B">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#git-merge"></use></svg>
                    </a>
                    <button class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect"
                            (click)="openEdit(item)" title="\u7DE8\u8F2F">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>
                    </button>
                    <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect"
                            (click)="delete(item)" title="\u522A\u9664">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>
                    </button>
                  </div>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="8" class="text-center text-muted py-4">\u5C1A\u7121\u7C3D\u6838\u9805\u76EE\u3002</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  </div>
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ApprovalList, { className: "ApprovalList", filePath: "src/app/features/admin/approvals/pages/approval-list/approval-list.ts", lineNumber: 16 });
})();

// src/app/features/admin/approvals/pages/approval-flow/approval-flow.ts
var _forTrack010 = ($index, $item) => $item.id;
function ApprovalFlow_Conditional_1_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "p", 10);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const item_r2 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(item_r2.description);
  }
}
function ApprovalFlow_Conditional_1_Conditional_12_For_19_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 29);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const dept_r5 = ctx.$implicit;
    \u0275\u0275property("ngValue", dept_r5.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(dept_r5.name);
  }
}
function ApprovalFlow_Conditional_1_Conditional_12_For_27_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 29);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const jt_r6 = ctx.$implicit;
    \u0275\u0275property("ngValue", jt_r6.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate2("", jt_r6.name, "\uFF08L", jt_r6.level, "\uFF09");
  }
}
function ApprovalFlow_Conditional_1_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    const _r3 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 11)(1, "div", 19);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 20)(4, "form", 21);
    \u0275\u0275listener("ngSubmit", function ApprovalFlow_Conditional_1_Conditional_12_Template_form_ngSubmit_4_listener() {
      \u0275\u0275restoreView(_r3);
      const ctx_r3 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r3.submitStep());
    });
    \u0275\u0275elementStart(5, "div", 22)(6, "div", 23)(7, "label", 24);
    \u0275\u0275text(8, "\u6B65\u9A5F\u9806\u5E8F ");
    \u0275\u0275elementStart(9, "span", 25);
    \u0275\u0275text(10, "*");
    \u0275\u0275elementEnd()();
    \u0275\u0275element(11, "input", 26);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "div", 27)(13, "label", 24);
    \u0275\u0275text(14, "\u90E8\u9580");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "select", 28)(16, "option", 29);
    \u0275\u0275text(17, "\u2014 \u4E0D\u9650\u90E8\u9580 \u2014");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(18, ApprovalFlow_Conditional_1_Conditional_12_For_19_Template, 2, 2, "option", 29, _forTrack010);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(20, "div", 27)(21, "label", 24);
    \u0275\u0275text(22, "\u8077\u7A31");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(23, "select", 30)(24, "option", 29);
    \u0275\u0275text(25, "\u2014 \u4E0D\u9650\u8077\u7A31 \u2014");
    \u0275\u0275elementEnd();
    \u0275\u0275repeaterCreate(26, ApprovalFlow_Conditional_1_Conditional_12_For_27_Template, 2, 3, "option", 29, _forTrack010);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(28, "div", 31)(29, "label", 24);
    \u0275\u0275text(30, "\u6B65\u9A5F\u8AAA\u660E");
    \u0275\u0275elementEnd();
    \u0275\u0275element(31, "input", 32);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(32, "div", 33);
    \u0275\u0275text(33, "\u90E8\u9580\u6216\u8077\u7A31\u81F3\u5C11\u9078\u4E00\u3002");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(34, "div", 34)(35, "button", 35);
    \u0275\u0275text(36);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(37, "button", 36);
    \u0275\u0275listener("click", function ApprovalFlow_Conditional_1_Conditional_12_Template_button_click_37_listener() {
      \u0275\u0275restoreView(_r3);
      const ctx_r3 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r3.closeStepForm());
    });
    \u0275\u0275text(38, "\u53D6\u6D88");
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const ctx_r3 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", ctx_r3.editStep ? "\u7DE8\u8F2F\u6B65\u9A5F" : "\u65B0\u589E\u6B65\u9A5F", " ");
    \u0275\u0275advance(2);
    \u0275\u0275property("formGroup", ctx_r3.stepForm);
    \u0275\u0275advance(12);
    \u0275\u0275property("ngValue", null);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(ctx_r3.departments);
    \u0275\u0275advance(6);
    \u0275\u0275property("ngValue", null);
    \u0275\u0275advance(2);
    \u0275\u0275repeater(ctx_r3.jobTitles);
    \u0275\u0275advance(10);
    \u0275\u0275textInterpolate1(" ", ctx_r3.editStep ? "\u66F4\u65B0" : "\u65B0\u589E", " ");
  }
}
function ApprovalFlow_Conditional_1_Conditional_20_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 17)(1, "div", 37);
    \u0275\u0275text(2, "\u5C1A\u672A\u8A2D\u5B9A\u4EFB\u4F55\u6B65\u9A5F");
    \u0275\u0275elementEnd()();
  }
}
function ApprovalFlow_Conditional_1_Conditional_21_For_2_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "div", 41);
  }
}
function ApprovalFlow_Conditional_1_Conditional_21_For_2_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 46);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 54);
    \u0275\u0275element(2, "use", 55);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const step_r8 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" ", step_r8.departmentName, " ");
  }
}
function ApprovalFlow_Conditional_1_Conditional_21_For_2_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 47);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 54);
    \u0275\u0275element(2, "use", 56);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const step_r8 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" ", step_r8.jobTitleName, " ");
  }
}
function ApprovalFlow_Conditional_1_Conditional_21_For_2_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 48);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const step_r8 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(step_r8.note);
  }
}
function ApprovalFlow_Conditional_1_Conditional_21_For_2_Template(rf, ctx) {
  if (rf & 1) {
    const _r7 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 38)(1, "div", 39)(2, "div", 40);
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(4, ApprovalFlow_Conditional_1_Conditional_21_For_2_Conditional_4_Template, 1, 0, "div", 41);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "div", 42)(6, "div", 43)(7, "div", 44)(8, "div", 45);
    \u0275\u0275conditionalCreate(9, ApprovalFlow_Conditional_1_Conditional_21_For_2_Conditional_9_Template, 4, 1, "span", 46);
    \u0275\u0275conditionalCreate(10, ApprovalFlow_Conditional_1_Conditional_21_For_2_Conditional_10_Template, 4, 1, "span", 47);
    \u0275\u0275conditionalCreate(11, ApprovalFlow_Conditional_1_Conditional_21_For_2_Conditional_11_Template, 2, 1, "span", 48);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "div", 49)(13, "button", 50);
    \u0275\u0275listener("click", function ApprovalFlow_Conditional_1_Conditional_21_For_2_Template_button_click_13_listener() {
      const step_r8 = \u0275\u0275restoreView(_r7).$implicit;
      const ctx_r3 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r3.openEditStep(step_r8));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(14, "svg", 15);
    \u0275\u0275element(15, "use", 51);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(16, "button", 52);
    \u0275\u0275listener("click", function ApprovalFlow_Conditional_1_Conditional_21_For_2_Template_button_click_16_listener() {
      const step_r8 = \u0275\u0275restoreView(_r7).$implicit;
      const ctx_r3 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r3.deleteStep(step_r8));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(17, "svg", 15);
    \u0275\u0275element(18, "use", 53);
    \u0275\u0275elementEnd()()()()()()();
  }
  if (rf & 2) {
    const step_r8 = ctx.$implicit;
    const \u0275$index_118_r9 = ctx.$index;
    const \u0275$count_118_r10 = ctx.$count;
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" ", step_r8.stepOrder, " ");
    \u0275\u0275advance();
    \u0275\u0275conditional(!(\u0275$index_118_r9 === \u0275$count_118_r10 - 1) ? 4 : -1);
    \u0275\u0275advance(5);
    \u0275\u0275conditional(step_r8.departmentName ? 9 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(step_r8.jobTitleName ? 10 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(step_r8.note ? 11 : -1);
  }
}
function ApprovalFlow_Conditional_1_Conditional_21_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 18);
    \u0275\u0275repeaterCreate(1, ApprovalFlow_Conditional_1_Conditional_21_For_2_Template, 19, 5, "div", 38, _forTrack010);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const item_r2 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275repeater(item_r2.steps);
  }
}
function ApprovalFlow_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 2)(1, "div", 3)(2, "a", 4);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(3, "svg", 5);
    \u0275\u0275element(4, "use", 6);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(5, "svg", 7);
    \u0275\u0275element(6, "use", 8);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(7, "h4", 9);
    \u0275\u0275text(8);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "span");
    \u0275\u0275text(10);
    \u0275\u0275elementEnd()()();
    \u0275\u0275conditionalCreate(11, ApprovalFlow_Conditional_1_Conditional_11_Template, 2, 1, "p", 10);
    \u0275\u0275conditionalCreate(12, ApprovalFlow_Conditional_1_Conditional_12_Template, 39, 5, "div", 11);
    \u0275\u0275elementStart(13, "div", 12)(14, "h6", 13);
    \u0275\u0275text(15);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(16, "button", 14);
    \u0275\u0275listener("click", function ApprovalFlow_Conditional_1_Template_button_click_16_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r3 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r3.openAddStep());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(17, "svg", 15);
    \u0275\u0275element(18, "use", 16);
    \u0275\u0275elementEnd();
    \u0275\u0275text(19, " \u65B0\u589E\u6B65\u9A5F ");
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(20, ApprovalFlow_Conditional_1_Conditional_20_Template, 3, 0, "div", 17)(21, ApprovalFlow_Conditional_1_Conditional_21_Template, 3, 0, "div", 18);
  }
  if (rf & 2) {
    const item_r2 = ctx;
    const ctx_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(8);
    \u0275\u0275textInterpolate1("", item_r2.name, "\uFF5C\u7C3D\u6838\u6D41\u7A0B");
    \u0275\u0275advance();
    \u0275\u0275classMap("badge " + (item_r2.isActive ? "bg-success-subtle text-success" : "bg-secondary-subtle text-secondary"));
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", item_r2.isActive ? "\u555F\u7528" : "\u505C\u7528", " ");
    \u0275\u0275advance();
    \u0275\u0275conditional(item_r2.description ? 11 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r3.showStepForm ? 12 : -1);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1("\u7C3D\u6838\u6B65\u9A5F\uFF08\u5171 ", item_r2.steps.length, " \u6B65\uFF09");
    \u0275\u0275advance(5);
    \u0275\u0275conditional(item_r2.steps.length === 0 ? 20 : 21);
  }
}
function ApprovalFlow_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "p", 1);
    \u0275\u0275text(1, "\u627E\u4E0D\u5230\u7C3D\u6838\u9805\u76EE\u3002");
    \u0275\u0275elementEnd();
  }
}
var ApprovalFlow = class _ApprovalFlow {
  route = inject(ActivatedRoute);
  approvalService = inject(ApprovalService);
  deptService = inject(DepartmentService);
  jobTitleService = inject(JobTitleService);
  fb = inject(FormBuilder);
  itemId = 0;
  item$ = new BehaviorSubject(void 0);
  departments = [];
  jobTitles = [];
  showStepForm = false;
  editStep = null;
  stepForm = this.fb.group({
    stepOrder: [1, [Validators.required, Validators.min(1)]],
    departmentId: [null],
    jobTitleId: [null],
    note: [""]
  });
  ngOnInit() {
    this.itemId = +(this.route.snapshot.paramMap.get("id") ?? 0);
    this.approvalService.getById(this.itemId).subscribe((item) => this.item$.next(item));
    this.deptService.getAll().pipe(take(1)).subscribe((d) => this.departments = d);
    this.jobTitleService.getAll().pipe(take(1)).subscribe((j) => this.jobTitles = j);
  }
  openAddStep() {
    this.editStep = null;
    const nextOrder = (this.item$.getValue()?.steps.length ?? 0) + 1;
    this.stepForm.reset({ stepOrder: nextOrder, departmentId: null, jobTitleId: null, note: "" });
    this.showStepForm = true;
  }
  openEditStep(step) {
    this.editStep = step;
    this.stepForm.patchValue({
      stepOrder: step.stepOrder,
      departmentId: step.departmentId ?? null,
      jobTitleId: step.jobTitleId ?? null,
      note: step.note ?? ""
    });
    this.showStepForm = true;
  }
  closeStepForm() {
    this.showStepForm = false;
  }
  submitStep() {
    if (this.stepForm.invalid)
      return;
    const v = this.stepForm.value;
    const deptId = v.departmentId || void 0;
    const jtId = v.jobTitleId || void 0;
    if (!deptId && !jtId) {
      alert("\u90E8\u9580\u6216\u8077\u7A31\u81F3\u5C11\u9078\u4E00\u3002");
      return;
    }
    const deptName = deptId ? this.departments.find((d) => d.id === deptId)?.name : void 0;
    const jtName = jtId ? this.jobTitles.find((j) => j.id === jtId)?.name : void 0;
    const stepData = {
      stepOrder: v.stepOrder,
      departmentId: deptId,
      departmentName: deptName,
      jobTitleId: jtId,
      jobTitleName: jtName,
      note: v.note ?? ""
    };
    const obs = this.editStep ? this.approvalService.updateStep(this.itemId, this.editStep.id, stepData) : this.approvalService.addStep(this.itemId, stepData);
    obs.subscribe((item) => {
      this.item$.next(item);
      this.showStepForm = false;
    });
  }
  deleteStep(step) {
    if (confirm(`\u78BA\u5B9A\u8981\u522A\u9664\u6B65\u9A5F ${step.stepOrder} \u55CE\uFF1F`)) {
      this.approvalService.deleteStep(this.itemId, step.id).subscribe((item) => this.item$.next(item));
    }
  }
  static \u0275fac = function ApprovalFlow_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ApprovalFlow)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ApprovalFlow, selectors: [["app-approval-flow"]], decls: 4, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "text-muted"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], ["routerLink", "/admin/approvals", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#git-merge"], [1, "mb-0"], [1, "text-muted", "small", "mb-4"], [1, "card", "border-0", "shadow-sm", "mb-4"], [1, "d-flex", "align-items-center", "justify-content-between", "mb-3"], [1, "mb-0", "text-muted"], [1, "btn", "btn-outline-primary", "btn-sm", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", 3, "click"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm"], [1, "d-flex", "flex-column", "gap-2"], [1, "card-header", "bg-transparent", "fw-500"], [1, "card-body"], [3, "ngSubmit", "formGroup"], [1, "row", "g-3"], [1, "col-6", "col-md-2"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "number", "formControlName", "stepOrder", "min", "1", 1, "form-control"], [1, "col-12", "col-md-4"], ["formControlName", "departmentId", 1, "form-select"], [3, "ngValue"], ["formControlName", "jobTitleId", 1, "form-select"], [1, "col-12", "col-md-6"], ["type", "text", "formControlName", "note", "placeholder", "\u9078\u586B\u8AAA\u660E", 1, "form-control"], [1, "text-muted", "small", "mt-2"], [1, "d-flex", "gap-2", "mt-3"], ["type", "submit", 1, "btn", "btn-primary", "btn-sm", "waves-effect", "waves-light"], ["type", "button", 1, "btn", "btn-outline-secondary", "btn-sm", "waves-effect", 3, "click"], [1, "card-body", "text-center", "text-muted", "py-5"], [1, "d-flex", "gap-2", "align-items-start"], [1, "d-flex", "flex-column", "align-items-center", 2, "min-width", "36px"], [1, "rounded-circle", "bg-primary", "text-white", "d-flex", "align-items-center", "justify-content-center", "fw-bold", 2, "width", "32px", "height", "32px", "font-size", ".85rem"], [1, "bg-secondary-subtle", 2, "width", "2px", "flex", "1", "min-height", "20px"], [1, "card", "border-0", "shadow-sm", "flex-fill", "mb-0"], [1, "card-body", "py-2", "px-3"], [1, "d-flex", "align-items-center", "justify-content-between", "flex-wrap", "gap-2"], [1, "d-flex", "flex-wrap", "gap-2", "align-items-center"], [1, "badge", "bg-primary-subtle", "text-primary", "d-inline-flex", "align-items-center", "gap-1", 2, "white-space", "nowrap"], [1, "badge", "bg-warning-subtle", "text-warning-emphasis", "d-inline-flex", "align-items-center", "gap-1", 2, "white-space", "nowrap"], [1, "text-muted", "small"], [1, "d-flex", "gap-1"], ["title", "\u7DE8\u8F2F", 1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#edit"], ["title", "\u522A\u9664", 1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], [1, "sa-icon", 2, "width", "12px", "height", "12px", "flex-shrink", "0", "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#git-branch"], ["href", "/assets/icons/sprite.svg#briefcase"]], template: function ApprovalFlow_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0);
      \u0275\u0275conditionalCreate(1, ApprovalFlow_Conditional_1_Template, 22, 8);
      \u0275\u0275pipe(2, "async");
      \u0275\u0275conditionalBranchCreate(3, ApprovalFlow_Conditional_3_Template, 2, 0, "p", 1);
      \u0275\u0275elementEnd();
    }
    if (rf & 2) {
      let tmp_0_0;
      \u0275\u0275advance();
      \u0275\u0275conditional((tmp_0_0 = \u0275\u0275pipeBind1(2, 1, ctx.item$)) ? 1 : 3, tmp_0_0);
    }
  }, dependencies: [RouterLink, ReactiveFormsModule, \u0275NgNoValidate, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, NumberValueAccessor, SelectControlValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, FormGroupDirective, FormControlName, AsyncPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ApprovalFlow, [{
    type: Component,
    args: [{ selector: "app-approval-flow", imports: [RouterLink, AsyncPipe, ReactiveFormsModule], template: `<div class="container-fluid py-3">
  @if (item$ | async; as item) {
    <!-- Page Header -->
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">
      <div class="d-flex align-items-center gap-2">
        <a routerLink="/admin/approvals" class="btn btn-sm btn-outline-secondary waves-effect">
          <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
        </a>
        <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">
          <use href="/assets/icons/sprite.svg#git-merge"></use>
        </svg>
        <h4 class="mb-0">{{ item.name }}\uFF5C\u7C3D\u6838\u6D41\u7A0B</h4>
        <span [class]="'badge ' + (item.isActive ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary')">
          {{ item.isActive ? '\u555F\u7528' : '\u505C\u7528' }}
        </span>
      </div>
    </div>
    @if (item.description) {
      <p class="text-muted small mb-4">{{ item.description }}</p>
    }

    <!-- Step Form -->
    @if (showStepForm) {
      <div class="card border-0 shadow-sm mb-4">
        <div class="card-header bg-transparent fw-500">
          {{ editStep ? '\u7DE8\u8F2F\u6B65\u9A5F' : '\u65B0\u589E\u6B65\u9A5F' }}
        </div>
        <div class="card-body">
          <form [formGroup]="stepForm" (ngSubmit)="submitStep()">
            <div class="row g-3">
              <div class="col-6 col-md-2">
                <label class="form-label fw-500">\u6B65\u9A5F\u9806\u5E8F <span class="text-danger">*</span></label>
                <input type="number" class="form-control" formControlName="stepOrder" min="1">
              </div>
              <div class="col-12 col-md-4">
                <label class="form-label fw-500">\u90E8\u9580</label>
                <select class="form-select" formControlName="departmentId">
                  <option [ngValue]="null">\u2014 \u4E0D\u9650\u90E8\u9580 \u2014</option>
                  @for (dept of departments; track dept.id) {
                    <option [ngValue]="dept.id">{{ dept.name }}</option>
                  }
                </select>
              </div>
              <div class="col-12 col-md-4">
                <label class="form-label fw-500">\u8077\u7A31</label>
                <select class="form-select" formControlName="jobTitleId">
                  <option [ngValue]="null">\u2014 \u4E0D\u9650\u8077\u7A31 \u2014</option>
                  @for (jt of jobTitles; track jt.id) {
                    <option [ngValue]="jt.id">{{ jt.name }}\uFF08L{{ jt.level }}\uFF09</option>
                  }
                </select>
              </div>
              <div class="col-12 col-md-6">
                <label class="form-label fw-500">\u6B65\u9A5F\u8AAA\u660E</label>
                <input type="text" class="form-control" formControlName="note" placeholder="\u9078\u586B\u8AAA\u660E">
              </div>
            </div>
            <div class="text-muted small mt-2">\u90E8\u9580\u6216\u8077\u7A31\u81F3\u5C11\u9078\u4E00\u3002</div>
            <div class="d-flex gap-2 mt-3">
              <button type="submit" class="btn btn-primary btn-sm waves-effect waves-light">
                {{ editStep ? '\u66F4\u65B0' : '\u65B0\u589E' }}
              </button>
              <button type="button" class="btn btn-outline-secondary btn-sm waves-effect"
                      (click)="closeStepForm()">\u53D6\u6D88</button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- Steps Timeline -->
    <div class="d-flex align-items-center justify-content-between mb-3">
      <h6 class="mb-0 text-muted">\u7C3D\u6838\u6B65\u9A5F\uFF08\u5171 {{ item.steps.length }} \u6B65\uFF09</h6>
      <button class="btn btn-outline-primary btn-sm d-inline-flex align-items-center gap-1 waves-effect" (click)="openAddStep()">
        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
        \u65B0\u589E\u6B65\u9A5F
      </button>
    </div>

    @if (item.steps.length === 0) {
      <div class="card border-0 shadow-sm">
        <div class="card-body text-center text-muted py-5">\u5C1A\u672A\u8A2D\u5B9A\u4EFB\u4F55\u6B65\u9A5F</div>
      </div>
    } @else {
      <div class="d-flex flex-column gap-2">
        @for (step of item.steps; track step.id; let last = $last) {
          <div class="d-flex gap-2 align-items-start">
            <!-- Step number bubble -->
            <div class="d-flex flex-column align-items-center" style="min-width:36px">
              <div class="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center fw-bold"
                   style="width:32px;height:32px;font-size:.85rem">
                {{ step.stepOrder }}
              </div>
              @if (!last) {
                <div class="bg-secondary-subtle" style="width:2px;flex:1;min-height:20px"></div>
              }
            </div>
            <!-- Step card -->
            <div class="card border-0 shadow-sm flex-fill mb-0">
              <div class="card-body py-2 px-3">
                <div class="d-flex align-items-center justify-content-between flex-wrap gap-2">
                  <div class="d-flex flex-wrap gap-2 align-items-center">
                    @if (step.departmentName) {
                      <span class="badge bg-primary-subtle text-primary d-inline-flex align-items-center gap-1" style="white-space:nowrap">
                        <svg class="sa-icon" style="width:12px;height:12px;flex-shrink:0;stroke:currentColor"><use href="/assets/icons/sprite.svg#git-branch"></use></svg>
                        {{ step.departmentName }}
                      </span>
                    }
                    @if (step.jobTitleName) {
                      <span class="badge bg-warning-subtle text-warning-emphasis d-inline-flex align-items-center gap-1" style="white-space:nowrap">
                        <svg class="sa-icon" style="width:12px;height:12px;flex-shrink:0;stroke:currentColor"><use href="/assets/icons/sprite.svg#briefcase"></use></svg>
                        {{ step.jobTitleName }}
                      </span>
                    }
                    @if (step.note) {
                      <span class="text-muted small">{{ step.note }}</span>
                    }
                  </div>
                  <div class="d-flex gap-1">
                    <button class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect"
                            (click)="openEditStep(step)" title="\u7DE8\u8F2F">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>
                    </button>
                    <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect"
                            (click)="deleteStep(step)" title="\u522A\u9664">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        }
      </div>
    }
  } @else {
    <p class="text-muted">\u627E\u4E0D\u5230\u7C3D\u6838\u9805\u76EE\u3002</p>
  }
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ApprovalFlow, { className: "ApprovalFlow", filePath: "src/app/features/admin/approvals/pages/approval-flow/approval-flow.ts", lineNumber: 18 });
})();

// src/app/features/admin/projects/services/project.service.ts
var MOCK_PROJECTS = [
  {
    id: 1,
    code: "P2024-001",
    departmentId: 3,
    departmentName: "\u696D\u52D9\u90E8",
    budgetAmount: 5e5,
    actualAmount: 48e4,
    businessAmount: 45e4,
    googleDriveUrl: "https://drive.google.com/drive/folders/example1",
    createdAt: /* @__PURE__ */ new Date("2024-03-01")
  },
  {
    id: 2,
    code: "P2024-002",
    departmentId: 2,
    departmentName: "\u8CC7\u8A0A\u90E8",
    budgetAmount: 12e5,
    actualAmount: 0,
    businessAmount: 11e5,
    googleDriveUrl: "https://drive.google.com/drive/folders/example2",
    createdAt: /* @__PURE__ */ new Date("2024-06-15")
  },
  {
    id: 3,
    code: "P2025-001",
    departmentId: 1,
    departmentName: "\u7BA1\u7406\u90E8",
    budgetAmount: 3e5,
    actualAmount: 28e4,
    businessAmount: 25e4,
    googleDriveUrl: "https://drive.google.com/drive/folders/example3",
    createdAt: /* @__PURE__ */ new Date("2025-01-10")
  }
];
var ProjectService = class _ProjectService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_PROJECTS);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id) {
    return of(this.items$.getValue().find((p) => p.id === id));
  }
  create(data) {
    const item = __spreadProps(__spreadValues({}, data), { id: Date.now(), createdAt: /* @__PURE__ */ new Date() });
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }
  update(id, changes) {
    const updated = this.items$.getValue().map((p) => p.id === id ? __spreadValues(__spreadValues({}, p), changes) : p);
    this.items$.next(updated);
    return of(updated.find((p) => p.id === id));
  }
  delete(id) {
    this.items$.next(this.items$.getValue().filter((p) => p.id !== id));
    return of(void 0);
  }
  static \u0275fac = function ProjectService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ProjectService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _ProjectService, factory: _ProjectService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ProjectService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/projects/pages/project-list/project-list.ts
var _c07 = (a0) => [a0, "edit"];
var _forTrack011 = ($index, $item) => $item.id;
function ProjectList_For_33_Conditional_4_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 17);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const project_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(project_r2.departmentName);
  }
}
function ProjectList_For_33_Conditional_5_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 18);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function ProjectList_For_33_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0);
    \u0275\u0275pipe(1, "number");
  }
  if (rf & 2) {
    const project_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind2(1, 1, project_r2.budgetAmount, "1.0-0"), " ");
  }
}
function ProjectList_For_33_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 18);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function ProjectList_For_33_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0);
    \u0275\u0275pipe(1, "number");
  }
  if (rf & 2) {
    const project_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind2(1, 1, project_r2.actualAmount, "1.0-0"), " ");
  }
}
function ProjectList_For_33_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 18);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function ProjectList_For_33_Conditional_13_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0);
    \u0275\u0275pipe(1, "number");
  }
  if (rf & 2) {
    const project_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind2(1, 1, project_r2.businessAmount, "1.0-0"), " ");
  }
}
function ProjectList_For_33_Conditional_14_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 18);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function ProjectList_For_33_Conditional_16_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "a", 19);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 26);
    \u0275\u0275element(2, "use", 27);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u958B\u555F ");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const project_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("href", project_r2.googleDriveUrl, \u0275\u0275sanitizeUrl);
  }
}
function ProjectList_For_33_Conditional_17_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 18);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function ProjectList_For_33_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td", 16);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td");
    \u0275\u0275conditionalCreate(4, ProjectList_For_33_Conditional_4_Template, 2, 1, "span", 17)(5, ProjectList_For_33_Conditional_5_Template, 2, 0, "span", 18);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "td", 14);
    \u0275\u0275conditionalCreate(7, ProjectList_For_33_Conditional_7_Template, 2, 4)(8, ProjectList_For_33_Conditional_8_Template, 2, 0, "span", 18);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 14);
    \u0275\u0275conditionalCreate(10, ProjectList_For_33_Conditional_10_Template, 2, 4)(11, ProjectList_For_33_Conditional_11_Template, 2, 0, "span", 18);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "td", 14);
    \u0275\u0275conditionalCreate(13, ProjectList_For_33_Conditional_13_Template, 2, 4)(14, ProjectList_For_33_Conditional_14_Template, 2, 0, "span", 18);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "td", 15);
    \u0275\u0275conditionalCreate(16, ProjectList_For_33_Conditional_16_Template, 4, 1, "a", 19)(17, ProjectList_For_33_Conditional_17_Template, 2, 0, "span", 18);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(18, "td", 20)(19, "div", 21)(20, "a", 22);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(21, "svg", 7);
    \u0275\u0275element(22, "use", 23);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(23, "button", 24);
    \u0275\u0275listener("click", function ProjectList_For_33_Template_button_click_23_listener() {
      const project_r2 = \u0275\u0275restoreView(_r1).$implicit;
      const ctx_r2 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r2.delete(project_r2));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(24, "svg", 7);
    \u0275\u0275element(25, "use", 25);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const project_r2 = ctx.$implicit;
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(project_r2.code);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(project_r2.departmentName ? 4 : 5);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(project_r2.budgetAmount != null ? 7 : 8);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(project_r2.actualAmount != null ? 10 : 11);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(project_r2.businessAmount != null ? 13 : 14);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(project_r2.googleDriveUrl ? 16 : 17);
    \u0275\u0275advance(4);
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(7, _c07, project_r2.id));
  }
}
function ProjectList_ForEmpty_34_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 28);
    \u0275\u0275text(2, "\u5C1A\u7121\u5C08\u6848\u8CC7\u6599\u3002");
    \u0275\u0275elementEnd()();
  }
}
var ProjectList = class _ProjectList {
  projectService = inject(ProjectService);
  projects$ = this.projectService.getAll();
  delete(project) {
    if (confirm(`\u78BA\u5B9A\u8981\u522A\u9664\u5C08\u6848\u300C${project.code}\u300D\u55CE\uFF1F`)) {
      this.projectService.delete(project.id).subscribe();
    }
  }
  static \u0275fac = function ProjectList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ProjectList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ProjectList, selectors: [["app-project-list"]], decls: 36, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#folder"], [1, "mb-0"], ["routerLink", "new", 1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "text-end"], [1, "d-none", "d-lg-table-cell"], [1, "fw-500", "font-monospace"], [1, "badge", "bg-primary-subtle", "text-primary"], [1, "text-muted"], ["target", "_blank", "rel", "noopener", 1, "d-inline-flex", "align-items-center", "gap-1", "text-decoration-none", "text-muted", "small", 3, "href"], [1, "text-end", 2, "white-space", "nowrap"], [1, "d-flex", "justify-content-end", "gap-1"], [1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], [1, "sa-icon", 2, "stroke", "currentColor", "width", "14px", "height", "14px"], ["href", "/assets/icons/sprite.svg#external-link"], ["colspan", "7", 1, "text-center", "text-muted", "py-4"]], template: function ProjectList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u5C08\u6848\u7BA1\u7406");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "a", 6);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(8, "svg", 7);
      \u0275\u0275element(9, "use", 8);
      \u0275\u0275elementEnd();
      \u0275\u0275text(10, " \u65B0\u589E\u5C08\u6848 ");
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(11, "div", 9)(12, "div", 10)(13, "div", 11)(14, "table", 12)(15, "thead", 13)(16, "tr")(17, "th");
      \u0275\u0275text(18, "\u7DE8\u865F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "th");
      \u0275\u0275text(20, "\u90E8\u9580");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(21, "th", 14);
      \u0275\u0275text(22, "\u9810\u5B9A\u91D1\u984D");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(23, "th", 14);
      \u0275\u0275text(24, "\u5BE6\u969B\u91D1\u984D");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(25, "th", 14);
      \u0275\u0275text(26, "\u696D\u52D9\u91D1\u984D");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(27, "th", 15);
      \u0275\u0275text(28, "Google Drive");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(29, "th", 14);
      \u0275\u0275text(30, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(31, "tbody");
      \u0275\u0275repeaterCreate(32, ProjectList_For_33_Template, 26, 9, "tr", null, _forTrack011, false, ProjectList_ForEmpty_34_Template, 3, 0, "tr");
      \u0275\u0275pipe(35, "async");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(32);
      \u0275\u0275repeater(\u0275\u0275pipeBind1(35, 1, ctx.projects$));
    }
  }, dependencies: [RouterLink, AsyncPipe, DecimalPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ProjectList, [{
    type: Component,
    args: [{ selector: "app-project-list", imports: [RouterLink, AsyncPipe, DecimalPipe], template: `<div class="container-fluid py-3">
  <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">
    <div class="d-flex align-items-center gap-2">
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">
        <use href="/assets/icons/sprite.svg#folder"></use>
      </svg>
      <h4 class="mb-0">\u5C08\u6848\u7BA1\u7406</h4>
    </div>
    <a routerLink="new" class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light">
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
      \u65B0\u589E\u5C08\u6848
    </a>
  </div>

  <div class="card border-0 shadow-sm">
    <div class="card-body p-0">
      <div class="table-responsive">
        <table class="table table-hover mb-0">
          <thead class="table-light">
            <tr>
              <th>\u7DE8\u865F</th>
              <th>\u90E8\u9580</th>
              <th class="text-end">\u9810\u5B9A\u91D1\u984D</th>
              <th class="text-end">\u5BE6\u969B\u91D1\u984D</th>
              <th class="text-end">\u696D\u52D9\u91D1\u984D</th>
              <th class="d-none d-lg-table-cell">Google Drive</th>
              <th class="text-end">\u64CD\u4F5C</th>
            </tr>
          </thead>
          <tbody>
            @for (project of projects$ | async; track project.id) {
              <tr>
                <td class="fw-500 font-monospace">{{ project.code }}</td>
                <td>
                  @if (project.departmentName) {
                    <span class="badge bg-primary-subtle text-primary">{{ project.departmentName }}</span>
                  } @else {
                    <span class="text-muted">\u2014</span>
                  }
                </td>
                <td class="text-end">
                  @if (project.budgetAmount != null) {
                    {{ project.budgetAmount | number:'1.0-0' }}
                  } @else {
                    <span class="text-muted">\u2014</span>
                  }
                </td>
                <td class="text-end">
                  @if (project.actualAmount != null) {
                    {{ project.actualAmount | number:'1.0-0' }}
                  } @else {
                    <span class="text-muted">\u2014</span>
                  }
                </td>
                <td class="text-end">
                  @if (project.businessAmount != null) {
                    {{ project.businessAmount | number:'1.0-0' }}
                  } @else {
                    <span class="text-muted">\u2014</span>
                  }
                </td>
                <td class="d-none d-lg-table-cell">
                  @if (project.googleDriveUrl) {
                    <a [href]="project.googleDriveUrl" target="_blank" rel="noopener"
                       class="d-inline-flex align-items-center gap-1 text-decoration-none text-muted small">
                      <svg class="sa-icon" style="stroke:currentColor;width:14px;height:14px"><use href="/assets/icons/sprite.svg#external-link"></use></svg>
                      \u958B\u555F
                    </a>
                  } @else {
                    <span class="text-muted">\u2014</span>
                  }
                </td>
                <td class="text-end" style="white-space: nowrap">
                  <div class="d-flex justify-content-end gap-1">
                    <a [routerLink]="[project.id, 'edit']" class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>
                    </a>
                    <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect" (click)="delete(project)">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>
                    </button>
                  </div>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="7" class="text-center text-muted py-4">\u5C1A\u7121\u5C08\u6848\u8CC7\u6599\u3002</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  </div>
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ProjectList, { className: "ProjectList", filePath: "src/app/features/admin/projects/pages/project-list/project-list.ts", lineNumber: 12 });
})();

// src/app/features/admin/projects/pages/project-form/project-form.ts
var _forTrack012 = ($index, $item) => $item.id;
function ProjectForm_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u5C08\u6848\u7DE8\u865F\u3002");
    \u0275\u0275elementEnd();
  }
}
function ProjectForm_For_28_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 17);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const dept_r1 = ctx.$implicit;
    \u0275\u0275property("ngValue", dept_r1.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(dept_r1.name);
  }
}
function ProjectForm_Conditional_29_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u90E8\u9580\u3002");
    \u0275\u0275elementEnd();
  }
}
function ProjectForm_Conditional_36_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u9810\u5B9A\u91D1\u984D\uFF08\u4E0D\u53EF\u70BA\u8CA0\u6578\uFF09\u3002");
    \u0275\u0275elementEnd();
  }
}
function ProjectForm_Conditional_43_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u5BE6\u969B\u91D1\u984D\uFF08\u4E0D\u53EF\u70BA\u8CA0\u6578\uFF09\u3002");
    \u0275\u0275elementEnd();
  }
}
function ProjectForm_Conditional_50_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u696D\u52D9\u91D1\u984D\uFF08\u4E0D\u53EF\u70BA\u8CA0\u6578\uFF09\u3002");
    \u0275\u0275elementEnd();
  }
}
function ProjectForm_Conditional_57_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 15);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165 Google Drive \u9023\u7D50\u3002");
    \u0275\u0275elementEnd();
  }
}
var ProjectForm = class _ProjectForm {
  fb = inject(FormBuilder);
  projectService = inject(ProjectService);
  deptService = inject(DepartmentService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  departments = [];
  isEdit = false;
  projectId = 0;
  form = this.fb.group({
    code: ["", Validators.required],
    departmentId: [null, Validators.required],
    budgetAmount: [null, [Validators.required, Validators.min(0)]],
    actualAmount: [null, [Validators.required, Validators.min(0)]],
    businessAmount: [null, [Validators.required, Validators.min(0)]],
    googleDriveUrl: ["", Validators.required]
  });
  ngOnInit() {
    this.deptService.getAll().subscribe((d) => this.departments = d);
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEdit = true;
      this.projectId = +id;
      this.projectService.getById(this.projectId).subscribe((p) => {
        if (p)
          this.form.patchValue({
            code: p.code,
            departmentId: p.departmentId ?? null,
            budgetAmount: p.budgetAmount ?? null,
            actualAmount: p.actualAmount ?? null,
            businessAmount: p.businessAmount ?? null,
            googleDriveUrl: p.googleDriveUrl ?? ""
          });
      });
    }
  }
  submit() {
    if (this.form.invalid)
      return;
    const v = this.form.value;
    const dept = this.departments.find((d) => d.id === v.departmentId);
    const payload = {
      code: v.code,
      departmentId: v.departmentId ?? void 0,
      departmentName: dept?.name,
      budgetAmount: v.budgetAmount ?? void 0,
      actualAmount: v.actualAmount ?? void 0,
      businessAmount: v.businessAmount ?? void 0,
      googleDriveUrl: v.googleDriveUrl || void 0
    };
    const obs = this.isEdit ? this.projectService.update(this.projectId, payload) : this.projectService.create(payload);
    obs.subscribe(() => this.router.navigate(["/admin/projects"]));
  }
  static \u0275fac = function ProjectForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ProjectForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ProjectForm, selectors: [["app-project-form"]], decls: 63, vars: 11, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], ["routerLink", "/admin/projects", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "row"], [1, "col-12", "col-lg-8", "col-xl-6"], [1, "card", "border-0", "shadow-sm"], [1, "card-body"], [3, "ngSubmit", "formGroup"], [1, "mb-3"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "code", "placeholder", "\u4F8B\u5982\uFF1AP2025-001", 1, "form-control", "font-monospace"], [1, "text-danger", "small", "mt-1"], ["formControlName", "departmentId", 1, "form-select"], [3, "ngValue"], ["type", "number", "formControlName", "budgetAmount", "min", "0", 1, "form-control"], ["type", "number", "formControlName", "actualAmount", "min", "0", 1, "form-control"], ["type", "number", "formControlName", "businessAmount", "min", "0", 1, "form-control"], [1, "mb-4"], ["type", "url", "formControlName", "googleDriveUrl", "placeholder", "https://drive.google.com/drive/folders/...", 1, "form-control"], [1, "d-flex", "gap-2"], ["type", "submit", 1, "btn", "btn-primary", "waves-effect", "waves-light", 3, "disabled"], ["routerLink", "/admin/projects", 1, "btn", "btn-outline-secondary", "waves-effect"]], template: function ProjectForm_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "a", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "div", 6)(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "form", 10);
      \u0275\u0275listener("ngSubmit", function ProjectForm_Template_form_ngSubmit_11_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(12, "div", 11)(13, "label", 12);
      \u0275\u0275text(14, "\u5C08\u6848\u7DE8\u865F ");
      \u0275\u0275elementStart(15, "span", 13);
      \u0275\u0275text(16, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(17, "input", 14);
      \u0275\u0275conditionalCreate(18, ProjectForm_Conditional_18_Template, 2, 0, "div", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "div", 11)(20, "label", 12);
      \u0275\u0275text(21, "\u90E8\u9580 ");
      \u0275\u0275elementStart(22, "span", 13);
      \u0275\u0275text(23, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(24, "select", 16)(25, "option", 17);
      \u0275\u0275text(26, "\u2014 \u8ACB\u9078\u64C7\u90E8\u9580 \u2014");
      \u0275\u0275elementEnd();
      \u0275\u0275repeaterCreate(27, ProjectForm_For_28_Template, 2, 2, "option", 17, _forTrack012);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(29, ProjectForm_Conditional_29_Template, 2, 0, "div", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(30, "div", 11)(31, "label", 12);
      \u0275\u0275text(32, "\u9810\u5B9A\u91D1\u984D ");
      \u0275\u0275elementStart(33, "span", 13);
      \u0275\u0275text(34, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(35, "input", 18);
      \u0275\u0275conditionalCreate(36, ProjectForm_Conditional_36_Template, 2, 0, "div", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(37, "div", 11)(38, "label", 12);
      \u0275\u0275text(39, "\u5BE6\u969B\u91D1\u984D ");
      \u0275\u0275elementStart(40, "span", 13);
      \u0275\u0275text(41, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(42, "input", 19);
      \u0275\u0275conditionalCreate(43, ProjectForm_Conditional_43_Template, 2, 0, "div", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(44, "div", 11)(45, "label", 12);
      \u0275\u0275text(46, "\u696D\u52D9\u91D1\u984D ");
      \u0275\u0275elementStart(47, "span", 13);
      \u0275\u0275text(48, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(49, "input", 20);
      \u0275\u0275conditionalCreate(50, ProjectForm_Conditional_50_Template, 2, 0, "div", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(51, "div", 21)(52, "label", 12);
      \u0275\u0275text(53, "Google Drive \u9023\u7D50 ");
      \u0275\u0275elementStart(54, "span", 13);
      \u0275\u0275text(55, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(56, "input", 22);
      \u0275\u0275conditionalCreate(57, ProjectForm_Conditional_57_Template, 2, 0, "div", 15);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(58, "div", 23)(59, "button", 24);
      \u0275\u0275text(60);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(61, "a", 25);
      \u0275\u0275text(62, "\u53D6\u6D88");
      \u0275\u0275elementEnd()()()()()()()();
    }
    if (rf & 2) {
      let tmp_2_0;
      let tmp_5_0;
      let tmp_6_0;
      let tmp_7_0;
      let tmp_8_0;
      let tmp_9_0;
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? "\u7DE8\u8F2F\u5C08\u6848" : "\u65B0\u589E\u5C08\u6848");
      \u0275\u0275advance(5);
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_2_0 = ctx.form.get("code")) == null ? null : tmp_2_0.invalid) && ((tmp_2_0 = ctx.form.get("code")) == null ? null : tmp_2_0.touched) ? 18 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275property("ngValue", null);
      \u0275\u0275advance(2);
      \u0275\u0275repeater(ctx.departments);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(((tmp_5_0 = ctx.form.get("departmentId")) == null ? null : tmp_5_0.invalid) && ((tmp_5_0 = ctx.form.get("departmentId")) == null ? null : tmp_5_0.touched) ? 29 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_6_0 = ctx.form.get("budgetAmount")) == null ? null : tmp_6_0.invalid) && ((tmp_6_0 = ctx.form.get("budgetAmount")) == null ? null : tmp_6_0.touched) ? 36 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_7_0 = ctx.form.get("actualAmount")) == null ? null : tmp_7_0.invalid) && ((tmp_7_0 = ctx.form.get("actualAmount")) == null ? null : tmp_7_0.touched) ? 43 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_8_0 = ctx.form.get("businessAmount")) == null ? null : tmp_8_0.invalid) && ((tmp_8_0 = ctx.form.get("businessAmount")) == null ? null : tmp_8_0.touched) ? 50 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_9_0 = ctx.form.get("googleDriveUrl")) == null ? null : tmp_9_0.invalid) && ((tmp_9_0 = ctx.form.get("googleDriveUrl")) == null ? null : tmp_9_0.touched) ? 57 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275property("disabled", ctx.form.invalid);
      \u0275\u0275advance();
      \u0275\u0275textInterpolate1(" ", ctx.isEdit ? "\u66F4\u65B0" : "\u5EFA\u7ACB", " ");
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, NumberValueAccessor, SelectControlValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, FormGroupDirective, FormControlName, RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ProjectForm, [{
    type: Component,
    args: [{ selector: "app-project-form", imports: [ReactiveFormsModule, RouterLink], template: `<div class="container-fluid py-3">
  <div class="d-flex align-items-center gap-2 mb-4">
    <a routerLink="/admin/projects" class="btn btn-sm btn-outline-secondary waves-effect">
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
    </a>
    <h4 class="mb-0">{{ isEdit ? '\u7DE8\u8F2F\u5C08\u6848' : '\u65B0\u589E\u5C08\u6848' }}</h4>
  </div>

  <div class="row">
    <div class="col-12 col-lg-8 col-xl-6">
      <div class="card border-0 shadow-sm">
        <div class="card-body">
          <form [formGroup]="form" (ngSubmit)="submit()">

            <div class="mb-3">
              <label class="form-label fw-500">\u5C08\u6848\u7DE8\u865F <span class="text-danger">*</span></label>
              <input type="text" class="form-control font-monospace" formControlName="code" placeholder="\u4F8B\u5982\uFF1AP2025-001">
              @if (form.get('code')?.invalid && form.get('code')?.touched) {
                <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u5C08\u6848\u7DE8\u865F\u3002</div>
              }
            </div>

            <div class="mb-3">
              <label class="form-label fw-500">\u90E8\u9580 <span class="text-danger">*</span></label>
              <select class="form-select" formControlName="departmentId">
                <option [ngValue]="null">\u2014 \u8ACB\u9078\u64C7\u90E8\u9580 \u2014</option>
                @for (dept of departments; track dept.id) {
                  <option [ngValue]="dept.id">{{ dept.name }}</option>
                }
              </select>
              @if (form.get('departmentId')?.invalid && form.get('departmentId')?.touched) {
                <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u90E8\u9580\u3002</div>
              }
            </div>

            <div class="mb-3">
              <label class="form-label fw-500">\u9810\u5B9A\u91D1\u984D <span class="text-danger">*</span></label>
              <input type="number" class="form-control" formControlName="budgetAmount" min="0">
              @if (form.get('budgetAmount')?.invalid && form.get('budgetAmount')?.touched) {
                <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u9810\u5B9A\u91D1\u984D\uFF08\u4E0D\u53EF\u70BA\u8CA0\u6578\uFF09\u3002</div>
              }
            </div>

            <div class="mb-3">
              <label class="form-label fw-500">\u5BE6\u969B\u91D1\u984D <span class="text-danger">*</span></label>
              <input type="number" class="form-control" formControlName="actualAmount" min="0">
              @if (form.get('actualAmount')?.invalid && form.get('actualAmount')?.touched) {
                <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u5BE6\u969B\u91D1\u984D\uFF08\u4E0D\u53EF\u70BA\u8CA0\u6578\uFF09\u3002</div>
              }
            </div>

            <div class="mb-3">
              <label class="form-label fw-500">\u696D\u52D9\u91D1\u984D <span class="text-danger">*</span></label>
              <input type="number" class="form-control" formControlName="businessAmount" min="0">
              @if (form.get('businessAmount')?.invalid && form.get('businessAmount')?.touched) {
                <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u696D\u52D9\u91D1\u984D\uFF08\u4E0D\u53EF\u70BA\u8CA0\u6578\uFF09\u3002</div>
              }
            </div>

            <div class="mb-4">
              <label class="form-label fw-500">Google Drive \u9023\u7D50 <span class="text-danger">*</span></label>
              <input type="url" class="form-control" formControlName="googleDriveUrl"
                     placeholder="https://drive.google.com/drive/folders/...">
              @if (form.get('googleDriveUrl')?.invalid && form.get('googleDriveUrl')?.touched) {
                <div class="text-danger small mt-1">\u8ACB\u8F38\u5165 Google Drive \u9023\u7D50\u3002</div>
              }
            </div>

            <div class="d-flex gap-2">
              <button type="submit" class="btn btn-primary waves-effect waves-light" [disabled]="form.invalid">
                {{ isEdit ? '\u66F4\u65B0' : '\u5EFA\u7ACB' }}
              </button>
              <a routerLink="/admin/projects" class="btn btn-outline-secondary waves-effect">\u53D6\u6D88</a>
            </div>

          </form>
        </div>
      </div>
    </div>
  </div>
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ProjectForm, { className: "ProjectForm", filePath: "src/app/features/admin/projects/pages/project-form/project-form.ts", lineNumber: 13 });
})();

// src/app/features/admin/payment-requests/services/payment-request.service.ts
var MOCK_REQUESTS = [
  {
    id: 1,
    type: "vendor",
    projectId: 1,
    projectCode: "P2024-001",
    invoices: [
      { id: "i1", fileName: "invoice_001.jpg", invoiceNo: "AB-12345678", amount: 15e3 },
      { id: "i2", fileName: "invoice_002.jpg", invoiceNo: "CD-87654321", amount: 8500 }
    ],
    totalAmount: 23500,
    approvalStatus: "approved",
    createdAt: /* @__PURE__ */ new Date("2024-04-10")
  },
  {
    id: 2,
    type: "travel",
    projectId: 2,
    projectCode: "P2024-002",
    invoices: [
      { id: "i3", fileName: "receipt_hotel.jpg", invoiceNo: "EF-11223344", amount: 4200 },
      { id: "i4", fileName: "receipt_train.jpg", invoiceNo: "GH-55667788", amount: 980 }
    ],
    totalAmount: 5180,
    approvalStatus: "pending",
    createdAt: /* @__PURE__ */ new Date("2024-07-05")
  },
  {
    id: 3,
    type: "advance",
    projectId: 3,
    projectCode: "P2025-001",
    invoices: [
      { id: "i5", fileName: "advance_001.jpg", invoiceNo: "IJ-99887766", amount: 2e4 }
    ],
    totalAmount: 2e4,
    approvalStatus: "rejected",
    createdAt: /* @__PURE__ */ new Date("2025-02-01")
  }
];
var PaymentRequestService = class _PaymentRequestService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_REQUESTS);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id) {
    return of(this.items$.getValue().find((r) => r.id === id));
  }
  create(data) {
    const item = __spreadProps(__spreadValues({}, data), { id: Date.now(), createdAt: /* @__PURE__ */ new Date() });
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }
  update(id, changes) {
    const updated = this.items$.getValue().map((r) => r.id === id ? __spreadValues(__spreadValues({}, r), changes) : r);
    this.items$.next(updated);
    return of(updated.find((r) => r.id === id));
  }
  delete(id) {
    this.items$.next(this.items$.getValue().filter((r) => r.id !== id));
    return of(void 0);
  }
  static \u0275fac = function PaymentRequestService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PaymentRequestService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _PaymentRequestService, factory: _PaymentRequestService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PaymentRequestService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/payment-requests/models/payment-request.model.ts
var PAYMENT_TYPE_LABELS = {
  vendor: "\u5EE0\u5546\u8ACB\u6B3E",
  travel: "\u54E1\u5DE5\u5DEE\u65C5",
  advance: "\u54E1\u5DE5\u9810\u652F"
};
var PAYMENT_TYPE_CLASSES = {
  vendor: "bg-info-subtle text-info",
  travel: "bg-primary-subtle text-primary",
  advance: "bg-warning-subtle text-warning-emphasis"
};
var APPROVAL_STATUS_LABELS = {
  pending: "\u5F85\u5BE9\u6838",
  approved: "\u5DF2\u6838\u51C6",
  rejected: "\u5DF2\u9000\u56DE"
};
var APPROVAL_STATUS_CLASSES = {
  pending: "bg-warning-subtle text-warning-emphasis",
  approved: "bg-success-subtle text-success",
  rejected: "bg-danger-subtle text-danger"
};

// src/app/features/admin/payment-requests/pages/payment-list/payment-list.ts
var _c08 = (a0) => [a0, "edit"];
var _forTrack013 = ($index, $item) => $item.id;
function PaymentList_For_33_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td")(2, "span");
    \u0275\u0275text(3);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(4, "td", 16);
    \u0275\u0275text(5);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "td")(7, "span", 17);
    \u0275\u0275text(8);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(9, "td", 18);
    \u0275\u0275text(10);
    \u0275\u0275pipe(11, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "td")(13, "span");
    \u0275\u0275text(14);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(15, "td", 19);
    \u0275\u0275text(16);
    \u0275\u0275pipe(17, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(18, "td", 20)(19, "div", 21)(20, "a", 22);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(21, "svg", 7);
    \u0275\u0275element(22, "use", 23);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(23, "button", 24);
    \u0275\u0275listener("click", function PaymentList_For_33_Template_button_click_23_listener() {
      const r_r2 = \u0275\u0275restoreView(_r1).$implicit;
      const ctx_r2 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r2.delete(r_r2));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(24, "svg", 7);
    \u0275\u0275element(25, "use", 25);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const r_r2 = ctx.$implicit;
    const ctx_r2 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275classMap("badge " + ctx_r2.typeClass[r_r2.type]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r2.typeLabel[r_r2.type]);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(r_r2.projectCode);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1("", r_r2.invoices.length, " \u5F35");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(11, 11, r_r2.totalAmount, "1.0-0"));
    \u0275\u0275advance(3);
    \u0275\u0275classMap("badge " + ctx_r2.statusClass[r_r2.approvalStatus]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r2.statusLabel[r_r2.approvalStatus]);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(17, 14, r_r2.createdAt, "yyyy-MM-dd"));
    \u0275\u0275advance(4);
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(17, _c08, r_r2.id));
  }
}
function PaymentList_ForEmpty_34_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 26);
    \u0275\u0275text(2, "\u5C1A\u7121\u8ACB\u6B3E\u7533\u8ACB\u3002");
    \u0275\u0275elementEnd()();
  }
}
var PaymentList = class _PaymentList {
  service = inject(PaymentRequestService);
  requests$ = this.service.getAll();
  typeLabel = PAYMENT_TYPE_LABELS;
  typeClass = PAYMENT_TYPE_CLASSES;
  statusLabel = APPROVAL_STATUS_LABELS;
  statusClass = APPROVAL_STATUS_CLASSES;
  delete(r) {
    if (confirm(`\u78BA\u5B9A\u8981\u522A\u9664\u6B64\u8ACB\u6B3E\u7533\u8ACB\u55CE\uFF1F`)) {
      this.service.delete(r.id).subscribe();
    }
  }
  static \u0275fac = function PaymentList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PaymentList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _PaymentList, selectors: [["app-payment-list"]], decls: 36, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#dollar-sign"], [1, "mb-0"], ["routerLink", "new", 1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "text-end"], [1, "d-none", "d-lg-table-cell"], [1, "fw-500", "font-monospace"], [1, "badge", "bg-secondary-subtle", "text-secondary"], [1, "text-end", "fw-500"], [1, "text-muted", "small", "d-none", "d-lg-table-cell"], [1, "text-end", 2, "white-space", "nowrap"], [1, "d-flex", "justify-content-end", "gap-1"], [1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], ["colspan", "7", 1, "text-center", "text-muted", "py-4"]], template: function PaymentList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u8ACB\u6B3E\u7533\u8ACB");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "a", 6);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(8, "svg", 7);
      \u0275\u0275element(9, "use", 8);
      \u0275\u0275elementEnd();
      \u0275\u0275text(10, " \u65B0\u589E\u7533\u8ACB ");
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(11, "div", 9)(12, "div", 10)(13, "div", 11)(14, "table", 12)(15, "thead", 13)(16, "tr")(17, "th");
      \u0275\u0275text(18, "\u985E\u578B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "th");
      \u0275\u0275text(20, "\u5C08\u6848\u7DE8\u865F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(21, "th");
      \u0275\u0275text(22, "\u767C\u7968\u6578");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(23, "th", 14);
      \u0275\u0275text(24, "\u7E3D\u91D1\u984D");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(25, "th");
      \u0275\u0275text(26, "\u7C3D\u6838\u72C0\u614B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(27, "th", 15);
      \u0275\u0275text(28, "\u5EFA\u7ACB\u6642\u9593");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(29, "th", 14);
      \u0275\u0275text(30, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(31, "tbody");
      \u0275\u0275repeaterCreate(32, PaymentList_For_33_Template, 26, 19, "tr", null, _forTrack013, false, PaymentList_ForEmpty_34_Template, 3, 0, "tr");
      \u0275\u0275pipe(35, "async");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(32);
      \u0275\u0275repeater(\u0275\u0275pipeBind1(35, 1, ctx.requests$));
    }
  }, dependencies: [RouterLink, AsyncPipe, DecimalPipe, DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PaymentList, [{
    type: Component,
    args: [{ selector: "app-payment-list", imports: [RouterLink, AsyncPipe, DecimalPipe, DatePipe], template: `<div class="container-fluid py-3">
  <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">
    <div class="d-flex align-items-center gap-2">
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">
        <use href="/assets/icons/sprite.svg#dollar-sign"></use>
      </svg>
      <h4 class="mb-0">\u8ACB\u6B3E\u7533\u8ACB</h4>
    </div>
    <a routerLink="new" class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light">
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
      \u65B0\u589E\u7533\u8ACB
    </a>
  </div>

  <div class="card border-0 shadow-sm">
    <div class="card-body p-0">
      <div class="table-responsive">
        <table class="table table-hover mb-0">
          <thead class="table-light">
            <tr>
              <th>\u985E\u578B</th>
              <th>\u5C08\u6848\u7DE8\u865F</th>
              <th>\u767C\u7968\u6578</th>
              <th class="text-end">\u7E3D\u91D1\u984D</th>
              <th>\u7C3D\u6838\u72C0\u614B</th>
              <th class="d-none d-lg-table-cell">\u5EFA\u7ACB\u6642\u9593</th>
              <th class="text-end">\u64CD\u4F5C</th>
            </tr>
          </thead>
          <tbody>
            @for (r of requests$ | async; track r.id) {
              <tr>
                <td>
                  <span [class]="'badge ' + typeClass[r.type]">{{ typeLabel[r.type] }}</span>
                </td>
                <td class="fw-500 font-monospace">{{ r.projectCode }}</td>
                <td>
                  <span class="badge bg-secondary-subtle text-secondary">{{ r.invoices.length }} \u5F35</span>
                </td>
                <td class="text-end fw-500">{{ r.totalAmount | number:'1.0-0' }}</td>
                <td>
                  <span [class]="'badge ' + statusClass[r.approvalStatus]">{{ statusLabel[r.approvalStatus] }}</span>
                </td>
                <td class="text-muted small d-none d-lg-table-cell">{{ r.createdAt | date:'yyyy-MM-dd' }}</td>
                <td class="text-end" style="white-space: nowrap">
                  <div class="d-flex justify-content-end gap-1">
                    <a [routerLink]="[r.id, 'edit']" class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>
                    </a>
                    <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect" (click)="delete(r)">
                      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>
                    </button>
                  </div>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="7" class="text-center text-muted py-4">\u5C1A\u7121\u8ACB\u6B3E\u7533\u8ACB\u3002</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  </div>
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(PaymentList, { className: "PaymentList", filePath: "src/app/features/admin/payment-requests/pages/payment-list/payment-list.ts", lineNumber: 16 });
})();

// src/app/features/admin/payment-requests/pages/payment-form/payment-form.ts
var _forTrack014 = ($index, $item) => $item.id;
function _forTrack12($index, $item) {
  let tmp_0_0;
  return (tmp_0_0 = $item.get("id")) == null ? null : tmp_0_0.value;
}
function PaymentForm_For_43_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 26);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const p_r1 = ctx.$implicit;
    \u0275\u0275property("ngValue", p_r1.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate2("", p_r1.code, "", p_r1.departmentName ? "\uFF08" + p_r1.departmentName + "\uFF09" : "");
  }
}
function PaymentForm_Conditional_44_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 27);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u5C08\u6848\u3002");
    \u0275\u0275elementEnd();
  }
}
function PaymentForm_Conditional_45_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 5)(1, "label", 15);
    \u0275\u0275text(2, "\u7C3D\u6838\u72C0\u614B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div")(4, "span", 48);
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
function PaymentForm_Conditional_60_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 36);
    \u0275\u0275text(1, "\u8ACB\u81F3\u5C11\u4E0A\u50B3\u4E00\u5F35\u767C\u7968\u3002");
    \u0275\u0275elementEnd();
  }
}
function PaymentForm_For_74_Template(rf, ctx) {
  if (rf & 1) {
    const _r3 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr", 44)(1, "td", 49);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 50);
    \u0275\u0275element(3, "use", 29);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "td");
    \u0275\u0275element(6, "input", 51);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "td");
    \u0275\u0275element(8, "input", 52);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 53)(10, "button", 54);
    \u0275\u0275listener("click", function PaymentForm_For_74_Template_button_click_10_listener() {
      const \u0275$index_144_r4 = \u0275\u0275restoreView(_r3).$index;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.removeInvoice(\u0275$index_144_r4));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(11, "svg", 55);
    \u0275\u0275element(12, "use", 56);
    \u0275\u0275elementEnd()()()();
  }
  if (rf & 2) {
    let tmp_12_0;
    const ctrl_r5 = ctx.$implicit;
    const \u0275$index_144_r4 = ctx.$index;
    \u0275\u0275property("formGroupName", \u0275$index_144_r4);
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate1(" ", (tmp_12_0 = ctrl_r5.get("fileName")) == null ? null : tmp_12_0.value, " ");
  }
}
function PaymentForm_ForEmpty_75_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 57);
    \u0275\u0275text(2, "\u5C1A\u672A\u65B0\u589E\u767C\u7968");
    \u0275\u0275elementEnd()();
  }
}
function PaymentForm_Conditional_76_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tfoot")(1, "tr", 39)(2, "td", 58);
    \u0275\u0275text(3, "\u5408\u8A08");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "td", 59);
    \u0275\u0275text(5);
    \u0275\u0275pipe(6, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275element(7, "td");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(6, 1, ctx_r1.totalAmount, "1.0-0"));
  }
}
var PaymentForm = class _PaymentForm {
  fb = inject(FormBuilder);
  service = inject(PaymentRequestService);
  projects$ = inject(ProjectService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  projects = [];
  isEdit = false;
  requestId = 0;
  showInvoiceError = false;
  approvalStatus = "pending";
  statusLabel = APPROVAL_STATUS_LABELS;
  statusClass = APPROVAL_STATUS_CLASSES;
  form = this.fb.group({
    type: ["vendor", Validators.required],
    projectId: [null, Validators.required],
    invoices: this.fb.array([])
  });
  get invoiceArray() {
    return this.form.get("invoices");
  }
  get invoiceControls() {
    return this.invoiceArray.controls;
  }
  get totalAmount() {
    return this.invoiceArray.controls.reduce((sum, c) => sum + (+c.get("amount")?.value || 0), 0);
  }
  ngOnInit() {
    this.projects$.getAll().subscribe((p) => this.projects = p);
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEdit = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe((r) => {
        if (!r)
          return;
        this.approvalStatus = r.approvalStatus;
        this.form.patchValue({ type: r.type, projectId: r.projectId });
        r.invoices.forEach((inv) => this.invoiceArray.push(this._invoiceGroup(inv.id, inv.fileName, inv.invoiceNo, inv.amount)));
      });
    }
  }
  onFilesSelected(event) {
    const input = event.target;
    if (!input.files?.length)
      return;
    Array.from(input.files).forEach((file) => {
      const mock = this._mockOcr(file.name);
      this.invoiceArray.push(this._invoiceGroup(`${Date.now()}-${Math.random().toString(36).slice(2)}`, file.name, mock.invoiceNo, mock.amount));
    });
    input.value = "";
    this.showInvoiceError = false;
  }
  removeInvoice(i) {
    this.invoiceArray.removeAt(i);
  }
  submit() {
    if (this.form.invalid)
      return;
    if (this.invoiceArray.length === 0) {
      this.showInvoiceError = true;
      return;
    }
    this.showInvoiceError = false;
    const v = this.form.value;
    const project = this.projects.find((p) => p.id === v.projectId);
    const invoices = this.invoiceArray.value.map((inv) => __spreadProps(__spreadValues({}, inv), { amount: +inv.amount }));
    const payload = {
      type: v.type,
      projectId: v.projectId,
      projectCode: project?.code ?? "",
      invoices,
      totalAmount: this.totalAmount,
      approvalStatus: this.approvalStatus
    };
    const obs = this.isEdit ? this.service.update(this.requestId, payload) : this.service.create(payload);
    obs.subscribe(() => this.router.navigate(["/admin/payment-requests"]));
  }
  _invoiceGroup(id, fileName, invoiceNo, amount) {
    return this.fb.group({
      id: [id],
      fileName: [fileName],
      invoiceNo: [invoiceNo, Validators.required],
      amount: [amount, [Validators.required, Validators.min(0)]]
    });
  }
  _mockOcr(fileName) {
    const abc = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const l1 = abc[Math.floor(Math.random() * 26)];
    const l2 = abc[Math.floor(Math.random() * 26)];
    const num = String(Math.floor(Math.random() * 1e8)).padStart(8, "0");
    const amount = Math.floor(Math.random() * 50 + 1) * 1e3;
    return { invoiceNo: `${l1}${l2}-${num}`, amount };
  }
  static \u0275fac = function PaymentForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PaymentForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _PaymentForm, selectors: [["app-payment-form"]], decls: 82, vars: 10, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], ["routerLink", "/admin/payment-requests", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [3, "ngSubmit", "formGroup"], [1, "row", "g-4"], [1, "col-12", "col-lg-10", "col-xl-8"], [1, "card", "border-0", "shadow-sm"], [1, "card-header", "bg-transparent", "border-bottom", "d-flex", "align-items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#dollar-sign"], [1, "card-body"], [1, "mb-3"], [1, "form-label", "fw-500"], [1, "text-danger"], [1, "d-flex", "flex-wrap", "gap-3", "mt-1"], [1, "form-check"], ["type", "radio", "formControlName", "type", "value", "vendor", "id", "typeVendor", 1, "form-check-input"], ["for", "typeVendor", 1, "form-check-label"], ["type", "radio", "formControlName", "type", "value", "travel", "id", "typeTravel", 1, "form-check-input"], ["for", "typeTravel", 1, "form-check-label"], ["type", "radio", "formControlName", "type", "value", "advance", "id", "typeAdvance", 1, "form-check-input"], ["for", "typeAdvance", 1, "form-check-label"], ["formControlName", "projectId", 1, "form-select"], [3, "ngValue"], [1, "text-danger", "small", "mt-1"], [1, "card", "border-0", "shadow-sm", "mt-4"], ["href", "/assets/icons/sprite.svg#file-text"], [1, "d-flex", "flex-column", "align-items-center", "justify-content-center", "rounded-3", "py-4", "px-3", "mb-3", "text-center", 2, "cursor", "pointer", "border", "2px dashed var(--bs-border-color)"], [1, "sa-icon", "sa-icon-2x", "text-muted", "mb-2", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#upload"], [1, "fw-500"], [1, "text-muted", "small", "mt-1"], ["type", "file", "multiple", "", "accept", "image/*,application/pdf", 1, "d-none", 3, "change"], [1, "alert", "alert-warning", "py-2", "small", "mb-3"], [1, "table-responsive"], [1, "table", "table-sm", "mb-0"], [1, "table-light"], [2, "min-width", "160px"], [2, "min-width", "120px"], [2, "width", "48px"], ["formArrayName", "invoices"], [3, "formGroupName"], [1, "mt-4", "d-flex", "gap-2"], ["type", "submit", 1, "btn", "btn-primary", "waves-effect", "waves-light", 3, "disabled"], ["routerLink", "/admin/payment-requests", 1, "btn", "btn-outline-secondary", "waves-effect"], [1, "badge", "rounded-pill", "px-3", "py-2"], [1, "align-middle", "text-muted", "small", 2, "max-width", "200px", "overflow", "hidden", "text-overflow", "ellipsis", "white-space", "nowrap"], [1, "sa-icon", "me-1", 2, "width", "14px", "height", "14px", "stroke", "currentColor"], ["formControlName", "invoiceNo", "placeholder", "AB-12345678", 1, "form-control", "form-control-sm", "font-monospace"], ["type", "number", "formControlName", "amount", "min", "0", "placeholder", "0", 1, "form-control", "form-control-sm"], [1, "text-end", "align-middle"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#x"], ["colspan", "4", 1, "text-center", "text-muted", "py-3", "small"], ["colspan", "2", 1, "text-end", "fw-500", "small"], [1, "fw-600"]], template: function PaymentForm_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "a", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "form", 6);
      \u0275\u0275listener("ngSubmit", function PaymentForm_Template_form_ngSubmit_7_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "div", 10);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(12, "svg", 11);
      \u0275\u0275element(13, "use", 12);
      \u0275\u0275elementEnd();
      \u0275\u0275text(14, " \u57FA\u672C\u8CC7\u8A0A ");
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(15, "div", 13)(16, "div", 14)(17, "label", 15);
      \u0275\u0275text(18, "\u8ACB\u6B3E\u985E\u578B ");
      \u0275\u0275elementStart(19, "span", 16);
      \u0275\u0275text(20, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(21, "div", 17)(22, "div", 18);
      \u0275\u0275element(23, "input", 19);
      \u0275\u0275elementStart(24, "label", 20);
      \u0275\u0275text(25, "\u5EE0\u5546\u8ACB\u6B3E");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(26, "div", 18);
      \u0275\u0275element(27, "input", 21);
      \u0275\u0275elementStart(28, "label", 22);
      \u0275\u0275text(29, "\u54E1\u5DE5\u5DEE\u65C5");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(30, "div", 18);
      \u0275\u0275element(31, "input", 23);
      \u0275\u0275elementStart(32, "label", 24);
      \u0275\u0275text(33, "\u54E1\u5DE5\u9810\u652F");
      \u0275\u0275elementEnd()()()();
      \u0275\u0275elementStart(34, "div", 14)(35, "label", 15);
      \u0275\u0275text(36, "\u5C08\u6848 ");
      \u0275\u0275elementStart(37, "span", 16);
      \u0275\u0275text(38, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(39, "select", 25)(40, "option", 26);
      \u0275\u0275text(41, "\u2014 \u8ACB\u9078\u64C7\u5C08\u6848 \u2014");
      \u0275\u0275elementEnd();
      \u0275\u0275repeaterCreate(42, PaymentForm_For_43_Template, 2, 3, "option", 26, _forTrack014);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(44, PaymentForm_Conditional_44_Template, 2, 0, "div", 27);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(45, PaymentForm_Conditional_45_Template, 6, 3, "div", 5);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(46, "div", 28)(47, "div", 10);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(48, "svg", 11);
      \u0275\u0275element(49, "use", 29);
      \u0275\u0275elementEnd();
      \u0275\u0275text(50, " \u767C\u7968 ");
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(51, "div", 13)(52, "label", 30);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(53, "svg", 31);
      \u0275\u0275element(54, "use", 32);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(55, "span", 33);
      \u0275\u0275text(56, "\u9EDE\u64CA\u4E0A\u50B3\u767C\u7968\u5716\u6A94");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(57, "span", 34);
      \u0275\u0275text(58, "\u652F\u63F4 JPG\u3001PNG\u3001PDF\uFF0C\u53EF\u591A\u9078");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(59, "input", 35);
      \u0275\u0275listener("change", function PaymentForm_Template_input_change_59_listener($event) {
        return ctx.onFilesSelected($event);
      });
      \u0275\u0275elementEnd()();
      \u0275\u0275conditionalCreate(60, PaymentForm_Conditional_60_Template, 2, 0, "div", 36);
      \u0275\u0275elementStart(61, "div", 37)(62, "table", 38)(63, "thead", 39)(64, "tr")(65, "th");
      \u0275\u0275text(66, "\u6A94\u6848\u540D\u7A31");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(67, "th", 40);
      \u0275\u0275text(68, "\u767C\u7968\u865F\u78BC");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(69, "th", 41);
      \u0275\u0275text(70, "\u91D1\u984D");
      \u0275\u0275elementEnd();
      \u0275\u0275element(71, "th", 42);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(72, "tbody", 43);
      \u0275\u0275repeaterCreate(73, PaymentForm_For_74_Template, 13, 2, "tr", 44, _forTrack12, false, PaymentForm_ForEmpty_75_Template, 3, 0, "tr");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(76, PaymentForm_Conditional_76_Template, 8, 4, "tfoot");
      \u0275\u0275elementEnd()()()();
      \u0275\u0275elementStart(77, "div", 45)(78, "button", 46);
      \u0275\u0275text(79);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(80, "a", 47);
      \u0275\u0275text(81, "\u53D6\u6D88");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      let tmp_4_0;
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? "\u7DE8\u8F2F\u8ACB\u6B3E\u7533\u8ACB" : "\u65B0\u589E\u8ACB\u6B3E\u7533\u8ACB");
      \u0275\u0275advance();
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(33);
      \u0275\u0275property("ngValue", null);
      \u0275\u0275advance(2);
      \u0275\u0275repeater(ctx.projects);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(((tmp_4_0 = ctx.form.get("projectId")) == null ? null : tmp_4_0.invalid) && ((tmp_4_0 = ctx.form.get("projectId")) == null ? null : tmp_4_0.touched) ? 44 : -1);
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.isEdit ? 45 : -1);
      \u0275\u0275advance(15);
      \u0275\u0275conditional(ctx.showInvoiceError ? 60 : -1);
      \u0275\u0275advance(13);
      \u0275\u0275repeater(ctx.invoiceControls);
      \u0275\u0275advance(3);
      \u0275\u0275conditional(ctx.invoiceControls.length > 0 ? 76 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275property("disabled", ctx.form.invalid);
      \u0275\u0275advance();
      \u0275\u0275textInterpolate1(" ", ctx.isEdit ? "\u66F4\u65B0" : "\u5EFA\u7ACB", " ");
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, NumberValueAccessor, SelectControlValueAccessor, RadioControlValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, FormGroupDirective, FormControlName, FormGroupName, FormArrayName, RouterLink, DecimalPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PaymentForm, [{
    type: Component,
    args: [{ selector: "app-payment-form", imports: [ReactiveFormsModule, RouterLink, DecimalPipe], template: `<div class="container-fluid py-3">
  <div class="d-flex align-items-center gap-2 mb-4">
    <a routerLink="/admin/payment-requests" class="btn btn-sm btn-outline-secondary waves-effect">
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
    </a>
    <h4 class="mb-0">{{ isEdit ? '\u7DE8\u8F2F\u8ACB\u6B3E\u7533\u8ACB' : '\u65B0\u589E\u8ACB\u6B3E\u7533\u8ACB' }}</h4>
  </div>

  <form [formGroup]="form" (ngSubmit)="submit()">
    <div class="row g-4">
      <div class="col-12 col-lg-10 col-xl-8">

        <!-- \u57FA\u672C\u8CC7\u8A0A -->
        <div class="card border-0 shadow-sm">
          <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">
            <svg class="sa-icon text-primary" style="stroke: currentColor">
              <use href="/assets/icons/sprite.svg#dollar-sign"></use>
            </svg>
            \u57FA\u672C\u8CC7\u8A0A
          </div>
          <div class="card-body">

            <div class="mb-3">
              <label class="form-label fw-500">\u8ACB\u6B3E\u985E\u578B <span class="text-danger">*</span></label>
              <div class="d-flex flex-wrap gap-3 mt-1">
                <div class="form-check">
                  <input class="form-check-input" type="radio" formControlName="type" value="vendor" id="typeVendor">
                  <label class="form-check-label" for="typeVendor">\u5EE0\u5546\u8ACB\u6B3E</label>
                </div>
                <div class="form-check">
                  <input class="form-check-input" type="radio" formControlName="type" value="travel" id="typeTravel">
                  <label class="form-check-label" for="typeTravel">\u54E1\u5DE5\u5DEE\u65C5</label>
                </div>
                <div class="form-check">
                  <input class="form-check-input" type="radio" formControlName="type" value="advance" id="typeAdvance">
                  <label class="form-check-label" for="typeAdvance">\u54E1\u5DE5\u9810\u652F</label>
                </div>
              </div>
            </div>

            <div class="mb-3">
              <label class="form-label fw-500">\u5C08\u6848 <span class="text-danger">*</span></label>
              <select class="form-select" formControlName="projectId">
                <option [ngValue]="null">\u2014 \u8ACB\u9078\u64C7\u5C08\u6848 \u2014</option>
                @for (p of projects; track p.id) {
                  <option [ngValue]="p.id">{{ p.code }}{{ p.departmentName ? '\uFF08' + p.departmentName + '\uFF09' : '' }}</option>
                }
              </select>
              @if (form.get('projectId')?.invalid && form.get('projectId')?.touched) {
                <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u5C08\u6848\u3002</div>
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

        <!-- \u767C\u7968 -->
        <div class="card border-0 shadow-sm mt-4">
          <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">
            <svg class="sa-icon text-primary" style="stroke: currentColor">
              <use href="/assets/icons/sprite.svg#file-text"></use>
            </svg>
            \u767C\u7968
          </div>
          <div class="card-body">

            <!-- Upload area -->
            <label class="d-flex flex-column align-items-center justify-content-center rounded-3 py-4 px-3 mb-3 text-center"
                   style="cursor:pointer; border: 2px dashed var(--bs-border-color);">
              <svg class="sa-icon sa-icon-2x text-muted mb-2" style="stroke: currentColor">
                <use href="/assets/icons/sprite.svg#upload"></use>
              </svg>
              <span class="fw-500">\u9EDE\u64CA\u4E0A\u50B3\u767C\u7968\u5716\u6A94</span>
              <span class="text-muted small mt-1">\u652F\u63F4 JPG\u3001PNG\u3001PDF\uFF0C\u53EF\u591A\u9078</span>
              <input type="file" class="d-none" multiple accept="image/*,application/pdf"
                     (change)="onFilesSelected($event)">
            </label>

            @if (showInvoiceError) {
              <div class="alert alert-warning py-2 small mb-3">\u8ACB\u81F3\u5C11\u4E0A\u50B3\u4E00\u5F35\u767C\u7968\u3002</div>
            }

            <!-- Invoice table -->
            <div class="table-responsive">
              <table class="table table-sm mb-0">
                <thead class="table-light">
                  <tr>
                    <th>\u6A94\u6848\u540D\u7A31</th>
                    <th style="min-width:160px">\u767C\u7968\u865F\u78BC</th>
                    <th style="min-width:120px">\u91D1\u984D</th>
                    <th style="width:48px"></th>
                  </tr>
                </thead>
                <tbody formArrayName="invoices">
                  @for (ctrl of invoiceControls; track ctrl.get('id')?.value; let i = $index) {
                    <tr [formGroupName]="i">
                      <td class="align-middle text-muted small"
                          style="max-width:200px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">
                        <svg class="sa-icon me-1" style="width:14px;height:14px;stroke:currentColor">
                          <use href="/assets/icons/sprite.svg#file-text"></use>
                        </svg>
                        {{ ctrl.get('fileName')?.value }}
                      </td>
                      <td>
                        <input class="form-control form-control-sm font-monospace"
                               formControlName="invoiceNo" placeholder="AB-12345678">
                      </td>
                      <td>
                        <input type="number" class="form-control form-control-sm"
                               formControlName="amount" min="0" placeholder="0">
                      </td>
                      <td class="text-end align-middle">
                        <button type="button"
                                class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect"
                                (click)="removeInvoice(i)">
                          <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#x"></use></svg>
                        </button>
                      </td>
                    </tr>
                  } @empty {
                    <tr>
                      <td colspan="4" class="text-center text-muted py-3 small">\u5C1A\u672A\u65B0\u589E\u767C\u7968</td>
                    </tr>
                  }
                </tbody>
                @if (invoiceControls.length > 0) {
                  <tfoot>
                    <tr class="table-light">
                      <td colspan="2" class="text-end fw-500 small">\u5408\u8A08</td>
                      <td class="fw-600">{{ totalAmount | number:'1.0-0' }}</td>
                      <td></td>
                    </tr>
                  </tfoot>
                }
              </table>
            </div>

          </div>
        </div>

        <!-- Submit -->
        <div class="mt-4 d-flex gap-2">
          <button type="submit" class="btn btn-primary waves-effect waves-light" [disabled]="form.invalid">
            {{ isEdit ? '\u66F4\u65B0' : '\u5EFA\u7ACB' }}
          </button>
          <a routerLink="/admin/payment-requests" class="btn btn-outline-secondary waves-effect">\u53D6\u6D88</a>
        </div>

      </div>
    </div>
  </form>
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(PaymentForm, { className: "PaymentForm", filePath: "src/app/features/admin/payment-requests/pages/payment-form/payment-form.ts", lineNumber: 15 });
})();

// src/app/features/admin/approval-tasks/services/approval-task.service.ts
var MOCK_TASKS = [
  // ── Payment requests ──────────────────────────────────────────────────────
  {
    id: 2,
    applicationType: "payment_request",
    title: "\u8ACB\u6B3E\u7533\u8ACB #2\uFF08P2024-002\uFF09",
    submittedBy: "\u738B\u5C0F\u660E",
    submittedAt: /* @__PURE__ */ new Date("2024-07-05"),
    status: "pending",
    flow: {
      id: 2,
      name: "\u63A1\u8CFC\u7533\u8ACB",
      steps: [
        { stepOrder: 1, jobTitleName: "\u90E8\u9580\u4E3B\u7BA1", note: "\u90E8\u9580\u4E3B\u7BA1\u5BE9\u6838" },
        { stepOrder: 2, departmentName: "\u7BA1\u7406\u90E8", jobTitleName: "\u90E8\u9580\u4E3B\u7BA1", note: "\u7BA1\u7406\u90E8\u6838\u51C6" }
      ]
    },
    paymentDetail: {
      paymentRequestId: 2,
      paymentType: "travel",
      projectCode: "P2024-002",
      invoices: [
        { id: "i3", fileName: "receipt_hotel.jpg", invoiceNo: "EF-11223344", amount: 4200 },
        { id: "i4", fileName: "receipt_train.jpg", invoiceNo: "GH-55667788", amount: 980 }
      ],
      totalAmount: 5180
    }
  },
  {
    id: 1,
    applicationType: "payment_request",
    title: "\u8ACB\u6B3E\u7533\u8ACB #1\uFF08P2024-001\uFF09",
    submittedBy: "\u674E\u5927\u83EF",
    submittedAt: /* @__PURE__ */ new Date("2024-04-10"),
    status: "approved",
    reviewedAt: /* @__PURE__ */ new Date("2024-04-12"),
    reviewNote: "\u91D1\u984D\u6838\u5BE6\u7121\u8AA4\uFF0C\u5DF2\u6838\u51C6\u3002",
    paymentDetail: {
      paymentRequestId: 1,
      paymentType: "vendor",
      projectCode: "P2024-001",
      invoices: [
        { id: "i1", fileName: "invoice_001.jpg", invoiceNo: "AB-12345678", amount: 15e3 },
        { id: "i2", fileName: "invoice_002.jpg", invoiceNo: "CD-87654321", amount: 8500 }
      ],
      totalAmount: 23500
    }
  },
  // ── Leave requests ────────────────────────────────────────────────────────
  {
    id: 1,
    applicationType: "leave",
    title: "\u8ACB\u5047\u7533\u8ACB #1\uFF08annual\uFF09",
    submittedBy: "Bob",
    submittedAt: /* @__PURE__ */ new Date("2026-02-10"),
    status: "pending",
    flow: {
      id: 1,
      name: "\u8ACB\u5047\u7533\u8ACB",
      steps: [
        { stepOrder: 1, departmentName: "\u7BA1\u7406\u90E8", jobTitleName: "\u90E8\u9580\u4E3B\u7BA1", note: "\u76F4\u5C6C\u4E3B\u7BA1\u6838\u53EF" },
        { stepOrder: 2, departmentName: "\u7BA1\u7406\u90E8", jobTitleName: "\u90E8\u9580\u4E3B\u7BA1", note: "\u4EBA\u4E8B\u90E8\u9580\u78BA\u8A8D" }
      ]
    },
    leaveDetail: {
      leaveRequestId: 1,
      leaveType: "annual",
      startDate: /* @__PURE__ */ new Date("2026-03-01"),
      endDate: /* @__PURE__ */ new Date("2026-03-05"),
      days: 5,
      reason: "\u500B\u4EBA\u65C5\u904A"
    }
  },
  {
    id: 2,
    applicationType: "leave",
    title: "\u8ACB\u5047\u7533\u8ACB #2\uFF08sick\uFF09",
    submittedBy: "Carol",
    submittedAt: /* @__PURE__ */ new Date("2026-02-20"),
    status: "approved",
    reviewedAt: /* @__PURE__ */ new Date("2026-02-20"),
    reviewNote: "\u6838\u51C6",
    leaveDetail: {
      leaveRequestId: 2,
      leaveType: "sick",
      startDate: /* @__PURE__ */ new Date("2026-02-20"),
      endDate: /* @__PURE__ */ new Date("2026-02-21"),
      days: 2,
      reason: "\u8EAB\u9AD4\u4E0D\u9069\u5C31\u91AB"
    }
  },
  // ── Travel requests ───────────────────────────────────────────────────────
  {
    id: 1,
    applicationType: "travel",
    title: "\u51FA\u5DEE\u7533\u8ACB #1\uFF08\u53F0\u5357\uFF09",
    submittedBy: "Alice",
    submittedAt: /* @__PURE__ */ new Date("2026-03-01"),
    status: "pending",
    travelDetail: {
      travelRequestId: 1,
      destination: "\u53F0\u5357",
      startDate: /* @__PURE__ */ new Date("2026-03-10"),
      endDate: /* @__PURE__ */ new Date("2026-03-12"),
      estimatedCost: 3e3,
      purpose: "\u5BA2\u6236\u73FE\u5834\u62DC\u8A2A\u8207\u9700\u6C42\u78BA\u8A8D",
      projectCode: "P2024-001"
    }
  }
];
var ApprovalTaskService = class _ApprovalTaskService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_TASKS);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id, applicationType) {
    return of(this.items$.getValue().find((t) => t.id === id && (!applicationType || t.applicationType === applicationType)));
  }
  review(id, applicationType, status, reviewNote) {
    const updated = this.items$.getValue().map((t) => t.id === id && t.applicationType === applicationType ? __spreadProps(__spreadValues({}, t), { status, reviewNote, reviewedAt: /* @__PURE__ */ new Date() }) : t);
    this.items$.next(updated);
    return of(updated.find((t) => t.id === id && t.applicationType === applicationType));
  }
  static \u0275fac = function ApprovalTaskService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ApprovalTaskService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _ApprovalTaskService, factory: _ApprovalTaskService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ApprovalTaskService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/approval-tasks/models/approval-task.model.ts
var TASK_STATUS_LABELS = {
  pending: "\u5F85\u5BE9\u6838",
  approved: "\u5DF2\u6838\u51C6",
  rejected: "\u5DF2\u9000\u56DE"
};
var TASK_STATUS_CLASSES = {
  pending: "bg-warning-subtle text-warning-emphasis",
  approved: "bg-success-subtle text-success",
  rejected: "bg-danger-subtle text-danger"
};
var PAYMENT_TYPE_LABELS2 = {
  vendor: "\u5EE0\u5546\u8ACB\u6B3E",
  travel: "\u54E1\u5DE5\u5DEE\u65C5",
  advance: "\u54E1\u5DE5\u9810\u652F"
};
var LEAVE_TYPE_LABELS = {
  annual: "\u5E74\u5047",
  personal: "\u4E8B\u5047",
  sick: "\u75C5\u5047",
  compensatory: "\u88DC\u4F11"
};

// src/app/features/admin/approval-tasks/pages/approval-task-list/approval-task-list.ts
var _c09 = (a0, a1) => [a0, a1, "review"];
var _forTrack015 = ($index, $item) => $item.applicationType + $item.id;
function ApprovalTaskList_For_26_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td")(2, "span");
    \u0275\u0275text(3);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(4, "td", 13);
    \u0275\u0275text(5);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "td", 14);
    \u0275\u0275text(7);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "td")(9, "span");
    \u0275\u0275text(10);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(11, "td", 15);
    \u0275\u0275text(12);
    \u0275\u0275pipe(13, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(14, "td", 16)(15, "a", 17);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(16, "svg", 18);
    \u0275\u0275element(17, "use");
    \u0275\u0275elementEnd()()()();
  }
  if (rf & 2) {
    const t_r1 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275classMap("badge " + ctx_r1.appTypeClass[t_r1.applicationType]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.appTypeLabel[t_r1.applicationType], " ");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(t_r1.submittedBy);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(ctx_r1.getSummary(t_r1));
    \u0275\u0275advance(2);
    \u0275\u0275classMap("badge " + ctx_r1.statusClass[t_r1.status]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.statusLabel[t_r1.status]);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(13, 13, t_r1.submittedAt, "yyyy-MM-dd"));
    \u0275\u0275advance(3);
    \u0275\u0275classMap("btn btn-sm d-inline-flex align-items-center waves-effect " + (t_r1.status === "pending" ? "btn-ghost-primary" : "btn-ghost-secondary"));
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction2(16, _c09, t_r1.applicationType, t_r1.id));
    \u0275\u0275advance(2);
    \u0275\u0275attribute("href", t_r1.status === "pending" ? "/assets/icons/sprite.svg#edit" : "/assets/icons/sprite.svg#eye");
  }
}
function ApprovalTaskList_ForEmpty_27_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 19);
    \u0275\u0275text(2, "\u5C1A\u7121\u7C3D\u6838\u4F5C\u696D\u3002");
    \u0275\u0275elementEnd()();
  }
}
var ApprovalTaskList = class _ApprovalTaskList {
  service = inject(ApprovalTaskService);
  tasks$ = this.service.getAll();
  statusLabel = TASK_STATUS_LABELS;
  statusClass = TASK_STATUS_CLASSES;
  appTypeLabel = APPLICATION_TYPE_LABELS;
  appTypeClass = APPLICATION_TYPE_CLASSES;
  payTypeLabel = PAYMENT_TYPE_LABELS2;
  leaveTypeLabel = LEAVE_TYPE_LABELS;
  getSummary(t) {
    if (t.paymentDetail) {
      return `${this.payTypeLabel[t.paymentDetail.paymentType]}\u30FB${t.paymentDetail.projectCode}\uFF08${t.paymentDetail.totalAmount.toLocaleString()} \u5143\uFF09`;
    }
    if (t.leaveDetail) {
      return `${this.leaveTypeLabel[t.leaveDetail.leaveType]}\u30FB${t.leaveDetail.days} \u5929`;
    }
    if (t.travelDetail) {
      return `${t.travelDetail.destination}\uFF08${t.travelDetail.estimatedCost.toLocaleString()} \u5143\uFF09`;
    }
    return "\u2014";
  }
  static \u0275fac = function ApprovalTaskList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ApprovalTaskList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ApprovalTaskList, selectors: [["app-approval-task-list"]], decls: 29, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#check-square"], [1, "mb-0"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "d-none", "d-md-table-cell"], [1, "d-none", "d-lg-table-cell"], [1, "text-end"], [1, "fw-500"], [1, "text-muted", "small", "d-none", "d-md-table-cell"], [1, "text-muted", "small", "d-none", "d-lg-table-cell"], [1, "text-end", 2, "white-space", "nowrap"], [3, "routerLink"], [1, "sa-icon", 2, "stroke", "currentColor"], ["colspan", "6", 1, "text-center", "text-muted", "py-4"]], template: function ApprovalTaskList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(2, "svg", 2);
      \u0275\u0275element(3, "use", 3);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(4, "h4", 4);
      \u0275\u0275text(5, "\u7C3D\u6838\u4F5C\u696D");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(6, "div", 5)(7, "div", 6)(8, "div", 7)(9, "table", 8)(10, "thead", 9)(11, "tr")(12, "th");
      \u0275\u0275text(13, "\u985E\u578B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(14, "th");
      \u0275\u0275text(15, "\u7533\u8ACB\u4EBA");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(16, "th", 10);
      \u0275\u0275text(17, "\u6458\u8981");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(18, "th");
      \u0275\u0275text(19, "\u72C0\u614B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(20, "th", 11);
      \u0275\u0275text(21, "\u7533\u8ACB\u6642\u9593");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(22, "th", 12);
      \u0275\u0275text(23, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(24, "tbody");
      \u0275\u0275repeaterCreate(25, ApprovalTaskList_For_26_Template, 18, 19, "tr", null, _forTrack015, false, ApprovalTaskList_ForEmpty_27_Template, 3, 0, "tr");
      \u0275\u0275pipe(28, "async");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(25);
      \u0275\u0275repeater(\u0275\u0275pipeBind1(28, 1, ctx.tasks$));
    }
  }, dependencies: [RouterLink, AsyncPipe, DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ApprovalTaskList, [{
    type: Component,
    args: [{ selector: "app-approval-task-list", imports: [RouterLink, AsyncPipe, DatePipe, DecimalPipe], template: `<div class="container-fluid py-3">\r
  <div class="d-flex align-items-center gap-2 mb-4">\r
    <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">\r
      <use href="/assets/icons/sprite.svg#check-square"></use>\r
    </svg>\r
    <h4 class="mb-0">\u7C3D\u6838\u4F5C\u696D</h4>\r
  </div>\r
\r
  <div class="card border-0 shadow-sm">\r
    <div class="card-body p-0">\r
      <div class="table-responsive">\r
        <table class="table table-hover mb-0">\r
          <thead class="table-light">\r
            <tr>\r
              <th>\u985E\u578B</th>\r
              <th>\u7533\u8ACB\u4EBA</th>\r
              <th class="d-none d-md-table-cell">\u6458\u8981</th>\r
              <th>\u72C0\u614B</th>\r
              <th class="d-none d-lg-table-cell">\u7533\u8ACB\u6642\u9593</th>\r
              <th class="text-end">\u64CD\u4F5C</th>\r
            </tr>\r
          </thead>\r
          <tbody>\r
            @for (t of tasks$ | async; track t.applicationType + t.id) {\r
              <tr>\r
                <td>\r
                  <span [class]="'badge ' + appTypeClass[t.applicationType]">\r
                    {{ appTypeLabel[t.applicationType] }}\r
                  </span>\r
                </td>\r
                <td class="fw-500">{{ t.submittedBy }}</td>\r
                <td class="text-muted small d-none d-md-table-cell">{{ getSummary(t) }}</td>\r
                <td>\r
                  <span [class]="'badge ' + statusClass[t.status]">{{ statusLabel[t.status] }}</span>\r
                </td>\r
                <td class="text-muted small d-none d-lg-table-cell">{{ t.submittedAt | date:'yyyy-MM-dd' }}</td>\r
                <td class="text-end" style="white-space: nowrap">\r
                  <a [routerLink]="[t.applicationType, t.id, 'review']"\r
                     [class]="'btn btn-sm d-inline-flex align-items-center waves-effect ' + (t.status === 'pending' ? 'btn-ghost-primary' : 'btn-ghost-secondary')">\r
                    <svg class="sa-icon" style="stroke: currentColor">\r
                      <use [attr.href]="t.status === 'pending' ? '/assets/icons/sprite.svg#edit' : '/assets/icons/sprite.svg#eye'"></use>\r
                    </svg>\r
                  </a>\r
                </td>\r
              </tr>\r
            } @empty {\r
              <tr>\r
                <td colspan="6" class="text-center text-muted py-4">\u5C1A\u7121\u7C3D\u6838\u4F5C\u696D\u3002</td>\r
              </tr>\r
            }\r
          </tbody>\r
        </table>\r
      </div>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ApprovalTaskList, { className: "ApprovalTaskList", filePath: "src/app/features/admin/approval-tasks/pages/approval-task-list/approval-task-list.ts", lineNumber: 17 });
})();

// src/app/features/admin/approval-tasks/pages/approval-task-review/approval-task-review.ts
var _forTrack016 = ($index, $item) => $item.id;
var _forTrack13 = ($index, $item) => $item.stepOrder;
function ApprovalTaskReview_Conditional_1_Case_14_Conditional_0_For_52_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 22);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 32);
    \u0275\u0275element(3, "use", 24);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "td", 33);
    \u0275\u0275text(6);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "td", 34);
    \u0275\u0275text(8);
    \u0275\u0275pipe(9, "number");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const inv_r1 = ctx.$implicit;
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate1(" ", inv_r1.fileName, " ");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(inv_r1.invoiceNo);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(9, 3, inv_r1.amount, "1.0-0"));
  }
}
function ApprovalTaskReview_Conditional_1_Case_14_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 12)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 15);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u8ACB\u6B3E\u7533\u8ACB\u8CC7\u8A0A ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "div", 17)(7, "div", 18)(8, "div", 19);
    \u0275\u0275text(9, "\u8ACB\u6B3E\u985E\u578B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "span", 20);
    \u0275\u0275text(11);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(12, "div", 18)(13, "div", 19);
    \u0275\u0275text(14, "\u5C08\u6848\u7DE8\u865F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "span", 21);
    \u0275\u0275text(16);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(17, "div", 18)(18, "div", 19);
    \u0275\u0275text(19, "\u7533\u8ACB\u4EBA");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(20, "span", 20);
    \u0275\u0275text(21);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(22, "div", 18)(23, "div", 19);
    \u0275\u0275text(24, "\u7533\u8ACB\u6642\u9593");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(25, "span", 22);
    \u0275\u0275text(26);
    \u0275\u0275pipe(27, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(28, "div", 18)(29, "div", 19);
    \u0275\u0275text(30, "\u7E3D\u91D1\u984D");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(31, "span", 23);
    \u0275\u0275text(32);
    \u0275\u0275pipe(33, "number");
    \u0275\u0275elementEnd()()()()();
    \u0275\u0275elementStart(34, "div", 11)(35, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(36, "svg", 14);
    \u0275\u0275element(37, "use", 24);
    \u0275\u0275elementEnd();
    \u0275\u0275text(38, " \u767C\u7968\u660E\u7D30 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(39, "div", 25)(40, "div", 26)(41, "table", 27)(42, "thead", 28)(43, "tr")(44, "th");
    \u0275\u0275text(45, "\u6A94\u6848\u540D\u7A31");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(46, "th");
    \u0275\u0275text(47, "\u767C\u7968\u865F\u78BC");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(48, "th", 29);
    \u0275\u0275text(49, "\u91D1\u984D");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(50, "tbody");
    \u0275\u0275repeaterCreate(51, ApprovalTaskReview_Conditional_1_Case_14_Conditional_0_For_52_Template, 10, 6, "tr", null, _forTrack016);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(53, "tfoot")(54, "tr", 28)(55, "td", 30);
    \u0275\u0275text(56, "\u5408\u8A08");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(57, "td", 31);
    \u0275\u0275text(58);
    \u0275\u0275pipe(59, "number");
    \u0275\u0275elementEnd()()()()()()();
  }
  if (rf & 2) {
    const d_r2 = ctx;
    const task_r3 = \u0275\u0275nextContext(2);
    const ctx_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(11);
    \u0275\u0275textInterpolate(ctx_r3.payTypeLabel[d_r2.paymentType]);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(d_r2.projectCode);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(task_r3.submittedBy);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(27, 6, task_r3.submittedAt, "yyyy-MM-dd"));
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(33, 9, d_r2.totalAmount, "1.0-0"));
    \u0275\u0275advance(19);
    \u0275\u0275repeater(d_r2.invoices);
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(59, 12, d_r2.totalAmount, "1.0-0"));
  }
}
function ApprovalTaskReview_Conditional_1_Case_14_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275conditionalCreate(0, ApprovalTaskReview_Conditional_1_Case_14_Conditional_0_Template, 60, 15);
  }
  if (rf & 2) {
    let tmp_3_0;
    const task_r3 = \u0275\u0275nextContext();
    \u0275\u0275conditional((tmp_3_0 = task_r3.paymentDetail) ? 0 : -1, tmp_3_0);
  }
}
function ApprovalTaskReview_Conditional_1_Case_15_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 12)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 35);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u8ACB\u5047\u7533\u8ACB\u8CC7\u8A0A ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "div", 36)(7, "div", 18)(8, "div", 19);
    \u0275\u0275text(9, "\u5047\u5225");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "span", 20);
    \u0275\u0275text(11);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(12, "div", 18)(13, "div", 19);
    \u0275\u0275text(14, "\u7533\u8ACB\u4EBA");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "span", 20);
    \u0275\u0275text(16);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(17, "div", 18)(18, "div", 19);
    \u0275\u0275text(19, "\u5929\u6578");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(20, "span", 23);
    \u0275\u0275text(21);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(22, "div", 18)(23, "div", 19);
    \u0275\u0275text(24, "\u958B\u59CB\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(25, "span", 22);
    \u0275\u0275text(26);
    \u0275\u0275pipe(27, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(28, "div", 18)(29, "div", 19);
    \u0275\u0275text(30, "\u7D50\u675F\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(31, "span", 22);
    \u0275\u0275text(32);
    \u0275\u0275pipe(33, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(34, "div", 37)(35, "div", 19);
    \u0275\u0275text(36, "\u8ACB\u5047\u539F\u56E0");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(37, "div", 38);
    \u0275\u0275text(38);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const d_r5 = ctx;
    const task_r3 = \u0275\u0275nextContext(2);
    const ctx_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(11);
    \u0275\u0275textInterpolate(ctx_r3.leaveTypeLabel[d_r5.leaveType]);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(task_r3.submittedBy);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate1("", d_r5.days, " \u5929");
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(27, 6, d_r5.startDate, "yyyy-MM-dd"));
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(33, 9, d_r5.endDate, "yyyy-MM-dd"));
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(d_r5.reason);
  }
}
function ApprovalTaskReview_Conditional_1_Case_15_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275conditionalCreate(0, ApprovalTaskReview_Conditional_1_Case_15_Conditional_0_Template, 39, 12, "div", 12);
  }
  if (rf & 2) {
    let tmp_3_0;
    const task_r3 = \u0275\u0275nextContext();
    \u0275\u0275conditional((tmp_3_0 = task_r3.leaveDetail) ? 0 : -1, tmp_3_0);
  }
}
function ApprovalTaskReview_Conditional_1_Case_16_Conditional_0_Conditional_35_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 18)(1, "div", 19);
    \u0275\u0275text(2, "\u95DC\u806F\u5C08\u6848");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "span", 40);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const d_r6 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate(d_r6.projectCode);
  }
}
function ApprovalTaskReview_Conditional_1_Case_16_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 12)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 39);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u51FA\u5DEE\u7533\u8ACB\u8CC7\u8A0A ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "div", 36)(7, "div", 18)(8, "div", 19);
    \u0275\u0275text(9, "\u51FA\u5DEE\u5730\u9EDE");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "span", 20);
    \u0275\u0275text(11);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(12, "div", 18)(13, "div", 19);
    \u0275\u0275text(14, "\u7533\u8ACB\u4EBA");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "span", 20);
    \u0275\u0275text(16);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(17, "div", 18)(18, "div", 19);
    \u0275\u0275text(19, "\u9810\u4F30\u8CBB\u7528");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(20, "span", 23);
    \u0275\u0275text(21);
    \u0275\u0275pipe(22, "number");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(23, "div", 18)(24, "div", 19);
    \u0275\u0275text(25, "\u51FA\u767C\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(26, "span", 22);
    \u0275\u0275text(27);
    \u0275\u0275pipe(28, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(29, "div", 18)(30, "div", 19);
    \u0275\u0275text(31, "\u8FD4\u56DE\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(32, "span", 22);
    \u0275\u0275text(33);
    \u0275\u0275pipe(34, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(35, ApprovalTaskReview_Conditional_1_Case_16_Conditional_0_Conditional_35_Template, 5, 1, "div", 18);
    \u0275\u0275elementStart(36, "div", 37)(37, "div", 19);
    \u0275\u0275text(38, "\u51FA\u5DEE\u76EE\u7684");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(39, "div", 38);
    \u0275\u0275text(40);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const d_r6 = ctx;
    const task_r3 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(11);
    \u0275\u0275textInterpolate(d_r6.destination);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(task_r3.submittedBy);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate1("", \u0275\u0275pipeBind2(22, 7, d_r6.estimatedCost, "1.0-0"), " \u5143");
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(28, 10, d_r6.startDate, "yyyy-MM-dd"));
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(34, 13, d_r6.endDate, "yyyy-MM-dd"));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(d_r6.projectCode ? 35 : -1);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(d_r6.purpose);
  }
}
function ApprovalTaskReview_Conditional_1_Case_16_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275conditionalCreate(0, ApprovalTaskReview_Conditional_1_Case_16_Conditional_0_Template, 41, 16, "div", 12);
  }
  if (rf & 2) {
    let tmp_3_0;
    const task_r3 = \u0275\u0275nextContext();
    \u0275\u0275conditional((tmp_3_0 = task_r3.travelDetail) ? 0 : -1, tmp_3_0);
  }
}
function ApprovalTaskReview_Conditional_1_Conditional_17_For_10_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 46);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const step_r7 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\uFF08", step_r7.departmentName, "\uFF09");
  }
}
function ApprovalTaskReview_Conditional_1_Conditional_17_For_10_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const step_r7 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(step_r7.note);
  }
}
function ApprovalTaskReview_Conditional_1_Conditional_17_For_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "li", 44)(1, "span", 45);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div")(4, "div", 20);
    \u0275\u0275text(5);
    \u0275\u0275conditionalCreate(6, ApprovalTaskReview_Conditional_1_Conditional_17_For_10_Conditional_6_Template, 2, 1, "span", 46);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(7, ApprovalTaskReview_Conditional_1_Conditional_17_For_10_Conditional_7_Template, 2, 1, "div", 22);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const step_r7 = ctx.$implicit;
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", step_r7.stepOrder, " ");
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" ", step_r7.jobTitleName || "\u2014", " ");
    \u0275\u0275advance();
    \u0275\u0275conditional(step_r7.departmentName ? 6 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(step_r7.note ? 7 : -1);
  }
}
function ApprovalTaskReview_Conditional_1_Conditional_17_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 11)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 41);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u7C3D\u6838\u6D41\u7A0B\uFF08\u53C3\u8003\uFF09 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "p", 42);
    \u0275\u0275text(7, "\u6B64\u6D41\u7A0B\u70BA\u53C3\u8003\u8CC7\u8A0A\uFF0C\u8AAA\u660E\u54EA\u4E9B\u90E8\u9580\uFF0F\u8077\u7A31\u61C9\u8A72\u5BE9\u6838\u6B64\u985E\u7533\u8ACB\u3002");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "ol", 43);
    \u0275\u0275repeaterCreate(9, ApprovalTaskReview_Conditional_1_Conditional_17_For_10_Template, 8, 4, "li", 44, _forTrack13);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const task_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(9);
    \u0275\u0275repeater(task_r3.flow.steps);
  }
}
function ApprovalTaskReview_Conditional_1_Conditional_18_Conditional_24_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 50);
    \u0275\u0275text(1, "*");
    \u0275\u0275elementEnd();
  }
}
function ApprovalTaskReview_Conditional_1_Conditional_18_Conditional_26_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 58);
    \u0275\u0275text(1, "\u9000\u56DE\u6642\u8ACB\u586B\u5BEB\u539F\u56E0\u3002");
    \u0275\u0275elementEnd();
  }
}
function ApprovalTaskReview_Conditional_1_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    const _r8 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 11)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 7);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u5BE9\u6838 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "form", 47);
    \u0275\u0275listener("ngSubmit", function ApprovalTaskReview_Conditional_1_Conditional_18_Template_form_ngSubmit_6_listener() {
      \u0275\u0275restoreView(_r8);
      const ctx_r3 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r3.submit());
    });
    \u0275\u0275elementStart(7, "div", 48)(8, "label", 49);
    \u0275\u0275text(9, "\u5BE9\u6838\u7D50\u679C ");
    \u0275\u0275elementStart(10, "span", 50);
    \u0275\u0275text(11, "*");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(12, "div", 51)(13, "div", 52);
    \u0275\u0275element(14, "input", 53);
    \u0275\u0275elementStart(15, "label", 54);
    \u0275\u0275text(16, "\u6838\u51C6");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(17, "div", 52);
    \u0275\u0275element(18, "input", 55);
    \u0275\u0275elementStart(19, "label", 56);
    \u0275\u0275text(20, "\u9000\u56DE");
    \u0275\u0275elementEnd()()()();
    \u0275\u0275elementStart(21, "div", 48)(22, "label", 49);
    \u0275\u0275text(23, " \u5BE9\u6838\u610F\u898B ");
    \u0275\u0275conditionalCreate(24, ApprovalTaskReview_Conditional_1_Conditional_18_Conditional_24_Template, 2, 0, "span", 50);
    \u0275\u0275elementEnd();
    \u0275\u0275element(25, "textarea", 57);
    \u0275\u0275conditionalCreate(26, ApprovalTaskReview_Conditional_1_Conditional_18_Conditional_26_Template, 2, 0, "div", 58);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(27, "div", 59)(28, "button", 60);
    \u0275\u0275text(29, "\u63D0\u4EA4\u5BE9\u6838");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(30, "a", 61);
    \u0275\u0275text(31, "\u53D6\u6D88");
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    let tmp_4_0;
    const ctx_r3 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(6);
    \u0275\u0275property("formGroup", ctx_r3.form);
    \u0275\u0275advance(18);
    \u0275\u0275conditional(((tmp_4_0 = ctx_r3.form.get("action")) == null ? null : tmp_4_0.value) === "rejected" ? 24 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r3.showNoteError ? 26 : -1);
  }
}
function ApprovalTaskReview_Conditional_1_Conditional_19_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 37)(1, "div", 19);
    \u0275\u0275text(2, "\u5BE9\u6838\u610F\u898B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 38);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const task_r3 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate(task_r3.reviewNote);
  }
}
function ApprovalTaskReview_Conditional_1_Conditional_19_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 11)(1, "div", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 14);
    \u0275\u0275element(3, "use", 7);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u5BE9\u6838\u7D50\u679C ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 16)(6, "div", 36)(7, "div", 18)(8, "div", 19);
    \u0275\u0275text(9, "\u5BE9\u6838\u7D50\u679C");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "span");
    \u0275\u0275text(11);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(12, "div", 18)(13, "div", 19);
    \u0275\u0275text(14, "\u5BE9\u6838\u6642\u9593");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "span", 22);
    \u0275\u0275text(16);
    \u0275\u0275pipe(17, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(18, ApprovalTaskReview_Conditional_1_Conditional_19_Conditional_18_Template, 5, 1, "div", 37);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "div", 62)(20, "a", 61);
    \u0275\u0275text(21, "\u8FD4\u56DE\u5217\u8868");
    \u0275\u0275elementEnd()()()();
  }
  if (rf & 2) {
    const task_r3 = \u0275\u0275nextContext();
    const ctx_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(10);
    \u0275\u0275classMap("badge px-3 py-2 rounded-pill " + ctx_r3.statusClass[task_r3.status]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r3.statusLabel[task_r3.status], " ");
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(17, 5, task_r3.reviewedAt, "yyyy-MM-dd"));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(task_r3.reviewNote ? 18 : -1);
  }
}
function ApprovalTaskReview_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 2)(1, "a", 3);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 4);
    \u0275\u0275element(3, "use", 5);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(4, "svg", 6);
    \u0275\u0275element(5, "use", 7);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(6, "h4", 8);
    \u0275\u0275text(7, "\u7C3D\u6838\u5BE9\u6838");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "span");
    \u0275\u0275text(9);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "span");
    \u0275\u0275text(11);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(12, "div", 9)(13, "div", 10);
    \u0275\u0275conditionalCreate(14, ApprovalTaskReview_Conditional_1_Case_14_Template, 1, 1)(15, ApprovalTaskReview_Conditional_1_Case_15_Template, 1, 1)(16, ApprovalTaskReview_Conditional_1_Case_16_Template, 1, 1);
    \u0275\u0275conditionalCreate(17, ApprovalTaskReview_Conditional_1_Conditional_17_Template, 11, 0, "div", 11);
    \u0275\u0275conditionalCreate(18, ApprovalTaskReview_Conditional_1_Conditional_18_Template, 32, 3, "div", 11)(19, ApprovalTaskReview_Conditional_1_Conditional_19_Template, 22, 8, "div", 11);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    let tmp_6_0;
    const task_r3 = ctx;
    const ctx_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(8);
    \u0275\u0275classMap("badge " + ctx_r3.appTypeClass[task_r3.applicationType]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r3.appTypeLabel[task_r3.applicationType]);
    \u0275\u0275advance();
    \u0275\u0275classMap("badge " + ctx_r3.statusClass[task_r3.status]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r3.statusLabel[task_r3.status]);
    \u0275\u0275advance(3);
    \u0275\u0275conditional((tmp_6_0 = task_r3.applicationType) === "payment_request" ? 14 : tmp_6_0 === "leave" ? 15 : tmp_6_0 === "travel" ? 16 : -1);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(task_r3.flow ? 17 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(task_r3.status === "pending" ? 18 : 19);
  }
}
function ApprovalTaskReview_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "p", 1);
    \u0275\u0275text(1, "\u627E\u4E0D\u5230\u6B64\u7C3D\u6838\u4F5C\u696D\u3002");
    \u0275\u0275elementEnd();
  }
}
var ApprovalTaskReview = class _ApprovalTaskReview {
  service = inject(ApprovalTaskService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  fb = inject(FormBuilder);
  task$;
  taskId = 0;
  applicationType = "";
  showNoteError = false;
  statusLabel = TASK_STATUS_LABELS;
  statusClass = TASK_STATUS_CLASSES;
  appTypeLabel = APPLICATION_TYPE_LABELS;
  appTypeClass = APPLICATION_TYPE_CLASSES;
  payTypeLabel = PAYMENT_TYPE_LABELS2;
  leaveTypeLabel = LEAVE_TYPE_LABELS;
  form = this.fb.group({
    action: ["approved", Validators.required],
    reviewNote: [""]
  });
  ngOnInit() {
    this.applicationType = this.route.snapshot.paramMap.get("applicationType") ?? "";
    this.taskId = +this.route.snapshot.paramMap.get("id");
    this.task$ = this.service.getById(this.taskId, this.applicationType);
  }
  submit() {
    const action = this.form.value.action;
    const note = this.form.value.reviewNote?.trim() ?? "";
    if (action === "rejected" && !note) {
      this.showNoteError = true;
      return;
    }
    this.showNoteError = false;
    this.service.review(this.taskId, this.applicationType, action, note).subscribe(() => {
      this.router.navigate(["/admin/approval-tasks"]);
    });
  }
  static \u0275fac = function ApprovalTaskReview_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ApprovalTaskReview)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _ApprovalTaskReview, selectors: [["app-approval-task-review"]], decls: 4, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "text-muted"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], ["routerLink", "/admin/approval-tasks", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#check-square"], [1, "mb-0"], [1, "row", "g-4"], [1, "col-12", "col-lg-10", "col-xl-8"], [1, "card", "border-0", "shadow-sm", "mt-4"], [1, "card", "border-0", "shadow-sm"], [1, "card-header", "bg-transparent", "border-bottom", "d-flex", "align-items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#dollar-sign"], [1, "card-body"], [1, "row", "g-3", "mb-0"], [1, "col-6", "col-md-4"], [1, "text-muted", "small", "mb-1"], [1, "fw-500"], [1, "fw-500", "font-monospace"], [1, "text-muted", "small"], [1, "fw-600", "fs-5"], ["href", "/assets/icons/sprite.svg#file-text"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-sm", "mb-0"], [1, "table-light"], [1, "text-end"], ["colspan", "2", 1, "text-end", "fw-500", "small"], [1, "text-end", "fw-600"], [1, "sa-icon", "me-1", 2, "width", "14px", "height", "14px", "stroke", "currentColor"], [1, "font-monospace", "small"], [1, "text-end", "fw-500"], ["href", "/assets/icons/sprite.svg#calendar"], [1, "row", "g-3"], [1, "col-12"], [1, "border", "rounded", "p-3", "bg-body-secondary", "small"], ["href", "/assets/icons/sprite.svg#map-pin"], [1, "font-monospace", "fw-500"], ["href", "/assets/icons/sprite.svg#git-merge"], [1, "text-muted", "small", "mb-3"], [1, "list-unstyled", "mb-0"], [1, "d-flex", "align-items-start", "gap-3", "mb-3"], [1, "badge", "bg-primary-subtle", "text-primary", "rounded-circle", "d-flex", "align-items-center", "justify-content-center", 2, "width", "28px", "height", "28px", "min-width", "28px", "font-size", ".75rem"], [1, "text-muted", "fw-400"], [3, "ngSubmit", "formGroup"], [1, "mb-3"], [1, "form-label", "fw-500"], [1, "text-danger"], [1, "d-flex", "gap-4", "mt-1"], [1, "form-check"], ["type", "radio", "formControlName", "action", "value", "approved", "id", "actionApprove", 1, "form-check-input"], ["for", "actionApprove", 1, "form-check-label", "fw-500", "text-success"], ["type", "radio", "formControlName", "action", "value", "rejected", "id", "actionReject", 1, "form-check-input"], ["for", "actionReject", 1, "form-check-label", "fw-500", "text-danger"], ["formControlName", "reviewNote", "rows", "3", "placeholder", "\u586B\u5BEB\u5BE9\u6838\u610F\u898B\uFF08\u9000\u56DE\u6642\u5FC5\u586B\uFF09...", 1, "form-control"], [1, "text-danger", "small", "mt-1"], [1, "d-flex", "gap-2"], ["type", "submit", 1, "btn", "btn-primary", "waves-effect", "waves-light"], ["routerLink", "/admin/approval-tasks", 1, "btn", "btn-outline-secondary", "waves-effect"], [1, "mt-3"]], template: function ApprovalTaskReview_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0);
      \u0275\u0275conditionalCreate(1, ApprovalTaskReview_Conditional_1_Template, 20, 9);
      \u0275\u0275pipe(2, "async");
      \u0275\u0275conditionalBranchCreate(3, ApprovalTaskReview_Conditional_3_Template, 2, 0, "p", 1);
      \u0275\u0275elementEnd();
    }
    if (rf & 2) {
      let tmp_0_0;
      \u0275\u0275advance();
      \u0275\u0275conditional((tmp_0_0 = \u0275\u0275pipeBind1(2, 1, ctx.task$)) ? 1 : 3, tmp_0_0);
    }
  }, dependencies: [RouterLink, ReactiveFormsModule, \u0275NgNoValidate, DefaultValueAccessor, RadioControlValueAccessor, NgControlStatus, NgControlStatusGroup, FormGroupDirective, FormControlName, AsyncPipe, DatePipe, DecimalPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ApprovalTaskReview, [{
    type: Component,
    args: [{ selector: "app-approval-task-review", imports: [RouterLink, ReactiveFormsModule, AsyncPipe, DatePipe, DecimalPipe], template: `<div class="container-fluid py-3">\r
  @if (task$ | async; as task) {\r
\r
    <!-- Page Header -->\r
    <div class="d-flex align-items-center gap-2 mb-4">\r
      <a routerLink="/admin/approval-tasks" class="btn btn-sm btn-outline-secondary waves-effect">\r
        <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>\r
      </a>\r
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">\r
        <use href="/assets/icons/sprite.svg#check-square"></use>\r
      </svg>\r
      <h4 class="mb-0">\u7C3D\u6838\u5BE9\u6838</h4>\r
      <span [class]="'badge ' + appTypeClass[task.applicationType]">{{ appTypeLabel[task.applicationType] }}</span>\r
      <span [class]="'badge ' + statusClass[task.status]">{{ statusLabel[task.status] }}</span>\r
    </div>\r
\r
    <div class="row g-4">\r
      <div class="col-12 col-lg-10 col-xl-8">\r
\r
        <!-- \u2500\u2500 \u7533\u8ACB\u8CC7\u8A0A\uFF08\u4F9D\u985E\u578B\u5207\u63DB\uFF09\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500 -->\r
        @switch (task.applicationType) {\r
\r
          @case ('payment_request') {\r
            @if (task.paymentDetail; as d) {\r
              <div class="card border-0 shadow-sm">\r
                <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">\r
                  <svg class="sa-icon text-primary" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#dollar-sign"></use></svg>\r
                  \u8ACB\u6B3E\u7533\u8ACB\u8CC7\u8A0A\r
                </div>\r
                <div class="card-body">\r
                  <div class="row g-3 mb-0">\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u8ACB\u6B3E\u985E\u578B</div>\r
                      <span class="fw-500">{{ payTypeLabel[d.paymentType] }}</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u5C08\u6848\u7DE8\u865F</div>\r
                      <span class="fw-500 font-monospace">{{ d.projectCode }}</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u7533\u8ACB\u4EBA</div>\r
                      <span class="fw-500">{{ task.submittedBy }}</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u7533\u8ACB\u6642\u9593</div>\r
                      <span class="text-muted small">{{ task.submittedAt | date:'yyyy-MM-dd' }}</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u7E3D\u91D1\u984D</div>\r
                      <span class="fw-600 fs-5">{{ d.totalAmount | number:'1.0-0' }}</span>\r
                    </div>\r
                  </div>\r
                </div>\r
              </div>\r
\r
              <!-- \u767C\u7968\u660E\u7D30 -->\r
              <div class="card border-0 shadow-sm mt-4">\r
                <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">\r
                  <svg class="sa-icon text-primary" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#file-text"></use></svg>\r
                  \u767C\u7968\u660E\u7D30\r
                </div>\r
                <div class="card-body p-0">\r
                  <div class="table-responsive">\r
                    <table class="table table-sm mb-0">\r
                      <thead class="table-light">\r
                        <tr>\r
                          <th>\u6A94\u6848\u540D\u7A31</th>\r
                          <th>\u767C\u7968\u865F\u78BC</th>\r
                          <th class="text-end">\u91D1\u984D</th>\r
                        </tr>\r
                      </thead>\r
                      <tbody>\r
                        @for (inv of d.invoices; track inv.id) {\r
                          <tr>\r
                            <td class="text-muted small">\r
                              <svg class="sa-icon me-1" style="width:14px;height:14px;stroke:currentColor">\r
                                <use href="/assets/icons/sprite.svg#file-text"></use>\r
                              </svg>\r
                              {{ inv.fileName }}\r
                            </td>\r
                            <td class="font-monospace small">{{ inv.invoiceNo }}</td>\r
                            <td class="text-end fw-500">{{ inv.amount | number:'1.0-0' }}</td>\r
                          </tr>\r
                        }\r
                      </tbody>\r
                      <tfoot>\r
                        <tr class="table-light">\r
                          <td colspan="2" class="text-end fw-500 small">\u5408\u8A08</td>\r
                          <td class="text-end fw-600">{{ d.totalAmount | number:'1.0-0' }}</td>\r
                        </tr>\r
                      </tfoot>\r
                    </table>\r
                  </div>\r
                </div>\r
              </div>\r
            }\r
          }\r
\r
          @case ('leave') {\r
            @if (task.leaveDetail; as d) {\r
              <div class="card border-0 shadow-sm">\r
                <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">\r
                  <svg class="sa-icon text-primary" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#calendar"></use></svg>\r
                  \u8ACB\u5047\u7533\u8ACB\u8CC7\u8A0A\r
                </div>\r
                <div class="card-body">\r
                  <div class="row g-3">\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u5047\u5225</div>\r
                      <span class="fw-500">{{ leaveTypeLabel[d.leaveType] }}</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u7533\u8ACB\u4EBA</div>\r
                      <span class="fw-500">{{ task.submittedBy }}</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u5929\u6578</div>\r
                      <span class="fw-600 fs-5">{{ d.days }} \u5929</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u958B\u59CB\u65E5\u671F</div>\r
                      <span class="text-muted small">{{ d.startDate | date:'yyyy-MM-dd' }}</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u7D50\u675F\u65E5\u671F</div>\r
                      <span class="text-muted small">{{ d.endDate | date:'yyyy-MM-dd' }}</span>\r
                    </div>\r
                    <div class="col-12">\r
                      <div class="text-muted small mb-1">\u8ACB\u5047\u539F\u56E0</div>\r
                      <div class="border rounded p-3 bg-body-secondary small">{{ d.reason }}</div>\r
                    </div>\r
                  </div>\r
                </div>\r
              </div>\r
            }\r
          }\r
\r
          @case ('travel') {\r
            @if (task.travelDetail; as d) {\r
              <div class="card border-0 shadow-sm">\r
                <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">\r
                  <svg class="sa-icon text-primary" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#map-pin"></use></svg>\r
                  \u51FA\u5DEE\u7533\u8ACB\u8CC7\u8A0A\r
                </div>\r
                <div class="card-body">\r
                  <div class="row g-3">\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u51FA\u5DEE\u5730\u9EDE</div>\r
                      <span class="fw-500">{{ d.destination }}</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u7533\u8ACB\u4EBA</div>\r
                      <span class="fw-500">{{ task.submittedBy }}</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u9810\u4F30\u8CBB\u7528</div>\r
                      <span class="fw-600 fs-5">{{ d.estimatedCost | number:'1.0-0' }} \u5143</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u51FA\u767C\u65E5\u671F</div>\r
                      <span class="text-muted small">{{ d.startDate | date:'yyyy-MM-dd' }}</span>\r
                    </div>\r
                    <div class="col-6 col-md-4">\r
                      <div class="text-muted small mb-1">\u8FD4\u56DE\u65E5\u671F</div>\r
                      <span class="text-muted small">{{ d.endDate | date:'yyyy-MM-dd' }}</span>\r
                    </div>\r
                    @if (d.projectCode) {\r
                      <div class="col-6 col-md-4">\r
                        <div class="text-muted small mb-1">\u95DC\u806F\u5C08\u6848</div>\r
                        <span class="font-monospace fw-500">{{ d.projectCode }}</span>\r
                      </div>\r
                    }\r
                    <div class="col-12">\r
                      <div class="text-muted small mb-1">\u51FA\u5DEE\u76EE\u7684</div>\r
                      <div class="border rounded p-3 bg-body-secondary small">{{ d.purpose }}</div>\r
                    </div>\r
                  </div>\r
                </div>\r
              </div>\r
            }\r
          }\r
\r
        }\r
\r
        <!-- \u2500\u2500 \u7C3D\u6838\u6D41\u7A0B\uFF08\u53C3\u8003\uFF09\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500 -->\r
        @if (task.flow) {\r
          <div class="card border-0 shadow-sm mt-4">\r
            <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">\r
              <svg class="sa-icon text-primary" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#git-merge"></use></svg>\r
              \u7C3D\u6838\u6D41\u7A0B\uFF08\u53C3\u8003\uFF09\r
            </div>\r
            <div class="card-body">\r
              <p class="text-muted small mb-3">\u6B64\u6D41\u7A0B\u70BA\u53C3\u8003\u8CC7\u8A0A\uFF0C\u8AAA\u660E\u54EA\u4E9B\u90E8\u9580\uFF0F\u8077\u7A31\u61C9\u8A72\u5BE9\u6838\u6B64\u985E\u7533\u8ACB\u3002</p>\r
              <ol class="list-unstyled mb-0">\r
                @for (step of task.flow.steps; track step.stepOrder) {\r
                  <li class="d-flex align-items-start gap-3 mb-3">\r
                    <span class="badge bg-primary-subtle text-primary rounded-circle d-flex align-items-center justify-content-center"\r
                          style="width:28px;height:28px;min-width:28px;font-size:.75rem">\r
                      {{ step.stepOrder }}\r
                    </span>\r
                    <div>\r
                      <div class="fw-500">\r
                        {{ step.jobTitleName || '\u2014' }}\r
                        @if (step.departmentName) {\r
                          <span class="text-muted fw-400">\uFF08{{ step.departmentName }}\uFF09</span>\r
                        }\r
                      </div>\r
                      @if (step.note) {\r
                        <div class="text-muted small">{{ step.note }}</div>\r
                      }\r
                    </div>\r
                  </li>\r
                }\r
              </ol>\r
            </div>\r
          </div>\r
        }\r
\r
        <!-- \u2500\u2500 \u5BE9\u6838\u5340\u584A \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500 -->\r
        @if (task.status === 'pending') {\r
          <div class="card border-0 shadow-sm mt-4">\r
            <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">\r
              <svg class="sa-icon text-primary" style="stroke: currentColor">\r
                <use href="/assets/icons/sprite.svg#check-square"></use>\r
              </svg>\r
              \u5BE9\u6838\r
            </div>\r
            <div class="card-body">\r
              <form [formGroup]="form" (ngSubmit)="submit()">\r
                <div class="mb-3">\r
                  <label class="form-label fw-500">\u5BE9\u6838\u7D50\u679C <span class="text-danger">*</span></label>\r
                  <div class="d-flex gap-4 mt-1">\r
                    <div class="form-check">\r
                      <input class="form-check-input" type="radio" formControlName="action" value="approved" id="actionApprove">\r
                      <label class="form-check-label fw-500 text-success" for="actionApprove">\u6838\u51C6</label>\r
                    </div>\r
                    <div class="form-check">\r
                      <input class="form-check-input" type="radio" formControlName="action" value="rejected" id="actionReject">\r
                      <label class="form-check-label fw-500 text-danger" for="actionReject">\u9000\u56DE</label>\r
                    </div>\r
                  </div>\r
                </div>\r
                <div class="mb-3">\r
                  <label class="form-label fw-500">\r
                    \u5BE9\u6838\u610F\u898B\r
                    @if (form.get('action')?.value === 'rejected') {\r
                      <span class="text-danger">*</span>\r
                    }\r
                  </label>\r
                  <textarea class="form-control" formControlName="reviewNote" rows="3"\r
                            placeholder="\u586B\u5BEB\u5BE9\u6838\u610F\u898B\uFF08\u9000\u56DE\u6642\u5FC5\u586B\uFF09..."></textarea>\r
                  @if (showNoteError) {\r
                    <div class="text-danger small mt-1">\u9000\u56DE\u6642\u8ACB\u586B\u5BEB\u539F\u56E0\u3002</div>\r
                  }\r
                </div>\r
                <div class="d-flex gap-2">\r
                  <button type="submit" class="btn btn-primary waves-effect waves-light">\u63D0\u4EA4\u5BE9\u6838</button>\r
                  <a routerLink="/admin/approval-tasks" class="btn btn-outline-secondary waves-effect">\u53D6\u6D88</a>\r
                </div>\r
              </form>\r
            </div>\r
          </div>\r
        } @else {\r
          <!-- \u5DF2\u5BE9\u6838\u7D50\u679C -->\r
          <div class="card border-0 shadow-sm mt-4">\r
            <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">\r
              <svg class="sa-icon text-primary" style="stroke: currentColor">\r
                <use href="/assets/icons/sprite.svg#check-square"></use>\r
              </svg>\r
              \u5BE9\u6838\u7D50\u679C\r
            </div>\r
            <div class="card-body">\r
              <div class="row g-3">\r
                <div class="col-6 col-md-4">\r
                  <div class="text-muted small mb-1">\u5BE9\u6838\u7D50\u679C</div>\r
                  <span [class]="'badge px-3 py-2 rounded-pill ' + statusClass[task.status]">\r
                    {{ statusLabel[task.status] }}\r
                  </span>\r
                </div>\r
                <div class="col-6 col-md-4">\r
                  <div class="text-muted small mb-1">\u5BE9\u6838\u6642\u9593</div>\r
                  <span class="text-muted small">{{ task.reviewedAt | date:'yyyy-MM-dd' }}</span>\r
                </div>\r
                @if (task.reviewNote) {\r
                  <div class="col-12">\r
                    <div class="text-muted small mb-1">\u5BE9\u6838\u610F\u898B</div>\r
                    <div class="border rounded p-3 bg-body-secondary small">{{ task.reviewNote }}</div>\r
                  </div>\r
                }\r
              </div>\r
              <div class="mt-3">\r
                <a routerLink="/admin/approval-tasks" class="btn btn-outline-secondary waves-effect">\u8FD4\u56DE\u5217\u8868</a>\r
              </div>\r
            </div>\r
          </div>\r
        }\r
\r
      </div>\r
    </div>\r
\r
  } @else {\r
    <p class="text-muted">\u627E\u4E0D\u5230\u6B64\u7C3D\u6838\u4F5C\u696D\u3002</p>\r
  }\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(ApprovalTaskReview, { className: "ApprovalTaskReview", filePath: "src/app/features/admin/approval-tasks/pages/approval-task-review/approval-task-review.ts", lineNumber: 19 });
})();

// src/app/features/admin/leave-requests/services/leave-request.service.ts
var MOCK_REQUESTS2 = [
  {
    id: 1,
    leaveType: "annual",
    startDate: /* @__PURE__ */ new Date("2026-03-01"),
    endDate: /* @__PURE__ */ new Date("2026-03-05"),
    days: 5,
    reason: "\u500B\u4EBA\u65C5\u904A",
    approvalStatus: "pending",
    createdAt: /* @__PURE__ */ new Date("2026-02-10")
  },
  {
    id: 2,
    leaveType: "sick",
    startDate: /* @__PURE__ */ new Date("2026-02-20"),
    endDate: /* @__PURE__ */ new Date("2026-02-21"),
    days: 2,
    reason: "\u8EAB\u9AD4\u4E0D\u9069\u5C31\u91AB",
    approvalStatus: "approved",
    createdAt: /* @__PURE__ */ new Date("2026-02-20"),
    reviewedAt: /* @__PURE__ */ new Date("2026-02-20"),
    reviewNote: "\u6838\u51C6"
  },
  {
    id: 3,
    leaveType: "compensatory",
    startDate: /* @__PURE__ */ new Date("2026-02-15"),
    endDate: /* @__PURE__ */ new Date("2026-02-15"),
    days: 1,
    reason: "\u88DC\u4F11\u52A0\u73ED\u6642\u6578",
    approvalStatus: "rejected",
    createdAt: /* @__PURE__ */ new Date("2026-02-13"),
    reviewedAt: /* @__PURE__ */ new Date("2026-02-14"),
    reviewNote: "\u88DC\u4F11\u6642\u6578\u4E0D\u8DB3\uFF0C\u8ACB\u78BA\u8A8D\u5F8C\u91CD\u65B0\u7533\u8ACB"
  }
];
var LeaveRequestService = class _LeaveRequestService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_REQUESTS2);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id) {
    return of(this.items$.getValue().find((r) => r.id === id));
  }
  create(data) {
    const item = __spreadProps(__spreadValues({}, data), { id: Date.now(), approvalStatus: "pending", createdAt: /* @__PURE__ */ new Date() });
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }
  update(id, changes) {
    const updated = this.items$.getValue().map((r) => r.id === id ? __spreadValues(__spreadValues({}, r), changes) : r);
    this.items$.next(updated);
    return of(updated.find((r) => r.id === id));
  }
  delete(id) {
    this.items$.next(this.items$.getValue().filter((r) => r.id !== id));
    return of(void 0);
  }
  static \u0275fac = function LeaveRequestService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _LeaveRequestService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _LeaveRequestService, factory: _LeaveRequestService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(LeaveRequestService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/leave-requests/models/leave-request.model.ts
var LEAVE_TYPE_LABELS2 = {
  annual: "\u5E74\u5047",
  personal: "\u4E8B\u5047",
  sick: "\u75C5\u5047",
  compensatory: "\u88DC\u4F11"
};
var LEAVE_TYPE_CLASSES = {
  annual: "bg-primary-subtle text-primary",
  personal: "bg-info-subtle text-info",
  sick: "bg-warning-subtle text-warning-emphasis",
  compensatory: "bg-secondary-subtle text-secondary"
};
var APPROVAL_STATUS_LABELS2 = {
  pending: "\u5F85\u5BE9\u6838",
  approved: "\u5DF2\u6838\u51C6",
  rejected: "\u5DF2\u9000\u56DE"
};
var APPROVAL_STATUS_CLASSES2 = {
  pending: "bg-warning-subtle text-warning-emphasis",
  approved: "bg-success-subtle text-success",
  rejected: "bg-danger-subtle text-danger"
};

// src/app/features/admin/leave-requests/pages/leave-request-list/leave-request-list.ts
var _c010 = (a0) => [a0, "edit"];
var _forTrack017 = ($index, $item) => $item.id;
function LeaveRequestList_For_35_Conditional_23_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "a", 24);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 7);
    \u0275\u0275element(2, "use", 25);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(3, "button", 26);
    \u0275\u0275listener("click", function LeaveRequestList_For_35_Conditional_23_Template_button_click_3_listener() {
      \u0275\u0275restoreView(_r1);
      const r_r2 = \u0275\u0275nextContext().$implicit;
      const ctx_r2 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r2.delete(r_r2));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(4, "svg", 7);
    \u0275\u0275element(5, "use", 27);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const r_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(1, _c010, r_r2.id));
  }
}
function LeaveRequestList_For_35_Conditional_24_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "a", 23);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 7);
    \u0275\u0275element(2, "use", 28);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const r_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(1, _c010, r_r2.id));
  }
}
function LeaveRequestList_For_35_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td")(2, "span");
    \u0275\u0275text(3);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(4, "td", 17);
    \u0275\u0275text(5);
    \u0275\u0275pipe(6, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "td", 17);
    \u0275\u0275text(8);
    \u0275\u0275pipe(9, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "td", 18);
    \u0275\u0275text(11);
    \u0275\u0275pipe(12, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "td", 19);
    \u0275\u0275text(14);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "td")(16, "span");
    \u0275\u0275text(17);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(18, "td", 20);
    \u0275\u0275text(19);
    \u0275\u0275pipe(20, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(21, "td", 21)(22, "div", 22);
    \u0275\u0275conditionalCreate(23, LeaveRequestList_For_35_Conditional_23_Template, 6, 3)(24, LeaveRequestList_For_35_Conditional_24_Template, 3, 3, "a", 23);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const r_r2 = ctx.$implicit;
    const ctx_r2 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275classMap("badge " + ctx_r2.typeClass[r_r2.leaveType]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r2.typeLabel[r_r2.leaveType]);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(6, 12, r_r2.startDate, "yyyy-MM-dd"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(9, 15, r_r2.endDate, "yyyy-MM-dd"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1("", \u0275\u0275pipeBind2(12, 18, r_r2.days, "1.0-1"), " \u5929");
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(r_r2.reason);
    \u0275\u0275advance(2);
    \u0275\u0275classMap("badge " + ctx_r2.statusClass[r_r2.approvalStatus]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r2.statusLabel[r_r2.approvalStatus]);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(20, 21, r_r2.createdAt, "yyyy-MM-dd"));
    \u0275\u0275advance(4);
    \u0275\u0275conditional(r_r2.approvalStatus === "pending" ? 23 : 24);
  }
}
function LeaveRequestList_ForEmpty_36_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 29);
    \u0275\u0275text(2, "\u5C1A\u7121\u8ACB\u5047\u7533\u8ACB\u3002");
    \u0275\u0275elementEnd()();
  }
}
var LeaveRequestList = class _LeaveRequestList {
  service = inject(LeaveRequestService);
  requests$ = this.service.getAll();
  typeLabel = LEAVE_TYPE_LABELS2;
  typeClass = LEAVE_TYPE_CLASSES;
  statusLabel = APPROVAL_STATUS_LABELS2;
  statusClass = APPROVAL_STATUS_CLASSES2;
  delete(r) {
    if (confirm(`\u78BA\u5B9A\u8981\u522A\u9664\u6B64\u8ACB\u5047\u7533\u8ACB\u55CE\uFF1F`)) {
      this.service.delete(r.id).subscribe();
    }
  }
  static \u0275fac = function LeaveRequestList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _LeaveRequestList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _LeaveRequestList, selectors: [["app-leave-request-list"]], decls: 38, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#calendar"], [1, "mb-0"], ["routerLink", "new", 1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "text-end"], [1, "d-none", "d-md-table-cell"], [1, "d-none", "d-lg-table-cell"], [1, "text-muted", "small"], [1, "text-end", "fw-500"], [1, "text-muted", "small", "d-none", "d-md-table-cell"], [1, "text-muted", "small", "d-none", "d-lg-table-cell"], [1, "text-end", 2, "white-space", "nowrap"], [1, "d-flex", "justify-content-end", "gap-1"], ["title", "\u6AA2\u8996", 1, "btn", "btn-sm", "btn-ghost-secondary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["title", "\u7DE8\u8F2F", 1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#edit"], ["title", "\u522A\u9664", 1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], ["href", "/assets/icons/sprite.svg#eye"], ["colspan", "8", 1, "text-center", "text-muted", "py-4"]], template: function LeaveRequestList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u8ACB\u5047\u7533\u8ACB");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "a", 6);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(8, "svg", 7);
      \u0275\u0275element(9, "use", 8);
      \u0275\u0275elementEnd();
      \u0275\u0275text(10, " \u65B0\u589E\u7533\u8ACB ");
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(11, "div", 9)(12, "div", 10)(13, "div", 11)(14, "table", 12)(15, "thead", 13)(16, "tr")(17, "th");
      \u0275\u0275text(18, "\u5047\u5225");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "th");
      \u0275\u0275text(20, "\u958B\u59CB\u65E5\u671F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(21, "th");
      \u0275\u0275text(22, "\u7D50\u675F\u65E5\u671F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(23, "th", 14);
      \u0275\u0275text(24, "\u5929\u6578");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(25, "th", 15);
      \u0275\u0275text(26, "\u539F\u56E0");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(27, "th");
      \u0275\u0275text(28, "\u72C0\u614B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(29, "th", 16);
      \u0275\u0275text(30, "\u7533\u8ACB\u6642\u9593");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(31, "th", 14);
      \u0275\u0275text(32, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(33, "tbody");
      \u0275\u0275repeaterCreate(34, LeaveRequestList_For_35_Template, 25, 24, "tr", null, _forTrack017, false, LeaveRequestList_ForEmpty_36_Template, 3, 0, "tr");
      \u0275\u0275pipe(37, "async");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(34);
      \u0275\u0275repeater(\u0275\u0275pipeBind1(37, 1, ctx.requests$));
    }
  }, dependencies: [RouterLink, AsyncPipe, DatePipe, DecimalPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(LeaveRequestList, [{
    type: Component,
    args: [{ selector: "app-leave-request-list", imports: [RouterLink, AsyncPipe, DatePipe, DecimalPipe], template: `<div class="container-fluid py-3">\r
  <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">\r
    <div class="d-flex align-items-center gap-2">\r
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">\r
        <use href="/assets/icons/sprite.svg#calendar"></use>\r
      </svg>\r
      <h4 class="mb-0">\u8ACB\u5047\u7533\u8ACB</h4>\r
    </div>\r
    <a routerLink="new" class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light">\r
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>\r
      \u65B0\u589E\u7533\u8ACB\r
    </a>\r
  </div>\r
\r
  <div class="card border-0 shadow-sm">\r
    <div class="card-body p-0">\r
      <div class="table-responsive">\r
        <table class="table table-hover mb-0">\r
          <thead class="table-light">\r
            <tr>\r
              <th>\u5047\u5225</th>\r
              <th>\u958B\u59CB\u65E5\u671F</th>\r
              <th>\u7D50\u675F\u65E5\u671F</th>\r
              <th class="text-end">\u5929\u6578</th>\r
              <th class="d-none d-md-table-cell">\u539F\u56E0</th>\r
              <th>\u72C0\u614B</th>\r
              <th class="d-none d-lg-table-cell">\u7533\u8ACB\u6642\u9593</th>\r
              <th class="text-end">\u64CD\u4F5C</th>\r
            </tr>\r
          </thead>\r
          <tbody>\r
            @for (r of requests$ | async; track r.id) {\r
              <tr>\r
                <td>\r
                  <span [class]="'badge ' + typeClass[r.leaveType]">{{ typeLabel[r.leaveType] }}</span>\r
                </td>\r
                <td class="text-muted small">{{ r.startDate | date:'yyyy-MM-dd' }}</td>\r
                <td class="text-muted small">{{ r.endDate | date:'yyyy-MM-dd' }}</td>\r
                <td class="text-end fw-500">{{ r.days | number:'1.0-1' }} \u5929</td>\r
                <td class="text-muted small d-none d-md-table-cell">{{ r.reason }}</td>\r
                <td>\r
                  <span [class]="'badge ' + statusClass[r.approvalStatus]">{{ statusLabel[r.approvalStatus] }}</span>\r
                </td>\r
                <td class="text-muted small d-none d-lg-table-cell">{{ r.createdAt | date:'yyyy-MM-dd' }}</td>\r
                <td class="text-end" style="white-space: nowrap">\r
                  <div class="d-flex justify-content-end gap-1">\r
                    @if (r.approvalStatus === 'pending') {\r
                      <a [routerLink]="[r.id, 'edit']" class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect" title="\u7DE8\u8F2F">\r
                        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>\r
                      </a>\r
                      <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect" (click)="delete(r)" title="\u522A\u9664">\r
                        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>\r
                      </button>\r
                    } @else {\r
                      <a [routerLink]="[r.id, 'edit']" class="btn btn-sm btn-ghost-secondary d-inline-flex align-items-center waves-effect" title="\u6AA2\u8996">\r
                        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#eye"></use></svg>\r
                      </a>\r
                    }\r
                  </div>\r
                </td>\r
              </tr>\r
            } @empty {\r
              <tr>\r
                <td colspan="8" class="text-center text-muted py-4">\u5C1A\u7121\u8ACB\u5047\u7533\u8ACB\u3002</td>\r
              </tr>\r
            }\r
          </tbody>\r
        </table>\r
      </div>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(LeaveRequestList, { className: "LeaveRequestList", filePath: "src/app/features/admin/leave-requests/pages/leave-request-list/leave-request-list.ts", lineNumber: 16 });
})();

// src/app/features/admin/leave-requests/pages/leave-request-form/leave-request-form.ts
function LeaveRequestForm_Conditional_45_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 30);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u958B\u59CB\u65E5\u671F\u3002");
    \u0275\u0275elementEnd();
  }
}
function LeaveRequestForm_Conditional_52_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 30);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u7D50\u675F\u65E5\u671F\u3002");
    \u0275\u0275elementEnd();
  }
}
function LeaveRequestForm_Conditional_59_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 30);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u6709\u6548\u5929\u6578\uFF08\u6700\u5C11 0.5\uFF09\u3002");
    \u0275\u0275elementEnd();
  }
}
function LeaveRequestForm_Conditional_66_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 30);
    \u0275\u0275text(1, "\u8ACB\u586B\u5BEB\u8ACB\u5047\u539F\u56E0\u3002");
    \u0275\u0275elementEnd();
  }
}
function LeaveRequestForm_Conditional_67_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 34)(1, "button", 36);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "a", 37);
    \u0275\u0275text(4, "\u53D6\u6D88");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r0.form.invalid);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r0.isEdit ? "\u66F4\u65B0" : "\u9001\u51FA\u7533\u8ACB", " ");
  }
}
function LeaveRequestForm_Conditional_68_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 35)(1, "a", 37);
    \u0275\u0275text(2, "\u8FD4\u56DE\u5217\u8868");
    \u0275\u0275elementEnd()();
  }
}
var LeaveRequestForm = class _LeaveRequestForm {
  fb = inject(FormBuilder);
  service = inject(LeaveRequestService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  isEdit = false;
  requestId = 0;
  isReadOnly = false;
  statusLabel = APPROVAL_STATUS_LABELS2;
  statusClass = APPROVAL_STATUS_CLASSES2;
  form = this.fb.group({
    leaveType: ["annual", Validators.required],
    startDate: ["", Validators.required],
    endDate: ["", Validators.required],
    days: [1, [Validators.required, Validators.min(0.5)]],
    reason: ["", Validators.required]
  });
  ngOnInit() {
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEdit = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe((r) => {
        if (!r)
          return;
        this.isReadOnly = r.approvalStatus !== "pending";
        this.form.patchValue({
          leaveType: r.leaveType,
          startDate: r.startDate instanceof Date ? r.startDate.toISOString().split("T")[0] : String(r.startDate),
          endDate: r.endDate instanceof Date ? r.endDate.toISOString().split("T")[0] : String(r.endDate),
          days: r.days,
          reason: r.reason
        });
        if (this.isReadOnly)
          this.form.disable();
      });
    }
  }
  submit() {
    if (this.form.invalid || this.isReadOnly)
      return;
    const v = this.form.value;
    const payload = {
      leaveType: v.leaveType,
      startDate: new Date(v.startDate),
      endDate: new Date(v.endDate),
      days: +v.days,
      reason: v.reason
    };
    const obs = this.isEdit ? this.service.update(this.requestId, payload) : this.service.create(payload);
    obs.subscribe(() => this.router.navigate(["/admin/leave-requests"]));
  }
  static \u0275fac = function LeaveRequestForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _LeaveRequestForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _LeaveRequestForm, selectors: [["app-leave-request-form"]], decls: 69, vars: 7, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], ["routerLink", "/admin/leave-requests", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [3, "ngSubmit", "formGroup"], [1, "row", "g-4"], [1, "col-12", "col-lg-10", "col-xl-8"], [1, "card", "border-0", "shadow-sm"], [1, "card-header", "bg-transparent", "border-bottom", "d-flex", "align-items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#calendar"], [1, "card-body"], [1, "mb-3"], [1, "form-label", "fw-500"], [1, "text-danger"], [1, "d-flex", "flex-wrap", "gap-3", "mt-1"], [1, "form-check"], ["type", "radio", "formControlName", "leaveType", "value", "annual", "id", "typeAnnual", 1, "form-check-input"], ["for", "typeAnnual", 1, "form-check-label"], ["type", "radio", "formControlName", "leaveType", "value", "personal", "id", "typePersonal", 1, "form-check-input"], ["for", "typePersonal", 1, "form-check-label"], ["type", "radio", "formControlName", "leaveType", "value", "sick", "id", "typeSick", 1, "form-check-input"], ["for", "typeSick", 1, "form-check-label"], ["type", "radio", "formControlName", "leaveType", "value", "compensatory", "id", "typeComp", 1, "form-check-input"], ["for", "typeComp", 1, "form-check-label"], [1, "row", "g-3", "mb-3"], [1, "col-12", "col-md-4"], ["type", "date", "formControlName", "startDate", 1, "form-control"], [1, "text-danger", "small", "mt-1"], ["type", "date", "formControlName", "endDate", 1, "form-control"], ["type", "number", "formControlName", "days", "min", "0.5", "step", "0.5", "placeholder", "1", 1, "form-control"], ["formControlName", "reason", "rows", "3", "placeholder", "\u8ACB\u586B\u5BEB\u8ACB\u5047\u539F\u56E0...", 1, "form-control"], [1, "mt-4", "d-flex", "gap-2"], [1, "mt-4"], ["type", "submit", 1, "btn", "btn-primary", "waves-effect", "waves-light", 3, "disabled"], ["routerLink", "/admin/leave-requests", 1, "btn", "btn-outline-secondary", "waves-effect"]], template: function LeaveRequestForm_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "a", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "form", 6);
      \u0275\u0275listener("ngSubmit", function LeaveRequestForm_Template_form_ngSubmit_7_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "div", 10);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(12, "svg", 11);
      \u0275\u0275element(13, "use", 12);
      \u0275\u0275elementEnd();
      \u0275\u0275text(14, " \u8ACB\u5047\u8CC7\u8A0A ");
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(15, "div", 13)(16, "div", 14)(17, "label", 15);
      \u0275\u0275text(18, "\u5047\u5225 ");
      \u0275\u0275elementStart(19, "span", 16);
      \u0275\u0275text(20, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(21, "div", 17)(22, "div", 18);
      \u0275\u0275element(23, "input", 19);
      \u0275\u0275elementStart(24, "label", 20);
      \u0275\u0275text(25, "\u5E74\u5047");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(26, "div", 18);
      \u0275\u0275element(27, "input", 21);
      \u0275\u0275elementStart(28, "label", 22);
      \u0275\u0275text(29, "\u4E8B\u5047");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(30, "div", 18);
      \u0275\u0275element(31, "input", 23);
      \u0275\u0275elementStart(32, "label", 24);
      \u0275\u0275text(33, "\u75C5\u5047");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(34, "div", 18);
      \u0275\u0275element(35, "input", 25);
      \u0275\u0275elementStart(36, "label", 26);
      \u0275\u0275text(37, "\u88DC\u4F11");
      \u0275\u0275elementEnd()()()();
      \u0275\u0275elementStart(38, "div", 27)(39, "div", 28)(40, "label", 15);
      \u0275\u0275text(41, "\u958B\u59CB\u65E5\u671F ");
      \u0275\u0275elementStart(42, "span", 16);
      \u0275\u0275text(43, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(44, "input", 29);
      \u0275\u0275conditionalCreate(45, LeaveRequestForm_Conditional_45_Template, 2, 0, "div", 30);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(46, "div", 28)(47, "label", 15);
      \u0275\u0275text(48, "\u7D50\u675F\u65E5\u671F ");
      \u0275\u0275elementStart(49, "span", 16);
      \u0275\u0275text(50, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(51, "input", 31);
      \u0275\u0275conditionalCreate(52, LeaveRequestForm_Conditional_52_Template, 2, 0, "div", 30);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(53, "div", 28)(54, "label", 15);
      \u0275\u0275text(55, "\u5929\u6578 ");
      \u0275\u0275elementStart(56, "span", 16);
      \u0275\u0275text(57, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(58, "input", 32);
      \u0275\u0275conditionalCreate(59, LeaveRequestForm_Conditional_59_Template, 2, 0, "div", 30);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(60, "div", 14)(61, "label", 15);
      \u0275\u0275text(62, "\u8ACB\u5047\u539F\u56E0 ");
      \u0275\u0275elementStart(63, "span", 16);
      \u0275\u0275text(64, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(65, "textarea", 33);
      \u0275\u0275conditionalCreate(66, LeaveRequestForm_Conditional_66_Template, 2, 0, "div", 30);
      \u0275\u0275elementEnd()()();
      \u0275\u0275conditionalCreate(67, LeaveRequestForm_Conditional_67_Template, 5, 2, "div", 34)(68, LeaveRequestForm_Conditional_68_Template, 3, 0, "div", 35);
      \u0275\u0275elementEnd()()()();
    }
    if (rf & 2) {
      let tmp_2_0;
      let tmp_3_0;
      let tmp_4_0;
      let tmp_5_0;
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? ctx.isReadOnly ? "\u6AA2\u8996\u8ACB\u5047\u7533\u8ACB" : "\u7DE8\u8F2F\u8ACB\u5047\u7533\u8ACB" : "\u65B0\u589E\u8ACB\u5047\u7533\u8ACB");
      \u0275\u0275advance();
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(38);
      \u0275\u0275conditional(((tmp_2_0 = ctx.form.get("startDate")) == null ? null : tmp_2_0.invalid) && ((tmp_2_0 = ctx.form.get("startDate")) == null ? null : tmp_2_0.touched) ? 45 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_3_0 = ctx.form.get("endDate")) == null ? null : tmp_3_0.invalid) && ((tmp_3_0 = ctx.form.get("endDate")) == null ? null : tmp_3_0.touched) ? 52 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_4_0 = ctx.form.get("days")) == null ? null : tmp_4_0.invalid) && ((tmp_4_0 = ctx.form.get("days")) == null ? null : tmp_4_0.touched) ? 59 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_5_0 = ctx.form.get("reason")) == null ? null : tmp_5_0.invalid) && ((tmp_5_0 = ctx.form.get("reason")) == null ? null : tmp_5_0.touched) ? 66 : -1);
      \u0275\u0275advance();
      \u0275\u0275conditional(!ctx.isReadOnly ? 67 : 68);
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, DefaultValueAccessor, NumberValueAccessor, RadioControlValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, FormGroupDirective, FormControlName, RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(LeaveRequestForm, [{
    type: Component,
    args: [{ selector: "app-leave-request-form", imports: [ReactiveFormsModule, RouterLink], template: `<div class="container-fluid py-3">\r
  <div class="d-flex align-items-center gap-2 mb-4">\r
    <a routerLink="/admin/leave-requests" class="btn btn-sm btn-outline-secondary waves-effect">\r
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>\r
    </a>\r
    <h4 class="mb-0">{{ isEdit ? (isReadOnly ? '\u6AA2\u8996\u8ACB\u5047\u7533\u8ACB' : '\u7DE8\u8F2F\u8ACB\u5047\u7533\u8ACB') : '\u65B0\u589E\u8ACB\u5047\u7533\u8ACB' }}</h4>\r
  </div>\r
\r
  <form [formGroup]="form" (ngSubmit)="submit()">\r
    <div class="row g-4">\r
      <div class="col-12 col-lg-10 col-xl-8">\r
        <div class="card border-0 shadow-sm">\r
          <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">\r
            <svg class="sa-icon text-primary" style="stroke: currentColor">\r
              <use href="/assets/icons/sprite.svg#calendar"></use>\r
            </svg>\r
            \u8ACB\u5047\u8CC7\u8A0A\r
          </div>\r
          <div class="card-body">\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u5047\u5225 <span class="text-danger">*</span></label>\r
              <div class="d-flex flex-wrap gap-3 mt-1">\r
                <div class="form-check">\r
                  <input class="form-check-input" type="radio" formControlName="leaveType" value="annual" id="typeAnnual">\r
                  <label class="form-check-label" for="typeAnnual">\u5E74\u5047</label>\r
                </div>\r
                <div class="form-check">\r
                  <input class="form-check-input" type="radio" formControlName="leaveType" value="personal" id="typePersonal">\r
                  <label class="form-check-label" for="typePersonal">\u4E8B\u5047</label>\r
                </div>\r
                <div class="form-check">\r
                  <input class="form-check-input" type="radio" formControlName="leaveType" value="sick" id="typeSick">\r
                  <label class="form-check-label" for="typeSick">\u75C5\u5047</label>\r
                </div>\r
                <div class="form-check">\r
                  <input class="form-check-input" type="radio" formControlName="leaveType" value="compensatory" id="typeComp">\r
                  <label class="form-check-label" for="typeComp">\u88DC\u4F11</label>\r
                </div>\r
              </div>\r
            </div>\r
\r
            <div class="row g-3 mb-3">\r
              <div class="col-12 col-md-4">\r
                <label class="form-label fw-500">\u958B\u59CB\u65E5\u671F <span class="text-danger">*</span></label>\r
                <input type="date" class="form-control" formControlName="startDate">\r
                @if (form.get('startDate')?.invalid && form.get('startDate')?.touched) {\r
                  <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u958B\u59CB\u65E5\u671F\u3002</div>\r
                }\r
              </div>\r
              <div class="col-12 col-md-4">\r
                <label class="form-label fw-500">\u7D50\u675F\u65E5\u671F <span class="text-danger">*</span></label>\r
                <input type="date" class="form-control" formControlName="endDate">\r
                @if (form.get('endDate')?.invalid && form.get('endDate')?.touched) {\r
                  <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u7D50\u675F\u65E5\u671F\u3002</div>\r
                }\r
              </div>\r
              <div class="col-12 col-md-4">\r
                <label class="form-label fw-500">\u5929\u6578 <span class="text-danger">*</span></label>\r
                <input type="number" class="form-control" formControlName="days" min="0.5" step="0.5" placeholder="1">\r
                @if (form.get('days')?.invalid && form.get('days')?.touched) {\r
                  <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u6709\u6548\u5929\u6578\uFF08\u6700\u5C11 0.5\uFF09\u3002</div>\r
                }\r
              </div>\r
            </div>\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u8ACB\u5047\u539F\u56E0 <span class="text-danger">*</span></label>\r
              <textarea class="form-control" formControlName="reason" rows="3"\r
                        placeholder="\u8ACB\u586B\u5BEB\u8ACB\u5047\u539F\u56E0..."></textarea>\r
              @if (form.get('reason')?.invalid && form.get('reason')?.touched) {\r
                <div class="text-danger small mt-1">\u8ACB\u586B\u5BEB\u8ACB\u5047\u539F\u56E0\u3002</div>\r
              }\r
            </div>\r
\r
          </div>\r
        </div>\r
\r
        @if (!isReadOnly) {\r
          <div class="mt-4 d-flex gap-2">\r
            <button type="submit" class="btn btn-primary waves-effect waves-light" [disabled]="form.invalid">\r
              {{ isEdit ? '\u66F4\u65B0' : '\u9001\u51FA\u7533\u8ACB' }}\r
            </button>\r
            <a routerLink="/admin/leave-requests" class="btn btn-outline-secondary waves-effect">\u53D6\u6D88</a>\r
          </div>\r
        } @else {\r
          <div class="mt-4">\r
            <a routerLink="/admin/leave-requests" class="btn btn-outline-secondary waves-effect">\u8FD4\u56DE\u5217\u8868</a>\r
          </div>\r
        }\r
\r
      </div>\r
    </div>\r
  </form>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(LeaveRequestForm, { className: "LeaveRequestForm", filePath: "src/app/features/admin/leave-requests/pages/leave-request-form/leave-request-form.ts", lineNumber: 15 });
})();

// src/app/features/admin/travel-requests/services/travel-request.service.ts
var MOCK_REQUESTS3 = [
  {
    id: 1,
    destination: "\u53F0\u5357",
    startDate: /* @__PURE__ */ new Date("2026-03-10"),
    endDate: /* @__PURE__ */ new Date("2026-03-12"),
    estimatedCost: 3e3,
    purpose: "\u5BA2\u6236\u73FE\u5834\u62DC\u8A2A\u8207\u9700\u6C42\u78BA\u8A8D",
    projectId: 1,
    projectCode: "P2024-001",
    approvalStatus: "pending",
    createdAt: /* @__PURE__ */ new Date("2026-03-01")
  },
  {
    id: 2,
    destination: "\u53F0\u4E2D",
    startDate: /* @__PURE__ */ new Date("2026-02-25"),
    endDate: /* @__PURE__ */ new Date("2026-02-26"),
    estimatedCost: 1500,
    purpose: "\u4F9B\u61C9\u5546\u5DE5\u5EE0\u53C3\u8A2A",
    projectId: 2,
    projectCode: "P2024-002",
    approvalStatus: "approved",
    createdAt: /* @__PURE__ */ new Date("2026-02-20"),
    reviewedAt: /* @__PURE__ */ new Date("2026-02-24"),
    reviewNote: "\u6838\u51C6"
  }
];
var TravelRequestService = class _TravelRequestService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject(MOCK_REQUESTS3);
  getAll() {
    return this.items$.asObservable();
  }
  getById(id) {
    return of(this.items$.getValue().find((r) => r.id === id));
  }
  create(data) {
    const item = __spreadProps(__spreadValues({}, data), { id: Date.now(), approvalStatus: "pending", createdAt: /* @__PURE__ */ new Date() });
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }
  update(id, changes) {
    const updated = this.items$.getValue().map((r) => r.id === id ? __spreadValues(__spreadValues({}, r), changes) : r);
    this.items$.next(updated);
    return of(updated.find((r) => r.id === id));
  }
  delete(id) {
    this.items$.next(this.items$.getValue().filter((r) => r.id !== id));
    return of(void 0);
  }
  static \u0275fac = function TravelRequestService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _TravelRequestService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _TravelRequestService, factory: _TravelRequestService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(TravelRequestService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/travel-requests/models/travel-request.model.ts
var APPROVAL_STATUS_LABELS3 = {
  pending: "\u5F85\u5BE9\u6838",
  approved: "\u5DF2\u6838\u51C6",
  rejected: "\u5DF2\u9000\u56DE"
};
var APPROVAL_STATUS_CLASSES3 = {
  pending: "bg-warning-subtle text-warning-emphasis",
  approved: "bg-success-subtle text-success",
  rejected: "bg-danger-subtle text-danger"
};

// src/app/features/admin/travel-requests/pages/travel-request-list/travel-request-list.ts
var _c011 = (a0) => [a0, "edit"];
var _forTrack018 = ($index, $item) => $item.id;
function TravelRequestList_For_35_Conditional_21_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "a", 25);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 7);
    \u0275\u0275element(2, "use", 26);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(3, "button", 27);
    \u0275\u0275listener("click", function TravelRequestList_For_35_Conditional_21_Template_button_click_3_listener() {
      \u0275\u0275restoreView(_r1);
      const r_r2 = \u0275\u0275nextContext().$implicit;
      const ctx_r2 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r2.delete(r_r2));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(4, "svg", 7);
    \u0275\u0275element(5, "use", 28);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const r_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(1, _c011, r_r2.id));
  }
}
function TravelRequestList_For_35_Conditional_22_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "a", 24);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 7);
    \u0275\u0275element(2, "use", 29);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const r_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(1, _c011, r_r2.id));
  }
}
function TravelRequestList_For_35_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 17);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td", 18);
    \u0275\u0275text(4);
    \u0275\u0275pipe(5, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "td", 18);
    \u0275\u0275text(7);
    \u0275\u0275pipe(8, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 19);
    \u0275\u0275text(10);
    \u0275\u0275pipe(11, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "td", 20);
    \u0275\u0275text(13);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(14, "td", 21);
    \u0275\u0275text(15);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(16, "td")(17, "span");
    \u0275\u0275text(18);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(19, "td", 22)(20, "div", 23);
    \u0275\u0275conditionalCreate(21, TravelRequestList_For_35_Conditional_21_Template, 6, 3)(22, TravelRequestList_For_35_Conditional_22_Template, 3, 3, "a", 24);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const r_r2 = ctx.$implicit;
    const ctx_r2 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(r_r2.destination);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(5, 10, r_r2.startDate, "yyyy-MM-dd"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(8, 13, r_r2.endDate, "yyyy-MM-dd"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(11, 16, r_r2.estimatedCost, "1.0-0"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(r_r2.purpose);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(r_r2.projectCode || "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275classMap("badge " + ctx_r2.statusClass[r_r2.approvalStatus]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r2.statusLabel[r_r2.approvalStatus]);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(r_r2.approvalStatus === "pending" ? 21 : 22);
  }
}
function TravelRequestList_ForEmpty_36_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 30);
    \u0275\u0275text(2, "\u5C1A\u7121\u51FA\u5DEE\u7533\u8ACB\u3002");
    \u0275\u0275elementEnd()();
  }
}
var TravelRequestList = class _TravelRequestList {
  service = inject(TravelRequestService);
  requests$ = this.service.getAll();
  statusLabel = APPROVAL_STATUS_LABELS3;
  statusClass = APPROVAL_STATUS_CLASSES3;
  delete(r) {
    if (confirm(`\u78BA\u5B9A\u8981\u522A\u9664\u6B64\u51FA\u5DEE\u7533\u8ACB\u55CE\uFF1F`)) {
      this.service.delete(r.id).subscribe();
    }
  }
  static \u0275fac = function TravelRequestList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _TravelRequestList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _TravelRequestList, selectors: [["app-travel-request-list"]], decls: 38, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "flex-wrap", "align-items-center", "justify-content-between", "gap-2", "mb-4"], [1, "d-flex", "align-items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#map-pin"], [1, "mb-0"], ["routerLink", "new", 1, "btn", "btn-primary", "d-inline-flex", "align-items-center", "gap-1", "waves-effect", "waves-light"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "text-end"], [1, "d-none", "d-md-table-cell"], [1, "d-none", "d-lg-table-cell"], [1, "fw-500"], [1, "text-muted", "small"], [1, "text-end", "fw-500"], [1, "text-muted", "small", "d-none", "d-md-table-cell"], [1, "font-monospace", "small", "d-none", "d-lg-table-cell"], [1, "text-end", 2, "white-space", "nowrap"], [1, "d-flex", "justify-content-end", "gap-1"], ["title", "\u6AA2\u8996", 1, "btn", "btn-sm", "btn-ghost-secondary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["title", "\u7DE8\u8F2F", 1, "btn", "btn-sm", "btn-ghost-primary", "d-inline-flex", "align-items-center", "waves-effect", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#edit"], ["title", "\u522A\u9664", 1, "btn", "btn-sm", "btn-ghost-danger", "d-inline-flex", "align-items-center", "waves-effect", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], ["href", "/assets/icons/sprite.svg#eye"], ["colspan", "8", 1, "text-center", "text-muted", "py-4"]], template: function TravelRequestList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u51FA\u5DEE\u7533\u8ACB");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "a", 6);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(8, "svg", 7);
      \u0275\u0275element(9, "use", 8);
      \u0275\u0275elementEnd();
      \u0275\u0275text(10, " \u65B0\u589E\u7533\u8ACB ");
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(11, "div", 9)(12, "div", 10)(13, "div", 11)(14, "table", 12)(15, "thead", 13)(16, "tr")(17, "th");
      \u0275\u0275text(18, "\u51FA\u5DEE\u5730\u9EDE");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(19, "th");
      \u0275\u0275text(20, "\u958B\u59CB\u65E5\u671F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(21, "th");
      \u0275\u0275text(22, "\u7D50\u675F\u65E5\u671F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(23, "th", 14);
      \u0275\u0275text(24, "\u9810\u4F30\u8CBB\u7528");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(25, "th", 15);
      \u0275\u0275text(26, "\u76EE\u7684");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(27, "th", 16);
      \u0275\u0275text(28, "\u5C08\u6848");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(29, "th");
      \u0275\u0275text(30, "\u72C0\u614B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(31, "th", 14);
      \u0275\u0275text(32, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(33, "tbody");
      \u0275\u0275repeaterCreate(34, TravelRequestList_For_35_Template, 23, 19, "tr", null, _forTrack018, false, TravelRequestList_ForEmpty_36_Template, 3, 0, "tr");
      \u0275\u0275pipe(37, "async");
      \u0275\u0275elementEnd()()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(34);
      \u0275\u0275repeater(\u0275\u0275pipeBind1(37, 1, ctx.requests$));
    }
  }, dependencies: [RouterLink, AsyncPipe, DatePipe, DecimalPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(TravelRequestList, [{
    type: Component,
    args: [{ selector: "app-travel-request-list", imports: [RouterLink, AsyncPipe, DatePipe, DecimalPipe], template: `<div class="container-fluid py-3">\r
  <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">\r
    <div class="d-flex align-items-center gap-2">\r
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">\r
        <use href="/assets/icons/sprite.svg#map-pin"></use>\r
      </svg>\r
      <h4 class="mb-0">\u51FA\u5DEE\u7533\u8ACB</h4>\r
    </div>\r
    <a routerLink="new" class="btn btn-primary d-inline-flex align-items-center gap-1 waves-effect waves-light">\r
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>\r
      \u65B0\u589E\u7533\u8ACB\r
    </a>\r
  </div>\r
\r
  <div class="card border-0 shadow-sm">\r
    <div class="card-body p-0">\r
      <div class="table-responsive">\r
        <table class="table table-hover mb-0">\r
          <thead class="table-light">\r
            <tr>\r
              <th>\u51FA\u5DEE\u5730\u9EDE</th>\r
              <th>\u958B\u59CB\u65E5\u671F</th>\r
              <th>\u7D50\u675F\u65E5\u671F</th>\r
              <th class="text-end">\u9810\u4F30\u8CBB\u7528</th>\r
              <th class="d-none d-md-table-cell">\u76EE\u7684</th>\r
              <th class="d-none d-lg-table-cell">\u5C08\u6848</th>\r
              <th>\u72C0\u614B</th>\r
              <th class="text-end">\u64CD\u4F5C</th>\r
            </tr>\r
          </thead>\r
          <tbody>\r
            @for (r of requests$ | async; track r.id) {\r
              <tr>\r
                <td class="fw-500">{{ r.destination }}</td>\r
                <td class="text-muted small">{{ r.startDate | date:'yyyy-MM-dd' }}</td>\r
                <td class="text-muted small">{{ r.endDate | date:'yyyy-MM-dd' }}</td>\r
                <td class="text-end fw-500">{{ r.estimatedCost | number:'1.0-0' }}</td>\r
                <td class="text-muted small d-none d-md-table-cell">{{ r.purpose }}</td>\r
                <td class="font-monospace small d-none d-lg-table-cell">{{ r.projectCode || '\u2014' }}</td>\r
                <td>\r
                  <span [class]="'badge ' + statusClass[r.approvalStatus]">{{ statusLabel[r.approvalStatus] }}</span>\r
                </td>\r
                <td class="text-end" style="white-space: nowrap">\r
                  <div class="d-flex justify-content-end gap-1">\r
                    @if (r.approvalStatus === 'pending') {\r
                      <a [routerLink]="[r.id, 'edit']" class="btn btn-sm btn-ghost-primary d-inline-flex align-items-center waves-effect" title="\u7DE8\u8F2F">\r
                        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>\r
                      </a>\r
                      <button class="btn btn-sm btn-ghost-danger d-inline-flex align-items-center waves-effect" (click)="delete(r)" title="\u522A\u9664">\r
                        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>\r
                      </button>\r
                    } @else {\r
                      <a [routerLink]="[r.id, 'edit']" class="btn btn-sm btn-ghost-secondary d-inline-flex align-items-center waves-effect" title="\u6AA2\u8996">\r
                        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#eye"></use></svg>\r
                      </a>\r
                    }\r
                  </div>\r
                </td>\r
              </tr>\r
            } @empty {\r
              <tr>\r
                <td colspan="8" class="text-center text-muted py-4">\u5C1A\u7121\u51FA\u5DEE\u7533\u8ACB\u3002</td>\r
              </tr>\r
            }\r
          </tbody>\r
        </table>\r
      </div>\r
    </div>\r
  </div>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(TravelRequestList, { className: "TravelRequestList", filePath: "src/app/features/admin/travel-requests/pages/travel-request-list/travel-request-list.ts", lineNumber: 15 });
})();

// src/app/features/admin/travel-requests/pages/travel-request-form/travel-request-form.ts
var _forTrack019 = ($index, $item) => $item.id;
function TravelRequestForm_Conditional_23_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 19);
    \u0275\u0275text(1, "\u8ACB\u586B\u5BEB\u51FA\u5DEE\u5730\u9EDE\u3002");
    \u0275\u0275elementEnd();
  }
}
function TravelRequestForm_Conditional_30_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 19);
    \u0275\u0275text(1, "\u8ACB\u8F38\u5165\u9810\u4F30\u8CBB\u7528\u3002");
    \u0275\u0275elementEnd();
  }
}
function TravelRequestForm_Conditional_38_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 19);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u51FA\u767C\u65E5\u671F\u3002");
    \u0275\u0275elementEnd();
  }
}
function TravelRequestForm_Conditional_45_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 19);
    \u0275\u0275text(1, "\u8ACB\u9078\u64C7\u8FD4\u56DE\u65E5\u671F\u3002");
    \u0275\u0275elementEnd();
  }
}
function TravelRequestForm_For_53_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 25);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const p_r1 = ctx.$implicit;
    \u0275\u0275property("ngValue", p_r1.id);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate2("", p_r1.code, "", p_r1.departmentName ? "\uFF08" + p_r1.departmentName + "\uFF09" : "");
  }
}
function TravelRequestForm_Conditional_60_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 19);
    \u0275\u0275text(1, "\u8ACB\u586B\u5BEB\u51FA\u5DEE\u76EE\u7684\u3002");
    \u0275\u0275elementEnd();
  }
}
function TravelRequestForm_Conditional_61_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 28)(1, "button", 30);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "a", 31);
    \u0275\u0275text(4, "\u53D6\u6D88");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r1.form.invalid);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.isEdit ? "\u66F4\u65B0" : "\u9001\u51FA\u7533\u8ACB", " ");
  }
}
function TravelRequestForm_Conditional_62_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 29)(1, "a", 31);
    \u0275\u0275text(2, "\u8FD4\u56DE\u5217\u8868");
    \u0275\u0275elementEnd()();
  }
}
var TravelRequestForm = class _TravelRequestForm {
  fb = inject(FormBuilder);
  service = inject(TravelRequestService);
  projects$ = inject(ProjectService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  isEdit = false;
  requestId = 0;
  isReadOnly = false;
  projects = [];
  statusLabel = APPROVAL_STATUS_LABELS3;
  statusClass = APPROVAL_STATUS_CLASSES3;
  form = this.fb.group({
    destination: ["", Validators.required],
    startDate: ["", Validators.required],
    endDate: ["", Validators.required],
    estimatedCost: [0, [Validators.required, Validators.min(0)]],
    purpose: ["", Validators.required],
    projectId: [null]
  });
  ngOnInit() {
    this.projects$.getAll().subscribe((p) => this.projects = p);
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEdit = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe((r) => {
        if (!r)
          return;
        this.isReadOnly = r.approvalStatus !== "pending";
        this.form.patchValue({
          destination: r.destination,
          startDate: r.startDate instanceof Date ? r.startDate.toISOString().split("T")[0] : String(r.startDate),
          endDate: r.endDate instanceof Date ? r.endDate.toISOString().split("T")[0] : String(r.endDate),
          estimatedCost: r.estimatedCost,
          purpose: r.purpose,
          projectId: r.projectId ?? null
        });
        if (this.isReadOnly)
          this.form.disable();
      });
    }
  }
  submit() {
    if (this.form.invalid || this.isReadOnly)
      return;
    const v = this.form.value;
    const project = this.projects.find((p) => p.id === v.projectId);
    const payload = {
      destination: v.destination,
      startDate: new Date(v.startDate),
      endDate: new Date(v.endDate),
      estimatedCost: +v.estimatedCost,
      purpose: v.purpose,
      projectId: v.projectId ?? void 0,
      projectCode: project?.code
    };
    const obs = this.isEdit ? this.service.update(this.requestId, payload) : this.service.create(payload);
    obs.subscribe(() => this.router.navigate(["/admin/travel-requests"]));
  }
  static \u0275fac = function TravelRequestForm_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _TravelRequestForm)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _TravelRequestForm, selectors: [["app-travel-request-form"]], decls: 63, vars: 9, consts: [[1, "container-fluid", "py-3"], [1, "d-flex", "align-items-center", "gap-2", "mb-4"], ["routerLink", "/admin/travel-requests", 1, "btn", "btn-sm", "btn-outline-secondary", "waves-effect"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [3, "ngSubmit", "formGroup"], [1, "row", "g-4"], [1, "col-12", "col-lg-10", "col-xl-8"], [1, "card", "border-0", "shadow-sm"], [1, "card-header", "bg-transparent", "border-bottom", "d-flex", "align-items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#map-pin"], [1, "card-body"], [1, "row", "g-3", "mb-3"], [1, "col-12", "col-md-6"], [1, "form-label", "fw-500"], [1, "text-danger"], ["type", "text", "formControlName", "destination", "placeholder", "\u4F8B\u5982\uFF1A\u53F0\u5357\u3001\u53F0\u4E2D", 1, "form-control"], [1, "text-danger", "small", "mt-1"], ["type", "number", "formControlName", "estimatedCost", "min", "0", "placeholder", "0", 1, "form-control"], [1, "col-12", "col-md-4"], ["type", "date", "formControlName", "startDate", 1, "form-control"], ["type", "date", "formControlName", "endDate", 1, "form-control"], ["formControlName", "projectId", 1, "form-select"], [3, "ngValue"], [1, "mb-3"], ["formControlName", "purpose", "rows", "3", "placeholder", "\u8ACB\u586B\u5BEB\u51FA\u5DEE\u76EE\u7684...", 1, "form-control"], [1, "mt-4", "d-flex", "gap-2"], [1, "mt-4"], ["type", "submit", 1, "btn", "btn-primary", "waves-effect", "waves-light", 3, "disabled"], ["routerLink", "/admin/travel-requests", 1, "btn", "btn-outline-secondary", "waves-effect"]], template: function TravelRequestForm_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "a", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "form", 6);
      \u0275\u0275listener("ngSubmit", function TravelRequestForm_Template_form_ngSubmit_7_listener() {
        return ctx.submit();
      });
      \u0275\u0275elementStart(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "div", 10);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(12, "svg", 11);
      \u0275\u0275element(13, "use", 12);
      \u0275\u0275elementEnd();
      \u0275\u0275text(14, " \u51FA\u5DEE\u8CC7\u8A0A ");
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(15, "div", 13)(16, "div", 14)(17, "div", 15)(18, "label", 16);
      \u0275\u0275text(19, "\u51FA\u5DEE\u5730\u9EDE ");
      \u0275\u0275elementStart(20, "span", 17);
      \u0275\u0275text(21, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(22, "input", 18);
      \u0275\u0275conditionalCreate(23, TravelRequestForm_Conditional_23_Template, 2, 0, "div", 19);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(24, "div", 15)(25, "label", 16);
      \u0275\u0275text(26, "\u9810\u4F30\u8CBB\u7528\uFF08\u5143\uFF09");
      \u0275\u0275elementStart(27, "span", 17);
      \u0275\u0275text(28, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(29, "input", 20);
      \u0275\u0275conditionalCreate(30, TravelRequestForm_Conditional_30_Template, 2, 0, "div", 19);
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(31, "div", 14)(32, "div", 21)(33, "label", 16);
      \u0275\u0275text(34, "\u51FA\u767C\u65E5\u671F ");
      \u0275\u0275elementStart(35, "span", 17);
      \u0275\u0275text(36, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(37, "input", 22);
      \u0275\u0275conditionalCreate(38, TravelRequestForm_Conditional_38_Template, 2, 0, "div", 19);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(39, "div", 21)(40, "label", 16);
      \u0275\u0275text(41, "\u8FD4\u56DE\u65E5\u671F ");
      \u0275\u0275elementStart(42, "span", 17);
      \u0275\u0275text(43, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(44, "input", 23);
      \u0275\u0275conditionalCreate(45, TravelRequestForm_Conditional_45_Template, 2, 0, "div", 19);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(46, "div", 21)(47, "label", 16);
      \u0275\u0275text(48, "\u95DC\u806F\u5C08\u6848\uFF08\u9078\u586B\uFF09");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(49, "select", 24)(50, "option", 25);
      \u0275\u0275text(51, "\u2014 \u4E0D\u95DC\u806F\u5C08\u6848 \u2014");
      \u0275\u0275elementEnd();
      \u0275\u0275repeaterCreate(52, TravelRequestForm_For_53_Template, 2, 3, "option", 25, _forTrack019);
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(54, "div", 26)(55, "label", 16);
      \u0275\u0275text(56, "\u51FA\u5DEE\u76EE\u7684 ");
      \u0275\u0275elementStart(57, "span", 17);
      \u0275\u0275text(58, "*");
      \u0275\u0275elementEnd()();
      \u0275\u0275element(59, "textarea", 27);
      \u0275\u0275conditionalCreate(60, TravelRequestForm_Conditional_60_Template, 2, 0, "div", 19);
      \u0275\u0275elementEnd()()();
      \u0275\u0275conditionalCreate(61, TravelRequestForm_Conditional_61_Template, 5, 2, "div", 28)(62, TravelRequestForm_Conditional_62_Template, 3, 0, "div", 29);
      \u0275\u0275elementEnd()()()();
    }
    if (rf & 2) {
      let tmp_2_0;
      let tmp_3_0;
      let tmp_4_0;
      let tmp_5_0;
      let tmp_8_0;
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.isEdit ? ctx.isReadOnly ? "\u6AA2\u8996\u51FA\u5DEE\u7533\u8ACB" : "\u7DE8\u8F2F\u51FA\u5DEE\u7533\u8ACB" : "\u65B0\u589E\u51FA\u5DEE\u7533\u8ACB");
      \u0275\u0275advance();
      \u0275\u0275property("formGroup", ctx.form);
      \u0275\u0275advance(16);
      \u0275\u0275conditional(((tmp_2_0 = ctx.form.get("destination")) == null ? null : tmp_2_0.invalid) && ((tmp_2_0 = ctx.form.get("destination")) == null ? null : tmp_2_0.touched) ? 23 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_3_0 = ctx.form.get("estimatedCost")) == null ? null : tmp_3_0.invalid) && ((tmp_3_0 = ctx.form.get("estimatedCost")) == null ? null : tmp_3_0.touched) ? 30 : -1);
      \u0275\u0275advance(8);
      \u0275\u0275conditional(((tmp_4_0 = ctx.form.get("startDate")) == null ? null : tmp_4_0.invalid) && ((tmp_4_0 = ctx.form.get("startDate")) == null ? null : tmp_4_0.touched) ? 38 : -1);
      \u0275\u0275advance(7);
      \u0275\u0275conditional(((tmp_5_0 = ctx.form.get("endDate")) == null ? null : tmp_5_0.invalid) && ((tmp_5_0 = ctx.form.get("endDate")) == null ? null : tmp_5_0.touched) ? 45 : -1);
      \u0275\u0275advance(5);
      \u0275\u0275property("ngValue", null);
      \u0275\u0275advance(2);
      \u0275\u0275repeater(ctx.projects);
      \u0275\u0275advance(8);
      \u0275\u0275conditional(((tmp_8_0 = ctx.form.get("purpose")) == null ? null : tmp_8_0.invalid) && ((tmp_8_0 = ctx.form.get("purpose")) == null ? null : tmp_8_0.touched) ? 60 : -1);
      \u0275\u0275advance();
      \u0275\u0275conditional(!ctx.isReadOnly ? 61 : 62);
    }
  }, dependencies: [ReactiveFormsModule, \u0275NgNoValidate, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, NumberValueAccessor, SelectControlValueAccessor, NgControlStatus, NgControlStatusGroup, MinValidator, FormGroupDirective, FormControlName, RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(TravelRequestForm, [{
    type: Component,
    args: [{ selector: "app-travel-request-form", imports: [ReactiveFormsModule, RouterLink], template: `<div class="container-fluid py-3">\r
  <div class="d-flex align-items-center gap-2 mb-4">\r
    <a routerLink="/admin/travel-requests" class="btn btn-sm btn-outline-secondary waves-effect">\r
      <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>\r
    </a>\r
    <h4 class="mb-0">{{ isEdit ? (isReadOnly ? '\u6AA2\u8996\u51FA\u5DEE\u7533\u8ACB' : '\u7DE8\u8F2F\u51FA\u5DEE\u7533\u8ACB') : '\u65B0\u589E\u51FA\u5DEE\u7533\u8ACB' }}</h4>\r
  </div>\r
\r
  <form [formGroup]="form" (ngSubmit)="submit()">\r
    <div class="row g-4">\r
      <div class="col-12 col-lg-10 col-xl-8">\r
        <div class="card border-0 shadow-sm">\r
          <div class="card-header bg-transparent border-bottom d-flex align-items-center gap-2 fw-600">\r
            <svg class="sa-icon text-primary" style="stroke: currentColor">\r
              <use href="/assets/icons/sprite.svg#map-pin"></use>\r
            </svg>\r
            \u51FA\u5DEE\u8CC7\u8A0A\r
          </div>\r
          <div class="card-body">\r
\r
            <div class="row g-3 mb-3">\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u51FA\u5DEE\u5730\u9EDE <span class="text-danger">*</span></label>\r
                <input type="text" class="form-control" formControlName="destination" placeholder="\u4F8B\u5982\uFF1A\u53F0\u5357\u3001\u53F0\u4E2D">\r
                @if (form.get('destination')?.invalid && form.get('destination')?.touched) {\r
                  <div class="text-danger small mt-1">\u8ACB\u586B\u5BEB\u51FA\u5DEE\u5730\u9EDE\u3002</div>\r
                }\r
              </div>\r
              <div class="col-12 col-md-6">\r
                <label class="form-label fw-500">\u9810\u4F30\u8CBB\u7528\uFF08\u5143\uFF09<span class="text-danger">*</span></label>\r
                <input type="number" class="form-control" formControlName="estimatedCost" min="0" placeholder="0">\r
                @if (form.get('estimatedCost')?.invalid && form.get('estimatedCost')?.touched) {\r
                  <div class="text-danger small mt-1">\u8ACB\u8F38\u5165\u9810\u4F30\u8CBB\u7528\u3002</div>\r
                }\r
              </div>\r
            </div>\r
\r
            <div class="row g-3 mb-3">\r
              <div class="col-12 col-md-4">\r
                <label class="form-label fw-500">\u51FA\u767C\u65E5\u671F <span class="text-danger">*</span></label>\r
                <input type="date" class="form-control" formControlName="startDate">\r
                @if (form.get('startDate')?.invalid && form.get('startDate')?.touched) {\r
                  <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u51FA\u767C\u65E5\u671F\u3002</div>\r
                }\r
              </div>\r
              <div class="col-12 col-md-4">\r
                <label class="form-label fw-500">\u8FD4\u56DE\u65E5\u671F <span class="text-danger">*</span></label>\r
                <input type="date" class="form-control" formControlName="endDate">\r
                @if (form.get('endDate')?.invalid && form.get('endDate')?.touched) {\r
                  <div class="text-danger small mt-1">\u8ACB\u9078\u64C7\u8FD4\u56DE\u65E5\u671F\u3002</div>\r
                }\r
              </div>\r
              <div class="col-12 col-md-4">\r
                <label class="form-label fw-500">\u95DC\u806F\u5C08\u6848\uFF08\u9078\u586B\uFF09</label>\r
                <select class="form-select" formControlName="projectId">\r
                  <option [ngValue]="null">\u2014 \u4E0D\u95DC\u806F\u5C08\u6848 \u2014</option>\r
                  @for (p of projects; track p.id) {\r
                    <option [ngValue]="p.id">{{ p.code }}{{ p.departmentName ? '\uFF08' + p.departmentName + '\uFF09' : '' }}</option>\r
                  }\r
                </select>\r
              </div>\r
            </div>\r
\r
            <div class="mb-3">\r
              <label class="form-label fw-500">\u51FA\u5DEE\u76EE\u7684 <span class="text-danger">*</span></label>\r
              <textarea class="form-control" formControlName="purpose" rows="3"\r
                        placeholder="\u8ACB\u586B\u5BEB\u51FA\u5DEE\u76EE\u7684..."></textarea>\r
              @if (form.get('purpose')?.invalid && form.get('purpose')?.touched) {\r
                <div class="text-danger small mt-1">\u8ACB\u586B\u5BEB\u51FA\u5DEE\u76EE\u7684\u3002</div>\r
              }\r
            </div>\r
\r
          </div>\r
        </div>\r
\r
        @if (!isReadOnly) {\r
          <div class="mt-4 d-flex gap-2">\r
            <button type="submit" class="btn btn-primary waves-effect waves-light" [disabled]="form.invalid">\r
              {{ isEdit ? '\u66F4\u65B0' : '\u9001\u51FA\u7533\u8ACB' }}\r
            </button>\r
            <a routerLink="/admin/travel-requests" class="btn btn-outline-secondary waves-effect">\u53D6\u6D88</a>\r
          </div>\r
        } @else {\r
          <div class="mt-4">\r
            <a routerLink="/admin/travel-requests" class="btn btn-outline-secondary waves-effect">\u8FD4\u56DE\u5217\u8868</a>\r
          </div>\r
        }\r
\r
      </div>\r
    </div>\r
  </form>\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(TravelRequestForm, { className: "TravelRequestForm", filePath: "src/app/features/admin/travel-requests/pages/travel-request-form/travel-request-form.ts", lineNumber: 14 });
})();

// src/app/features/admin/admin.routes.ts
var ADMIN_ROUTES = [
  { path: "", redirectTo: "users", pathMatch: "full" },
  // 員工管理
  { path: "users", component: UserList, data: { title: "\u54E1\u5DE5\u7BA1\u7406" } },
  { path: "users/new", component: UserForm, data: { title: "\u65B0\u589E\u54E1\u5DE5" } },
  { path: "users/:id/edit", component: UserForm, data: { title: "\u7DE8\u8F2F\u54E1\u5DE5" } },
  // 部門管理
  { path: "departments", component: DepartmentList, data: { title: "\u90E8\u9580\u7BA1\u7406" } },
  { path: "departments/new", component: DepartmentForm, data: { title: "\u65B0\u589E\u90E8\u9580" } },
  { path: "departments/:id/edit", component: DepartmentForm, data: { title: "\u7DE8\u8F2F\u90E8\u9580" } },
  // 職稱管理
  { path: "job-titles", component: JobTitleList, data: { title: "\u8077\u7A31\u7BA1\u7406" } },
  { path: "job-titles/new", component: JobTitleForm, data: { title: "\u65B0\u589E\u8077\u7A31" } },
  { path: "job-titles/:id/edit", component: JobTitleForm, data: { title: "\u7DE8\u8F2F\u8077\u7A31" } },
  // 簽核管理
  { path: "approvals", component: ApprovalList, data: { title: "\u7C3D\u6838\u7BA1\u7406" } },
  { path: "approvals/:id/flow", component: ApprovalFlow, data: { title: "\u7C3D\u6838\u6D41\u7A0B" } },
  // 角色 / 權限
  { path: "roles", component: RoleList, data: { title: "\u89D2\u8272\u7BA1\u7406" } },
  { path: "roles/new", component: RoleForm, data: { title: "\u65B0\u589E\u89D2\u8272" } },
  { path: "roles/:id/edit", component: RoleForm, data: { title: "\u7DE8\u8F2F\u89D2\u8272" } },
  { path: "permissions", component: PermissionList, data: { title: "\u6B0A\u9650\u7BA1\u7406" } },
  { path: "permissions/new", component: PermissionForm, data: { title: "\u65B0\u589E\u6B0A\u9650" } },
  { path: "permissions/:id/edit", component: PermissionForm, data: { title: "\u7DE8\u8F2F\u6B0A\u9650" } },
  // 專案管理
  { path: "projects", component: ProjectList, data: { title: "\u5C08\u6848\u7BA1\u7406" } },
  { path: "projects/new", component: ProjectForm, data: { title: "\u65B0\u589E\u5C08\u6848" } },
  { path: "projects/:id/edit", component: ProjectForm, data: { title: "\u7DE8\u8F2F\u5C08\u6848" } },
  // 請款申請
  { path: "payment-requests", component: PaymentList, data: { title: "\u8ACB\u6B3E\u7533\u8ACB" } },
  { path: "payment-requests/new", component: PaymentForm, data: { title: "\u65B0\u589E\u8ACB\u6B3E\u7533\u8ACB" } },
  { path: "payment-requests/:id/edit", component: PaymentForm, data: { title: "\u7DE8\u8F2F\u8ACB\u6B3E\u7533\u8ACB" } },
  // 請假申請
  { path: "leave-requests", component: LeaveRequestList, data: { title: "\u8ACB\u5047\u7533\u8ACB" } },
  { path: "leave-requests/new", component: LeaveRequestForm, data: { title: "\u65B0\u589E\u8ACB\u5047\u7533\u8ACB" } },
  { path: "leave-requests/:id/edit", component: LeaveRequestForm, data: { title: "\u7DE8\u8F2F\u8ACB\u5047\u7533\u8ACB" } },
  // 出差申請
  { path: "travel-requests", component: TravelRequestList, data: { title: "\u51FA\u5DEE\u7533\u8ACB" } },
  { path: "travel-requests/new", component: TravelRequestForm, data: { title: "\u65B0\u589E\u51FA\u5DEE\u7533\u8ACB" } },
  { path: "travel-requests/:id/edit", component: TravelRequestForm, data: { title: "\u7DE8\u8F2F\u51FA\u5DEE\u7533\u8ACB" } },
  // 簽核作業
  { path: "approval-tasks", component: ApprovalTaskList, data: { title: "\u7C3D\u6838\u4F5C\u696D" } },
  { path: "approval-tasks/:applicationType/:id/review", component: ApprovalTaskReview, data: { title: "\u5BE9\u6838" } },
  // 系統設定
  { path: "settings", component: Settings, data: { title: "\u7CFB\u7D71\u8A2D\u5B9A" } }
];
export {
  ADMIN_ROUTES
};
//# sourceMappingURL=chunk-ZLGDANMU.js.map
