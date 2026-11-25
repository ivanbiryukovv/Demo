using System;
using System.Collections.Generic;

namespace ChudoObuv.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public int CategoryId { get; set; }

    public int SubcategoryId { get; set; }

    public string? Picture { get; set; }

    public string ProductName { get; set; } = null!;

    public int ManufacturerId { get; set; }

    public string Description { get; set; } = null!;

    public string Compound { get; set; } = null!;

    public decimal Price { get; set; }

    public virtual ProductCategory Category { get; set; } = null!;

    public virtual Manufacturer Manufacturer { get; set; } = null!;

    public virtual ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();

    public virtual ProductsSubcategory Subcategory { get; set; } = null!;
}
