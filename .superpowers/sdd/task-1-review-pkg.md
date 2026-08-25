# Review Package — Task 1
Base: working tree before Task 1 (uncommitted)
Head: working tree after Task 1
Commits: none (user rule)

## Stat
 Seven.Vue3/src/locales/lang/en-US.json | 3 ++-
 Seven.Vue3/src/locales/lang/ja-JP.json | 3 ++-
 Seven.Vue3/src/locales/lang/zh-CN.json | 3 ++-
 3 files changed, 6 insertions(+), 3 deletions(-)

## Diff

diff --git a/Seven.Vue3/src/locales/lang/en-US.json b/Seven.Vue3/src/locales/lang/en-US.json
index 7708166..8f1a9fd 100644
--- a/Seven.Vue3/src/locales/lang/en-US.json
+++ b/Seven.Vue3/src/locales/lang/en-US.json
@@ -49,21 +49,22 @@
     "userName": "Username",
     "password": "Password",
     "userNamePlaceholder": "Operator account",
     "passwordPlaceholder": "Password",
     "captcha": "Captcha",
     "captchaPlaceholder": "Enter the code shown",
     "captchaRefresh": "Click to refresh captcha",
     "submit": "Enter Dashboard",
     "success": "Signed in successfully",
     "failed": "Sign in failed",
-    "requestFailed": "Sign in request failed"
+    "requestFailed": "Sign in request failed",
+    "expired": "Session expired. Please sign in again"
   },
   "home": {
     "title": "Master Dashboard",
     "subtitle": "Business operations & system overview",
     "systemOk": "System Normal",
     "kpiInventory": "Inventory Qty",
     "kpiInventoryMeta": "Available quantity total",
     "kpiOccupancy": "Slot Occupancy",
     "kpiOccupancyMeta": "Across all zones",
     "kpiOnlineDevices": "Online Devices",
diff --git a/Seven.Vue3/src/locales/lang/ja-JP.json b/Seven.Vue3/src/locales/lang/ja-JP.json
index 2295ddc..593405f 100644
--- a/Seven.Vue3/src/locales/lang/ja-JP.json
+++ b/Seven.Vue3/src/locales/lang/ja-JP.json
@@ -46,21 +46,22 @@
     "footer": "Production Ready · .NET 8 + Vue 3",
     "panelTitle": "ログイン",
     "panelSubtitle": "オペレーターアカウントを入力",
     "userName": "ユーザー名",
     "password": "パスワード",
     "userNamePlaceholder": "オペレーターアカウント",
     "passwordPlaceholder": "パスワード",
     "submit": "ダッシュボードへ",
     "success": "ログイン成功",
     "failed": "ログイン失敗",
-    "requestFailed": "ログイン要求失敗"
+    "requestFailed": "ログイン要求失敗",
+    "expired": "ログインの有効期限が切れました。再度ログインしてください"
   },
   "home": {
     "title": "Master ダッシュボード",
     "subtitle": "業務稼働とシステム概要",
     "systemOk": "システム正常",
     "kpiInventory": "在庫数量",
     "kpiInventoryMeta": "出荷可能数量の合計",
     "kpiOccupancy": "棚占有率",
     "kpiOccupancyMeta": "全ゾーン",
     "kpiOnlineDevices": "オンライン設備",
diff --git a/Seven.Vue3/src/locales/lang/zh-CN.json b/Seven.Vue3/src/locales/lang/zh-CN.json
index f11b9d5..5ba9bdf 100644
--- a/Seven.Vue3/src/locales/lang/zh-CN.json
+++ b/Seven.Vue3/src/locales/lang/zh-CN.json
@@ -49,21 +49,22 @@
     "userName": "用户名",
     "password": "密码",
     "userNamePlaceholder": "操作员账号",
     "passwordPlaceholder": "登录密码",
     "captcha": "验证码",
     "captchaPlaceholder": "请输入右侧验证码",
     "captchaRefresh": "点击刷新验证码",
     "submit": "进入控制台",
     "success": "登录成功",
     "failed": "登录失败",
-    "requestFailed": "登录请求失败"
+    "requestFailed": "登录请求失败",
+    "expired": "登录已过期，请重新登录"
   },
   "home": {
     "title": "Master 控制台",
     "subtitle": "业务运行与系统状态概览",
     "systemOk": "系统正常",
     "kpiInventory": "库存总量",
     "kpiInventoryMeta": "当前可发数量汇总",
     "kpiOccupancy": "货位占用率",
     "kpiOccupancyMeta": "库区总占用",
     "kpiOnlineDevices": "在线设备",
