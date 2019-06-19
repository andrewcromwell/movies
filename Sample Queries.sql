-- Query #1: which movies were playing on 06/18/2019 in the US?
-- Keep in mind, the timestamp is in UTC.
SELECT
	R.RegionCode,
	MR.SysStartTime,
	M.MovieID,
	M.Title
FROM
	dbo.Movie M
	INNER JOIN dbo.MovieRun
	FOR SYSTEM_TIME AS OF '2019-06-18 00:00:00' MR
		ON M.MovieID = MR.MovieID
	INNER JOIN dbo.Region R
		ON MR.RegionID = R.RegionID
WHERE
	R.RegionCode = 'US'

-- Query #2: Which movies were directed by Seth Green?
SELECT
	P.Name,
	J.JobName,
	M.Title
FROM
	dbo.Person P
	INNER JOIN dbo.PersonCredit PC
		ON P.PersonID = PC.PersonID
	INNER JOIN dbo.Job J
		ON PC.JobID = J.JobID
	INNER JOIN dbo.Movie M
		ON PC.MovieID = M.MovieID
WHERE
	P.Name = 'Seth Green'
	AND J.JobName = 'Director';

-- Query #3: How many action movies are playing in Australia?
SELECT
	R.RegionCode,
	M.MovieID,
	M.Title,
	G.Description
FROM
	dbo.Movie M
	INNER JOIN dbo.MovieRun MR
		ON M.MovieID = MR.MovieID
	INNER JOIN dbo.Region R
		ON MR.RegionID = R.RegionID
	INNER JOIN dbo.GenreMovie GM
		ON M.MovieID = GM.MovieID
	INNER JOIN dbo.Genre G
		ON GM.GenreID = G.GenreID
WHERE
	R.RegionCode = 'AU'
	AND G.Description = 'Action';

-- Query #4: Which review authors write the most reviews per country?
WITH CTE AS
(
	SELECT
		RegionCode,
		AuthorName,
		COUNT(RV.ReviewID) NumberOfReviews,
		ROW_NUMBER() OVER (PARTITION BY RegionCode ORDER BY COUNT(RV.ReviewID) DESC) ReviewRank
	FROM
		dbo.Region R
		INNER JOIN dbo.MovieProducedInCountry MPIC
			ON R.RegionID = MPIC.RegionID
		INNER JOIN dbo.Movie M
			ON MPIC.MovieID = M.MovieID
		INNER JOIN dbo.Review RV
			ON M.MovieID = RV.MovieID
		INNER JOIN dbo.Author A
			ON RV.AuthorID = A.AuthorID
	GROUP BY RegionCode, AuthorName
)
SELECT
	RegionCode,
	AuthorName,
	NumberOfReviews
FROM
	CTE
WHERE
	ReviewRank = 1;

-- Query #5: In which country are most movies produced?
SELECT TOP 1
	RegionCode,
	COUNT(M.MovieID)
FROM
	dbo.Region R
	INNER JOIN dbo.MovieProducedInCountry MPIC
		ON R.RegionID = MPIC.RegionID
	INNER JOIN dbo.Movie M
		ON MPIC.MovieID = M.MovieID
GROUP BY RegionCode
ORDER BY COUNT(M.MovieID) DESC;

-- Query #6: What was the popularity of a particular movie on a particular date?
SELECT
	M.MovieID,
	M.Title,
	M.Popularity
FROM
	dbo.Movie
	FOR SYSTEM_TIME AS OF '2019-06-18 00:00:00' M
WHERE
	M.Title = 'Pokémon Detective Pikachu';