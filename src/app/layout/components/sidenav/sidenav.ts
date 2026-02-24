import {Component} from '@angular/core';
import {AppMenuComponent} from '@layouts/components/sidenav/app-menu/app-menu';
import {SimplebarAngularModule} from 'simplebar-angular';
import {menuItems} from '@layouts/components/data';

@Component({
  selector: 'app-sidenav',
  imports: [
    AppMenuComponent,
    SimplebarAngularModule
  ],
  templateUrl: './sidenav.html',
  styleUrl: './sidenav.scss'
})
export class Sidenav {
  protected readonly menuItems = menuItems;
}
