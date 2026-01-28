using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using demodemo.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace demodemo
{
    public partial class ProductEditWindow : Window
    {
        private readonly LopuxDbContext _context = new LopuxDbContext();
        // _product используется для хранения начальных данных (особенно Id при редактировании)
        private readonly Product _product;
        private readonly bool _isNewProduct;
        private string? _newImagePath;
        // _productMaterials - это список материалов, который видит пользователь в UI
        private List<ProductMaterial> _productMaterials = new List<ProductMaterial>();

        public ProductEditWindow()
        {
            InitializeComponent();
            _isNewProduct = true;
            _product = new Product();
            Title = "Добавление продукции";
            DeleteButton.IsVisible = false;
        }

        public ProductEditWindow(Product product)
        {
            InitializeComponent();
            _isNewProduct = false;
            // Важно: 'product' здесь - это объект из другого DbContext (из главного окна).
            // Мы не будем пытаться обновить его напрямую. Мы используем его только для получения Id и начальных данных.
            _product = product;
            Title = "Редактирование продукции";
            DeleteButton.IsVisible = true;
            LoadProductData();
        }

        private async void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            ProductTypeComboBox.ItemsSource = await _context.ProductTypes.ToListAsync();
            ProductTypeComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Title");

            AddMaterialComboBox.ItemsSource = await _context.Materials.ToListAsync();
            AddMaterialComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("Title");

            if (!_isNewProduct && _product.ProductType != null)
            {
                // Находим и устанавливаем элемент ComboBox по Id, чтобы избежать проблем с отслеживанием
                ProductTypeComboBox.SelectedItem = _context.ProductTypes.Local.FirstOrDefault(pt => pt.Id == _product.ProductTypeId);
            }
        }

        private void LoadProductData()
        {
            ArticleTextBox.Text = _product.ArticleNumber;
            TitleTextBox.Text = _product.Title;
            PersonCountTextBox.Text = _product.ProductionPersonCount?.ToString();
            WorkshopNumberTextBox.Text = _product.ProductionWorkshopNumber?.ToString();
            MinCostTextBox.Text = _product.MinCostForAgent.ToString("F2");
            DescriptionTextBox.Text = _product.Description;

            try
            {
                if (!string.IsNullOrEmpty(_product.Image) && File.Exists(_product.Image))
                {
                    ProductImage.Source = new Bitmap(_product.Image);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
            }

            _productMaterials = _context.ProductMaterials
                .Include(pm => pm.Material)
                .Where(pm => pm.ProductId == _product.Id)
                .ToList();

            UpdateMaterialsList();
        }

        private void UpdateMaterialsList()
        {
            MaterialsListBox.ItemsSource = null;
            MaterialsListBox.ItemsSource = _productMaterials;
        }

        private async void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            // --- 1. Валидация данных ---
            if (string.IsNullOrWhiteSpace(ArticleTextBox.Text) || string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                await CustomMessageBox.ShowAsync(this, "Ошибка", "Артикул и наименование должны быть заполнены.", MessageBoxButtons.Ok);
                return;
            }

            if (!decimal.TryParse(MinCostTextBox.Text, out var minCost) || minCost < 0)
            {
                await CustomMessageBox.ShowAsync(this, "Ошибка", "Стоимость введена некорректно или является отрицательной.", MessageBoxButtons.Ok);
                return;
            }

            // --- 2. Проверка на уникальность артикула ---
            // Используем AsNoTracking, чтобы эта проверка не начала отслеживать сущность и не вызвала конфликт
            var productWithSameArticle = await _context.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ArticleNumber == ArticleTextBox.Text);

            if (productWithSameArticle != null && productWithSameArticle.Id != _product.Id)
            {
                await CustomMessageBox.ShowAsync(this, "Ошибка", "Продукт с таким артикулом уже существует.", MessageBoxButtons.Ok);
                return;
            }

            // --- 3. Сохранение основной информации о продукте (Добавление или Обновление) ---
            Product productToSave;
            if (_isNewProduct)
            {
                // Для нового продукта создаем новый объект
                productToSave = new Product();
                _context.Products.Add(productToSave);
            }
            else
            {
                // Для существующего - НАХОДИМ его в базе в ТЕКУЩЕМ контексте.
                // Это ключевое изменение для исправления ошибки.
                productToSave = await _context.Products.FindAsync(_product.Id);
                if (productToSave == null)
                {
                    await CustomMessageBox.ShowAsync(this, "Критическая ошибка", "Продукт для редактирования не найден в базе.", MessageBoxButtons.Ok);
                    return;
                }
            }

            // Обновляем свойства найденного (или нового) объекта из полей UI
            productToSave.ArticleNumber = ArticleTextBox.Text;
            productToSave.Title = TitleTextBox.Text;
            productToSave.ProductTypeId = (ProductTypeComboBox.SelectedItem as ProductType)?.Id;
            productToSave.MinCostForAgent = minCost;
            productToSave.Description = DescriptionTextBox.Text;

            if (int.TryParse(PersonCountTextBox.Text, out var personCount) && personCount > 0)
                productToSave.ProductionPersonCount = personCount;
            else
                productToSave.ProductionPersonCount = null;

            if (int.TryParse(WorkshopNumberTextBox.Text, out var workshopNumber) && workshopNumber > 0)
                productToSave.ProductionWorkshopNumber = workshopNumber;
            else
                productToSave.ProductionWorkshopNumber = null;

            if (!string.IsNullOrWhiteSpace(_newImagePath))
            {
                productToSave.Image = _newImagePath;
            }

            // Сохраняем изменения продукта (INSERT или UPDATE)
            await _context.SaveChangesAsync();

            // --- 4. Обновление материалов (более эффективный способ) ---
            var productId = productToSave.Id;
            var existingMaterialsInDb = await _context.ProductMaterials
                .Where(pm => pm.ProductId == productId).ToListAsync();

            // 4.1. Находим материалы для удаления
            var materialsToDelete = existingMaterialsInDb
                .Where(db_pm => !_productMaterials.Any(ui_pm => ui_pm.MaterialId == db_pm.MaterialId))
                .ToList();
            if (materialsToDelete.Any())
                _context.ProductMaterials.RemoveRange(materialsToDelete);

            // 4.2. Находим материалы для добавления или обновления
            foreach (var uiMaterial in _productMaterials)
            {
                var existingDbMaterial = existingMaterialsInDb.FirstOrDefault(db_pm => db_pm.MaterialId == uiMaterial.MaterialId);
                if (existingDbMaterial == null)
                {
                    // Добавляем новый материал
                    _context.ProductMaterials.Add(new ProductMaterial
                    {
                        ProductId = productId,
                        MaterialId = uiMaterial.MaterialId,
                        Count = uiMaterial.Count
                    });
                }
                else
                {
                    // Обновляем количество у существующего
                    existingDbMaterial.Count = uiMaterial.Count;
                }
            }

            // Сохраняем все изменения по материалам за один раз
            await _context.SaveChangesAsync();

            this.Close(true); // Закрываем окно после успешного сохранения
        }

        private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            var result = await CustomMessageBox.ShowAsync(this, "Подтверждение", "Вы уверены, что хотите удалить этот продукт?", MessageBoxButtons.YesNo);
            if (result != MessageBoxResult.Yes) return;

            // Проверяем, есть ли по продукту история продаж
            if (await _context.ProductSales.AnyAsync(ps => ps.ProductId == _product.Id))
            {
                await CustomMessageBox.ShowAsync(this, "Ошибка", "Невозможно удалить продукт, так как по нему есть история продаж.", MessageBoxButtons.Ok);
                return;
            }

            // Находим продукт в текущем контексте, чтобы удаление было безопасным
            var productToDelete = await _context.Products.FindAsync(_product.Id);
            if (productToDelete != null)
            {
                // Сначала удаляем связанные материалы
                var materialsToRemove = await _context.ProductMaterials.Where(pm => pm.ProductId == productToDelete.Id).ToListAsync();
                if (materialsToRemove.Any())
                    _context.ProductMaterials.RemoveRange(materialsToRemove);

                // Затем удаляем сам продукт
                _context.Products.Remove(productToDelete);

                await _context.SaveChangesAsync();
                this.Close(true);
            }
            else
            {
                await CustomMessageBox.ShowAsync(this, "Ошибка", "Продукт не найден в базе данных.", MessageBoxButtons.Ok);
            }
        }

        // Остальные методы без изменений
        private async void ChangeImageButton_Click(object? sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите изображение",
                Filters = new List<FileDialogFilter> { new FileDialogFilter { Name = "Image Files", Extensions = { "jpg", "jpeg", "png", "bmp" } } }
            };

            var result = await openFileDialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                string sourcePath = result[0];
                string fileName = Path.GetFileName(sourcePath);
                string destPath = Path.Combine(Directory.GetCurrentDirectory(), "products", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(sourcePath, destPath, true);

                _newImagePath = "products\\" + fileName;
                ProductImage.Source = new Bitmap(destPath);
            }
        }

        private async void AddMaterialButton_Click(object? sender, RoutedEventArgs e)
        {
            if (AddMaterialComboBox.SelectedItem is Material selectedMaterial &&
                int.TryParse(AddMaterialCountTextBox.Text, out int count) && count > 0)
            {
                var existingMaterial = _productMaterials.FirstOrDefault(pm => pm.MaterialId == selectedMaterial.Id);
                if (existingMaterial != null)
                {
                    existingMaterial.Count += count;
                }
                else
                {
                    _productMaterials.Add(new ProductMaterial
                    {
                        // ProductId здесь не важен, т.к. мы ориентируемся на MaterialId
                        // При сохранении ProductId будет взят из основной сущности
                        Material = selectedMaterial,
                        MaterialId = selectedMaterial.Id,
                        Count = count
                    });
                }
                UpdateMaterialsList();
                AddMaterialCountTextBox.Clear();
            }
            else
            {
                await CustomMessageBox.ShowAsync(this, "Ошибка", "Выберите материал и укажите корректное количество.", MessageBoxButtons.Ok);
            }
        }

        private void DeleteMaterialButton_Click(object? sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ProductMaterial materialToRemove)
            {
                _productMaterials.Remove(materialToRemove);
                UpdateMaterialsList();
            }
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            Window_Loaded(this, e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _context.Dispose();
            base.OnClosed(e);
        }
    }
}