using System;
using System.Collections.Generic;
using System.Linq;
using Audiophile.Common;
using Audiophile.Models;
using Dapper;
using Dapper.FastCrud;

namespace Audiophile.Services
{
    public class TextService : BaseService
    {
        public bool Insert(Content content)
        {
            try
            {
                GetConnection().Insert(content);
                return content.ID > 0;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        
        public bool Insert(int key, string contentTr, string contentEn)
        {
            try
            {
                GetConnection().Insert(new Content{ Key = key, TextContent = contentTr, LanguageID = 1});
                GetConnection().Insert(new Content{ Key = key, TextContent = contentEn, LanguageID = 2});
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public bool Update(Content content)
        {
            try
            {
                return GetConnection().Update(content);
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        
        public bool Update(int key, string content, int lang)
        {
            try
            {
                var sql = 
                    $"UPDATE \"Contents\" set \"TextContent\"=@content where \"Key\"=@key and \"LanguageID\"=@lang";
                var count = GetConnection().Execute(sql, new {content, key, lang});
                return count > 0;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
        

        public bool Insert(ContentArray contentArray)
        {
            try
            {
                GetConnection().Insert(contentArray);
                return contentArray.ID > 0;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public int GetNextKey()
        {
            try
            {
                var sql = $"select max(\"Key\")+1 as \"Max\" from \"Contents\"";
                var next = GetConnection().Query<int>(sql).SingleOrDefault();
                if (next > 0)
                    return next;
            }
            catch (System.Exception)
            {}
            return new Random().Next(1000,9999);
        }

        public string GetText(Enums.Texts key, int lang)
        { 
            return GetText((int) key, lang);
        }
        public string GetTextWithoutHtml(Enums.Texts key, int lang)
        {
            return GetText((int)key, lang);
        }

        public string GetText(int key, int language)
        {   
            var text = GetConnection().Find<Content>(s => s
                .Where($"{nameof(Content.Key):C}=@key and {nameof(Content.LanguageID):C}=@language")
                .WithParameters(new {key, language})
            ).SingleOrDefault()?.TextContent;
            return text;
        }

        public List<Content> GetContents()
        {
            var list = GetConnection().Find<Content>().ToList();
            return list;
        }

        public Content GetContent(int id)
        {
            var c = GetConnection().Find<Content>(s => s.Where($"{nameof(Content.ID):C}=@id").WithParameters(new {id}))
                .SingleOrDefault();
            return c;
        }
        public Content GetContent(Enums.Texts key, int language)
        {
            return GetContent((int)key, language);
        }
        public Content GetContent(int key, int language)
        {
            var text = GetConnection().Find<Content>(s => s
                .Where($"{nameof(Content.Key):C}=@key and {nameof(Content.LanguageID):C}=@language")
                .WithParameters(new { key, language })
            ).FirstOrDefault();
            return text;
        }
        public List<ContentArray> GetContentArray(int typeId, int lang = 1)
        {
            try
            {
                var sql = "select * from ( "+
                    "select *, (select \"GetText\"(\"TitleID\", @lang)) as \"Title\", (select \"GetText\"(\"ContentID\", @lang)) as \"Content\" " +
                    "from \"ContentArrays\" where \"TypeID\" = @typeId "+
                    ") as tbl where \"Content\" is not null";
                var list = GetConnection().Query<ContentArray>(sql, new {typeId, lang}).ToList();
                return list;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "TextService.GetContentArray", CreatedDate = DateTime.Now, Message = e.Message, Detail = e.InnerException?.Message,
                    IsError = true, Params = typeId.ToString() + " "+lang.ToString() 
                });
                return null;
            }
        }


        
    }
}