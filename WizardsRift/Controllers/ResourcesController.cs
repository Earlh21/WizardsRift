using Microsoft.AspNetCore.Mvc;

namespace WizardsRift.Controllers;

public class ResourcesController : Controller
{
    [Route("resources/images/{image}")]
    public async Task<IActionResult> GetImage(string image)
    {
        byte[] bytes = await System.IO.File.ReadAllBytesAsync($"E:\\resources\\images\\{image}");
        return File(bytes, "image/png");
    }
}