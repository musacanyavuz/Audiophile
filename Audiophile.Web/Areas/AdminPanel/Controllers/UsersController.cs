using System.Collections.Generic;
using Audiophile.Common;
using Audiophile.Models;
using Audiophile.Services;
using Audiophile.Web.Areas.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace Audiophile.Web.Areas.AdminPanel.Controllers
{
    public class UsersController : AdminBaseController
    {
        public IActionResult Index()
        {
            using (var service = new UserService())
            {
                //var users = service.GetAll();
                return View(null);
            }
        }

        public JsonResult GetList(Search search, int start, int length)
        {
            using (var service = new UserService())
            {
                var users = service.GetList(search?.Value, length, start);
                var count = service.GetUsersCount();
                var filteredCount = 0;
                if (!string.IsNullOrEmpty(search?.Value))
                    filteredCount = service.GetUsersCount(search?.Value);
                
                return Json(new
                {
                    recordsTotal = count,
                    recordsFiltered = filteredCount == 0 ? count : filteredCount,
                    data = users
                });
            }
        }
        
        [HttpPost]
        public JsonResult UpdateStatus(int id, bool status)
        {
            using (var service = new UserService())
            {
                var user = service.Get(id);
                if (user == null)
                {
                    return Json(new {isSuccess = false, message = "Kullanıcı bulunamadı"});
                }

                user.IsActive = status;
                var update = service.Update(user);
                if (!update)
                    return Json(new {isSuccess = false, message = "Güncelleme başarısız"});
                
                return Json(new {isSuccess = true, message = "Kullanıcı aktiflik durumu güncellendi."});
            }
        }

        public IActionResult Details(int id)
        {
            
            using (var service = new UserService())
            {
                var user = service.Get(id);
               
                
                if (user == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Kullanıcı bulunamadı.");
                    return RedirectToAction("Index");
                }
                var userPayment = service.GetSecurePaymentDetail(id);
                var model=new UserDetailViewModel()
                {
                    User = user,
                    UserSecurePaymentDetail = userPayment,
                    UserAddress = service.GetUserAddresses(id)
                };
                return View(model);
            }
        }
        public IActionResult ExcelExport(){
            string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            using(var package=new ExcelPackage())
            using(var service=new UserService()){
                var users=service.GetAll();
                var worksheet = package.Workbook.Worksheets.Add("UserExpert");
                 var export=new List<User>();
                foreach(var user in users){
                    export.Add(user);
                }
                worksheet.Cells["A1"].LoadFromCollection(export, true, TableStyles.Light8);
                for (var col = 1; col < 9+ 1; col++)
                {
                    worksheet.Column(col).AutoFit();
                }           
                return File(package.GetAsByteArray(), XlsxContentType, 
                    "personelexpert.xlsx");
            }
        }

        public IActionResult Edit(User user)
        {
            using (var service = new UserService())
            {
                if (user.ID == 0)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Kullanıcı bulunamadı.");
                    return RedirectToAction("Index");
                }
                var _user = service.Get(user.ID);
                _user.UserName = user.UserName;
                _user.Email = user.Email;
                _user.Name = user.Name;
                _user.MobilePhone = user.MobilePhone;
                _user.TC = user.TC;
                _user.WebSite = user.WebSite;
                _user.About = user.About;
                _user.Role = user.Role;
                _user.WorkPhone = user.WorkPhone;

                var update = service.Update(_user);
                if (!update)
                    AdminNotification = new UiMessage(NotyType.error, "Güncelleme başarısız.");
                else
                    AdminNotification = new UiMessage(NotyType.success, "Kullanıcı bilgileri güncellendi.");

                return RedirectToAction("Details", new {id = user.ID});
            }
        }

        public IActionResult UserAddressDetail(int id)
        {
             
            using (var servicePublic = new PublicService())
            using (var service=new UserService())
            {
                var model= new UserAddressDetailViewModel()
                {
                    UserAddress = service.GetUserAddress(0, id),
                    Countries = servicePublic.GetCountries(),
                    Cities = servicePublic.GetCities(),
                    Districts = servicePublic.GetDistricts()
                }; 
               return View(model);

            }
        }
        [HttpPost]
        public IActionResult UserAddressDetail(UserAddress userAddress)
        {
             
            using (var servicePublic = new PublicService())
            using (var service=new UserService())
            {
                var _userPayment = service.GetUserAddress(0, userAddress.ID);
                _userPayment.CityID = userAddress.CityID;
                _userPayment.DistrictID = userAddress.DistrictID;
                _userPayment.CountryID = userAddress.CountryID;
                _userPayment.Title = userAddress.Title;
                _userPayment.Address = userAddress.Address;

                var update = service.UpdateAddress(_userPayment);
                if (update)
                {
                    AdminNotification = new UiMessage(NotyType.success, "Güncelleme işlemi tamamlandı.");
                    return RedirectToAction("Details", new {_userPayment.UserID});

                }
                AdminNotification = new UiMessage(NotyType.error, "Güncelleme işlemi başarısız.");

                return RedirectToAction("UserAddressDetail",new{userAddress.ID});


            }
        }
        
        
        
    }
}