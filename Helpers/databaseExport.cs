using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CodigosQRComprasCEDIS_2._0.Helpers
{
    class DatabaseExport
    {
        IDataReader rd = null;
        int rowsAfected = 0;
        public SqlConnection conn;

        public DatabaseExport()
        {
            var AppSettings = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

            String conectionString = AppSettings["DB_AYTEXPORT"];
            // conn = new SqlConnection(Environment.GetEnvironmentVariable("DB_AYTEXPORT").ToString());
            conn = new SqlConnection(conectionString);
            conn.Close();
        }

        public async Task<IDataReader> Query(String queryStr)
        {
            SqlCommand command = new SqlCommand(queryStr, conn);
            if (conn.State == ConnectionState.Closed)
                await conn.OpenAsync();
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

        public async Task<int> QueryInsert(String queryStr)
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
