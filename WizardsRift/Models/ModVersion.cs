using System.ComponentModel.DataAnnotations;

namespace WizardsRift.Data.Models;

public class ModVersion
{
    [Key]
    public int Id { get; set; }
    
    public Mod Mod { get; set; }
    
    public string Version { get; set; }
    public string Changes { get; set; }
    public DateTime Date { get; set; }
}