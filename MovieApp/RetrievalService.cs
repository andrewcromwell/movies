using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace MovieApp
{
    class RetrievalService
    {
        private string token = "bbb0e77b94b09193e6f32d5fac7a3b9c";
        private string[] regions;

        public RetrievalService()
        {

        }

        public RetrievalService(string[] regions)
        {
            this.regions = regions;
        }

        public FullResponse GetNowPlaying()
        {

            List<NowPlayingResults> resultSet = new List<NowPlayingResults>();

            foreach (string region in regions)
            {
                int i = 1;
                int total_pages = 0;
                do
                {
                    NowPlayingResults npr = GetRegionPageResults(region, i);
                    npr.region = region;
                    total_pages = npr.total_pages;

                    resultSet.Add(npr);

                    i++;

                } while (i <= total_pages);
            }

            var movieIDs = (from result in resultSet
                            from movie in result.results
                            select movie.id).Distinct().ToList();

            Dictionary<int, MovieDetails> movieDetails = new Dictionary<int, MovieDetails>();
            Dictionary<int, MovieCredits> movieCredits = new Dictionary<int, MovieCredits>();
            Dictionary<int, MovieReviews[]> movieReviews = new Dictionary<int, MovieReviews[]>();

            foreach (int movieID in movieIDs)
            {
                movieDetails[movieID] = getDetailsForMovieID(movieID);
                movieCredits[movieID] = getCreditsForMovieID(movieID);
                movieReviews[movieID] = getReviewsForMovieID(movieID);
            }

            FullResponse fr = new FullResponse
            {
                nowPlaying = resultSet,
                details = movieDetails,
                credits = movieCredits,
                reviews = movieReviews
            };
            return fr;
        }

        public NowPlayingResults GetRegionPageResults(string region, int page)
        {
            var client = new RestClient("https://api.themoviedb.org/3/movie/now_playing");
            var request = new RestRequest(Method.GET);
            bool done = true;
            IRestResponse response;


            request.AddParameter("region", region, ParameterType.QueryString);
            request.AddParameter("page", page, ParameterType.QueryString);
            request.AddParameter("language", "en-US", ParameterType.QueryString);
            request.AddParameter("api_key", token, ParameterType.QueryString);


            request.AddParameter("undefined", "{}", ParameterType.RequestBody);
            do
            {
                done = true;
                Console.WriteLine("Grabbing now playing page = " + page);
                response = client.Execute(request);

                if (response.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    Console.WriteLine("Waiting");
                    int waitTime = Int32.Parse(response.Headers[2].Value.ToString());
                    System.Threading.Thread.Sleep((waitTime + 2) * 1000);
                    done = false;
                }

            } while (!done);

            NowPlayingResults p = JsonConvert.DeserializeObject<NowPlayingResults>(response.Content);
            return p;
        }

        public class NowPlayingResults
        {
            public class movie : IEquatable<movie>
            {
                public int id { get; set; }
                public string title { get; set; }
                public double popularity { get; set; }
                public string original_title { get; set; }
                public int[] genre_ids { get; set; }

                public bool Equals(movie other)
                {
                    if (other == null) return false;
                    return (this.id == other.id);
                }

                public override bool Equals(object obj)
                {
                    movie other = obj as movie;
                    if (other != null)
                    {
                        return Equals(other);
                    }
                    else
                    {
                        return false;
                    }
                }

                public override int GetHashCode()
                {
                    return id.GetHashCode();
                }
            }

            public movie[] results { get; set; }
            public int page { get; set; }
            public int total_results { get; set; }
            public int total_pages { get; set; }
            public string region { get; set; }
        }

        public class MovieDetails
        {
            public class Genre : IEquatable<Genre>
            {
                public int id { get; set; }
                public string name { get; set; }

                public bool Equals(Genre other)
                {
                    if (other == null) return false;
                    return (this.id == other.id);
                }

                public override bool Equals(object obj)
                {
                    Genre other = obj as Genre;
                    if (other != null)
                    {
                        return Equals(other);
                    }
                    else
                    {
                        return false;
                    }
                }

                public override int GetHashCode()
                {
                    return id.GetHashCode();
                }
            }

            public class ProductionCountry : IEquatable<ProductionCountry>
            {
                public string iso_3166_1 { get; set; }
                public string name { get; set; }

                public bool Equals(ProductionCountry other)
                {
                    if (other == null) return false;
                    return (this.iso_3166_1 == other.iso_3166_1);
                }

                public override bool Equals(object obj)
                {
                    ProductionCountry other = obj as ProductionCountry;
                    if (other != null)
                    {
                        return Equals(other);
                    }
                    else
                    {
                        return false;
                    }
                }

                public override int GetHashCode()
                {
                    return iso_3166_1.GetHashCode();
                }
            }
            public Genre[] genres { get; set; }
            public ProductionCountry[] production_countries { get; set; }
        }

        public class MovieCredits
        {
            public int id { get; set; }

            public class Cast
            {
                public int cast_id { get; set; }
                public string character { get; set; }
                public string credit_id { get; set; }
                public int gender { get; set; }
                public int id { get; set; }
                public string name { get; set; }
                public int order { get; set; }
                public string profile_path { get; set; }
            }

            public Cast[] cast { get; set; }

            public class Crew
            {
                public string credit_id {get; set; }
                public string department {get; set; }
                public int gender {get; set; }
                public int id {get; set; }
                public string job {get; set; }
                public string name {get; set; }
                public string profile_path {get; set; }
            }

            public Crew[] crew { get; set; }
        }

        public class MovieReviews
        {
            public int id { get; set; }

            public int page { get; set; }
            public int total_results { get; set; }
            public int total_pages { get; set; }

            public class Result
            {
                public string author { get; set; }
                public string id { get; set; }
                public string url { get; set; }
            }

            public Result[] results { get; set; }
        }


        public MovieDetails getDetailsForMovieID(int movieID)
        {
            MovieDetails md;
            bool done = true;
            IRestResponse response;

            do
            {
                done = true;
                Console.WriteLine("Grabbing details for movieID = " + movieID);
                var client = new RestClient("https://api.themoviedb.org/3/movie/" + movieID);
                var request = new RestRequest(Method.GET);


                request.AddParameter("language", "en-US", ParameterType.QueryString);
                request.AddParameter("api_key", token, ParameterType.QueryString);


                request.AddParameter("undefined", "{}", ParameterType.RequestBody);

                response = client.Execute(request);
                
                if (response.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    Console.WriteLine("Waiting");
                    int waitTime = Int32.Parse(response.Headers[2].Value.ToString());
                    System.Threading.Thread.Sleep((waitTime + 2) * 1000);
                    done = false;
                }

                md = JsonConvert.DeserializeObject<MovieDetails>(response.Content);

            } while (!done);

            return md;
        }

        public MovieCredits getCreditsForMovieID(int movieID)
        {
            MovieCredits mc;
            bool done = true;
            IRestResponse response;

            do
            {
                done = true;
                Console.WriteLine("Grabbing credits for movieID = " + movieID);
                var client = new RestClient("https://api.themoviedb.org/3/movie/" + movieID + "/credits");
                var request = new RestRequest(Method.GET);

                
                request.AddParameter("api_key", token, ParameterType.QueryString);


                request.AddParameter("undefined", "{}", ParameterType.RequestBody);

                response = client.Execute(request);

                if (response.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    Console.WriteLine("Waiting");
                    int waitTime = Int32.Parse(response.Headers[2].Value.ToString());
                    System.Threading.Thread.Sleep((waitTime + 2) * 1000);
                    done = false;
                }

                mc = JsonConvert.DeserializeObject<MovieCredits>(response.Content);

            } while (!done);

            return mc;
        }

        public MovieReviews[] getReviewsForMovieID(int movieID)
        {
            int i = 1;
            int total_pages = 0;

            List<MovieReviews> movieReviewList = new List<MovieReviews>();
            do
            {
                MovieReviews npr = GetReviewsForMovieByPage(movieID, i);
                total_pages = npr.total_pages;

                movieReviewList.Add(npr);

                i++;

            } while (i <= total_pages);
            return movieReviewList.ToArray();
        }

        public MovieReviews GetReviewsForMovieByPage(int movieID, int page)
        {
            MovieReviews mc;
            bool done = true;
            IRestResponse response;

            do
            {
                done = true;
                Console.WriteLine("Grabbing reviews for movieID = " + movieID + " page = " + page);
                var client = new RestClient("https://api.themoviedb.org/3/movie/" + movieID + "/reviews");
                var request = new RestRequest(Method.GET);


                request.AddParameter("page", page, ParameterType.QueryString);
                request.AddParameter("language", "en-US", ParameterType.QueryString);
                request.AddParameter("api_key", token, ParameterType.QueryString);


                request.AddParameter("undefined", "{}", ParameterType.RequestBody);

                response = client.Execute(request);

                if (response.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    Console.WriteLine("Waiting");
                    int waitTime = Int32.Parse(response.Headers[2].Value.ToString());
                    System.Threading.Thread.Sleep((waitTime + 2) * 1000);
                    done = false;
                }

                mc = JsonConvert.DeserializeObject<MovieReviews>(response.Content);

            } while (!done);

            return mc;
        }

        public class FullResponse
        {
            public List<NowPlayingResults> nowPlaying { get; set; }
            public Dictionary<int, MovieDetails> details { get; set; }
            public Dictionary<int, MovieCredits> credits { get; set; }
            public Dictionary<int, MovieReviews[]> reviews { get; set; }
        }

    }
}
