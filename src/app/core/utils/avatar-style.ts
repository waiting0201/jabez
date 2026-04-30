/**
 * 頭像顯示樣式 helper：將位置百分比與縮放倍率組合成 CSS。
 * 套用於 user-form 預覽框、topbar profile-dropdown 等任何顯示員工頭像的元素。
 */
export function getAvatarStyle(x = 50, y = 50, scale = 1): { [key: string]: string } {
  const style: { [key: string]: string } = {
    'object-position': `${x}% ${y}%`,
    'transform-origin': 'center',
  };
  if (scale !== 1) {
    style['transform'] = `scale(${scale})`;
  }
  return style;
}
