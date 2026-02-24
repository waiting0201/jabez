import {Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, NavigationEnd, Router, RouterOutlet} from '@angular/router';
import {provideIcons} from '@ng-icons/core';
import * as fontAwesomeSolidIcons from '@ng-icons/font-awesome/solid';
import * as fontAwesomeBrandIcons from '@ng-icons/font-awesome/brands';
import * as fontAwesomeRegularIcons from '@ng-icons/font-awesome/regular';
import {Title} from '@angular/platform-browser';
import {filter, map, mergeMap} from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  viewProviders: [provideIcons({...fontAwesomeSolidIcons, ...fontAwesomeBrandIcons, ...fontAwesomeRegularIcons})]
})
export class App implements OnInit {
  private titleService = inject(Title);
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);

  ngOnInit(): void {
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        map(() => {
          let route = this.activatedRoute;
          while (route.firstChild) {
            route = route.firstChild;
          }
          return route;
        }),
        mergeMap(route => route.data)
      )
      .subscribe(data => {
        if (data['title']) {
          this.titleService.setTitle(data['title'] + ' | Admin');
        }
      });
  }
}
