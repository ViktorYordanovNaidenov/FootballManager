using System;
using System.Drawing;
using System.Windows.Forms;

namespace FootballManager
{
    public static class Theme
    {
        // ЦВЕТОВА ПАЛИТРА (Modern Dark Football Style)
        public static Color PrimaryColor = Color.FromArgb(52, 152, 219);   // Синьо (за акценти)
        public static Color SecondaryColor = Color.FromArgb(46, 204, 113); // Зелено (за Save бутони)
        public static Color DangerColor = Color.FromArgb(231, 76, 60);     // Червено (за Delete/Cancel)
        public static Color BackgroundColor = Color.FromArgb(44, 62, 80);  // Тъмно синьо-сиво (Фон)
        public static Color PanelColor = Color.FromArgb(52, 73, 94);       // По-светло сиво (за панели/кутийки)
        public static Color TextColor = Color.WhiteSmoke;                // Бял текст

        public static void Apply(Form form)
        {
            form.BackColor = BackgroundColor;
            form.ForeColor = TextColor;
            form.FormBorderStyle = FormBorderStyle.FixedSingle; // Модерен рамка
            form.MaximizeBox = false;

            foreach (Control c in form.Controls)
            {
                ApplyToControl(c);
            }
        }

        private static void ApplyToControl(Control c)
        {
            // 1. СТИЛИЗИРАНЕ НА БУТОНИ
            if (c is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Cursor = Cursors.Hand;
                btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                btn.ForeColor = Color.White;

                // Автоматично избираме цвят според текста
                if (btn.Text.ToUpper().Contains("ИЗТРИЙ") || btn.Text.ToUpper().Contains("МАХНИ") || btn.Text == "X")
                    btn.BackColor = DangerColor;
                else if (btn.Text.ToUpper().Contains("ЗАПИШИ") || btn.Text.ToUpper().Contains("СЪЗДАЙ") || btn.Text.ToUpper().Contains("ДОБАВИ") || btn.Text.ToUpper().Contains("ГОЛ"))
                    btn.BackColor = SecondaryColor;
                else
                    btn.BackColor = PrimaryColor; // Стандартен бутон
            }

            // 2. СТИЛИЗИРАНЕ НА ЕТИКЕТИ (LABELS)
            if (c is Label lbl)
            {
                lbl.ForeColor = TextColor;
                lbl.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                // Ако е заглавие (голям шрифт), го правим удебелен
                if (lbl.Font.Size > 12) lbl.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            }

            // 3. СТИЛИЗИРАНЕ НА ТАБЛИЦИ (GRID) - Най-важното!
            if (c is DataGridView grid)
            {
                grid.BackgroundColor = PanelColor;
                grid.BorderStyle = BorderStyle.None;

                // Хедър (Заглавния ред)
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = PrimaryColor;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.ColumnHeadersHeight = 35;

                // Редове
                grid.DefaultCellStyle.BackColor = PanelColor;
                grid.DefaultCellStyle.ForeColor = Color.White;
                grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
                grid.DefaultCellStyle.SelectionBackColor = SecondaryColor; // Зелено при клик
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                grid.RowTemplate.Height = 30;

                // Махаме грозните рамки
                grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                grid.GridColor = Color.Gray;
                grid.RowHeadersVisible = false; // Махаме лявата лента
            }

            // 4. ТЕКСТОВИ ПОЛЕТА И ПАДАЩИ МЕНЮТА
            if (c is TextBox txt)
            {
                txt.BackColor = Color.White;
                txt.ForeColor = Color.Black;
                txt.Font = new Font("Segoe UI", 10);
                txt.BorderStyle = BorderStyle.FixedSingle;
            }
            if (c is ComboBox combo)
            {
                combo.BackColor = Color.White;
                combo.Font = new Font("Segoe UI", 10);
                combo.FlatStyle = FlatStyle.Flat;
            }

            // Рекурсия (ако има панели вътре във формата)
            if (c.HasChildren)
            {
                foreach (Control child in c.Controls)
                {
                    ApplyToControl(child);
                }
            }
        }
    }
}