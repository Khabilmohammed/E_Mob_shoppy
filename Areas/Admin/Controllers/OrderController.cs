using E_mob_shoppy.DataAccess.Repository.IRepository;
using E_mob_shoppy.Models;
using E_mob_shoppy.Models.ViewModel;
using E_mob_shoppy.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using System.Diagnostics;
using System.Security.Claims;

namespace E_mob_shoppy.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM orderVm { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Detail(int orderId)
        {
            orderVm = new()
            {
                orderHeader = _unitOfWork.OrderHeader.Get(u => u.OrderHeaderId == orderId, includeProperties: "ApplicationUser"),
                orderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };

            return View(orderVm);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.OrderHeaderId == orderVm.orderHeader.OrderHeaderId);
            orderHeaderFromDb.Name = orderVm.orderHeader.Name;
            orderHeaderFromDb.PhoneNumber = orderVm.orderHeader.PhoneNumber;
            orderHeaderFromDb.streetAddress = orderVm.orderHeader.streetAddress;
            orderHeaderFromDb.City = orderVm.orderHeader.City;
            orderHeaderFromDb.state = orderVm.orderHeader.state;
            orderHeaderFromDb.postalCode = orderVm.orderHeader.postalCode;
            if (!string.IsNullOrEmpty(orderVm.orderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = orderVm.orderHeader.Carrier;
            }
            if (string.IsNullOrEmpty(orderHeaderFromDb.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = orderVm.orderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Upadte(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["success"] = "Order details Updated Successfully";

            return RedirectToAction(nameof(Detail), new { orderId = orderHeaderFromDb.OrderHeaderId });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult StartProccessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderVm.orderHeader.OrderHeaderId, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["success"] = "Order details Updated Successfully";
            return RedirectToAction(nameof(Detail), new { orderId = orderVm.orderHeader.OrderHeaderId });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.OrderHeaderId == orderVm.orderHeader.OrderHeaderId);
            orderHeader.TrackingNumber = orderVm.orderHeader.TrackingNumber;
            orderHeader.Carrier = orderVm.orderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.shippingDate = DateTime.Now;

            _unitOfWork.OrderHeader.Upadte(orderHeader);
            _unitOfWork.Save();

            TempData["success"] = "Order shippe Successfully";
            return RedirectToAction(nameof(Detail), new { orderId = orderVm.orderHeader.OrderHeaderId });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.OrderHeaderId == orderVm.orderHeader.OrderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };
            var service= new RefundService();
            Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.OrderHeaderId, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.OrderHeaderId, SD.StatusCancelled, SD.StatusCancelled);

            }

            _unitOfWork.Save();

            TempData["success"] = "Order cancelled Successfully";
            return RedirectToAction(nameof(Detail), new { orderId = orderVm.orderHeader.OrderHeaderId });
        }




        #region API CALLS 
        [HttpGet] 
		public IActionResult GetAll(string status)
		{
            IEnumerable<OrderHeader> objOrderHeaders; _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

            if (User.IsInRole(SD.Role_Admin))
            {
                objOrderHeaders =_unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentiy=(ClaimsIdentity)User.Identity;
                var userId = claimsIdentiy.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders=_unitOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId==userId,includeProperties:"ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayed);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default: break;

            }




            return Json(new {data=objOrderHeaders});

        }

		


		#endregion
	}
}
