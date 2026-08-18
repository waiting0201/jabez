/** ==================== 列舉常數 ==================== */

export const GENDERS = [
  { value: 'M', label: '男' },
  { value: 'F', label: '女' },
] as const;

export const MARITAL_STATUSES = [
  { value: 'single',   label: '未婚' },
  { value: 'married',  label: '已婚' },
  { value: 'divorced', label: '離婚' },
  { value: 'widowed',  label: '喪偶' },
] as const;

export const DEGREE_OPTIONS = [
  { value: 'graduated',  label: '畢業' },
  { value: 'incomplete', label: '肄業' },
] as const;

export const LANGUAGE_LEVELS = [
  { value: 'good', label: '佳' },
  { value: 'fair', label: '可' },
] as const;

export const REWARD_PUNISHMENT_TYPES = [
  { value: 'reward',     label: '獎勵' },
  { value: 'punishment', label: '懲處' },
] as const;

export const DEPENDENT_RELATIONSHIPS = [
  { value: 'spouse',       label: '配偶' },
  { value: 'father',       label: '父' },
  { value: 'mother',       label: '母' },
  { value: 'son',          label: '子' },
  { value: 'daughter',     label: '女' },
  { value: 'father_in_law',label: '公（公公）' },
  { value: 'mother_in_law',label: '婆（婆婆）' },
  { value: 'father_in_law_wife', label: '翁（岳父）' },
  { value: 'mother_in_law_wife', label: '姑（岳母）' },
  { value: 'other',        label: '其他' },
] as const;

export const FAMILY_RELATIONSHIPS = [
  { value: 'spouse',      label: '配偶' },
  { value: 'father',      label: '父' },
  { value: 'mother',      label: '母' },
  { value: 'son',         label: '子' },
  { value: 'daughter',    label: '女' },
  { value: 'sibling',     label: '兄弟姊妹' },
  { value: 'grandparent', label: '祖父母' },
  { value: 'other',       label: '其他' },
] as const;

/** ==================== 子表 Interface ==================== */

export interface EducationRecord {
  id?: string | null;
  userId?: string;
  school: string;
  department: string;
  degree: string;  // 'graduated' | 'incomplete'
  startDate?: string | null;
  endDate?: string | null;
  order: number;
}

export interface EmploymentHistoryRecord {
  id?: string | null;
  userId?: string;
  organization: string;
  jobTitle: string;
  startDate?: string | null;
  endDate?: string | null;
  order: number;
}

export interface FamilyMember {
  id?: string | null;
  userId?: string;
  name: string;
  relationship: string;
  age?: number | null;
  occupation: string;
}

export interface ProfessionalTraining {
  id?: string | null;
  userId?: string;
  trainingName: string;
  trainingOrg: string;
  startDate?: string | null;
  endDate?: string | null;
  hours?: number | null;
}

export interface LanguageAbility {
  id?: string | null;
  userId?: string;
  language: string;
  listening: string;  // 'good' | 'fair'
  speaking: string;
  reading: string;
  writing: string;
}

export interface JobTransferRecord {
  id?: string | null;
  userId?: string;
  effectiveDate?: string | null;
  fromDepartment: string;
  toDepartment: string;
  fromJobTitle: string;
  toJobTitle: string;
}

export interface RewardPunishmentRecord {
  id?: string | null;
  userId?: string;
  effectiveDate?: string | null;
  type: string;  // 'reward' | 'punishment'
  category: string;
  count?: number | null;
  reason: string;
}

export interface SalaryAdjustmentRecord {
  id?: string | null;
  userId?: string;
  effectiveDate?: string | null;
  baseSalary?: number | null;
  otherAllowance?: number | null;
  adjustmentDifference?: number | null;
  mealAllowance?: number | null;
  totalAmount?: number | null;
  notes?: string | null;
}

export interface HealthInsuranceDependent {
  id?: string | null;
  userId?: string;
  name: string;
  relationship: string;
  idNumber?: string | null;
  birthDate?: string | null;
}

/** ==================== 主表 Interface ==================== */

export interface EmployeeProfileDetail {
  userId: string;
  // 基本補充
  employeeNumber?: string | null;
  englishName?: string | null;
  idNumber?: string | null;
  gender?: string | null;         // 'M' | 'F'
  maritalStatus?: string | null;  // 'single' | 'married' | 'divorced' | 'widowed'
  birthPlace?: string | null;
  mobilePhone?: string | null;
  // 戶籍 & 通訊
  residentialAddress?: string | null;
  residentialPhone?: string | null;
  mailingAddress?: string | null;
  mailingPhone?: string | null;
  // 緊急聯絡
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  // 銀行
  bankCode?: string | null;
  bankAccount?: string | null;
  // 保險
  insuranceStartDate?: string | null;
  dependentCount?: number | null;
  // 其他
  specialties?: string | null;
  resignationReason?: string | null;
  // 身分證影本
  idCardFrontUrl?: string | null;
  idCardBackUrl?: string | null;
  // 最高學歷證明
  highestEducationProofUrl?: string | null;
  // 存摺封面
  bankBookImageUrl?: string | null;
  // 子表
  educationRecords: EducationRecord[];
  employmentHistoryRecords: EmploymentHistoryRecord[];
  familyMembers: FamilyMember[];
  professionalTrainings: ProfessionalTraining[];
  languageAbilities: LanguageAbility[];
  jobTransferRecords: JobTransferRecord[];
  rewardPunishmentRecords: RewardPunishmentRecord[];
  salaryAdjustmentRecords: SalaryAdjustmentRecord[];
  healthInsuranceDependents: HealthInsuranceDependent[];
}

/** Upsert payload（與 backend EmployeeProfileUpsertRequest 相同結構） */
export type EmployeeProfileUpsertRequest = Omit<EmployeeProfileDetail, 'userId'>;
