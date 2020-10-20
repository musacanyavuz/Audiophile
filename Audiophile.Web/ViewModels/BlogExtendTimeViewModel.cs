using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class BlogExtendTimeViewModel
    {
        public List<DopingType> Dopings { get; set; }
        public BlogPost Post { get; set; }
    }
}