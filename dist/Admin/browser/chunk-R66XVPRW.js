// src/app/features/admin/payment-requests/models/payment-request.model.ts
var PAYMENT_TYPE_LABELS = {
  vendor: "\u5EE0\u5546\u8ACB\u6B3E",
  general: "\u4E00\u822C\u8ACB\u6B3E",
  business_trip: "\u54E1\u5DE5\u516C\u51FA\u8ACB\u6B3E"
};
var PAYMENT_TYPE_CLASSES = {
  vendor: "bg-info-subtle text-info",
  general: "bg-accent-subtle text-accent",
  business_trip: "bg-primary-subtle text-primary"
};
var APPROVAL_STATUS_LABELS = {
  draft: "\u8349\u7A3F",
  pending: "\u5F85\u6838\u51C6",
  approved: "\u5DF2\u6838\u51C6",
  rejected: "\u5DF2\u62D2\u7D55",
  returned: "\u9000\u56DE\u4FEE\u6539"
};
var APPROVAL_STATUS_CLASSES = {
  draft: "bg-blue-subtle text-blue-emphasis",
  pending: "bg-warning-subtle text-warning-emphasis",
  approved: "bg-success-subtle text-success",
  rejected: "bg-danger-subtle text-danger",
  returned: "bg-secondary-subtle text-secondary"
};
var PAYMENT_STATE_LABELS = {
  unpaid: "\u5F85\u64A5\u6B3E",
  paid: "\u5DF2\u64A5\u6B3E"
};
var PAYMENT_STATE_CLASSES = {
  unpaid: "bg-warning-subtle text-warning-emphasis",
  paid: "bg-primary-subtle text-primary"
};

// src/app/features/admin/leave-requests/models/leave-request.model.ts
var LEAVE_TYPE_LABELS = {
  annual: "\u5E74\u5047(\u7279\u4F11\u5047)",
  personal: "\u4E8B\u5047",
  sick: "\u75C5\u5047",
  compensatory: "\u88DC\u4F11",
  official: "\u516C\u5047",
  marriage: "\u5A5A\u5047",
  maternity: "\u7522\u5047",
  miscarriage_3m: "\u6D41\u7522\u5047(3\u500B\u6708\u4EE5\u4E0A)",
  miscarriage_2to3m: "\u6D41\u7522\u5047(2-3\u500B\u6708)",
  miscarriage_under2m: "\u6D41\u7522\u5047(\u672A\u6EFF2\u500B\u6708)",
  prenatal_checkup: "\u7522\u6AA2\u5047",
  paternity: "\u966A\u7522\u5047",
  bereavement: "\u55AA\u5047",
  ceremonial_festival: "\u6B72\u6642\u796D\u5100\u5047",
  senior_executive: "\u9AD8\u968E\u4E3B\u7BA1\u5047"
};
var LEAVE_TIME_UNIT = {
  personal: "hour",
  sick: "hour",
  prenatal_checkup: "hour",
  paternity: "hour",
  annual: "half_day",
  compensatory: "half_day",
  senior_executive: "half_day",
  official: "day",
  marriage: "day",
  maternity: "day",
  bereavement: "day",
  ceremonial_festival: "day",
  miscarriage_3m: "day",
  miscarriage_2to3m: "day",
  miscarriage_under2m: "day"
};
function formatLeaveDuration(leaveType, hours) {
  const unit = LEAVE_TIME_UNIT[leaveType];
  if (unit === "hour")
    return `${Math.round(hours * 10) / 10} \u5C0F\u6642`;
  const days = Math.round(hours / 8 * 10) / 10;
  return `${days} \u5929`;
}
var LEAVE_TYPE_CLASSES = {
  annual: "bg-[rgba(105,159,52,0.12)] text-[#4A6B3A]",
  personal: "bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]",
  sick: "bg-[rgba(184,137,42,0.12)] text-[#B8892A]",
  compensatory: "bg-[rgba(140,115,85,0.12)] text-[#8C7355]",
  official: "bg-[rgba(74,107,58,0.12)] text-[#4A6B3A]",
  marriage: "bg-[rgba(160,64,64,0.12)] text-[#A04040]",
  maternity: "bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]",
  miscarriage_3m: "bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]",
  miscarriage_2to3m: "bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]",
  miscarriage_under2m: "bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]",
  prenatal_checkup: "bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]",
  paternity: "bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]",
  bereavement: "bg-[rgba(82,83,88,0.12)] text-[#525358]",
  ceremonial_festival: "bg-[rgba(140,115,85,0.12)] text-[#8C7355]",
  senior_executive: "bg-[rgba(105,159,52,0.12)] text-[#4A6B3A]"
};
var LEAVE_TYPE_GROUPS = [
  { label: "\u4E00\u822C\u5047\u5225", types: ["annual", "personal", "sick", "official", "compensatory"] },
  { label: "\u5A5A\u5047", types: ["marriage"] },
  { label: "\u7522\u5047\u985E\u5225", types: ["maternity", "miscarriage_3m", "miscarriage_2to3m", "miscarriage_under2m", "prenatal_checkup", "paternity"] },
  { label: "\u55AA\u5047", types: ["bereavement"] },
  { label: "\u5176\u4ED6\u5047\u5225", types: ["ceremonial_festival"] },
  // 高階主管假僅協理以上可見（實際顯示由前端依 auth.isSeniorExecutive() 過濾）
  { label: "\u9AD8\u968E\u4E3B\u7BA1\u5047", types: ["senior_executive"] }
];
var LEAVE_TYPE_DAYS_LIMIT = {
  marriage: 8,
  maternity: 56,
  miscarriage_3m: 28,
  miscarriage_2to3m: 7,
  miscarriage_under2m: 5,
  prenatal_checkup: 7,
  paternity: 7,
  ceremonial_festival: 3
};
var BEREAVEMENT_RELATIONSHIP_LABELS = {
  spouse: "\u914D\u5076",
  parent: "\u7236\u6BCD",
  adoptive_parent: "\u990A\u7236\u6BCD",
  step_parent: "\u7E7C\u7236\u6BCD",
  grandparent: "\u7956\u7236\u6BCD(\u542B\u5916\u7956\u7236\u6BCD)",
  child: "\u5B50\u5973",
  spouse_parent: "\u914D\u5076\u4E4B\u7236\u6BCD",
  spouse_adoptive_parent: "\u914D\u5076\u4E4B\u990A\u7236\u6BCD\u6216\u7E7C\u7236\u6BCD",
  great_grandparent: "\u66FE\u7956\u7236\u6BCD",
  sibling: "\u5144\u5F1F\u59CA\u59B9",
  spouse_grandparent: "\u914D\u5076\u4E4B\u7956\u7236\u6BCD"
};
var BEREAVEMENT_DAYS = {
  spouse: 8,
  parent: 8,
  adoptive_parent: 8,
  step_parent: 8,
  grandparent: 6,
  child: 6,
  spouse_parent: 6,
  spouse_adoptive_parent: 6,
  great_grandparent: 3,
  sibling: 3,
  spouse_grandparent: 3
};
var BEREAVEMENT_GROUPS = [
  { days: 8, relationships: ["spouse", "parent", "adoptive_parent", "step_parent"] },
  { days: 6, relationships: ["grandparent", "child", "spouse_parent", "spouse_adoptive_parent"] },
  { days: 3, relationships: ["great_grandparent", "sibling", "spouse_grandparent"] }
];
var APPROVAL_STATUS_LABELS2 = {
  draft: "\u8349\u7A3F",
  pending: "\u5F85\u5BE9\u6838",
  approved: "\u5DF2\u6838\u51C6",
  rejected: "\u5DF2\u62D2\u7D55",
  returned: "\u9000\u56DE\u4FEE\u6539"
};
var APPROVAL_STATUS_CLASSES2 = {
  draft: "bg-blue-subtle text-blue-emphasis",
  pending: "bg-warning-subtle text-warning-emphasis",
  approved: "bg-success-subtle text-success",
  rejected: "bg-danger-subtle text-danger",
  returned: "bg-secondary-subtle text-secondary"
};

