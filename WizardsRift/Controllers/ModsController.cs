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
using X.PagedList;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;

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

    private AmazonS3Client CreateS3Client()
    {
        var key = Environment.GetEnvironmentVariable("WRIFT_S3_KEY", EnvironmentVariableTarget.User);
        var secret = Environment.GetEnvironmentVariable("WRIFT_S3_SECRET", EnvironmentVariableTarget.User);
        var region = Environment.GetEnvironmentVariable("WRIFT_S3_REGION", EnvironmentVariableTarget.User);
        
        return new AmazonS3Client(key, secret, Amazon.RegionEndpoint.GetBySystemName(region));
    }

    private async Task PutS3FileAsync(string key, IFormFile file)
    {
        var bucket = Environment.GetEnvironmentVariable("WRIFT_S3_BUCKET", EnvironmentVariableTarget.User);
        using var client = CreateS3Client();

        var request = new TransferUtilityUploadRequest
        {
            InputStream = file.OpenReadStream(),
            Key =  key,
            BucketName = bucket
        };

        using var transfer = new TransferUtility(client);
        await transfer.UploadAsync(request);
    }

    private async Task<byte[]?> GetS3FileAsync(string key)
    {
        var bucket = Environment.GetEnvironmentVariable("WRIFT_S3_BUCKET", EnvironmentVariableTarget.User);
        using var client = CreateS3Client();

        try
        {
            using var response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = bucket,
                Key = key
            });

            using var m_stream = new MemoryStream();

            using var response_stream = response.ResponseStream;
            await response.ResponseStream.CopyToAsync(m_stream);
            return m_stream.ToArray();
        }
        catch(AmazonS3Exception)
        {
            return null;
        }
    }

    private string GetS3PreSignedUrl(string key)
    {
        var bucket = Environment.GetEnvironmentVariable("WRIFT_S3_BUCKET", EnvironmentVariableTarget.User);
        using var client = CreateS3Client();

        return client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Expires = DateTime.UtcNow.AddHours(1)
        });
    }
    
    private bool IsFilenameValid(string filename)
    {
        //Test for routing in the filename
        var test_path = Path.Combine("E:\\" + filename);
        return Path.GetDirectoryName(test_path) == "E:\\";
    }

    [Route("mods")]
    public async Task<IActionResult> Index(int? page, string? search, string? sort_order)
    {
        ViewBag.SearchParam = search;
        ViewBag.SortParam = sort_order;
        ViewBag.NameSortParm = sort_order == "name_asc" ? "name_desc" : "name_asc";
        ViewBag.DateSortParm = sort_order == "date_asc" ? "date_desc" : "date_asc";
        ViewBag.DownloadsSortParm = sort_order == "downloads_desc" ? "downloads_asc" : "downloads_desc";
        
        var mods = DbContext.Mods.Include(m => m.Author).AsQueryable();

        if (search != null)
        {
            search = search.ToUpper();
            mods = mods.Where(m => m.Name.ToUpper().Contains(search) 
                                   || (m.Summary != null && m.Summary.ToUpper().Contains(search))
                                   || m.Description.ToUpper().Contains(search));
        }
        
        switch (sort_order)
        {
            case "name_asc":
                mods = mods.OrderBy(m => m.Name.ToUpper());
                break;
            case "name_desc":
                mods = mods.OrderByDescending(m => m.Name.ToUpper());
                break;
            case "date_asc":
                mods = mods.OrderBy(m => m.DateCreated);
                break;
            case "date_desc":
                mods = mods.OrderByDescending(m => m.DateCreated);
                break;
            case "downloads_asc":
                mods = mods.OrderBy(m => m.DownloadCount);
                break;
            case "downloads_desc":
                mods = mods.OrderByDescending(m => m.DownloadCount);
                break;
            default:
                mods = mods.OrderBy(m => m.DateCreated);
                break;
        }

        return View(await mods.ToPagedListAsync(page ?? 1, 4));
    }
    
    [Route("mods/{id}")]
    public IActionResult Details(int id)
    {
        var mod = DbContext.Mods.Include(m => m.Author).FirstOrDefault(m => m.Id == id);

        if (mod == null)
        {
            return NotFound();
        }
        
        return View(mod);
    }
    
    [Route("mods/{id}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        var mod = DbContext.Mods.FirstOrDefault(m => m.Id == id);
        
        if (mod == null)
        {
            return NotFound();
        }

        mod.DownloadCount++;

        DbContext.SaveChanges();

        return File(await GetS3FileAsync($"mod_{id}/archive"), "application/force-download", mod.FileName);
    }

    [Route("mods/{id}/image")]
    public async Task<IActionResult> Image(int id)
    {
        var mod = await DbContext.Mods.FirstOrDefaultAsync(m => m.Id == id);
        
        if (mod == null)
        {
            return NotFound();
        }

        string url = GetS3PreSignedUrl($"mod_{id}/image");

        return Redirect(url);
    }

    [Route("mods/new")]
    public IActionResult Create()
    {
        if (User.Identity?.Name == null)
        {
            return StatusCode(401);
        }
        
        return View();
    }
    
    [HttpPost]
    [Route("mods/new")]
    public async Task<IActionResult> Create([Bind("Name", "Description", "Summary")] Mod mod, IFormFile? archive, IFormFile? image_file)
    {
        var username = User.Identity?.Name;
        if (username == null) { return StatusCode(401); }
        
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
            ModelState.Remove("FileName");
            
            if(!IsFilenameValid(archive.FileName))
            {
                ModelState.AddModelError("FileName", "The filename contains invalid characters.");
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

        mod.Author = await UserManager.FindByNameAsync(username);
        mod.DateCreated = DateTime.Now;
        mod.FileName = archive.FileName;
        
        await using var tx = await DbContext.Database.BeginTransactionAsync();
        
        DbContext.Mods.Add(mod);
        await DbContext.SaveChangesAsync();

        await PutS3FileAsync($"mod_{mod.Id}/archive", archive);
        
        if (image_file != null)
        {
            await PutS3FileAsync($"mod_{mod.Id}/image", image_file);
        }

        await tx.CommitAsync();

        return Redirect($"/mods/{mod.Id}");
    }
    
    [Route("mods/{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var mod = await DbContext.Mods.Include(m => m.Author).FirstOrDefaultAsync(m => m.Id == id);
        
        if (mod == null)
        {
            return NotFound();
        }

        if(mod.Author.UserName != User.Identity?.Name)
        {
            return StatusCode(403);
        }
        
        return View(mod);
    }

    [HttpPost]
    [Route("mods/{id}/edit")]
    public async Task<IActionResult> Edit(int id, [Bind("Name", "Description", "Summary")] Mod form_mod, IFormFile? archive, IFormFile? image_file)
    {
        //These fields are automatically set
        ModelState.Remove("FileName");
        ModelState.Remove("Author");
        ModelState.Remove("DateCreated");
        
        var mod = await DbContext.Mods.Include(m => m.Author).FirstOrDefaultAsync(m => m.Id == id);
        
        if (mod == null)
        {
            return NotFound();
        }
        
        if(mod.Author.UserName != User.Identity?.Name)
        {
            return StatusCode(403);
        }

        if (archive != null)
        {
            if (!IsFilenameValid(archive.FileName))
            {
                ModelState.AddModelError("FileName", "The filename contains invalid characters.");
            }
        }
        
        if(DbContext.Mods.Any(m => m.Name == form_mod.Name && m.Id != id))
        {
            ModelState.AddModelError("Name", $"A mod with the name '{form_mod.Name}' already exists.");
        }

        if(!ModelState.IsValid)
        {
            return View(mod);
        }
        
        await using var tx = await DbContext.Database.BeginTransactionAsync();

        mod.Name = form_mod.Name;
        mod.Description = form_mod.Description;
        mod.Summary = form_mod.Summary;
        mod.FileName = archive?.FileName ?? mod.FileName;
        
        await DbContext.SaveChangesAsync();
        
        if (archive != null)
        {
            await PutS3FileAsync($"mod_{mod.Id}/archive", archive);
        }
        
        if (image_file != null)
        {
            await PutS3FileAsync($"mod_{mod.Id}/image", image_file);
        }
        
        await tx.CommitAsync();
        
        return Redirect($"/mods/{mod.Id}");
    }
}