export type LayoutThemeType =  'light' | 'dark'


export interface LayoutState {
  theme: LayoutThemeType
  headerFixed: boolean
  navFull: boolean
  navFixed: boolean
  navCollapsed: boolean
  navMinified: boolean
  darkNavigation: boolean
  colorblindMode: boolean
  highContrastMode: boolean
}

export type MenuItemType = {
    label: string
    isTitle?: boolean
    icon?: string
    url?: string
    badge?: {
        variant: string
        text: string
    }
    target?: string
    isDisabled?: boolean
    isSpecial?: boolean
    children?: MenuItemType[]
    isCollapsed?: boolean
    requiredPermission?: string
    /** 專案工時上線日（WorkModeService.isProjectAllocationActive）之前隱藏 */
    requiresProjectAllocation?: boolean
}
