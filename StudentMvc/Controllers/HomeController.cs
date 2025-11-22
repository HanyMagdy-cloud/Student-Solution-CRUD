using Microsoft.AspNetCore.Mvc;
using StudentMvc.Models;
using System.Net.Http.Json;

namespace StudentMvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("StudentApi");
        }

        //public async Task<IActionResult> Index()
        //{
        //    var students = await _httpClient.GetFromJsonAsync<List<Student>>("api/students");
        //    return View(students);
        //}

        public async Task<IActionResult> Create()  // GET
        {
            // Provide default values for required properties
            return View(new Student { Name = string.Empty, Email = string.Empty });  // empty form
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student model)  // POST
        {
            if (!ModelState.IsValid) return View(model);

            // send to API
            var resp = await _httpClient.PostAsJsonAsync("api/students", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, $"API error: {(int)resp.StatusCode}");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Home/Edit/1
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _httpClient.GetFromJsonAsync<Student>($"api/students/{id}");
            if (student == null) return NotFound();
            return View(student);
        }
        // POST: /Home/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var resp = await _httpClient.PutAsJsonAsync($"api/students/{id}", model);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, $"API error: {(int)resp.StatusCode}");
                return View(model);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Home/Delete/1  -> simple confirm page
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _httpClient.GetFromJsonAsync<Student>($"api/students/{id}");
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resp = await _httpClient.DeleteAsync($"api/students/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, $"API error: {(int)resp.StatusCode}");
                var student = await _httpClient.GetFromJsonAsync<Student>($"api/students/{id}");
                return View(student);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Index(string? searchString)
        {
            ViewBag.SearchString = searchString;

            var url = string.IsNullOrWhiteSpace(searchString)
                ? "api/students"
                : $"api/students?searchString={Uri.EscapeDataString(searchString)}";

            var students = await _httpClient.GetFromJsonAsync<List<Student>>(url)
                           ?? new List<Student>();

            return View(students);
        }



    }
}
