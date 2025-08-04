using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduMation.Data;
using EduMation.Models;

namespace EduMation.Controllers
{
    public class VideosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VideosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var videos = await _context.Videos.ToListAsync();
            return View(videos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var video = await _context.Videos.FindAsync(id);
            if (video == null)
            {
                return NotFound();
            }
            return View(video);
        }
    }
}