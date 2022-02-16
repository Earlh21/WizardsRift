using Microsoft.AspNetCore.Identity;
using WizardsRift.Models;

namespace WizardsRift.Data;

public class ApplicationUser : IdentityUser
{
    public List<Mod> Mods { get; set; } = new();
}