using System.Runtime.ExceptionServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using WizardsRift.Data;
using WizardsRift.Models;
using SixLabors.ImageSharp;

namespace WizardsRift.Controllers;

public class ModsController : Controller
{
    private readonly ApplicationDbContext DbContext;
    private readonly UserManager<ApplicationUser> UserManager;

    public ModsController(ApplicationDbContext db, UserManager<ApplicationUser> user_manager)
    {
        UserManager = user_manager;
        DbContext = db;
    }

    private async Task SaveModArchiveAsync(int id, IFormFile file)
    {
        Directory.CreateDirectory($"E:\\mods\\{id}");

        await using (Stream fs = new FileStream($"E:\\mods\\{id}\\archive.bin", FileMode.Create))
        {
            await file.CopyToAsync(fs);
        }
    }
    
    private async Task SaveModImageAsync(int id, IFormFile image_file, string filename)
    {
        Directory.CreateDirectory($"E:\\mods\\{id}");

        var image = await SixLabors.ImageSharp.Image.LoadAsync(image_file.OpenReadStream());

        await using var fs = new FileStream($"E:\\mods\\{id}\\image.png", FileMode.Create);
        await image.SaveAsync(fs, new PngEncoder());
    }

    [Route("mods")]
    public IActionResult Index()
    {
        return View();
    }
    
    [Route("mods/{id}")]
    public IActionResult Details(int id)
    {
        var mod = DbContext.Mods.FirstOrDefault(m => m.Id == id);
        
        if (mod == null)
        {
            return NotFound();
        }
        
        return View(mod);
    }
    
    [Route("mods/{id}/archive")]
    public IActionResult Archive(int id)
    {
        var mod = DbContext.Mods.FirstOrDefault(m => m.Id == id);
        
        if (mod == null)
        {
            return NotFound();
        }
        
        byte[] bytes = System.IO.File.ReadAllBytes($"E:\\mods\\{id}\\archive.bin");
        return File(bytes, "application/force-download", mod.FileName);
    }

    [Route("mods/{id}/image")]
    public IActionResult Image(int id)
    {
        var mod = DbContext.Mods.FirstOrDefault(m => m.Id == id);
        
        if (mod == null)
        {
            return NotFound();
        }

        if (System.IO.File.Exists($"E:\\mods\\{id}\\image.png"))
        {
            byte[] bytes = System.IO.File.ReadAllBytes($"E:\\mods\\{id}\\image.png");
            return File(bytes, "image/png");
        }
        else
        {
            byte[] bytes = System.IO.File.ReadAllBytes($"E:\\mods\\default-image.png");
            return File(bytes, "image/png");
        }
    }

    [Route("mods/new")]
    public IActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    [Route("mods/new")]
    public async Task<IActionResult> Create([Bind("Name", "Description", "Summary")] Mod mod, IFormFile? archive, IFormFile? image_file)
    {
        var username = User.Identity?.Name;
        if (username == null) { return RedirectToPage("/"); }

        mod.Author = await UserManager.FindByNameAsync(username);
        mod.DateCreated = DateTime.Now;
        
        //These fields are automatically set
        ModelState.Remove("FileName");
        ModelState.Remove("Author");
        ModelState.Remove("DateCreated");
        
        if (archive == null)
        {
            ModelState.AddModelError("FileName", "Mod files must be included.");
        }
        else
        {
            mod.FileName = archive.FileName;
            ModelState.Remove("FileName");
            
            //Test for routing in the filename
            var test_path = Path.Combine("E:\\" + archive.FileName);
            if (Path.GetDirectoryName(test_path) != "E:\\")
            {
                ModelState.AddModelError("FileName", "File name is invalid.");
            }
        }
        
        if(DbContext.Mods.Any(m => m.Name == mod.Name))
        {
            ModelState.AddModelError("Name", $"A mod with the name '{mod.Name}' already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(mod);
        }

        await using var tx = await DbContext.Database.BeginTransactionAsync();
        
        DbContext.Mods.Add(mod);
        await DbContext.SaveChangesAsync();

        await SaveModArchiveAsync(mod.Id, archive);
        
        if (image_file != null)
        {
            await SaveModImageAsync(mod.Id, image_file, "image.png");
        }

        await tx.CommitAsync();

        return Redirect($"/mods/{mod.Id}");
    }
    
}