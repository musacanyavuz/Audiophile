using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("AdvertCategories")]
    public class AdvertCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        
        public int NameID { get; set; }
        //[ForeignKey("Content")]
        public int DescriptionID { get; set; }
        public int Order { get; set; }
        public int SeoTitleID { get; set; }
        public int SeoKeywordsID { get; set; }
        public int SeoDescriptionID { get; set; }
        [ForeignKey("ParentCategory")]
        public int ParentCategoryID { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string ImageSource { get; set; }
        public string IconSource { get; set; }
        public bool IsActive { get; set; }
        public int SlugID { get; set; }

        [NotMapped] public string Name { get; set; }
        [NotMapped] public string Description { get; set; }
        [NotMapped] public string SeoTitle { get; set; }
        [NotMapped] public string SeoKeywords { get; set; }
        [NotMapped] public string SeoDescription { get; set; }
        [NotMapped] public string Slug { get; set; }
        public IEnumerable<AdvertCategory> ChildCategories { get; set; }
        public AdvertCategory ParentCategory { get; set; }
    }
}