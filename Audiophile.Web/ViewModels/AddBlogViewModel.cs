using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class AddBlogViewModel
    {
        public BlogPost BlogPost { get; set; }
        public List<DopingType> Dopings { get; set; }
    }
}