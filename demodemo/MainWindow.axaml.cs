using Avalonia.Controls;
using Avalonia.Data; 
using Avalonia.Interactivity;
using demodemo.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

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

       
        private List<SortItem> _sortItems = new List<SortItem>
        {
            new SortItem { Name = "Без сортировки", Value = SortCriteria.Default },
            new SortItem { Name = "Наименование (по возрастанию)", Value = SortCriteria.TitleAsc },
            new SortItem { Name = "Наименование (по убыванию)", Value = SortCriteria.TitleDesc },
            new SortItem { Name = "Номер цеха (по возрастанию)", Value = SortCriteria.WorkshopAsc },
            new SortItem { Name = "Номер цеха (по убыванию)", Value = SortCriteria.WorkshopDesc },
            new SortItem { Name = "Мин. стоимость (по возрастанию)", Value = SortCriteria.CostAsc },
            new SortItem { Name = "Мин. стоимость (по убыванию)", Value = SortCriteria.CostDesc }
        };

        public MainWindow()
        {
            InitializeComponent();

            
            SearchBox.TextChanged += OnFilterOrSortChanged;

            SortComboBox.ItemsSource = _sortItems;
            SortComboBox.DisplayMemberBinding = new Binding("Name");
            SortComboBox.SelectedIndex = 0; 
            SortComboBox.SelectionChanged += OnFilterOrSortChanged;

            
            LoadProducts();
        }

        
        public void LoadProducts()
        {
            using (var context = new LopuxDbContext())
            {
                
                string? searchText = SearchBox.Text;
                var sortCriteria = (SortComboBox.SelectedItem as SortItem)?.Value ?? SortCriteria.Default;
                var query = context.Products
                                   .Include(p => p.ProductType)
                                   .AsQueryable();

               
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    string lowercasedSearchText = searchText.ToLower();
                    query = query.Where(p =>
                        (p.Title != null && p.Title.ToLower().Contains(lowercasedSearchText)) ||
                        (p.ArticleNumber != null && p.ArticleNumber.ToLower().Contains(lowercasedSearchText))
                    );
                }

                
                switch (sortCriteria)
                {
                    case SortCriteria.TitleAsc:
                        query = query.OrderBy(p => p.Title);
                        break;
                    case SortCriteria.TitleDesc:
                        query = query.OrderByDescending(p => p.Title);
                        break;
                    case SortCriteria.WorkshopAsc:
                        query = query.OrderBy(p => p.ProductionWorkshopNumber);
                        break;
                    case SortCriteria.WorkshopDesc:
                        query = query.OrderByDescending(p => p.ProductionWorkshopNumber);
                        break;
                    case SortCriteria.CostAsc:
                        query = query.OrderBy(p => p.MinCostForAgent);
                        break;
                    case SortCriteria.CostDesc:
                        query = query.OrderByDescending(p => p.MinCostForAgent);
                        break;
                    default: 
                        query = query.OrderBy(p => p.Id);
                        break;
                }

               
                var totalItems = query.Count();
                _totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
                if (_totalPages == 0) _totalPages = 1;
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                var pagedProducts = query
                    .Skip((_currentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                
                ProductList.ItemsSource = pagedProducts;
                GeneratePageNumbers();
                UpdatePageButtons();
            }
        }

        
        private void OnFilterOrSortChanged(object? sender, EventArgs e)
        {
          
            _currentPage = 1;
            LoadProducts();
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

        // Обработчики пагинации теперь вызывают LoadProducts без параметров
        private void PageNumber_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int page)
            {
                _currentPage = page;
                LoadProducts();
            }
        }

        private void PrevButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadProducts();
            }
        }

        private void NextButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadProducts();
            }
        }
    }
}