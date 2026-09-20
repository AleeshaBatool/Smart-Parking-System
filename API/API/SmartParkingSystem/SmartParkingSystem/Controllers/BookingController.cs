using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartParkingSystem.DBContext;
using SmartParkingSystem.Helper;

namespace SmartParkingSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }


      
        // GET: Booking
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.CarParkings.Include(c => c.Parking);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carParking = await _context.CarParkings
                .Include(c => c.Parking)
                .FirstOrDefaultAsync(m => m.id == id);
            if (carParking == null)
            {
                return NotFound();
            }

            return View(carParking);
        }

       
        public IActionResult Create()
        {
            var parkings = _context.Parkings.Where(p => !p.isOccupied).ToList();
            ViewData["parkingID"] = new SelectList(parkings, "id", "name");
            ViewBag.ParkingRates = parkings.ToDictionary(p => p.id, p => p.priceperhour);

            return View();
        }

        // POST: Booking/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,parkingID,carNumber,parkingStartTime,parkingEndTime,totalAmount,EasyPaisaPhoneNumber,Email")] CarParking carParking)
        {
            if (ModelState.IsValid)
            {
                string secureKey = "RElHSVNZTkNURUNITk9MT0dJRVM6ZWZkYmE5YWI0NzFkNmZmOThmNTJkMWJlNmJlYTZkODU="; // Move to appsettings in real project
                string orderId = "ORD" + Guid.NewGuid().ToString("N").Substring(0, 10);

                var paymentRequest = new EasyPaisaRequest
                {
                    orderId = orderId,
                    transactionAmount = carParking.totalAmount.ToString(),
                    mobileAccountNo = carParking.EasyPaisaPhoneNumber,
                    emailAddress = "arslanhabib41@gmail.com"
                };

                var response = await InitiateEasyPaisaPayment(paymentRequest, secureKey);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<JObject>(body);

                    string responseCode = result["responseCode"]?.ToString();
                    string responseDesc = result["responseDesc"]?.ToString();
                    string transactionId = result["transactionId"]?.ToString();

                    if (responseCode == "0000" && responseDesc == "SUCCESS")
                    {
                        //carParking.TransactionId = transactionId;
                        //carParking.OrderId = orderId;

                        _context.Add(carParking);
                        await _context.SaveChangesAsync();

                        return View("Success", new { orderId, amount = carParking.totalAmount, transactionId });
                    }
                    else
                    {
                        return View("Error", new { message = responseDesc });


                    }
                }
                else
                {
                    return View("Error", new { message = "Unable to connect to EasyPaisa API." });
                }
            }

            ViewData["parkingID"] = new SelectList(_context.Parkings, "id", "id", carParking.parkingID);
            return View(carParking);
        }


        private async Task<HttpResponseMessage> InitiateEasyPaisaPayment(EasyPaisaRequest requestData, string secureKey)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Credentials", secureKey);

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                return await client.PostAsync("https://easypay.easypaisa.com.pk/easypay-service/rest/v4/initiate-ma-transaction", content);
            }
        }

         
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var carParking = await _context.CarParkings.FindAsync(id);
            if (carParking == null)
                return NotFound();

            ViewData["parkingID"] = new SelectList(
                _context.Parkings.Where(p => !p.isOccupied), "id", "name", carParking.parkingID);
            return View(carParking);
        }

        // POST: Booking/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,parkingID,carNumber,parkingStartTime,parkingEndTime,totalAmount,EasyPaisaPhoneNumber,Email")] CarParking carParking)
        {
            if (id != carParking.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(carParking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarParkingExists(carParking.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["parkingID"] = new SelectList(_context.Parkings, "id", "id", carParking.parkingID);
            return View(carParking);
        }

        // GET: Booking/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carParking = await _context.CarParkings
                .Include(c => c.Parking)
                .FirstOrDefaultAsync(m => m.id == id);
            if (carParking == null)
            {
                return NotFound();
            }

            return View(carParking);
        }

        // POST: Booking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var carParking = await _context.CarParkings.FindAsync(id);
            if (carParking != null)
            {
                _context.CarParkings.Remove(carParking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarParkingExists(int id)
        {
            return _context.CarParkings.Any(e => e.id == id);
        }
    }
}
