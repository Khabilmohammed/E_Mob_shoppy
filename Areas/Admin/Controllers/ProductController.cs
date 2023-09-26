using E_mob_shoppy.DataAccess.Data;
using E_mob_shoppy.DataAccess.Repository;
using E_mob_shoppy.DataAccess.Repository.IRepository;
using E_mob_shoppy.Models;
using Microsoft.AspNetCore.Mvc;

namespace E_mob_shoppy.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController:Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        public ProductController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;   
        }


        public IActionResult index()
        {
            var objproduct=_unitOfWork.Product.GetAll().ToList();
            return View(objproduct);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product? productfromdb = _unitOfWork.Product.Get(u => u.ProductId == id);
            if (productfromdb == null)
            {

                return NotFound();
            }
            return View(productfromdb);
        }


        [HttpPost]
        public IActionResult Edit(Product obj)
        {
            
            if (ModelState.IsValid)
            {
                _unitOfWork.Product.Upadte(obj);
                _unitOfWork.Save();
                TempData["success"] = "The data is upadated";
                return RedirectToAction("Index", "Product");
            }
            return View();

        }


        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Product obj)
        {
            
            if (ModelState.IsValid)
            {
                _unitOfWork.Product.Add(obj);
                _unitOfWork.Save();
                TempData["success"] = "The Data is Created";
                return RedirectToAction("Index", "Product");
            }
            return View();

        }


        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product? product = _unitOfWork.Product.Get(u => u.ProductId == id);
            if (product == null)
            {

                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeletePost(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product? product = _unitOfWork.Product.Get(u => u.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }
            _unitOfWork.Product.Remove(product);
            _unitOfWork.Save();
            TempData["success"] = "The data is Removed";
            return RedirectToAction("Index", "Product");
        }
    }
}
