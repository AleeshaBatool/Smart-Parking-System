using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.DBContext;
using SmartParkingSystem.Helper;
using SmartParkingSystem.Models;
using System;
using System.Diagnostics;

namespace SmartParkingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }


        private (List<ParkingWithStatusViewModel> ParkingData, int Total, int Occupied, double TodayTime, double TodayAmount) GetParkingSummary()
        {
            var today = DateTime.Today;

            var parkingData = _context.Parkings
                .Select(p => new ParkingWithStatusViewModel
                {
                    Parking = p,
                    CurrentCarParking = _context.CarParkings
                        .Where(cp => cp.parkingID == p.id
                                     && cp.parkingStartTime.Value.Date == today
                                     && (cp.parkingEndTime == null || cp.parkingEndTime > DateTime.Now || cp.parkingEndTime == DateTime.MinValue))
                        .OrderByDescending(cp => cp.parkingStartTime)
                        .FirstOrDefault()
                })
                .ToList();

            var total = parkingData.Count;
            var occupied = parkingData.Count(p => p.Parking.isOccupied);
            var todayTime = parkingData
                .Where(p => p.CurrentCarParking != null)
                .Sum(p =>
                {
                    var startTime = p.CurrentCarParking.parkingStartTime;
                    var endTime = p.CurrentCarParking.parkingEndTime ?? DateTime.Now;
                    var duration = endTime - startTime;
                    return duration.Value.TotalHours > 0 ? duration.Value.TotalHours : 0;
                });

            var todayAmount = parkingData
                .Where(p => p.CurrentCarParking != null)
                .Sum(p =>
                {
                    var startTime = p.CurrentCarParking.parkingStartTime;
                    var endTime = p.CurrentCarParking.parkingEndTime ?? DateTime.Now;
                    var duration = endTime - startTime;
                    var hours = duration.Value.TotalHours > 0 ? duration.Value.TotalHours : 0;
                    return hours * p.Parking.priceperhour;
                });

            return (parkingData, total, occupied, todayTime, todayAmount);
        }

        public IActionResult Index()
        {
            var (parkingData, total, occupied, todayTime, todayAmount) = GetParkingSummary();

            ViewBag.TotalParking = total;
            ViewBag.OccupiedParking = occupied;
            ViewBag.TodayTime = todayTime;
            ViewBag.TodayAmount = todayAmount;

            return View(parkingData);
        }

        public IActionResult GetParkingStatusPartial()
        {
            var (parkingData, _, _, _, _) = GetParkingSummary();
            return PartialView("_ParkingStatus", parkingData);
        }

        [HttpGet]
        public JsonResult GetParkingData()
        {
            var (_, total, occupied, todayTime, todayAmount) = GetParkingSummary();

            return Json(new
            {
                totalParking = total,
                occupiedParking = occupied,
                todayTime,
                todayAmount
            });
        }



        //public IActionResult SetCamera()
        //{
        //    var model = new SetCamera();
        //    // Initialize defaults if needed
        //    return View(model);
        //}

        //[HttpPost]
        //public IActionResult SetCamera(SetCamera model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Process settings
        //        // ...
        //        return RedirectToAction("SomeOtherAction");
        //    }
        //    return View(model);
        //}
        // GET: SetCamera
        public IActionResult SetCamera()
        {
            // Initialize default values for the model
            var model = new SetCamera
            {
                Brightness = 0,
                Contrast = 0,
                Saturation = 0,
                SpecialEffect = 0,
                WhiteBalance = false,
                AwbGain = false,
                WbMode = 0,
                ExposureCtrl = false,
                Aec2 = false,
                AeLevel = 0,
                AecValue = 0,
                GainCtrl = false,
                AgcGain = 0,
                Gainceiling = 0,
                Bpc = false,
                Wpc = false,
                RawGma = false,
                Lenc = false,
                Hmirror = false,
                Vflip = false,
                Dcw = false,
                Colorbar = false
            };

            return View(model);
        }

        // POST: SetCamera
        [HttpPost]
        [ValidateAntiForgeryToken]
       
       
        public IActionResult SetCamera(SetCamera model)
        {
            if (ModelState.IsValid)
            {
                // Save settings logic here (DB or send to ESP32-CAM)

                // Add success message to ViewData
                ViewData["SuccessMessage"] = "Settings applied successfully!";

                // Return the same view with the model and success message
                return View(model);
            }

            // If validation failed, return the same view with errors
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost("api/scan-qr")]
        public IActionResult ScanQrFromImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "Image file is required." });

            // Define the folder path where you want to save the image
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ESP32Images");

            // Ensure the folder exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Generate unique file name
            var fileName = $"ESP32_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var filePath = Path.Combine(folderPath, fileName);

            // Save the image file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                image.CopyTo(stream);
            }

            // Now process the image bytes for QR code detection
            using (var ms = new MemoryStream())
            {
                image.CopyTo(ms);  // Read again for processing (or read from saved file)
                var imageBytes = ms.ToArray();

                var resultText = QRCodeScanner.GetJsonFromQrCode(imageBytes);

                if (string.IsNullOrWhiteSpace(resultText))
                    return NotFound(new { message = "No QR code detected in the image." });

                return Ok(new
                {
                    text = resultText,
                    savedPath = $"/ESP32Images/{fileName}"
                });
            }
        }



    }
}
