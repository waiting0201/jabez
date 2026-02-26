import { Component, CUSTOM_ELEMENTS_SCHEMA, inject, Input, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { MenuItemType } from '@core/layout/models/layout.model';
import { CommonModule } from '@angular/common';
import { NgbCollapseModule } from '@ng-bootstrap/ng-bootstrap';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';
import { LayoutService } from '@core/layout/services/layout.service';
import { scrollToElement } from '@shared/utils/layout-utils';
import { HasPermissionDirective } from '@shared/directives/has-permission.directive';
import { AuthService } from '@core/auth/services/auth.service';

@Component({
  selector: 'app-menu',
  imports: [CommonModule, NgbCollapseModule, RouterLink, HasPermissionDirective],
  templateUrl: './app-menu.html',
  schemas: [CUSTOM_ELEMENTS_SCHEMA]
})
export class AppMenuComponent implements OnInit {

  router = inject(Router)
  layout = inject(LayoutService)
  private authService = inject(AuthService)
  @Input({ required: true }) menuItems: MenuItemType[] = [];

  @ViewChild('MenuItemWithChildren', { static: true })
  menuItemWithChildren!: TemplateRef<{ item: MenuItemType }>;

  @ViewChild('MenuItem', { static: true })
  menuItem!: TemplateRef<{ item: MenuItemType }>;


  ngOnInit(): void {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.expandActivePaths(this.menuItems);
        setTimeout(() => this.scrollToActiveLink(), 50);
        if (document.documentElement.classList.contains('app-mobile-menu-open')) {
          document.documentElement.classList.remove('app-mobile-menu-open');
          this.layout.hideBackdrop();
        }
      });

    this.expandActivePaths(this.menuItems);
    setTimeout(() => this.scrollToActiveLink(), 100);
  }

  hasSubMenu(item: MenuItemType): boolean {
    return !!item.children;
  }

  expandActivePaths(items: MenuItemType[]) {
    for (const item of items) {
      if (this.hasSubMenu(item)) {
        item.isCollapsed = !this.isChildActive(item);
        this.expandActivePaths(item.children || []);
      }
    }
  }

  isChildActive(item: MenuItemType): boolean {
    if (item.url && this.router.url === item.url) return true;
    if (!item.children) return false;
    return item.children.some((child: MenuItemType) => this.isChildActive(child));
  }

  isActive(item: MenuItemType): boolean {
    return this.router.url === item.url;
  }

  scrollToActiveLink(): void {
    const activeItem = document.querySelector('[data-active-link="true"]') as HTMLElement;
    const scrollContainer = document.querySelector("#sidenav .simplebar-content-wrapper") as HTMLElement;

    if (activeItem && scrollContainer) {
      const containerRect = scrollContainer.getBoundingClientRect();
      const itemRect = activeItem.getBoundingClientRect();

      const offset = itemRect.top - containerRect.top - window.innerHeight * 0.4;

      scrollToElement(scrollContainer, scrollContainer.scrollTop + offset, 500);
    }
  }

  toggleCollaspe(item: MenuItemType, siblings: MenuItemType[] = []) {
    item.isCollapsed = !item.isCollapsed;

    if (!item.isCollapsed && Array.isArray(siblings)) {
      for (const sib of siblings) {
        if (sib !== item) sib.isCollapsed = true;
      }
    }
  }

  expandFilteredPaths(items: MenuItemType[]) {
    for (const item of items) {
      if (this.hasSubMenu(item)) {
        item.isCollapsed = false;
        this.expandFilteredPaths(item.children || []);
      }
    }
  }

  /** 判斷選單項目是否可見（遞迴檢查權限與子項目） */
  isItemVisible(item: MenuItemType): boolean {
    if (item.requiredPermission && !this.authService.hasPermission(item.requiredPermission)) {
      return false;
    }
    // 有子選單時，至少一個子項目可見才顯示母選單
    if (item.children && item.children.length > 0) {
      return item.children.some(child => this.isItemVisible(child));
    }
    return true;
  }

  /** 判斷區段標題是否可見（該區段至少有一個項目可見） */
  isTitleVisible(titleIndex: number): boolean {
    for (let i = titleIndex + 1; i < this.menuItems.length; i++) {
      if (this.menuItems[i].isTitle) break;
      if (this.isItemVisible(this.menuItems[i])) return true;
    }
    return false;
  }

}
