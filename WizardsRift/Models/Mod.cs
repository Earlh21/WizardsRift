using System.ComponentModel.DataAnnotations;

namespace WizardsRift.Data.Models;

public class Mod
{
    [Key]
    public int Id { get; set; }
    
    public ApplicationUser Author { get; set; }
    
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime DateCreated { get; set; }
    public string DownloadLink { get; set; }
    
    public List<ModVersion> VersionHistory { get; set; }
    public List<Mod> Dependencies { get; set; }
}