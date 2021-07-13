using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Transactions;
using Audiophile.Common;
using Audiophile.Models;
using Dapper;
using Dapper.FastCrud;
using Dapper.FastCrud.Configuration.StatementOptions.Builders;
using Dapper.FastCrud.Mappings;
using Npgsql;

namespace Audiophile.Services
{
    public class AdvertService : BaseService
    {
        #region Dopingli_Listelemeler_Algoritmik

        public List<Advert> GetHomePageAdverts(int lang = 1, int userId = 0)
        {
            try
            {
                //var sql_ = "  SELECT\n    \"Adverts\".*,\n  " +
                //         "( SELECT count(*) AS count FROM \"AdvertLikes\" WHERE (\"AdvertLikes\".\"AdvertID\" = \"Adverts\".\"ID\")) AS \"LikesCount\",\n  " +
                //         "( SELECT \"GetLabelDoping\"((\"Adverts\".\"ID\")) AS \"GetLabelDoping\") AS \"LabelDoping\",\n  " +
                //         "( SELECT \"GetYellowFrameDoping\"((\"Adverts\".\"ID\")) AS \"GetYellowFrameDoping\") AS \"YellowFrameDoping\",\n " +
                //         " ( SELECT \"IsILiked\"(\"Adverts\".\"ID\", @userId ) ) as \"IsILiked\",\n  \"GetText\"((\"AdvertCategories\".\"SlugID\"), (1)) AS \"SubCategorySlugTr\",\n " +
                //         " \"GetText\"((\"ParentCategory\".\"SlugID\"), (1)) AS \"ParentCategorySlugTr\",\n  " +
                //         "\"GetText\"((\"AdvertCategories\".\"SlugID\"), (2)) AS \"SubCategorySlugEn\",\n  " +
                //         "\"GetText\"((\"ParentCategory\".\"SlugID\"), (2)) AS \"ParentCategorySlugEn\", \n  " +
                //         "\"GetText\"((\"AdvertCategories\".\"NameID\"), (1)) AS \"CategoryNameTr\", " +
                //         " \"GetText\"((\"AdvertCategories\".\"NameID\"), (2)) AS \"CategoryNameEn\", " +
                //         "\"Users\".\"Name\" AS UserName " +
                //         "FROM \"Adverts\",\"Users\", \"AdvertCategories\", \"AdvertCategories\" \"ParentCategory\"\n\n  " +
                //         "WHERE " +
                //         "\"Adverts\".\"IsActive\" = true\n\n  " +
                //         "AND \"Adverts\".\"CategoryID\" = \"AdvertCategories\".\"ID\"\n\n  " +
                //         "AND \"Adverts\".\"UserID\" = \"Users\".\"ID\"\n\n  " +
                //         "AND (\"AdvertCategories\".\"ParentCategoryID\" = \"ParentCategory\".\"ID\")\n\n  AND (( SELECT \"Users\".\"IsActive\"  FROM \"Users\" WHERE (\"Users\".\"ID\" = \"Adverts\".\"UserID\")) = true)\n\n  AND (\n            (select count(*)\n             from \"AdvertDopings\"\n             where \"AdvertID\" = \"Adverts\".\"ID\"\n               and \"StartDate\" < now()\n               and \"EndDate\" > now()\n               and \"IsActive\" = true\n               and \"TypeID\" in (1, 2)) > 0\n      OR\n                \"Adverts\".\"UseSecurePayment\" = true\n            )\n  ORDER BY \"Adverts\".\"LastUpdateDate\" DESC,\"Adverts\".\"CreatedDate\" DESC;";

                var sql = "  SELECT\n    Adverts.*,\n  " +
                          "( SELECT count(*) AS count FROM AdvertLikes WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n  " +
                          "( SELECT GetLabelDoping((Adverts.ID)) AS GetLabelDoping) AS LabelDoping,\n  " +
                          "( SELECT GetYellowFrameDoping((Adverts.ID)) AS GetYellowFrameDoping) AS YellowFrameDoping,\n " +
                          " ( SELECT IsILiked(Adverts.ID, @userId ) ) as IsILiked,\n  GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n " +
                          " GetText((ParentCategory.SlugID), (1)) AS ParentCategorySlugTr,\n  " +
                          "GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn,\n  " +
                          "GetText((ParentCategory.SlugID), (2)) AS ParentCategorySlugEn, \n  " +
                          "GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, " +
                          " GetText((AdvertCategories.NameID), (2)) AS CategoryNameEn, " +
                          "Users.Name AS UserName " +
                          "FROM Adverts,Users, AdvertCategories, AdvertCategories ParentCategory\n\n  " +
                          "WHERE " +
                          "Adverts.IsActive = true\n\n  " +
                          "AND Adverts.CategoryID = AdvertCategories.ID\n\n  " +
                          "AND Adverts.UserID = Users.ID\n\n  " +
                          "AND (AdvertCategories.ParentCategoryID = ParentCategory.ID)\n\n  AND (( SELECT Users.IsActive  FROM Users WHERE (Users.ID = Adverts.UserID)) = true)\n\n  AND (\n            (select count(*)\n             from AdvertDopings\n             where AdvertID = Adverts.ID\n               and StartDate < now()\n               and EndDate > now()\n               and IsActive = true\n               and TypeID in (1, 2)) > 0\n      OR\n                Adverts.UseSecurePayment = true\n            )\n  ORDER BY Adverts.LastUpdateDate DESC,Adverts.CreatedDate DESC;";

                var list = GetConnection().Query<Advert>(sql, new { userId }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                        x.LabelDopingModel = (x.LabelDoping != 0) ? dopingTypes.Single(d => d.ID == x.LabelDoping) : null;
                    });
                }

