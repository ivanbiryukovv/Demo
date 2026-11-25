using System;
using System.Collections.Generic;

namespace ChudoObuv.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderNumber { get; set; }

    public int ProductId { get; set; }

    public int SizeId { get; set; }

    public short Quantity { get; set; }

    public decimal Price { get; set; }

    public virtual Order OrderNumberNavigation { get; set; } = null!;

    public virtual StockItem StockItem { get; set; } = null!;
}
