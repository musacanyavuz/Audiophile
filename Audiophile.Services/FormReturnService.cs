using System;
using Audiophile.Models;
using Dapper.FastCrud;

namespace Audiophile.Services
{
    public class FormReturnService : BaseService
    {
        public bool Insert(FormReturn formReturn)
        {
            try
            {
                GetConnection().Insert(formReturn);
                return formReturn.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}