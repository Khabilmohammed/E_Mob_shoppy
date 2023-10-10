using E_mob_shoppy.DataAccess.Data;
using E_mob_shoppy.DataAccess.Repository.IRepository;
using E_mob_shoppy.Models;
using E_mob_shoppy.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_mob_shoppy.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController: Controller
    {
        private readonly ApplicationDbContext _db;
        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }

        public ActionResult Index() 
        {
            return View();
        
        }

        #region API CALLS
        [HttpGet]
        public ActionResult Get()
        {
            List<ApplicationUser> userlist= _db.ApplicationUsers.ToList();
            return Json(new {data= userlist });
                
        }



        #endregion

    }
}
