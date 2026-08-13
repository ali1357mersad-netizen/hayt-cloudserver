using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace Hayt
{
    public sealed class CategoryBooksConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Array.Empty<object>();

            object category = values[0];

            if (category == null ||
                ReferenceEquals(category, DependencyProperty.UnsetValue))
            {
                return Array.Empty<object>();
            }

            object booksValue = values[1];

            if (booksValue == null ||
                ReferenceEquals(booksValue, DependencyProperty.UnsetValue) ||
                booksValue is not IEnumerable books)
            {
                return Array.Empty<object>();
            }

            string categoryId = GetPropertyText(category, "Id");
            string categoryTitle = GetPropertyText(category, "Title");

            var result = new List<object>();

            foreach (object book in books)
            {
                if (book == null)
                    continue;

                string bookCategoryId = GetPropertyText(book, "CategoryId");
                string bookCategoryTitle = GetPropertyText(book, "CategoryTitle");
                string bookCategoryPath = GetPropertyText(book, "CategoryPath");

                bool idMatches =
                    !string.IsNullOrWhiteSpace(categoryId) &&
                    !string.IsNullOrWhiteSpace(bookCategoryId) &&
                    string.Equals(categoryId, bookCategoryId, StringComparison.OrdinalIgnoreCase);

                bool titleMatches =
                    !string.IsNullOrWhiteSpace(categoryTitle) &&
                    !string.IsNullOrWhiteSpace(bookCategoryTitle) &&
                    string.Equals(categoryTitle, bookCategoryTitle, StringComparison.OrdinalIgnoreCase);

                bool pathMatches =
                    !string.IsNullOrWhiteSpace(categoryTitle) &&
                    !string.IsNullOrWhiteSpace(bookCategoryPath) &&
                    bookCategoryPath.IndexOf(categoryTitle, StringComparison.OrdinalIgnoreCase) >= 0;

                if (idMatches || titleMatches || pathMatches)
                    result.Add(book);
            }

            return result;
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static string GetPropertyText(
            object source,
            string propertyName)
        {
            PropertyInfo property =
                source.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.IgnoreCase);

            object value = property?.GetValue(source);

            return value?.ToString()?.Trim() ?? string.Empty;
        }
    }
}
