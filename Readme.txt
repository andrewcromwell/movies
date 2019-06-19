This is the movieDB etl project.

To get it working, start by creating a new database. Run MovieDB.sql to create the structure.

Update App.config with your connection string.

Then just run the tool!

"Sample Queries.sql" contains some sample queries.

The procedure [dbo].[MonthlyTask] contains the code for the monthly task, which inserts yearmonth|country|numberofmovies into a table once per month.

== Room for Improvement ==

* It would be better if the API key and the connection string were not kept in source control. There's a way around this. Code can use AWS Secrets, for example, to pull sensitive information without having to store it in source control.
* It would be better if there were unit tests. This would require some refactoring. In particular, the Persistence layer shouldn't connect directly to the database, but rather interact with an object. Similarly, the retrieval layer shouldn't connect directly to the Web API. Rather, it should interact with an object (which can be mocked/faked), which will handle the implementation details.
* It would be better to use an ORM instead of using SQL directly.
* To transform JSON to a relational format, it would have been better to use AWS Glue, AWS's ETL solution. AWS Glue has a method "Relationalize" which auto-generates a Python script to parse and transform data into a relational format.
