using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.Areas.AdminPanel.Models
{
    public class SettingsIndexViewModel
    {
        public List<SystemSetting> SystemSettins { get; set; }
        public List<DopingType> Dopings { get; set; }
    }
}