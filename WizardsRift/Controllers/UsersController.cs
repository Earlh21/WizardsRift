using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PagedList;
using WizardsRift.Data;

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
    
    [Route("account")]
    public async Task<IActionResult> Index(int? page)
    {
        var username = User.Identity?.Name;
        if (username == null) { return Redirect("/"); }

        var user = await UserManager.FindByNameAsync(username);
        var mods = DbContext.Mods.Where(m => m.Author == user).OrderBy(m => m.DateCreated);
        return View(mods.ToPagedList(page ?? 1, 10));
    }
}