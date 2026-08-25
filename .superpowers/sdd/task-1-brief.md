### Task 1: 澧炲姞 `login.expired` 涓夎鏂囨

**Files:**
- Modify: `Seven.Master/Seven.Vue3/src/locales/lang/zh-CN.json`
- Modify: `Seven.Master/Seven.Vue3/src/locales/lang/en-US.json`
- Modify: `Seven.Master/Seven.Vue3/src/locales/lang/ja-JP.json`

**Interfaces:**
- Consumes: 鏃?
- Produces: i18n key `login.expired`锛堜笁璇█锛?

- [ ] **Step 1: 鍦?zh-CN `login` 瀵硅薄鏈熬澧炲姞閿?*

鍦?`requestFailed` 鍚庡鍔狅細

```json
"requestFailed": "鐧诲綍璇锋眰澶辫触",
"expired": "鐧诲綍宸茶繃鏈燂紝璇烽噸鏂扮櫥褰?
```

- [ ] **Step 2: 鍦?en-US `login` 瀵硅薄鏈熬澧炲姞閿?*

```json
"requestFailed": "Sign in request failed",
"expired": "Session expired. Please sign in again"
```

- [ ] **Step 3: 鍦?ja-JP `login` 瀵硅薄鏈熬澧炲姞閿?*

```json
"requestFailed": "銉偘銈ゃ兂瑕佹眰澶辨晽",
"expired": "銉偘銈ゃ兂銇湁鍔规湡闄愩亴鍒囥倢銇俱仐銇熴€傚啀搴︺儹銈般偆銉炽仐銇︺亸銇犮仌銇?
```

- [ ] **Step 4: 鎵嬪伐鏍稿 JSON 鍚堟硶**

鍦?`Seven.Master/Seven.Vue3` 鐩綍杩愯锛?

```powershell
node -e "JSON.parse(require('fs').readFileSync('src/locales/lang/zh-CN.json','utf8')); JSON.parse(require('fs').readFileSync('src/locales/lang/en-US.json','utf8')); JSON.parse(require('fs').readFileSync('src/locales/lang/ja-JP.json','utf8')); console.log('ok')"
```

Expected: 鎵撳嵃 `ok`锛屾棤 SyntaxError

---

