import {Component, inject} from '@angular/core';
import {LayoutService} from '@core/layout/services/layout.service';

@Component({
  selector: 'app-toggle-sidenav',
  imports: [],
  template: `
    <button type="button" class="collapse-icon mr-3 hidden lg:inline-flex xl:inline-flex 2xl:inline-flex"
          (click)="toggleSidenav()">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 5 8">
        <polygon fill="#878787" points="4.5,1 3.8,0.2 0,4 3.8,7.8 4.5,7 1.5,4"/>
      </svg>
    </button>
  `,
  styles: ``
})

export class ToggleSidenav {
  private layoutStore = inject(LayoutService);

  toggleSidenav() {
    this.layoutStore.toggleSetting('navMinified', !this.layoutStore.navMinified);
  }
}
