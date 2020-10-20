using Microsoft.AspNetCore.Http;

namespace Audiophile.Web.Areas.AdminPanel.Models
{
    public class AdvertCategoryPostModel
    {
        public int ID { get; set; }
        public int ParentCategoryID { get; set; }
        public string NameTr { get; set; }
        public string DescriptionTr { get; set; }
        public string SeoTitleTr { get; set; }
        public string SeoDescriptionTr { get; set; }
        public string SeoKeywordsTr { get; set; }
        public string SlugTr { get; set; }
        public string NameEn { get; set; }
        public string DescriptionEn { get; set; }
        public string SeoTitleEn { get; set; }
        public string SeoDescriptionEn { get; set; }
        public string SeoKeywordsEn { get; set; }
        public string SlugEn { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public IFormFile File { get; set; }
    }
}