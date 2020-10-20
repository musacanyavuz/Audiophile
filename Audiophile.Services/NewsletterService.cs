using Audiophile.Models;
using Dapper.FastCrud;

namespace Audiophile.Services
{
    public class NewsletterService : BaseService
    {
        public bool Insert(NewsletterSubscriber newsletterSubscriber)
        {
            try
            {
                GetConnection().Insert(newsletterSubscriber);
                return newsletterSubscriber.ID > 0;
            }
            catch (System.Exception)
            {
                return false;                
            }
        }
    }
}