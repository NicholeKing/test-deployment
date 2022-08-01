#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations;
namespace beltReview.Models;
public class Song
{
    [Key]
    public int SongId {get;set;}
    [Required]
    [MinLength(2)]
    public string Title {get;set;}
    [Required]
    [Range(0, 59)]
    public int DurMinutes {get;set;}
    [Required]
    [Range(0, 59)]
    public int DurSeconds {get;set;}
    [Required]
    public string Genre {get;set;}
    public int UserId {get;set;}
    public User? Artist {get;set;}
    public List<Like> UsersWhoLiked {get;set;} = new List<Like>();
    public DateTime CreatedAt {get;set;} = DateTime.Now;
    public DateTime UpdatedAt {get;set;} = DateTime.Now;
}