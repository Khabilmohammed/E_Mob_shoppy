using E_mob_shoppy.DataAccess.Repository.IRepository;
using E_mob_shoppy.Models;
using E_mob_shoppy.Models.ViewModel;
using E_mob_shoppy.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;
using Stripe.Checkout;
using System.Security.Claims;

namespace E_mob_shoppy.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]

    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM shoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            shoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader=new()

            };

            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuatity(cart);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.count);
            }

           

            return View(shoppingCartVM);
        }

        public ActionResult Summary()
        {


            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            shoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.  ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()

            };

            shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            shoppingCartVM.OrderHeader.Name=shoppingCartVM.OrderHeader.ApplicationUser.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.PhoneNumber;
            shoppingCartVM.OrderHeader.streetAddress = shoppingCartVM.OrderHeader.streetAddress;
            shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.City;
            shoppingCartVM.OrderHeader.postalCode = shoppingCartVM.OrderHeader.postalCode;
            shoppingCartVM.OrderHeader.state = shoppingCartVM.OrderHeader.state;
            


            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuatity(cart);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.count);
            }

            return View(shoppingCartVM);
        }



        [HttpPost]
        [ActionName("Summary")]
		public ActionResult SummaryPost()
		{


			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            shoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product");
				
            shoppingCartVM.OrderHeader.OrderDate=System.DateTime.Now;
            shoppingCartVM.OrderHeader.ApplicationUserId = userId;
		
			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

			foreach (var cart in shoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuatity(cart);
				shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.count);
			}

           

            shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            _unitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
            _unitOfWork.Save();



            foreach(var cart in shoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = shoppingCartVM.OrderHeader.OrderHeaderId,
                    Price= cart.Price,  
                    Count=cart.count,
                };

				_unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
			}


			var domain = "https://localhost:44346/";
			var options = new SessionCreateOptions
			{
				SuccessUrl = domain + $"customer/cart/OrderConformation?id={shoppingCartVM.OrderHeader.OrderHeaderId}",
				CancelUrl = domain + $"customer/cart/Index",
				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
			};

			foreach (var item in shoppingCartVM.ShoppingCartList)
			{
				var sessionLineItem = new SessionLineItemOptions
				{
					PriceData = new SessionLineItemPriceDataOptions
					{
						UnitAmount = (long)(item.Price * 100),
						Currency = "usd",
						ProductData = new SessionLineItemPriceDataProductDataOptions
						{
							Name = item.Product.ProductName
						}

					},
					Quantity = item.count
				};
				options.LineItems.Add(sessionLineItem);
			}
			var service = new SessionService();
			Session session = service.Create(options);
			_unitOfWork.OrderHeader.UpdateStripePaymentId(shoppingCartVM.OrderHeader.OrderHeaderId, session.Id, session.PaymentIntentId);
			_unitOfWork.Save();
			Response.Headers.Add("Location", session.Url);
			return new StatusCodeResult(303);

			return RedirectToAction(nameof(OrderConformation),new {id=shoppingCartVM.OrderHeader.OrderHeaderId});
		}


        public IActionResult OrderConformation(int id)
        {


            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u=>u.OrderHeaderId == id,includeProperties:"ApplicationUser");

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayed)
            {
                //this order of cs
                var service=new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
					_unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id,SD.StatusApproved,SD.PaymentStatusApproved);
                    _unitOfWork.Save();
				}
			}

            List<ShoppingCart> shoppingCarts= _unitOfWork.ShoppingCart.GetAll(u=>u.ApplicationUserId==orderHeader.ApplicationUserId).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
            return View(id);
        }

		public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.CartId == cartId);
            cartFromDb.count += 1;
            _unitOfWork.ShoppingCart.Upadte(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.CartId == cartId);
            if (cartFromDb.count <= 1)
            {
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.count -= 1;
                _unitOfWork.ShoppingCart.Upadte(cartFromDb);
            }


            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }


        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.CartId == cartId);

            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuatity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.count <= 50)
            {
                return shoppingCart.Product.ListPrice;
            }
            else
            {
                if (shoppingCart.count <= 100)
                {
                    return shoppingCart.Product.Listprice50;
                }
                else
                {
                    return shoppingCart.Product.Listprice100;
                }
            }
        }
    }
}
