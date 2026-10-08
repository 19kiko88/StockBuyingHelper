# StockBuyingHelper.Jobs

每天收盤後抓取台灣證交所(TWSE)當日全部股票收盤行情,寫入 SQLite,用來累積近 52 週
(約 250 個交易日)的歷史高低價資料。

資料來源:`https://www.twse.com.tw/rwd/zh/afterTrading/MI_INDEX`

## 資料庫結構

```sql
Stocks(Code TEXT PRIMARY KEY, Name TEXT)
DailyPrices(Code TEXT, TradeDate TEXT, HighPrice REAL, LowPrice REAL, PRIMARY KEY(Code, TradeDate))
```

啟動時會自動建立(`CREATE TABLE IF NOT EXISTS`),不需手動建置。

## 設定(`appsettings.json`)

| 欄位 | 說明 | 預設值 |
|---|---|---|
| `DbPath` | SQLite 檔案路徑(相對於程式執行目錄) | `stock.db` |
| `BaseUrl` | TWSE API 網址 | `https://www.twse.com.tw/rwd/zh/afterTrading/MI_INDEX` |
| `RequestDelaySeconds` | 回補模式下,每次打 API 之間的間隔秒數(避免被證交所暫時封鎖 IP) | `5` |
| `BackfillDays` | 回補模式往回抓的日曆天數 | `380` |
| `RetainTradingDays` | 資料庫保留最近幾個交易日,更早的整批刪除。**不可小於 250**,否則程式拒絕執行(避免 52 週高低價被低估) | `250` |
| `VpsBaseUrl` | VPS 上 Web API 的 base URL(例如 `http://主機/SBH_Stg/api/Stock`),各任務會接上各自的 endpoint;留空則略過上傳 | 空 |

## 可用的 `--mode`

| mode | 說明 |
|---|---|
| `daily` | 抓當日行情、寫入 SQLite、清理舊資料,並把 52 週高低價 POST 到 `{VpsBaseUrl}/SaveHighLow52ToCsv`(預設) |
| `backfill` | 一次性回補歷史資料 |
| `ETF0050` | 用 Selenium 抓 0050 成分股清單,POST 到 `{VpsBaseUrl}/Save0050List`(需要本機有 Chrome) |

新增任務:實作 `IJob`,並在 `Program.cs` 的 `jobs` 字典加一行。

上傳由 `VpsUploader` 統一處理:沒有資料或沒設定 `VpsBaseUrl` 時只記 log 不上傳;
HTTP 回應不是成功時會丟例外、以非 0 結束碼結束,資料仍留在 SQLite,下次執行會重新上傳。

`--mode daily` 寫入當日資料後,會用 SQLite 算出每檔股票近 52 週(364 天)的最高/最低價,
以 JSON 上傳給 Web 的 `SaveHighLow52ToCsv`(Web 讀取最新的 CSV)。

`daily` 成功寫入當日資料後,以及 `backfill` 結束時,會刪除超過 `RetainTradingDays` 個交易日的舊資料
(依日期整批刪除,不是每檔各留 N 筆),讓資料庫大小維持穩定。抓取失敗或假日不會刪除。

## 建置與發布

```powershell
dotnet build
dotnet publish -c Release -o publish
```

## 使用方式

### 1. 一次性歷史回補(啟用每日排程前先執行一次)

```powershell
cd publish
.\StockBuyingHelper.Jobs.exe --mode backfill
```

- 由今天往回抓約 380 天,自動跳過週六/週日
- 遇到國定假日(API 回應查無資料)會記錄並跳過,不視為錯誤
- 每次實際打 API 後間隔 5 秒,全部跑完約需 20 分鐘
- 可重複執行:已抓過的日期會被跳過,中斷後重跑可從中斷處接續

### 2. 每日排程(收盤後執行)

```powershell
.\StockBuyingHelper.Jobs.exe --mode daily
```
(不帶 `--mode` 參數時預設即為 `daily`)

- 若當天是週六/週日,直接結束,不打 API
- 若 API 回應顯示非交易日(國定假日),記錄並正常結束
- 重複執行同一天會覆寫(INSERT OR REPLACE),不會造成重複資料

### 設定 Windows 工作排程器

1. 開啟「工作排程器」→ 建立工作
2. 觸發程序:每天 14:30(收盤 13:30 後留緩衝時間)
3. 動作:啟動程式 `publish\StockBuyingHelper.Jobs.exe`,「開始位置」設為 `publish`
   資料夾(確保 `appsettings.json`、`stock.db`、`logs\` 的相對路徑正確)

## 查詢 52 週高低價

這支程式只負責資料蒐集,52 週高低價用一般 SQL 查詢即可算出,不需要額外功能:

```sql
SELECT p.Code, s.Name,
       MAX(p.HighPrice) AS High52w,
       MIN(p.LowPrice)  AS Low52w
FROM DailyPrices p
JOIN Stocks s ON s.Code = p.Code
WHERE p.TradeDate >= date('now', '-364 days')
GROUP BY p.Code;
```

## 記錄

每次執行的 log 會寫到執行目錄下的 `logs\log-yyyyMMdd.txt`(依日期分檔,保留最近 30 天),
因為工作排程器執行時沒有人看 console 輸出,事後排查需要靠這個檔案。
