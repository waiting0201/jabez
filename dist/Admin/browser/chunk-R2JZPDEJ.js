import {
  ContentRef,
  NgbConfig,
  ScrollBar,
  getFocusableBoundaryElements,
  isDefined,
  isPromise,
  isString,
  ngbFocusTrap,
  ngbRunTransition,
  reflow
} from "./chunk-JZXKSRAX.js";
import {
  ApplicationRef,
  ChangeDetectorRef,
  Component,
  DOCUMENT,
  ElementRef,
  EnvironmentInjector,
  EventEmitter,
  HttpClient,
  Injectable,
  Injector,
  Input,
  NgModule,
  NgZone,
  Output,
  Subject,
  TemplateRef,
  ViewChild,
  ViewEncapsulation,
  afterNextRender,
  createComponent,
  environment,
  filter,
  fromEvent,
  inject,
  of,
  setClassMetadata,
  switchMap,
  take,
  takeUntil,
  tap,
  zip,
  ɵɵattribute,
  ɵɵclassMap,
  ɵɵclassProp,
  ɵɵdefineComponent,
  ɵɵdefineInjectable,
  ɵɵdefineInjector,
  ɵɵdefineNgModule,
  ɵɵdomElementEnd,
  ɵɵdomElementStart,
  ɵɵloadQuery,
  ɵɵprojection,
  ɵɵprojectionDef,
  ɵɵqueryRefresh,
  ɵɵviewQuery
} from "./chunk-EZPNPJLO.js";
import {
  __spreadProps,
  __spreadValues
} from "./chunk-KWSTWQNB.js";

