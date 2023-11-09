using E_mob_shoppy.DataAccess.Repository.IRepository;
using E_mob_shoppy.Models;
using E_mob_shoppy.Models.ViewModel;
using E_mob_shoppy.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace E_mob_shoppy.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class DashBoardController: Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public DashboardVM dashboardVm { get; set; }
        public DashBoardController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        public IActionResult Index(int page = 1, int itemsPerPage = 5)
        {
            IEnumerable<OrderHeader> orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll();
            IEnumerable<Category> catogoryList = _unitOfWork.Category.GetAll();
            int shippedCount = orderHeaders.Count(u => u.OrderStatus == "Shipped");
            int approvedCount = orderHeaders.Count(u => u.OrderStatus == "Approved");
            int cancelledCount = orderHeaders.Count(u => u.OrderStatus == "Cancelled");        
            int productCount=productList.Count();   
            int orderCount=orderHeaders.Count();
            int categoryCount=catogoryList.Count();
            double totalSales = 0;
            DateTime today = DateTime.Now;
            DateTime lastWeek = today.AddDays(-7);
            IEnumerable<OrderHeader> ordersLastWeek = orderHeaders.Where(order => order.OrderDate >= lastWeek && order.OrderDate <= today).OrderByDescending(order => order.OrderDate);
           
            int numberOfOrdersLastWeek = ordersLastWeek.Count();
            double totalRevenueLastWeek = ordersLastWeek.Sum(order => order.OrderTotal); 

            foreach (var order in orderHeaders)
            {
                totalSales += order.OrderTotal;
            }

            var viewModel = new DashboardVM
            {
                categors= catogoryList,
                product =productList,
                orderHeader= ordersLastWeek,
                OrderCount = orderCount,
                ProductCount = productCount,
                CategoryCount = categoryCount,
                TotalSales = totalSales,
                ApprovedCount = approvedCount,
                CancelledCount = cancelledCount,
                ShippedCount = shippedCount,
                TotalRevenueLastWeek = totalRevenueLastWeek,
                NumberOfOrdersLastWeek= numberOfOrdersLastWeek,
                CurrentPage = page,
                ItemsPerPage = itemsPerPage
            };
            viewModel.orderHeader = ordersLastWeek.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);
            return View(viewModel);
        }
    }
}
