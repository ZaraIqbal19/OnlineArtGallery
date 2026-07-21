using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Art_Gallery.Areas.Identity.Data;

// Add profile data for application users by adding properties to the Art_GalleryUser class
public class Art_GalleryUser : IdentityUser
{
    public string gender { get; set; }

    public string address { get; set; }
    public int age { get; set; }

    public string Role { get; set; } = "User";

}

