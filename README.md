# Exercise: Aggregating Data

Exercise SQL for the lesson **Exercise: Aggregating Data** in the C# Full-Stack Bootcamp on [ask-codey.com](https://www.ask-codey.com).

Twelve questions about the **Northwind** database, answered with aggregate functions, `GROUP BY` and `HAVING`. Each file holds one question as a comment, and what the right answer looks like: write your query under it. Nothing here changes any data.

## Getting started

1. Open SQL Server Management Studio, and connect to `localhost`.
2. Choose **File > Open > File...** and open `01_Count_Products.sql`.
3. Choose **Northwind** in the database drop-down.
4. Write your query under the question, select it, and press **F5**.

Compare your result with the file's **Expect** line: the number of rows, and the values it names. If they don't match, read the question again: check your `WHERE`, your `GROUP BY` and your `HAVING`.

## Check your answers

`AggregatingExercise.Tests` runs each of your files against your Northwind database and compares what it returns with the file's **Expect** line. Nothing is changed: every file runs inside a transaction that is rolled back. It needs the .NET SDK, which you installed in the first week. Open a terminal in this folder and run:

```bash
dotnet test AggregatingExercise.Tests
```

At first every test fails: that is expected. Each question you answer turns its test green, and a failing test's message says what the right answer looks like. Your column names and their order don't matter. The tests use the Northwind database on `localhost`; to use another one, set the `NORTHWIND_CONNECTION` environment variable to its connection string.

## Tips

- One row for the whole table? An aggregate on its own. One row per something? `GROUP BY` that something.
- Filtering rows before counting is `WHERE`; filtering the groups by their counts or totals is `HAVING`.
- Some tools, such as sqlcmd, show money values with four decimal places: 17250.0000 is the same answer as the 17250.00 in an **Expect** line.

## Hand it in

When it works, push your solution to a public repo of your own on GitHub, then paste that repo's address into **Submit your exercise** on the lesson page at [ask-codey.com](https://www.ask-codey.com). That's what completes the lesson. The lesson page shows the Git commands, step by step.
