using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace demodemo.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public int? ProductTypeId { get; set; }

    public string ArticleNumber { get; set; } = null!;

    public string? Description { get; set; }

    public string? Image { get; set; }
    public Bitmap ImagePath => new Bitmap(AppDomain.CurrentDomain.BaseDirectory + "/" + Image);

    public int? ProductionPersonCount { get; set; }

    public int? ProductionWorkshopNumber { get; set; }

    public decimal MinCostForAgent { get; set; }

    public virtual ICollection<ProductCostHistory> ProductCostHistories { get; set; } = new List<ProductCostHistory>();

    public virtual ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();

    public virtual ICollection<ProductSale> ProductSales { get; set; } = new List<ProductSale>();

    public virtual ProductType? ProductType { get; set; }
}
