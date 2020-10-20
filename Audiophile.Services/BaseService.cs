using System;
using Audiophile.Models;
using Dapper.FastCrud;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Audiophile.Services
{
    public class BaseService : IDisposable
    {
        private NpgsqlConnection connection;
        // private string connectionString = "";


        //string connectionString = "Server=109.232.220.87;Database=test_audiophile;User Id=postgres;Password=wQb3jbFJ9meRdJ46";
        private readonly string connectionString = "";// "Server=109.232.220.87;Database=Audiophile;User Id=postgres;Password=wQb3jbFJ9meRdJ46";


        public BaseService()
        {
           connectionString = AppConfiguration.GetConnectionString();
            connection = new NpgsqlConnection(connectionString);
        }
       

        public NpgsqlConnection GetConnection()
        {
            OrmConfiguration.DefaultDialect = SqlDialect.PostgreSql;
            if (connection != null)
                return connection;
            connection = new NpgsqlConnection(connectionString);
            return connection;
        }

        public void Log(Log log)
        {
            try
            {
                GetConnection().Insert(log);
            }
            catch (Exception)
            { }
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }

}