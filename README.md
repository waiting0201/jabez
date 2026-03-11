1. 母資料夾 (Git 倉庫 A)：管理 CLAUDE.md、.claude 設定以及 Api 程式碼。
2. Admin (Git 倉庫 B / Submodule)：獨立管理自己的 CI/CD 流程，並連結到母專案。

日常開發流程：

當修改了 Api：
1. 直接在母資料夾 git commit 即可。

當修改了 Admin：
1. 進入 Admin 資料夾。
2. 執行 git add/commit/push（這會觸發 Admin 的 CI/CD）。
3. 回到母資料夾。
4. 執行 git add Admin。
5. 執行 git commit -m "update admin submodule reference"。
(這步就像是在母專案更新「連結點」，確保母專案紀錄的是最新版的 Admin)

換電腦開發時：
1. 只需要執行：git clone --recursive <母專案的遠端URL>
這行指令會自動幫你把「母專案」抓下來，並根據 .gitmodules 的紀錄，自動把「Admin」也抓下來放好。