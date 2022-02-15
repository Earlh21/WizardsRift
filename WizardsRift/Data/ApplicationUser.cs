using Microsoft.AspNetCore.Identity;

namespace WizardsRift.Data.Models;

public class ApplicationUser : IdentityUser
{
    public List<Mod> Mods { get; set; }
}