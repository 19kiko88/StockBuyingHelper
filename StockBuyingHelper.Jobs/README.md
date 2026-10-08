# StockBuyingHelper.Jobs

在本機(Windows 工作排程器)執行的排程程式集合。目前有三個任務,用 `--mode` 參數選擇:

| mode | 說明 |
|---|---|
| `daily`(預設) | 收盤後抓證交所當日全部股票行情,寫入本機 SQLite,清理舊資料,再把每檔股票的 52 週最高/最低價上傳到 VPS 上的 Web |
| `backfill` | 一次性回補歷史資料(啟用 `daily` 排程前先跑一次) |
| `ETF0050` | 用 Selenium 抓 0050 成分股清單,上傳到 VPS 上的 Web(需要本機安裝 Chrome) |

資料流向:

```
證交所 MI_INDEX ──> 本機 SQLite(完整歷史) ──計算 52 週高低價──> POST 到 VPS Web ──> Web 讀取最新的 CSV
```

VPS 上的 SQL Server 不需要連線,本機與 Web 之間只透過下方「上傳 API」溝通。

## 專案結構

```
Program.cs            讀設定、建立共用物件、依 --mode 執行對應任務
Runners/              各個任務,皆實作 IJob(DailyRunner、BackfillRunner、Get0050ListRunner)
Clients/              對外部系統的連線:TwseClient(抓證交所)、VpsUploader(上傳到 Web)
Data/PriceDatabase.cs SQLite 存取、52 週高低價查詢、舊資料清理
Models/               DailyQuote、HighLow52
```

新增任務:在 `Runners/` 新增一個實作 `IJob` 的類別,再到 `Program.cs` 的 `jobs` 字典加一行。

## 設定

### `appsettings.json`(會進版控,不可放機敏資料)

| 欄位 | 說明 | 預設值 |
|---|---|---|
| `DbPath` | SQLite 檔案路徑(相對於執行檔所在目錄) | `stock.db` |
| `BaseUrl` | 證交所 API 網址 | `https://www.twse.com.tw/rwd/zh/afterTrading/MI_INDEX` |
| `RequestDelaySeconds` | 回補時每次打 API 後的間隔秒數(避免被證交所暫時封鎖 IP) | `5` |
| `BackfillDays` | 回補往回抓的日曆天數 | `380` |
| `RetainTradingDays` | 資料庫保留最近幾個交易日。**不可小於 250**,否則程式拒絕執行 | `250` |
| `VpsBaseUrl` | VPS 上 Web API 的 base URL(例如 `http://主機/SBH_Stg/api/Stock`),各任務會接上自己的 endpoint;留空則略過上傳 | 空 |

### API 金鑰(上傳驗證)

上傳時會在標頭帶 `X-Api-Key`,Web 核對後才處理。**金鑰不可寫進被追蹤的 `appsettings.json`**
(已 push 的檔案會留在 git 歷史)。擇一設定,兩者都有時以環境變數為準:

1. 環境變數 `SBH_VPS_API_KEY`(可設在執行工作排程器那個帳號的使用者環境變數)
2. 專案資料夾裡的 `appsettings.Local.json`(已加入 `.gitignore`,不進版控):
   ```json
   { "VpsApiKey": "你的金鑰" }
   ```
   建置與發布時會自動複製到輸出資料夾,檔案不存在也不會報錯。

注意事項:

- 金鑰只加在上傳請求上,不會送給證交所。
- 沒設定金鑰時仍會嘗試上傳,但 log 會有警告;Web 端會回 401,程式以非 0 結束碼結束。
- **Web 端要設定相同的金鑰**(環境變數 `SBH_VPS_API_KEY`,或 Web 專案的 `appsettings.Local.json`)。
  Web 沒設定金鑰時會拒絕所有上傳(HTTP 500),不會變成沒有保護。
- 金鑰一旦曾被 commit 或 push,就視為外洩,需要更換。

## 建置與發布

```powershell
dotnet build
dotnet publish -c Release -o publish
```

