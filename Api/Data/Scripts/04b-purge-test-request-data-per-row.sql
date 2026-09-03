/*
================================================================================
 清除測試申請單與測試專案 —— 逐筆展開版
================================================================================

 與 04-purge-test-request-data.sql 的刪除範圍完全相同，只是把集合式 DELETE
 攤成「一張單一組 DELETE」，方便逐筆檢視、挑著跑、或註解掉不想刪的那幾張。

 每一組固定四行，順序不可調換：
   1~3) 清簽核足跡 ApprovalRecords / EscalationOverrides / RequestDesignatedReviewers
        —— 這三張表以 (ApplicationType, ApplicationId) 多型關聯指向 9 種申請父表，
           **沒有真 FK**，cascade 不會清。必須趕在父列消失前用 Id 比對刪掉，
           否則殘列仍掛著 ReviewerId / ReviewedById 指向 Users，
           日後刪該員工會噴 FK_RequestDesignatedReviewers_Users_ReviewerId。
   4) 刪父列本身 —— 明細、附件、分期撥款子表皆為 CASCADE，會自動一起走。

 區塊順序也不可調換：先刪子單（沖銷 ← 預支 / 出差、銷假 ← 請假），再刪母單。
 反過來的話母單的 CASCADE 會先把子單掃掉，子單的簽核足跡就變成孤兒。

 挑著跑的規則
 ------------
 · 要保留某張單 → 把它那一整組四行一起註解掉（不可只留其中幾行）。
 · 保留了某張預支 / 出差 / 請假單 → 它底下的沖銷 / 銷假單也要一併保留。
 · 保留了掛在測試專案上的單 → 檔尾的專案 DELETE 會失敗（外鍵擋住），
   請一併把該專案的 DELETE 註解掉。

 用法
 ----
 整份包在一個 transaction 裡，@Commit = 0 為空跑（跑完 ROLLBACK）。
 確認無誤後改成 1 再執行一次才會真正寫入。

 docker exec -i sqlserver /opt/mssql-tools18/bin/sqlcmd \
   -S localhost -U sa -P 'Strong@Password123' -d JabezDb -C -N -u \
   -i /tmp/04b.sql

 ⚠ 附件 blob 不會被刪（發票影像 / 報價單 / 整單附件成為孤兒 blob，本機不影響功能）。
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;   -- ← 空跑用 0；確認後改成 1 才會真正寫入

BEGIN TRANSACTION;


-- ============================================================================
-- 預支沖銷申請（子單，母單＝預支單，必須先刪）（5 張）→ WriteOffRecords
-- ============================================================================

-- WO-20260803-001  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'write_off' AND ApplicationId = 33;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'write_off' AND ApplicationId = 33;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'write_off' AND RequestId     = 33;
DELETE FROM WriteOffRecords WHERE Id = 33;

-- WO-20260812-001  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'write_off' AND ApplicationId = 34;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'write_off' AND ApplicationId = 34;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'write_off' AND RequestId     = 34;
DELETE FROM WriteOffRecords WHERE Id = 34;

-- WO-20260812-002  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'write_off' AND ApplicationId = 35;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'write_off' AND ApplicationId = 35;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'write_off' AND RequestId     = 35;
DELETE FROM WriteOffRecords WHERE Id = 35;

-- WO-20260813-002  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'write_off' AND ApplicationId = 37;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'write_off' AND ApplicationId = 37;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'write_off' AND RequestId     = 37;
DELETE FROM WriteOffRecords WHERE Id = 37;

-- WO-20260813-003  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'write_off' AND ApplicationId = 38;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'write_off' AND ApplicationId = 38;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'write_off' AND RequestId     = 38;
DELETE FROM WriteOffRecords WHERE Id = 38;

-- ============================================================================
-- 出差預支沖銷申請（子單，母單＝出差單，必須先刪）（1 張）→ TravelWriteOffRecords
-- ============================================================================

-- TWO-20260812-001  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'travel_write_off' AND ApplicationId = 5;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'travel_write_off' AND ApplicationId = 5;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'travel_write_off' AND RequestId     = 5;
DELETE FROM TravelWriteOffRecords WHERE Id = 5;

-- ============================================================================
-- 銷假申請（子單，母單＝請假單，必須先刪）（4 張）→ LeaveRevocations
-- ============================================================================

-- LVR#1  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'leave_revocation' AND ApplicationId = 1;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'leave_revocation' AND ApplicationId = 1;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'leave_revocation' AND RequestId     = 1;
DELETE FROM LeaveRevocations WHERE Id = 1;

-- LVR#2  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'leave_revocation' AND ApplicationId = 2;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'leave_revocation' AND ApplicationId = 2;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'leave_revocation' AND RequestId     = 2;
DELETE FROM LeaveRevocations WHERE Id = 2;

-- LVR#3  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'leave_revocation' AND ApplicationId = 3;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'leave_revocation' AND ApplicationId = 3;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'leave_revocation' AND RequestId     = 3;
DELETE FROM LeaveRevocations WHERE Id = 3;

-- LVR#5  [approved]  申請人：總監室專案部門測試協理
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'leave_revocation' AND ApplicationId = 5;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'leave_revocation' AND ApplicationId = 5;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'leave_revocation' AND RequestId     = 5;
DELETE FROM LeaveRevocations WHERE Id = 5;

-- ============================================================================
-- 請款申請（41 張）→ PaymentRequests
-- ============================================================================

-- PR-20260731-001  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 60;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 60;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 60;
DELETE FROM PaymentRequests WHERE Id = 60;

-- PR-20260803-001  [approved]  申請人：數位研發測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 61;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 61;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 61;
DELETE FROM PaymentRequests WHERE Id = 61;

-- PR-20260803-002  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 62;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 62;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 62;
DELETE FROM PaymentRequests WHERE Id = 62;

-- PR-20260803-003  [rejected]  申請人：楊雪
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 63;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 63;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 63;
DELETE FROM PaymentRequests WHERE Id = 63;

-- PR-20260803-004  [approved]  申請人：楊雪
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 64;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 64;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 64;
DELETE FROM PaymentRequests WHERE Id = 64;

-- PR-20260804-001  [approved]  申請人：孔德元
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 65;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 65;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 65;
DELETE FROM PaymentRequests WHERE Id = 65;

-- PR-20260804-002  [approved]  申請人：張雅婷
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 66;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 66;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 66;
DELETE FROM PaymentRequests WHERE Id = 66;

-- PR-20260804-003  [rejected]  申請人：楊雪
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 67;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 67;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 67;
DELETE FROM PaymentRequests WHERE Id = 67;

-- PR-20260804-004  [rejected]  申請人：發三規畫師
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 68;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 68;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 68;
DELETE FROM PaymentRequests WHERE Id = 68;

-- PR-20260804-005  [rejected]  申請人：發三規畫師
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 69;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 69;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 69;
DELETE FROM PaymentRequests WHERE Id = 69;

-- PR-20260804-006  [rejected]  申請人：發三規畫師
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 70;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 70;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 70;
DELETE FROM PaymentRequests WHERE Id = 70;

-- PR-20260804-007  [rejected]  申請人：發三規畫師
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 71;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 71;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 71;
DELETE FROM PaymentRequests WHERE Id = 71;

-- PR-20260805-001  [rejected]  申請人：陳婉婷
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 72;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 72;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 72;
DELETE FROM PaymentRequests WHERE Id = 72;

-- PR-20260808-001  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 73;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 73;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 73;
DELETE FROM PaymentRequests WHERE Id = 73;

-- PR-20260812-001  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 74;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 74;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 74;
DELETE FROM PaymentRequests WHERE Id = 74;

-- PR-20260813-001  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 75;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 75;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 75;
DELETE FROM PaymentRequests WHERE Id = 75;

-- PR-20260813-002  [draft]  申請人：洪薇淳
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 76;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 76;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 76;
DELETE FROM PaymentRequests WHERE Id = 76;

-- PR-20260814-001  [rejected]  申請人：洪薇淳
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 77;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 77;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 77;
DELETE FROM PaymentRequests WHERE Id = 77;

-- PR-20260814-002  [approved]  申請人：洪薇淳
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 78;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 78;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 78;
DELETE FROM PaymentRequests WHERE Id = 78;

-- PR-20260814-003  [approved]  申請人：洪薇淳
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 79;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 79;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 79;
DELETE FROM PaymentRequests WHERE Id = 79;

-- PR-20260814-004  [approved]  申請人：陳姍雯
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 80;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 80;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 80;
DELETE FROM PaymentRequests WHERE Id = 80;

-- PR-20260814-005  [approved]  申請人：陳姍雯
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 81;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 81;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 81;
DELETE FROM PaymentRequests WHERE Id = 81;

-- PR-20260814-006  [approved]  申請人：陳姍雯
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 82;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 82;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 82;
DELETE FROM PaymentRequests WHERE Id = 82;

-- PR-20260822-001  [approved]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 83;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 83;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 83;
DELETE FROM PaymentRequests WHERE Id = 83;

-- PR-20260822-002  [rejected]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 84;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 84;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 84;
DELETE FROM PaymentRequests WHERE Id = 84;

-- PR-20260822-003  [rejected]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 85;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 85;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 85;
DELETE FROM PaymentRequests WHERE Id = 85;

-- PR-20260830-001  [pending]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 87;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 87;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 87;
DELETE FROM PaymentRequests WHERE Id = 87;

-- PR-20260830-002  [pending]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 88;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 88;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 88;
DELETE FROM PaymentRequests WHERE Id = 88;

-- PR-20260830-003  [pending]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 89;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 89;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 89;
DELETE FROM PaymentRequests WHERE Id = 89;

-- PR-20260830-004  [rejected]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 90;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 90;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 90;
DELETE FROM PaymentRequests WHERE Id = 90;

-- PR-20260830-005  [returned]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 91;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 91;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 91;
DELETE FROM PaymentRequests WHERE Id = 91;

-- PR-20260830-006  [pending]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 92;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 92;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 92;
DELETE FROM PaymentRequests WHERE Id = 92;

-- PR-20260830-007  [returned]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 93;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 93;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 93;
DELETE FROM PaymentRequests WHERE Id = 93;

-- PR-20260830-008  [pending]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 94;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 94;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 94;
DELETE FROM PaymentRequests WHERE Id = 94;

-- PR-20260830-009  [pending]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 95;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 95;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 95;
DELETE FROM PaymentRequests WHERE Id = 95;

-- PR-20260830-010  [pending]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 96;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 96;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 96;
DELETE FROM PaymentRequests WHERE Id = 96;

-- PR-20260830-011  [pending]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 97;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 97;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 97;
DELETE FROM PaymentRequests WHERE Id = 97;

-- PR-20260830-012  [rejected]  申請人：徐嘉秀
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 98;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 98;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 98;
DELETE FROM PaymentRequests WHERE Id = 98;

-- PR-20260830-013  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 99;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 99;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 99;
DELETE FROM PaymentRequests WHERE Id = 99;

-- PR-20260901-001  [pending]  申請人：總監室專案部門測試協理
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 100;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 100;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 100;
DELETE FROM PaymentRequests WHERE Id = 100;

-- PR-20260901-002  [approved]  申請人：總監室專案部門測試協理
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'payment_request' AND ApplicationId = 101;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'payment_request' AND ApplicationId = 101;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'payment_request' AND RequestId     = 101;
DELETE FROM PaymentRequests WHERE Id = 101;

-- ============================================================================
-- 預審申請（全表清空）（24 張）→ PreReviewRequests
-- ============================================================================

-- PRV-20260630-001  [approved]  申請人：數位研發測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 1;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 1;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 1;
DELETE FROM PreReviewRequests WHERE Id = 1;

-- PRV-20260701-001  [approved]  申請人：品牌事業BA
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 2;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 2;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 2;
DELETE FROM PreReviewRequests WHERE Id = 2;

-- PRV-20260701-002  [approved]  申請人：?
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 3;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 3;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 3;
DELETE FROM PreReviewRequests WHERE Id = 3;

-- PRV-20260701-003  [approved]  申請人：?
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 4;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 4;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 4;
DELETE FROM PreReviewRequests WHERE Id = 4;

-- PRV-20260701-004  [approved]  申請人：?
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 5;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 5;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 5;
DELETE FROM PreReviewRequests WHERE Id = 5;

-- PRV-20260701-005  [approved]  申請人：太平洋經理PA
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 6;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 6;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 6;
DELETE FROM PreReviewRequests WHERE Id = 6;

-- PRV-20260701-006  [approved]  申請人：發三規畫師
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 7;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 7;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 7;
DELETE FROM PreReviewRequests WHERE Id = 7;

-- PRV-20260701-007  [approved]  申請人：發一部協理測試d1
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 8;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 8;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 8;
DELETE FROM PreReviewRequests WHERE Id = 8;

-- PRV-20260701-008  [approved]  申請人：執行長測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 9;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 9;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 9;
DELETE FROM PreReviewRequests WHERE Id = 9;

-- PRV-20260701-009  [pending]  申請人：會計測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 10;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 10;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 10;
DELETE FROM PreReviewRequests WHERE Id = 10;

-- PRV-20260701-010  [approved]  申請人：會計測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 11;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 11;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 11;
DELETE FROM PreReviewRequests WHERE Id = 11;

-- PRV-20260701-011  [approved]  申請人：財務主管測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 12;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 12;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 12;
DELETE FROM PreReviewRequests WHERE Id = 12;

-- PRV-20260701-012  [approved]  申請人：數位研發測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 13;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 13;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 13;
DELETE FROM PreReviewRequests WHERE Id = 13;

-- PRV-20260701-013  [approved]  申請人：數位研發測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 14;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 14;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 14;
DELETE FROM PreReviewRequests WHERE Id = 14;

-- PRV-20260702-001  [approved]  申請人：數位研發測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 15;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 15;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 15;
DELETE FROM PreReviewRequests WHERE Id = 15;

-- PRV-20260706-001  [approved]  申請人：?
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 16;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 16;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 16;
DELETE FROM PreReviewRequests WHERE Id = 16;

-- PRV-20260725-001  [approved]  申請人：品牌事業部主管測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 17;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 17;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 17;
DELETE FROM PreReviewRequests WHERE Id = 17;

-- PRV-20260802-001  [approved]  申請人：發三規畫師
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 18;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 18;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 18;
DELETE FROM PreReviewRequests WHERE Id = 18;

-- PRV-20260802-002  [approved]  申請人：陳婉婷
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 19;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 19;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 19;
DELETE FROM PreReviewRequests WHERE Id = 19;

-- PRV-20260803-001  [approved]  申請人：數位研發測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 20;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 20;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 20;
DELETE FROM PreReviewRequests WHERE Id = 20;

-- PRV-20260813-001  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 21;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 21;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 21;
DELETE FROM PreReviewRequests WHERE Id = 21;

-- PRV-20260825-001  [returned]  申請人：執行長測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 22;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 22;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 22;
DELETE FROM PreReviewRequests WHERE Id = 22;

-- PRV-20260825-002  [approved]  申請人：洪薇淳
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 23;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 23;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 23;
DELETE FROM PreReviewRequests WHERE Id = 23;

-- PRV-20260901-001  [rejected]  申請人：品牌事業BA
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'pre_review' AND ApplicationId = 24;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'pre_review' AND ApplicationId = 24;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'pre_review' AND RequestId     = 24;
DELETE FROM PreReviewRequests WHERE Id = 24;

-- ============================================================================
-- 預支申請（7 張）→ AdvanceRequests
-- ============================================================================

-- ADV-20260803-001  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'advance' AND ApplicationId = 32;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'advance' AND ApplicationId = 32;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'advance' AND RequestId     = 32;
DELETE FROM AdvanceRequests WHERE Id = 32;

-- ADV-20260812-001  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'advance' AND ApplicationId = 33;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'advance' AND ApplicationId = 33;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'advance' AND RequestId     = 33;
DELETE FROM AdvanceRequests WHERE Id = 33;

-- ADV-20260812-002  [approved]  申請人：洪薇淳
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'advance' AND ApplicationId = 34;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'advance' AND ApplicationId = 34;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'advance' AND RequestId     = 34;
DELETE FROM AdvanceRequests WHERE Id = 34;

-- ADV-20260812-003  [approved]  申請人：陳姍雯
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'advance' AND ApplicationId = 35;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'advance' AND ApplicationId = 35;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'advance' AND RequestId     = 35;
DELETE FROM AdvanceRequests WHERE Id = 35;

-- ADV-20260813-001  [rejected]  申請人：會計測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'advance' AND ApplicationId = 36;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'advance' AND ApplicationId = 36;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'advance' AND RequestId     = 36;
DELETE FROM AdvanceRequests WHERE Id = 36;

-- ADV-20260813-002  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'advance' AND ApplicationId = 37;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'advance' AND ApplicationId = 37;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'advance' AND RequestId     = 37;
DELETE FROM AdvanceRequests WHERE Id = 37;

-- ADV-20260901-004  [rejected]  申請人：洪薇淳
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'advance' AND ApplicationId = 41;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'advance' AND ApplicationId = 41;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'advance' AND RequestId     = 41;
DELETE FROM AdvanceRequests WHERE Id = 41;

-- ============================================================================
-- 出差預支申請（1 張）→ TravelRequests
-- ============================================================================

-- TR-20260812-001  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'travel' AND ApplicationId = 20;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'travel' AND ApplicationId = 20;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'travel' AND RequestId     = 20;
DELETE FROM TravelRequests WHERE Id = 20;

-- ============================================================================
-- 假日執行活動申請（3 張）→ TravelRequests
-- ============================================================================

-- HTR-20260701-001  [approved]  申請人：品牌事業BA
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'holiday_travel' AND ApplicationId = 17;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'holiday_travel' AND ApplicationId = 17;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'holiday_travel' AND RequestId     = 17;
DELETE FROM TravelRequests WHERE Id = 17;

-- HTR-20260812-001  [rejected]  申請人：張雅婷
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'holiday_travel' AND ApplicationId = 18;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'holiday_travel' AND ApplicationId = 18;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'holiday_travel' AND RequestId     = 18;
DELETE FROM TravelRequests WHERE Id = 18;

-- HTR-20260812-002  [returned]  申請人：簡子珮
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'holiday_travel' AND ApplicationId = 19;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'holiday_travel' AND ApplicationId = 19;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'holiday_travel' AND RequestId     = 19;
DELETE FROM TravelRequests WHERE Id = 19;

-- ============================================================================
-- 出差請款申請（3 張）→ TravelPaymentRequests
-- ============================================================================

-- TPR-20260812-001  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'travel_payment' AND ApplicationId = 2;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'travel_payment' AND ApplicationId = 2;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'travel_payment' AND RequestId     = 2;
DELETE FROM TravelPaymentRequests WHERE Id = 2;

-- TPR-20260813-001  [approved]  申請人：陳姍雯
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'travel_payment' AND ApplicationId = 3;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'travel_payment' AND ApplicationId = 3;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'travel_payment' AND RequestId     = 3;
DELETE FROM TravelPaymentRequests WHERE Id = 3;

-- TPR-20260813-002  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'travel_payment' AND ApplicationId = 4;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'travel_payment' AND ApplicationId = 4;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'travel_payment' AND RequestId     = 4;
DELETE FROM TravelPaymentRequests WHERE Id = 4;

-- ============================================================================
-- 加班申請（20 張）→ OvertimeRequests
-- ============================================================================

-- OT#39  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 39;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 39;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 39;
DELETE FROM OvertimeRequests WHERE Id = 39;

-- OT#40  [approved]  申請人：財務主管測試
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 40;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 40;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 40;
DELETE FROM OvertimeRequests WHERE Id = 40;

-- OT#41  [approved]  申請人：陳姍雯
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 41;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 41;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 41;
DELETE FROM OvertimeRequests WHERE Id = 41;

-- OT#42  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 42;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 42;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 42;
DELETE FROM OvertimeRequests WHERE Id = 42;

-- OT#43  [approved]  申請人：品牌事業BA
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 43;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 43;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 43;
DELETE FROM OvertimeRequests WHERE Id = 43;

-- OT#44  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 44;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 44;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 44;
DELETE FROM OvertimeRequests WHERE Id = 44;

-- OT#47  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 47;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 47;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 47;
DELETE FROM OvertimeRequests WHERE Id = 47;

-- OT#48  [rejected]  申請人：陳婉婷
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 48;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 48;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 48;
DELETE FROM OvertimeRequests WHERE Id = 48;

-- OT#49  [rejected]  申請人：陳婉婷
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 49;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 49;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 49;
DELETE FROM OvertimeRequests WHERE Id = 49;

-- OT#50  [approved]  申請人：陳麗安
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 50;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 50;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 50;
DELETE FROM OvertimeRequests WHERE Id = 50;

-- OT#52  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 52;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 52;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 52;
DELETE FROM OvertimeRequests WHERE Id = 52;

-- OT#53  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 53;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 53;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 53;
DELETE FROM OvertimeRequests WHERE Id = 53;

-- OT#54  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 54;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 54;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 54;
DELETE FROM OvertimeRequests WHERE Id = 54;

-- OT#55  [pending]  申請人：董修慈
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 55;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 55;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 55;
DELETE FROM OvertimeRequests WHERE Id = 55;

-- OT#56  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 56;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 56;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 56;
DELETE FROM OvertimeRequests WHERE Id = 56;

-- OT#57  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 57;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 57;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 57;
DELETE FROM OvertimeRequests WHERE Id = 57;

-- OT#59  [rejected]  申請人：張雅婷
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 59;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 59;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 59;
DELETE FROM OvertimeRequests WHERE Id = 59;

-- OT#61  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 61;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 61;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 61;
DELETE FROM OvertimeRequests WHERE Id = 61;

-- OT#63  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 63;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 63;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 63;
DELETE FROM OvertimeRequests WHERE Id = 63;

-- OT#76  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'overtime' AND ApplicationId = 76;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'overtime' AND ApplicationId = 76;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'overtime' AND RequestId     = 76;
DELETE FROM OvertimeRequests WHERE Id = 76;

-- ============================================================================
-- 請假申請（6 張）→ LeaveRequests
-- ============================================================================

-- LV#38  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'leave' AND ApplicationId = 38;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'leave' AND ApplicationId = 38;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'leave' AND RequestId     = 38;
DELETE FROM LeaveRequests WHERE Id = 38;

-- LV#39  [cancelled]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'leave' AND ApplicationId = 39;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'leave' AND ApplicationId = 39;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'leave' AND RequestId     = 39;
DELETE FROM LeaveRequests WHERE Id = 39;

-- LV#41  [approved]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'leave' AND ApplicationId = 41;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'leave' AND ApplicationId = 41;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'leave' AND RequestId     = 41;
DELETE FROM LeaveRequests WHERE Id = 41;

-- LV#44  [rejected]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'leave' AND ApplicationId = 44;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'leave' AND ApplicationId = 44;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'leave' AND RequestId     = 44;
DELETE FROM LeaveRequests WHERE Id = 44;

-- LV#45  [cancelled]  申請人：Charles
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'leave' AND ApplicationId = 45;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'leave' AND ApplicationId = 45;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'leave' AND RequestId     = 45;
DELETE FROM LeaveRequests WHERE Id = 45;

-- LV#50  [cancelled]  申請人：總監室專案部門測試協理
DELETE FROM ApprovalRecords            WHERE ApplicationType = 'leave' AND ApplicationId = 50;
DELETE FROM EscalationOverrides        WHERE ApplicationType = 'leave' AND ApplicationId = 50;
DELETE FROM RequestDesignatedReviewers WHERE RequestType     = 'leave' AND RequestId     = 50;
DELETE FROM LeaveRequests WHERE Id = 50;


-- ============================================================================
-- 測試專案（5 個）→ Projects
-- ============================================================================
-- ProjectPaymentSchedules / PaymentRequests / PreReviewRequests / AdvanceRequests
-- 對 Projects 是 CASCADE；TravelRequests / TravelPaymentRequests 是 SET NULL。
-- 但 OvertimeRequestProjects.ProjectId 是 NO_ACTION（雙 FK 子表的第二主檔，
-- 見 backend-design.md §7.5），殘一列就會擋住整個 DELETE —— 上面已把引用測試專案的
-- 加班單整張刪掉（cascade 連帶清掉明細），下面這行只為保險，正常會是 0 筆。

DELETE FROM OvertimeRequestProjects
WHERE ProjectId IN (SELECT Id FROM Projects WHERE Code IN ('P2026CoreTest','D1test','D2test','D3test','Digitaltest'));

DELETE FROM Projects WHERE Code = 'P2026CoreTest';   -- 2026e化測試
DELETE FROM Projects WHERE Code = 'D1test';          -- 發一部專案測試
DELETE FROM Projects WHERE Code = 'D2test';          -- 發二部專案測試
DELETE FROM Projects WHERE Code = 'D3test';          -- 發三部專案測試
DELETE FROM Projects WHERE Code = 'Digitaltest';     -- 數位部門專案測試


-- ============================================================================
-- 收尾
-- ============================================================================
IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT N'✅ 已 COMMIT，變更已寫入資料庫。';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT N'🔍 空跑模式（@Commit = 0），已 ROLLBACK，資料庫未變更。';
    PRINT N'   確認無誤後把 @Commit 改成 1 再執行一次。';
END