// node_modules/@ng-bootstrap/ng-bootstrap/fesm2022/ng-bootstrap-ng-bootstrap-modal.mjs
var _c0 = ["dialog"];
var _c1 = ["*"];
var NgbModalConfig = class _NgbModalConfig {
  constructor() {
    this._ngbConfig = inject(NgbConfig);
    this.backdrop = true;
    this.fullscreen = false;
    this.keyboard = true;
    this.role = "dialog";
  }
  get animation() {
    return this._animation ?? this._ngbConfig.animation;
  }
  set animation(animation) {
    this._animation = animation;
  }
  static {
    this.\u0275fac = function NgbModalConfig_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _NgbModalConfig)();
    };
  }
  static {
    this.\u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({
      token: _NgbModalConfig,
      factory: _NgbModalConfig.\u0275fac,
      providedIn: "root"
    });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(NgbModalConfig, [{
    type: Injectable,
    args: [{
      providedIn: "root"
    }]
  }], null, null);
})();
var BACKDROP_ATTRIBUTES = ["animation", "backdropClass"];
var NgbModalBackdrop = class _NgbModalBackdrop {
  constructor() {
    this._nativeElement = inject(ElementRef).nativeElement;
    this._zone = inject(NgZone);
    this._injector = inject(Injector);
    this._cdRef = inject(ChangeDetectorRef);
  }
  ngOnInit() {
    afterNextRender({
      mixedReadWrite: () => ngbRunTransition(this._zone, this._nativeElement, (element, animation) => {
        if (animation) {
          reflow(element);
        }
        element.classList.add("show");
      }, {
        animation: this.animation,
        runningTransition: "continue"
      })
    }, {
      injector: this._injector
    });
  }
  hide() {
    return ngbRunTransition(this._zone, this._nativeElement, ({
      classList
    }) => classList.remove("show"), {
      animation: this.animation,
      runningTransition: "stop"
    });
  }
  updateOptions(options) {
    BACKDROP_ATTRIBUTES.forEach((optionName) => {
      if (isDefined(options[optionName])) {
        this[optionName] = options[optionName];
      }
    });
    this._cdRef.markForCheck();
  }
  static {
    this.\u0275fac = function NgbModalBackdrop_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _NgbModalBackdrop)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({
      type: _NgbModalBackdrop,
      selectors: [["ngb-modal-backdrop"]],
      hostAttrs: [2, "z-index", "1055"],
      hostVars: 6,
      hostBindings: function NgbModalBackdrop_HostBindings(rf, ctx) {
        if (rf & 2) {
          \u0275\u0275classMap("modal-backdrop" + (ctx.backdropClass ? " " + ctx.backdropClass : ""));
          \u0275\u0275classProp("show", !ctx.animation)("fade", ctx.animation);
        }
      },
      inputs: {
        animation: "animation",
        backdropClass: "backdropClass"
      },
      decls: 0,
      vars: 0,
      template: function NgbModalBackdrop_Template(rf, ctx) {
      },
      encapsulation: 2
    });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(NgbModalBackdrop, [{
    type: Component,
    args: [{
      selector: "ngb-modal-backdrop",
      encapsulation: ViewEncapsulation.None,
      template: "",
      host: {
        "[class]": '"modal-backdrop" + (backdropClass ? " " + backdropClass : "")',
        "[class.show]": "!animation",
        "[class.fade]": "animation",
        style: "z-index: 1055"
      }
    }]
  }], null, {
    animation: [{
      type: Input
    }],
    backdropClass: [{
      type: Input
    }]
  });
})();
var NgbActiveModal = class {
  /**
   * Updates options of an opened modal.
   *
   * @since 14.2.0
   */
  update(options) {
  }
  /**
   * Closes the modal with an optional `result` value.
   *
   * The `NgbModalRef.result` promise will be resolved with the provided value.
   */
  close(result) {
  }
  /**
   * Dismisses the modal with an optional `reason` value.
   *
   * The `NgbModalRef.result` promise will be rejected with the provided value.
   */
  dismiss(reason) {
  }
};
var NgbModalRef = class {
  /**
   * Updates options of an opened modal.
   *
   * @since 14.2.0
   */
  update(options) {
    this._windowCmptRef.instance.updateOptions(options);
    if (this._backdropCmptRef && this._backdropCmptRef.instance) {
      this._backdropCmptRef.instance.updateOptions(options);
    }
  }
  /**
   * The instance of a component used for the modal content.
   *
   * When a `TemplateRef` is used as the content or when the modal is closed, will return `undefined`.
   */
  get componentInstance() {
    if (this._contentRef && this._contentRef.componentRef) {
      return this._contentRef.componentRef.instance;
    }
  }
  /**
   * The observable that emits when the modal is closed via the `.close()` method.
   *
   * It will emit the result passed to the `.close()` method.
   *
   * @since 8.0.0
   */
  get closed() {
    return this._closed.asObservable().pipe(takeUntil(this._hidden));
  }
  /**
   * The observable that emits when the modal is dismissed via the `.dismiss()` method.
   *
   * It will emit the reason passed to the `.dismissed()` method by the user, or one of the internal
   * reasons like backdrop click or ESC key press.
   *
   * @since 8.0.0
   */
  get dismissed() {
    return this._dismissed.asObservable().pipe(takeUntil(this._hidden));
  }
  /**
   * The observable that emits when both modal window and backdrop are closed and animations were finished.
   * At this point modal and backdrop elements will be removed from the DOM tree.
   *
   * This observable will be completed after emitting.
   *
   * @since 8.0.0
   */
  get hidden() {
    return this._hidden.asObservable();
  }
  /**
   * The observable that emits when modal is fully visible and animation was finished.
   * Modal DOM element is always available synchronously after calling 'modal.open()' service.
   *
   * This observable will be completed after emitting.
   * It will not emit, if modal is closed before open animation is finished.
   *
   * @since 8.0.0
   */
  get shown() {
    return this._windowCmptRef.instance.shown.asObservable();
  }
  constructor(_windowCmptRef, _contentRef, _backdropCmptRef, _beforeDismiss) {
    this._windowCmptRef = _windowCmptRef;
    this._contentRef = _contentRef;
    this._backdropCmptRef = _backdropCmptRef;
    this._beforeDismiss = _beforeDismiss;
    this._closed = new Subject();
    this._dismissed = new Subject();
    this._hidden = new Subject();
    _windowCmptRef.instance.dismissEvent.subscribe((reason) => {
      this.dismiss(reason);
    });
    this.result = new Promise((resolve, reject) => {
      this._resolve = resolve;
      this._reject = reject;
    });
    this.result.then(null, () => {
    });
  }
  /**
   * Closes the modal with an optional `result` value.
   *
   * The `NgbMobalRef.result` promise will be resolved with the provided value.
   */
  close(result) {
    if (this._windowCmptRef) {
      this._closed.next(result);
      this._resolve(result);
      this._removeModalElements();
    }
  }
  _dismiss(reason) {
    this._dismissed.next(reason);
    this._reject(reason);
    this._removeModalElements();
  }
  /**
   * Dismisses the modal with an optional `reason` value.
   *
   * The `NgbModalRef.result` promise will be rejected with the provided value.
   */
  dismiss(reason) {
    if (this._windowCmptRef) {
      if (!this._beforeDismiss) {
        this._dismiss(reason);
      } else {
        const dismiss = this._beforeDismiss();
        if (isPromise(dismiss)) {
          dismiss.then((result) => {
            if (result !== false) {
              this._dismiss(reason);
            }
          }, () => {
          });
        } else if (dismiss !== false) {
          this._dismiss(reason);
        }
      }
    }
  }
  _removeModalElements() {
    const windowTransition$ = this._windowCmptRef.instance.hide();
    const backdropTransition$ = this._backdropCmptRef ? this._backdropCmptRef.instance.hide() : of(void 0);
    windowTransition$.subscribe(() => {
      const {
        nativeElement
      } = this._windowCmptRef.location;
      nativeElement.parentNode.removeChild(nativeElement);
      this._windowCmptRef.destroy();
      this._contentRef?.viewRef?.destroy();
      this._windowCmptRef = null;
      this._contentRef = null;
    });
    backdropTransition$.subscribe(() => {
      if (this._backdropCmptRef) {
        const {
          nativeElement
        } = this._backdropCmptRef.location;
        nativeElement.parentNode.removeChild(nativeElement);
        this._backdropCmptRef.destroy();
        this._backdropCmptRef = null;
      }
    });
    zip(windowTransition$, backdropTransition$).subscribe(() => {
      this._hidden.next();
      this._hidden.complete();
    });
  }
};
var ModalDismissReasons;
(function(ModalDismissReasons2) {
  ModalDismissReasons2[ModalDismissReasons2["BACKDROP_CLICK"] = 0] = "BACKDROP_CLICK";
  ModalDismissReasons2[ModalDismissReasons2["ESC"] = 1] = "ESC";
})(ModalDismissReasons || (ModalDismissReasons = {}));
var WINDOW_ATTRIBUTES = ["animation", "ariaLabelledBy", "ariaDescribedBy", "backdrop", "centered", "fullscreen", "keyboard", "role", "scrollable", "size", "windowClass", "modalDialogClass"];
var NgbModalWindow = class _NgbModalWindow {
  constructor() {
    this._document = inject(DOCUMENT);
    this._elRef = inject(ElementRef);
    this._zone = inject(NgZone);
    this._injector = inject(Injector);
    this._cdRef = inject(ChangeDetectorRef);
    this._closed$ = new Subject();
    this._elWithFocus = null;
    this.backdrop = true;
    this.keyboard = true;
    this.role = "dialog";
    this.dismissEvent = new EventEmitter();
    this.shown = new Subject();
    this.hidden = new Subject();
  }
  get fullscreenClass() {
    return this.fullscreen === true ? " modal-fullscreen" : isString(this.fullscreen) ? ` modal-fullscreen-${this.fullscreen}-down` : "";
  }
  dismiss(reason) {
    this.dismissEvent.emit(reason);
  }
  ngOnInit() {
    this._elWithFocus = this._document.activeElement;
    afterNextRender({
      mixedReadWrite: () => this._show()
    }, {
      injector: this._injector
    });
  }
  ngOnDestroy() {
    this._disableEventHandling();
  }
  hide() {
    const {
      nativeElement
    } = this._elRef;
    const context = {
      animation: this.animation,
      runningTransition: "stop"
    };
    const windowTransition$ = ngbRunTransition(this._zone, nativeElement, () => nativeElement.classList.remove("show"), context);
    const dialogTransition$ = ngbRunTransition(this._zone, this._dialogEl.nativeElement, () => {
    }, context);
    const transitions$ = zip(windowTransition$, dialogTransition$);
    transitions$.subscribe(() => {
      this.hidden.next();
      this.hidden.complete();
    });
    this._disableEventHandling();
    this._restoreFocus();
    return transitions$;
  }
  updateOptions(options) {
    WINDOW_ATTRIBUTES.forEach((optionName) => {
      if (isDefined(options[optionName])) {
        this[optionName] = options[optionName];
      }
    });
    this._cdRef.markForCheck();
  }
  _show() {
    const context = {
      animation: this.animation,
      runningTransition: "continue"
    };
    const windowTransition$ = ngbRunTransition(this._zone, this._elRef.nativeElement, (element, animation) => {
      if (animation) {
        reflow(element);
      }
      element.classList.add("show");
    }, context);
    const dialogTransition$ = ngbRunTransition(this._zone, this._dialogEl.nativeElement, () => {
    }, context);
    zip(windowTransition$, dialogTransition$).subscribe(() => {
      this.shown.next();
      this.shown.complete();
    });
    this._enableEventHandling();
    this._setFocus();
  }
  _enableEventHandling() {
    const {
      nativeElement
    } = this._elRef;
    this._zone.runOutsideAngular(() => {
      fromEvent(nativeElement, "keydown").pipe(takeUntil(this._closed$), filter((e) => e.key === "Escape")).subscribe((event) => {
        if (this.keyboard) {
          requestAnimationFrame(() => {
            if (!event.defaultPrevented) {
              this._zone.run(() => this.dismiss(ModalDismissReasons.ESC));
            }
          });
        } else if (this.backdrop === "static") {
          this._bumpBackdrop();
        }
      });
      let preventClose = false;
      fromEvent(this._dialogEl.nativeElement, "mousedown").pipe(takeUntil(this._closed$), tap(() => preventClose = false), switchMap(() => fromEvent(nativeElement, "mouseup").pipe(takeUntil(this._closed$), take(1))), filter(({
        target
      }) => nativeElement === target)).subscribe(() => {
        preventClose = true;
      });
      fromEvent(nativeElement, "click").pipe(takeUntil(this._closed$)).subscribe(({
        target
      }) => {
        if (nativeElement === target) {
          if (this.backdrop === "static") {
            this._bumpBackdrop();
          } else if (this.backdrop === true && !preventClose) {
            this._zone.run(() => this.dismiss(ModalDismissReasons.BACKDROP_CLICK));
          }
        }
        preventClose = false;
      });
    });
  }
  _disableEventHandling() {
    this._closed$.next();
  }
  _setFocus() {
    const {
      nativeElement
    } = this._elRef;
    if (!nativeElement.contains(document.activeElement)) {
      const autoFocusable = nativeElement.querySelector(`[ngbAutofocus]`);
      const firstFocusable = getFocusableBoundaryElements(nativeElement)[0];
      const elementToFocus = autoFocusable || firstFocusable || nativeElement;
      elementToFocus.focus();
    }
  }
  _restoreFocus() {
    const body = this._document.body;
    const elWithFocus = this._elWithFocus;
    let elementToFocus;
    if (elWithFocus && elWithFocus["focus"] && body.contains(elWithFocus)) {
      elementToFocus = elWithFocus;
    } else {
      elementToFocus = body;
    }
    this._zone.runOutsideAngular(() => {
      setTimeout(() => elementToFocus.focus());
      this._elWithFocus = null;
    });
  }
  _bumpBackdrop() {
    if (this.backdrop === "static") {
      ngbRunTransition(this._zone, this._elRef.nativeElement, ({
        classList
      }) => {
        classList.add("modal-static");
        return () => classList.remove("modal-static");
      }, {
        animation: this.animation,
        runningTransition: "continue"
      });
    }
  }
  static {
    this.\u0275fac = function NgbModalWindow_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _NgbModalWindow)();
    };
  }
  static {
    this.\u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({
      type: _NgbModalWindow,
      selectors: [["ngb-modal-window"]],
      viewQuery: function NgbModalWindow_Query(rf, ctx) {
        if (rf & 1) {
          \u0275\u0275viewQuery(_c0, 7);
        }
        if (rf & 2) {
          let _t;
          \u0275\u0275queryRefresh(_t = \u0275\u0275loadQuery()) && (ctx._dialogEl = _t.first);
        }
      },
      hostAttrs: ["tabindex", "-1"],
      hostVars: 8,
      hostBindings: function NgbModalWindow_HostBindings(rf, ctx) {
        if (rf & 2) {
          \u0275\u0275attribute("aria-modal", true)("aria-labelledby", ctx.ariaLabelledBy)("aria-describedby", ctx.ariaDescribedBy)("role", ctx.role);
          \u0275\u0275classMap("modal d-block" + (ctx.windowClass ? " " + ctx.windowClass : ""));
          \u0275\u0275classProp("fade", ctx.animation);
        }
      },
      inputs: {
        animation: "animation",
        ariaLabelledBy: "ariaLabelledBy",
        ariaDescribedBy: "ariaDescribedBy",
        backdrop: "backdrop",
        centered: "centered",
        fullscreen: "fullscreen",
        keyboard: "keyboard",
        role: "role",
        scrollable: "scrollable",
        size: "size",
        windowClass: "windowClass",
        modalDialogClass: "modalDialogClass"
      },
      outputs: {
        dismissEvent: "dismiss"
      },
      ngContentSelectors: _c1,
      decls: 4,
      vars: 2,
      consts: [["dialog", ""], ["role", "document"], [1, "modal-content"]],
      template: function NgbModalWindow_Template(rf, ctx) {
        if (rf & 1) {
          \u0275\u0275projectionDef();
          \u0275\u0275domElementStart(0, "div", 1, 0)(2, "div", 2);
          \u0275\u0275projection(3);
          \u0275\u0275domElementEnd()();
        }
        if (rf & 2) {
          \u0275\u0275classMap("modal-dialog" + (ctx.size ? " modal-" + ctx.size : "") + (ctx.centered ? " modal-dialog-centered" : "") + ctx.fullscreenClass + (ctx.scrollable ? " modal-dialog-scrollable" : "") + (ctx.modalDialogClass ? " " + ctx.modalDialogClass : ""));
        }
      },
      styles: ["ngb-modal-window .component-host-scrollable{display:flex;flex-direction:column;overflow:hidden}\n"],
      encapsulation: 2
    });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(NgbModalWindow, [{
    type: Component,
    args: [{
      selector: "ngb-modal-window",
      host: {
        "[class]": '"modal d-block" + (windowClass ? " " + windowClass : "")',
        "[class.fade]": "animation",
        tabindex: "-1",
        "[attr.aria-modal]": "true",
        "[attr.aria-labelledby]": "ariaLabelledBy",
        "[attr.aria-describedby]": "ariaDescribedBy",
        "[attr.role]": "role"
      },
      template: `
		<div
			#dialog
			[class]="
				'modal-dialog' +
				(size ? ' modal-' + size : '') +
				(centered ? ' modal-dialog-centered' : '') +
				fullscreenClass +
				(scrollable ? ' modal-dialog-scrollable' : '') +
				(modalDialogClass ? ' ' + modalDialogClass : '')
			"
			role="document"
		>
			<div class="modal-content"><ng-content /></div>
		</div>
	`,
      encapsulation: ViewEncapsulation.None,
      styles: ["ngb-modal-window .component-host-scrollable{display:flex;flex-direction:column;overflow:hidden}\n"]
    }]
  }], null, {
    _dialogEl: [{
      type: ViewChild,
      args: ["dialog", {
        static: true
      }]
    }],
    animation: [{
      type: Input
    }],
    ariaLabelledBy: [{
      type: Input
    }],
    ariaDescribedBy: [{
      type: Input
    }],
    backdrop: [{
      type: Input
    }],
    centered: [{
      type: Input
    }],
    fullscreen: [{
      type: Input
    }],
    keyboard: [{
      type: Input
    }],
    role: [{
      type: Input
    }],
    scrollable: [{
      type: Input
    }],
    size: [{
      type: Input
    }],
    windowClass: [{
      type: Input
    }],
    modalDialogClass: [{
      type: Input
    }],
    dismissEvent: [{
      type: Output,
      args: ["dismiss"]
    }]
  });
})();
var NgbModalStack = class _NgbModalStack {
  constructor() {
    this._applicationRef = inject(ApplicationRef);
    this._injector = inject(Injector);
    this._environmentInjector = inject(EnvironmentInjector);
    this._document = inject(DOCUMENT);
    this._scrollBar = inject(ScrollBar);
    this._activeWindowCmptHasChanged = new Subject();
    this._ariaHiddenValues = /* @__PURE__ */ new Map();
    this._scrollBarRestoreFn = null;
    this._modalRefs = [];
    this._windowCmpts = [];
    this._activeInstances = new EventEmitter();
    const ngZone = inject(NgZone);
    this._activeWindowCmptHasChanged.subscribe(() => {
      if (this._windowCmpts.length) {
        const activeWindowCmpt = this._windowCmpts[this._windowCmpts.length - 1];
        ngbFocusTrap(ngZone, activeWindowCmpt.location.nativeElement, this._activeWindowCmptHasChanged);
        this._revertAriaHidden();
        this._setAriaHidden(activeWindowCmpt.location.nativeElement);
      }
    });
  }
  _restoreScrollBar() {
    const scrollBarRestoreFn = this._scrollBarRestoreFn;
    if (scrollBarRestoreFn) {
      this._scrollBarRestoreFn = null;
      scrollBarRestoreFn();
    }
  }
  _hideScrollBar() {
    if (!this._scrollBarRestoreFn) {
      this._scrollBarRestoreFn = this._scrollBar.hide();
    }
  }
  open(contentInjector, content, options) {
    const containerEl = options.container instanceof HTMLElement ? options.container : isDefined(options.container) ? this._document.querySelector(options.container) : this._document.body;
    if (!containerEl) {
      throw new Error(`The specified modal container "${options.container || "body"}" was not found in the DOM.`);
    }
    this._hideScrollBar();
    const activeModal = new NgbActiveModal();
    contentInjector = options.injector || contentInjector;
    const environmentInjector = contentInjector.get(EnvironmentInjector, null) || this._environmentInjector;
    const contentRef = this._getContentRef(contentInjector, environmentInjector, content, activeModal, options);
    let backdropCmptRef = options.backdrop !== false ? this._attachBackdrop(containerEl) : void 0;
    let windowCmptRef = this._attachWindowComponent(containerEl, contentRef.nodes);
    let ngbModalRef = new NgbModalRef(windowCmptRef, contentRef, backdropCmptRef, options.beforeDismiss);
    this._registerModalRef(ngbModalRef);
    this._registerWindowCmpt(windowCmptRef);
    ngbModalRef.hidden.pipe(take(1)).subscribe(() => Promise.resolve(true).then(() => {
      if (!this._modalRefs.length) {
        this._document.body.classList.remove("modal-open");
        this._restoreScrollBar();
        this._revertAriaHidden();
      }
    }));
    activeModal.close = (result) => {
      ngbModalRef.close(result);
    };
    activeModal.dismiss = (reason) => {
      ngbModalRef.dismiss(reason);
    };
    activeModal.update = (options2) => {
      ngbModalRef.update(options2);
    };
    ngbModalRef.update(options);
    if (this._modalRefs.length === 1) {
      this._document.body.classList.add("modal-open");
    }
    if (backdropCmptRef && backdropCmptRef.instance) {
      backdropCmptRef.changeDetectorRef.detectChanges();
    }
    windowCmptRef.changeDetectorRef.detectChanges();
    return ngbModalRef;
  }
  get activeInstances() {
    return this._activeInstances;
  }
  dismissAll(reason) {
    this._modalRefs.forEach((ngbModalRef) => ngbModalRef.dismiss(reason));
  }
  hasOpenModals() {
    return this._modalRefs.length > 0;
  }
  _attachBackdrop(containerEl) {
    let backdropCmptRef = createComponent(NgbModalBackdrop, {
      environmentInjector: this._applicationRef.injector,
      elementInjector: this._injector
    });
    this._applicationRef.attachView(backdropCmptRef.hostView);
    containerEl.appendChild(backdropCmptRef.location.nativeElement);
    return backdropCmptRef;
  }
  _attachWindowComponent(containerEl, projectableNodes) {
    let windowCmptRef = createComponent(NgbModalWindow, {
      environmentInjector: this._applicationRef.injector,
      elementInjector: this._injector,
      projectableNodes
    });
    this._applicationRef.attachView(windowCmptRef.hostView);
    containerEl.appendChild(windowCmptRef.location.nativeElement);
    return windowCmptRef;
  }
  _getContentRef(contentInjector, environmentInjector, content, activeModal, options) {
    if (!content) {
      return new ContentRef([]);
    } else if (content instanceof TemplateRef) {
      return this._createFromTemplateRef(content, activeModal);
    } else if (isString(content)) {
      return this._createFromString(content);
    } else {
      return this._createFromComponent(contentInjector, environmentInjector, content, activeModal, options);
    }
  }
  _createFromTemplateRef(templateRef, activeModal) {
    const context = {
      $implicit: activeModal,
      close(result) {
        activeModal.close(result);
      },
      dismiss(reason) {
        activeModal.dismiss(reason);
      }
    };
    const viewRef = templateRef.createEmbeddedView(context);
    this._applicationRef.attachView(viewRef);
    return new ContentRef([viewRef.rootNodes], viewRef);
  }
  _createFromString(content) {
    const component = this._document.createTextNode(`${content}`);
    return new ContentRef([[component]]);
  }
  _createFromComponent(contentInjector, environmentInjector, componentType, context, options) {
    const elementInjector = Injector.create({
      providers: [{
        provide: NgbActiveModal,
        useValue: context
      }],
      parent: contentInjector
    });
    const componentRef = createComponent(componentType, {
      environmentInjector,
      elementInjector
    });
    const componentNativeEl = componentRef.location.nativeElement;
    if (options.scrollable) {
      componentNativeEl.classList.add("component-host-scrollable");
    }
    this._applicationRef.attachView(componentRef.hostView);
    return new ContentRef([[componentNativeEl]], componentRef.hostView, componentRef);
  }
  _setAriaHidden(element) {
    const parent = element.parentElement;
    if (parent && element !== this._document.body) {
      Array.from(parent.children).forEach((sibling) => {
        if (sibling !== element && sibling.nodeName !== "SCRIPT") {
          this._ariaHiddenValues.set(sibling, sibling.getAttribute("aria-hidden"));
          sibling.setAttribute("aria-hidden", "true");
        }
      });
      this._setAriaHidden(parent);
    }
  }
  _revertAriaHidden() {
    this._ariaHiddenValues.forEach((value, element) => {
      if (value) {
        element.setAttribute("aria-hidden", value);
      } else {
        element.removeAttribute("aria-hidden");
      }
    });
    this._ariaHiddenValues.clear();
  }
  _registerModalRef(ngbModalRef) {
    const unregisterModalRef = () => {
      const index = this._modalRefs.indexOf(ngbModalRef);
      if (index > -1) {
        this._modalRefs.splice(index, 1);
        this._activeInstances.emit(this._modalRefs);
      }
    };
    this._modalRefs.push(ngbModalRef);
    this._activeInstances.emit(this._modalRefs);
    ngbModalRef.result.then(unregisterModalRef, unregisterModalRef);
  }
  _registerWindowCmpt(ngbWindowCmpt) {
    this._windowCmpts.push(ngbWindowCmpt);
    this._activeWindowCmptHasChanged.next();
    ngbWindowCmpt.onDestroy(() => {
      const index = this._windowCmpts.indexOf(ngbWindowCmpt);
      if (index > -1) {
        this._windowCmpts.splice(index, 1);
        this._activeWindowCmptHasChanged.next();
      }
    });
  }
  static {
    this.\u0275fac = function NgbModalStack_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _NgbModalStack)();
    };
  }
  static {
    this.\u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({
      token: _NgbModalStack,
      factory: _NgbModalStack.\u0275fac,
      providedIn: "root"
    });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(NgbModalStack, [{
    type: Injectable,
    args: [{
      providedIn: "root"
    }]
  }], () => [], null);
})();
var NgbModal = class _NgbModal {
  constructor() {
    this._injector = inject(Injector);
    this._modalStack = inject(NgbModalStack);
    this._config = inject(NgbModalConfig);
  }
  /**
   * Opens a new modal window with the specified content and supplied options.
   *
   * Content can be provided as a `TemplateRef` or a component type. If you pass a component type as content,
   * then instances of those components can be injected with an instance of the `NgbActiveModal` class. You can then
   * use `NgbActiveModal` methods to close / dismiss modals from "inside" of your component.
   *
   * Also see the [`NgbModalOptions`](#/components/modal/api#NgbModalOptions) for the list of supported options.
   */
  open(content, options = {}) {
    const combinedOptions = __spreadValues(__spreadProps(__spreadValues({}, this._config), {
      animation: this._config.animation
    }), options);
    return this._modalStack.open(this._injector, content, combinedOptions);
  }
  /**
   * Returns an observable that holds the active modal instances.
   */
  get activeInstances() {
    return this._modalStack.activeInstances;
  }
  /**
   * Dismisses all currently displayed modal windows with the supplied reason.
   *
   * @since 3.1.0
   */
  dismissAll(reason) {
    this._modalStack.dismissAll(reason);
  }
  /**
   * Indicates if there are currently any open modal windows in the application.
   *
   * @since 3.3.0
   */
  hasOpenModals() {
    return this._modalStack.hasOpenModals();
  }
  static {
    this.\u0275fac = function NgbModal_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _NgbModal)();
    };
  }
  static {
    this.\u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({
      token: _NgbModal,
      factory: _NgbModal.\u0275fac,
      providedIn: "root"
    });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(NgbModal, [{
    type: Injectable,
    args: [{
      providedIn: "root"
    }]
  }], null, null);
})();
var NgbModalModule = class _NgbModalModule {
  static {
    this.\u0275fac = function NgbModalModule_Factory(__ngFactoryType__) {
      return new (__ngFactoryType__ || _NgbModalModule)();
    };
  }
  static {
    this.\u0275mod = /* @__PURE__ */ \u0275\u0275defineNgModule({
      type: _NgbModalModule
    });
  }
  static {
    this.\u0275inj = /* @__PURE__ */ \u0275\u0275defineInjector({
      providers: [NgbModal]
    });
  }
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(NgbModalModule, [{
    type: NgModule,
    args: [{
      providers: [NgbModal]
    }]
  }], null, null);
})();

