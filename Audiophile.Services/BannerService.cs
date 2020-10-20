using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Audiophile.Models;
using Dapper;
using Dapper.FastCrud;

namespace Audiophile.Services
{
    public class BannerService : BaseService
    {
        public Banner Get(int id)
        {
            try
            {
                return GetConnection().Get(new Banner() {ID = id});
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public Banner GetWithUser(int id)
        {
            try
            {
                return GetConnection().Find<Banner>(s => s
                    .Where($"{nameof(Banner.ID):C}=@id")
                    .WithParameters(new {id})
                    .Include<User>()
                ).SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        public List<Banner> GetBanners()
        {
            try
            {
                return GetConnection().Find<Banner>(s => s
                    .Where($"{nameof(Banner.IsActive):C}=true and {nameof(Banner.StartDate):C}<now() and {nameof(Banner.EndDate):C}>now()")
                    //.WithParameters(new {start = DateTime.Now, end = DateTime.Now})
                ).ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public List<Banner> GetAllBanners()
        {
            return GetConnection().Find<Banner>(s=>s.Include<User>()).ToList();
        }
        
        public List<Banner> GetBannerHistroy(int userId)
        {
            try
            {
                return GetConnection().Find<Banner>(s => s
                    .Where($"{nameof(Banner.UserID):C}=@userId")
                    .WithParameters(new {userId})
                ).ToList();
            }
            catch (Exception e)
            {
                return null;
            }   
        }


        public bool Insert(Banner banner)
        {
            try
            {
                GetConnection().Insert(banner);
                return banner.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        
        public bool Update(Banner banner)
        {
            try
            {
                return GetConnection().Update(banner);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool Delete(int id)
        {
            return GetConnection().Delete(new Banner {ID = id});
        }

        public void IncraseClickCount(int id)
        {
            try
            {
                var sql = "update \"Banners\" set \"ClickCount\" = \"ClickCount\" + 1 where \"ID\"=@id";
                GetConnection().Execute(sql, new {id});
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}