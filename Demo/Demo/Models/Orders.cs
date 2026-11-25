using System;
using System.Collections.Generic;

namespace ChudoObuv.Models;

public partial class Orders
{
    public int OrderNumber { get; set; }

    public DateOnly OrderDate { get; set; }

    public int UserId { get; set; }
}
