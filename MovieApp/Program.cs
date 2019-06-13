using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace MovieApp
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string regions = System.Configuration.ConfigurationManager.AppSettings["regions"];
            string[] splitRegions = regions.Split(',');

            RetrievalService rs = new RetrievalService(splitRegions);

            RetrievalService.FullResponse fr = rs.GetNowPlaying();

            var connStr = System.Configuration.ConfigurationManager.
                ConnectionStrings["movieDBConnStr"].ConnectionString;
            PersistenceService ps = new PersistenceService(connStr);

            ps.processData(fr);
            

        }
    }
}