// src/app/features/admin/users/services/user.service.ts
var UserService = class _UserService {
  http = inject(HttpClient);
  getAll() {
    return this.http.get(`${environment.apiUrl}/users`);
  }
  /** 輕量級使用者清單（不需 users:read 權限，供指定審核者下拉選單用） */
  getLookup() {
    return this.http.get(`${environment.apiUrl}/users/lookup`);
  }
  getPaged(page, pageSize) {
    return this.http.get(`${environment.apiUrl}/users`, { params: { page, pageSize } });
  }
  getById(id) {
    return this.http.get(`${environment.apiUrl}/users/${id}`);
  }
  create(data, files) {
    const formData = this.buildFormData(data, files);
    return this.http.post(`${environment.apiUrl}/users`, formData);
  }
  update(id, data, files) {
    const formData = this.buildFormData(data, files);
    if (files?.removeSignature)
      formData.append("removeSignature", "true");
    if (files?.removeAvatar)
      formData.append("removeAvatar", "true");
    if (files?.removeIndigenousProof)
      formData.append("removeIndigenousProof", "true");
    return this.http.patch(`${environment.apiUrl}/users/${id}`, formData);
  }
  delete(id) {
    return this.http.delete(`${environment.apiUrl}/users/${id}`);
  }
  sendCredentials(id) {
    return this.http.post(`${environment.apiUrl}/users/${id}/send-credentials`, {});
  }
  /** 以 JWT 取得原住民證明檔（HR 權限保護，回傳 Blob 供前端開啟） */
  getIndigenousProof(fileName) {
    return this.http.get(`${environment.apiUrl}/files/indigenous-proofs/${fileName}`, { responseType: "blob" });
  }
  buildFormData(data, files) {
    const fd = new FormData();
    for (const [key, value] of Object.entries(data)) {
      if (value === void 0 || value === null)
        continue;
      if (Array.isArray(value)) {
        value.forEach((v) => fd.append(key, String(v)));
      } else if (value instanceof Date) {
        fd.append(key, value.toISOString());
      } else {
        fd.append(key, String(value));
      }
    }
    if (files?.signatureFile)
      fd.append("signature", files.signatureFile);
    if (files?.avatarFile)
      fd.append("avatar", files.avatarFile);
    if (files?.indigenousProofFile)
      fd.append("indigenousProof", files.indigenousProofFile);
    return fd;
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

// src/app/features/admin/job-titles/services/job-title.service.ts
var JobTitleService = class _JobTitleService {
  http = inject(HttpClient);
  getAll() {
    return this.http.get(`${environment.apiUrl}/job-titles`);
  }
  /** 輕量級職稱清單（不需 job-titles:read 權限，供下拉選單用） */
  getLookup() {
    return this.http.get(`${environment.apiUrl}/job-titles/lookup`);
  }
  getById(id) {
    return this.http.get(`${environment.apiUrl}/job-titles/${id}`);
  }
  create(data) {
    return this.http.post(`${environment.apiUrl}/job-titles`, data);
  }
  update(id, changes) {
    return this.http.patch(`${environment.apiUrl}/job-titles/${id}`, changes);
  }
  delete(id) {
    return this.http.delete(`${environment.apiUrl}/job-titles/${id}`);
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

// src/app/features/admin/approvals/services/approval.service.ts
var ApprovalService = class _ApprovalService {
  http = inject(HttpClient);
  getAll() {
    return this.http.get(`${environment.apiUrl}/approval-items`);
  }
  getById(id) {
    return this.http.get(`${environment.apiUrl}/approval-items/${id}`);
  }
  create(data) {
    return this.http.post(`${environment.apiUrl}/approval-items`, data);
  }
  update(id, changes) {
    return this.http.patch(`${environment.apiUrl}/approval-items/${id}`, changes);
  }
  delete(id) {
    return this.http.delete(`${environment.apiUrl}/approval-items/${id}`);
  }
  // ── Steps ──────────────────────────────────────────────────────────────
  addStep(itemId, step) {
    return this.http.post(`${environment.apiUrl}/approval-items/${itemId}/steps`, step);
  }
  updateStep(itemId, stepId, changes) {
    return this.http.patch(`${environment.apiUrl}/approval-items/${itemId}/steps/${stepId}`, changes);
  }
  deleteStep(itemId, stepId) {
    return this.http.delete(`${environment.apiUrl}/approval-items/${itemId}/steps/${stepId}`);
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

// src/app/features/admin/projects/services/project.service.ts
var ProjectService = class _ProjectService {
  http = inject(HttpClient);
  getAll() {
    return this.http.get(`${environment.apiUrl}/projects`);
  }
  /** 取得未結案專案（不需 ProjectsRead 權限） */
  getActive() {
    return this.http.get(`${environment.apiUrl}/projects/active`);
  }
  getPaged(page, pageSize, search, year, status) {
    const params = { page, pageSize };
    if (search)
      params["search"] = search;
    if (year)
      params["year"] = year;
    if (status)
      params["status"] = status;
    return this.http.get(`${environment.apiUrl}/projects`, { params });
  }
  /** 取得所有年度（依 StartDate 年份去重） */
  getYears() {
    return this.http.get(`${environment.apiUrl}/projects/years`);
  }
  getById(id) {
    return this.http.get(`${environment.apiUrl}/projects/${id}`);
  }
  create(data) {
    return this.http.post(`${environment.apiUrl}/projects`, data);
  }
  update(id, changes) {
    return this.http.patch(`${environment.apiUrl}/projects/${id}`, changes);
  }
  delete(id) {
    return this.http.delete(`${environment.apiUrl}/projects/${id}`);
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

export {
  NgbModal,
  UserService,
  JobTitleService,
  ApprovalService,
  ProjectService
};
//# sourceMappingURL=chunk-R2JZPDEJ.js.map
