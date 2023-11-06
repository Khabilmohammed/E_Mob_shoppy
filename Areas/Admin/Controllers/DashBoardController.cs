using E_mob_shoppy.DataAccess.Repository.IRepository;
using E_mob_shoppy.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace E_mob_shoppy.Areas.Admin.Controllers
{
    public class DashBoardController: Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM orderVm { get; set; }
        public DashBoardController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
