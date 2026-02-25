import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-app-logo',
  imports: [
    RouterLink
  ],
  host: { style: 'display: block;' },
  template: `
    <a routerLink="/dashboard" class="app-logo flex-shrink-0">
      <img src="/assets/img/jabez-logo.png" alt="Jabez" class="app-logo-img">
    </a>
  `,
  styles: ``
})
export class AppLogo {

}
