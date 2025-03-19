using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Stripe.Climate;
using System.Security.Claims;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Stripe;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM orderVm { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult details(int OrderId)
        {
            orderVm = new OrderVM();
            orderVm.OrderHeader = _unitOfWork.orderHeader.Get(u => u.Id == OrderId, "ApplicationUser");
            orderVm.OrderDetail = _unitOfWork.orderDetail.GetAll(u => u.OrderHeaderId == OrderId, "Product");
            return View(orderVm);

        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]

        public IActionResult UpdateOrderDetails()
        {
            var orderHeader = _unitOfWork.orderHeader.Get(u => u.Id == orderVm.OrderHeader.Id);
            //orderHeader.OrderStatus = SD.StatusShipped;
            //orderHeader.ShippingDate = DateTime.Now;
            //if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            //{
            //    orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            //}

            orderHeader.Name = orderVm.OrderHeader.Name;
            orderHeader.PhoneNumber = orderVm.OrderHeader.PhoneNumber;
            orderHeader.State = orderVm.OrderHeader.State;
            orderHeader.StreetAddress = orderVm.OrderHeader.StreetAddress;
            orderHeader.PostalCode = orderVm.OrderHeader.PostalCode;
            orderHeader.City = orderVm.OrderHeader.City;
            if (!string.IsNullOrEmpty(orderVm.OrderHeader.Carrier))
            {
                orderHeader.Carrier = orderVm.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(orderVm.OrderHeader.TrackingNumber))
            {
                orderHeader.TrackingNumber = orderVm.OrderHeader.TrackingNumber;
            }

            _unitOfWork.orderHeader.Update(orderHeader);
            _unitOfWork.save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(details),new {OrderId = orderHeader.Id});

        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.orderHeader.UpdateStatus(orderVm.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = orderVm.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {

            var orderHeader = _unitOfWork.orderHeader.Get(u => u.Id == orderVm.OrderHeader.Id);
            orderHeader.TrackingNumber = orderVm.OrderHeader.TrackingNumber;
            orderHeader.Carrier = orderVm.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _unitOfWork.orderHeader.Update(orderHeader);
            _unitOfWork.save();
            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = orderVm.OrderHeader.Id });
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            OrderHeader orderHeader = new OrderHeader();

            orderHeader = _unitOfWork.orderHeader.Get(u => u.Id == orderVm.OrderHeader.Id);
            //var orderDetail = _unitOfWork.orderDetail.GetAll(u => u.OrderHeaderId == orderVm.OrderHeader.Id).ToList();

            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.orderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                //_unitOfWork.orderHeader.Remove(orderHeader);
                //foreach (var item in orderDetail)
                //{
                //    if (item != null)
                //    {
                //        _unitOfWork.orderDetail.Remove(item);

                //    }
                //}

                _unitOfWork.orderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);

            }
            _unitOfWork.save();
            TempData["Success"] = "Order Cancelled Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = orderVm.OrderHeader.Id });

        }
        [ActionName("details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            orderVm.OrderHeader = _unitOfWork.orderHeader
                .Get(u => u.Id == orderVm.OrderHeader.Id, includeProperties: "ApplicationUser");
            orderVm.OrderDetail = _unitOfWork.orderDetail
                .GetAll(u => u.OrderHeaderId == orderVm.OrderHeader.Id, includeProperties: "Product");

            //stripe logic
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={orderVm.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={orderVm.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in orderVm.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
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


            var service = new SessionService();
            Session session = service.Create(options);
            _unitOfWork.orderHeader.UpdateStripePaymentID(orderVm.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {

            OrderHeader orderHeader = _unitOfWork.orderHeader.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //this is an order by company

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.orderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.orderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.save();
                }


            }


            return View(orderHeaderId);
        }




        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaderList;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaderList = _unitOfWork.orderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaderList = _unitOfWork.orderHeader
                    .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }


            switch (status)
            {
                case "pending":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;

            }
            return Json(new { data = objOrderHeaderList });
        }
        #endregion
    }
}
