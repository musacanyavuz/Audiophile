using System;
using Audiophile.Models;
using Dapper.FastCrud;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Npgsql;

namespace Audiophile.Services
{
    public class BaseService : IDisposable
    {
        private MySqlConnection connection;
        // private string connectionString = "";


        //string connectionString = "Server=109.232.220.87;Database=test_audiophile;User Id=postgres;Password=wQb3jbFJ9meRdJ46";
        private readonly string connectionString = "";// "Server=109.232.220.87;Database=Audiophile;User Id=postgres;Password=wQb3jbFJ9meRdJ46";


        public BaseService()
        {
           connectionString = AppConfiguration.GetConnectionString();
            connection = new MySqlConnection(connectionString);
        }
       

        public MySqlConnection GetConnection()
        {
            OrmConfiguration.DefaultDialect = SqlDialect.MySql;
            if (connection != null)
                return connection;
            connection = new MySqlConnection(connectionString);
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