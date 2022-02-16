using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WizardsRift.Data;
using X.PagedList;

namespace WizardsRift.Controllers;

public class UsersController : Controller
{
    private readonly ApplicationDbContext DbContext;
    private readonly UserManager<ApplicationUser> UserManager;
    
    public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> user_manager)
    {
        UserManager = user_manager;
        DbContext = db;
    }
    
    [Route("users/{username}")]
    public async Task<IActionResult> Details(string username, int? page)
    {
        var user = await UserManager.Users.Where(u => u.UserName == username).FirstOrDefaultAsync();

        if (user == null)
        {
            return View(null);
        }
        
        var mods = DbContext.Mods.Where(m => m.Author == user).OrderBy(m => m.DateCreated);
        return View(mods.ToPagedList(page ?? 1, 4));
    }
}