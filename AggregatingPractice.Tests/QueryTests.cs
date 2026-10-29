using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace AggregatingPractice.Tests;

// Runs each of your .sql files against your own Northwind database and compares what it returns with the
// file's Expect line. Nothing is changed: every file runs inside a transaction that is rolled back.
// You don't need to change this file.
[TestClass]
public sealed class QueryTests
{
    // The NORTHWIND_CONNECTION environment variable, if set, points the tests at another database.
    private static readonly string connectionString =
        Environment.GetEnvironmentVariable("NORTHWIND_CONNECTION")
        ?? "Server=localhost;Database=Northwind;Integrated Security=True;TrustServerCertificate=True";

    // Runs one file (its GO lines split it into batches) and returns the last result it shows.
    private static DataTable Run(string file)
    {
        string sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Queries", file));
        var results = new List<DataTable>();
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        foreach (string batch in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            using var command = new SqlCommand(batch, connection, transaction);
            using var reader = command.ExecuteReader();
            while (!reader.IsClosed && reader.FieldCount > 0)
            {
                var result = new DataTable();
                result.Load(reader);   // reads one result, then moves on to the next
                results.Add(result);
            }
        }
        transaction.Rollback();
        Assert.IsNotEmpty(results, $"{file} shows no result yet. Write your query under the question.");
        return results[^1];
    }

    // True when one of the row's values is the text, or the row reads it across its values (a first and a
    // last name in two columns), so your column names and order don't matter.
    private static bool Has(DataRow row, string value) =>
        row.ItemArray.Any(cell => string.Equals(cell?.ToString(), value, StringComparison.Ordinal))
        || string.Join(" ", row.ItemArray.Select(cell => cell?.ToString())).Contains(value, StringComparison.Ordinal);

    // True when one of the row's values is the number: 263.5000 is the same answer as 263.50.
    private static bool Has(DataRow row, decimal value) =>
        row.ItemArray.Any(cell => cell is not null and not DBNull && decimal.TryParse(Convert.ToString(cell, CultureInfo.InvariantCulture),
            NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number) && Math.Abs(number - value) < 0.005m);

    private static DataRow[] Rows(DataTable result) => result.Rows.Cast<DataRow>().ToArray();

    // One question whose answer is a single row, with the numbers the Expect line names.
    private static void OneRow(string file, string question, params decimal[] values)
    {
        DataRow[] rows = Rows(Run(file));
        Assert.HasCount(1, rows, $"{question}: one row. An aggregate with no GROUP BY gives one.");
        foreach (decimal value in values)
        {
            Assert.IsTrue(Has(rows[0], value), $"{question}: the row shows {string.Join(", ", values)}.");
        }
    }

    [TestMethod]
    public void Query01_CountProducts_Is77() => OneRow("01_Count_Products.sql", "01", 77m);

    [TestMethod]
    public void Query02_CountDiscontinued_Is8() => OneRow("02_Count_Discontinued.sql", "02", 8m);

    [TestMethod]
    public void Query03_UnitsInStock_Is3119() => OneRow("03_Units_In_Stock.sql", "03", 3119m);

    [TestMethod]
    public void Query04_PriceRange_ShowsTheAverageCheapestAndDearest() => OneRow("04_Price_Range.sql", "04", 28.8663m, 2.50m, 263.50m);

    [TestMethod]
    public void Query05_CustomersWithFax_CountsOnlyTheFaxes() =>
        OneRow("05_Customers_With_Fax.sql", "05 (one of the COUNT forms skips NULL)", 69m);

    [TestMethod]
    public void Query06_SupplierCountries_CountsEachCountryOnce() => OneRow("06_Supplier_Countries.sql", "06", 16m);

    [TestMethod]
    public void Query07_OrdersPerYear_CountsEachYear()
    {
        DataRow[] rows = Rows(Run("07_Orders_Per_Year.sql"));

        Assert.HasCount(3, rows, "07: one row per year, so 3 rows.");
        Assert.IsTrue(Has(rows[0], 2016m) && Has(rows[0], 152m), "07: the first row is 2016, 152.");
        Assert.IsTrue(Has(rows[1], 2017m) && Has(rows[1], 408m), "07: the second row is 2017, 408.");
        Assert.IsTrue(Has(rows[2], 2018m) && Has(rows[2], 270m), "07: the last row is 2018, 270.");
    }

    [TestMethod]
    public void Query08_StockPerCategory_GivesEachCategoryItsCountAndStock()
    {
        DataRow[] rows = Rows(Run("08_Stock_Per_Category.sql"));

        Assert.HasCount(8, rows, "08: one row per category, so 8 rows.");
        Assert.IsTrue(Has(rows[0], 1m) && Has(rows[0], 12m) && Has(rows[0], 559m), "08: the first row is category 1: 12 products, 559 in stock.");
        Assert.IsTrue(Has(rows[7], 8m) && Has(rows[7], 12m) && Has(rows[7], 701m), "08: the last row is category 8: 12 products, 701 in stock.");
    }

    [TestMethod]
    public void Query09_BestCustomers_ListsTheTopThree()
    {
        DataRow[] rows = Rows(Run("09_Best_Customers.sql"));

        Assert.HasCount(3, rows, "09: the top three, so 3 rows.");
        Assert.IsTrue(Has(rows[0], "SAVEA") && Has(rows[0], 31m), "09: the first row is SAVEA, 31.");
        Assert.IsTrue(Has(rows[1], "ERNSH") && Has(rows[1], 30m), "09: the second row is ERNSH, 30.");
        Assert.IsTrue(Has(rows[2], "QUICK") && Has(rows[2], 28m), "09: the last row is QUICK, 28.");
    }

    [TestMethod]
    public void Query10_BigOrders_ListsTheOrdersOver10000BiggestFirst()
    {
        DataRow[] rows = Rows(Run("10_Big_Orders.sql"));

        Assert.HasCount(14, rows, "10: 14 orders are worth more than 10,000.");
        Assert.IsTrue(Has(rows[0], 10865m) && Has(rows[0], 17250.00m), "10: biggest first: order 10865, worth 17250.00.");
    }

    [TestMethod]
    public void Query11_BusyCountries2018_ListsTheFour()
    {
        DataRow[] rows = Rows(Run("11_Busy_Countries_2018.sql"));

        Assert.HasCount(4, rows, "11: four countries. One filter is on rows (WHERE), the other on groups (HAVING).");
        (string Country, decimal Orders)[] expected = [("USA", 39m), ("Germany", 34m), ("Brazil", 28m), ("France", 23m)];
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.IsTrue(Has(rows[i], expected[i].Country) && Has(rows[i], expected[i].Orders),
                $"11: row {i + 1} is {expected[i].Country}, {expected[i].Orders}.");
        }
    }

    [TestMethod]
    public void Query12_BonusRevenue_IsTheTotalAfterDiscounts() => OneRow("12_Bonus_Revenue.sql", "12", 1265793.04m);
}
