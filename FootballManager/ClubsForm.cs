using System;
using System.Data;
using System.Data.SqlClient; // За SQL връзката
using System.Drawing;
using System.Windows.Forms;

namespace FootballManager
{
    public partial class ClubsForm : Form
    {
        // Контроли
        private DataGridView gridClubs;
        private TextBox txtName, txtCity, txtStadium, txtYear;
        private Button btnSave, btnDelete, btnClear;

        // Променлива, за да знаем дали добавяме НОВ или РЕДАКТИРАМЕ стар
        private int selectedClubId = 0;

        public ClubsForm()
        {
            InitializeComponent();
            SetupUI();    // Рисуваме екрана
            LoadClubs();  // Зареждаме данните от SQL
            Theme.Apply(this);
        }

        // 1. Рисуване на интерфейса (за да не губим време с мишката)
        private void SetupUI()
        {
            this.Text = "Управление на Клубове";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Етикети и Полета
            Label lblName = new Label() { Text = "Име на Клуб:", Location = new Point(20, 20), AutoSize = true };
            txtName = new TextBox() { Location = new Point(20, 45), Width = 200 };

            Label lblCity = new Label() { Text = "Град:", Location = new Point(240, 20), AutoSize = true };
            txtCity = new TextBox() { Location = new Point(240, 45), Width = 150 };

            Label lblStadium = new Label() { Text = "Стадион:", Location = new Point(410, 20), AutoSize = true };
            txtStadium = new TextBox() { Location = new Point(410, 45), Width = 150 };

            Label lblYear = new Label() { Text = "Година:", Location = new Point(580, 20), AutoSize = true };
            txtYear = new TextBox() { Location = new Point(580, 45), Width = 80 };

            // Бутони
            btnSave = new Button() { Text = "ЗАПИШИ", Location = new Point(680, 43), Width = 80, BackColor = Color.LightGreen };
            btnSave.Click += BtnSave_Click;

            btnDelete = new Button() { Text = "ИЗТРИЙ", Location = new Point(20, 80), Width = 80, BackColor = Color.Salmon, Enabled = false };
            btnDelete.Click += BtnDelete_Click;

            btnClear = new Button() { Text = "Изчисти", Location = new Point(110, 80), Width = 80 };
            btnClear.Click += (s, e) => ClearForm();

            // Таблица (Grid)
            // В SetupUI() промени дефиницията на gridClubs:
            gridClubs = new DataGridView()
            {
                Location = new Point(20, 120),
                Size = new Size(740, 320),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false // <--- ТОВА Е НОВОТО (Маха празния ред отдолу)
            };
            gridClubs.CellClick += GridClubs_CellClick; // Събитие при цъкане на ред

            // Добавяне към формата
            this.Controls.Add(lblName); this.Controls.Add(txtName);
            this.Controls.Add(lblCity); this.Controls.Add(txtCity);
            this.Controls.Add(lblStadium); this.Controls.Add(txtStadium);
            this.Controls.Add(lblYear); this.Controls.Add(txtYear);
            this.Controls.Add(btnSave); this.Controls.Add(btnDelete); this.Controls.Add(btnClear);
            this.Controls.Add(gridClubs);
        }

        // 2. Зареждане на данните от базата (SELECT)
        private void LoadClubs()
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Clubs", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gridClubs.DataSource = dt;
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
        }

        // 3. Записване (INSERT или UPDATE)
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Името е задължително!"); return; }

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;

                    if (selectedClubId == 0) // НОВ ЗАПИС
                    {
                        cmd.CommandText = "INSERT INTO Clubs (Name, City, Stadium, FoundedYear) VALUES (@n, @c, @s, @y)";
                    }
                    else // РЕДАКЦИЯ НА СТАР
                    {
                        cmd.CommandText = "UPDATE Clubs SET Name=@n, City=@c, Stadium=@s, FoundedYear=@y WHERE ClubId=@id";
                        cmd.Parameters.AddWithValue("@id", selectedClubId);
                    }

                    // Параметри (за защита от хакери)
                    cmd.Parameters.AddWithValue("@n", txtName.Text);
                    cmd.Parameters.AddWithValue("@c", txtCity.Text);
                    cmd.Parameters.AddWithValue("@s", txtStadium.Text);

                    // Валидация за годината (да е число)
                    int year = 0; int.TryParse(txtYear.Text, out year);
                    cmd.Parameters.AddWithValue("@y", year == 0 ? (object)DBNull.Value : year);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Успешен запис!");
                    LoadClubs(); // Презареждаме таблицата
                    ClearForm();
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
        }

        // 4. Изтриване (DELETE)
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedClubId == 0) return;

            if (MessageBox.Show("Сигурен ли си?", "Изтриване", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = Db.GetConnection())
                    {
                        // Първо трием връзките (ако има), но за сега директно клуба
                        SqlCommand cmd = new SqlCommand("DELETE FROM Clubs WHERE ClubId = @id", conn);
                        cmd.Parameters.AddWithValue("@id", selectedClubId);
                        cmd.ExecuteNonQuery();

                        LoadClubs();
                        ClearForm();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Не може да изтриеш клуб, който вече има играчи или мачове!\n" + ex.Message); }
            }
        }

        // 5. При клик върху ред -> попълваме полетата
        private void GridClubs_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = gridClubs.Rows[e.RowIndex];
                selectedClubId = Convert.ToInt32(row.Cells["ClubId"].Value);

                txtName.Text = row.Cells["Name"].Value.ToString();
                txtCity.Text = row.Cells["City"].Value.ToString();
                txtStadium.Text = row.Cells["Stadium"].Value.ToString();
                txtYear.Text = row.Cells["FoundedYear"].Value.ToString();

                btnDelete.Enabled = true;
                btnSave.Text = "ПРОМЕНИ"; // Сменяме текста на бутона
                btnSave.BackColor = Color.Yellow;
            }
        }

        private void ClearForm()
        {
            txtName.Clear(); txtCity.Clear(); txtStadium.Clear(); txtYear.Clear();
            selectedClubId = 0;
            btnSave.Text = "ЗАПИШИ";
            btnSave.BackColor = Color.LightGreen;
            btnDelete.Enabled = false;
        }
    }
}