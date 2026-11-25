using System;
using System.Collections.Generic;

namespace ChudoObuv.Models;

public partial class User
{
    public int UserId { get; set; }

    public string LastName { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public string Login { get; set; } = null!;

    public int RoleId { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role Role { get; set; } = null!;

    public string FullName => $"{LastName} {Name} {Surname}".Trim();

    public string FullNameWithRole => $"{LastName} {Name} {Surname} ({Login})".Trim();
}
