using Microsoft.Data.Sqlite;
using StockBuyingHelper.Jobs.Models;

namespace StockBuyingHelper.Jobs.Data;

public sealed class PriceDatabase
{
    private readonly string _connectionString;

    public PriceDatabase(string dbPath)
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
    }

    public void EnsureSchema()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Stocks (
                Code TEXT PRIMARY KEY,
                Name TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS DailyPrices (
                Code TEXT NOT NULL,
                TradeDate TEXT NOT NULL,
                HighPrice REAL NOT NULL,
                LowPrice REAL NOT NULL,
                PRIMARY KEY (Code, TradeDate),
                FOREIGN KEY (Code) REFERENCES Stocks(Code)
            );
            """;
        command.ExecuteNonQuery();
    }

    public bool HasDataForDate(DateOnly date)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM DailyPrices WHERE TradeDate = $date)";
        command.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd"));
        return (long)command.ExecuteScalar()! == 1;
    }

    // 52 週(364 天)約涵蓋 243~248 個交易日,保留數低於此值會讓 52 週高低價被低估
    public const int MinRetainTradingDays = 250;

    public int TrimOldData(int retainTradingDays)
    {
        if (retainTradingDays < MinRetainTradingDays)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retainTradingDays), retainTradingDays, $"保留交易日數不可小於 {MinRetainTradingDays}");
        }

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM DailyPrices
            WHERE TradeDate < (
                SELECT MIN(TradeDate) FROM (
                    SELECT DISTINCT TradeDate FROM DailyPrices ORDER BY TradeDate DESC LIMIT $n
                )
            )
            """;
        command.Parameters.AddWithValue("$n", retainTradingDays);
        return command.ExecuteNonQuery();
    }

    public List<HighLow52> GetHighLow52(DateOnly asOf)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.Code, s.Name, MAX(p.HighPrice), MIN(p.LowPrice)
            FROM DailyPrices p
            JOIN Stocks s ON s.Code = p.Code
            WHERE p.TradeDate > $from AND p.TradeDate <= $to
            GROUP BY p.Code, s.Name
            ORDER BY p.Code
            """;
        command.Parameters.AddWithValue("$from", asOf.AddDays(-364).ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$to", asOf.ToString("yyyy-MM-dd"));

        var result = new List<HighLow52>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new HighLow52(reader.GetString(0), reader.GetString(1), reader.GetDecimal(2), reader.GetDecimal(3)));
        }

        return result;
    }

    public void UpsertDay(DateOnly date, IEnumerable<DailyQuote> quotes)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();

        using var stockCommand = connection.CreateCommand();
        stockCommand.Transaction = transaction;
        stockCommand.CommandText = "INSERT OR REPLACE INTO Stocks (Code, Name) VALUES ($code, $name)";
        var stockCode = stockCommand.Parameters.Add("$code", SqliteType.Text);
        var stockName = stockCommand.Parameters.Add("$name", SqliteType.Text);

        using var priceCommand = connection.CreateCommand();
        priceCommand.Transaction = transaction;
        priceCommand.CommandText =
            "INSERT OR REPLACE INTO DailyPrices (Code, TradeDate, HighPrice, LowPrice) VALUES ($code, $date, $high, $low)";
        var priceCode = priceCommand.Parameters.Add("$code", SqliteType.Text);
        var priceDate = priceCommand.Parameters.Add("$date", SqliteType.Text);
        var priceHigh = priceCommand.Parameters.Add("$high", SqliteType.Real);
        var priceLow = priceCommand.Parameters.Add("$low", SqliteType.Real);

        var dateText = date.ToString("yyyy-MM-dd");

        foreach (var quote in quotes)
        {
            stockCode.Value = quote.Code;
            stockName.Value = quote.Name;
            stockCommand.ExecuteNonQuery();

            if (quote.HighPrice is not { } high || quote.LowPrice is not { } low)
            {
                continue;
            }

            priceCode.Value = quote.Code;
            priceDate.Value = dateText;
            priceHigh.Value = high;
            priceLow.Value = low;
            priceCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