                CheckAdvertsIsDraft(ref list);
                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetHomePageAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true,
                    Params = ""
                });
            }
            return null;
        }

        public List<Advert> GetHomePageAdverts(int lang, int maxId, int userId = 0, int limit = 10, bool maxToMin = false)
        {
            try
            {
                var sql = "SELECT * FROM (\n SELECT ( SELECT GetOrderFromDopingInHomepage((Adverts.ID)) AS GetOrderFromDopingInHomepage) AS AdvertOrder,\n    Adverts.*,\n    ( SELECT count(*) AS count\n           FROM AdvertLikes\n          WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n    ( SELECT GetLabelDoping((Adverts.ID)) AS GetLabelDoping) AS LabelDoping,\n    ( SELECT GetYellowFrameDoping((Adverts.ID)) AS GetYellowFrameDoping) AS YellowFrameDoping,\n    ( SELECT IsILiked(Adverts.ID, @userId ) ) as IsILiked,\n    GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n    GetText((ParentCategory.SlugID), (1)) AS ParentCategorySlugTr,\n    GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn,\n    GetText((ParentCategory.SlugID), (2)) AS ParentCategorySlugEn\n   FROM Adverts,\n    AdvertCategories,\n    AdvertCategories ParentCategory\n  WHERE Adverts.IsActive = true AND (Adverts.CategoryID = AdvertCategories.ID)\n           AND (AdvertCategories.ParentCategoryID = ParentCategory.ID)\n           AND (( SELECT Users.IsActive FROM Users WHERE (Users.ID = Adverts.UserID)) = true)\n  AND Adverts.ID @maxOperator @maxId\n     ) " +
                          " tbl WHERE AdvertOrder > 1\n  " +
                          " ORDER BY CreatedDate DESC " +
                          "";
                if (limit > 0)
                    sql += " LIMIT  @limit;";

                sql = sql.Replace("@maxOperator", !maxToMin ? "<" : ">=");

                var list = GetConnection().Query<Advert>(sql, new { maxId, userId, limit }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                        x.LabelDopingModel = (x.LabelDoping != 0) ? dopingTypes.Single(d => d.ID == x.LabelDoping) : null;
                    });
                }
                CheckAdvertsIsDraft(ref list);
                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "GetAdvertCategories",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true,
                    Params = ""
                });
            }
            return null;
        }

        public List<Advert> GetCategoryPageAdverts(int categoryId, int parentCategoryId, int lang, int maxId, int userId, bool limit = true, int offset = 0)
        {
            try
            {
                var sqlBase = "SELECT\n  " +
                              "GetOrderFromDopingInCategoryPage(Adverts.ID) as AdvertOrder,\n  " +
                              "Adverts.*,\n  ( SELECT count(*) AS count FROM AdvertLikes WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n " +
                              " ( SELECT GetLabelDoping((Adverts.ID)) AS GetLabelDoping) AS LabelDoping,\n " +
                              " ( SELECT GetYellowFrameDoping((Adverts.ID)) AS GetYellowFrameDoping) AS YellowFrameDoping,\n " +
                              " ( SELECT IsILiked(Adverts.ID, @userId )) as IsILiked,\n " +
                              " GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n  " +
                              "GetText((ParentCategory.SlugID), (1)) AS ParentCategorySlugTr,\n  " +
                              "GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn,\n  " +
                              "GetText((ParentCategory.SlugID), (2)) AS ParentCategorySlugEn,\n " +
                              "GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, " +
                              " GetText((AdvertCategories.NameID), (2)) AS CategoryNameEn, " +
                              "Users.Name AS UserName " +
                              " FROM " +
                              " Adverts,Users, AdvertCategories, AdvertCategories ParentCategory\n\n  WHERE   Adverts.UserID = Users.ID  AND Adverts.CategoryID = AdvertCategories.ID\n\n  AND (AdvertCategories.ParentCategoryID = ParentCategory.ID)\n\n  AND (SELECT Users.IsActive  FROM Users WHERE (Users.ID = Adverts.UserID)) = true";
                var sql = String.Copy(sqlBase);

                if (parentCategoryId != 0)
                {
                    sql += " AND ParentCategory.ID = @parentCategoryId ";
                }

                if (categoryId != 0)
                {
                    sql += " AND AdvertCategories.ID = @categoryId ";
                }
                //Advert.IsActive =

                //if (maxId > 0)
                //{
                //    sql += " AND Adverts.ID < @maxId ";
                //}
                sql += " order by AdvertOrder, CreatedDate desc";
                if (limit)
                {
                    sql += " limit 40";
                }

                if (offset > 0)
                {
                    sql += $" offset {offset};";
                }

                var list = GetConnection().Query<Advert>(sql, new { categoryId, parentCategoryId, userId, maxId }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    //aktif dile göre ilanlaraın kategori slug'larını günceller.
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                        x.LabelDopingModel = (x.LabelDoping != 0) ? dopingTypes.Single(d => d.ID == x.LabelDoping) : null;
                    });
                }
                CheckAdvertsIsDraft(ref list);
                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetCategoryPageAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true,
                    Params = ""
                });
            }
            return null;
        }


        public List<Advert> GetSimilarAdverts(int categoryId, int limit = 4, int lang = 1)
        {
            try
            {
                var sql = "select " +
                          "Adverts.*, " +
                          "(GetText( (select SlugID from AdvertCategories where AdvertCategories.ID= (SELECT ParentCategoryID from AdvertCategories where AdvertCategories.ID=Adverts.CategoryID) ) ,@lang)) as CategorySlug, " +
                          "(GetText( (select SlugID from AdvertCategories where AdvertCategories.ID=Adverts.CategoryID) ,@lang)) as SubCategorySlug, " +
                          "AdvertCategories.*,Users.*,ParentCategories.* " +
                          "from " +
                          "Adverts, AdvertCategories, Users, AdvertCategories as ParentCategories " +
                          "where " +
                          "Adverts.UserID=Users.ID " +
                          "and Adverts.CategoryID = AdvertCategories.ID " +
                          "and AdvertCategories.ParentCategoryID = ParentCategories.ID " +
                          //"and Adverts.IsActive = true " +
                          "and Users.IsActive = true " +
                          "and AdvertCategories.ID=@categoryId " +
                          "order by Adverts.CreatedDate desc " +
                          "LIMIT " + limit;
                var result = GetConnection().Query<Advert, AdvertCategory, User, AdvertCategory, Advert>(sql,
                    (advert, category, user, parentCategory) =>
                    {
                        advert.Category = category;
                        advert.Category.ParentCategory = parentCategory;
                        advert.User = user;
                        return advert;
                    }, splitOn: "ID", param: new { categoryId, lang }).ToList();
                CheckAdvertsIsDraft(ref result);
                return result;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetSimilarAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true
                });
                return null;
            }
        }

        #endregion

        #region Base

        public Advert Get(int id)
        {
            try
            {
                var sql = "select " +
                          "Adverts.*, " +
                          "(SELECT count(*) AS count           FROM AdvertLikes          WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount," +
                          "(GetText(AdvertCategories.SlugID, 1)) as SubCategorySlugTr, " +
                          "(GetText(ParentCategory.SlugID, 1)) as ParentCategorySlugTr, " +
                          "(GetText(AdvertCategories.SlugID, 2)) as SubCategorySlugEn, " +
                          "(GetText(ParentCategory.SlugID, 2)) as ParentCategorySlugEn, " +
                          " " +
                          "AdvertCategories.*,Users.*,ParentCategory.* " +
                          " " +
                          "from " +
                          "Adverts, AdvertCategories, Users, AdvertCategories as ParentCategory " +
                          "where " +
                          "Adverts.UserID=Users.ID " +
                          "and Adverts.CategoryID = AdvertCategories.ID " +
                          "and AdvertCategories.ParentCategoryID = ParentCategory.ID " +
                          //"and Adverts.IsActive = true " +
                          "and Users.IsActive = true " +
                          "and Adverts.ID = @id; " +
                          $"select * from AdvertPhotos where AdvertID = @id;";
                using (var multi = GetConnection().QueryMultiple(sql, new { id }))
                {
                    var ad = multi.Read<Advert, AdvertCategory, User, AdvertCategory, Advert>(
                        (advert, category, user, parentCategory) =>
                        {
                            advert.Category = category;
                            advert.Category.ParentCategory = parentCategory;
                            advert.User = user;
                            return advert;
                        }, splitOn: "ID").SingleOrDefault();
                    if (ad == null)
                        return null;

                    ad.Photos = multi.Read<AdvertPhoto>()?.OrderBy(p => p.OrderNumber).ToList();
                    return ad;
                }
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.Get",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true
                });
                return null;
            }
        }

        public Advert GetAdvert(int id) //admin panelden tüm ilanlara erişebilmek için
        {
            try
            {
                var sql = "select\n  Adverts.*,\n  GetText(AdvertCategories.NameID, 1) as CategorySlug,\n  Users.*\nfrom Adverts, AdvertCategories, Users\nwhere Adverts.CategoryID = AdvertCategories.ID\nand Adverts.UserID = Users.ID\nand Adverts.ID = @id; " +
                          $"select * from AdvertPhotos where AdvertID = @id;" +
                          $" select * from AdvertDopings, DopingTypes where AdvertID = @id and TypeID = DopingTypes.ID;";

                using (var multi = GetConnection().QueryMultiple(sql, new { id }))
                {
                    var ad = multi.Read<Advert, User, Advert>(
                        (advert, user) =>
                        {
                            advert.User = user;
                            return advert;
                        }, splitOn: "ID").SingleOrDefault();
                    if (ad == null)
                        return null;

                    ad.Photos = multi.Read<AdvertPhoto>().ToList();

                    ad.AdvertDopings = multi.Read<AdvertDoping, DopingType, AdvertDoping>(
                        (doping, type) =>
                        {
                            doping.DopingType = type;
                            return doping;
                        }, splitOn: "ID")
                    .ToList();

                    return ad;
                }
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.Get",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true
                });
                return null;
            }
        }

        public bool Insert(Advert advert)
        {
            try
            {
                GetConnection().Insert(advert);
                return advert.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool Update(Advert advert)
        {
            try
            {
                return GetConnection().Update(advert);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool Delete(int id)
        {
            try
            {
                return GetConnection().Delete(new Advert() { ID = id });
            }
            catch (Exception e)
            {

                Log(new Log
                {
                    Function = "AdvertServie.DeletePublishRequests",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true,
                    Params = "advertId" + id.ToString()
                });
                return false;
            }


        }

        public Advert GetMyAdvert(int id, int userId)
        {
            var ad = GetConnection().Find<Advert>(s => s
                    .Include<AdvertCategory>()
                    .Where($"{nameof(Advert.ID):C}=@id and {nameof(Advert.UserID):C}=@userId")
                    .WithParameters(new { id, userId }))
                .SingleOrDefault();
            if (ad != null)
            {
                var request = GetConnection().Find<AdvertPublishRequest>(s => s
                    .Where($"{nameof(AdvertPublishRequest.AdvertID):C}=@id and {nameof(AdvertPublishRequest.IsActive):C}=true")
                    .OrderBy($"{nameof(AdvertPublishRequest.ID):C} DESC")
                    .WithParameters(new { id })
                ).FirstOrDefault();
                ad.IsPendingApproval = request != null; //request null değil ise yayınlanma onayı bekliyor demektir.
            }
            return ad;
        }

        public List<Advert> GetUserAdverts(int userId)
        {
            try
            {
                var sql = "select Adverts.*, " +
                          "GetIsPendingApproval(Adverts.ID) as IsPendingApproval, Users.* " +
                          "from Adverts, Users where Adverts.UserID=@userId and Adverts.UserID=Users.ID " +
                          " " +
                          " " +
                          " ";
                var list = GetConnection().Query<Advert, User, Advert>(sql, (advert, user) =>
                {
                    advert.User = user;
                    return advert;
                }, new { userId }, splitOn: "ID").ToList();
                CheckAdvertsIsDraft(ref list);
                return list;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public List<Advert> GetAdverts()
        {
            var sql = "select\n    Adverts.*,\n    GetText(AdvertCategories.NameID,1) as CategorySlug,\n    Users.*\nfrom Adverts, Users, AdvertCategories\nwhere Adverts.UserID = Users.ID\nand Adverts.CategoryID = AdvertCategories.ID";
            var query = GetConnection().Query<Advert, User, Advert>(sql, (advert, user) =>
            {
                advert.User = user;
                return advert;
            }, splitOn: "ID").ToList();
            CheckAdvertsIsDraft(ref query);
            return query;
        }

        #endregion

        #region Filter


        public List<Advert> FilterAdverts(int parentCategoryId, int categoryId, string content, string brand,
            string model, int advertId, int minPrice, int maxPrice, int lang, int userId, int passivFilterStatus)
        {
            try
            {
                var list = GetCategoryPageAdverts(categoryId, parentCategoryId, lang, 0, userId, false);
                if (!string.IsNullOrEmpty(content))
                {
                    content = content.ToLower();
                    list = list.Where(x => string.IsNullOrEmpty(x.Content) == false && string.IsNullOrEmpty(x.Title) == false && x.Content.ToLower().Contains(content) || x.Title.ToLower().Contains(content)).ToList();
                }
                if (!string.IsNullOrEmpty(brand))
                {
                    brand = brand.ToLower();
                    list = list.Where(x => string.IsNullOrEmpty(x.Brand) == false && x.Brand.ToLower().Contains(brand)).ToList();
                }
                if (!string.IsNullOrEmpty(model))
                {
                    model = model.ToLower();
                    list = list.Where(x => string.IsNullOrEmpty(x.Model) == false && x.Model.ToLower().Contains(model)).ToList();
                }
                if (advertId > 0)
                {
                    list = list.Where(x => x.ID.ToString().Contains(advertId.ToString())).ToList();
                }
                if (minPrice > 0)
                {
                    list = list.Where(x => x.Price >= minPrice).ToList();
                }
                if (maxPrice > 0)
                {
                    list = list.Where(x => x.Price <= maxPrice).ToList();
                }
                if (passivFilterStatus == 1)
                {
                    list = list.Where(x => x.IsActive == true).ToList();
                }
                CheckAdvertsIsDraft(ref list);
                return list;
            }
            catch (Exception e)
            {
                return null;
            }
        }


        #endregion

        #region PublishRequest

        public void DeletePublishRequests(int advertId)
        {
            try
            {
                var sql = $"DELETE FROM AdvertPublishRequests where AdvertID=@advertId ";
                GetConnection().Execute(sql, new { advertId });
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertServie.DeletePublishRequests",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true,
                    Params = "advertId" + advertId.ToString()
                });
            }
        }
        public void DeactivetePublishRequests(int advertId)
        {
            try
            {
                var sql = $"UPDATE AdvertPublishRequests SET IsActive = false where AdvertID=@advertId ";
                GetConnection().Execute(sql, new { advertId });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public AdvertPublishRequest GetPublishRequest(int id)
        {
            try
            {
                return GetConnection().Find<AdvertPublishRequest>(s => s
                    .Where($"{nameof(AdvertPublishRequest.ID):C}=@id")
                    .WithParameters(new { id })
                    .Include<Advert>()
                    .Include<User>()
                ).SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public List<AdvertPublishRequest> GetPublishRequests()
        {
            var sql = "select\n   AdvertPublishRequests.*,\n   Adverts.*,\n   GetText(AdvertCategories.NameID, 1) as CategorySlug,\n   Users.*\nfrom\n  AdvertPublishRequests, Adverts, AdvertCategories, Users\nwhere\n    AdvertPublishRequests.AdvertID = Adverts.ID\nand Adverts.UserID = Users.ID\nand Adverts.CategoryID = AdvertCategories.ID\nand AdvertPublishRequests.IsActive = true \n and Adverts.IsDraft=false";

            var list = GetConnection().Query<AdvertPublishRequest, Advert, User, AdvertPublishRequest>(sql,
                (request, advert, user) =>
                {
                    request.Advert = advert;
                    request.Advert.User = user;
                    return request;
                }, splitOn: "ID").ToList();

            return list;
        }

        public bool InsertPublishRequest(AdvertPublishRequest publishRequest)
        {
            try
            {
                GetConnection().Insert(publishRequest);
                return publishRequest.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool Update(AdvertPublishRequest publishRequest)
        {
            try
            {
                return GetConnection().Update(publishRequest);
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public bool UpdateAdvertPhotos(int advertId, int[] advertPhotos)
        {
            if (advertPhotos != null && advertPhotos.Length > 0)
            {
                var ids = "";
                foreach (var id in advertPhotos)
                {
                    ids += id + ",";
                }
                ids = ids.Substring(0, ids.Length - 1);
                var sql = $"UPDATE AdvertPhotos set AdvertID=@advertId where ID in (" + ids + ") ";
                return GetConnection().Execute(sql, new { advertId }) > 0;
            }
            return false;
        }
        #endregion

        public List<Advert> GetList(string search, int count, int offset)
        {
            var sql = $"select Adverts.*, " +
                      $"(select GetText((select NameID from AdvertCategories where ID = Adverts.CategoryID), 1 )) as CategorySlug," +
                      $" Users.* " +
                      $"from Adverts, Users " +
                      $"where UserID=Users.ID " +
                      $"order by Adverts.ID desc limit @count offset @offset";
            if (!string.IsNullOrEmpty(search))
            {
                sql = $"select Adverts.*, " +
                      $"(select GetText((select NameID from AdvertCategories where ID = Adverts.CategoryID), 1 )) as CategorySlug," +
                      $" Users.* " +
                      $" from Adverts, Users " +
                      $"where UserID=Users.ID and (" +
                      $"Title like @search or Content like @search or Brand like @search\nor " +
                      $" (select GetText((select NameID from AdvertCategories where ID = Adverts.CategoryID), 1 )) like @search  or " +
                      $" Users.Name like @search or " +
                      $"cast(Adverts.ID as varchar) like @search ) " +
                      $"order by Adverts.ID desc limit @count offset @offset";
            }
            var list = GetConnection().Query<Advert, User, Advert>(sql, (advert, user) =>
            {
                advert.User = user;
                return advert;
            }, new { search = "%" + search + "%", count, offset }, splitOn: "ID")
            .ToList();
            //CheckAdvertsIsDraft(ref list);
            return list;
        }

        public int GetAdvertsCount(string search = null)
        {
            if (string.IsNullOrEmpty(search))
            {
                return GetConnection().Query<int>($"select count(*) from Adverts, Users where UserID=Users.ID").First();
            }

            var sql = $"select count(Adverts.ID)\nfrom Adverts\nwhere\n  Title like @search\n  or Content like @search\n  or Brand like @search\n  or  (select GetText((select NameID from AdvertCategories where ID = Adverts.CategoryID), 1 )) like @search\n  or (select Name from Users where Users.ID=UserID) like @search\n  or cast(Adverts.ID as varchar) like @search";
            var count = GetConnection().Query<int>(sql, new { search = "%" + search + "%" }).First();
            return count;
        }

        public List<Advert> Search(string query, int userId, int lang = 1)
        {
            try
            {
                var sql =
                      "SELECT ( SELECT GetOrderFromDopingInHomepage((Adverts.ID)) AS GetOrderFromDopingInHomepage) AS AdvertOrder,\n       Adverts.*,    " +
                      "( SELECT count(*) AS count           FROM AdvertLikes          WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n       ( SELECT GetLabelDoping((Adverts.ID)) AS GetLabelDoping) AS LabelDoping,\n       ( SELECT GetYellowFrameDoping((Adverts.ID)) AS GetYellowFrameDoping) AS YellowFrameDoping,\n       ( SELECT IsILiked(Adverts.ID, @userId ) ) as IsILiked,    GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n       GetText((ParentCategory.SlugID), (1)) AS ParentCategorySlugTr,    GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn,\n       GetText((ParentCategory.SlugID), (2)) AS ParentCategorySlugEn\nFROM Adverts,    AdvertCategories,    AdvertCategories ParentCategory ,Users \nWHERE (Adverts.CategoryID = AdvertCategories.ID)\n  AND (AdvertCategories.ParentCategoryID = ParentCategory.ID)\n  AND (Adverts.UserID=Users.ID ) \n  AND (Users.IsActive=true) \n AND (Adverts.Title like @query or Adverts.Content like @query or Adverts.ProductDefects like @query or Adverts.ID like @query or Users.Name like @query )\nORDER BY AdvertOrder, CreatedDate DESC limit 200;";
                //"SELECT ( SELECT GetOrderFromDopingInHomepage((Adverts.ID)) AS GetOrderFromDopingInHomepage) AS AdvertOrder,\n       Adverts.*,    ( SELECT count(*) AS count           FROM AdvertLikes          WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n       ( SELECT GetLabelDoping((Adverts.ID)) AS GetLabelDoping) AS LabelDoping,\n       ( SELECT GetYellowFrameDoping((Adverts.ID)) AS GetYellowFrameDoping) AS YellowFrameDoping,\n       ( SELECT IsILiked(Adverts.ID, @userId ) ) as IsILiked,    GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n       GetText((ParentCategory.SlugID), (1)) AS ParentCategorySlugTr,    GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn,\n       GetText((ParentCategory.SlugID), (2)) AS ParentCategorySlugEn\nFROM Adverts,    AdvertCategories,    AdvertCategories ParentCategory ,Users \nWHERE (Adverts.CategoryID = AdvertCategories.ID)\n  AND (AdvertCategories.ParentCategoryID = ParentCategory.ID)\n  AND (Adverts.UserID=Users.ID ) \n  AND (Users.IsActive=true) \n AND (Adverts.Title ilike @query or Adverts.Content ilike @query or Adverts.ProductDefects ilike @query or Adverts.ID::text ilike @query or Users.Name ilike @query )\nORDER BY AdvertOrder, CreatedDate DESC limit 200;";
                var list = GetConnection().Query<Advert>(sql, new { query = "%" + query + "%", userId, lang }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                        x.LabelDopingModel = (x.LabelDoping != 0) ? dopingTypes.Single(d => d.ID == x.LabelDoping) : null;
                    });
                }
                CheckAdvertsIsDraft(ref list);
                //aktif dile göre ilanlaraın kategori slug'larını günceller.
                return list;
            }
            catch (Exception e)
            {
                throw;
                return new List<Advert>();
            }

        }


        public void UpdateViewCount(int id)
        {
            try
            {
                GetConnection().Execute("update Adverts set ViewCount = ViewCount + 1 where ID = @id",
                    new { id });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public bool BulkInsert(List<Advert> list)
        {
            try
            {
                var sql = "INSERT INTO Adverts ( " +
                          " ID,Title,Content,NewProductPrice,Price,IsAvailableForSwap,IsAvailableBargain,PaymentMethodID,FreeShipping,ShippingTypeID, " +
                          " OriginalBox,TermsOfUse,ProductStatus,WebSite,CategoryID,CreatedDate, LastUpdateDate,UserID,ViewCount,MoneyTypeID, " +
                          " StockAmount,IsActive,ShippingPrice,UseSecurePayment,ProductDefects,IsDeleted,Brand,AvailableInstallments,Model " +
                          ") " +
                          " values ( " +
                          "@ID, @Title, @Content, @NewProductPrice, @Price, @IsAvailableForSwap, @IsAvailableBargain, @PaymentMethodID, @FreeShipping, @ShippingTypeID, @OriginalBox, @TermsOfUse, @ProductStatus, @WebSite, " +
                          "@CategoryID, @CreatedDate, @LastUpdateDate, @UserID, @ViewCount, @MoneyTypeID, @StockAmount, @IsActive, @ShippingPrice, @UseSecurePayment, @ProductDefects, @IsDeleted,   " +
                          "@Brand, @AvailableInstallments, @Model" +
                          ") ";
                GetConnection().Open();
                IDbTransaction trans = GetConnection().BeginTransaction();
                GetConnection().Execute(sql, list, trans);

                trans.Commit();
                GetConnection().Close();
                return true;
            }
            catch (Exception e)
            {
                throw e;
                return false;
            }
        }

        //satıcının diğer ilanları sayfası için
        // userId = satıcıID , loginId = sayfaya giren kullanıcını id
        public List<Advert> GetUserAdverts(int userId, int loginId, int lang)
        {
            try
            {
                var sql = "  SELECT ( SELECT GetOrderFromDopingInHomepage((Adverts.ID)) AS GetOrderFromDopingInHomepage) AS AdvertOrder,\n    Adverts.*,\n    ( SELECT count(*) AS count\n           FROM AdvertLikes\n          WHERE (AdvertLikes.AdvertID = Adverts.ID)) AS LikesCount,\n    ( SELECT GetLabelDoping((Adverts.ID)) AS GetLabelDoping) AS LabelDoping,\n    ( SELECT GetYellowFrameDoping((Adverts.ID)) AS GetYellowFrameDoping) AS YellowFrameDoping,\n    ( SELECT IsILiked(Adverts.ID, @loginId ) ) as IsILiked,\n    GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n    GetText((ParentCategory.SlugID), (1)) AS ParentCategorySlugTr,\n    GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn,\n    GetText((ParentCategory.SlugID), (2)) AS ParentCategorySlugEn\n   FROM Adverts,\n    AdvertCategories,\n    AdvertCategories ParentCategory\n  WHERE (Adverts.IsActive = true) AND (Adverts.CategoryID = AdvertCategories.ID)\n           AND (AdvertCategories.ParentCategoryID = ParentCategory.ID)\n    AND (( SELECT Users.IsActive  FROM Users WHERE (Users.ID = Adverts.UserID)) = true)\n AND Adverts.UserID = @userId  ORDER BY AdvertOrder, CreatedDate DESC; ";
                var list = GetConnection().Query<Advert>(sql, new { userId, loginId }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                        x.LabelDopingModel = (x.LabelDoping != 0) ? dopingTypes.Single(d => d.ID == x.LabelDoping) : null;
                    });
                }
                //aktif dile göre ilanlaraın kategori slug'larını günceller.
                CheckAdvertsIsDraft(ref list);
                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetHomePageAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true,
                    Params = ""
                });
            }
            return null;
        }

        #region AdvertPhotos

        public bool UpdatePhotoOrder(int id, int order)
        {
            try
            {
                var sql = $"UPDATE AdvertPhotos set OrderNumber=@order where ID=@id";
                GetConnection().Execute(sql, new { order, id });
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public bool DeletePhoto(AdvertPhoto photo)
        {
            try
            {
                return GetConnection().Delete(photo);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool InsertAdvertPhoto(AdvertPhoto advertPhoto)
        {
            try
            {
                GetConnection().Insert(advertPhoto);
                return advertPhoto.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public List<AdvertPhoto> GetPhotos(int advertId)
        {
            try
            {
                return GetConnection().Find<AdvertPhoto>(s => s
                    .Where($"{nameof(AdvertPhoto.AdvertID):C}=@advertId")
                    .WithParameters(new { advertId })
                ).ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public AdvertPhoto GetPhoto(int id)
        {
            try
            {
                return GetConnection().Find<AdvertPhoto>(s => s
                    .Where($"{nameof(AdvertPhoto.ID):C}=@id")
                    .WithParameters(new { id })
                    .Include<Advert>()
                ).SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool BulkInsertAdvertPhoto(List<AdvertPhoto> list)
        {
            try
            {
                var sql = "INSERT INTO AdvertPhotos ( " +
                          " ID,AdvertID,Source,Thumbnail,CreatedDate " +
                          ") " +
                          " values ( " +
                          "@ID, @AdvertID, @Source, @Thumbnail, @CreatedDate ) ";

                GetConnection().Open();
                IDbTransaction trans = GetConnection().BeginTransaction();
                GetConnection().Execute(sql, list, trans);

                trans.Commit();
                GetConnection().Close();
                return true;
                return true;
            }
            catch (Exception e)
            {
                throw e;
                return false;
            }
        }

        #endregion

        #region AdvertDopings

        public bool InsertAdvertDoping(AdvertDoping advertDoping)
        {
            try
            {
                GetConnection().Insert(advertDoping);
                return advertDoping.ID > 0;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "InsertAdvertDoping",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true
                });
                return false;
            }
        }

        public AdvertDoping GetAdvertDoping(int id)
        {
            return GetConnection().Get(new AdvertDoping { ID = id });
        }

        public List<AdvertDoping> GetadvertDopings(int advertId)
        {
            string sql = "SELECT * FROM AdvertDopings AS A INNER JOIN DopingTypes AS B ON A.TypeID = B.ID where A.AdvertID= " + advertId + " and A.IsActive= true and A.EndDate > '" + DateTime.Now + "'";

            var advertDopings = GetConnection().Query<AdvertDoping, DopingType, AdvertDoping>(
                    sql,
                    (advertDoping, dopingType) =>
                    {
                        advertDoping.DopingType = dopingType;
                        return advertDoping;
                    },
                    splitOn: "TypeID")
                .Distinct()
                .ToList();

            return advertDopings;
        }

        public bool UpdateAdvertDoping(AdvertDoping advertDoping)
        {
            return GetConnection().Update(advertDoping);
        }

        public int ActivateAllPendingDopings(int advertId, int paymentId)
        {
            var sql = $"UPDATE AdvertDopings set IsActive=true, IsPendingApproval=false " +
                      $"where IsPendingApproval=true and PaymentID=@paymentId " +
                      $"and AdvertID=@advertId ";

            return GetConnection().Execute(sql, new { advertId, paymentId });
        }

        public int PassiveAllPendingDopings(int advertId, int paymentId)
        {
            var sql = $"UPDATE AdvertDopings set IsPendingApproval=false " +
                      $"where IsPendingApproval=true and PaymentID=@paymentId " +
                      $"and AdvertID=@advertId ";

            return GetConnection().Execute(sql, new { advertId, paymentId });
        }

        public void UpdateDopingPaymentId(int id, int paymentId)
        {
            try
            {
                var sql = $"update AdvertDopings set PaymentID=@paymentId where ID = @id";
                GetConnection().Query(sql, new { paymentId, id });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        #endregion

        #region AdvertLikes

        public AdvertLike GetAdvertLike(int advertId, int userId)
        {
            try
            {
                return GetConnection().Find<AdvertLike>(s => s
                    .Where($"{nameof(AdvertLike.AdvertID):C}=@advertId and {nameof(AdvertLike.UserID):C}=@userId")
                    .WithParameters(new { advertId, userId })
                ).SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool InsertAdvertLike(AdvertLike advertLike)
        {
            try
            {
                GetConnection().Insert(advertLike);
                return advertLike.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool DeleteAdvertLike(AdvertLike advertLike)
        {
            try
            {
                return GetConnection().Delete(advertLike);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        #endregion


        public List<Advert> GetAllActiveAdverts(int lang = 1, int userId = 0)
        {
            try
            {
                var sql = "  SELECT\n    Adverts.*,\n  " +
                          "\n  GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n " +
                          " GetText((ParentCategory.SlugID), (1)) AS ParentCategorySlugTr,\n  " +
                          "GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn,\n  " +
                          "GetText((ParentCategory.SlugID), (2)) AS ParentCategorySlugEn, \n  " +
                          "GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, " +
                          " GetText((AdvertCategories.NameID), (2)) AS CategoryNameEn " +
                          "FROM Adverts, AdvertCategories, AdvertCategories ParentCategory\n\n  " +
                          "WHERE " +
                          "Adverts.IsActive = true\n\n  " +
                          "AND Adverts.CategoryID = AdvertCategories.ID\n\n  " +
                          "AND (AdvertCategories.ParentCategoryID = ParentCategory.ID)\n\n  " +
                          "AND (( SELECT Users.IsActive  FROM Users WHERE (Users.ID = Adverts.UserID)) = true)\n\n " +
                          "  " +
                          " ORDER BY Adverts.ID DESC;";

                var list = GetConnection().Query<Advert>(sql, new { userId }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                    });
                }
                CheckAdvertsIsDraft(ref list);
                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetAllActiveAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true,
                    Params = ""
                });
            }
            return null;
        }
        public List<Advert> GetAdvertsByTake(int lang = 1, int userId = 0, int take = 0)
        {
            try
            {
                var sql = "SELECT\n Adverts.*,\n  " +
                          "\n  GetText((AdvertCategories.SlugID), (1)) AS SubCategorySlugTr,\n " +
                          " GetText((ParentCategory.SlugID), (1)) AS ParentCategorySlugTr,\n  " +
                          "GetText((AdvertCategories.SlugID), (2)) AS SubCategorySlugEn,\n  " +
                          "GetText((ParentCategory.SlugID), (2)) AS ParentCategorySlugEn, \n  " +
                          "GetText((AdvertCategories.NameID), (1)) AS CategoryNameTr, " +
                          " GetText((AdvertCategories.NameID), (2)) AS CategoryNameEn " +
                          "FROM Adverts, AdvertCategories, AdvertCategories ParentCategory\n\n  " +
                          "WHERE " +
                          "Adverts.IsActive = true\n\n  " +
                          "AND Adverts.CategoryID = AdvertCategories.ID\n\n  " +
                          "AND (AdvertCategories.ParentCategoryID = ParentCategory.ID)\n\n  " +
                          "AND (( SELECT Users.IsActive  FROM Users WHERE (Users.ID = Adverts.UserID)) = true)\n\n " +
                          "  " +
                          $" ORDER BY Adverts.ID DESC LIMIT {take}";

                var list = GetConnection().Query<Advert>(sql, new { userId }).ToList();
                using (var publicService = new PublicService())
                {
                    var dopingTypes = publicService.GetDopingTypes();
                    list.ForEach(x =>
                    {
                        x.CategorySlug = (lang == 1) ? x.ParentCategorySlugTr : x.ParentCategorySlugEn;
                        x.SubCategorySlug = (lang == 1) ? x.SubCategorySlugTr : x.SubCategorySlugEn;
                    });
                }
                CheckAdvertsIsDraft(ref list);
                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "AdvertService.GetAllActiveAdverts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true,
                    Params = ""
                });
            }
            return null;
        }



        public void SetDraft(int id, bool status)
        {
            var advert = GetConnection().Find<Advert>().FirstOrDefault(p => p.ID == id);
            if (advert == null)
            {
                return;
            }
            advert.IsDraft = status;
            GetConnection().Update(advert);
        }

        private void CheckAdvertsIsDraft(ref List<Advert> adverts)
        {
            if (adverts.Any())
            {
                adverts = adverts.Where(p => p.IsDraft == false).ToList();
            }
        }


    }
}