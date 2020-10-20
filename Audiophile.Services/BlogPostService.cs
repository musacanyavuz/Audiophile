using System;
using System.Collections.Generic;
using System.Linq;
using Audiophile.Models;
using Dapper;
using Dapper.FastCrud;

namespace Audiophile.Services
{
    public class BlogPostService : BaseService
    {
        public bool Delete(int id)
        {
            try
            {
                return GetConnection().Delete(new BlogPost { ID = id });
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public BlogPost Get(int id)
        {
            try
            {
                return GetConnection().Find<BlogPost>(s => s
                    .Where($"{nameof(BlogPost.ID):C}=@id")
                    .WithParameters(new { id })
                    .Include<User>()
                    .Include<BlogPostImage>(j => j.LeftOuterJoin())
                ).SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public void UpdateViewCount(int id)
        {
            try
            {
                GetConnection().Execute($"update \"BlogPosts\" set \"ViewCount\" = \"ViewCount\" + 1 where \"ID\" = @id",
                    new { id });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public List<BlogPost> Search(string query)
        {
            try
            {
                return GetConnection().Find<BlogPost>(s => s
                    .Where($"{nameof(BlogPost.IsActive):C}=true and ( {nameof(BlogPost.Title):C} ilike @query and {nameof(BlogPost.Content):C} ilike @query ) ")
                    .WithParameters(new { query = "%" + query + "%" })
                    .OrderBy($"{nameof(BlogPost.ID):C} DESC")
                    .Top(20)
                ).ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public List<BlogPost> GetHomePagePosts()
        {
            try
            {
                return GetConnection().Find<BlogPost>(s => s
                    .Where($"{nameof(BlogPost.DopingEndDate):C}>now() and {nameof(BlogPost.IsActive):C}=true ")
                    .OrderBy($"{nameof(BlogPost.DopingEndDate):C} DESC")
                    .Top(50)
                ).ToList();
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "GetHomePagePosts",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.InnerException?.Message,
                    IsError = true
                });
            }
            return null;
        }

        public List<BlogPost> GetUserPosts(int userId)
        {
            return GetConnection().Find<BlogPost>(s => s
                .Where($"{nameof(BlogPost.UserID):C}=@userId")
                .WithParameters(new { userId })
                .OrderBy($"{nameof(BlogPost.ID):C} DESC")
            ).ToList();
        }

        public List<BlogPost> GetPosts()
        {
            return GetConnection().Find<BlogPost>(s => s
                .Include<User>()
            ).ToList();
        }

        public List<BlogPost> GetPosts(int categoryId, bool dopingFilter)
        {
            if (dopingFilter)
            {
                return GetConnection().Find<BlogPost>(s => s
                    .Where($"{nameof(BlogPost.IsActive):C}=true and {nameof(BlogPost.DopingEndDate):C}> now() and {nameof(BlogPost.CategoryID):C}=@categoryId")
                    .WithParameters(new { categoryId })
                    .OrderBy($"{nameof(BlogPost.ID):C} DESC")
                    .Include<User>()
                    .Top(20)
                ).ToList();
            }
            else
            {
                return GetConnection().Find<BlogPost>(s => s
                    .Where($"{nameof(BlogPost.IsActive):C}=true and {nameof(BlogPost.CategoryID):C}=@categoryId")
                    .WithParameters(new { categoryId })
                    .OrderBy($"{nameof(BlogPost.ID):C} DESC")
                    .Include<User>()
                    .Top(20)
                ).ToList();
            }
        }

        public List<BlogPost> GetPosts(int categoryId, bool dopingFilter, int minId)
        {
            if (dopingFilter)
            {
                return GetConnection().Find<BlogPost>(s => s
                    .Where($"{nameof(BlogPost.IsActive):C}=true and {nameof(BlogPost.DopingEndDate):C}> now() and {nameof(BlogPost.CategoryID):C}=@categoryId and {nameof(BlogPost.ID):C}<@minId")
                    .WithParameters(new { categoryId, minId })
                    .Include<User>()
                    .OrderBy($"{nameof(BlogPost.ID):C} DESC")
                    .Top(20)
                ).ToList();
            }
            else
            {
                return GetConnection().Find<BlogPost>(s => s
                    .Where($"{nameof(BlogPost.IsActive):C}=true and {nameof(BlogPost.CategoryID):C}=@categoryId and {nameof(BlogPost.ID):C}<@minId")
                    .WithParameters(new { categoryId, minId })
                    .OrderBy($"{nameof(BlogPost.ID):C} DESC")
                    .Include<User>()
                    .Top(20)
                ).ToList();
            }
        }

        public int GetPostCount(int categoryId, bool dopingFilter)
        { //doping filter true; sadece dopingi aktif postların sayısını dönmek için.
            try
            {
                if (dopingFilter)
                {
                    return GetConnection().Query<int>("select count(*) from \"BlogPosts\" where " +
                                                      " \"DopingEndDate\" > now() " +
                                                      "and \"IsActive\"=true and \"CategoryID\"=@categoryId", new { categoryId })
                        .SingleOrDefault();
                }
                return GetConnection().Query<int>("select count(*) from \"BlogPosts\" where " +
                                           "\"IsActive\"=true and \"CategoryID\"=@categoryId", new { categoryId })
                    .SingleOrDefault();
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public bool Insert(BlogPost post)
        {
            try
            {
                GetConnection().Insert(post);
                return post.ID > 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(post.ID + " " + post.Title + " : " + e.Message);
                return false;
            }
        }

        public bool Update(BlogPost post)
        {
            try
            {
                return GetConnection().Update(post);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public int UpdateImage(int imageId, int postId)
        {
            try
            {
                var image = GetBlogPostImage(imageId);
                if (image == null) return 0;
                image.PostID = postId;
                GetConnection().Update(image);
                return imageId;
            }
            catch (Exception e)
            {
                return 0;
            }
        }
        public int InsertImage(BlogPostImage image)
        {
            try
            {
                GetConnection().Insert(image);
                return image.ID;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public BlogPostImage GetBlogPostImage(int id)
        {
            try
            {
                return GetConnection().Find<BlogPostImage>(s => s
                    .Where($"{nameof(BlogPostImage.ID):C}=@id")
                    .WithParameters(new { id })
                    .Include<BlogPost>()
                ).SingleOrDefault();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool DeleteImage(BlogPostImage image)
        {
            try
            {
                return GetConnection().Delete(image);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public int DeletePostImages(int postId)
        {
            try
            {
                var counts = GetConnection().Execute($"DELETE FROM \"BlogPostImages\"  WHERE \"PostID\"=@postId",
                    new { postId });
                return counts;
            }
            catch (Exception e)
            {
                return 0;
            }
        }
    }
}