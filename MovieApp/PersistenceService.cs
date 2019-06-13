using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace MovieApp
{
    class PersistenceService
    {
        private string connStr = "";

        public PersistenceService()
        {

        }

        public PersistenceService(string connectionString)
        {
            connStr = connectionString;
        }

        public bool processData(RetrievalService.FullResponse fullResponse)
        {
            // STEP 1: populate movies, where necessary
            truncateMovieStageTable();

            var movies = (from result in fullResponse.nowPlaying
                         from movie in result.results
                         select movie).Distinct().ToList();

            foreach (RetrievalService.NowPlayingResults.movie m in movies)
            {
                addMoviesToStagingTable(m);
            }

            mergeMovies();

            // STEP 2: populate regions, where necessary
            var productionCountries = (from keypair in fullResponse.details
                           from country in keypair.Value.production_countries
                           select country
                           );
            var playingCountries = (from result in fullResponse.nowPlaying
                                    where !productionCountries.Any(x => x.iso_3166_1.Equals(result.region))
                                    select new RetrievalService.MovieDetails.ProductionCountry
                                    {
                                        iso_3166_1 = result.region,
                                        name = ""
                                    });
            var countries = productionCountries.Union(playingCountries).Distinct().ToList();

            foreach (RetrievalService.MovieDetails.ProductionCountry country in countries)
            {
                addRegionsWhereNeeded(country);
            }

            // STEP 3: populate the Movie-Region cross-ref table
            truncateMovieRegionStageTable();

            var moviesShowingInEachCountry = (from result in fullResponse.nowPlaying
                                              from movie in result.results
                                              select new Tuple<int, string>(movie.id, result.region))
                                              .Distinct().ToList();
            foreach (Tuple<int, string> a in moviesShowingInEachCountry)
            {
                populateTupleInStagingTable(a.Item1, a.Item2);
            }

            mergeMovieRuns();

            // STEP 4: populate genres where necessary
            var genres = (from keyPair in fullResponse.details
                          from g in keyPair.Value.genres
                          select g).Distinct().ToArray();

            foreach (RetrievalService.MovieDetails.Genre g in genres)
            {
                addGenre(g);
            }

            // STEP 5: insert into GenreMovie where necessary
            var moviesByGenre = (from keypair in fullResponse.details
                                 from g in keypair.Value.genres
                                 select new Tuple<int, int>(g.id, keypair.Key))
                                 .Distinct().ToArray();

            foreach (Tuple<int, int> mg in moviesByGenre)
            {
                insertIntoGenreMovie(mg.Item1, mg.Item2);
            }

            // STEP 6: insert into MovieProducedInCountry where necessary
            var moviesByProducedIn = (from keypair in fullResponse.details
                                    from country in keypair.Value.production_countries
                                    select new Tuple<int, string>(keypair.Key, country.iso_3166_1))
                                 .Distinct().ToArray();
            foreach (Tuple<int, string> mc in moviesByProducedIn)
            {
                InsertIntoProducedIn(mc.Item1, mc.Item2);
            }

            // STEP 7: insert into PersonCredit
            var crewByMovie = (from keypair in fullResponse.credits
                               from crewEntry in keypair.Value.crew
                               where crewEntry.job.Equals("Director")
                               select new Tuple<int, string>(keypair.Key, crewEntry.name))
                                 .Distinct().ToArray();

            foreach (Tuple<int, string> mc in crewByMovie)
            {
                InsertIntoCredits(mc.Item1, mc.Item2);
            }
            return true;
        }

        public void truncateMovieStageTable()
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText = "TRUNCATE TABLE StagedMovie";

                command.ExecuteNonQuery(); // truncate the Staging table.
            }
        }

        public void addMoviesToStagingTable(RetrievalService.NowPlayingResults.movie movie)
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText = 
                    "INSERT INTO dbo.StagedMovie                    " +
                    "(MovieID, Title, OriginalTitle, Popularity)    " +
                    "VALUES                                         " +
                    "(@movieID, @title, @originalTitle, @popularity)";
                command.Parameters.AddWithValue("@movieID", movie.id);
                command.Parameters.AddWithValue("@title", movie.title);
                command.Parameters.AddWithValue("@originalTitle", movie.original_title);
                command.Parameters.AddWithValue("@popularity", movie.popularity);

                command.ExecuteNonQuery();
            }
        }

        public void mergeMovies()
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText =
                    "EXEC dbo.MergeMovies";

                command.ExecuteNonQuery(); 
            }
        }

        public void addRegionsWhereNeeded(RetrievalService.MovieDetails.ProductionCountry country)
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText =
                    "EXEC dbo.MergeRegions @RegionName, @RegionCode";

                command.Parameters.AddWithValue("@RegionName", country.name);
                command.Parameters.AddWithValue("@RegionCode", country.iso_3166_1);

                command.ExecuteNonQuery();
            }
        }

        public void truncateMovieRegionStageTable()
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText = "TRUNCATE TABLE StagedMovieByRegion";

                command.ExecuteNonQuery(); // truncate the Staging table.
            }
        }

        public void populateTupleInStagingTable(int movieID, string RegionCode)
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText =
                    "INSERT INTO dbo.StagedMovieByRegion            " +
                    "(MovieID, RegionCode)                          " +
                    "VALUES                                         " +
                    "(@movieID, @regionCode)                        ";
                command.Parameters.AddWithValue("@movieID", movieID);
                command.Parameters.AddWithValue("@regionCode", RegionCode);

                command.ExecuteNonQuery();
            }
        }

        public void mergeMovieRuns()
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText =
                    "EXEC dbo.MergeMovieRuns";

                command.ExecuteNonQuery();
            }
        }

        public void addGenre(RetrievalService.MovieDetails.Genre genre)
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText =
                    "EXEC dbo.MergeGenres @GenreID, @GenreName";

                command.Parameters.AddWithValue("@GenreID", genre.id);
                command.Parameters.AddWithValue("@GenreName", genre.name);

                command.ExecuteNonQuery();
            }
        }

        public void insertIntoGenreMovie(int genreID, int movieID)
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText =
                    "EXEC dbo.MergeGenreMovies @GenreID, @MovieID";

                command.Parameters.AddWithValue("@GenreID", genreID);
                command.Parameters.AddWithValue("@MovieID", movieID);

                command.ExecuteNonQuery();
            }
        }

        public void InsertIntoProducedIn(int movieID, string regionCode)
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText =
                    "EXEC dbo.MergeMovieProducedInCountry @MovieID, @RegionCode";

                command.Parameters.AddWithValue("@MovieID", movieID);
                command.Parameters.AddWithValue("@RegionCode", regionCode);

                command.ExecuteNonQuery();
            }
        }

        public void InsertIntoCredits(int movieID, string personName)
        {
            using (SqlConnection sqlconn = new SqlConnection(connStr))
            {
                sqlconn.Open();
                SqlCommand command = sqlconn.CreateCommand();
                command.CommandText =
                    "EXEC dbo.InsertIntoCredits @MovieID, @JobName, @PersonName";

                command.Parameters.AddWithValue("@MovieID", movieID);
                command.Parameters.AddWithValue("@JobName", "Directory");
                command.Parameters.AddWithValue("@PersonName", personName);

                command.ExecuteNonQuery();
            }
        }
    }
}
