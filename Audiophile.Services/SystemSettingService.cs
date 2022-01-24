using System.Collections.Generic;
using System.Linq;
using Audiophile.Common;
using Audiophile.Models;
using Dapper.FastCrud;

namespace Audiophile.Services
{
    public class SystemSettingService : BaseService
    {
        public List<SystemSetting> GetSystemSettings() => GetConnection().Find<SystemSetting>().ToList();
        
        public bool Update(SystemSetting setting)
        {
            return GetConnection().Update(setting);
        }

        public SystemSetting GetSetting(Enums.SystemSettingName name)
        {
            var setting = GetSystemSettings().FirstOrDefault(p => p.Name == name);
            return setting;
        }
      
    }
}