- `publish\` 已被 `.gitignore` 排除。
- `dotnet publish` 不會刪除資料夾裡多出來的檔案,但會用新版覆蓋同名檔案。程式更新後務必重新發布;
  舊版執行檔不會有新增的 mode(會出現「未知的 --mode 參數」)。

## 使用方式

### 1. 一次性歷史回補(啟用每日排程前先執行)

```powershell
cd publish
.\StockBuyingHelper.Jobs.exe --mode backfill
```

- 由今天往回抓 `BackfillDays` 天,自動跳過週六/週日
- 國定假日(證交所回應查無資料)會記錄並跳過,不視為錯誤
- 每次實際打 API 後間隔 `RequestDelaySeconds` 秒,全部跑完約 20 分鐘
- 可重複執行:已有資料的日期會被跳過,中斷後重跑可從中斷處接續
- 結束時會依 `RetainTradingDays` 清理舊資料
- 回補**不會**上傳,上傳由 `daily` 負責

### 2. 每日排程

```powershell
.\StockBuyingHelper.Jobs.exe --mode daily
```

- 週六/週日直接結束,不打 API;國定假日記錄後正常結束
- 寫入當日資料後,依 `RetainTradingDays` 清理舊資料(依日期整批刪除,不是每檔各留 N 筆;抓取失敗或假日不會刪)
- 再用 SQLite 算出每檔股票近 52 週(364 天)的最高/最低價,上傳到 `{VpsBaseUrl}/SaveHighLow52ToCsv`
- 重複執行同一天會覆寫,不會產生重複資料
- 上傳失敗會以非 0 結束碼結束;資料仍在 SQLite,下次執行會重新上傳

### 3. 0050 成分股清單

```powershell
.\StockBuyingHelper.Jobs.exe --mode ETF0050
```

抓取 `https://www.pocket.tw/etf/tw/0050/fundholding` 的成分股代號,上傳到 `{VpsBaseUrl}/Save0050List`。
此任務不使用 SQLite,需要本機安裝 Chrome。

### 設定 Windows 工作排程器

每個任務各建一個排程:

1. 開啟「工作排程器」→ 建立工作
2. 觸發程序:`daily` 建議每天 14:30(收盤 13:30 後留緩衝時間);`ETF0050` 依需求決定頻率
3. 動作:啟動程式 `publish\StockBuyingHelper.Jobs.exe`,引數填 `--mode daily`(或 `--mode ETF0050`)
4. 「開始位置」設為 `publish` 資料夾(`stock.db`、`logs\` 會產生在執行檔旁邊)

## 上傳 API(Web 端)

| endpoint | 內容 | Web 端行為 |
|---|---|---|
| `POST {VpsBaseUrl}/SaveHighLow52ToCsv` | JSON 陣列 `[{ "stockId", "stockName", "high52", "low52" }]` | 存成 `StockList_yyyyMMdd.csv`,Web 讀取檔名最新的 CSV |
| `POST {VpsBaseUrl}/Save0050List` | JSON 陣列 `["2330", ...]` | 存成 0050 清單 CSV |

兩者都需要標頭 `X-Api-Key`(驗證失敗回 401)。沒有資料時程式不會上傳,因為 API 收到空陣列會回成功但不處理。

## 資料庫(本機 SQLite)

```sql
Stocks(Code TEXT PRIMARY KEY, Name TEXT)
DailyPrices(Code TEXT, TradeDate TEXT, HighPrice REAL, LowPrice REAL, PRIMARY KEY(Code, TradeDate))
```

- 啟動時自動建立,不需手動建置。
- `stock.db` 在執行檔旁邊,第一次執行 `backfill` 或 `daily` 時才會產生,已被 `.gitignore` 排除。
- 當日無成交量的股票(證交所高低價為 `--`)只寫入 `Stocks`,不寫入 `DailyPrices`。
- 只保留最近 `RetainTradingDays` 個交易日。52 週(364 天)約涵蓋 243~248 個交易日,所以 250 是下限。
- 孤立的 `Stocks` 紀錄(例如已下市)不會清理。

直接用 SQL 查詢 52 週高低價:

```sql
SELECT p.Code, s.Name,
       MAX(p.HighPrice) AS High52w,
       MIN(p.LowPrice)  AS Low52w
FROM DailyPrices p
JOIN Stocks s ON s.Code = p.Code
WHERE p.TradeDate >= date('now', '-364 days')
GROUP BY p.Code;
```

## 記錄與疑難排解

每次執行的 log 寫到執行檔旁邊的 `logs\log-yyyyMMdd.txt`(依日期分檔,保留最近 30 天)。
工作排程器執行時沒有人看 console,事後排查要看這個檔案。

| 現象 | 可能原因 |
|---|---|
| 「未知的 --mode 參數」 | `publish` 裡是舊版執行檔,重新發布;或 mode 名稱拼錯(區分大小寫) |
| 「未設定 VpsBaseUrl,略過上傳」 | `appsettings.json` 的 `VpsBaseUrl` 是空的 |
| 「未設定 VpsApiKey,以未驗證方式上傳」 | 沒設定環境變數,也沒有 `appsettings.Local.json` |
| 上傳失敗 HTTP 401 | 金鑰與 Web 端不一致,或沒帶金鑰 |
| 上傳失敗 HTTP 500 | Web 端沒有設定金鑰,或伺服器錯誤,看回應內容 |
| 找不到 `stock.db` | 還沒執行過 `backfill` 或 `daily` |
| `RetainTradingDays` 小於 250 的錯誤 | 設定值低於下限,52 週高低價會被低估,拒絕執行 |
