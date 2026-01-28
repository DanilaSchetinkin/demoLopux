using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using demodemo.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace demodemo
{
    public enum SortCriteria
    {
        Default, TitleAsc, TitleDesc, WorkshopAsc, WorkshopDesc, CostAsc, CostDesc
    }

    public class SortItem
    {
        public string Name { get; set; }
        public SortCriteria Value { get; set; }
    }

    public partial class MainWindow : Window
    {
        private int _currentPage = 1;
        private const int PageSize = 20;
        private int _totalPages;

        private readonly List<SortItem> _sortItems = new List<SortItem>
        {
            new SortItem { Name = "По умолчанию", Value = SortCriteria.Default },
            new SortItem { Name = "Наименование (по возрастанию)", Value = SortCriteria.TitleAsc },
            new SortItem { Name = "Наименование (по убыванию)", Value = SortCriteria.TitleDesc },
            new SortItem { Name = "Номер цеха (по возрастанию)", Value = SortCriteria.WorkshopAsc },
            new SortItem { Name = "Номер цеха (по убыванию)", Value = SortCriteria.WorkshopDesc },
            new SortItem { Name = "Стоимость (по возрастанию)", Value = SortCriteria.CostAsc },
            new SortItem { Name = "Стоимость (по убыванию)", Value = SortCriteria.CostDesc }
        };

        public MainWindow()
        {
            InitializeComponent();
            // Метод OnLoaded вызовется автоматически, когда окно будет готово
        }

        // Переименовали InitializeControls в OnLoaded, чтобы следовать стандартным практикам Avalonia
        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            SearchBox.TextChanged += OnFilterOrSortChanged;

            SortComboBox.ItemsSource = _sortItems;
            SortComboBox.DisplayMemberBinding = new Binding("Name");
            SortComboBox.SelectedIndex = 0;
            SortComboBox.SelectionChanged += OnFilterOrSortChanged;

            using (var context = new LopuxDbContext())
            {
                var allTypesItem = new ProductType { Id = 0, Title = "Все типы" };
                var productTypes = new List<ProductType> { allTypesItem };
                productTypes.AddRange(await context.ProductTypes.OrderBy(pt => pt.Title).ToListAsync());

                FilterComboBox.ItemsSource = productTypes;
                FilterComboBox.DisplayMemberBinding = new Binding("Title");
                FilterComboBox.SelectedIndex = 0;
            }
            FilterComboBox.SelectionChanged += OnFilterOrSortChanged;

            await LoadProductsAsync();
        }

        public async Task LoadProductsAsync()
        {
            await using (var context = new LopuxDbContext())
            {
                string? searchText = SearchBox.Text;
                var sortCriteria = (SortComboBox.SelectedItem as SortItem)?.Value ?? SortCriteria.Default;
                var selectedProductType = FilterComboBox.SelectedItem as ProductType;

                var query = context.Products
                    .Include(p => p.ProductType)
                    .Include(p => p.ProductMaterials)
                    .ThenInclude(pm => pm.Material)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    string lowercasedSearchText = searchText.ToLower();
                    query = query.Where(p =>
                        (p.Title != null && p.Title.ToLower().Contains(lowercasedSearchText)) ||
                        (p.ArticleNumber != null && p.ArticleNumber.ToLower().Contains(lowercasedSearchText))
                    );
                }

                if (selectedProductType != null && selectedProductType.Id != 0)
                {
                    query = query.Where(p => p.ProductTypeId == selectedProductType.Id);
                }

                // --- ГЛАВНОЕ ИСПРАВЛЕНИЕ ЗДЕСЬ ---
                // Добавили обработку NULL-значений, чтобы избежать вылета
                query = sortCriteria switch
                {
                    SortCriteria.TitleAsc => query.OrderBy(p => p.Title ?? ""),
                    SortCriteria.TitleDesc => query.OrderByDescending(p => p.Title ?? ""),
                    SortCriteria.WorkshopAsc => query.OrderBy(p => p.ProductionWorkshopNumber ?? int.MaxValue),
                    SortCriteria.WorkshopDesc => query.OrderByDescending(p => p.ProductionWorkshopNumber ?? int.MaxValue),
                    SortCriteria.CostAsc => query.OrderBy(p => p.MinCostForAgent), // Стоимость обычно не бывает null
                    SortCriteria.CostDesc => query.OrderByDescending(p => p.MinCostForAgent),
                    _ => query.OrderBy(p => p.Id),
                };

                var totalItems = await query.CountAsync();
                _totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
                if (_totalPages == 0) _totalPages = 1;
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                var pagedProducts = await query
                    .Skip((_currentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                ProductList.ItemsSource = pagedProducts;
                GeneratePageNumbers();
                UpdatePageButtons();
            }
        }

        private async void OnFilterOrSortChanged(object? sender, EventArgs e)
        {
            _currentPage = 1;
            await LoadProductsAsync();
        }

        private async void AddProductButton_Click(object? sender, RoutedEventArgs e)
        {
            var editWindow = new ProductEditWindow();
            var result = await editWindow.ShowDialog<bool>(this);

            if (result)
            {
                await LoadProductsAsync();
            }
        }

        private async void ProductList_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (ProductList.SelectedItem is Product selectedProduct)
            {
                var editWindow = new ProductEditWindow(selectedProduct);
                var result = await editWindow.ShowDialog<bool>(this);

                if (result)
                {
                    await LoadProductsAsync();
                }
            }
        }

        private void GeneratePageNumbers()
        {
            PageNumbersPanel.Children.Clear();
            for (int i = 1; i <= _totalPages; i++)
            {
                var button = new Button
                {
                    Content = i.ToString(),
                    Tag = i,
                    IsEnabled = (i != _currentPage)
                };
                button.Click += PageNumber_Click;
                PageNumbersPanel.Children.Add(button);
            }
        }

        private void UpdatePageButtons()
        {
            PrevButton.IsEnabled = _currentPage > 1;
            NextButton.IsEnabled = _currentPage < _totalPages;
        }

        private async void PageNumber_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int page)
            {
                _currentPage = page;
                await LoadProductsAsync();
            }
        }

        private async void PrevButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadProductsAsync();
            }
        }

        private async void NextButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadProductsAsync();
            }
        }
    }
}