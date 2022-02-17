using Microsoft.AspNetCore.Mvc;

namespace WizardsRift.Controllers;

public class ErrorController : Controller
{
    [Route("404")]
    public IActionResult PageNotFound()
    {
        return View();
    }
    
    [Route("403")]
    public IActionResult Forbidden()
    {
        return View();
    }
    
    [Route("401")]
    public new IActionResult Unauthorized()
    {
        return View();
    }
}