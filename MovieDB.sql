CREATE TABLE Movie
(
    MovieID INT PRIMARY KEY,
    Title NVARCHAR(300) NOT NULL,
    OriginalTitle NVARCHAR(300),
    Popularity NUMERIC(6,2),

    SysStartTime DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    SysEndTime DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime)
)
WITH
(
    SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.MovieHistory)
)

CREATE TABLE StagedMovie
(
    MovieID INT PRIMARY KEY,
    Title NVARCHAR(300) NOT NULL,
    OriginalTitle NVARCHAR(300),
    Popularity NUMERIC(6,2)
);

CREATE TABLE Region
(
    RegionID INT PRIMARY KEY IDENTITY(1,1),
    RegionCode NCHAR(2)
)

CREATE TABLE MovieRun
(
    MovieRunID INT PRIMARY KEY IDENTITY(1,1),
    MovieID INT FOREIGN KEY REFERENCES Movie(MovieID) NOT NULL,
    RegionID INT FOREIGN KEY REFERENCES Region(RegionID) NOT NULL,

    SysStartTime DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    SysEndTime DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime)
)
WITH
(
    SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.MovieRunHistory)
)

CREATE TABLE StagedMovieByRegion
(
    ID INT PRIMARY KEY IDENTITY(1,1),
    MovieID INT NOT NULL,
    RegionCode NCHAR(2) NOT NULL
);

CREATE TABLE MovieProducedInCountry
(
    MovieProducedInCountryID INT PRIMARY KEY IDENTITY(1,1),
    MovieID INT FOREIGN KEY REFERENCES Movie(MovieID) NOT NULL,
    RegionID INT FOREIGN KEY REFERENCES Region(RegionID) NOT NULL
)

CREATE TABLE Genre
(
    GenreID INT PRIMARY KEY,
    [Description] NVARCHAR(100) NOT NULL
)

CREATE TABLE GenreMovie
(
    GenreMovieID INT PRIMARY KEY IDENTITY(1,1),
    MovieID INT FOREIGN KEY REFERENCES Movie(MovieID) NOT NULL,
    GenreID INT FOREIGN KEY REFERENCES Genre(GenreID) NOT NULL
)

CREATE TABLE Person
(
    PersonID INT PRIMARY KEY IDENTITY(1,1),
    [Name] NVARCHAR(300)
)

CREATE TABLE Job
(
    JobID INT PRIMARY KEY IDENTITY(1,1),
    JobName NVARCHAR(300) NOT NULL UNIQUE
)

CREATE TABLE PersonCredit
(
    PersonCreditID INT PRIMARY KEY IDENTITY(1,1),
    PersonID INT FOREIGN KEY REFERENCES Person(PersonID) NOT NULL,
    JobID INT FOREIGN KEY REFERENCES Job(JobID) NOT NULL,
    MovieID INT FOREIGN KEY REFERENCES Movie(MovieID) NOT NULL
)

CREATE TABLE Author
(
	AuthorID INT PRIMARY KEY IDENTITY(1,1),
	AuthorName NVARCHAR(300)
)

CREATE TABLE Review
(
	ReviewID INT PRIMARY KEY IDENTITY(1,1),
	AuthorID INT FOREIGN KEY REFERENCES Author(AuthorID),
	MovieID INT FOREIGN KEY REFERENCES Movie(MovieID),
	jsonID NVARCHAR(24),
	ReviewURL NVARCHAR(300)
)

GO;

CREATE OR ALTER PROCEDURE [dbo].[MergeMovies]
AS
BEGIN
	SET NOCOUNT ON; 

	MERGE dbo.Movie AS target  
    USING dbo.StagedMovie as source
    ON (target.movieID = source.movieID)  
    WHEN MATCHED THEN   
        UPDATE SET 
			Title = source.Title, 
			originalTitle = source.OriginalTitle, 
			Popularity = source.Popularity
	WHEN NOT MATCHED THEN  
		INSERT (MovieID, Title, OriginalTitle, Popularity)  
		VALUES (source.MovieID, source.Title, source.OriginalTitle, source.Popularity);

	RETURN 0
END

GO;

