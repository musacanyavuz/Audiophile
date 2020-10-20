using System;
using System.Collections.Generic;
using System.Linq;
using Audiophile.Common;
using Audiophile.Models;
using Dapper;
using Dapper.FastCrud;

namespace Audiophile.Services
{
    public class DopingService : BaseService
    {
        public List<AdvertDoping> GetDopingHistory(int userId)
        {
            try
            {
                return GetConnection().Find<AdvertDoping>(s => s
                    .Include<Advert>(j => j.Where($"{nameof(Advert.UserID):C}=@userId"))
                    .Include<DopingType>()
                    .WithParameters(new { userId })
                ).ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public int GetActiveDopingCount(int userId)
        {
            return GetConnection().Find<AdvertDoping>(s =>
                s.Where($"{nameof(AdvertDoping.IsActive):C}=true")
                    .Include<Advert>(a => a.Where($"{nameof(Advert.UserID):C}=@userId"))).Count();
        }

        public DopingType Get(int id)
        {
            return GetConnection().Get(new DopingType() { ID = id });
        }
        public AdvertDoping GetByPaymentId(int id)
        {
            return GetConnection().Find<AdvertDoping>().FirstOrDefault(p => p.PaymentID == id && p.TypeID == (int)Enums.Dopings.IlanTarihiGuncelle);
        }
        public bool Update(DopingType doping)
        {
            return GetConnection().Update(doping);
        }
    }
}