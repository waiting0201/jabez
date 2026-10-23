/**
 * 台灣縣市代碼（與後端 `Constants.TaiwanCounties` 同步，順序亦同）。
 * 用於颱風假 / 天然災害停班的「工作地縣市」：部門 `county`、員工 `workCounty`、颱風假紀錄 `county`。
 */
export interface TaiwanCounty {
  code: string;
  name: string;
}

export const TAIWAN_COUNTIES: readonly TaiwanCounty[] = [
  {code: 'TPE', name: '臺北市'},
  {code: 'NWT', name: '新北市'},
  {code: 'KEL', name: '基隆市'},
  {code: 'TAO', name: '桃園市'},
  {code: 'HSZ', name: '新竹市'},
  {code: 'HSQ', name: '新竹縣'},
  {code: 'MIA', name: '苗栗縣'},
  {code: 'TXG', name: '臺中市'},
  {code: 'CHA', name: '彰化縣'},
  {code: 'NAN', name: '南投縣'},
  {code: 'YUN', name: '雲林縣'},
  {code: 'CYI', name: '嘉義市'},
  {code: 'CYQ', name: '嘉義縣'},
  {code: 'TNN', name: '臺南市'},
  {code: 'KHH', name: '高雄市'},
  {code: 'PIF', name: '屏東縣'},
  {code: 'ILA', name: '宜蘭縣'},
  {code: 'HUA', name: '花蓮縣'},
  {code: 'TTT', name: '臺東縣'},
  {code: 'PEN', name: '澎湖縣'},
  {code: 'KIN', name: '金門縣'},
  {code: 'LIE', name: '連江縣'},
];

/** 代碼 → 中文名稱（查無回傳原值，null 回傳空字串） */
export function countyName(code: string | null | undefined): string {
  if (!code) return '';
  return TAIWAN_COUNTIES.find(c => c.code === code)?.name ?? code;
}