CREATE   PROCEDURE [dbo].[MergeRegions]
	@RegionName AS NVARCHAR(150),
	@RegionCode AS NCHAR(2)
AS
BEGIN
	SET NOCOUNT ON; 

	IF (SELECT COUNT(1) FROM dbo.Region WHERE RegionCode = @RegionCode) = 0
	BEGIN
		INSERT INTO dbo.Region
		(RegionName, RegionCode)
		VALUES
		(@RegionName, @RegionCode);
	END

	RETURN 0
END

GO;

/****** Object: SqlProcedure [dbo].[MergeMovies] Script Date: 6/13/2019 12:32:45 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE   PROCEDURE [dbo].[MergeMovieRuns]
AS
BEGIN
	SET NOCOUNT ON; 

	WITH CTE AS (
		SELECT
			SMBR.MovieID,
			R.RegionID
		FROM
			dbo.StagedMovieByRegion SMBR
			LEFT JOIN dbo.Region R
				ON R.RegionCode = SMBR.RegionCode
	)
	MERGE dbo.MovieRun AS target  
    USING CTE as source
    ON (target.movieID = source.movieID AND target.RegionID = source.RegionID)  
	WHEN NOT MATCHED BY SOURCE THEN  
		DELETE
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (MovieID, RegionID)  
		VALUES (source.MovieID, source.RegionID);

	RETURN 0
END

GO;

CREATE   PROCEDURE [dbo].[MergeGenreMovies]
	@GenreID AS INT,
	@MovieID AS INT
AS
BEGIN
	SET NOCOUNT ON; 

	IF (SELECT COUNT(1) FROM dbo.GenreMovie WHERE GenreID = @GenreID AND MovieID = @MovieID) = 0
	BEGIN
		INSERT INTO dbo.GenreMovie
		(GenreID, MovieID)
		VALUES
		(@GenreID, @MovieID);
	END

	RETURN 0
END

GO;

CREATE   PROCEDURE [dbo].[MergeGenres]
	@GenreID AS INT,
	@GenreName AS NVARCHAR(100)
AS
BEGIN
	SET NOCOUNT ON; 

	IF (SELECT COUNT(1) FROM dbo.Genre WHERE GenreID = @GenreID) = 0
	BEGIN
		INSERT INTO dbo.Genre
		(GenreID, [Description])
		VALUES
		(@GenreID, @GenreName);
	END

	RETURN 0
END

GO;

CREATE OR ALTER   PROCEDURE [dbo].[MergeMovieProducedInCountry]
	@MovieID AS INT,
	@RegionCode AS NCHAR(2)
AS
BEGIN
	SET NOCOUNT ON; 

	DECLARE @CountryID INT;

	SELECT 
		@countryID = RegionID
	FROM dbo.Region 
	WHERE RegionCode = @RegionCode

	IF (SELECT COUNT(1) FROM dbo.MovieProducedInCountry WHERE MovieID = @MovieID AND RegionID = @CountryID) = 0
	BEGIN
		INSERT INTO dbo.MovieProducedInCountry
		(MovieID, RegionID)
		VALUES
		(@MovieID, @CountryID);
	END

	RETURN 0
END

GO;

CREATE     PROCEDURE [dbo].InsertIntoReviews
	@AuthorName AS NCHAR(300),
	@MovieID AS INT,
	@JsonID AS NVARCHAR(24),
	@ReviewUrl AS NVARCHAR(300)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @AuthorID INT;

	SELECT
		@AuthorID = AuthorID
	FROM dbo.Author
	WHERE AuthorName = @AuthorName

	if @AuthorID IS NULL
	BEGIN
		INSERT INTO Author
		(AuthorName)
		VALUES
		(@AuthorName);

		SELECT @AuthorID = SCOPE_IDENTITY();
	END

	IF (SELECT COUNT(1) FROM dbo.Review WHERE jsonID = @JsonID) = 0
	BEGIN
		INSERT INTO dbo.Review
		(AuthorID, MovieID, jsonID, ReviewURL)
		VALUES
		(@AuthorID, @MovieID, @JsonID, @ReviewUrl);
	END

	RETURN 0
END
