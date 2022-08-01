using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using beltReview.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace beltReview.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private MyContext _context;

    public HomeController(ILogger<HomeController> logger, MyContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        ViewBag.NotLoggedIn = true;
        return View();
    }
    
    [HttpPost("user/register")]
    public IActionResult Register(User newUser)
    {
        ViewBag.NotLoggedIn = true;
        if(ModelState.IsValid)
        {
            // Verify if the email is unique
            if(_context.Users.Any(u => u.Email == newUser.Email))
            {
                ModelState.AddModelError("Email", "Email already in use!");
                return View("Index");
            } 
            // Hash the password before adding it to the database
            PasswordHasher<User> Hasher = new PasswordHasher<User>();
            newUser.Password = Hasher.HashPassword(newUser, newUser.Password);
            _context.Add(newUser);
            _context.SaveChanges();
            HttpContext.Session.SetInt32("UserId", newUser.UserId);
            return RedirectToAction("Dashboard");
        } else {
            return View("Index");
        }
    }

    [HttpPost("user/login")]
    public IActionResult Login(LogUser loginUser)
    {
        ViewBag.NotLoggedIn = true;
        if(ModelState.IsValid)
        {
            // verify the email is in our database
            User? userInDb = _context.Users.FirstOrDefault(u => u.Email == loginUser.LogEmail);
            if(userInDb == null)
            {
                ModelState.AddModelError("LogEmail", "Invalid login attempt");
                return View("Index");
            }
            // Then verify if the password matches what's in the database
            PasswordHasher<LogUser> hasher = new PasswordHasher<LogUser>();
            var result = hasher.VerifyHashedPassword(loginUser, userInDb.Password, loginUser.LogPassword);
            if(result == 0)
            {
                ModelState.AddModelError("LogEmail", "Invalid login attempt");
                return View("Index");
            }
            HttpContext.Session.SetInt32("UserId", userInDb.UserId);
            return RedirectToAction("Dashboard");
        } else {
            return View("Index");
        }
    }

    [HttpGet("Dashboard")]
    public IActionResult Dashboard()
    {
        if(HttpContext.Session.GetInt32("UserId") == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.NotLoggedIn = false;
        User? userInDb = _context.Users.Include(a => a.SongsWritten).FirstOrDefault(a => a.UserId == HttpContext.Session.GetInt32("UserId"));
        ViewBag.LoggedIn = userInDb;
        ViewBag.Top = _context.Songs.Include(s => s.Artist).Include(d => d.UsersWhoLiked).OrderByDescending(f => f.UsersWhoLiked.Count).Take(3).ToList();
        return View();
    }

    [HttpGet("song/create")]
    public IActionResult AddSong()
    {
        if(HttpContext.Session.GetInt32("UserId") == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.NotLoggedIn = false;
        return View();
    }

    [HttpPost("song/add")]
    public IActionResult CreateSong(Song newSong)
    {
        ViewBag.NotLoggedIn = false;
        if(ModelState.IsValid)
        {
            newSong.UserId = (int)HttpContext.Session.GetInt32("UserId");
            _context.Add(newSong);
            _context.SaveChanges();
            return Redirect($"/song/{newSong.SongId}");
        } else {
            return View("AddSong");
        }
    }

    [HttpGet("song/all")]
    public IActionResult AllSongs()
    {
        if(HttpContext.Session.GetInt32("UserId") == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.NotLoggedIn = false;
        ViewBag.AllSongs = _context.Songs.Include(a => a.Artist).Include(d => d.UsersWhoLiked).ToList();
        return View();
    }

    [HttpGet("song/{songId}")]
    public IActionResult OneSong(int songId)
    {
        ViewBag.NotLoggedIn = false;
        Song? songToShow = _context.Songs.Include(s => s.Artist).Include(d => d.UsersWhoLiked).FirstOrDefault(a => a.SongId == songId);
        if(songToShow == null) {
            return RedirectToAction("AllSongs");
        }
        return View(songToShow);
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    [HttpGet("song/delete/{songId}")]
    public IActionResult DeleteSong(int songId)
    {
        Song? songToDelete = _context.Songs.SingleOrDefault(a => a.SongId == songId);
        if(songToDelete == null) {
            return RedirectToAction("Dashboard");
        }
        if(songToDelete.UserId != HttpContext.Session.GetInt32("UserId"))
        {
            return RedirectToAction("Logout");
        }
        _context.Songs.Remove(songToDelete);
        _context.SaveChanges();
        return RedirectToAction("Dashboard");
    }

    [HttpGet("song/like/{songId}")]
    public IActionResult LikeSong(int songId)
    {
        if(HttpContext.Session.GetInt32("UserId") == null)
        {
            return RedirectToAction("Index");
        }
        Like newLike = new Like()
        {
            UserId = (int)HttpContext.Session.GetInt32("UserId"),
            SongId = songId
        };
        _context.Add(newLike);
        _context.SaveChanges();
        return Redirect($"/song/{songId}");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
