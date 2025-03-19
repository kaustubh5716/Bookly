using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Stripe.Checkout;


namespace BulkyWeb.Areas.Customer.Controllers
{
[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    
    private readonly IUnitOfWork _unitOfWork;
    [BindProperty]
    public ShoppingCartVM ShoppingCartVm { get; set; }

    public CartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        ShoppingCartVm = new ShoppingCartVM();
        ShoppingCartVm.OrderHeader = new OrderHeader();
        ShoppingCartVm.ShoppingList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,includeProperties:"Product");
        foreach (var cart in ShoppingCartVm.ShoppingList )
        {
            cart.price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.price * cart.Count);
        }

        //var orderHead = _unitOfWork.orderHeader.Get(u => u.ApplicationUserId == userId);
        //if (orderHead!= null && orderHead.SessionId != null && orderHead.ApplicationUser.CompanyId != 0)
        //{
        //    _unitOfWork.orderHeader.Remove(orderHead);
        //    _unitOfWork.save();
        //    TempData["Success"] = "Payment cancel Successfully";
        //}
        return View(ShoppingCartVm);
    }

    public IActionResult plus(int cartId)
    {
        var cartFromDb = _unitOfWork.shoppingCart.Get(u=>u.Id == cartId);
        cartFromDb.Count += 1;
        _unitOfWork.shoppingCart.Update(cartFromDb);
        _unitOfWork.save();
        return RedirectToAction(nameof(Index));

    }
    public IActionResult minus(int cartId)
    {
        ShoppingCart cartFromDb = new ShoppingCart();
        cartFromDb = _unitOfWork.shoppingCart.Get(u => u.Id == cartId);
        if (cartFromDb.Count <= 1)
        {
            _unitOfWork.shoppingCart.Remove(cartFromDb);
            HttpContext.Session.SetInt32(SD.sessionCart , _unitOfWork.shoppingCart
                .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
            }
        else
        {
            cartFromDb.Count -= 1;
            _unitOfWork.shoppingCart.Update(cartFromDb);

        }
        _unitOfWork.save();
        return RedirectToAction(nameof(Index));

    }
    public IActionResult remove(int cartId)
    {
        var cartFromDb = _unitOfWork.shoppingCart.Get(u => u.Id == cartId);
        _unitOfWork.shoppingCart.Remove(cartFromDb);
        HttpContext.Session.SetInt32(SD.sessionCart, _unitOfWork.shoppingCart
            .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
            _unitOfWork.save();
        return RedirectToAction(nameof(Index));

    }

    public IActionResult Summary()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        ShoppingCartVm = new ShoppingCartVM();
        ShoppingCartVm.OrderHeader = new OrderHeader();
        ShoppingCartVm.ShoppingList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
        ShoppingCartVm.OrderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);
        ShoppingCartVm.OrderHeader.Name = ShoppingCartVm.OrderHeader.ApplicationUser.Name;
        ShoppingCartVm.OrderHeader.PhoneNumber = ShoppingCartVm.OrderHeader.ApplicationUser.PhoneNumber;
        ShoppingCartVm.OrderHeader.StreetAddress = ShoppingCartVm.OrderHeader.ApplicationUser.StreetAddress;
        ShoppingCartVm.OrderHeader.City = ShoppingCartVm.OrderHeader.ApplicationUser.City;
        ShoppingCartVm.OrderHeader.State = ShoppingCartVm.OrderHeader.ApplicationUser.State;
        ShoppingCartVm.OrderHeader.PostalCode = ShoppingCartVm.OrderHeader.ApplicationUser.PostalCode;
            foreach (var cart in ShoppingCartVm.ShoppingList)
            {
            cart.price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.price * cart.Count);
            }
        return View(ShoppingCartVm);
    }
    [HttpPost]
    [ActionName("Summary")]
    public IActionResult SummaryPost()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        ShoppingCartVm.ShoppingList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
        ShoppingCartVm.OrderHeader.OrderDate = System.DateTime.Now;
        ShoppingCartVm.OrderHeader.ApplicationUserId = userId;
        ApplicationUser ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);
        foreach (var cart in ShoppingCartVm.ShoppingList)
        {
            cart.price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.price * cart.Count);
        }

        if (ApplicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            //normal customer do payment now
            ShoppingCartVm.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVm.OrderHeader.OrderStatus = SD.StatusPending;
        }
        else
        {
                //company customer can do payment later
                ShoppingCartVm.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVm.OrderHeader.OrderStatus = SD.StatusApproved;

        }
        _unitOfWork.orderHeader.Add(ShoppingCartVm.OrderHeader);
        _unitOfWork.save();
        foreach (var cart in ShoppingCartVm.ShoppingList)
        {
            OrderDetail orderDetail = new OrderDetail();
            orderDetail.ProductId = cart.ProductId;
            orderDetail.OrderHeaderId = ShoppingCartVm.OrderHeader.Id;
            orderDetail.Price = cart.price;
            orderDetail.Count = cart.Count;
            _unitOfWork.orderDetail.Add(orderDetail);
            _unitOfWork.save();
        }
        if (ApplicationUser.CompanyId.GetValueOrDefault() == 0)
        {
                //normal customer Strip Payment
                var domain = Request.Scheme + "://" + Request.Host.Value + "/";
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVm.OrderHeader.Id}",
                    CancelUrl = domain + "Customer/Cart/Index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };
                foreach (var item in ShoppingCartVm.ShoppingList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.price * 100), // $20.50 => 2050
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }
                var service = new Stripe.Checkout.SessionService();
                 Session session =  service.Create(options);
                 _unitOfWork.orderHeader.UpdateStripePaymentID(ShoppingCartVm.OrderHeader.Id,session.Id,session.PaymentIntentId);
                 _unitOfWork.save();
                 Response.Headers.Add("Location", session.Url);
                 return new StatusCodeResult(303);
        }

        return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVm.OrderHeader.Id });
    }

    public IActionResult OrderConfirmation(int id)
    {
        OrderHeader orderHeader = _unitOfWork.orderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
        if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
        {
            //this is an order by customer

            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.orderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                _unitOfWork.orderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                _unitOfWork.save();
            }
            //HttpContext.Session.Clear();

        }
        List<ShoppingCart> shoppingCarts = _unitOfWork.shoppingCart
            .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

        _unitOfWork.shoppingCart.RemoveRange(shoppingCarts);
        _unitOfWork.save();
        HttpContext.Session.Clear();

            return View(id);
    }

        public double GetPriceBasedOnQuantity(ShoppingCart cart)
    {
        if (cart.Count <= 50)
        {
            return cart.Product.Price;
        }
        else if (cart.Count <= 100)
        {
            return cart.Product.Price50;
        }
        else
        {
            return cart.Product.Price100;

        }
    }

}
}
