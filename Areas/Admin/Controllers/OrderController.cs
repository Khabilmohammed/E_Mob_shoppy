

using E_mob_shoppy.DataAccess.Repository.IRepository;
using E_mob_shoppy.Models;
using E_mob_shoppy.Models.ViewModel;
using E_mob_shoppy.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PdfSharpCore;
using PdfSharpCore.Pdf;
using TheArtOfDev.HtmlRenderer.PdfSharp;
using Stripe;
using System.Security.Claims;


namespace E_mob_shoppy.Areas.Admin.Controllers
{
    [Area("Admin")]
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
                var service = new RefundService();
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

       
        public IActionResult Invoice(int orderId)
        {
            orderVm = new()
            {
                orderHeader = _unitOfWork.OrderHeader.Get(u => u.OrderHeaderId == orderId, includeProperties: "ApplicationUser"),
                orderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };

            return View(orderVm);
        }



        /* [HttpGet]
         public IActionResult GenerateInvoice(string format, int orderId)
         {
             var orderHeader = _unitOfWork.OrderHeader.Get(u => u.OrderHeaderId == orderId, includeProperties: "ApplicationUser");
             string fileName = $"invoice_{orderHeader.OrderHeaderId}";

             switch (format.ToLower())
             {
                 case "pdf":
                     // Convert HTML to PDF
                     var pdfDoc = new HtmlToPdfDocument
                     {
                         GlobalSettings = {
                     ColorMode = ColorMode.Color,
                     Orientation = Orientation.Portrait,
                     PaperSize = PaperKind.A4,
                 },
                         Objects = {
                     new ObjectSettings {
                         PagesCount = true,
                         HtmlContent = GetHtmlContent(orderHeader),
                     }
                 }
                     };
                     var pdfBytes = _pdfConverter.Convert(pdfDoc);

                     return File(pdfBytes, "application/pdf", $"{fileName}.pdf");

                 case "excel":
                     // Generate Excel file using EPPlus
                     var excelPackage = new ExcelPackage();
                     var worksheet = excelPackage.Workbook.Worksheets.Add("Invoice");

                     // Fill in Excel worksheet with data from the orderHeader
                     FillExcelWorksheet(worksheet, orderHeader);

                     var excelBytes = excelPackage.GetAsByteArray();

                     return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileName}.xlsx");

                 default:
                     return BadRequest("Invalid format specified");

             }

         }
         private string GetHtmlContent(OrderHeader orderHeader)
         {
             return $@"
         <h1>Invoice</h1>
         <p>Order ID: {orderHeader.OrderHeaderId}</p>
         <p>Customer: {orderHeader.ApplicationUser.UserName}</p>
         <p>Total Amount: {orderHeader.OrderTotal}</p>
     ";
         }

         private void FillExcelWorksheet(ExcelWorksheet worksheet, OrderHeader orderHeader)
         {
             // Add your logic here to fill in the Excel worksheet with data from the orderHeader
             // For example:
             worksheet.Cells["A1"].Value = "Invoice";
             worksheet.Cells["A2"].Value = "Order ID:";
             worksheet.Cells["B2"].Value = orderHeader.OrderHeaderId;
             worksheet.Cells["A3"].Value = "Customer:";
             worksheet.Cells["B3"].Value = orderHeader.ApplicationUser.UserName;

             worksheet.Cells["A4"].Value = "Total Amount:";
             worksheet.Cells["B4"].Value = orderHeader.OrderTotal;
             // Add more cells as needed
         }*/


       /* [HttpGet]
        [Authorize]
        public IActionResult GenerateInvoice(int orderId) 
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.OrderHeaderId == orderId, includeProperties: "ApplicationUser");
            var document = new PdfDocument();
            string htmlcontent = "<h1>welcome</h2>";*/
            /*htmlcontent += "<h2>VENDOR store</h2>";

            if (orderHeader != null)
            {
                htmlcontent += "<h2> Invoice No: INV" + orderId + " & Invoice Date:" + DateTime.Now + "</h2>";
                htmlcontent += "<h3> Customer : " + orderHeader.Name;
                htmlcontent += "<p>" + orderHeader.streetAddress + "</p>";
               
                htmlcontent += "<div>";
            }

            htmlcontent += "<table style ='width:100%; border: 1px solid #000'>";
            htmlcontent += "<thead style='font-weight:bold'>";
            htmlcontent += "<tr>";
            htmlcontent += "<td style='border:1px solid #000'> Product Code </td>";
            htmlcontent += "<td style='border:1px solid #000'> Description </td>";
            htmlcontent += "<td style='border:1px solid #000'>Qty</td>";
            htmlcontent += "<td style='border:1px solid #000'>Price</td >";
            htmlcontent += "<td style='border:1px solid #000'>Total</td>";
            htmlcontent += "</tr>";
            htmlcontent += "</thead >";
            htmlcontent += "<tbody>";
            if (order != null)
            {
                foreach (var item in OrderVM.OrderDetail)
                {
                    htmlcontent += "<tr>";
                    htmlcontent += "<td>" + item.ProductId + "</td>";
                    htmlcontent += "<td>" + item.Product.Title + "</td>";
                    htmlcontent += "<td>" + item.Count + "</td >";
                    htmlcontent += "<td>" + item.Price.ToString("c") + "</td>";
                    htmlcontent += "<td> " + (item.Count * item.Price).ToString("c") + "</td >";
                    htmlcontent += "</tr>";
                };
            }
            htmlcontent += "</tbody>";

            htmlcontent += "</table>";
            htmlcontent += "</div>";
            htmlcontent += "<br/>";
            htmlcontent += "<br/>";
            htmlcontent += "<div style='text-align:left'>";
            htmlcontent += "<table style='width:100%; border:1px solid #000;float:right' >";
            htmlcontent += "<tr>";
            htmlcontent += "<td style='border:1px solid #000'> Summary Total </td>";
            htmlcontent += "</tr>";
            if (OrderVM != null)
            {
                htmlcontent += "<tr>";
                htmlcontent += "<td style='border: 1px solid #000'> " + OrderVM.OrderHeader.OrderTotal.ToString("c") + " </td>";

                htmlcontent += "</tr>";
            }
            htmlcontent += "</table>";
            htmlcontent += "</div>";
            htmlcontent += "</div>";*/

           /* PdfGenerator.AddPdfPages(document, htmlcontent, PageSize.A4);

            byte[]? response = null;
            using (MemoryStream ms = new MemoryStream())
            {
                document.Save(ms);
                response = ms.ToArray();
            }
            string Filename = "Invoice_" + orderId + ".pdf";
            return File(response, "application/pdf", Filename, true);
              
        }*/

            

        #region API CALLS 
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").OrderByDescending(orderHeader => orderHeader.OrderHeaderId).ToList();

            if (User.IsInRole(SD.Role_Admin))
            {
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").OrderByDescending(orderHeader => orderHeader.OrderHeaderId).ToList();
            }
            else
            {
                var claimsIdentiy = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentiy.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser").OrderByDescending(orderHeader => orderHeader.OrderHeaderId);
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
            return Json(new { data = objOrderHeaders });
        }

        #endregion
    }
}
