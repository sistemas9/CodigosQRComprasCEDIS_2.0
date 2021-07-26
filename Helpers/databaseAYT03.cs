using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CodigosQRComprasCEDIS_2._0.Helpers
{
    public class DatabaseAYT03
    {
        IDataReader rd = null;
        int rowsAfected = 0;
        public SqlConnection conn;

        public DatabaseAYT03()
        {
            var AppSettings = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();

            String conectionString = AppSettings["DB_AYT03"];
            conn = new SqlConnection(conectionString);
            conn.Close();
        }

        public async Task<IDataReader> Query(String queryStr)
        {
            SqlCommand command = new SqlCommand(queryStr, conn);
            if (conn.State == ConnectionState.Closed)
                conn.Open();
            try
            {
                rd = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return rd;
        }

        public async Task<int> queryInsert(String queryStr)
        {
            SqlCommand command = new SqlCommand(queryStr, conn);
            if (conn.State == ConnectionState.Closed)
                await conn.OpenAsync();
            try
            {
                rowsAfected = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return -2;
            }
            return rowsAfected;
        }
    }
}
