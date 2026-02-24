using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace FootballManager
{
    public partial class PlayersForm : Form
    {
        // Контроли
        private DataGridView gridPlayers;
        private TextBox txtFirstName, txtLastName, txtNation, txtKitNum;
        private ComboBox comboPosition, comboClub;
        private DateTimePicker dateBirth;
        private CheckBox chkActive;
        private Button btnSave, btnDelete, btnClear;

        private int selectedPlayerId = 0;

        public PlayersForm()
        {
            InitializeComponent();
            SetupUI();
            LoadClubsIntoCombo(); // Зареждаме клубовете в падащото меню
            LoadPlayers();        // Зареждаме играчите в таблицата
            Theme.Apply(this);
        }

        private void SetupUI()
        {
            this.Text = "Картотека на Футболисти";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            int y = 20;
            // Име и Фамилия
            Label lblFirst = new Label() { Text = "Име:", Location = new Point(20, y), AutoSize = true };
            txtFirstName = new TextBox() { Location = new Point(20, y + 25), Width = 150 };

            Label lblLast = new Label() { Text = "Фамилия:", Location = new Point(180, y), AutoSize = true };
            txtLastName = new TextBox() { Location = new Point(180, y + 25), Width = 150 };

            // Позиция (Падащо меню)
            Label lblPos = new Label() { Text = "Позиция:", Location = new Point(340, y), AutoSize = true };
            comboPosition = new ComboBox() { Location = new Point(340, y + 25), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            comboPosition.Items.AddRange(new string[] { "GK", "DF", "MF", "FW" }); // GK=Вратар, DF=Защитник...

            // Националност
            Label lblNat = new Label() { Text = "Националност:", Location = new Point(450, y), AutoSize = true };
            txtNation = new TextBox() { Location = new Point(450, y + 25), Width = 100 };

            // Клуб (Падащо меню - ще се пълни от базата)
            Label lblClub = new Label() { Text = "Клуб:", Location = new Point(560, y), AutoSize = true };
            comboClub = new ComboBox() { Location = new Point(560, y + 25), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };

            // Номер на фланелка
            Label lblKit = new Label() { Text = "Номер:", Location = new Point(720, y), AutoSize = true };
            txtKitNum = new TextBox() { Location = new Point(720, y + 25), Width = 50 };

            y += 60;
            // Дата на раждане
            Label lblDob = new Label() { Text = "Рождена дата:", Location = new Point(20, y), AutoSize = true };
            dateBirth = new DateTimePicker() { Location = new Point(20, y + 25), Width = 200, Format = DateTimePickerFormat.Short };

            // Активен ли е?
            chkActive = new CheckBox() { Text = "Активен състезател", Location = new Point(240, y + 25), Checked = true, AutoSize = true };

            // Бутони
            btnSave = new Button() { Text = "ЗАПИШИ", Location = new Point(600, y + 20), Width = 100, Height = 35, BackColor = Color.LightGreen };
            btnSave.Click += BtnSave_Click;

            btnDelete = new Button() { Text = "ИЗТРИЙ", Location = new Point(710, y + 20), Width = 100, Height = 35, BackColor = Color.Salmon, Enabled = false };
            btnDelete.Click += BtnDelete_Click;

            btnClear = new Button() { Text = "Изчисти", Location = new Point(820, y + 20), Width = 60, Height = 35 };
            btnClear.Click += (s, e) => ClearForm();

            // Таблица
            gridPlayers = new DataGridView()
            {
                Location = new Point(20, 160),
                Size = new Size(840, 380),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false // Важно, за да не гърми
            };
            gridPlayers.CellClick += GridPlayers_CellClick;

            // Добавяне на контролите
            this.Controls.Add(lblFirst); this.Controls.Add(txtFirstName);
            this.Controls.Add(lblLast); this.Controls.Add(txtLastName);
            this.Controls.Add(lblPos); this.Controls.Add(comboPosition);
            this.Controls.Add(lblNat); this.Controls.Add(txtNation);
            this.Controls.Add(lblClub); this.Controls.Add(comboClub);
            this.Controls.Add(lblKit); this.Controls.Add(txtKitNum);
            this.Controls.Add(lblDob); this.Controls.Add(dateBirth);
            this.Controls.Add(chkActive);
            this.Controls.Add(btnSave); this.Controls.Add(btnDelete); this.Controls.Add(btnClear);
            this.Controls.Add(gridPlayers);
        }

        // Зареждаме списъка с клубове за падащото меню
        private void LoadClubsIntoCombo()
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT ClubId, Name FROM Clubs ORDER BY Name", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Добавяме празен ред "Без клуб" (Свободен агент)
                    DataRow row = dt.NewRow();
                    row["ClubId"] = DBNull.Value;
                    row["Name"] = "-- Свободен агент --";
                    dt.Rows.InsertAt(row, 0);

                    comboClub.DataSource = dt;
                    comboClub.DisplayMember = "Name";  // Какво вижда потребителят
                    comboClub.ValueMember = "ClubId";  // Какво записваме в базата (ID-то)
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка при зареждане на клубове: " + ex.Message); }
        }

        private void LoadPlayers()
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    // Правим JOIN, за да вземем ИМЕТО на клуба, а не само ID-то му
                    string sql = @"
                        SELECT p.PlayerId, p.FirstName, p.LastName, p.Position, 
                               c.Name as ClubName, p.Nationality, p.BirthDate, p.KitNumber, p.IsActive, p.CurrentClubId
                        FROM Players p
                        LEFT JOIN Clubs c ON p.CurrentClubId = c.ClubId
                        ORDER BY p.LastName";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gridPlayers.DataSource = dt;

                    // Скриваме ID колоните, които не са за потребителя
                    gridPlayers.Columns["PlayerId"].Visible = false;
                    gridPlayers.Columns["CurrentClubId"].Visible = false;
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Име и Фамилия са задължителни!"); return;
            }
            if (comboPosition.SelectedItem == null)
            {
                MessageBox.Show("Избери позиция!"); return;
            }

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;

                    if (selectedPlayerId == 0)
                        cmd.CommandText = "INSERT INTO Players (FirstName, LastName, Position, Nationality, BirthDate, CurrentClubId, KitNumber, IsActive) VALUES (@fn, @ln, @pos, @nat, @bd, @cid, @kit, @act)";
                    else
                    {
                        cmd.CommandText = "UPDATE Players SET FirstName=@fn, LastName=@ln, Position=@pos, Nationality=@nat, BirthDate=@bd, CurrentClubId=@cid, KitNumber=@kit, IsActive=@act WHERE PlayerId=@id";
                        cmd.Parameters.AddWithValue("@id", selectedPlayerId);
                    }

                    cmd.Parameters.AddWithValue("@fn", txtFirstName.Text);
                    cmd.Parameters.AddWithValue("@ln", txtLastName.Text);
                    cmd.Parameters.AddWithValue("@pos", comboPosition.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@nat", txtNation.Text);
                    cmd.Parameters.AddWithValue("@bd", dateBirth.Value);

                    // Взимаме ID-то на избрания клуб
                    if (comboClub.SelectedValue == DBNull.Value || comboClub.SelectedValue == null)
                        cmd.Parameters.AddWithValue("@cid", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@cid", comboClub.SelectedValue);

                    int kit = 0; int.TryParse(txtKitNum.Text, out kit);
                    cmd.Parameters.AddWithValue("@kit", kit);
                    cmd.Parameters.AddWithValue("@act", chkActive.Checked);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Играчът е записан!");
                    LoadPlayers();
                    ClearForm();
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedPlayerId == 0) return;
            if (MessageBox.Show("Изтриване на играча?", "Потвърди", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = Db.GetConnection())
                    {
                        SqlCommand cmd = new SqlCommand("DELETE FROM Players WHERE PlayerId=@id", conn);
                        cmd.Parameters.AddWithValue("@id", selectedPlayerId);
                        cmd.ExecuteNonQuery();
                        LoadPlayers();
                        ClearForm();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Грешка (може би играчът има статистика в мачове): " + ex.Message); }
            }
        }

        private void GridPlayers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < gridPlayers.Rows.Count)
            {
                var row = gridPlayers.Rows[e.RowIndex];
                if (row.Cells["PlayerId"].Value == DBNull.Value) return;

                selectedPlayerId = Convert.ToInt32(row.Cells["PlayerId"].Value);

                txtFirstName.Text = row.Cells["FirstName"].Value.ToString();
                txtLastName.Text = row.Cells["LastName"].Value.ToString();
                txtNation.Text = row.Cells["Nationality"].Value.ToString();
                txtKitNum.Text = row.Cells["KitNumber"].Value.ToString();

                if (row.Cells["Position"].Value != DBNull.Value)
                    comboPosition.SelectedItem = row.Cells["Position"].Value.ToString();

                if (row.Cells["BirthDate"].Value != DBNull.Value)
                    dateBirth.Value = Convert.ToDateTime(row.Cells["BirthDate"].Value);

                chkActive.Checked = Convert.ToBoolean(row.Cells["IsActive"].Value);

                // Нагласяне на Клуба в ComboBox-а
                if (row.Cells["CurrentClubId"].Value != DBNull.Value)
                    comboClub.SelectedValue = row.Cells["CurrentClubId"].Value;
                else
                    comboClub.SelectedIndex = 0; // Свободен агент

                btnSave.Text = "ПРОМЕНИ";
                btnSave.BackColor = Color.Yellow;
                btnDelete.Enabled = true;
            }
        }

        private void ClearForm()
        {
            txtFirstName.Clear(); txtLastName.Clear(); txtNation.Clear(); txtKitNum.Clear();
            comboPosition.SelectedIndex = -1;
            comboClub.SelectedIndex = -1;
            dateBirth.Value = DateTime.Now;
            selectedPlayerId = 0;
            btnSave.Text = "ЗАПИШИ";
            btnSave.BackColor = Color.LightGreen;
            btnDelete.Enabled = false;
        }
    }
}