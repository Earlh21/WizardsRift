using System.ComponentModel.DataAnnotations;
using WizardsRift.Data;

namespace WizardsRift.Models;

public class Mod
{
    [Key]
    public int Id { get; set; }
    
    public ApplicationUser Author { get; set; }
    
    [StringLength(120, MinimumLength = 3)]
    public string Name { get; set; }
    
    
    [StringLength(4000, MinimumLength = 3)]
    public string Description { get; set; }
    
    [StringLength(300)]
    public string? Summary { get; set; }
    
    public DateTime DateCreated { get; set; }
    
    public string FileName { get; set; }


    public int DownloadCount { get; set; } = 0;
}