// src/app/features/admin/approval-tasks/models/approval-task.model.ts
var TASK_STATUS_LABELS = APPROVAL_STATUS_LABELS;
var TASK_STATUS_CLASSES = APPROVAL_STATUS_CLASSES;
var PAYMENT_TYPE_LABELS2 = {
  vendor: "\u5EE0\u5546\u8ACB\u6B3E",
  general: "\u4E00\u822C\u8ACB\u6B3E",
  business_trip: "\u54E1\u5DE5\u516C\u51FA\u8ACB\u6B3E"
};
var PAYMENT_INSTALLMENT_STATUS_LABELS = {
  Unpaid: "\u672A\u64A5\u6B3E",
  PartiallyPaid: "\u90E8\u5206\u64A5\u6B3E",
  FullyPaid: "\u5DF2\u5168\u6578\u64A5\u6B3E"
};
var PAYMENT_INSTALLMENT_STATUS_CLASSES = {
  Unpaid: "bg-secondary",
  PartiallyPaid: "bg-warning",
  FullyPaid: "bg-success"
};

export {
  PAYMENT_TYPE_LABELS,
  PAYMENT_TYPE_CLASSES,
  APPROVAL_STATUS_LABELS,
  APPROVAL_STATUS_CLASSES,
  PAYMENT_STATE_LABELS,
  PAYMENT_STATE_CLASSES,
  LEAVE_TYPE_LABELS,
  LEAVE_TIME_UNIT,
  formatLeaveDuration,
  LEAVE_TYPE_CLASSES,
  LEAVE_TYPE_GROUPS,
  LEAVE_TYPE_DAYS_LIMIT,
  BEREAVEMENT_RELATIONSHIP_LABELS,
  BEREAVEMENT_DAYS,
  BEREAVEMENT_GROUPS,
  APPROVAL_STATUS_LABELS2,
  APPROVAL_STATUS_CLASSES2,
  TASK_STATUS_LABELS,
  TASK_STATUS_CLASSES,
  PAYMENT_TYPE_LABELS2,
  PAYMENT_INSTALLMENT_STATUS_LABELS,
  PAYMENT_INSTALLMENT_STATUS_CLASSES
};
//# sourceMappingURL=chunk-R66XVPRW.js.map
