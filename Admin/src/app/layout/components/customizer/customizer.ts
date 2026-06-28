import {Component, inject} from '@angular/core';
import {LayoutService} from '@core/layout/services/layout.service';
import {LayoutState} from '@core/layout/models/layout.model';
import {NgbActiveOffcanvas} from '@ng-bootstrap/ng-bootstrap';
import {SimplebarAngularModule} from 'simplebar-angular';

@Component({
  selector: 'app-customizer',
  imports: [
    SimplebarAngularModule
  ],
  templateUrl: './customizer.html',
  styles: ``
})
export class Customizer {
  private offcanvas = inject(NgbActiveOffcanvas)

  close() {
    this.offcanvas.dismiss()
  }
  constructor(public layout: LayoutService) {
  }

  toggle(key: keyof LayoutState, e: Event) {
    const val = (e.target as HTMLInputElement).checked;
    this.layout.toggleSetting(key, val);
    if (key === 'navCollapsed' && val && !this.layout.state().navFull) {
      this.layout.toggleSetting('navFull', true);
    }
  }

  resetAll() {
    this.layout.reset();
  }
}
