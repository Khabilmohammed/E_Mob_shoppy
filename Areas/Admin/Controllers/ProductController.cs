using E_mob_shoppy.DataAccess.Data;
using E_mob_shoppy.DataAccess.Repository;
using E_mob_shoppy.DataAccess.Repository.IRepository;
using E_mob_shoppy.Models;
using E_mob_shoppy.Models.ViewModel;
using E_mob_shoppy.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace E_mob_shoppy.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController:Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork,IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }   


        public IActionResult index()
        {
            List<Product> productlist=_unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
           
            return View(productlist);
        }


        public IActionResult Upsert(int? id)
        {

            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category
               .GetAll().Select(u => new SelectListItem
               {
                   Text = u.Name,
                   Value = u.Category_Id.ToString()
               }),
                Product = new Product()
            };

            if(id == null || id == 0)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product=_unitOfWork.Product.Get(u=>u.ProductId == id);
                return View(productVM);
            }
           
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, IFormFile? file)
        {
            
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName=Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath= Path.Combine(wwwRootPath, @"Images\Product");
                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        //delete the old image
                        var OldImagePath=
                            Path.Combine(wwwRootPath,productVM.Product.ImageUrl.TrimStart('\\'));

                        if(System.IO.File.Exists(OldImagePath))
                        {
                            System.IO.File.Delete(OldImagePath);
                        }
                    }


                    using (var filestream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(filestream);
                    }

                    productVM.Product.ImageUrl = @"\Images\Product\" + fileName;
                   
                }

                if (productVM.Product.ProductId == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);
                }
                else
                {
                    _unitOfWork.Product.Upadte(productVM.Product);
                }
               
                _unitOfWork.Save();
                TempData["success"] = "The Data is Created";
                return RedirectToAction("Index", "Product");
            }
            else
            {

                productVM.CategoryList = _unitOfWork.Category
          .GetAll().Select(u => new SelectListItem
          {
              Text = u.Name,
              Value = u.Category_Id.ToString()
          });
                return View(productVM);
            }
        }


       


        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objproductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objproductList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var producteToBeDeleted = _unitOfWork.Product.Get(u => u.ProductId == id);

            if (producteToBeDeleted == null)
            {
                return Json(new {sucess=false,message="Eroor while deleting"});
            }
            var OldImagePath =
                          Path.Combine(_webHostEnvironment.WebRootPath,
                          producteToBeDeleted.ImageUrl.TrimStart('\\'));

            if (System.IO.File.Exists(OldImagePath))
            {
                System.IO.File.Delete(OldImagePath);
            }

            _unitOfWork.Product.Remove(producteToBeDeleted);
            _unitOfWork.Save();
            return Json(new { success = true,message="Deleted Successfully" });
        }


        #endregion
    }
}
