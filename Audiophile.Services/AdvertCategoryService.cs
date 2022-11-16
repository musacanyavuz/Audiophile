using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Audiophile.Models;
using Dapper;
using Dapper.FastCrud;

namespace Audiophile.Services
{
    public class AdvertCategoryService : BaseService
    {
        public AdvertCategory Get(int id, int lang = 1)
        {
            DbConnection cnn=null;
            try
            {
                 cnn = GetConnection();
                var sql = "select AdvertCategories.*, " +
                      "(select GetText(AdvertCategories.NameID, @lang)) as Name, " +
                      "(select GetText(AdvertCategories.DescriptionID, @lang)) as Description, " +
                      "(select GetText(AdvertCategories.SeoTitleID, @lang)) as SeoTitle, " +
                      "(select GetText(AdvertCategories.SeoKeywordsID, @lang)) as SeoKeywords, " +
                      "(select GetText(AdvertCategories.SeoDescriptionID, @lang)) as SeoDescription, " +
                      "(select GetText(AdvertCategories.SlugID, @lang)) as Slug, " +
                      "parent.*, " +
                      "(select GetText(parent.NameID, @lang)) as Name, " +
                      "(select GetText(parent.DescriptionID, @lang)) as Description, " +
                      "(select GetText(parent.SeoTitleID, @lang)) as SeoTitle, " +
                      "(select GetText(parent.SeoKeywordsID, @lang)) as SeoKeywords, " +
                      "(select GetText(parent.SeoDescriptionID, @lang)) as SeoDescription, " +
                      "(select GetText(parent.SlugID, @lang) ) " +
                      "from AdvertCategories LEFT OUTER JOIN AdvertCategories as parent on AdvertCategories.ParentCategoryID=parent.ID " +
                      "where AdvertCategories.ID = @id ";
                var x = cnn.Query<AdvertCategory,AdvertCategory,AdvertCategory>(sql,
                    (category, parentCategory) =>
                    {
                        category.ParentCategory = parentCategory;
                        return category;
                    },  new { lang, id }
                    ).SingleOrDefault();
               
                return x;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertCategoryService.Get", CreatedDate = DateTime.Now, Message = e.Message, Detail = e.InnerException?.Message,
                    IsError = true, Params = id.ToString() + " "+ lang.ToString()
                });
            }
            finally
            {
                cnn.Close();
                cnn.Dispose();
            }     
            return null;
        }
        
        public AdvertCategory Get(string slug, int lang = 1)
        {
            DbConnection cnn = null;
            try
            {
                cnn = GetConnection();
                var sql = "select AdvertCategories.*, " +
                      "(select GetText(AdvertCategories.NameID, @lang)) as Name, " +
                      "(select GetText(AdvertCategories.DescriptionID, @lang)) as Description, " +
                      "(select GetText(AdvertCategories.SeoTitleID, @lang)) as SeoTitle, " +
                      "(select GetText(AdvertCategories.SeoKeywordsID, @lang)) as SeoKeywords, " +
                      "(select GetText(AdvertCategories.SeoDescriptionID, @lang)) as SeoDescription, " +
                      "(select GetText(AdvertCategories.SlugID, @lang) ), " +
                      "parent.*, " +
                      "(select GetText(parent.NameID, @lang)) as Name, " +
                      "(select GetText(parent.DescriptionID, @lang)) as Description, " +
                      "(select GetText(parent.SeoTitleID, @lang)) as SeoTitle, " +
                      "(select GetText(parent.SeoKeywordsID, @lang)) as SeoKeywords, " +
                      "(select GetText(parent.SeoDescriptionID, @lang)) as SeoDescription, " +
                      "(select GetText(parent.SlugID, @lang) ) " +
                      "from AdvertCategories LEFT OUTER JOIN AdvertCategories as parent on AdvertCategories.ParentCategoryID=parent.ID " +
                      "where (select GetText(AdvertCategories.SlugID, @lang)) like @slug ";
                var x = GetConnection().Query<AdvertCategory,AdvertCategory,AdvertCategory>(sql,
                    (category, parentCategory) =>
                    {
                        category.ParentCategory = parentCategory;
                        return category;
                    },  new { lang, slug }
                    ).SingleOrDefault(); 
                return x;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertCategoryService.Get", CreatedDate = DateTime.Now, Message = e.Message, Detail = e.InnerException?.Message,
                    IsError = true, Params = slug + " " + lang.ToString() 
                });
            }
            finally
            {
                cnn.Close();
                cnn.Dispose();
            }
            return null;
        }

        public bool Update(AdvertCategory advertCategory)
        {
            return GetConnection().Update(advertCategory);
        }

        public bool Insert(AdvertCategory advertCategory)
        {
            GetConnection().Insert(advertCategory);
            return advertCategory.ID > 0;
        }
        
        public List<AdvertCategory> GetMasterCategories(int lang = 1)
        {
            DbConnection cnn = null;
            try
            {
                cnn = GetConnection();
                var sql = "select *, "+
                          "(select GetText(NameID, @lang)) as Name, "+
                          "    (select GetText(DescriptionID, @lang)) as Description, "+
                          "    (select GetText(SeoTitleID, @lang)) as SeoTitle, "+
                          "    (select GetText(SeoKeywordsID, @lang)) as SeoKeywords, "+
                          "    (select GetText(SeoDescriptionID, @lang)) as SeoDescription, "+
                          "    (select GetText(SlugID, @lang)) as Slug "+
                          "from AdvertCategories " +
                          "where ParentCategoryID is null or ParentCategoryID=0 " +
                          "and AdvertCategories.IsActive=true " +
                          "order by `Order` ";
                DynamicParameters pars = new DynamicParameters();
                pars.Add("@lang",lang);
                var x = cnn.Query<AdvertCategory>(sql, pars).ToList(); 
                return x;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertCategoryService.GetMasterCategories", CreatedDate = DateTime.Now, Message = e.Message, Detail = e.InnerException?.Message,
                    IsError = true, Params = lang.ToString()
                });
            }
            finally
            {
                cnn.Close();
                cnn.Dispose();
            }
            return null;
        }

        public List<AdvertCategory> GetAll(int lang = 1)
        {
            DbConnection cnn = null;
            try
            {
                cnn = GetConnection();
                var sql = "select *, "+
                          "(select GetText(NameID, @lang)) as Name, "+
                          "    (select GetText(DescriptionID, @lang)) as Description, "+
                          "    (select GetText(SeoTitleID, @lang)) as SeoTitle, "+
                          "    (select GetText(SeoKeywordsID, @lang)) as SeoKeywords, "+
                          "    (select GetText(SeoDescriptionID, @lang)) as SeoDescription, "+
                          "    (select GetText(SlugID, @lang)) as Slug "+
                          "from AdvertCategories " +
                          "where IsActive=true "+
                          "order by \"Order\" ";  
                var categories = cnn.Query<AdvertCategory>(sql, new { lang }).ToList();
                var masterCategories = categories.Where(x => x.ParentCategoryID == 0).ToList();
                var childCategories = categories.Where(x => x.ParentCategoryID > 0).ToList();
                
                
                masterCategories.ForEach(x=>x.ChildCategories = childCategories.Where(c=>c.ParentCategoryID==x.ID) );
                return masterCategories;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertCategoryService.GetAll", CreatedDate = DateTime.Now, Message = e.Message, Detail = e.InnerException?.Message,
                    IsError = true, Params = lang.ToString() 
                });
            }
            finally
            {
                if(cnn != null && cnn.State != System.Data.ConnectionState.Closed)
                {
                    cnn.Close();
                    cnn.Dispose();
                }
               
            }
            return null;
        }

        public List<AdvertCategory> GetAllCategories(int lang = 1)
        {
            List<AdvertCategory> query = null;
            DbConnection cnn = null;
            try
            {
                cnn = GetConnection();
                //TODO tüm kategoriler çekilecek, parent kategori propertysi dolu olarak
                var sql = "select AdvertCategories.*,\n  (select GetText(AdvertCategories.NameID, @lang)) as Name,\n  (select GetText(AdvertCategories.DescriptionID, @lang)) as Description,\n  (select GetText(AdvertCategories.SeoTitleID, @lang)) as SeoTitle,\n  (select GetText(AdvertCategories.SeoKeywordsID, @lang)) as SeoKeywords,\n  (select GetText(AdvertCategories.SeoDescriptionID, @lang)) as SeoDescription,\n  (select GetText(AdvertCategories.SlugID, @lang)) as Slug,\n  Parent.*,\n  (select GetText(Parent.NameID, @lang)) as Name,\n  (select GetText(Parent.DescriptionID, @lang)) as Description,\n  (select GetText(Parent.SeoTitleID, @lang)) as SeoTitle,\n  (select GetText(Parent.SeoKeywordsID, @lang)) as SeoKeywords,\n  (select GetText(Parent.SeoDescriptionID, @lang)) as SeoDescription,\n  (select GetText(Parent.SlugID, @lang)) as Slug\nfrom\n     AdvertCategories left outer join AdvertCategories as Parent on AdvertCategories.ParentCategoryID=Parent.ID\n\norder by AdvertCategories.Order";
                query = GetConnection().Query<AdvertCategory, AdvertCategory, AdvertCategory>(sql,
                    (category, parent) =>
                    {
                        category.ParentCategory = parent;
                        return category;
                    }, splitOn: "ID", param: new { lang }).ToList();
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertCategoryService.GetAll",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true,
                    Params = lang.ToString()
                });
            }
            finally
            {
                if (cnn != null && cnn.State != System.Data.ConnectionState.Closed)
                {
                    cnn.Close();
                    cnn.Dispose();
                }

            }
            return query;
        }
        /// <summary>
        /// Ajax olmayan urlden gelen aramalar.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public int GetAdvertsCount(AdvertCategory category)
        {
            try
            {
                var sql = " select count(*) from Adverts where CategoryID=@categoryID  ";
                if (category.ParentCategoryID == 0)
                {
                    //sql = "select count(Adverts.ID) from Adverts,AdvertCategories,AdvertCategories as ParentCategories " +
                    //      "where Adverts.IsActive=true " +
                    //      "  and Adverts.CategoryID = AdvertCategories.ID " +
                    //      "  and AdvertCategories.ParentCategoryID = ParentCategories.ID " +
                    //      " and AdvertCategories.ParentCategoryID = @categoryID" +
                    //      "  and ( AdvertCategories.ID=@categoryID or ParentCategories.ID = AdvertCategories.ParentCategoryID )";

                    sql = @"select count(*) from  Adverts  where CategoryID in (select ID from AdvertCategories ac   where ParentCategoryID =@categoryID)";
                }
                
                var count = GetConnection().Query<int>(sql, new {categoryID = category.ID}).SingleOrDefault();
                return count;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertCategoryService.GetAdvertsCount", CreatedDate = DateTime.Now, Message = e.Message, Detail = e.InnerException?.Message,
                    IsError = true, Params = category.ID.ToString()
                });
            }
            return 0;
        }
    }
}