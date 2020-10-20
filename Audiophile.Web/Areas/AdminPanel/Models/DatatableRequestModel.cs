namespace Audiophile.Web.Areas.AdminPanel.Models
{
    public class DatatableRequestModel
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public Search Search { get; set; }
    }

    public class Search
    {
        public string Value { get; set; }
        public bool Regex { get; set; }
    }
}