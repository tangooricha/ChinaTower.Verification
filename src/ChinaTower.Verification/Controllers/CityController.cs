﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Authorization;
using Microsoft.Data.Entity;
using Newtonsoft.Json;
using ChinaTower.Verification.Models;
using ChinaTower.Verification.Models.Infrastructures;

namespace ChinaTower.Verification.Controllers
{
    [Authorize]
    public class CityController : BaseController
    {
        [HttpGet]
        public IActionResult Index(string city)
        {
            IEnumerable<City> ret = DB.Cities;
            if (!string.IsNullOrEmpty(city))
                ret = ret.Where(x => x.Id.Contains(city) || city.Contains(x.Id));
            return PagedView(ret);
        }

        [HttpPost]
        [AnyRoles("Root")]
        [ValidateAntiForgeryToken]
        public IActionResult Insert(string city, [FromHeader] string Referer)
        {
            if (DB.Cities.SingleOrDefault(x => x.Id == city) != null)
                return Prompt(x =>
                {
                    x.Title = "添加失败";
                    x.Details = $"系统中已经存在了名为{city}的城市，请勿重复添加！";
                    x.StatusCode = 400;
                });
            DB.Cities.Add(new City { Id = city });
            DB.SaveChanges();
            return Redirect(Referer);
        }


        [HttpPost]
        [AnyRoles("Root")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string city)
        {
            var c = DB.Cities.Single(x => x.Id == city);
            DB.Cities.Remove(c);
            DB.SaveChanges();
            DB.Database.ExecuteSqlCommand("UPDATE \"Form\" SET \"Status\"={0}, \"VerificationJson\" = {1} WHERE \"Type\" = {2}", VerificationStatus.Pending, "[]", FormType.站址);
            return Content("ok");
        }

        [HttpGet]
        [AnyRoles("Root")]
        public IActionResult Edge(string id)
        {
            var city = DB.Cities
                .Where(x => x.Id == id)
                .SingleOrDefault();
            return View(city);
        }

        [HttpPost]
        [AnyRoles("Root")]
        [ValidateAntiForgeryToken]
        public IActionResult Edge(string id, string edge)
        {
            var city = DB.Cities.Single(x => x.Id == id);
            city.EdgeJson = edge;
            DB.Database.ExecuteSqlCommand("UPDATE \"Form\" SET \"Status\"={0}, \"VerificationJson\" = {1} WHERE \"Type\" = {2}", VerificationStatus.Pending, "[]", FormType.站址);
            DB.SaveChanges();
            return Prompt(x =>
            {
                x.Title = "修改成功";
                x.Details = "新的城市边界已经保存！";
                x.RedirectText = "返回城市列表";
                x.RedirectUrl = Url.Action("Index", "City");
            });
        }
        
        [HttpGet]
        public async Task<IActionResult> Gps(string city, string address)
        {
            var client = new HttpClient();
            var result = await client.GetAsync($"http://api.map.baidu.com/geocoder/v2/?city={city}&address={address}&output=json&ak=lrMh687iqexo2F4V9bMYLmbX");
            var jsonStr = await result.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<dynamic>(jsonStr);
            var ret = new { Lon = obj.result.location.lng, Lat = obj.result.location.lat };
            return Json(ret);
        }

        [HttpGet]
        public IActionResult GetDistricts(string id)
        {
            var city = DB.Cities.Single(x => x.Id == id);
            return Json(city.Districts);
        }

        [HttpPost]
        [AnyRoles("Root")]
        public IActionResult District(string id, string district)
        {
            var city = DB.Cities.Single(x => x.Id == id);
            city.DistrictJson = district;
            DB.SaveChanges();
            DB.Database.ExecuteSqlCommand("UPDATE \"Form\" SET \"Status\"={0}, \"VerificationJson\" = {1} WHERE \"Type\" = {2}", VerificationStatus.Pending, "[]", FormType.站址);
            return Prompt(x =>
            {
                x.Title = "保存成功";
                x.Details = "下属区县信息保存成功！";
                x.HideBack = true;
                x.RedirectText = "返回城市列表";
                x.RedirectUrl = Url.Action("Index", "City");
            });
        }
    }
}