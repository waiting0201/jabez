import {ChangeDetectorRef, Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {DomSanitizer, SafeResourceUrl} from '@angular/platform-browser';
import {createWorker, PSM} from 'tesseract.js';
import {PaymentRequestService} from '../../services/payment-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {PaymentType, ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/payment-request.model';

@Component({
  selector: 'app-payment-form',
  templateUrl: './payment-form.html',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
})
export class PaymentForm implements OnInit {
  private fb        = inject(FormBuilder);
  private service   = inject(PaymentRequestService);
  private projects$ = inject(ProjectService);
  private route     = inject(ActivatedRoute);
  private router    = inject(Router);
  private cdr       = inject(ChangeDetectorRef);
  private sanitizer = inject(DomSanitizer);

  projects: Project[] = [];
  isEdit     = false;
  isReturned = false;
  requestId  = 0;
  showInvoiceError = false;
  approvalStatus: ApprovalStatus = 'pending';

  /** IDs of invoice rows currently being OCR-processed */
  ocrLoadingIds = new Set<string>();
  get isAnyOcrPending(): boolean { return this.ocrLoadingIds.size > 0; }

  /** File preview modal state */
  previewFile: {name: string; url: string; safeUrl: SafeResourceUrl} | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }
  isImageFile(name: string) { return /\.(jpe?g|png|gif|webp|bmp)$/i.test(name); }
  isPdfFile(name: string)   { return /\.pdf$/i.test(name); }

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  form = this.fb.group({
    type:      ['vendor', Validators.required],
    projectId: [null as number | null, Validators.required],
    invoices:  this.fb.array([]),
  });

  get invoiceArray(): FormArray { return this.form.get('invoices') as FormArray; }
  get invoiceControls(): AbstractControl[] { return this.invoiceArray.controls; }
  get totalAmount(): number {
    return this.invoiceArray.controls.reduce((sum, c) => sum + (+(c.get('amount')?.value) || 0), 0);
  }

  ngOnInit() {
    this.projects$.getAll().subscribe(p => this.projects = p);
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.approvalStatus = r.approvalStatus;
        this.isReturned     = r.approvalStatus === 'returned';
        const isReadOnly    = r.approvalStatus !== 'pending' && r.approvalStatus !== 'returned';
        if (isReadOnly) this.form.disable();
        this.form.patchValue({type: r.type, projectId: r.projectId});
        r.invoices.forEach(inv => this.invoiceArray.push(
          this._invoiceGroup(inv.id, inv.fileName, inv.invoiceNo, inv.amount, inv.previewUrl ?? '')
        ));
      });
    }
  }

  async onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const files = Array.from(input.files);
    input.value = '';
    this.showInvoiceError = false;

    // Add all rows immediately as "loading" placeholders; create blob URL for preview
    const entries = files.map(file => {
      const id         = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      const previewUrl = URL.createObjectURL(file);
      this.ocrLoadingIds.add(id);
      this.invoiceArray.push(this._invoiceGroup(id, file.name, '', 0, previewUrl));
      return {id, file};
    });

    // Two workers initialised once per batch — each locked to its own PSM mode.
    // AUTO: better for structured large-text blocks (invoice number).
    // SPARSE_TEXT: better for scattered small text (amount line between barcodes).
    const [wAuto, wSparse] = await Promise.all([
      createWorker(['eng', 'chi_tra']),
      createWorker(['eng', 'chi_tra']),
    ]);
    await wSparse.setParameters({tessedit_pageseg_mode: PSM.SPARSE_TEXT});
    try {
      for (const {id, file} of entries) {
        try {
          const [{data: {text: textAuto}}, {data: {text: textSparse}}] =
            await Promise.all([wAuto.recognize(file), wSparse.recognize(file)]);

          console.log('[OCR AUTO]', textAuto);
          console.log('[OCR SPARSE]', textSparse);

          const auto   = this._extractInvoiceData(textAuto);
          const sparse = this._extractInvoiceData(textSparse);
          const invoiceNo = auto.invoiceNo || sparse.invoiceNo;
          const amount    = auto.amount    || sparse.amount;
          const idx = this.invoiceArray.controls.findIndex(c => c.get('id')?.value === id);
          if (idx >= 0) this.invoiceArray.controls[idx].patchValue({invoiceNo, amount});
        } catch {
          // OCR failed — leave fields empty for manual entry
        } finally {
          this.ocrLoadingIds.delete(id);
          this.cdr.markForCheck();
        }
      }
    } finally {
      await Promise.all([wAuto.terminate(), wSparse.terminate()]);
    }
  }

  removeInvoice(i: number) {
    const url = this.invoiceArray.at(i).get('previewUrl')?.value as string;
    if (url?.startsWith('blob:')) URL.revokeObjectURL(url);
    this.invoiceArray.removeAt(i);
  }

  submit() {
    if (this.form.invalid) return;
    if (this.invoiceArray.length === 0) {this.showInvoiceError = true; return;}
    this.showInvoiceError = false;
    const v = this.form.value;
    const project = this.projects.find(p => p.id === v.projectId);
    const invoices = this.invoiceArray.value.map((inv: any) => ({...inv, amount: +inv.amount}));
    const payload = {
      type:           v.type as PaymentType,
      projectId:      v.projectId!,
      projectCode:    project?.code ?? '',
      invoices,
      totalAmount:    this.totalAmount,
      approvalStatus: this.approvalStatus,
    };
    const obs = this.isEdit ? this.service.update(this.requestId, payload) : this.service.create(payload);
    obs.subscribe(() => this.router.navigate(['/admin/payment-requests']));
  }

  private _invoiceGroup(id: string, fileName: string, invoiceNo: string, amount: number, previewUrl = '') {
    return this.fb.group({
      id:         [id],
      fileName:   [fileName],
      invoiceNo:  [invoiceNo, Validators.required],
      amount:     [amount, [Validators.required, Validators.min(0)]],
      previewUrl: [previewUrl],
    });
  }

  /** Extract Taiwanese invoice number and total amount from raw OCR text. */
  private _extractInvoiceData(text: string): {invoiceNo: string; amount: number} {
    // Invoice number（統一發票號碼）: 2 uppercase letters + optional separator + 8 digits
    const noMatch = text.match(/[A-Z]{2}[-\s]?\d{8}/);
    let invoiceNo = '';
    if (noMatch) {
      invoiceNo = noMatch[0].replace(/[-\s]/g, '');
    }

    // Amount: try common invoice label patterns, then NT$ prefix.
    // [^0-9]{0,20} as separator tolerates mis-read colons, extra spaces, etc.
    // Two passes per label: first exact match, then with \s? between characters
    // to tolerate OCR inserting spaces within Chinese words (e.g. "總 計").
    const patterns = [
      /(?:合計|總計|總金額|應付金額|應付款項|小計)[^0-9]{0,20}([\d,]+)/,
      /(?:合\s*計|總\s*計|總\s*金\s*額|應\s*付\s*金\s*額|應\s*付\s*款\s*項|小\s*計)[^0-9]{0,20}([\d,]+)/,
      /金額[^0-9]{0,10}([\d,]+)/,
      /金\s*額[^0-9]{0,10}([\d,]+)/,
      /(?:Amount|Total)[^0-9]{0,10}([\d,]+)/i,
      /NT\$\s*([\d,]+)/,
      /\$\s*([\d,]+)/,
    ];
    let amount = 0;
    for (const p of patterns) {
      const m = text.match(p);
      if (m) {amount = parseInt(m[1].replace(/,/g, ''), 10); break;}
    }

    return {invoiceNo, amount};
  }
}
