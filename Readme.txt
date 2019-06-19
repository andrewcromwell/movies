This is the movieDB etl project.

To get it working, start by creating a new database. Run MovieDB.sql to create the structure.

Update App.config with your connection string.

Then just run the tool!

"Sample Queries.sql" contains some sample queries.

The procedure [dbo].[MonthlyTask] contains the code for the monthly task, which inserts yearmonth|country|numberofmovies into a table once per month.
