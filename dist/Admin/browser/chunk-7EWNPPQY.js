// src/app/features/admin/travel-payment-requests/models/travel-payment-request.model.ts
var APPROVAL_STATUS_LABELS = {
  draft: "\u8349\u7A3F",
  pending: "\u5F85\u5BE9\u6838",
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
var ITEM_CATEGORIES = ["\u4EA4\u901A\u8CBB", "\u4F4F\u5BBF\u8CBB", "\u9910\u8CBB", "\u4EBA\u4E8B\u8CBB", "\u96DC\u652F"];

export {
  APPROVAL_STATUS_LABELS,
  APPROVAL_STATUS_CLASSES,
  ITEM_CATEGORIES
};
//# sourceMappingURL=chunk-7EWNPPQY.js.map
