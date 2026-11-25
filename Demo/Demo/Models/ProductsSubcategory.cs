using System;
using System.Collections.Generic;

namespace ChudoObuv.Models;

public partial class ProductsSubcategory
{
    public int SubcategoryId { get; set; }

    public string? SubcategoryName { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
