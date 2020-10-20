using System;
using System.Collections.Generic;
using System.Linq;
using Audiophile.Models;
using Dapper;
using Dapper.FastCrud;

namespace Audiophile.Services
{
    public class LogServices : BaseService
    {
        
        public bool Insert(Log log)
        {
            try
            {
                GetConnection().Insert(log);
                return log.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public List<Log> GetLog500() 
        { 
            string sql = "Select * from \"Logs\" WHERE \"CreatedDate\" is not null  ORDER BY \"CreatedDate\"  DESC limit 500 "; 
            return GetConnection().Query<Log>(sql).ToList(); 
        } 
    }
}
