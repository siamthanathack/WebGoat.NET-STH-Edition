using WebGoatCore.Models;
using WebGoatCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using WebGoatCore.ViewModels;

using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace WebGoatCore.Controllers
{
    [Route("[controller]/[action]")]
    public class ProductController : Controller
    {
        private readonly ProductRepository _productRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly SupplierRepository _supplierRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ProductRepository productRepository, IWebHostEnvironment webHostEnvironment, CategoryRepository categoryRepository, SupplierRepository supplierRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _supplierRepository = supplierRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        //---------------------------------------- new
        public IActionResult Search(string? nameFilter, int? selectedCategoryId)
        {
            // log search keyword
            if (nameFilter != null)
            {
                // https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca3006
                // https://www.veracode.com/security/dotnet/cwe-78
                // https://knowledge-base.secureflag.com/vulnerabilities/code_injection/os_command_injection__net.html
                
                var process = new System.Diagnostics.Process();
                var startInfo = new System.Diagnostics.ProcessStartInfo();
                var contentPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = System.IO.Path.Combine(contentPath, "search_keyword.log");
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/C echo {nameFilter} >> {filePath}";
                }
                else
                {
                    startInfo.FileName = "/bin/bash";
                    startInfo.Arguments = $"-c \"echo {nameFilter} >> {filePath}\"";
                }

                // -------------------- uncomment to fix OS Command Injection
                /*Regex regex = new Regex(@"^[_'""a-zA-Z0-9]*$");
                Match match = regex.Match(nameFilter);
                if (!match.Success)
                {
                    return StatusCode(500);
                }*/

                process.StartInfo = startInfo;
                process.Start();

                if (process != null)
                {
                    process.WaitForExit();
                }
                else
                { 
                    return StatusCode(500);
                }
            }


            if (selectedCategoryId != null && _categoryRepository.GetById(selectedCategoryId.Value) == null)
            {
                selectedCategoryId = null;
            }

            var product = _productRepository.FindNonDiscontinuedProducts(nameFilter, selectedCategoryId)
                .Select(p => new ProductListViewModel.ProductItem()
                {
                    Product = p,
                    ImageUrl = GetImageUrlForProduct(p),
                });

            return View(new ProductListViewModel()
            {
                Products = product,
                ProductCategories = _categoryRepository.GetAllCategories(),
                SelectedCategoryId = selectedCategoryId,
                NameFilter = nameFilter
            });
           
        }
        //---------------------------------------- old
        /* public IActionResult Search(string? nameFilter, int? selectedCategoryId)
        {
            if (selectedCategoryId != null && _categoryRepository.GetById(selectedCategoryId.Value) == null)
            {
                selectedCategoryId = null;
            }

            var product = _productRepository.FindNonDiscontinuedProducts(nameFilter, selectedCategoryId)
                .Select(p => new ProductListViewModel.ProductItem()
                {
                    Product = p,
                    ImageUrl = GetImageUrlForProduct(p),
                });

            return View(new ProductListViewModel()
            {
                Products = product,
                ProductCategories = _categoryRepository.GetAllCategories(),
                SelectedCategoryId = selectedCategoryId,
                NameFilter = nameFilter
            });
        }*/
        //----------------------------------------




        //---------------------------------------- new
        [HttpGet("{productId}")]
        public IActionResult Details(int productId, string quantity = "1")
        {
            var model = new ProductDetailsViewModel();
            if (_productRepository.isInStock(productId, quantity))
            {
                try
                {
                    var product = _productRepository.GetProductById(productId);
                    model.Product = product;
                    model.CanAddToCart = true;
                    model.ProductImageUrl = GetImageUrlForProduct(product);
                    model.Quantity = Convert.ToInt16(quantity);
                }
                catch (InvalidOperationException)
                {
                    model.ErrorMessage = "Product not found.";
                }
                catch (Exception ex)
                {
                    model.ErrorMessage = string.Format("An error has occurred: {0}", ex.Message);
                }
            }
            else
            {
                model.ErrorMessage = "Out of Stock.";
            }
            

            return View(model);
        }
        

        [HttpGet]
        public IActionResult Image(string imgName)
        {
            // ---------------- uncomment to fix path traversal
            /*imgName = imgName.Split('/').Last();
            imgName = imgName.Split('\\').Last();*/
            // ----------------

            Byte[] b = System.IO.File.ReadAllBytes(@"./wwwroot/Images/ProductImages/"+imgName);
            return File(b, "image/jpeg");
        }
        //---------------------------------------- old
        /*[HttpGet("{productId}")]
        public IActionResult Details(int productId, short quantity = 1)
        {
            var model = new ProductDetailsViewModel();
            try
            {
                var product = _productRepository.GetProductById(productId);
                model.Product = product;
                model.CanAddToCart = true;
                model.ProductImageUrl = GetImageUrlForProduct(product);
                model.Quantity = quantity;
            }
            catch (InvalidOperationException)
            {
                model.ErrorMessage = "Product not found.";
            }
            catch (Exception ex)
            {
                model.ErrorMessage = string.Format("An error has occurred: {0}", ex.Message);
            }

            return View(model);
        }*/
        //----------------------------------------

        [Authorize(Roles = "Admin")]
        public IActionResult Manage()
        {
            return View(_productRepository.GetAllProducts());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Add()
        {
            return View("AddOrEdit", new ProductAddOrEditViewModel()
            {
                AddsNew = true,
                ProductCategories = _categoryRepository.GetAllCategories(),
                Suppliers = _supplierRepository.GetAllSuppliers(),
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Add(Product product)
        {
            try
            {
                _productRepository.Add(product);
                return RedirectToAction("Edit", new { id = product.ProductId });
            }
            catch
            {
                return View("AddOrEdit", new ProductAddOrEditViewModel()
                {
                    AddsNew = true,
                    ProductCategories = _categoryRepository.GetAllCategories(),
                    Suppliers = _supplierRepository.GetAllSuppliers(),
                    Product = product,
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public IActionResult Edit(int id)
        {
            return View("AddOrEdit", new ProductAddOrEditViewModel()
            {
                AddsNew = false,
                ProductCategories = _categoryRepository.GetAllCategories(),
                Product = _productRepository.GetProductById(id),
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id?}")]
        public IActionResult Edit(Product product)
        {
            product = _productRepository.Update(product);
            return View("AddOrEdit", new ProductAddOrEditViewModel()
            {
                AddsNew = false,
                ProductCategories = _categoryRepository.GetAllCategories(),
                Product = product,
            });
        }

        //---------------------------------------- new
        private string GetImageUrlForProduct(Product product)
        {
            return $"/Product/Image?imgName={product.ProductId}.jpg";
        }

        //---------------------------------------- old
        /*private string GetImageUrlForProduct(Product product)
        {
            var imageUrl = $"/Images/ProductImages/{product.ProductId}.jpg";
            if (!_webHostEnvironment.WebRootFileProvider.GetFileInfo(imageUrl).Exists)
            {
                imageUrl = "/Images/ProductImages/NoImage.jpg";
            }
            return imageUrl;
        }*/
    }
}